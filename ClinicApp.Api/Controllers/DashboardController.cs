using ClinicApp.Application.Interfaces;
using ClinicApp.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<DashboardSummary>> Get()
    {
        return Ok(await _service.GetDashboardAsync());
    }

    [HttpGet("charts")]
    public async Task<ActionResult<DashboardChartData>> GetCharts()
    {
        return Ok(await _service.GetDashboardChartDataAsync());
    }
}
