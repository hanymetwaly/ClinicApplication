using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DoctorsController(IClinicService clinicService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(
        int? page = null,
        int pageSize = 10,
        string? sortBy = "fullName",
        bool descending = false)
    {
        if (page.HasValue)
        {
            var paged = await clinicService.GetDoctorsAsync(page.Value, pageSize, sortBy, descending);
            return Ok(paged);
        }

        return Ok(await clinicService.GetDoctorsAsync());
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> Lookup(string? query = null, int limit = 20)
    {
        var results = await clinicService.LookupDoctorsAsync(query, limit);
        return Ok(results);
    }
}
