using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.TaskItem;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;

namespace AgencyFlow.Services;

public class TaskItemService
{
    private readonly AppDbContext _db;
    private readonly WorkAssignmentService _workAssignment;
    private readonly ResourceAuthorizationService _authorization;
    private readonly NotificationService _notifications;

    public TaskItemService(AppDbContext db, WorkAssignmentService workAssignment, ResourceAuthorizationService authorization, NotificationService? notifications = null)
    {
        _db = db;
        _workAssignment = workAssignment;
        _authorization = authorization;
        _notifications = notifications ?? new NotificationService(db, authorization);
    }

    public async Task<PagedResultDto<TaskItemDto>> GetAllAsync(
        int page, int pageSize,
        string? titulo, Guid? clientCompanyId, Guid? projectId,
        Guid? subProjectId,
        string? status, Guid? assignedUserId)
    {
        var query = _db.TaskItems
            .AsNoTracking()
            .Include(t => t.SubProject)
            .Include(t => t.AssignedUser)
            .Where(t =>
                t.DeletedAt == null &&
                t.SubProject.DeletedAt == null &&
                t.SubProject.Project.DeletedAt == null);
        query = _authorization.FilterTasks(query);

        if (!string.IsNullOrWhiteSpace(titulo))
            query = query.Where(t => t.Title.ToLower().Contains(titulo.ToLower()));

        if (clientCompanyId.HasValue)
            query = query.Where(t =>
                t.SubProject.Project.ClientUser != null &&
                t.SubProject.Project.ClientUser.ClientCompanyId ==
                    clientCompanyId.Value);

        if (projectId.HasValue)
            query = query.Where(t =>
                t.SubProject.ProjectId == projectId.Value);

        if (subProjectId.HasValue)
            query = query.Where(t => t.SubProjectId == subProjectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (assignedUserId.HasValue)
            query = query.Where(t => t.AssignedUserId == assignedUserId.Value);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Detail = t.Detail,
                CreatedAt = t.CreatedAt,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status,
                SubProjectId = t.SubProjectId,
                SubProjectTitle = t.SubProject.Title,
                ProjectId = t.SubProject.ProjectId,
                ProjectTitle = t.SubProject.Project.Title,
                ClientCompanyId = t.SubProject.Project.ClientUser != null
                    ? t.SubProject.Project.ClientUser.ClientCompanyId
                    : null,
                ClientCompanyName = t.SubProject.Project.ClientUser != null &&
                    t.SubProject.Project.ClientUser.ClientCompany != null
                        ? t.SubProject.Project.ClientUser.ClientCompany.Name
                        : null,
                AssignedUserId = t.AssignedUserId,
                AssignedUserFirstName = t.AssignedUser != null ? t.AssignedUser.FirstName : null,
                AssignedUserLastName = t.AssignedUser != null ? t.AssignedUser.LastName : null,
                SubTaskCount = t.SubTasks.Count(st =>
                    st.DeletedAt == null),
                CompletedSubTaskCount = t.SubTasks.Count(st =>
                    st.DeletedAt == null &&
                    st.Status == Models.SubTaskStatuses.Completed),
                OwnProgressPercentage = t.OwnProgressPercentage
            })
            .ToListAsync();

        foreach (var item in items)
            item.ProgressPercentage = CalculateProgress(
                item.CompletedSubTaskCount,
                item.SubTaskCount,
                item.OwnProgressPercentage);

        return new PagedResultDto<TaskItemDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<TaskItemDto?> GetByIdAsync(Guid id)
    {
        await EnsureTaskScopeAsync(id);
        var task = await _db.TaskItems
            .AsNoTracking()
            .Include(t => t.SubProject)
            .ThenInclude(subProject => subProject.Project)
            .ThenInclude(project => project.ClientUser)
            .ThenInclude(clientUser => clientUser!.ClientCompany)
            .Include(t => t.AssignedUser)
            .Include(t => t.SubTasks.Where(st => st.DeletedAt == null))
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.DeletedAt == null &&
                t.SubProject.DeletedAt == null &&
                t.SubProject.Project.DeletedAt == null);

