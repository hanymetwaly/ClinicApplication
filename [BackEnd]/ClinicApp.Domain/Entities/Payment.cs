using ClinicApp.Domain.Common;

namespace ClinicApp.Domain.Entities;

public class Payment : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
