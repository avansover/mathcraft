using System.Security.Cryptography;
using FluentValidation;
using Mathcraft.Server.Common;
using Mathcraft.Server.Data;
using Mathcraft.Server.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Mathcraft.Server.Features.Auth;

public record RequestPasswordResetCommand(string Email) : IRequest<Result<string>>;

public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}

public class RequestPasswordResetCommandHandler(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<RequestPasswordResetCommandHandler> logger) : IRequestHandler<RequestPasswordResetCommand, Result<string>>
{
    private const string Message = "If this email is registered, a reset link has been sent.";

    public async Task<Result<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await db.FamilyAccounts
            .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

        // Always return the same message — do not reveal if email exists
        if (account is null)
            return Result<string>.Ok(Message);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(
            HMACSHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Secret"] ?? ""),
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            FamilyAccountId = account.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync(cancellationToken);

        await SendResetEmailAsync(email, rawToken);

        return Result<string>.Ok(Message);
    }

    private async Task SendResetEmailAsync(string email, string rawToken)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("SendGrid API key not configured. Password reset email not sent for {Email}.", email);
            return;
        }

        var fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@mathcraft.app";
        var client = new SendGridClient(apiKey);
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(fromEmail, "Mathcraft"),
            new EmailAddress(email),
            "Reset your Mathcraft password",
            $"Use this token to reset your password: {rawToken}",
            $"<p>Use this token to reset your password: <strong>{rawToken}</strong></p>");

        var response = await client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
            logger.LogError("SendGrid failed to send reset email to {Email}. Status: {Status}", email, response.StatusCode);
    }
}
