namespace Mathcraft.Server.Models;

public class RefreshToken : BaseEntity
{
    public Guid FamilyAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    public FamilyAccount FamilyAccount { get; set; } = null!;
}
