using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Account;

public record DeletePinCommand(Guid AccountId) : IRequest<Result<bool>>;

public class DeletePinCommandHandler(AppDbContext db) : IRequestHandler<DeletePinCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeletePinCommand request, CancellationToken cancellationToken)
    {
        var account = await db.FamilyAccounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return Result<bool>.Fail("Account not found.", ErrorCode.NotFound);

        account.PinHash = null;
        account.PinFailedAttempts = 0;
        account.PinLockedUntil = null;
        account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}
