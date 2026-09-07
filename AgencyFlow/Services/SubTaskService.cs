using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.SubTask;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;

namespace AgencyFlow.Services;

public class SubTaskService
{
    private readonly AppDbContext _db;
    private readonly WorkAssignmentService _workAssignment;
    private readonly ResourceAuthorizationService _authorization;
    private readonly NotificationService _notifications;
    private readonly WorkTimingService _timing;

    public SubTaskService(AppDbContext db, WorkAssignmentService workAssignment, ResourceAuthorizationService authorization, NotificationService? notifications = null, WorkTimingService? timing = null)
    {
        _db = db;
        _workAssignment = workAssignment;
        _authorization = authorization;
        _notifications = notifications ?? new NotificationService(db, authorization);
        _timing = timing ?? new WorkTimingService(db);
    }

    public async Task<PagedResultDto<SubTaskDto>> GetAllAsync(
        int page, int pageSize,
        string? titulo, Guid? taskItemId,
        string? status, Guid? assignedUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.SubTasks
            .AsNoTracking()
            .Include(st => st.TaskItem)
            .Include(st => st.AssignedUser)
            .Where(st =>
                st.DeletedAt == null &&
                st.TaskItem.DeletedAt == null &&
                st.TaskItem.SubProject.DeletedAt == null &&
                st.TaskItem.SubProject.Project.DeletedAt == null);
        query = _authorization.FilterSubTasks(query);

        if (!string.IsNullOrWhiteSpace(titulo))
            query = query.Where(st => st.Title.ToLower().Contains(titulo.ToLower()));

        if (taskItemId.HasValue)
            query = query.Where(st => st.TaskItemId == taskItemId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(st => st.Status == status);

        if (assignedUserId.HasValue)
            query = query.Where(st => st.AssignedUserId == assignedUserId.Value);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(st => new SubTaskDto
            {
                Id = st.Id,
                Title = st.Title,
                Detail = st.Detail,
                StartDate = st.StartDate,
                EndDate = st.EndDate,
                Status = st.Status,
                IsOverdue = st.EndDate.HasValue &&
                    st.EndDate.Value < today &&
                    st.Status != Models.SubTaskStatuses.Completed,
                CommentCount = st.Comments.Count(comment =>
                    comment.DeletedAt == null),
                TaskItemId = st.TaskItemId,
                TaskItemTitle = st.TaskItem.Title,
                AssignedUserId = st.AssignedUserId,
                AssignedUserFirstName = st.AssignedUser != null ? st.AssignedUser.FirstName : null,
                AssignedUserLastName = st.AssignedUser != null ? st.AssignedUser.LastName : null
            })
            .ToListAsync();

        return new PagedResultDto<SubTaskDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<SubTaskDto?> GetByIdAsync(Guid id)
    {
        await EnsureSubTaskScopeAsync(id);
        var st = await _db.SubTasks
            .AsNoTracking()
            .Include(s => s.TaskItem)
            .Include(s => s.AssignedUser)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.TaskItem.DeletedAt == null &&
                s.TaskItem.SubProject.DeletedAt == null &&
                s.TaskItem.SubProject.Project.DeletedAt == null);

        if (st == null) return null;

        return new SubTaskDto
        {
            Id = st.Id,
            Title = st.Title,
            Detail = st.Detail,
            StartDate = st.StartDate,
            EndDate = st.EndDate,
            Status = st.Status,
            IsOverdue = IsOverdue(st.EndDate, st.Status),
            CommentCount = await GetCommentCountAsync(st.Id),
            TaskItemId = st.TaskItemId,
            TaskItemTitle = st.TaskItem.Title,
            AssignedUserId = st.AssignedUserId,
            AssignedUserFirstName = st.AssignedUser?.FirstName,
            AssignedUserLastName = st.AssignedUser?.LastName
        };
    }

    public async Task<SubTaskDto> CreateAsync(CreateSubTaskDto dto)
    {
        var assignedUserId = dto.AssignedUserId;

        if (_authorization.IsOperationalUser)
        {
            var ownsParentTask = await _db.TaskItems.AnyAsync(task =>
                task.Id == dto.TaskItemId &&
                task.DeletedAt == null &&
                task.SubProject.DeletedAt == null &&
                task.SubProject.Project.DeletedAt == null &&
                task.AssignedUserId == _authorization.UserId);

            if (!ownsParentTask)
            {
                throw new ForbiddenException(
                    "Solo puede crear subtareas dentro de sus propias tareas.");
            }

            // La identidad se obtiene del JWT; nunca se acepta un responsable
            // enviado por el cliente para una creacion operativa.
            assignedUserId = _authorization.UserId;
        }
        else
        {
            await EnsureTaskScopeAsync(dto.TaskItemId, assignedUserId);
        }

        await ValidateRelationsAsync(dto.TaskItemId, assignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            assignedUserId, dto.Status, "subtarea");
        var now = DateTime.UtcNow;

        var st = new Models.SubTask
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            TaskItemId = dto.TaskItemId,
            AssignedUserId = assignedUserId,
            LastActivityAt = now,
            ActualStartedAt = dto.Status == Models.SubTaskStatuses.InProgress ? now : null,
            CompletedAt = dto.Status == Models.SubTaskStatuses.Completed ? now : null
        };

        _db.SubTasks.Add(st);
        _db.SubTaskStatusHistories.Add(new Models.SubTaskStatusHistory
        {
            SubTaskId = st.Id,
            FromStatus = null,
            ToStatus = st.Status,
            Timestamp = now,
            ChangedByUserId = _authorization.UserId
        });
        await _notifications.NotifyAssignment(st.AssignedUserId, _authorization.UserId,
            "subtask", st.Id, st.Title);
        await _db.SaveChangesAsync();
        await _workAssignment.TouchParentTaskAsync(st.TaskItemId, now);
        await _db.SaveChangesAsync();
        await SyncParentTaskStatusAsync(st.TaskItemId);

        await _db.Entry(st).Reference(s => s.TaskItem).LoadAsync();
        await _db.Entry(st).Reference(s => s.AssignedUser).LoadAsync();

        return new SubTaskDto
        {
            Id = st.Id,
            Title = st.Title,
            Detail = st.Detail,
            StartDate = st.StartDate,
            EndDate = st.EndDate,
            Status = st.Status,
            IsOverdue = IsOverdue(st.EndDate, st.Status),
            CommentCount = 0,
            TaskItemId = st.TaskItemId,
            TaskItemTitle = st.TaskItem.Title,
            AssignedUserId = st.AssignedUserId,
            AssignedUserFirstName = st.AssignedUser?.FirstName,
            AssignedUserLastName = st.AssignedUser?.LastName
        };
    }

