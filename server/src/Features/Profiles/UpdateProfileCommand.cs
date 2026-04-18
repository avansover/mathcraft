using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Profiles;

public record UpdateProfileCommand(Guid AccountId, Guid ProfileId, string? DisplayName, int? AvatarId, int? Age)
    : IRequest<Result<ProfileResponse>>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        When(x => x.DisplayName is not null, () =>
            RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name cannot be empty."));

        When(x => x.Age is not null, () =>
            RuleFor(x => x.Age).InclusiveBetween(4, 18).WithMessage("Age must be between 4 and 18."));

        When(x => x.AvatarId is not null, () =>
            RuleFor(x => x.AvatarId).GreaterThan(0).WithMessage("Invalid avatar."));
    }
}

public class UpdateProfileCommandHandler(AppDbContext db) : IRequestHandler<UpdateProfileCommand, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await db.PlayerProfiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile is null)
            return Result<ProfileResponse>.Fail("Profile not found.", ErrorCode.NotFound);

        if (profile.FamilyAccountId != request.AccountId)
            return Result<ProfileResponse>.Fail("Access denied.", ErrorCode.Forbidden);

        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName.Trim();
        if (request.AvatarId is not null) profile.AvatarId = request.AvatarId.Value;
        if (request.Age is not null) profile.Age = request.Age.Value;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result<ProfileResponse>.Ok(new ProfileResponse(profile.Id, profile.DisplayName, profile.AvatarId, profile.Age, profile.Gold));
    }
}
