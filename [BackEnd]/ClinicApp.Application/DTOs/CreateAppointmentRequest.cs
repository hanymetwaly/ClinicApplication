using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Application.DTOs;

public class CreateAppointmentRequest : IValidatableObject
{
    private static readonly TimeSpan MaxAppointmentDuration = TimeSpan.FromHours(2);

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime == default || EndTime == default)
        {
            yield return new ValidationResult(
                "Start time and end time are required.",
                [nameof(StartTime), nameof(EndTime)]);
        }
        else if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "End time must be later than start time.",
                [nameof(EndTime)]);
        }
        else if (EndTime - StartTime > MaxAppointmentDuration)
        {
            yield return new ValidationResult(
                $"Appointment duration cannot exceed {MaxAppointmentDuration.TotalHours} hours.",
                [nameof(EndTime)]);
        }
    }
}
