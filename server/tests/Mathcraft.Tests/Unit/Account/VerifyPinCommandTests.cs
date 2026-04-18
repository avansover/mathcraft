using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Account;
using Mathcraft.Server.Models;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Mathcraft.Tests.Unit.Account;

public class VerifyPinCommandTests
{
    private readonly IPasswordHasher<FamilyAccount> _hasher = Substitute.For<IPasswordHasher<FamilyAccount>>();

    private static FamilyAccount MakeAccount() => new()
    {
        Id = Guid.NewGuid(), Email = "test@example.com",
        PasswordHash = "hashed", PinHash = "pin-hashed",
        PinFailedAttempts = 0,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_CorrectPin_ReturnsTrue()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        _hasher.VerifyHashedPassword(Arg.Any<FamilyAccount>(), "pin-hashed", "1234")
            .Returns(PasswordVerificationResult.Success);

        var handler = new VerifyPinCommandHandler(db, _hasher);
        var result = await handler.Handle(new VerifyPinCommand(account.Id, "1234"), default);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongPin_ReturnsUnauthorized()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        _hasher.VerifyHashedPassword(Arg.Any<FamilyAccount>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Failed);

        var handler = new VerifyPinCommandHandler(db, _hasher);
        var result = await handler.Handle(new VerifyPinCommand(account.Id, "9999"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_FiveFailedAttempts_LocksAccount()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        _hasher.VerifyHashedPassword(Arg.Any<FamilyAccount>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PasswordVerificationResult.Failed);

        var handler = new VerifyPinCommandHandler(db, _hasher);
        for (var i = 0; i < 5; i++)
            await handler.Handle(new VerifyPinCommand(account.Id, "9999"), default);

        var lockedAccount = db.FamilyAccounts.Find(account.Id)!;
        lockedAccount.PinLockedUntil.Should().NotBeNull();
        lockedAccount.PinLockedUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_LockedAccount_ReturnsRateLimited()
    {
        using var db = TestDbFactory.Create();
        var account = MakeAccount();
        account.PinLockedUntil = DateTime.UtcNow.AddSeconds(30);
        db.FamilyAccounts.Add(account);
        await db.SaveChangesAsync();

        var handler = new VerifyPinCommandHandler(db, _hasher);
        var result = await handler.Handle(new VerifyPinCommand(account.Id, "1234"), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.RateLimited);
    }
}
