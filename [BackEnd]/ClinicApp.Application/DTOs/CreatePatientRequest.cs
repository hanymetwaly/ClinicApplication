using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Application.DTOs;

public class CreatePatientRequest
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    [StringLength(10000)]
    public string MedicalHistory { get; set; } = string.Empty;

    [StringLength(500)]
    public string InsuranceInfo { get; set; } = string.Empty;
}
