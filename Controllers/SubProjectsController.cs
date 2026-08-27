using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.SubProject;
using AgencyFlow.Services;

namespace AgencyFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/subprojects")]
public class SubProjectsController : ControllerBase
{
    private readonly SubProjectService _subProjectService;

    public SubProjectsController(SubProjectService subProjectService)
    {
        _subProjectService = subProjectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? titulo = null,
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? assignedUserId = null)
    {
        var result = await _subProjectService.GetAllAsync(
            page, pageSize, titulo, projectId, departmentId, status, assignedUserId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _subProjectService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubProjectDto dto)
    {
        var result = await _subProjectService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubProjectDto dto)
    {
        var result = await _subProjectService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _subProjectService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