    public async Task<SubTaskDto?> UpdateAsync(Guid id, UpdateSubTaskDto dto)
    {
        await EnsureSubTaskScopeAsync(id);
        await EnsureTaskScopeAsync(dto.TaskItemId, dto.AssignedUserId);
        var st = await _db.SubTasks
            .Include(s => s.TaskItem)
            .ThenInclude(task => task.SubProject)
            .Include(s => s.AssignedUser)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.TaskItem.DeletedAt == null &&
                s.TaskItem.SubProject.DeletedAt == null &&
                s.TaskItem.SubProject.Project.DeletedAt == null);

        if (st == null) return null;

        await ValidateRelationsAsync(dto.TaskItemId, dto.AssignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            dto.AssignedUserId, dto.Status, "subtarea");

        var previousStatus = st.Status;
        var previousTaskItemId = st.TaskItemId;
        var previousAssigneeId = st.AssignedUserId;

        st.Title = dto.Title;
        st.Detail = dto.Detail;
        st.StartDate = dto.StartDate;
        st.EndDate = dto.EndDate;
        st.Status = dto.Status;
        st.TaskItemId = dto.TaskItemId;
        st.AssignedUserId = dto.AssignedUserId;
        st.UpdatedAt = DateTime.UtcNow;
        st.LastActivityAt = st.UpdatedAt;

