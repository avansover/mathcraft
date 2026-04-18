using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Auth;
using Mathcraft.Server.Models;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Mathcraft.Tests.Unit.Auth;

public class RegisterCommandTests
{
    private readonly IPasswordHasher<FamilyAccount> _hasher = Substitute.For<IPasswordHasher<FamilyAccount>>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();

    public RegisterCommandTests()
    {
        _hasher.HashPassword(Arg.Any<FamilyAccount>(), Arg.Any<string>()).Returns("hashed");
        _refreshTokenService.GenerateToken().Returns("raw-token");
        _refreshTokenService.HashToken("raw-token").Returns("hashed-token");
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesAccountAndReturns201Data()
    {
        using var db = TestDbFactory.Create();
        var handler = new RegisterCommandHandler(db, _hasher, _refreshTokenService);

        var result = await handler.Handle(new RegisterCommand("Test@Example.com", "password123"), default);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("test@example.com");
        db.FamilyAccounts.Should().HaveCount(1);
        db.RefreshTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflict()
    {
        using var db = TestDbFactory.Create();
        db.FamilyAccounts.Add(new FamilyAccount
        {
            Id = Guid.NewGuid(), Email = "test@example.com",
            PasswordHash = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(db, _hasher, _refreshTokenService);
        var result = await handler.Handle(new RegisterCommand("test@example.com", "password123"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
    }

    [Fact]
    public async Task Validator_WeakPassword_ReturnsValidationError()
    {
        var validator = new RegisterCommandValidator();
        var result = await validator.ValidateAsync(new RegisterCommand("test@example.com", "short"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validator_InvalidEmail_ReturnsValidationError()
    {
        var validator = new RegisterCommandValidator();
        var result = await validator.ValidateAsync(new RegisterCommand("not-an-email", "password123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
