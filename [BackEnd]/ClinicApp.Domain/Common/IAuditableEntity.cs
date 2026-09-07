namespace ClinicApp.Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
