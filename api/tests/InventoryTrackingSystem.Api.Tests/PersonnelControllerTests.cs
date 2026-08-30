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
/// Integration tests for <c>GET /api/personnel</c>
/// (<see cref="Controllers.PersonnelController"/>) via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, covering the
/// Room Assignment screen's personnel selector: an authenticated, non-admin
/// caller can list seeded <see cref="Personnel"/> rows, since this endpoint
/// is <c>[Authorize]</c>-only and performs no admin check. Each test builds
/// its own factory with the real <see cref="AppDbContext"/> SQL Server
/// registration swapped for a uniquely-named EF Core InMemory database, so
/// no real SQL Server is needed and tests never share state.
/// </summary>
public class PersonnelControllerTests
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

    private static async Task<int> SeedPersonnelAsync(WebApplicationFactory<Program> factory, string firstName, string lastName)
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
    public async Task List_ReturnsSeededPersonnel_ForNonAdminCaller()
    {
        await using var factory = CreateFactory(nameof(List_ReturnsSeededPersonnel_ForNonAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var firstId = await SeedPersonnelAsync(factory, "Ayşe", "Yılmaz");
        var secondId = await SeedPersonnelAsync(factory, "Mehmet", "Demir");
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/personnel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<PersonnelListItem>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);

        var first = body.Single(p => p.Id == firstId);
        Assert.Equal("Ayşe", first.FirstName);
        Assert.Equal("Yılmaz", first.LastName);

        var second = body.Single(p => p.Id == secondId);
        Assert.Equal("Mehmet", second.FirstName);
        Assert.Equal("Demir", second.LastName);
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
    }

    private class PersonnelListItem
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }
}
