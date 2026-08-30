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
/// Integration tests for <c>POST /api/fixed-assets</c>
/// (<see cref="Controllers.FixedAssetsController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md AC-3
/// (admin + all valid fields -> 201 Created with the asset echoed back),
/// AC-8 (non-admin caller -> 403 Forbidden), AC-5 (empty/whitespace name ->
/// 400 ASSET_NAME_REQUIRED and no row created), AC-6 (unknown assetTypeId
/// -> 400 INVALID_ASSET_TYPE), and AC-7 (duplicate asset name -> 409
/// DUPLICATE_ASSET_NAME). Each test builds its own factory with the real
/// <see cref="AppDbContext"/> SQL Server registration swapped for a
/// uniquely-named EF Core InMemory database, so no real SQL Server is
/// needed and tests never share state.
/// </summary>
public class FixedAssetsControllerTests
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
                // never throws on a duplicate FixedAsset.Name the way SQL
                // Server's real unique index does in production. This
                // interceptor stands in for that missing enforcement so
                // AC-7's DUPLICATE_ASSET_NAME path (FixedAssetsController's
                // catch (DbUpdateException)) is genuinely exercised here.
                services.AddDbContext<AppDbContext>(options => options
                    .UseInMemoryDatabase(dbName)
                    .AddInterceptors(new DuplicateFixedAssetNameSimulatingInterceptor()));
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
    public async Task Create_ReturnsCreated_ForAdminWithValidFields()
    {
        // AC-3/AC-8
        await using var factory = CreateFactory(nameof(Create_ReturnsCreated_ForAdminWithValidFields));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var purchaseDate = new DateTime(2026, 1, 15);

        var response = await client.PostAsJsonAsync("/api/fixed-assets", new
        {
            name = "Dizüstü Bilgisayar",
            price = 199.99m,
            purchaseDate,
            assetTypeId,
            quantity = 10,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FixedAssetResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal("Dizüstü Bilgisayar", body.Name);
        Assert.Equal(199.99m, body.Price);
        Assert.Equal(purchaseDate, body.PurchaseDate);
        Assert.Equal(assetTypeId, body.AssetTypeId);
        Assert.Equal(10, body.Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName(string name)
    {
        // AC-5
        await using var factory = CreateFactory(
            $"{nameof(Create_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName)}-{name.Length}");
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/fixed-assets", new
        {
            name,
            price = 199.99m,
            purchaseDate = new DateTime(2026, 1, 15),
            assetTypeId,
            quantity = 10,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ASSET_NAME_REQUIRED", body!.Error);
        Assert.Equal("Demirbaş adı gereklidir.", body.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.FixedAssets.CountAsync());
    }

    [Fact]
    public async Task Create_ReturnsInvalidAssetType_ForUnknownAssetTypeId()
    {
        // AC-6
        await using var factory = CreateFactory(nameof(Create_ReturnsInvalidAssetType_ForUnknownAssetTypeId));
        await SeedKnownUserAsync(factory, yetkiId: true);
        await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/fixed-assets", new
        {
            name = "Dizüstü Bilgisayar",
            price = 199.99m,
            purchaseDate = new DateTime(2026, 1, 15),
            assetTypeId = 999999,
            quantity = 10,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_ASSET_TYPE", body!.Error);
        Assert.Equal("Geçersiz demirbaş türü.", body.Message);
    }

    [Fact]
    public async Task Create_ReturnsDuplicateAssetName_ForSameNameTwice()
    {
        // AC-7
        await using var factory = CreateFactory(nameof(Create_ReturnsDuplicateAssetName_ForSameNameTwice));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            name = "Dizüstü Bilgisayar",
            price = 199.99m,
            purchaseDate = new DateTime(2026, 1, 15),
            assetTypeId,
            quantity = 10,
        };

        var firstResponse = await client.PostAsJsonAsync("/api/fixed-assets", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/fixed-assets", request);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("DUPLICATE_ASSET_NAME", body!.Error);
        Assert.StartsWith("Kayıtlı Demirbaş", body.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.FixedAssets.CountAsync());
    }

    [Fact]
    public async Task Create_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-8
        await using var factory = CreateFactory($"FixedAssets_{nameof(Create_ReturnsForbidden_ForNonAdminCaller)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/fixed-assets", new
        {
            name = "Dizüstü Bilgisayar",
            price = 199.99m,
            purchaseDate = new DateTime(2026, 1, 15),
            assetTypeId,
            quantity = 10,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private class FixedAssetResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public DateTime PurchaseDate { get; set; }

        public int AssetTypeId { get; set; }

        public int Quantity { get; set; }
    }

    /// <summary>
    /// Stands in for the real SQL Server unique index on
    /// <see cref="FixedAsset.Name"/> (see <see cref="AppDbContext"/>), which
    /// the EF Core InMemory provider does not enforce: throws the same
    /// <see cref="DbUpdateException"/> a unique-index violation would
    /// produce in production, so
    /// <see cref="Controllers.FixedAssetsController.Create"/>'s
    /// <c>catch (DbUpdateException)</c> path is exercised for real by AC-7's
    /// duplicate-name test. Only checks newly <c>Added</c> assets — unlike
    /// <see cref="RoomsControllerTests.DuplicateRoomNameSimulatingInterceptor"/>,
    /// there is no rename/Update endpoint for <see cref="FixedAsset"/> to
    /// cover.
    /// </summary>
    private sealed class DuplicateFixedAssetNameSimulatingInterceptor : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context is not null)
            {
                var candidates = context.ChangeTracker.Entries<FixedAsset>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => new { e.Entity.Id, e.Entity.Name })
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var duplicateExists = await context.Set<FixedAsset>()
                        .AnyAsync(a => a.Name == candidate.Name && a.Id != candidate.Id, cancellationToken);
                    if (duplicateExists)
                    {
                        throw new DbUpdateException(
                            $"Simulated unique-index violation for FixedAsset.Name '{candidate.Name}'.");
                    }
                }
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
