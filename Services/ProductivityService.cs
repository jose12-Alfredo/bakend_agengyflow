using AgencyFlow.Data;
using AgencyFlow.DTOs.Productivity;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public class ProductivityService(AppDbContext db)
{
    public async Task<DelayReportDto> GetDelayReportAsync(
        DelayReportFilterDto? filter = null)
    {
        filter ??= new DelayReportFilterDto();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var projects = await db.Projects
            .AsNoTracking()
            .Where(project =>
                project.DeletedAt == null &&
                (!filter.ProjectId.HasValue || project.Id == filter.ProjectId) &&
                (!filter.ClientCompanyId.HasValue ||
                    project.ClientUser != null &&
                    project.ClientUser.ClientCompanyId == filter.ClientCompanyId))
            .Select(project => new ProjectRow(
                project.Id,
                project.Title,
                project.StartDate,
                project.EndDate,
                project.UpdatedAt))
            .ToListAsync();

        var projectIds = projects.Select(project => project.Id).ToHashSet();
        var subProjects = await db.SubProjects
            .AsNoTracking()
            .Where(subProject =>
                subProject.DeletedAt == null &&
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
                subProject.AssignedUserId,
                BuildName(subProject.AssignedUser!.FirstName,
                    subProject.AssignedUser.LastName)))
            .ToListAsync();

        var subProjectIds = subProjects.Select(item => item.Id).ToHashSet();
        var tasks = await db.TaskItems
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
                task.AssignedUserId,
                BuildName(task.AssignedUser!.FirstName,
                    task.AssignedUser.LastName)))
            .ToListAsync();

        var taskIds = tasks.Select(item => item.Id).ToHashSet();
        var subTasks = await db.SubTasks
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

        var items = new List<DelayItemDto>();
        foreach (var subTask in subTasks)
        {
            var isCompleted = IsCompleted(subTask.Status);
            var completedAt = isCompleted
                ? subTaskCompletionDates.GetValueOrDefault(subTask.Id) ?? subTask.UpdatedAt
                : null;
            items.Add(BuildItem("SubTask", subTask.Id, subTask.TaskId,
                subTask.ProjectId, subTask.SubProjectId, subTask.TaskId,
                subTask.Title, subTask.Status, subTask.ResponsibleUserId,
                subTask.ResponsibleUserName, subTask.StartDate, subTask.EndDate,
                isCompleted, completedAt, today,
                subTaskCompletionDates.ContainsKey(subTask.Id)
                    ? "StatusHistory"
                    : isCompleted ? "UpdatedAt" : "CurrentDate"));
        }

        foreach (var task in tasks)
        {
            var isCompleted = IsCompleted(task.Status);
            var completedAt = isCompleted
                ? taskCompletionDates.GetValueOrDefault(task.Id) ?? task.UpdatedAt
                : null;
            items.Add(BuildItem("Task", task.Id, task.SubProjectId,
                task.ProjectId, task.SubProjectId, task.Id, task.Title,
                task.Status, task.ResponsibleUserId, task.ResponsibleUserName,
                task.StartDate, task.EndDate, isCompleted, completedAt, today,
                taskCompletionDates.ContainsKey(task.Id)
                    ? "StatusHistory"
                    : isCompleted ? "UpdatedAt" : "CurrentDate"));
        }

        foreach (var subProject in subProjects)
        {
            var childTasks = tasks.Where(task =>
                task.SubProjectId == subProject.Id).ToList();
            var isCompleted = IsCompleted(subProject.Status);
            var completedAt = isCompleted
                ? childTasks
                    .Select(task => taskCompletionDates.GetValueOrDefault(task.Id))
                    .Where(date => date.HasValue)
                    .Max() ?? subProject.UpdatedAt
                : null;
            items.Add(BuildItem("SubProject", subProject.Id,
                subProject.ProjectId, subProject.ProjectId, subProject.Id, null,
                subProject.Title, subProject.Status,
                subProject.ResponsibleUserId, subProject.ResponsibleUserName,
                subProject.StartDate, subProject.EndDate, isCompleted,
                completedAt, today, completedAt.HasValue && childTasks.Any()
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
                ? children.Select(child => child.UpdatedAt)
                    .Where(date => date.HasValue).Max() ?? project.UpdatedAt
                : null;
            items.Add(BuildItem("Project", project.Id, null, project.Id,
                null, null, project.Title,
                isCompleted ? "Completado" : "En curso", null, null,
                project.StartDate, project.EndDate, isCompleted, completedAt,
                today, isCompleted ? "ChildSubProjects" : "CurrentDate"));
        }

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
                        100.0 / measurable.Count, 1)
            }
        };
    }

    private static DelayItemDto BuildItem(
        string level, Guid id, Guid? parentId, Guid projectId,
        Guid? subProjectId, Guid? taskId, string title, string status,
        Guid? responsibleUserId, string? responsibleUserName,
        DateOnly startDate, DateOnly? endDate, bool isCompleted,
        DateTime? completedAt, DateOnly today, string completionSource)
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
            IsCompleted = isCompleted,
            IsDelayed = delayDays > 0,
            DelayDays = delayDays,
            PlannedDurationDays = endDate.HasValue
                ? Math.Max(0, endDate.Value.DayNumber - startDate.DayNumber)
                : null,
            ActualDurationDays = Math.Max(0,
                referenceDate.DayNumber - startDate.DayNumber),
            CompletionSource = completionSource
        };
    }

    private static bool IsCompleted(string status) =>
        status is "Completado" or "Completada";

    private static string? BuildName(string? firstName, string? lastName)
    {
        var name = string.Join(" ", new[] { firstName, lastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private sealed record ProjectRow(Guid Id, string Title,
        DateOnly StartDate, DateOnly? EndDate, DateTime? UpdatedAt);
    private sealed record SubProjectRow(Guid Id, Guid ProjectId, string Title,
        string Status, DateOnly StartDate, DateOnly? EndDate,
        DateTime? UpdatedAt, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
    private sealed record TaskRow(Guid Id, Guid SubProjectId, Guid ProjectId,
        string Title, string Status, DateOnly StartDate, DateOnly? EndDate,
        DateTime? UpdatedAt, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
    private sealed record SubTaskRow(Guid Id, Guid TaskId, Guid SubProjectId,
        Guid ProjectId, string Title, string Status, DateOnly StartDate,
        DateOnly? EndDate, DateTime? UpdatedAt, Guid? ResponsibleUserId,
        string? ResponsibleUserName);
}
