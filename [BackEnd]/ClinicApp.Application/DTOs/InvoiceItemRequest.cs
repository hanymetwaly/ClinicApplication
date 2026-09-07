using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Application.DTOs;

public class InvoiceItemRequest
{
    [Required]
    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int Quantity { get; set; } = 1;

    [Range(0.01, 100000000)]
    public decimal UnitPrice { get; set; }
}
