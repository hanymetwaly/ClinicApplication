using ClinicApp.Domain.Common;

namespace ClinicApp.Domain.Entities;

public class Appointment : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ReminderSentAt { get; set; }
}
