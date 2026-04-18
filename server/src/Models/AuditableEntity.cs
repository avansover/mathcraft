namespace Mathcraft.Server.Models;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime UpdatedAt { get; set; }
}
