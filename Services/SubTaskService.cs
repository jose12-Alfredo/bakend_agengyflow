using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.SubTask;
using AgencyFlow.Exceptions;

namespace AgencyFlow.Services;

public class SubTaskService
{
    private readonly AppDbContext _db;
    private readonly WorkAssignmentService _workAssignment;

    public SubTaskService(AppDbContext db, WorkAssignmentService workAssignment)
    {
        _db = db;
        _workAssignment = workAssignment;
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
        await ValidateRelationsAsync(dto.TaskItemId, dto.AssignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            dto.AssignedUserId, dto.Status, "subtarea");
        var now = DateTime.UtcNow;

        var st = new Models.SubTask
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            TaskItemId = dto.TaskItemId,
            AssignedUserId = dto.AssignedUserId,
            LastActivityAt = now
        };

        _db.SubTasks.Add(st);
        _db.SubTaskStatusHistories.Add(new Models.SubTaskStatusHistory
        {
            SubTaskId = st.Id,
            FromStatus = null,
            ToStatus = st.Status
        });
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
        var st = await _db.SubTasks
            .Include(s => s.TaskItem)
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
            _db.SubTaskStatusHistories.Add(new Models.SubTaskStatusHistory
            {
                SubTaskId = st.Id,
                FromStatus = previousStatus,
                ToStatus = st.Status,
                Timestamp = st.UpdatedAt.Value
            });
        }

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

    public async Task<bool> DeleteAsync(Guid id)
    {
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
        var subTask = await _db.SubTasks.FirstOrDefaultAsync(item =>
            item.Id == id && item.DeletedAt == null &&
            item.TaskItem.DeletedAt == null &&
            item.TaskItem.SubProject.DeletedAt == null &&
            item.TaskItem.SubProject.Project.DeletedAt == null);

        if (subTask == null)
            return null;

        await _workAssignment.ValidateAssigneeAsync(assignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            assignedUserId, subTask.Status, "subtarea");

        var now = DateTime.UtcNow;
        subTask.AssignedUserId = assignedUserId;
        subTask.UpdatedAt = now;
        subTask.LastActivityAt = now;
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

    private Task<int> GetCommentCountAsync(Guid subTaskId)
    {
        return _db.SubTaskComments.CountAsync(comment =>
            comment.SubTaskId == subTaskId &&
            comment.DeletedAt == null);
    }

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

        _db.TaskStatusHistories.Add(new Models.TaskStatusHistory
        {
            TaskId = task.Id,
            FromStatus = previousStatus,
            ToStatus = nextStatus,
            Timestamp = changedAt
        });

        await _db.SaveChangesAsync();
    }
}
