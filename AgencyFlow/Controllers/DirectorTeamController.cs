using AgencyFlow.Authorization;
using AgencyFlow.DTOs.User;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.Director)]
[ApiController]
[Route("api/director/team-members")]
public sealed class DirectorTeamController(DirectorTeamService service) : ControllerBase
{
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllowedRoles() => Ok(await service.GetAllowedRolesAsync());

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] Guid departmentId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await service.GetCandidatesAsync(departmentId, search, page, pageSize));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTeamMemberDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Created($"/api/users/{result.Id}", result);
    }

    [HttpPost("existing")]
    public async Task<IActionResult> AddExisting(AddExistingTeamMemberDto dto)
    {
        var (result, conflict) = await service.AddExistingAsync(dto);
        if (conflict)
            return Conflict(new { message = "El usuario ya pertenece a este departamento." });

        return Created($"/api/department-users/{result!.Id}", result);
    }
}
