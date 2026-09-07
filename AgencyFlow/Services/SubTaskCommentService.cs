using AgencyFlow.Data;
using AgencyFlow.DTOs.SubTaskComment;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public class SubTaskCommentService
{
    private readonly AppDbContext _db;
    private readonly WorkAssignmentService _workAssignment;
    private readonly ResourceAuthorizationService _authorization;
    private readonly NotificationService _notifications;

    public SubTaskCommentService(AppDbContext db, WorkAssignmentService workAssignment, ResourceAuthorizationService authorization, NotificationService? notifications = null)
    {
        _db = db;
        _workAssignment = workAssignment;
        _authorization = authorization;
        _notifications = notifications ?? new NotificationService(db, authorization);
    }

    public async Task<List<SubTaskCommentDto>> GetAllAsync(Guid subTaskId)
    {
        await EnsureSubTaskScopeAsync(subTaskId);
        var subTaskExists = await _db.SubTasks.AnyAsync(subTask =>
            subTask.Id == subTaskId &&
            subTask.DeletedAt == null &&
            subTask.TaskItem.DeletedAt == null &&
            subTask.TaskItem.SubProject.DeletedAt == null &&
            subTask.TaskItem.SubProject.Project.DeletedAt == null);

        if (!subTaskExists)
            throw new BusinessValidationException(
                "La subtarea indicada no existe.");

        return await _db.SubTaskComments
            .AsNoTracking()
            .Where(comment =>
                comment.SubTaskId == subTaskId &&
                comment.DeletedAt == null)
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new SubTaskCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId,
                UserFirstName = comment.User.FirstName,
                UserLastName = comment.User.LastName
            })
            .ToListAsync();
    }

    public async Task<SubTaskCommentDto> CreateAsync(
        Guid subTaskId,
        Guid userId,
        CreateSubTaskCommentDto dto)
    {
        await EnsureSubTaskScopeAsync(subTaskId);
        var content = dto.Content.Trim();

        if (string.IsNullOrWhiteSpace(content))
            throw new BusinessValidationException(
                "El comentario no puede estar vacío.");

        var subTaskExists = await _db.SubTasks.AnyAsync(subTask =>
            subTask.Id == subTaskId &&
            subTask.DeletedAt == null &&
            subTask.TaskItem.DeletedAt == null &&
            subTask.TaskItem.SubProject.DeletedAt == null &&
            subTask.TaskItem.SubProject.Project.DeletedAt == null);

        if (!subTaskExists)
            throw new BusinessValidationException(
                "La subtarea indicada no existe.");

        var userExists = await _db.Users.AnyAsync(user =>
            user.Id == userId && user.DeletedAt == null);

        if (!userExists)
            throw new BusinessValidationException(
                "El usuario autenticado no existe.");

        var comment = new Models.SubTaskComment
        {
            Content = content,
            SubTaskId = subTaskId,
            UserId = userId,
            CreatedBy = userId.ToString()
        };

        _db.SubTaskComments.Add(comment);
        var now = DateTime.UtcNow;
        var subTask = await _db.SubTasks.FirstAsync(item => item.Id == subTaskId);
        subTask.LastActivityAt = now;
        subTask.UpdatedAt = now;
        await _workAssignment.TouchParentTaskAsync(subTask.TaskItemId, now);
        await _notifications.NotifyCommentAsync(subTask, userId);
        await _db.SaveChangesAsync();
        await _db.Entry(comment).Reference(item => item.User).LoadAsync();

        return new SubTaskCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UserId = comment.UserId,
            UserFirstName = comment.User.FirstName,
            UserLastName = comment.User.LastName
        };
    }

    public async Task<bool> DeleteAsync(
        Guid subTaskId,
        Guid commentId,
        Guid userId)
    {
        await EnsureSubTaskScopeAsync(subTaskId);
        var comment = await _db.SubTaskComments.FirstOrDefaultAsync(item =>
            item.Id == commentId &&
            item.SubTaskId == subTaskId &&
            item.UserId == userId &&
            item.DeletedAt == null &&
            item.SubTask.DeletedAt == null &&
            item.SubTask.TaskItem.DeletedAt == null &&
            item.SubTask.TaskItem.SubProject.DeletedAt == null &&
            item.SubTask.TaskItem.SubProject.Project.DeletedAt == null);

        if (comment == null)
            return false;

        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = userId.ToString();
        comment.SubTask.LastActivityAt = comment.DeletedAt;
        comment.SubTask.UpdatedAt = comment.DeletedAt;
        await _workAssignment.TouchParentTaskAsync(
            comment.SubTask.TaskItemId, comment.DeletedAt.Value);
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task EnsureSubTaskScopeAsync(Guid subTaskId)
    {
        if (_authorization.IsOperationalUser)
        {
            var ownsSubTask = await _db.SubTasks.AnyAsync(subTask =>
                subTask.Id == subTaskId &&
                subTask.DeletedAt == null &&
                subTask.AssignedUserId == _authorization.UserId);

            if (!ownsSubTask)
            {
                throw new ForbiddenException(
                    "No tiene permiso para acceder a esta subtarea.");
            }

            return;
        }

        var scope = await _db.SubTasks.Where(s => s.Id == subTaskId && s.DeletedAt == null)
            .Select(s => new { s.TaskItem.SubProject.ProjectId, s.TaskItem.SubProject.DepartmentId }).FirstOrDefaultAsync();
        if (scope != null) await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
    }
}
