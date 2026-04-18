using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mathcraft.Tests.Integration.Auth;

// Custom factory that swaps IRefreshTokenService for a non-secure-cookie version
// (test clients use HTTP, so Secure=true cookies are never sent back)
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddScoped<IRefreshTokenService, TestRefreshTokenService>();
        });
    }
}

public class TestRefreshTokenService(IConfiguration configuration) : IRefreshTokenService
{
    private const string CookieName = "refreshToken";

    public string GenerateToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public void SetCookie(Microsoft.AspNetCore.Http.HttpResponse response, string token)
    {
        var expiryDays = configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        response.Cookies.Append(CookieName, token, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = false,   // HTTP-safe for tests
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(expiryDays)
        });
    }

    public void ClearCookie(Microsoft.AspNetCore.Http.HttpResponse response)
    {
        response.Cookies.Delete(CookieName);
    }
}

public class RefreshTokenTests : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;
    private Guid _accountId;

    public RefreshTokenTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.FamilyAccounts.FindAsync(_accountId);
        if (account is not null)
        {
            db.FamilyAccounts.Remove(account);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRevokesOldOne()
    {
        var email = $"refresh-test-{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "TestPassword123"
        });
        var registerData = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();
        _accountId = registerData!.AccountId;

        // First refresh — uses the cookie set during register
        var firstRefresh = await _client.GetAsync("/api/auth/refresh");
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstData = await firstRefresh.Content.ReadFromJsonAsync<RefreshResult>();
        firstData!.AccessToken.Should().NotBeNullOrEmpty();

        // Second refresh — cookie was rotated, should still work
        var secondRefresh = await _client.GetAsync("/api/auth/refresh");
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify old token is revoked in the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var revokedCount = db.RefreshTokens
            .Count(t => t.FamilyAccountId == _accountId && t.IsRevoked);

        revokedCount.Should().BeGreaterThan(0);
    }

    private record RegisterResult(Guid AccountId, string Email);
    private record RefreshResult(string AccessToken, Guid AccountId);
}
