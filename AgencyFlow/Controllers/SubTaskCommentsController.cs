using System.Security.Claims;
using AgencyFlow.DTOs.SubTaskComment;
using AgencyFlow.Services;
using AgencyFlow.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyFlow.Controllers;

[Authorize(Roles = RoleNames.SubTaskWorkers)]
[ApiController]
[Route("api/subtasks/{subTaskId:guid}/comments")]
public class SubTaskCommentsController : ControllerBase
{
    private readonly SubTaskCommentService _commentService;

    public SubTaskCommentsController(SubTaskCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid subTaskId)
    {
        var result = await _commentService.GetAllAsync(subTaskId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid subTaskId,
        [FromBody] CreateSubTaskCommentDto dto)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _commentService.CreateAsync(
            subTaskId, userId, dto);

        return CreatedAtAction(
            nameof(GetAll),
            new { subTaskId },
            result);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid subTaskId,
        Guid commentId)
    {
        var userId = GetAuthenticatedUserId();
        var deleted = await _commentService.DeleteAsync(
            subTaskId, commentId, userId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException(
                "El token no contiene un usuario válido.");

        return userId;
    }
}
