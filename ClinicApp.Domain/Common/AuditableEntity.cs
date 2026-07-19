namespace ClinicApp.Domain.Common;

public abstract class AuditableEntity : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
