using System.ComponentModel.DataAnnotations;
using AgencyFlow.Authorization;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.SubTaskWorkers)]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 50)] int pageSize = 20,
        [FromQuery] bool unreadOnly = false) =>
        Ok(await service.GetAllAsync(page, pageSize, unreadOnly));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await service.GetSummaryAsync());

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id) =>
        await service.MarkAsReadAsync(id) ? NoContent() : NotFound();

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await service.MarkAllAsReadAsync();
        return NoContent();
    }
}
