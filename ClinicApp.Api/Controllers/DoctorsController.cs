using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

/// <summary>
/// Lists active doctors and supports lightweight lookup for booking.
/// </summary>
[ApiController]
[Authorize(Policy = "ClinicStaff")]
[Route("api/[controller]")]
public class DoctorsController(IClinicService clinicService) : ControllerBase
{
    /// <summary>
    /// Returns all active doctors, or a paged list when a page number is provided.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> Get(
        int? page = null,
        int? pageSize = null,
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

    /// <summary>
    /// Performs a lightweight lookup of active doctors by name or specialty.
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> Lookup(string? query = null, int? limit = null)
    {
        var results = await clinicService.LookupDoctorsAsync(query, limit);
        return Ok(results);
    }
}
