namespace Mathcraft.Server.Models;

public class PasswordResetToken : BaseEntity
{
    public Guid FamilyAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public FamilyAccount FamilyAccount { get; set; } = null!;
}
