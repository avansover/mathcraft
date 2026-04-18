using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Mathcraft.Server.Features.Auth;

public record RegisterCommand(string Email, string Password) : IRequest<Result<RegisterResponse>>;

public record RegisterResponse(Guid AccountId, string Email, string RefreshToken);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}

public class RegisterCommandHandler(
    AppDbContext db,
    IPasswordHasher<FamilyAccount> passwordHasher,
    IRefreshTokenService refreshTokenService) : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = db.FamilyAccounts.Any(a => a.Email == email);
        if (exists)
            return Result<RegisterResponse>.Fail("Email is already registered.", ErrorCode.Conflict);

        var account = new FamilyAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        var rawToken = refreshTokenService.GenerateToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            FamilyAccountId = account.Id,
            TokenHash = refreshTokenService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        db.FamilyAccounts.Add(account);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse>.Ok(new RegisterResponse(account.Id, account.Email, rawToken));
    }
}
