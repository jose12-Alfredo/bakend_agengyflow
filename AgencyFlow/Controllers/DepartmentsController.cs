using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.Department;
using AgencyFlow.Services;
using AgencyFlow.Authorization;


namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.Administrators)]
[ApiController]
[Route("api/departments")]
public class DepartmentsController(DepartmentService departmentService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] string? nombre = null)
    {
        var result = await departmentService.GetAllAsync(
            page,
            pageSize,
            nombre);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await departmentService.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentDto dto)
    {
        var result = await departmentService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDepartmentDto dto)
    {
        var result = await departmentService.UpdateAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await departmentService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
