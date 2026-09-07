using AgencyFlow.DTOs.Productivity;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgencyFlow.Authorization;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.SubTaskWorkers)]
[ApiController]
[Route("api/productivity")]
public class ProductivityController(ProductivityService productivityService)
    : ControllerBase
{
    [HttpGet("delays")]
    public async Task<IActionResult> GetDelays(
        [FromQuery] DelayReportFilterDto filter)
    {
        return Ok(await productivityService.GetDelayReportAsync(filter));
    }

    [HttpGet("cycle-times")]
    public async Task<IActionResult> GetCycleTimes([FromQuery] DelayReportFilterDto filter)
    {
        return Ok(await productivityService.GetDelayReportAsync(filter));
    }
}
