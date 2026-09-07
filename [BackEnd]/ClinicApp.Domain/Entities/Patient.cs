using ClinicApp.Domain.Common;

namespace ClinicApp.Domain.Entities;

public class Patient : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string MedicalHistory { get; set; } = string.Empty;
    public string InsuranceInfo { get; set; } = string.Empty;
    public List<PatientDocument> Documents { get; set; } = new();
}
