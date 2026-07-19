using ClinicApp.Application.Interfaces;
using ClinicApp.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

/// <summary>
/// Exposes clinic dashboard metrics and chart data.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IClinicService _service;

    public DashboardController(IClinicService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns a summary of today's appointments, patients, revenue, and unpaid invoices.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardSummary>> Get()
    {
        return Ok(await _service.GetDashboardAsync());
    }

    /// <summary>
    /// Returns chart data for appointments and revenue over time.
    /// </summary>
    [HttpGet("charts")]
    public async Task<ActionResult<DashboardChartData>> GetCharts()
    {
        return Ok(await _service.GetDashboardChartDataAsync());
    }
}
