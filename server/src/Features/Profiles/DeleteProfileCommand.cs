using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Profiles;

public record DeleteProfileCommand(Guid AccountId, Guid ProfileId) : IRequest<Result<bool>>;

public class DeleteProfileCommandHandler(AppDbContext db) : IRequestHandler<DeleteProfileCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await db.PlayerProfiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile is null)
            return Result<bool>.Fail("Profile not found.", ErrorCode.NotFound);

        if (profile.FamilyAccountId != request.AccountId)
            return Result<bool>.Fail("Access denied.", ErrorCode.Forbidden);

        db.PlayerProfiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}
