using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Profiles;

public record GetProfilesQuery(Guid AccountId) : IRequest<Result<List<ProfileResponse>>>;

public record ProfileResponse(Guid Id, string DisplayName, int AvatarId, int Age, int Gold);

public class GetProfilesQueryHandler(AppDbContext db) : IRequestHandler<GetProfilesQuery, Result<List<ProfileResponse>>>
{
    public async Task<Result<List<ProfileResponse>>> Handle(GetProfilesQuery request, CancellationToken cancellationToken)
    {
        var profiles = await db.PlayerProfiles
            .Where(p => p.FamilyAccountId == request.AccountId)
            .Select(p => new ProfileResponse(p.Id, p.DisplayName, p.AvatarId, p.Age, p.Gold))
            .ToListAsync(cancellationToken);

        return Result<List<ProfileResponse>>.Ok(profiles);
    }
}
