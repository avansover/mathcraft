using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Auth;
using Mathcraft.Server.Models;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Mathcraft.Tests.Unit.Auth;

public class LoginCommandTests
{
    private readonly IPasswordHasher<FamilyAccount> _hasher = Substitute.For<IPasswordHasher<FamilyAccount>>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    private static FamilyAccount MakeAccount() => new()
    {
        Id = Guid.NewGuid(), Email = "test@example.com",
        PasswordHash = "hashed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAccessToken()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        _hasher.VerifyHashedPassword(account, "hashed", "password123")
            .Returns(PasswordVerificationResult.Success);
        _jwt.GenerateAccessToken(Arg.Any<FamilyAccount>()).Returns("jwt-token");
        _refreshTokenService.GenerateToken().Returns("raw-token");
        _refreshTokenService.HashToken("raw-token").Returns("hashed-token");

        var handler = new LoginCommandHandler(db, _hasher, _jwt, _refreshTokenService);
        var result = await handler.Handle(new LoginCommand("test@example.com", "password123"), default);

        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        _hasher.VerifyHashedPassword(Arg.Any<FamilyAccount>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Failed);

        var handler = new LoginCommandHandler(db, _hasher, _jwt, _refreshTokenService);
        var result = await handler.Handle(new LoginCommand("test@example.com", "wrongpassword"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var handler = new LoginCommandHandler(db, _hasher, _jwt, _refreshTokenService);

        var result = await handler.Handle(new LoginCommand("nobody@example.com", "password123"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Validator_MissingFields_ReturnsValidationErrors()
    {
        var validator = new LoginCommandValidator();
        var result = await validator.ValidateAsync(new LoginCommand("", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
