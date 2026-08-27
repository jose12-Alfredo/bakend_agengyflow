using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.SubTask;
using AgencyFlow.DTOs.TaskItem;
using AgencyFlow.Services;

namespace AgencyFlow.Controllers;

[Authorize]
[ApiController]
[Route("api/subtasks")]
public class SubTasksController : ControllerBase
{
    private readonly SubTaskService _subTaskService;

    public SubTasksController(SubTaskService subTaskService)
    {
        _subTaskService = subTaskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? titulo = null,
        [FromQuery] Guid? taskItemId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? assignedUserId = null)
    {
        var result = await _subTaskService.GetAllAsync(
            page, pageSize, titulo, taskItemId, status, assignedUserId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _subTaskService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubTaskDto dto)
    {
        var result = await _subTaskService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubTaskDto dto)
    {
        var result = await _subTaskService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _subTaskService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}/assignee")]
    public async Task<IActionResult> UpdateAssignee(
        Guid id,
        [FromBody] UpdateTaskAssigneeDto dto)
    {
        var result = await _subTaskService.UpdateAssigneeAsync(id, dto.AssignedUserId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
