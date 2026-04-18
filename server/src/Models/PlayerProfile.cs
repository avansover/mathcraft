namespace Mathcraft.Server.Models;

public class PlayerProfile : AuditableEntity
{
    public Guid FamilyAccountId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int AvatarId { get; set; }
    public int Age { get; set; }
    public int Gold { get; set; } = 0;

    public FamilyAccount FamilyAccount { get; set; } = null!;
}
