using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryTrackingSystem.Api.Tests;

/// <summary>
/// Integration tests for <c>POST</c>/<c>GET</c>/<c>PUT /api/rooms</c>
/// (<see cref="Controllers.RoomsController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md AC-3
/// (admin + valid name/department -> 201 Created with the room echoed back;
/// admin + valid rename -> 200 OK with the renamed room), AC-4
/// (empty/whitespace name -> 400 ROOM_NAME_REQUIRED and no row
/// created/renamed), AC-5 (duplicate room name on create or rename -> 409
/// DUPLICATE_ROOM_NAME), AC-9/AC-10 (non-admin caller -> 403 Forbidden for
/// both list and rename), AC-13 (unknown departmentId on create -> 400
/// INVALID_DEPARTMENT; unknown oldName on rename -> 404 ROOM_NOT_FOUND), and
/// a happy-path <c>GET /api/rooms</c> listing. Each test builds its own
/// factory with the real <see cref="AppDbContext"/> SQL Server registration
/// swapped for a uniquely-named EF Core InMemory database, so no real SQL
/// Server is needed and tests never share state.
/// </summary>
public class RoomsControllerTests
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

                // EF Core's InMemory provider treats HasIndex().IsUnique() as a
                // no-op (indexes are a relational-only concept to it — only a
                // primary/alternate key gets identity-map enforcement), so it
                // never throws on a duplicate Room.Name the way SQL Server's
                // real unique index does in production. This interceptor
                // stands in for that missing enforcement so AC-5's
                // DUPLICATE_ROOM_NAME path (RoomsController's
                // catch (DbUpdateException)) is genuinely exercised here.
                services.AddDbContext<AppDbContext>(options => options
                    .UseInMemoryDatabase(dbName)
                    .AddInterceptors(new DuplicateRoomNameSimulatingInterceptor()));
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
    public async Task Create_ReturnsCreated_ForAdminWithValidNameAndDepartment()
    {
        // AC-3
        await using var factory = CreateFactory(nameof(Create_ReturnsCreated_ForAdminWithValidNameAndDepartment));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            name = "Conference Room A",
            departmentId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Conference Room A", body.Name);
        Assert.Equal(departmentId, body.DepartmentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ReturnsRoomNameRequired_ForEmptyOrWhitespaceName(string name)
    {
        // AC-4
        await using var factory = CreateFactory(
            $"{nameof(Create_ReturnsRoomNameRequired_ForEmptyOrWhitespaceName)}-{name.Length}");
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            name,
            departmentId,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ROOM_NAME_REQUIRED", body!.Error);
        Assert.Equal("Oda adı gereklidir.", body.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Rooms.CountAsync());
    }

    [Fact]
    public async Task Create_ReturnsDuplicateRoomName_ForSameNameTwice()
    {
        // AC-5
        await using var factory = CreateFactory(nameof(Create_ReturnsDuplicateRoomName_ForSameNameTwice));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { name = "Conference Room A", departmentId };

        var firstResponse = await client.PostAsJsonAsync("/api/rooms", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/rooms", request);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("DUPLICATE_ROOM_NAME", body!.Error);
        Assert.StartsWith("Kayıtlı Oda", body.Message);
    }

    [Fact]
    public async Task Create_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-9
        await using var factory = CreateFactory(nameof(Create_ReturnsForbidden_ForNonAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            name = "Conference Room A",
            departmentId,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsInvalidDepartment_ForUnknownDepartmentId()
    {
        // AC-13
        await using var factory = CreateFactory(nameof(Create_ReturnsInvalidDepartment_ForUnknownDepartmentId));
        await SeedKnownUserAsync(factory, yetkiId: true);
        await SeedDepartmentAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            name = "Conference Room A",
            departmentId = 999999,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_DEPARTMENT", body!.Error);
        Assert.Equal("Geçersiz departman.", body.Message);
    }

    [Fact]
    public async Task List_ReturnsRooms_ForAdmin()
    {
        await using var factory = CreateFactory(nameof(List_ReturnsRooms_ForAdmin));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        var roomId = await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<RoomListItem>>();
        Assert.NotNull(body);
        var room = Assert.Single(body!);
        Assert.Equal(roomId, room.Id);
        Assert.Equal("Conference Room A", room.Name);
    }

    [Fact]
    public async Task List_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-10
        // Prefixed (rather than the bare method name) because
        // DepartmentsControllerTests declares a test with this exact same
        // name — and EF Core's InMemory provider shares one underlying store
        // across DbContexts configured with the same database name, even
        // across unrelated WebApplicationFactory instances in the same test
        // process, so an unqualified name here would collide with that
        // other test's seeded "known.user" row and intermittently fail
        // Login's SingleOrDefaultAsync with "Sequence contains more than
        // one element".
        await using var factory = CreateFactory($"Rooms_{nameof(List_ReturnsForbidden_ForNonAdminCaller)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsOk_ForAdminWithValidRename()
    {
        // AC-3
        await using var factory = CreateFactory(nameof(Update_ReturnsOk_ForAdminWithValidRename));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/rooms", new
        {
            oldName = "Conference Room A",
            newName = "Meeting Room B",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(body);
        Assert.Equal("Meeting Room B", body!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_ReturnsRoomNameRequired_ForEmptyOrWhitespaceNewName(string newName)
    {
        // AC-4
        await using var factory = CreateFactory(
            $"{nameof(Update_ReturnsRoomNameRequired_ForEmptyOrWhitespaceNewName)}-{newName.Length}");
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/rooms", new
        {
            oldName = "Conference Room A",
            newName,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ROOM_NAME_REQUIRED", body!.Error);
        Assert.Equal("Oda adı gereklidir.", body.Message);
    }

    [Fact]
    public async Task Update_ReturnsDuplicateRoomName_ForRenameCollidingWithAnotherRoom()
    {
        // AC-5
        await using var factory = CreateFactory(nameof(Update_ReturnsDuplicateRoomName_ForRenameCollidingWithAnotherRoom));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Room A", departmentId);
        await SeedRoomAsync(factory, "Room B", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/rooms", new
        {
            oldName = "Room A",
            newName = "Room B",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("DUPLICATE_ROOM_NAME", body!.Error);
        Assert.Equal("Hatalı İşlem...", body.Message);
    }

    [Fact]
    public async Task Update_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-9
        await using var factory = CreateFactory(nameof(Update_ReturnsForbidden_ForNonAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/rooms", new
        {
            oldName = "Conference Room A",
            newName = "Meeting Room B",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsRoomNotFound_ForUnknownOldName()
    {
        // AC-13
        await using var factory = CreateFactory(nameof(Update_ReturnsRoomNotFound_ForUnknownOldName));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/rooms", new
        {
            oldName = "Nonexistent Room",
            newName = "Meeting Room B",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ROOM_NOT_FOUND", body!.Error);
        Assert.Equal("Hatalı İşlem...", body.Message);
    }

    [Fact]
    public async Task Delete_ReturnsOk_ForAdminWithMatchingName()
    {
        // AC-3
        await using var factory = CreateFactory(nameof(Delete_ReturnsOk_ForAdminWithMatchingName));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/rooms")
        {
            Content = JsonContent.Create(new { name = "Conference Room A" }),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(body);
        Assert.Equal("Conference Room A", body!.Name);
    }

    [Fact]
    public async Task Delete_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-4
        await using var factory = CreateFactory(nameof(Delete_ReturnsForbidden_ForNonAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/rooms")
        {
            Content = JsonContent.Create(new { name = "Conference Room A" }),
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsRoomNotFound_ForUnknownName()
    {
        // AC-11
        await using var factory = CreateFactory(nameof(Delete_ReturnsRoomNotFound_ForUnknownName));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/rooms")
        {
            Content = JsonContent.Create(new { name = "Nonexistent Room" }),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ROOM_NOT_FOUND", body!.Error);
        Assert.Equal("Hatalı İşlem...", body.Message);
    }

    [Fact]
    public async Task Delete_RemovesRoomFromDatabase_ForAdminWithMatchingName()
    {
        // AC-12
        await using var factory = CreateFactory(nameof(Delete_RemovesRoomFromDatabase_ForAdminWithMatchingName));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var departmentId = await SeedDepartmentAsync(factory);
        await SeedRoomAsync(factory, "Conference Room A", departmentId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/rooms")
        {
            Content = JsonContent.Create(new { name = "Conference Room A" }),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Rooms.CountAsync());
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

    private class RoomResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DepartmentId { get; set; }
    }

    private class RoomListItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stands in for the real SQL Server unique index on <see cref="Room.Name"/>
    /// (see <see cref="AppDbContext"/>), which the EF Core InMemory provider
    /// does not enforce: throws the same <see cref="DbUpdateException"/> a
    /// unique-index violation would produce in production, so
    /// <see cref="Controllers.RoomsController.Create"/>'s and
    /// <see cref="Controllers.RoomsController.Update"/>'s
    /// <c>catch (DbUpdateException)</c> paths are exercised for real by
    /// AC-5's duplicate-name tests. Covers both a newly <c>Added</c> room
    /// (Create) and an existing room's <c>Modified</c> <see cref="Room.Name"/>
    /// (Update's rename), excluding the candidate's own row (by
    /// <see cref="Room.Id"/>) from the duplicate check so a no-op rename
    /// isn't mistaken for a collision.
    /// </summary>
    private sealed class DuplicateRoomNameSimulatingInterceptor : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context is not null)
            {
                var candidates = context.ChangeTracker.Entries<Room>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .Select(e => new { e.Entity.Id, e.Entity.Name })
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var duplicateExists = await context.Set<Room>()
                        .AnyAsync(r => r.Name == candidate.Name && r.Id != candidate.Id, cancellationToken);
                    if (duplicateExists)
                    {
                        throw new DbUpdateException(
                            $"Simulated unique-index violation for Room.Name '{candidate.Name}'.");
                    }
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
