using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Application.DTOs;

public class CreateInvoiceRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Range(0, 100)]
    public decimal VatRate { get; set; } = 15m;

    [Range(0, 100)]
    public decimal DiscountRate { get; set; }

    [MinLength(1)]
    public List<InvoiceItemRequest> Items { get; set; } = new();
}
