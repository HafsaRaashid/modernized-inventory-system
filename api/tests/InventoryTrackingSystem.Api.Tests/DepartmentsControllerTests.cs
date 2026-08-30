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
/// Integration tests for <c>GET /api/departments</c> (<see cref="Controllers.DepartmentsController"/>)
/// via <see cref="WebApplicationFactory{TEntryPoint}"/>, covering the
/// happy path (admin caller with seeded departments -> 200 OK listing them)
/// and spec.md AC-10 (non-admin caller -> 403 Forbidden). Each test builds
/// its own factory with the real <see cref="AppDbContext"/> SQL Server
/// registration swapped for a uniquely-named EF Core InMemory database, so
/// no real SQL Server is needed and tests never share state.
/// </summary>
public class DepartmentsControllerTests
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

    private static async Task SeedDepartmentsAsync(WebApplicationFactory<Program> factory, params string[] names)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var name in names)
        {
            db.Departments.Add(new Department { Name = name });
        }

        await db.SaveChangesAsync();
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
    public async Task List_ReturnsOkWithSeededDepartments_ForAdminCaller()
    {
        await using var factory = CreateFactory(nameof(List_ReturnsOkWithSeededDepartments_ForAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: true);
        await SeedDepartmentsAsync(factory, "Sales", "Warehouse");
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/departments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        Assert.Contains(body, d => d.Name == "Sales");
        Assert.Contains(body, d => d.Name == "Warehouse");
        Assert.All(body, d => Assert.True(d.Id > 0));
    }

    [Fact]
    public async Task List_ReturnsForbidden_ForNonAdminCaller()
    {
        // AC-10
        await using var factory = CreateFactory(nameof(List_ReturnsForbidden_ForNonAdminCaller));
        await SeedKnownUserAsync(factory, yetkiId: false);
        await SeedDepartmentsAsync(factory, "Sales");
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/departments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;
    }

    private class DepartmentResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
