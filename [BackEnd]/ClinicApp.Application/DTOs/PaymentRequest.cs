using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Application.DTOs;

public class PaymentRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}
