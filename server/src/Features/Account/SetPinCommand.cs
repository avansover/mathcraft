using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Account;

public record SetPinCommand(Guid AccountId, string Pin) : IRequest<Result<string>>;

public class SetPinCommandValidator : AbstractValidator<SetPinCommand>
{
    public SetPinCommandValidator()
    {
        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN is required.")
            .Matches(@"^\d{4,6}$").WithMessage("PIN must be 4–6 digits.");
    }
}

public class SetPinCommandHandler(AppDbContext db, IPasswordHasher<FamilyAccount> passwordHasher)
    : IRequestHandler<SetPinCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetPinCommand request, CancellationToken cancellationToken)
    {
        var account = await db.FamilyAccounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return Result<string>.Fail("Account not found.", ErrorCode.NotFound);

        account.PinHash = passwordHasher.HashPassword(account, request.Pin);
        account.PinFailedAttempts = 0;
        account.PinLockedUntil = null;
        account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result<string>.Ok("PIN set successfully.");
    }
}
