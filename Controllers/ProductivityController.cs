using AgencyFlow.DTOs.Productivity;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyFlow.Controllers;

[Authorize]
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
}
