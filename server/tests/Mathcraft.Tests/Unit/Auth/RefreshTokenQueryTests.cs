using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Auth;
using Mathcraft.Server.Models;
using NSubstitute;

namespace Mathcraft.Tests.Unit.Auth;

public class RefreshTokenQueryTests
{
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    private static FamilyAccount MakeAccount() => new()
    {
        Id = Guid.NewGuid(), Email = "test@example.com",
        PasswordHash = "hashed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAccessToken()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);

        _refreshTokenService.HashToken("raw-token").Returns("hashed-token");
        _jwt.GenerateAccessToken(Arg.Any<FamilyAccount>()).Returns("new-jwt");
        _refreshTokenService.GenerateToken().Returns("new-raw-token");
        _refreshTokenService.HashToken("new-raw-token").Returns("new-hashed-token");

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), FamilyAccountId = account.Id,
            TokenHash = "hashed-token", ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RefreshTokenQueryHandler(db, _jwt, _refreshTokenService);
        var result = await handler.Handle(new RefreshTokenQuery("raw-token"), default);

        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("new-jwt");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);

        _refreshTokenService.HashToken("raw-token").Returns("hashed-token");
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), FamilyAccountId = account.Id,
            TokenHash = "hashed-token", ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RefreshTokenQueryHandler(db, _jwt, _refreshTokenService);
        var result = await handler.Handle(new RefreshTokenQuery("raw-token"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);

        _refreshTokenService.HashToken("raw-token").Returns("hashed-token");
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), FamilyAccountId = account.Id,
            TokenHash = "hashed-token", ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = true, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RefreshTokenQueryHandler(db, _jwt, _refreshTokenService);
        var result = await handler.Handle(new RefreshTokenQuery("raw-token"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_MissingCookie_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var handler = new RefreshTokenQueryHandler(db, _jwt, _refreshTokenService);

        var result = await handler.Handle(new RefreshTokenQuery(null), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }
}
