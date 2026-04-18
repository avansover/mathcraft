using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Profiles;
using Mathcraft.Server.Models;

namespace Mathcraft.Tests.Unit.Profiles;

public class DeleteProfileCommandTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid OtherAccountId = Guid.NewGuid();

    private static PlayerProfile MakeProfile(Guid accountId) => new()
    {
        Id = Guid.NewGuid(), FamilyAccountId = accountId,
        DisplayName = "Noa", AvatarId = 1, Age = 8,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ValidDelete_RemovesProfile()
    {
        using var db = TestDbFactory.Create();
        var profile = MakeProfile(AccountId);
        db.PlayerProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new DeleteProfileCommandHandler(db);
        var result = await handler.Handle(new DeleteProfileCommand(AccountId, profile.Id), default);

        result.Success.Should().BeTrue();
        db.PlayerProfiles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WrongAccount_ReturnsForbidden()
    {
        using var db = TestDbFactory.Create();
        var profile = MakeProfile(AccountId);
        db.PlayerProfiles.Add(profile);
        await db.SaveChangesAsync();

        var handler = new DeleteProfileCommandHandler(db);
        var result = await handler.Handle(new DeleteProfileCommand(OtherAccountId, profile.Id), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        using var db = TestDbFactory.Create();
        var handler = new DeleteProfileCommandHandler(db);

        var result = await handler.Handle(new DeleteProfileCommand(AccountId, Guid.NewGuid()), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }
}
