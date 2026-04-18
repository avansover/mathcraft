using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;

namespace Mathcraft.Server.Features.Profiles;

public record CreateProfileCommand(Guid AccountId, string DisplayName, int AvatarId, int Age)
    : IRequest<Result<ProfileResponse>>;

public class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.");

        RuleFor(x => x.Age)
            .InclusiveBetween(4, 18).WithMessage("Age must be between 4 and 18.");

        RuleFor(x => x.AvatarId)
            .GreaterThan(0).WithMessage("Invalid avatar.");
    }
}

public class CreateProfileCommandHandler(AppDbContext db, IConfiguration configuration)
    : IRequestHandler<CreateProfileCommand, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        var maxProfiles = configuration.GetValue<int>("App:MaxProfilesPerAccount", 10);
        var count = db.PlayerProfiles.Count(p => p.FamilyAccountId == request.AccountId);

        if (count >= maxProfiles)
            return Result<ProfileResponse>.Fail($"Maximum of {maxProfiles} profiles allowed per account.", ErrorCode.Conflict);

        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            FamilyAccountId = request.AccountId,
            DisplayName = request.DisplayName.Trim(),
            AvatarId = request.AvatarId,
            Age = request.Age,
            Gold = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.PlayerProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return Result<ProfileResponse>.Ok(new ProfileResponse(profile.Id, profile.DisplayName, profile.AvatarId, profile.Age, profile.Gold));
    }
}
