using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Account;

public record VerifyPinCommand(Guid AccountId, string Pin) : IRequest<Result<bool>>;

public class VerifyPinCommandValidator : AbstractValidator<VerifyPinCommand>
{
    public VerifyPinCommandValidator()
    {
        RuleFor(x => x.Pin).NotEmpty().WithMessage("PIN is required.");
    }
}

public class VerifyPinCommandHandler(AppDbContext db, IPasswordHasher<FamilyAccount> passwordHasher)
    : IRequestHandler<VerifyPinCommand, Result<bool>>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

    public async Task<Result<bool>> Handle(VerifyPinCommand request, CancellationToken cancellationToken)
    {
        var account = await db.FamilyAccounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return Result<bool>.Fail("Account not found.", ErrorCode.NotFound);

        if (account.PinHash is null)
            return Result<bool>.Fail("No PIN is set on this account.", ErrorCode.Validation);

        if (account.PinLockedUntil.HasValue && account.PinLockedUntil > DateTime.UtcNow)
            return Result<bool>.Fail("Too many failed attempts. Try again in 30 seconds.", ErrorCode.RateLimited);

        var result = passwordHasher.VerifyHashedPassword(account, account.PinHash, request.Pin);

        if (result == PasswordVerificationResult.Failed)
        {
            account.PinFailedAttempts++;

            if (account.PinFailedAttempts >= MaxFailedAttempts)
                account.PinLockedUntil = DateTime.UtcNow.Add(LockoutDuration);

            await db.SaveChangesAsync(cancellationToken);
            return Result<bool>.Fail("Incorrect PIN.", ErrorCode.Unauthorized);
        }

        account.PinFailedAttempts = 0;
        account.PinLockedUntil = null;
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}
