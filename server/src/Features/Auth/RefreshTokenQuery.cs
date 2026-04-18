using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Auth;

public record RefreshTokenQuery(string? TokenCookie) : IRequest<Result<RefreshTokenResponse>>;

public record RefreshTokenResponse(string AccessToken, Guid AccountId, string NewRefreshToken);

public class RefreshTokenQueryHandler(
    AppDbContext db,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : IRequestHandler<RefreshTokenQuery, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenCookie))
            return Result<RefreshTokenResponse>.Fail("Refresh token is missing.", ErrorCode.Unauthorized);

        var tokenHash = refreshTokenService.HashToken(request.TokenCookie);

        var stored = await db.RefreshTokens
            .Include(t => t.FamilyAccount)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Result<RefreshTokenResponse>.Fail("Refresh token is invalid or expired.", ErrorCode.Unauthorized);

        // Rotate: revoke old, issue new
        stored.IsRevoked = true;

        var rawToken = refreshTokenService.GenerateToken();
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            FamilyAccountId = stored.FamilyAccountId,
            TokenHash = refreshTokenService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(stored.FamilyAccount);

        return Result<RefreshTokenResponse>.Ok(new RefreshTokenResponse(accessToken, stored.FamilyAccountId, rawToken));
    }
}
