using AgencyFlow.Data;
using AgencyFlow.Authorization;
using AgencyFlow.DTOs.Productivity;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public class ProductivityService(AppDbContext db, ResourceAuthorizationService authorization)
{
    public async Task<DelayReportDto> GetDelayReportAsync(
        DelayReportFilterDto? filter = null)
    {
        filter ??= new DelayReportFilterDto();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var projectsQuery = db.Projects
            .AsNoTracking()
            .Where(project => project.DeletedAt == null);
        projectsQuery = authorization.IsOperationalUser
            ? projectsQuery.Where(project => project.SubProjects.Any(sp =>
                sp.TaskItems.Any(task => task.AssignedUserId == authorization.UserId ||
                    task.SubTasks.Any(sub => sub.AssignedUserId == authorization.UserId))))
            : authorization.FilterProjects(projectsQuery);
        var projects = await projectsQuery.Where(project =>
                (!filter.ProjectId.HasValue || project.Id == filter.ProjectId) &&
                (!filter.ClientCompanyId.HasValue ||
                    project.ClientUser != null && project.ClientUser.ClientCompanyId == filter.ClientCompanyId) &&
                (!filter.DateFrom.HasValue || project.EndDate >= filter.DateFrom) &&
                (!filter.DateTo.HasValue || project.StartDate <= filter.DateTo))
            .Select(project => new ProjectRow(
                project.Id,
                project.Title,
                project.StartDate,
                project.EndDate,
                project.UpdatedAt, project.ActualStartedAt, project.CompletedAt,
                project.ClientUser != null ? project.ClientUser.ClientCompanyId : null,
                project.ClientUser != null && project.ClientUser.ClientCompany != null ? project.ClientUser.ClientCompany.Name : null))
            .ToListAsync();

        var projectIds = projects.Select(project => project.Id).ToHashSet();
        var subProjectsQuery = db.SubProjects
            .AsNoTracking()
            .Where(subProject => subProject.DeletedAt == null &&
                projectIds.Contains(subProject.ProjectId));
        subProjectsQuery = authorization.IsOperationalUser
            ? subProjectsQuery.Where(sp => sp.TaskItems.Any(task =>
                task.AssignedUserId == authorization.UserId || task.SubTasks.Any(sub => sub.AssignedUserId == authorization.UserId)))
            : authorization.FilterSubProjects(subProjectsQuery);
        var subProjects = await subProjectsQuery.Where(subProject =>
                projectIds.Contains(subProject.ProjectId) &&
                (!filter.SubProjectId.HasValue || subProject.Id == filter.SubProjectId) &&
                (!filter.DepartmentId.HasValue || subProject.DepartmentId == filter.DepartmentId) &&
                (!filter.ResponsibleUserId.HasValue ||
                    subProject.AssignedUserId == filter.ResponsibleUserId))
            .Select(subProject => new SubProjectRow(
                subProject.Id,
                subProject.ProjectId,
                subProject.Title,
                subProject.Status,
                subProject.StartDate,
                subProject.EndDate,
                subProject.UpdatedAt,
                subProject.ActualStartedAt, subProject.CompletedAt,
                subProject.DepartmentId, subProject.Department != null ? subProject.Department.Name : null,
                subProject.AssignedUserId,
                BuildName(subProject.AssignedUser!.FirstName,
                    subProject.AssignedUser.LastName)))
            .ToListAsync();

        var subProjectIds = subProjects.Select(item => item.Id).ToHashSet();
        var tasksQuery = authorization.FilterTasks(db.TaskItems.AsNoTracking().Where(task => task.DeletedAt == null));
        var tasks = await tasksQuery
            .AsNoTracking()
            .Where(task =>
                task.DeletedAt == null &&
                subProjectIds.Contains(task.SubProjectId) &&
                (!filter.ResponsibleUserId.HasValue ||
                    task.AssignedUserId == filter.ResponsibleUserId))
            .Select(task => new TaskRow(
                task.Id,
                task.SubProjectId,
                task.SubProject.ProjectId,
                task.Title,
                task.Status,
                task.StartDate,
                task.EndDate,
                task.UpdatedAt,
                task.ActualStartedAt, task.CompletedAt,
                task.AssignedUserId,
                BuildName(task.AssignedUser!.FirstName,
                    task.AssignedUser.LastName)))
            .ToListAsync();

        var taskIds = tasks.Select(item => item.Id).ToHashSet();
        var subTasksQuery = authorization.FilterSubTasks(db.SubTasks.AsNoTracking().Where(subTask => subTask.DeletedAt == null));
        var subTasks = await subTasksQuery
            .AsNoTracking()
            .Where(subTask =>
                subTask.DeletedAt == null &&
                taskIds.Contains(subTask.TaskItemId) &&
                (!filter.ResponsibleUserId.HasValue ||
                    subTask.AssignedUserId == filter.ResponsibleUserId))
            .Select(subTask => new SubTaskRow(
                subTask.Id,
                subTask.TaskItemId,
                subTask.TaskItem.SubProjectId,
                subTask.TaskItem.SubProject.ProjectId,
                subTask.Title,
                subTask.Status,
                subTask.StartDate,
                subTask.EndDate,
                subTask.UpdatedAt,
                subTask.ActualStartedAt, subTask.CompletedAt,
                subTask.AssignedUserId,
                BuildName(subTask.AssignedUser!.FirstName,
                    subTask.AssignedUser.LastName)))
            .ToListAsync();

        var taskCompletionDates = await db.TaskStatusHistories
            .AsNoTracking()
            .Where(history =>
                taskIds.Contains(history.TaskId) &&
                (history.ToStatus == TaskItemStatuses.Completed ||
                 history.ToStatus == "Completada"))
            .GroupBy(history => history.TaskId)
            .ToDictionaryAsync(
                group => group.Key,
                group => (DateTime?)group.Max(history => history.Timestamp));

        var subTaskIds = subTasks.Select(item => item.Id).ToHashSet();
        var subTaskCompletionDates = await db.SubTaskStatusHistories
            .AsNoTracking()
            .Where(history =>
                subTaskIds.Contains(history.SubTaskId) &&
                (history.ToStatus == SubTaskStatuses.Completed ||
                 history.ToStatus == "Completado"))
            .GroupBy(history => history.SubTaskId)
            .ToDictionaryAsync(
                group => group.Key,
                group => (DateTime?)group.Max(history => history.Timestamp));

        var taskHistoryRows = await db.TaskStatusHistories.AsNoTracking()
            .Where(history => taskIds.Contains(history.TaskId))
            .OrderBy(history => history.Timestamp).ToListAsync();
        var subTaskHistoryRows = await db.SubTaskStatusHistories.AsNoTracking()
            .Where(history => subTaskIds.Contains(history.SubTaskId))
            .OrderBy(history => history.Timestamp).ToListAsync();
        var subProjectHistoryRows = await db.SubProjectStatusHistories.AsNoTracking()
            .Where(history => subProjectIds.Contains(history.SubProjectId))
            .OrderBy(history => history.Timestamp).ToListAsync();
        var projectHistoryRows = await db.ProjectStatusHistories.AsNoTracking()
            .Where(history => projectIds.Contains(history.ProjectId))
            .OrderBy(history => history.Timestamp).ToListAsync();

        var items = new List<DelayItemDto>();
        foreach (var subTask in subTasks)
        {
            var isCompleted = IsCompleted(subTask.Status);
            var completedAt = isCompleted
                ? subTask.CompletedAt ?? subTaskCompletionDates.GetValueOrDefault(subTask.Id) ?? subTask.UpdatedAt
                : null;
            items.Add(BuildItem("SubTask", subTask.Id, subTask.TaskId,
                subTask.ProjectId, subTask.SubProjectId, subTask.TaskId,
                subTask.Title, subTask.Status, subTask.ResponsibleUserId,
                subTask.ResponsibleUserName, subTask.StartDate, subTask.EndDate,
                isCompleted, subTask.ActualStartedAt, completedAt, today,
                subTask.CompletedAt.HasValue ? "TrackedTimestamp" : subTaskCompletionDates.ContainsKey(subTask.Id)
                    ? "StatusHistory"
                    : isCompleted ? "UpdatedAt" : "CurrentDate"));
        }

        foreach (var task in tasks)
        {
            var isCompleted = IsCompleted(task.Status);
            var completedAt = isCompleted
                ? task.CompletedAt ?? taskCompletionDates.GetValueOrDefault(task.Id) ?? task.UpdatedAt
                : null;
            items.Add(BuildItem("Task", task.Id, task.SubProjectId,
                task.ProjectId, task.SubProjectId, task.Id, task.Title,
                task.Status, task.ResponsibleUserId, task.ResponsibleUserName,
                task.StartDate, task.EndDate, isCompleted, task.ActualStartedAt, completedAt, today,
                task.CompletedAt.HasValue ? "TrackedTimestamp" : taskCompletionDates.ContainsKey(task.Id)
                    ? "StatusHistory"
                    : isCompleted ? "UpdatedAt" : "CurrentDate"));
        }

        foreach (var subProject in subProjects)
        {
            var childTasks = tasks.Where(task =>
                task.SubProjectId == subProject.Id).ToList();
            var isCompleted = IsCompleted(subProject.Status);
            var completedAt = isCompleted
                ? subProject.CompletedAt ?? childTasks
                    .Select(task => taskCompletionDates.GetValueOrDefault(task.Id))
                    .Where(date => date.HasValue)
                    .Max() ?? subProject.UpdatedAt
                : null;
            items.Add(BuildItem("SubProject", subProject.Id,
                subProject.ProjectId, subProject.ProjectId, subProject.Id, null,
                subProject.Title, subProject.Status,
                subProject.ResponsibleUserId, subProject.ResponsibleUserName,
                subProject.StartDate, subProject.EndDate, isCompleted,
                subProject.ActualStartedAt, completedAt, today, completedAt.HasValue && childTasks.Any()
                    ? "ChildTasks"
                    : isCompleted ? "UpdatedAt" : "CurrentDate"));
        }

        foreach (var project in projects)
        {
            var children = subProjects.Where(subProject =>
                subProject.ProjectId == project.Id).ToList();
            var isCompleted = children.Count > 0 && children.All(child =>
                IsCompleted(child.Status));
            var completedAt = isCompleted
                ? project.CompletedAt ?? children.Select(child => child.CompletedAt ?? child.UpdatedAt)
                    .Where(date => date.HasValue).Max() ?? project.UpdatedAt
                : null;
            items.Add(BuildItem("Project", project.Id, null, project.Id,
                null, null, project.Title,
                isCompleted ? "Completado" : "En curso", null, null,
                project.StartDate, project.EndDate, isCompleted, project.ActualStartedAt, completedAt,
                today, isCompleted ? "ChildSubProjects" : "CurrentDate"));
        }

        foreach (var item in items)
        {
            var projectInfo = projects.First(project => project.Id == item.ProjectId);
            var subProjectInfo = item.SubProjectId.HasValue
                ? subProjects.FirstOrDefault(subProject => subProject.Id == item.SubProjectId)
                : null;
            item.ProjectName = projectInfo.Title;
            item.ClientCompanyId = projectInfo.ClientCompanyId;
            item.ClientCompanyName = projectInfo.ClientCompanyName;
            item.DepartmentId = subProjectInfo?.DepartmentId;
            item.DepartmentName = subProjectInfo?.DepartmentName;
            var transitions = item.Level switch
            {
                "Task" => taskHistoryRows.Where(row => row.TaskId == item.Id)
                    .Select(row => (row.ToStatus, row.Timestamp)),
                "SubTask" => subTaskHistoryRows.Where(row => row.SubTaskId == item.Id)
                    .Select(row => (row.ToStatus, row.Timestamp)),
                "SubProject" => subProjectHistoryRows.Where(row => row.SubProjectId == item.Id)
                    .Select(row => (row.ToStatus, row.Timestamp)),
                "Project" => projectHistoryRows.Where(row => row.ProjectId == item.Id)
                    .Select(row => (row.ToStatus, row.Timestamp)),
                _ => Enumerable.Empty<(string, DateTime)>()
            };
            item.TimeByStatusHours = CalculateTimeByStatus(transitions, item.CompletedAt ?? now);
        }

        if (!string.IsNullOrWhiteSpace(filter.Level))
            items = items.Where(item => string.Equals(item.Level, filter.Level,
                StringComparison.OrdinalIgnoreCase)).ToList();
        if (filter.OnlyDelayed)
            items = items.Where(item => item.IsDelayed).ToList();

        items = items
            .OrderByDescending(item => item.DelayDays ?? -1)
            .ThenBy(item => item.Level)
            .ThenBy(item => item.Title)
            .ToList();
        var measurable = items.Where(item => item.DelayDays.HasValue).ToList();
        var delayed = measurable.Where(item => item.DelayDays > 0).ToList();
        var completed = items.Count(item => item.IsCompleted);

        return new DelayReportDto
        {
            GeneratedAt = now,
            Items = items,
            Summary = new DelaySummaryDto
            {
                TotalItemCount = items.Count,
                CompletedItemCount = completed,
                DelayedItemCount = delayed.Count,
                AverageDelayDays = delayed.Count == 0
                    ? 0
                    : Math.Round(delayed.Average(item => item.DelayDays!.Value), 1),
                MaximumDelayDays = delayed.Count == 0
                    ? 0
                    : delayed.Max(item => item.DelayDays!.Value),
                OnTimePercentage = measurable.Count == 0
                    ? 0
                    : Math.Round(measurable.Count(item => item.DelayDays == 0) *
                        100.0 / measurable.Count, 1),
                AverageCycleDays = completed == 0 ? 0 : Math.Round(items
                    .Where(item => item.IsCompleted && item.ActualDurationHours.HasValue)
                    .Select(item => item.ActualDurationHours!.Value / 24).DefaultIfEmpty(0).Average(), 1),
                CurrentlyOverdueCount = items.Count(item => !item.IsCompleted && item.IsDelayed)
            }
        };
    }

    private static DelayItemDto BuildItem(
        string level, Guid id, Guid? parentId, Guid projectId,
        Guid? subProjectId, Guid? taskId, string title, string status,
        Guid? responsibleUserId, string? responsibleUserName,
        DateOnly startDate, DateOnly? endDate, bool isCompleted,
        DateTime? actualStartedAt, DateTime? completedAt, DateOnly today, string completionSource)
    {
        var referenceDate = completedAt.HasValue
            ? DateOnly.FromDateTime(completedAt.Value)
            : today;
        int? delayDays = endDate.HasValue
            ? Math.Max(0, referenceDate.DayNumber - endDate.Value.DayNumber)
            : null;
        return new DelayItemDto
        {
            Level = level,
            Id = id,
            ParentId = parentId,
            ProjectId = projectId,
            SubProjectId = subProjectId,
            TaskId = taskId,
            Title = title,
            Status = status,
            ResponsibleUserId = responsibleUserId,
            ResponsibleUserName = responsibleUserName,
            PlannedStartDate = startDate,
            PlannedEndDate = endDate,
            CompletedAt = completedAt,
            ActualStartedAt = actualStartedAt,
            IsCompleted = isCompleted,
            IsDelayed = delayDays > 0,
            DelayDays = delayDays,
            PlannedDurationDays = endDate.HasValue
                ? Math.Max(0, endDate.Value.DayNumber - startDate.DayNumber)
                : null,
            ActualDurationDays = Math.Max(0,
                referenceDate.DayNumber - DateOnly.FromDateTime(actualStartedAt ?? startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).DayNumber),
            ActualDurationHours = Math.Round(((completedAt ?? DateTime.UtcNow) -
                (actualStartedAt ?? startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))).TotalHours, 1),
            StartDelayDays = actualStartedAt.HasValue
                ? Math.Max(0, DateOnly.FromDateTime(actualStartedAt.Value).DayNumber - startDate.DayNumber)
                : null,
            DataQuality = completionSource.StartsWith("Child", StringComparison.Ordinal) ||
                          !actualStartedAt.HasValue || (isCompleted && !completedAt.HasValue)
                ? "Estimated"
                : "Exact",
            CompletionSource = completionSource
        };
    }

    private static bool IsCompleted(string status) =>
        status is "Completado" or "Completada";

    private static Dictionary<string, double> CalculateTimeByStatus(
        IEnumerable<(string Status, DateTime At)> source, DateTime until)
    {
        var transitions = source.OrderBy(item => item.At).ToList();
        var result = new Dictionary<string, double>();
        for (var index = 0; index < transitions.Count; index++)
        {
            var current = transitions[index];
            var end = index + 1 < transitions.Count ? transitions[index + 1].At : until;
            var hours = Math.Max(0, (end - current.At).TotalHours);
            result[current.Status] = Math.Round(result.GetValueOrDefault(current.Status) + hours, 1);
        }
        return result;
    }

    private static string? BuildName(string? firstName, string? lastName)
    {
        var name = string.Join(" ", new[] { firstName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private sealed record ProjectRow(Guid Id, string Title,
        DateOnly StartDate, DateOnly? EndDate, DateTime? UpdatedAt,
        DateTime? ActualStartedAt, DateTime? CompletedAt,
        Guid? ClientCompanyId, string? ClientCompanyName);
    private sealed record SubProjectRow(Guid Id, Guid ProjectId, string Title,
        string Status, DateOnly StartDate, DateOnly? EndDate,
        DateTime? UpdatedAt, DateTime? ActualStartedAt, DateTime? CompletedAt,
        Guid? DepartmentId, string? DepartmentName, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
    private sealed record TaskRow(Guid Id, Guid SubProjectId, Guid ProjectId,
        string Title, string Status, DateOnly StartDate, DateOnly? EndDate,
        DateTime? UpdatedAt, DateTime? ActualStartedAt, DateTime? CompletedAt, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
    private sealed record SubTaskRow(Guid Id, Guid TaskId, Guid SubProjectId,
        Guid ProjectId, string Title, string Status, DateOnly StartDate,
        DateOnly? EndDate, DateTime? UpdatedAt, DateTime? ActualStartedAt, DateTime? CompletedAt, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
}
