using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryTrackingSystem.Api.Tests;

/// <summary>
/// Integration tests for <c>POST</c>/<c>GET /api/asset-assignments</c>
/// (<see cref="Controllers.AssetAssignmentsController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md AC-7
/// (missing roomId/assetId -> 400 SELECTION_REQUIRED), AC-8 (missing or
/// non-positive quantity -> 400 QUANTITY_REQUIRED), AC-9 (unknown roomId ->
/// 400 INVALID_ROOM), AC-10 (unknown assetId -> 400 INVALID_ASSET), AC-11
/// (quantity exceeding stock -> 400 INSUFFICIENT_STOCK, with no partial
/// writes), a boundary test proving the stock guard is strictly greater-than,
/// AC-12 (no responsibility row on file for the room -> 400
/// NO_RESPONSIBLE_PERSONNEL), AC-13 (the core atomicity proof — a successful
/// create both inserts the assignment row and decrements the asset's stock
/// in the same <c>SaveChangesAsync</c> call), AC-14 (the most recently
/// created responsibility row wins when a room has more than one), and
/// AC-15 (List only returns asset-issue rows for the room, excluding
/// responsibility rows and other rooms' rows). Each test builds its own
/// factory with the real <see cref="AppDbContext"/> SQL Server registration
/// swapped for a uniquely-named EF Core InMemory database, so no real SQL
/// Server is needed and tests never share state.
/// </summary>
public class AssetAssignmentsControllerTests
{
    private const string KnownUsername = "known.user";
    private const string KnownPassword = "correct horse battery staple";

