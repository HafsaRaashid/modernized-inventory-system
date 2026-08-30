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
/// Integration tests for <c>POST /api/rooms</c> (<see cref="Controllers.RoomsController"/>)
/// via <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md
/// AC-3 (admin + valid name/department -> 201 Created with the room echoed
/// back), AC-4 (empty/whitespace name -> 400 ROOM_NAME_REQUIRED and no row
/// created), AC-5 (duplicate room name -> 409 DUPLICATE_ROOM_NAME), AC-9
/// (non-admin caller -> 403 Forbidden), and AC-13 (unknown departmentId ->
/// 400 INVALID_DEPARTMENT). Each test builds its own factory with the real
/// <see cref="AppDbContext"/> SQL Server registration swapped for a
/// uniquely-named EF Core InMemory database, so no real SQL Server is
/// needed and tests never share state.
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

    /// <summary>
    /// Stands in for the real SQL Server unique index on <see cref="Room.Name"/>
    /// (see <see cref="AppDbContext"/>), which the EF Core InMemory provider
    /// does not enforce: throws the same <see cref="DbUpdateException"/> a
    /// unique-index violation would produce in production, so
    /// <see cref="Controllers.RoomsController.Create"/>'s
    /// <c>catch (DbUpdateException)</c> path is exercised for real by AC-5's
    /// duplicate-name test.
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
                var addedNames = context.ChangeTracker.Entries<Room>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity.Name)
                    .ToList();

                foreach (var name in addedNames)
                {
                    var duplicateExists = await context.Set<Room>()
                        .AnyAsync(r => r.Name == name, cancellationToken);
                    if (duplicateExists)
                    {
                        throw new DbUpdateException($"Simulated unique-index violation for Room.Name '{name}'.");
                    }
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
