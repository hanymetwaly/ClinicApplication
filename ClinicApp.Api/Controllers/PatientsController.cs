using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PatientsController(IClinicService clinicService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> Get(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "fullName",
        bool descending = false)
    {
        return Ok(await clinicService.GetPatientsAsync(search, page, pageSize, sortBy, descending));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        return Ok(await clinicService.GetPatientAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request)
    {
        var patient = await clinicService.CreatePatientAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, UpdatePatientRequest request)
    {
        return Ok(await clinicService.UpdatePatientAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await clinicService.DeletePatientAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<PatientDocumentDto>>> GetDocuments(Guid id)
    {
        return Ok(await clinicService.GetPatientDocumentsAsync(id));
    }

    [HttpPost("{id:guid}/documents")]
    [Authorize(Policy = "ReceptionistOrAdmin")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PatientDocumentDto>> UploadDocument(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { detail = "No file was uploaded." });
        }

        using var stream = file.OpenReadStream();
        var document = await clinicService.UploadPatientDocumentAsync(
            id,
            file.FileName,
            file.ContentType,
            file.Length,
            stream);
        return CreatedAtAction(nameof(GetDocuments), new { id }, document);
    }
}
