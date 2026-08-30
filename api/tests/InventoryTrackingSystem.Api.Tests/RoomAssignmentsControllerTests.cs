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
/// Integration tests for <c>POST /api/room-assignments</c>
/// (<see cref="Controllers.RoomAssignmentsController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md AC-4
/// (authenticated, non-admin caller with valid roomId/personnelId -> 201
/// Created, echoing both ids back), AC-6 (either selection null -> 400
/// SELECTION_REQUIRED), AC-7 (unknown roomId -> 400 INVALID_ROOM; unknown
/// personnelId -> 400 INVALID_PERSONNEL), and AC-13 (submitting the exact
/// same valid pair twice both succeed with 201 — no upsert/deduplication,
/// proven by asserting two rows exist afterward). Each test builds its own
/// factory with the real <see cref="AppDbContext"/> SQL Server registration
/// swapped for a uniquely-named EF Core InMemory database, so no real SQL
/// Server is needed and tests never share state.
/// </summary>
public class RoomAssignmentsControllerTests
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

    private static async Task<int> SeedPersonnelAsync(WebApplicationFactory<Program> factory, string firstName = "Ayşe", string lastName = "Yılmaz")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var personnel = new Personnel { FirstName = firstName, LastName = lastName };
        db.Personnel.Add(personnel);
        await db.SaveChangesAsync();

        return personnel.Id;
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

    [Fact]
    public async Task Create_ReturnsCreated_ForNonAdminWithValidRoomAndPersonnel()
    {
        // AC-4
        await using var factory = CreateFactory(nameof(Create_ReturnsCreated_ForNonAdminWithValidRoomAndPersonnel));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/room-assignments", new
        {
            roomId,
            personnelId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RoomAssignmentResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal(roomId, body.RoomId);
        Assert.Equal(personnelId, body.PersonnelId);
    }

    [Fact]
    public async Task Create_ReturnsSelectionRequired_ForNullRoomId()
    {
        // AC-6
        await using var factory = CreateFactory(nameof(Create_ReturnsSelectionRequired_ForNullRoomId));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var personnelId = await SeedPersonnelAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/room-assignments", new
        {
            roomId = (int?)null,
            personnelId,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("SELECTION_REQUIRED", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsSelectionRequired_ForNullPersonnelId()
    {
        // AC-6
        await using var factory = CreateFactory(nameof(Create_ReturnsSelectionRequired_ForNullPersonnelId));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/room-assignments", new
        {
            roomId,
            personnelId = (int?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("SELECTION_REQUIRED", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsInvalidRoom_ForUnknownRoomId()
    {
        // AC-7
        await using var factory = CreateFactory(nameof(Create_ReturnsInvalidRoom_ForUnknownRoomId));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var personnelId = await SeedPersonnelAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/room-assignments", new
        {
            roomId = 999999,
            personnelId,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_ROOM", body!.Error);
    }

    [Fact]
    public async Task Create_ReturnsInvalidPersonnel_ForUnknownPersonnelId()
    {
        // AC-7
        await using var factory = CreateFactory(nameof(Create_ReturnsInvalidPersonnel_ForUnknownPersonnelId));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/room-assignments", new
        {
            roomId,
            personnelId = 999999,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_PERSONNEL", body!.Error);
    }

    [Fact]
    public async Task Create_AllowsSamePairTwice_WithNoDeduplication()
    {
        // AC-13
        await using var factory = CreateFactory(nameof(Create_AllowsSamePairTwice_WithNoDeduplication));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var personnelId = await SeedPersonnelAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { roomId, personnelId };

        var firstResponse = await client.PostAsJsonAsync("/api/room-assignments", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/room-assignments", request);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.RoomAssetAssignments.CountAsync());
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

    private class RoomAssignmentResponse
    {
        public int Id { get; set; }

        public int? RoomId { get; set; }

        public int? PersonnelId { get; set; }
    }
}
