using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AgencyFlow.Authorization;
using AgencyFlow.DTOs.ProjectDirectorAccess;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.Administrators)]
[ApiController]
[Route("api/project-director-accesses")]
public sealed class ProjectDirectorAccessesController(ProjectDirectorAccessService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10, [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? directorUserId = null) => Ok(await service.GetAllAsync(page, pageSize, projectId, directorUserId));

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectDirectorAccessDto dto)
    {
        var (result, conflict) = await service.CreateAsync(dto, CurrentUserId());
        if (conflict) return Conflict(new { message = "El director ya tiene acceso activo al proyecto." });
        return CreatedAtAction(nameof(GetAll), new { projectId = result!.ProjectId }, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id) => await service.RevokeAsync(id, CurrentUserId())
        ? NoContent() : NotFound(new { message = "El acceso indicado no existe." });

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