    private static WebApplicationFactory<Program> CreateFactory(string dbName)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options => options
                    .UseInMemoryDatabase(dbName));
            });
        });
    }

    private static async Task SeedKnownUserAsync(WebApplicationFactory<Program> factory, bool? yetkiId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = new PasswordHasherService();

        db.Users.Add(new User
        {
            Username = KnownUsername,
            PasswordHash = hasher.Hash(KnownPassword),
            YetkiID = yetkiId,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedDepartmentAsync(WebApplicationFactory<Program> factory, string name = "Sales")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var department = new Department { Name = name };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return department.Id;
    }

    private static async Task<int> SeedRoomAsync(WebApplicationFactory<Program> factory, string name, int departmentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var room = new Room { Name = name, DepartmentId = departmentId };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return room.Id;
    }

    private static async Task<int> SeedAssetTypeAsync(WebApplicationFactory<Program> factory, string name = "Elektronik")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var assetType = new AssetType { Name = name };
        db.AssetTypes.Add(assetType);
        await db.SaveChangesAsync();

        return assetType.Id;
    }

    private static async Task<int> SeedFixedAssetAsync(
        WebApplicationFactory<Program> factory,
        string name,
        int assetTypeId,
        int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var asset = new FixedAsset
        {
            Name = name,
            Price = 199.99m,
            PurchaseDate = new DateTime(2026, 1, 15),
            AssetTypeId = assetTypeId,
            Quantity = quantity,
        };
        db.FixedAssets.Add(asset);
        await db.SaveChangesAsync();

        return asset.Id;
    }

    private static async Task<int> SeedPersonnelAsync(WebApplicationFactory<Program> factory, string firstName = "Ayşe", string lastName = "Yılmaz")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var personnel = new Personnel { FirstName = firstName, LastName = lastName };
        db.Personnel.Add(personnel);
        await db.SaveChangesAsync();

        return personnel.Id;
    }

    /// <summary>
    /// Inserts a responsibility row directly via a fresh <see cref="AppDbContext"/>
    /// scope — a null-<see cref="RoomAssetAssignment.AssetId"/> row that mirrors
    /// what <see cref="Controllers.RoomAssignmentsController.Create"/> would
    /// produce, without an HTTP round-trip.
    /// </summary>
    private static async Task<int> SeedRoomResponsibilityAsync(WebApplicationFactory<Program> factory, int roomId, int personnelId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var responsibility = new RoomAssetAssignment { RoomId = roomId, PersonnelId = personnelId };
        db.RoomAssetAssignments.Add(responsibility);
        await db.SaveChangesAsync();

        return responsibility.Id;
    }

    /// <summary>
    /// Inserts an asset-issue row directly via a fresh <see cref="AppDbContext"/>
    /// scope — a fully-populated row, as <see cref="Controllers.AssetAssignmentsController.Create"/>
    /// would produce, without an HTTP round-trip.
    /// </summary>
    private static async Task<int> SeedAssetIssueAsync(WebApplicationFactory<Program> factory, int roomId, int assetId, int personnelId, int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var issue = new RoomAssetAssignment { RoomId = roomId, AssetId = assetId, PersonnelId = personnelId, Quantity = quantity };
        db.RoomAssetAssignments.Add(issue);
        await db.SaveChangesAsync();

        return issue.Id;
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = KnownUsername,
            password = KnownPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);

        return body!.Token;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_ReturnsSelectionRequired_ForMissingRoomOrAsset(bool omitRoomId)
    {
        // AC-7
        await using var factory = CreateFactory(
            $"{nameof(Create_ReturnsSelectionRequired_ForMissingRoomOrAsset)}-{omitRoomId}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId = omitRoomId ? (int?)null : roomId,
            assetId = omitRoomId ? assetId : (int?)null,
            quantity = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("SELECTION_REQUIRED", body!.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_ReturnsQuantityRequired_ForMissingOrNonPositiveQuantity(int? quantity)
    {
        // AC-8
        await using var factory = CreateFactory(
            $"{nameof(Create_ReturnsQuantityRequired_ForMissingOrNonPositiveQuantity)}-{quantity}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("QUANTITY_REQUIRED", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsInvalidRoom_ForUnknownRoomId()
    {
        // AC-9
        await using var factory = CreateFactory($"AssetAssignments_{nameof(Create_ReturnsInvalidRoom_ForUnknownRoomId)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId = 999999,
            assetId,
            quantity = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_ROOM", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsInvalidAsset_ForUnknownAssetId()
    {
        // AC-10
        await using var factory = CreateFactory(nameof(Create_ReturnsInvalidAsset_ForUnknownAssetId));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId = 999999,
            quantity = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_ASSET", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsInsufficientStock_ForQuantityExceedingStock()
    {
        // AC-11
        await using var factory = CreateFactory(nameof(Create_ReturnsInsufficientStock_ForQuantityExceedingStock));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        await SeedRoomResponsibilityAsync(factory, roomId, personnelId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 5);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity = 6,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INSUFFICIENT_STOCK", body!.Error);
        Assert.StartsWith("Girilen değer stok miktarından fazla", body.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.RoomAssetAssignments.AnyAsync(a => a.RoomId == roomId && a.AssetId != null));
        var asset = await db.FixedAssets.SingleAsync(a => a.Id == assetId);
        Assert.Equal(5, asset.Quantity);
    }

    [Fact]
    public async Task Create_ReturnsCreated_ForQuantityExactlyEqualToStock()
    {
        // Boundary test — the stock guard must be strictly greater-than, not >=
        await using var factory = CreateFactory(nameof(Create_ReturnsCreated_ForQuantityExactlyEqualToStock));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        await SeedRoomResponsibilityAsync(factory, roomId, personnelId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 5);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await db.FixedAssets.SingleAsync(a => a.Id == assetId);
        Assert.Equal(0, asset.Quantity);
    }

    [Fact]
    public async Task Create_ReturnsNoResponsiblePersonnel_ForRoomWithNoResponsibilityRow()
    {
        // AC-12
        await using var factory = CreateFactory(nameof(Create_ReturnsNoResponsiblePersonnel_ForRoomWithNoResponsibilityRow));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("NO_RESPONSIBLE_PERSONNEL", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsCreated_AndAtomicallyInsertsAssignmentAndDecrementsStock()
    {
        // AC-13 — core atomicity proof
        await using var factory = CreateFactory(nameof(Create_ReturnsCreated_AndAtomicallyInsertsAssignmentAndDecrementsStock));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        await SeedRoomResponsibilityAsync(factory, roomId, personnelId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 20);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity = 7,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateAssetAssignmentResponse>();
        Assert.NotNull(body);
        Assert.Equal(13, body!.RemainingStock);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assignment = await db.RoomAssetAssignments.SingleAsync(a => a.RoomId == roomId && a.AssetId == assetId);
        Assert.Equal(7, assignment.Quantity);
        Assert.Equal(personnelId, assignment.PersonnelId);

        var asset = await db.FixedAssets.SingleAsync(a => a.Id == assetId);
        Assert.Equal(13, asset.Quantity);
    }

    [Fact]
    public async Task Create_UsesMostRecentResponsibilityRow_WhenRoomHasMultiple()
    {
        // AC-14
        await using var factory = CreateFactory(nameof(Create_UsesMostRecentResponsibilityRow_WhenRoomHasMultiple));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var firstPersonnelId = await SeedPersonnelAsync(factory, "Ayşe", "Yılmaz");
        await SeedRoomResponsibilityAsync(factory, roomId, firstPersonnelId);
        var secondPersonnelId = await SeedPersonnelAsync(factory, "Mehmet", "Demir");
        await SeedRoomResponsibilityAsync(factory, roomId, secondPersonnelId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/asset-assignments", new
        {
            roomId,
            assetId,
            quantity = 1,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateAssetAssignmentResponse>();
        Assert.NotNull(body);
        Assert.Equal(secondPersonnelId, body!.PersonnelId);
    }

    [Fact]
    public async Task List_ReturnsOnlyAssetIssueRows_ForRoom()
    {
        // AC-15
        await using var factory = CreateFactory(nameof(List_ReturnsOnlyAssetIssueRows_ForRoom));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomAId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var roomBId = await SeedRoomAsync(factory, "Conference Room B", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        await SeedRoomResponsibilityAsync(factory, roomAId, personnelId);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetAId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId, quantity: 10);
        var assetBId = await SeedFixedAssetAsync(factory, "Masaüstü Bilgisayar", assetTypeId, quantity: 10);
        await SeedAssetIssueAsync(factory, roomAId, assetAId, personnelId, quantity: 3);
        await SeedAssetIssueAsync(factory, roomBId, assetBId, personnelId, quantity: 4);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/asset-assignments?roomId={roomAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<AssetAssignmentListItem>>();
        Assert.NotNull(body);
        var item = Assert.Single(body!);
        Assert.Equal(assetAId, item.AssetId);
        Assert.Equal("Dizüstü Bilgisayar", item.AssetName);
        Assert.Equal(3, item.Quantity);
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    private class CreateAssetAssignmentResponse
    {
        public int Id { get; set; }

        public int? RoomId { get; set; }

        public int? AssetId { get; set; }

        public int? PersonnelId { get; set; }

        public int? Quantity { get; set; }

        public int RemainingStock { get; set; }
    }

    private class AssetAssignmentListItem
    {
        public int Id { get; set; }

        public int? AssetId { get; set; }

        public string AssetName { get; set; } = string.Empty;

        public int? Quantity { get; set; }
    }
}
