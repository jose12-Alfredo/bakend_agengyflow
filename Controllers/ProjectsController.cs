using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.Project;
using AgencyFlow.Services;

namespace AgencyFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projectService;

    public ProjectsController(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? titulo = null,
        [FromQuery] Guid? projectTypeId = null,
        [FromQuery] Guid? clientUserId = null,
        [FromQuery] DateOnly? startDateFrom = null,
        [FromQuery] DateOnly? startDateTo = null)
    {
        var result = await _projectService.GetAllAsync(
            page,
            pageSize,
            titulo,
            projectTypeId,
            clientUserId,
            startDateFrom,
            startDateTo);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _projectService.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetFull(Guid id)
    {
        var result = await _projectService.GetFullAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectDto dto)
    {
        var result = await _projectService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectDto dto)
    {
        var result = await _projectService.UpdateAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _projectService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
