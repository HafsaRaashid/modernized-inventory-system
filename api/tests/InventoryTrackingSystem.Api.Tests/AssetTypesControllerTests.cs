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
/// Integration tests for <c>GET /api/asset-types</c>
/// (<see cref="Controllers.AssetTypesController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering a happy-path
/// listing for an admin caller and the non-admin-caller 403 Forbidden path.
/// Each test builds its own factory with the real <see cref="AppDbContext"/>
/// SQL Server registration swapped for a uniquely-named EF Core InMemory
/// database, so no real SQL Server is needed and tests never share state.
/// </summary>
public class AssetTypesControllerTests
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

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
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

    private static async Task<int> SeedAssetTypeAsync(WebApplicationFactory<Program> factory, string name = "Elektronik")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var assetType = new AssetType { Name = name };
        db.AssetTypes.Add(assetType);
        await db.SaveChangesAsync();

        return assetType.Id;
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
    public async Task List_ReturnsAssetTypes_ForAdmin()
    {
        await using var factory = CreateFactory(nameof(List_ReturnsAssetTypes_ForAdmin));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var elektronikId = await SeedAssetTypeAsync(factory, "Elektronik");
        var mobilyaId = await SeedAssetTypeAsync(factory, "Mobilya");
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/asset-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<AssetTypeResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        Assert.Contains(body, t => t.Id == elektronikId && t.Name == "Elektronik");
        Assert.Contains(body, t => t.Id == mobilyaId && t.Name == "Mobilya");
    }

    [Fact]
    public async Task List_ReturnsForbidden_ForNonAdminCaller()
    {
        // Prefixed (rather than the bare method name) because other test
        // classes declare tests with this exact same name — and EF Core's
        // InMemory provider shares one underlying store across DbContexts
        // configured with the same database name, even across unrelated
        // WebApplicationFactory instances in the same test process, so an
        // unqualified name here would collide with that other test's seeded
        // "known.user" row and intermittently fail Login's
        // SingleOrDefaultAsync with "Sequence contains more than one
        // element".
        await using var factory = CreateFactory($"AssetTypes_{nameof(List_ReturnsForbidden_ForNonAdminCaller)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/asset-types");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
    }

    private class AssetTypeResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
