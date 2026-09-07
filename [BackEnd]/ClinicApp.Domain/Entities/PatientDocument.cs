using ClinicApp.Domain.Common;

namespace ClinicApp.Domain.Entities;

public class PatientDocument : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
