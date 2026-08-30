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
/// Integration tests for <c>POST</c>/<c>GET</c>/<c>PUT /api/fixed-assets</c>
/// (<see cref="Controllers.FixedAssetsController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md AC-3
/// (admin + all valid fields -> 201 Created with the asset echoed back on
/// Create; admin + valid edit -> 200 OK with the updated fields on Update),
/// AC-8 (non-admin caller -> 403 Forbidden for Create), AC-5 (empty/whitespace
/// name -> 400 ASSET_NAME_REQUIRED and no row created, Create), AC-6
/// (unknown assetTypeId -> 400 INVALID_ASSET_TYPE, Create; empty/whitespace
/// name -> 400 ASSET_NAME_REQUIRED and no row changed, Update), AC-7
/// (duplicate asset name -> 409 DUPLICATE_ASSET_NAME, Create; unknown
/// assetTypeId -> 400 INVALID_ASSET_TYPE, Update), AC-8a/AC-8b (rename
/// colliding with another asset -> 409 DUPLICATE_ASSET_NAME; no-op rename to
/// the asset's own current name -> 200 OK, Update), AC-9 (unknown id -> 404
/// ASSET_NOT_FOUND, Update), AC-11a/AC-11b (List returns all assets for an
/// admin; 403 Forbidden for a non-admin), and non-admin -> 403 Forbidden for
/// Update. Each test builds its own factory with the real
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

    private static async Task<int> SeedFixedAssetAsync(
        WebApplicationFactory<Program> factory,
        string name,
        int assetTypeId,
        decimal price = 199.99m,
        DateTime? purchaseDate = null,
        int quantity = 10)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var asset = new FixedAsset
        {
            Name = name,
            Price = price,
            PurchaseDate = purchaseDate ?? new DateTime(2026, 1, 15),
            AssetTypeId = assetTypeId,
            Quantity = quantity,
        };
        db.FixedAssets.Add(asset);
        await db.SaveChangesAsync();

        return asset.Id;
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

    [Fact]
    public async Task List_ReturnsFixedAssets_ForAdmin()
    {
        // AC-11a
        await using var factory = CreateFactory(nameof(List_ReturnsFixedAssets_ForAdmin));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/fixed-assets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<FixedAssetResponse>>();
        Assert.NotNull(body);
        var asset = Assert.Single(body!);
        Assert.Equal(assetId, asset.Id);
        Assert.Equal("Dizüstü Bilgisayar", asset.Name);
        Assert.Equal(199.99m, asset.Price);
        Assert.Equal(new DateTime(2026, 1, 15), asset.PurchaseDate);
        Assert.Equal(assetTypeId, asset.AssetTypeId);
        Assert.Equal(10, asset.Quantity);
    }

    [Fact]
    public async Task List_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-11b
        await using var factory = CreateFactory($"FixedAssets_{nameof(List_ReturnsForbidden_ForNonAdminCaller)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/fixed-assets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsOk_ForAdminWithValidEdit()
    {
        // AC-3
        await using var factory = CreateFactory(nameof(Update_ReturnsOk_ForAdminWithValidEdit));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var otherAssetTypeId = await SeedAssetTypeAsync(factory, "Mobilya");
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newPurchaseDate = new DateTime(2026, 3, 1);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetId,
            name = "Masaüstü Bilgisayar",
            price = 299.99m,
            purchaseDate = newPurchaseDate,
            assetTypeId = otherAssetTypeId,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FixedAssetResponse>();
        Assert.NotNull(body);
        Assert.Equal(assetId, body!.Id);
        Assert.Equal("Masaüstü Bilgisayar", body.Name);
        Assert.Equal(299.99m, body.Price);
        Assert.Equal(newPurchaseDate, body.PurchaseDate);
        Assert.Equal(otherAssetTypeId, body.AssetTypeId);
        Assert.Equal(5, body.Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName(string name)
    {
        // AC-6
        await using var factory = CreateFactory(
            $"{nameof(Update_ReturnsAssetNameRequired_ForEmptyOrWhitespaceName)}-{name.Length}");
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetId,
            name,
            price = 299.99m,
            purchaseDate = new DateTime(2026, 3, 1),
            assetTypeId,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ASSET_NAME_REQUIRED", body!.Error);
        Assert.Equal("Demirbaş adı gereklidir.", body.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = await db.FixedAssets.SingleAsync(a => a.Id == assetId);
        Assert.Equal("Dizüstü Bilgisayar", asset.Name);
    }

    [Fact]
    public async Task Update_ReturnsInvalidAssetType_ForUnknownAssetTypeId()
    {
        // AC-7
        await using var factory = CreateFactory(nameof(Update_ReturnsInvalidAssetType_ForUnknownAssetTypeId));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetId,
            name = "Masaüstü Bilgisayar",
            price = 299.99m,
            purchaseDate = new DateTime(2026, 3, 1),
            assetTypeId = 999999,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_ASSET_TYPE", body!.Error);
        Assert.Equal("Geçersiz demirbaş türü.", body.Message);
    }

    [Fact]
    public async Task Update_ReturnsDuplicateAssetName_ForRenameCollidingWithAnotherAsset()
    {
        // AC-8a
        await using var factory = CreateFactory(nameof(Update_ReturnsDuplicateAssetName_ForRenameCollidingWithAnotherAsset));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetAId = await SeedFixedAssetAsync(factory, "Asset A", assetTypeId);
        await SeedFixedAssetAsync(factory, "Asset B", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetAId,
            name = "Asset B",
            price = 199.99m,
            purchaseDate = new DateTime(2026, 1, 15),
            assetTypeId,
            quantity = 10,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("DUPLICATE_ASSET_NAME", body!.Error);
        Assert.StartsWith("Kayıtlı Demirbaş", body.Message);
    }

    [Fact]
    public async Task Update_ReturnsOk_ForNoOpRenameToOwnCurrentName()
    {
        // AC-8b
        await using var factory = CreateFactory(nameof(Update_ReturnsOk_ForNoOpRenameToOwnCurrentName));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Asset A", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetId,
            name = "Asset A",
            price = 299.99m,
            purchaseDate = new DateTime(2026, 3, 1),
            assetTypeId,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FixedAssetResponse>();
        Assert.NotNull(body);
        Assert.Equal("Asset A", body!.Name);
    }

    [Fact]
    public async Task Update_ReturnsAssetNotFound_ForUnknownId()
    {
        // AC-9
        await using var factory = CreateFactory(nameof(Update_ReturnsAssetNotFound_ForUnknownId));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = 999999,
            name = "Masaüstü Bilgisayar",
            price = 299.99m,
            purchaseDate = new DateTime(2026, 3, 1),
            assetTypeId,
            quantity = 5,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("ASSET_NOT_FOUND", body!.Error);
        Assert.Equal("Demirbaş bulunamadı.", body.Message);
    }

    [Fact]
    public async Task Update_ReturnsForbidden_ForNonAdminCaller()
    {
        await using var factory = CreateFactory($"FixedAssets_{nameof(Update_ReturnsForbidden_ForNonAdminCaller)}");
        await SeedKnownUserAsync(factory, yetkiId: false);
        var assetTypeId = await SeedAssetTypeAsync(factory);
        var assetId = await SeedFixedAssetAsync(factory, "Dizüstü Bilgisayar", assetTypeId);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/fixed-assets", new
        {
            id = assetId,
            name = "Masaüstü Bilgisayar",
            price = 299.99m,
            purchaseDate = new DateTime(2026, 3, 1),
            assetTypeId,
            quantity = 5,
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
    /// <see cref="Controllers.FixedAssetsController.Create"/>'s and
    /// <see cref="Controllers.FixedAssetsController.Update"/>'s
    /// <c>catch (DbUpdateException)</c> paths are exercised for real by
    /// AC-7's and AC-8a's duplicate-name tests. Covers both a newly
    /// <c>Added</c> asset (Create) and an existing asset's <c>Modified</c>
    /// <see cref="FixedAsset.Name"/> (Update's rename), excluding the
    /// candidate's own row (by <see cref="FixedAsset.Id"/>) from the
    /// duplicate check so a no-op rename isn't mistaken for a collision —
    /// mirrors <see cref="RoomsControllerTests.DuplicateRoomNameSimulatingInterceptor"/>.
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
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
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
