using FluentAssertions;
using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Profiles;
using Mathcraft.Server.Models;
using Microsoft.Extensions.Configuration;

namespace Mathcraft.Tests.Unit.Profiles;

public class CreateProfileCommandTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    private static IConfiguration BuildConfig(int maxProfiles = 10)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:MaxProfilesPerAccount"] = maxProfiles.ToString()
            })
            .Build();
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesProfile()
    {
        using var db = TestDbFactory.Create();
        var handler = new CreateProfileCommandHandler(db, BuildConfig());

        var result = await handler.Handle(new CreateProfileCommand(AccountId, "Noa", 1, 9), default);

        result.Success.Should().BeTrue();
        result.Data!.DisplayName.Should().Be("Noa");
        result.Data.Gold.Should().Be(0);
        db.PlayerProfiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task Validator_AgeTooLow_ReturnsValidationError()
    {
        var validator = new CreateProfileCommandValidator();
        var result = await validator.ValidateAsync(new CreateProfileCommand(AccountId, "Noa", 1, 3));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    [Fact]
    public async Task Validator_AgeTooHigh_ReturnsValidationError()
    {
        var validator = new CreateProfileCommandValidator();
        var result = await validator.ValidateAsync(new CreateProfileCommand(AccountId, "Noa", 1, 19));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    [Fact]
    public async Task Handle_AtProfileLimit_ReturnsConflict()
    {
        using var db = TestDbFactory.Create();
        for (var i = 0; i < 2; i++)
        {
            db.PlayerProfiles.Add(new PlayerProfile
            {
                Id = Guid.NewGuid(), FamilyAccountId = AccountId,
                DisplayName = $"Kid {i}", AvatarId = 1, Age = 8,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var handler = new CreateProfileCommandHandler(db, BuildConfig(maxProfiles: 2));
        var result = await handler.Handle(new CreateProfileCommand(AccountId, "One Too Many", 1, 8), default);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
    }
}
