using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

/// <summary>
/// Manages invoices and records payments against outstanding balances.
/// </summary>
[ApiController]
[Authorize(Policy = "ReceptionistOrAdmin")]
[Route("api/[controller]")]
public class InvoicesController(IClinicService clinicService) : ControllerBase
{
    /// <summary>
    /// Searches and pages through invoices, optionally filtering by patient and status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> Get(
        Guid? patientId = null,
        InvoiceStatus? status = null,
        int page = 1,
        int? pageSize = null,
        string? sortBy = "invoiceDate",
        bool descending = true)
    {
        return Ok(await clinicService.GetInvoicesAsync(
            patientId,
            status,
            page,
            pageSize,
            sortBy,
            descending));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
    {
        return Ok(await clinicService.GetInvoiceAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request)
    {
        var invoice = await clinicService.CreateInvoiceAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> Pay(Guid id, PaymentRequest request)
    {
        return Ok(await clinicService.PayInvoiceAsync(id, request.Amount));
    }
}
