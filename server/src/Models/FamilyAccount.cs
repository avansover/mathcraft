namespace Mathcraft.Server.Models;

public class FamilyAccount : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PinHash { get; set; }
    public int PinFailedAttempts { get; set; } = 0;
    public DateTime? PinLockedUntil { get; set; }

    public ICollection<PlayerProfile> PlayerProfiles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
