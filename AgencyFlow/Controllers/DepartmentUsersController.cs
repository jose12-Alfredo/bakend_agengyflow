using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.DTOs.DepartmentUser;
using AgencyFlow.Services;
using AgencyFlow.Authorization;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.ProjectManagers)]
[ApiController]
[Route("api/department-users")]
public class DepartmentUsersController(
    DepartmentUserService departmentUserService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isDirector = null)
    {
        var result = await departmentUserService.GetAllAsync(
            page, pageSize, userId, departmentId, isDirector);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result =
            await departmentUserService.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrators)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentUserDto dto)
    {
        var (result, conflict) =
            await departmentUserService.CreateAsync(dto);

        if (conflict)
        {
            return Conflict(new
            {
                message =
                    "El usuario ya pertenece a ese departamento."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result!.Id },
            result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Administrators)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDepartmentUserDto dto)
    {
        var result =
            await departmentUserService.UpdateAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Administrators)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted =
            await departmentUserService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
