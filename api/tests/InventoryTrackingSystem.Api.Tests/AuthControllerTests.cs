using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using InventoryTrackingSystem.Domain.Entities;
using InventoryTrackingSystem.Infrastructure.Auth;
using InventoryTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace InventoryTrackingSystem.Api.Tests;

/// <summary>
/// Integration tests for <c>POST /api/auth/login</c> (<see cref="AuthController"/>)
/// via <see cref="WebApplicationFactory{TEntryPoint}"/>, covering spec.md
/// AC-1 (correct credentials -> 200 + a well-formed signed JWT), AC-2
/// (wrong password -> 401 with the exact rejection body), AC-3 (an empty
/// username or password falls through the SAME 401 path, not a distinct
/// error), and AC-6 (the issued token is well-formed and its signature
/// validates), plus <c>GET /api/auth/me</c> (BL-003) AC-3 through AC-7:
/// a valid token reports <c>isAdmin</c> from <c>YetkiID</c> (true -> true,
/// false -> false, null -> false, fail-closed), a missing or invalid
/// bearer token is rejected with 401, and anonymous login keeps working
/// once JWT bearer authentication is registered. Each test builds its own factory with the real
/// <see cref="AppDbContext"/> SQL Server registration swapped for a
/// uniquely-named EF Core InMemory database, so no real SQL Server is
/// needed and tests never share state.
/// </summary>
public class AuthControllerTests
{
    private const string TestSigningKey = "unit-test-jwt-signing-key-at-least-32-bytes-long";
    private const string TestIssuer = "InventoryTrackingSystem.Tests";
    private const string KnownUsername = "known.user";
    private const string KnownPassword = "correct horse battery staple";

    private static WebApplicationFactory<Program> CreateFactory(string dbName)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            // Guarantee a known-good signing key regardless of which
            // appsettings.*.json files are present in the test's content
            // root (AC-6 needs to validate the signature against a key it
            // knows for certain).
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = TestSigningKey,
                    ["Jwt:Issuer"] = TestIssuer,
                });
            });

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

    private static async Task SeedKnownUserAsync(WebApplicationFactory<Program> factory, bool? yetkiId = null)
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

    /// <summary>
    /// Logs in as <see cref="KnownUsername"/> exactly as a real client would
    /// and returns the issued JWT, so the <c>/api/auth/me</c> tests never
    /// hand-craft a token.
    /// </summary>
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
    public async Task Login_ReturnsOkWithWellFormedJwt_ForCorrectCredentials()
    {
        await using var factory = CreateFactory(nameof(Login_ReturnsOkWithWellFormedJwt_ForCorrectCredentials));
        await SeedKnownUserAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = KnownUsername,
            password = KnownPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Equal(KnownUsername, body!.Username);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));

        // AC-6: the token is well-formed (3 dot-separated segments) and its
        // signature validates against the same signing key the test API
        // instance was configured with.
        AssertWellFormedSignedJwt(body.Token);
    }

    [Fact]
    public async Task Login_ReturnsInvalidLoginCredentials_ForWrongPassword()
    {
        await using var factory = CreateFactory(nameof(Login_ReturnsInvalidLoginCredentials_ForWrongPassword));
        await SeedKnownUserAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = KnownUsername,
            password = "the-wrong-password",
        });

        await AssertInvalidLoginCredentialsAsync(response);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", KnownPassword)]
    [InlineData(KnownUsername, "")]
    public async Task Login_ReturnsInvalidLoginCredentials_ForEmptyUsernameOrPassword(string username, string password)
    {
        await using var factory = CreateFactory(
            $"{nameof(Login_ReturnsInvalidLoginCredentials_ForEmptyUsernameOrPassword)}-{username}-{password}");
        await SeedKnownUserAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });

        // AC-3: the SAME rejection path as AC-2, not a distinct "required
        // field" server error.
        await AssertInvalidLoginCredentialsAsync(response);
    }

    [Fact]
    public async Task Login_ReturnsOkAnonymously_WithAuthenticationRegistered()
    {
        // AC-7: registering JWT bearer authentication (for the new [Authorize]
        // /api/auth/me endpoint) does not affect the anonymous Login endpoint.
        await using var factory = CreateFactory(nameof(Login_ReturnsOkAnonymously_WithAuthenticationRegistered));
        await SeedKnownUserAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = KnownUsername,
            password = KnownPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsIsAdminTrue_ForYetkiIdTrueUser()
    {
        // AC-3
        await using var factory = CreateFactory(nameof(Me_ReturnsIsAdminTrue_ForYetkiIdTrueUser));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal(KnownUsername, body!.Username);
        Assert.True(body.IsAdmin);
    }

    [Fact]
    public async Task Me_ReturnsIsAdminFalse_ForYetkiIdFalseUser()
    {
        // AC-4
        await using var factory = CreateFactory(nameof(Me_ReturnsIsAdminFalse_ForYetkiIdFalseUser));
        await SeedKnownUserAsync(factory, yetkiId: false);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal(KnownUsername, body!.Username);
        Assert.False(body.IsAdmin);
    }

    [Fact]
    public async Task Me_ReturnsIsAdminFalse_ForYetkiIdNullUser()
    {
        // AC-5: fail-closed when YetkiID is null.
        await using var factory = CreateFactory(nameof(Me_ReturnsIsAdminFalse_ForYetkiIdNullUser));
        await SeedKnownUserAsync(factory, yetkiId: null);
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal(KnownUsername, body!.Username);
        Assert.False(body.IsAdmin);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_ForMissingAuthorizationHeader()
    {
        // AC-6
        await using var factory = CreateFactory(nameof(Me_ReturnsUnauthorized_ForMissingAuthorizationHeader));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_ForInvalidToken()
    {
        // AC-6
        await using var factory = CreateFactory(nameof(Me_ReturnsUnauthorized_ForInvalidToken));
        await SeedKnownUserAsync(factory, yetkiId: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AssertInvalidLoginCredentialsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INVALID_LOGIN_CREDENTIALS", body!.Error);
        Assert.Equal("Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!", body.Message);
    }

    private static void AssertWellFormedSignedJwt(string token)
    {
        Assert.Equal(3, token.Split('.').Length);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
        };

        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);
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

    private class MeResponse
    {
        public string Username { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }
    }
}
