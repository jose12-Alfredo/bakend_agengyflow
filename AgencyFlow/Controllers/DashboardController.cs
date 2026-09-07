using AgencyFlow.Services;
using AgencyFlow.DTOs.Dashboard;
using AgencyFlow.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.Authorization;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.ProjectManagers)]
[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    DashboardService dashboardService,
    ManagementDashboardService managementDashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DashboardFilterDto filter)
    {
        return Ok(await dashboardService.GetAsync(filter));
    }

    [HttpGet("clients/{clientId:guid}/hierarchy")]
    public async Task<IActionResult> GetClientHierarchy(
        Guid clientId,
        [FromQuery] ClientHierarchyFilterDto filter)
    {
        if (filter.Year is < 2000 or > 9999 || filter.Month is < 1 or > 12)
            throw new BusinessValidationException(
                "year y month son obligatorios; month debe estar entre 1 y 12.");

        var result = await managementDashboardService.GetClientHierarchyAsync(
            clientId, filter);

        if (result == null)
            return NotFound(new { message = "El cliente indicado no existe o no está disponible." });

        return Ok(result);
    }
}
