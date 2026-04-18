using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Auth;

public record LogoutCommand(string? RefreshTokenCookie) : IRequest<Result<bool>>;

public class LogoutCommandHandler(
    AppDbContext db,
    IRefreshTokenService refreshTokenService) : IRequestHandler<LogoutCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshTokenCookie))
        {
            var tokenHash = refreshTokenService.HashToken(request.RefreshTokenCookie);

            var token = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsRevoked, cancellationToken);

            if (token is not null)
            {
                token.IsRevoked = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Result<bool>.Ok(true);
    }
}
