using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mathcraft.Server.Features.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result<string>>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}

public class ResetPasswordCommandHandler(
    AppDbContext db,
    IConfiguration configuration,
    IPasswordHasher<FamilyAccount> passwordHasher) : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.HMACSHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? ""),
                System.Text.Encoding.UTF8.GetBytes(request.Token)));

        var resetToken = await db.PasswordResetTokens
            .Include(t => t.FamilyAccount)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            return Result<string>.Fail("Token is invalid or expired.", ErrorCode.Validation);

        resetToken.IsUsed = true;

        var account = resetToken.FamilyAccount;
        account.PasswordHash = passwordHasher.HashPassword(account, request.NewPassword);
        account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result<string>.Ok("Password updated successfully.");
    }
}
