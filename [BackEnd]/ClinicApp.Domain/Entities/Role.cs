using ClinicApp.Domain.Common;

namespace ClinicApp.Domain.Entities;

public class Role : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<User> Users { get; set; } = new();
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";
}
