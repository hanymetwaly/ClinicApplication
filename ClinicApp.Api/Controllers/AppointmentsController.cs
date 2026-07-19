using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Common;
using ClinicApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

/// <summary>
/// Manages appointments: booking, cancellation, rescheduling, and paged listing.
/// </summary>
[ApiController]
[Authorize(Policy = "ClinicStaff")]
[Route("api/[controller]")]
public class AppointmentsController(IClinicService clinicService) : ControllerBase
{
    /// <summary>
    /// Searches and pages through appointments, optionally filtering by date range, doctor, and status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> Get(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? doctorId = null,
        AppointmentStatus? status = null,
        int page = 1,
        int? pageSize = null,
        string? sortBy = "startTime",
        bool descending = false)
    {
        return Ok(await clinicService.GetAppointmentsAsync(
            startDate,
            endDate,
            doctorId,
            status,
            page,
            pageSize,
            sortBy,
            descending));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id)
    {
        return Ok(await clinicService.GetAppointmentAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<ActionResult<AppointmentDto>> Book(CreateAppointmentRequest request)
    {
        var appointment = await clinicService.BookAppointmentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id)
    {
        return Ok(await clinicService.CancelAppointmentAsync(id));
    }

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        Guid id,
        RescheduleAppointmentRequest request)
    {
        return Ok(await clinicService.RescheduleAppointmentAsync(id, request));
    }
}