        if (!string.Equals(previousStatus, st.Status, StringComparison.Ordinal))
        {
            WorkTimingService.ApplyTransition(st, previousStatus, st.Status,
                st.UpdatedAt.Value, _authorization.UserId);
        }

        if (previousAssigneeId != st.AssignedUserId)
            await _notifications.NotifyAssignment(st.AssignedUserId, _authorization.UserId,
                "subtask", st.Id, st.Title);
        if (!string.Equals(previousStatus, st.Status, StringComparison.Ordinal))
            await _notifications.NotifySubTaskStatusAsync(st, _authorization.UserId, previousStatus);

        await _db.SaveChangesAsync();
        await _workAssignment.TouchParentTaskAsync(st.TaskItemId, st.UpdatedAt.Value);
        await _db.SaveChangesAsync();
        await SyncParentTaskStatusAsync(previousTaskItemId);

        if (previousTaskItemId != st.TaskItemId)
            await SyncParentTaskStatusAsync(st.TaskItemId);

        await _db.Entry(st).Reference(s => s.TaskItem).LoadAsync();
        await _db.Entry(st).Reference(s => s.AssignedUser).LoadAsync();

        return new SubTaskDto
        {
            Id = st.Id,
            Title = st.Title,
            Detail = st.Detail,
            StartDate = st.StartDate,
            EndDate = st.EndDate,
            Status = st.Status,
            IsOverdue = IsOverdue(st.EndDate, st.Status),
            CommentCount = await GetCommentCountAsync(st.Id),
            TaskItemId = st.TaskItemId,
            TaskItemTitle = st.TaskItem.Title,
            AssignedUserId = st.AssignedUserId,
            AssignedUserFirstName = st.AssignedUser?.FirstName,
            AssignedUserLastName = st.AssignedUser?.LastName
        };
    }

    public async Task<SubTaskDto?> UpdateStatusAsync(Guid id, string status)
    {
        if (!Models.SubTaskStatuses.OperatorAllowed.Contains(status))
            throw new BusinessValidationException("El estado de la subtarea no es valido.");

        var subTask = await _db.SubTasks
            .Include(item => item.TaskItem)
            .ThenInclude(task => task.SubProject)
            .Include(item => item.AssignedUser)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null);

        if (subTask == null)
            return null;

        if (_authorization.IsOperationalUser)
        {
            if (subTask.AssignedUserId != _authorization.UserId)
                throw new ForbiddenException("No tiene permiso para modificar esta subtarea.");
        }
        else if (_authorization.IsAdministrator || _authorization.IsDirector)
        {
            await EnsureSubTaskScopeAsync(id);
        }
        else
        {
            throw new ForbiddenException("No tiene permiso para modificar esta subtarea.");
        }

        var previousStatus = subTask.Status;
        if (!Models.SubTaskStatuses.IsAllowedTransition(previousStatus, status))
            throw new BusinessValidationException(
                $"No se permite cambiar el estado de '{previousStatus}' a '{status}'.");

        if (previousStatus == status)
            return await MapSubTaskAsync(subTask);

        subTask.Status = status;
        subTask.UpdatedAt = DateTime.UtcNow;
        subTask.LastActivityAt = subTask.UpdatedAt;
        WorkTimingService.ApplyTransition(subTask, previousStatus, status,
            subTask.UpdatedAt.Value, _authorization.UserId);
        await _workAssignment.TouchParentTaskAsync(subTask.TaskItemId, subTask.UpdatedAt.Value);
        await _notifications.NotifySubTaskStatusAsync(subTask, _authorization.UserId, previousStatus);
        await _db.SaveChangesAsync();
        await SyncParentTaskStatusAsync(subTask.TaskItemId);

        return await MapSubTaskAsync(subTask);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await EnsureSubTaskScopeAsync(id);
        var st = await _db.SubTasks
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.TaskItem.DeletedAt == null &&
                s.TaskItem.SubProject.DeletedAt == null &&
                s.TaskItem.SubProject.Project.DeletedAt == null);

        if (st == null) return false;

        var taskItemId = st.TaskItemId;
        st.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _workAssignment.TouchParentTaskAsync(taskItemId, st.DeletedAt.Value);
        await _db.SaveChangesAsync();
        await SyncParentTaskStatusAsync(taskItemId);

        return true;
    }

    private async Task ValidateRelationsAsync(
        Guid taskItemId,
        Guid? assignedUserId)
    {
        var taskExists = await _db.TaskItems.AnyAsync(t =>
            t.Id == taskItemId &&
            t.DeletedAt == null &&
            t.SubProject.DeletedAt == null &&
            t.SubProject.Project.DeletedAt == null);

        if (!taskExists)
            throw new BusinessValidationException(
                "La tarea indicada no existe.");

        await _workAssignment.ValidateAssigneeAsync(assignedUserId);
    }

    public async Task<SubTaskDto?> UpdateAssigneeAsync(Guid id, Guid? assignedUserId)
    {
        await EnsureSubTaskScopeAsync(id);
        var subTask = await _db.SubTasks.FirstOrDefaultAsync(item =>
            item.Id == id && item.DeletedAt == null &&
            item.TaskItem.DeletedAt == null &&
            item.TaskItem.SubProject.DeletedAt == null &&
            item.TaskItem.SubProject.Project.DeletedAt == null);

        if (subTask == null)
            return null;

        await _workAssignment.ValidateAssigneeAsync(assignedUserId);
        var departmentId = await _db.SubTasks.Where(s => s.Id == id).Select(s => s.TaskItem.SubProject.DepartmentId).SingleAsync();
        await _authorization.EnsureAssigneeAsync(assignedUserId, departmentId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            assignedUserId, subTask.Status, "subtarea");

        var now = DateTime.UtcNow;
        var previousAssigneeId = subTask.AssignedUserId;
        subTask.AssignedUserId = assignedUserId;
        subTask.UpdatedAt = now;
        subTask.LastActivityAt = now;
        if (previousAssigneeId != assignedUserId)
            await _notifications.NotifyAssignment(assignedUserId, _authorization.UserId,
                "subtask", subTask.Id, subTask.Title);
        await _workAssignment.TouchParentTaskAsync(subTask.TaskItemId, now);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private static bool IsOverdue(DateOnly? endDate, string status)
    {
        return endDate.HasValue &&
            endDate.Value < DateOnly.FromDateTime(DateTime.UtcNow) &&
            status != Models.SubTaskStatuses.Completed;
    }

    private async Task EnsureSubTaskScopeAsync(Guid id)
    {
        if (_authorization.IsOperationalUser)
        {
            var ownsSubTask = await _db.SubTasks.AnyAsync(subTask =>
                subTask.Id == id &&
                subTask.DeletedAt == null &&
                subTask.AssignedUserId == _authorization.UserId);

            if (!ownsSubTask)
            {
                throw new ForbiddenException(
                    "No tiene permiso para acceder a esta subtarea.");
            }

            return;
        }

        var scope = await _db.SubTasks.Where(s => s.Id == id && s.DeletedAt == null)
            .Select(s => new { s.TaskItem.SubProject.ProjectId, s.TaskItem.SubProject.DepartmentId }).FirstOrDefaultAsync();
        if (scope != null) await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
    }

    private async Task EnsureTaskScopeAsync(Guid taskItemId, Guid? assignedUserId)
    {
        if (_authorization.IsOperationalUser)
        {
            var ownsParentTask = await _db.TaskItems.AnyAsync(task =>
                task.Id == taskItemId &&
                task.DeletedAt == null &&
                task.SubProject.DeletedAt == null &&
                task.SubProject.Project.DeletedAt == null &&
                task.AssignedUserId == _authorization.UserId);

            if (!ownsParentTask)
            {
                throw new ForbiddenException(
                    "Solo puede usar tareas que le fueron asignadas.");
            }

            var departmentId = await _db.TaskItems
                .Where(task => task.Id == taskItemId)
                .Select(task => task.SubProject.DepartmentId)
                .SingleAsync();
            await _authorization.EnsureAssigneeAsync(assignedUserId, departmentId);
            return;
        }

        var scope = await _db.TaskItems.Where(t => t.Id == taskItemId && t.DeletedAt == null)
            .Select(t => new { t.SubProject.ProjectId, t.SubProject.DepartmentId }).FirstOrDefaultAsync();
        if (scope == null) return;
        await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
        await _authorization.EnsureAssigneeAsync(assignedUserId, scope.DepartmentId);
    }

    private Task<int> GetCommentCountAsync(Guid subTaskId)
    {
        return _db.SubTaskComments.CountAsync(comment =>
            comment.SubTaskId == subTaskId &&
            comment.DeletedAt == null);
    }

    private async Task<SubTaskDto> MapSubTaskAsync(Models.SubTask subTask) => new()
    {
        Id = subTask.Id,
        Title = subTask.Title,
        Detail = subTask.Detail,
        StartDate = subTask.StartDate,
        EndDate = subTask.EndDate,
        Status = subTask.Status,
        IsOverdue = IsOverdue(subTask.EndDate, subTask.Status),
        CommentCount = await GetCommentCountAsync(subTask.Id),
        TaskItemId = subTask.TaskItemId,
        TaskItemTitle = subTask.TaskItem.Title,
        AssignedUserId = subTask.AssignedUserId,
        AssignedUserFirstName = subTask.AssignedUser?.FirstName,
        AssignedUserLastName = subTask.AssignedUser?.LastName
    };

    private async Task SyncParentTaskStatusAsync(Guid taskItemId)
    {
        var task = await _db.TaskItems.FirstOrDefaultAsync(item =>
            item.Id == taskItemId &&
            item.DeletedAt == null &&
            item.SubProject.DeletedAt == null &&
            item.SubProject.Project.DeletedAt == null);

        if (task == null)
            return;

        var subTaskStatuses = await _db.SubTasks
            .Where(subTask =>
                subTask.TaskItemId == taskItemId &&
                subTask.DeletedAt == null)
            .Select(subTask => subTask.Status)
            .ToListAsync();

        var nextStatus = subTaskStatuses.Count switch
        {
            0 => Models.TaskItemStatuses.Pending,
            _ when subTaskStatuses.All(status =>
                status == Models.SubTaskStatuses.Pending) =>
                Models.TaskItemStatuses.Pending,
            _ when subTaskStatuses.All(status =>
                status == Models.SubTaskStatuses.Completed) =>
                Models.TaskItemStatuses.Completed,
            _ when subTaskStatuses
                .Where(status => status != Models.SubTaskStatuses.Completed)
                .All(status => status == Models.SubTaskStatuses.InReview) =>
                Models.TaskItemStatuses.InReview,
            _ => Models.TaskItemStatuses.InProgress
        };

        if (string.Equals(task.Status, nextStatus, StringComparison.Ordinal))
            return;

        if (!task.AssignedUserId.HasValue && nextStatus != Models.TaskItemStatuses.Pending)
            return;

        var previousStatus = task.Status;
        var changedAt = DateTime.UtcNow;

        task.Status = nextStatus;
        task.UpdatedAt = changedAt;
        WorkTimingService.ApplyTransition(task, previousStatus, nextStatus,
            changedAt, _authorization.UserId);

        await _db.SaveChangesAsync();
        await _timing.SyncHierarchyAsync(task.Id, changedAt, _authorization.UserId);
    }
}
