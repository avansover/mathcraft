using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public record LoginResponse(string AccessToken, Guid AccountId, string RefreshToken);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginCommandHandler(
    AppDbContext db,
    IPasswordHasher<FamilyAccount> passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.FamilyAccounts
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        if (account is null)
            return Result<LoginResponse>.Fail("Invalid credentials.", ErrorCode.Unauthorized);

        var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Result<LoginResponse>.Fail("Invalid credentials.", ErrorCode.Unauthorized);

        // Revoke all existing refresh tokens for this account
        var existingTokens = await db.RefreshTokens
            .Where(t => t.FamilyAccountId == account.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in existingTokens)
            token.IsRevoked = true;

        var rawToken = refreshTokenService.GenerateToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            FamilyAccountId = account.Id,
            TokenHash = refreshTokenService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenService.GenerateAccessToken(account);

        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, account.Id, rawToken));
    }
}
