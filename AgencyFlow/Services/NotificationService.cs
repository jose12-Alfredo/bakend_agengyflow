using AgencyFlow.Authorization;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.Notification;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public sealed class NotificationService(
    AppDbContext db,
    ResourceAuthorizationService authorization)
{
    public async Task<PagedResultDto<NotificationDto>> GetAllAsync(
        int page, int pageSize, bool unreadOnly)
    {
        await EnsureDeadlineNotificationsAsync();

        var query = db.Notifications.AsNoTracking().Where(notification =>
            notification.UserId == authorization.UserId &&
            notification.DeletedAt == null);

        if (unreadOnly)
            query = query.Where(notification => notification.ReadAt == null);

        var total = await query.CountAsync();
        var notifications = await query.OrderByDescending(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = new List<NotificationDto>(notifications.Count);
        foreach (var notification in notifications)
        {
            var url = await ResolveUrlAsync(notification.ResourceType, notification.ResourceId,
                notification.Url);
            items.Add(Map(notification, url));
        }

        return new PagedResultDto<NotificationDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync()
    {
        await EnsureDeadlineNotificationsAsync();
        return new NotificationSummaryDto
        {
            UnreadCount = await db.Notifications.CountAsync(notification =>
                notification.UserId == authorization.UserId &&
                notification.ReadAt == null && notification.DeletedAt == null)
        };
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(item =>
            item.Id == id && item.UserId == authorization.UserId && item.DeletedAt == null);
        if (notification == null) return false;
        notification.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync()
    {
        var now = DateTime.UtcNow;
        var notifications = await db.Notifications.Where(item =>
            item.UserId == authorization.UserId && item.ReadAt == null && item.DeletedAt == null)
            .ToListAsync();
        foreach (var notification in notifications) notification.ReadAt = now;
        await db.SaveChangesAsync();
    }

    public async Task NotifyAssignment(Guid? recipientId, Guid actorId, string resourceType,
        Guid resourceId, string title)
    {
        if (!recipientId.HasValue || recipientId == actorId) return;
        var actorName = await GetActorNameAsync(actorId);
        Add(recipientId.Value, "assignment", $"Nueva {ResourceLabel(resourceType)} asignada",
            $"Se te asignó “{title}”", resourceType, resourceId,
            $"assignment:{resourceType}:{resourceId}:{recipientId}", actorName: actorName);
    }

    public async Task NotifySubTaskStatusAsync(SubTask subTask, Guid actorId,
        string previousStatus)
    {
        var actorName = await GetActorNameAsync(actorId);
        var recipients = await GetSupervisorsAsync(subTask.TaskItem.SubProject.ProjectId,
            subTask.TaskItem.SubProject.DepartmentId);
        if (subTask.AssignedUserId.HasValue) recipients.Add(subTask.AssignedUserId.Value);
        recipients.Remove(actorId);

        foreach (var recipient in recipients)
            Add(recipient, subTask.Status == SubTaskStatuses.InReview ? "review" : "status",
                subTask.Status == SubTaskStatuses.InReview ? "Subtarea enviada a revisión" : "Estado actualizado",
                $"“{subTask.Title}” cambió de {previousStatus} a {subTask.Status}",
                "subtask", subTask.Id, actorName: actorName);
    }

    public async Task NotifyCommentAsync(SubTask subTask, Guid actorId)
    {
        var actorName = await GetActorNameAsync(actorId);
        var recipients = await db.SubTaskComments.Where(comment =>
                comment.SubTaskId == subTask.Id && comment.DeletedAt == null)
            .Select(comment => comment.UserId).Distinct().ToListAsync();
        if (subTask.AssignedUserId.HasValue) recipients.Add(subTask.AssignedUserId.Value);

        foreach (var recipient in recipients.Distinct().Where(id => id != actorId))
            Add(recipient, "comment", $"Nuevo comentario de {actorName}",
                $"Comentaron la subtarea “{subTask.Title}”", "subtask", subTask.Id);
    }

    private async Task<string> GetActorNameAsync(Guid actorId)
    {
        return await db.Users.AsNoTracking()
            .Where(user => user.Id == actorId && user.DeletedAt == null)
            .Select(user => user.FirstName + " " + user.LastName)
            .FirstOrDefaultAsync() ?? "Un integrante";
    }

    private async Task<HashSet<Guid>> GetSupervisorsAsync(Guid projectId, Guid departmentId)
    {
        var administrators = await db.Users.Where(user => user.DeletedAt == null &&
                user.Role.DeletedAt == null &&
                (user.Role.Name == RoleNames.Manager || user.Role.Name == RoleNames.SuperUser))
            .Select(user => user.Id).ToListAsync();
        var directors = await db.Users.Where(user => user.DeletedAt == null &&
                user.Role.Name == RoleNames.Director &&
                user.DepartmentUsers.Any(member => member.DepartmentId == departmentId &&
                    member.IsDirector && member.DeletedAt == null) &&
                db.ProjectDirectorAccesses.Any(access => access.ProjectId == projectId &&
                    access.DirectorUserId == user.Id && access.DeletedAt == null))
            .Select(user => user.Id).ToListAsync();
        return administrators.Concat(directors).ToHashSet();
    }

    private async Task EnsureDeadlineNotificationsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(3);
        var userId = authorization.UserId;
        var tasks = await db.TaskItems.AsNoTracking().Where(task => task.DeletedAt == null &&
                task.AssignedUserId == userId && task.EndDate.HasValue && task.EndDate <= limit &&
                task.Status != TaskItemStatuses.Completed)
            .Select(task => new { task.Id, task.Title, EndDate = task.EndDate!.Value }).ToListAsync();
        var subTasks = await db.SubTasks.AsNoTracking().Where(item => item.DeletedAt == null &&
                item.AssignedUserId == userId && item.EndDate.HasValue && item.EndDate <= limit &&
                item.Status != SubTaskStatuses.Completed)
            .Select(item => new { item.Id, item.Title, EndDate = item.EndDate!.Value }).ToListAsync();

        foreach (var task in tasks)
            AddDeadline(userId, "task", task.Id, task.Title, task.EndDate, today);
        foreach (var subTask in subTasks)
            AddDeadline(userId, "subtask", subTask.Id, subTask.Title, subTask.EndDate, today);
        await db.SaveChangesAsync();
    }

    private void AddDeadline(Guid userId, string resourceType, Guid resourceId,
        string title, DateOnly endDate, DateOnly today)
    {
        var overdue = endDate < today;
        var type = overdue ? "overdue" : "deadline";
        var heading = overdue ? "Trabajo vencido" : "Próximo vencimiento";
        var message = overdue
            ? $"“{title}” venció el {endDate:dd/MM/yyyy}"
            : $"“{title}” vence el {endDate:dd/MM/yyyy}";
        Add(userId, type, heading, message, resourceType, resourceId,
            $"deadline:{resourceType}:{resourceId}:{type}:{today:yyyyMMdd}");
    }

    private void Add(Guid userId, string type, string title, string message,
        string? resourceType = null, Guid? resourceId = null, string? dedupeKey = null,
        string? actorName = null)
    {
        var url = resourceType == "task" ? "/tasks" :
            resourceType == "subtask" ? "/subtasks" : null;

        if (!string.IsNullOrWhiteSpace(actorName))
        {
            message = $"{actorName}: {message}";
        }
        if (resourceType == "task" && resourceId.HasValue)
        {
            var context = db.TaskItems.AsNoTracking()
                .Where(task => task.Id == resourceId.Value && task.DeletedAt == null)
                .Select(task => new
                {
                    task.SubProjectId,
                    ProjectTitle = task.SubProject.Project.Title,
                    SubProjectTitle = task.SubProject.Title
                })
                .FirstOrDefault();

            if (context != null)
            {
                url = $"/subtasks?subProjectId={context.SubProjectId}&taskId={resourceId.Value}";
                message = $"{message} Proyecto: {context.ProjectTitle} · " +
                    $"Subproyecto: {context.SubProjectTitle}";
            }
        }
        else if (resourceType == "subtask" && resourceId.HasValue)
        {
            var context = db.SubTasks.AsNoTracking()
                .Where(subTask => subTask.Id == resourceId.Value &&
                    subTask.DeletedAt == null)
                .Select(subTask => new
                {
                    TaskId = subTask.TaskItemId,
                    SubProjectId = subTask.TaskItem.SubProjectId,
                    subTask.TaskItem.SubProject.Project.Title,
                    SubProjectTitle = subTask.TaskItem.SubProject.Title,
                    TaskTitle = subTask.TaskItem.Title
                })
                .FirstOrDefault();

            if (context != null)
            {
                url = $"/subtasks?subProjectId={context.SubProjectId}&taskId={context.TaskId}";
                message = $"{message} Proyecto: {context.Title} · " +
                    $"Subproyecto: {context.SubProjectTitle} · " +
                    $"Tarea: {context.TaskTitle}";
            }
        }

        if (dedupeKey != null && db.Notifications.Local.Any(item =>
                item.UserId == userId && item.DedupeKey == dedupeKey)) return;
        if (dedupeKey != null && db.Notifications.Any(item =>
                item.UserId == userId && item.DedupeKey == dedupeKey && item.DeletedAt == null)) return;

        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Url = url,
            DedupeKey = dedupeKey
        });
    }

    private static string ResourceLabel(string type) => type == "task" ? "tarea" : "subtarea";

    private async Task<string?> ResolveUrlAsync(string? resourceType, Guid? resourceId,
        string? fallbackUrl)
    {
        if (!resourceId.HasValue) return fallbackUrl;

        if (resourceType == "task")
        {
            var subProjectId = await db.TaskItems.AsNoTracking()
                .Where(task => task.Id == resourceId.Value && task.DeletedAt == null)
                .Select(task => (Guid?)task.SubProjectId)
                .FirstOrDefaultAsync();
            return subProjectId.HasValue
                ? $"/subtasks?subProjectId={subProjectId.Value}&taskId={resourceId.Value}"
                : fallbackUrl;
        }

        if (resourceType == "subtask")
        {
            var route = await db.SubTasks.AsNoTracking()
                .Where(subTask => subTask.Id == resourceId.Value && subTask.DeletedAt == null)
                .Select(subTask => new { subTask.TaskItemId, subTask.TaskItem.SubProjectId })
                .FirstOrDefaultAsync();
            return route == null
                ? fallbackUrl
                : $"/subtasks?subProjectId={route.SubProjectId}&taskId={route.TaskItemId}";
        }

        return fallbackUrl;
    }

    private static NotificationDto Map(Notification item, string? url = null) => new()
    {
        Id = item.Id,
        Type = item.Type,
        Title = item.Title,
        Message = item.Message,
        ResourceType = item.ResourceType,
        ResourceId = item.ResourceId,
        Url = url ?? item.Url,
        CreatedAt = item.CreatedAt,
        ReadAt = item.ReadAt
    };
}