        if (task == null) return null;

        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Detail = task.Detail,
            CreatedAt = task.CreatedAt,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Status = task.Status,
            SubProjectId = task.SubProjectId,
            SubProjectTitle = task.SubProject.Title,
            ProjectId = task.SubProject.ProjectId,
            ProjectTitle = task.SubProject.Project.Title,
            ClientCompanyId = task.SubProject.Project.ClientUser?.ClientCompanyId,
            ClientCompanyName = task.SubProject.Project.ClientUser?.ClientCompany?.Name,
            AssignedUserId = task.AssignedUserId,
            AssignedUserFirstName = task.AssignedUser?.FirstName,
            AssignedUserLastName = task.AssignedUser?.LastName,
            SubTaskCount = task.SubTasks.Count(st =>
                st.DeletedAt == null),
            CompletedSubTaskCount = task.SubTasks.Count(st =>
                st.Status == Models.SubTaskStatuses.Completed),
            ProgressPercentage = CalculateProgress(
                task.SubTasks.Count(st =>
                    st.Status == Models.SubTaskStatuses.Completed),
                task.SubTasks.Count(st => st.DeletedAt == null),
                task.OwnProgressPercentage),
            OwnProgressPercentage = task.OwnProgressPercentage
        };
    }

    public async Task<TaskItemDto> CreateAsync(CreateTaskItemDto dto)
    {
        await EnsureSubProjectScopeAsync(dto.SubProjectId, dto.AssignedUserId);
        await ValidateRelationsAsync(dto.SubProjectId, dto.AssignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            dto.AssignedUserId, Models.TaskItemStatuses.Pending, "tarea");

        var now = DateTime.UtcNow;

        var task = new Models.TaskItem
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = Models.TaskItemStatuses.Pending,
            SubProjectId = dto.SubProjectId,
            AssignedUserId = dto.AssignedUserId,
            OwnProgressPercentage = dto.OwnProgressPercentage,
            LastActivityAt = now,
            ActualStartedAt = null,
            CompletedAt = null
        };

        _db.TaskItems.Add(task);
        _db.TaskStatusHistories.Add(new Models.TaskStatusHistory
        {
            TaskId = task.Id,
            FromStatus = null,
            ToStatus = task.Status,
            Timestamp = now,
            ChangedByUserId = _authorization.UserId
        });
        await _notifications.NotifyAssignment(task.AssignedUserId, _authorization.UserId,
            "task", task.Id, task.Title);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(task.Id))!;
    }

    public async Task<TaskItemDto?> UpdateAsync(Guid id, UpdateTaskItemDto dto)
    {
        await EnsureTaskScopeAsync(id);
        await EnsureSubProjectScopeAsync(dto.SubProjectId, dto.AssignedUserId);
        var task = await _db.TaskItems
            .Include(t => t.SubProject)
            .Include(t => t.AssignedUser)
            .Include(t => t.SubTasks.Where(st => st.DeletedAt == null))
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.DeletedAt == null &&
                t.SubProject.DeletedAt == null &&
                t.SubProject.Project.DeletedAt == null);

        if (task == null) return null;

        await ValidateRelationsAsync(dto.SubProjectId, dto.AssignedUserId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            dto.AssignedUserId, dto.Status, "tarea");

        var previousAssigneeId = task.AssignedUserId;
        task.Title = dto.Title;
        task.Detail = dto.Detail;
        task.StartDate = dto.StartDate;
        task.EndDate = dto.EndDate;
        task.SubProjectId = dto.SubProjectId;
        task.AssignedUserId = dto.AssignedUserId;
        task.OwnProgressPercentage = dto.OwnProgressPercentage;
        task.UpdatedAt = DateTime.UtcNow;
        task.LastActivityAt = task.UpdatedAt;

        if (previousAssigneeId != task.AssignedUserId)
            await _notifications.NotifyAssignment(task.AssignedUserId, _authorization.UserId,
                "task", task.Id, task.Title);

        await _db.SaveChangesAsync();

        return await GetByIdAsync(task.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await EnsureTaskScopeAsync(id);
        var task = await _db.TaskItems
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.DeletedAt == null &&
                t.SubProject.DeletedAt == null &&
                t.SubProject.Project.DeletedAt == null);

        if (task == null) return false;

        task.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<TaskItemDto?> UpdateAssigneeAsync(
        Guid id,
        Guid? assignedUserId)
    {
        await EnsureTaskScopeAsync(id);
        var task = await _db.TaskItems.FirstOrDefaultAsync(item =>
            item.Id == id &&
            item.DeletedAt == null &&
            item.SubProject.DeletedAt == null &&
            item.SubProject.Project.DeletedAt == null);

        if (task == null)
            return null;

        await _workAssignment.ValidateAssigneeAsync(assignedUserId);
        var departmentId = await _db.TaskItems.Where(t => t.Id == id).Select(t => t.SubProject.DepartmentId).SingleAsync();
        await _authorization.EnsureAssigneeAsync(assignedUserId, departmentId);
        WorkAssignmentService.EnsureUnassignedWorkIsPending(
            assignedUserId, task.Status, "tarea");

        var previousAssigneeId = task.AssignedUserId;
        task.AssignedUserId = assignedUserId;
        task.UpdatedAt = DateTime.UtcNow;
        task.LastActivityAt = task.UpdatedAt;
        if (previousAssigneeId != assignedUserId)
            await _notifications.NotifyAssignment(assignedUserId, _authorization.UserId,
                "task", task.Id, task.Title);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(task.Id);
    }

    private async Task ValidateRelationsAsync(
        Guid subProjectId,
        Guid? assignedUserId)
    {
        var subProjectExists = await _db.SubProjects.AnyAsync(sp =>
            sp.Id == subProjectId &&
            sp.DeletedAt == null &&
            sp.Project.DeletedAt == null);

        if (!subProjectExists)
            throw new BusinessValidationException(
                "El subproyecto indicado no existe.");

        await _workAssignment.ValidateAssigneeAsync(assignedUserId);
    }

    private async Task EnsureTaskScopeAsync(Guid id)
    {
        var scope = await _db.TaskItems.Where(t => t.Id == id && t.DeletedAt == null)
            .Select(t => new { t.SubProject.ProjectId, t.SubProject.DepartmentId }).FirstOrDefaultAsync();
        if (scope != null) await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
    }

    private async Task EnsureSubProjectScopeAsync(Guid subProjectId, Guid? assignedUserId)
    {
        var scope = await _db.SubProjects.Where(s => s.Id == subProjectId && s.DeletedAt == null)
            .Select(s => new { s.ProjectId, s.DepartmentId }).FirstOrDefaultAsync();
        if (scope == null) return;
        await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
        await _authorization.EnsureAssigneeAsync(assignedUserId, scope.DepartmentId);
    }

    private static int CalculateProgress(int completed, int total, int? ownProgress = null)
    {
        if (total == 0)
            return ownProgress ?? 0;

        return (int)Math.Round(completed * 100.0 / total);
    }
}
