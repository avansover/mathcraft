using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mathcraft.Server.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mathcraft.Tests.Integration.Auth;

public class RegistrationFlowTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private Guid _accountId;

    public RegistrationFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up test data
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
    public async Task Register_CreateProfile_ThenGetProfiles()
    {
        // Step 1: Register
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"integration-test-{Guid.NewGuid()}@example.com",
            password = "TestPassword123"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerData = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();
        _accountId = registerData!.AccountId;

        // Step 2: Login to get access token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = registerData.Email,
            password = "TestPassword123"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        // Step 3: Create a profile
        var createResponse = await _client.PostAsJsonAsync("/api/profiles", new
        {
            displayName = "Noa",
            avatarId = 1,
            age = 9
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 4: Get profiles — should return the one we just created
        var getResponse = await _client.GetAsync("/api/profiles");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profiles = await getResponse.Content.ReadFromJsonAsync<List<ProfileResult>>();
        profiles.Should().HaveCount(1);
        profiles![0].DisplayName.Should().Be("Noa");
    }

    private record RegisterResult(Guid AccountId, string Email);
    private record LoginResult(string AccessToken, Guid AccountId);
    private record ProfileResult(Guid Id, string DisplayName, int AvatarId, int Age, int Gold);
}
