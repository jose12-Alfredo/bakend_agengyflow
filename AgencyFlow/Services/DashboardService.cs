using AgencyFlow.Data;
using AgencyFlow.DTOs.Dashboard;
using AgencyFlow.Models;
using AgencyFlow.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public class DashboardService(
    AppDbContext db,
    ManagementDashboardService managementDashboardService,
    ResourceAuthorizationService authorization)
{
    private const int UpcomingDeadlineDays = 5;

    public async Task<DashboardDto> GetAsync(DashboardFilterDto? filter = null)
    {
        filter ??= new DashboardFilterDto();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var tasksQuery = db.SubTasks
            .AsNoTracking()
            .Where(task =>
                task.DeletedAt == null &&
                task.TaskItem.DeletedAt == null &&
                task.TaskItem.SubProject.DeletedAt == null &&
                task.TaskItem.SubProject.Project.DeletedAt == null);
        var tasks = await authorization.FilterSubTasks(tasksQuery)
            .Select(task => new TaskSnapshot
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                ParentTaskId = task.TaskItemId,
                ParentTaskTitle = task.TaskItem.Title,
                SubProjectId = task.TaskItem.SubProjectId,
                SubProjectTitle = task.TaskItem.SubProject.Title,
                ProjectId = task.TaskItem.SubProject.ProjectId,
                ProjectTitle = task.TaskItem.SubProject.Project.Title,
                ClientCompanyId = task.TaskItem.SubProject.Project.ClientUser != null
                    ? task.TaskItem.SubProject.Project.ClientUser.ClientCompanyId
                    : null,
                ClientCompanyName = task.TaskItem.SubProject.Project.ClientUser != null &&
                    task.TaskItem.SubProject.Project.ClientUser.ClientCompany != null
                        ? task.TaskItem.SubProject.Project.ClientUser!.ClientCompany!.Name
                        : null,
                DepartmentId = task.TaskItem.SubProject.DepartmentId,
                AssignedUserId = task.AssignedUserId,
                AssignedUserFirstName = task.AssignedUser != null
                    ? task.AssignedUser.FirstName
                    : null,
                AssignedUserLastName = task.AssignedUser != null
                    ? task.AssignedUser.LastName
                    : null
            })
            .ToListAsync();

        var parentTasksQuery = db.TaskItems
            .AsNoTracking()
            .Where(task =>
                task.DeletedAt == null &&
                task.SubProject.DeletedAt == null &&
                task.SubProject.Project.DeletedAt == null);
        var parentTasks = await authorization.FilterTasks(parentTasksQuery)
            .Select(task => new ParentTaskSnapshot
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                SubProjectId = task.SubProjectId,
                SubProjectTitle = task.SubProject.Title,
                ProjectId = task.SubProject.ProjectId,
                ProjectTitle = task.SubProject.Project.Title,
                ClientCompanyId = task.SubProject.Project.ClientUser != null
                    ? task.SubProject.Project.ClientUser.ClientCompanyId
                    : null,
                ClientCompanyName = task.SubProject.Project.ClientUser != null &&
                    task.SubProject.Project.ClientUser.ClientCompany != null
                        ? task.SubProject.Project.ClientUser!.ClientCompany!.Name
                        : null,
                DepartmentId = task.SubProject.DepartmentId,
                AssignedUserId = task.AssignedUserId,
                AssignedUserFirstName = task.AssignedUser != null
                    ? task.AssignedUser.FirstName
                    : null,
                AssignedUserLastName = task.AssignedUser != null
                    ? task.AssignedUser.LastName
                    : null,
                SubTaskCount = task.SubTasks.Count(subTask =>
                    subTask.DeletedAt == null),
                CompletedSubTaskCount = task.SubTasks.Count(subTask =>
                    subTask.DeletedAt == null &&
                    subTask.Status == SubTaskStatuses.Completed)
            })
            .ToListAsync();

        var teamUsersQuery = db.Users
            .AsNoTracking()
            .Where(user =>
                user.DeletedAt == null &&
                user.Role.DeletedAt == null);
        if (authorization.IsDirector)
        {
            var directorUserId = authorization.UserId;
            teamUsersQuery = teamUsersQuery.Where(user => user.DepartmentUsers.Any(membership =>
                membership.DeletedAt == null &&
                db.DepartmentUsers.Any(directorMembership =>
                    directorMembership.UserId == directorUserId &&
                    directorMembership.DepartmentId == membership.DepartmentId &&
                    directorMembership.IsDirector &&
                    directorMembership.DeletedAt == null)));
        }
        var teamUsers = await teamUsersQuery
            .Select(user => new TeamUserSnapshot
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = user.Role.Name,
                DepartmentIds = user.DepartmentUsers
                    .Where(departmentUser => departmentUser.DeletedAt == null)
                    .Select(departmentUser => departmentUser.DepartmentId)
                    .ToList()
            })
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync();

        var assignedUserIds = parentTasks
            .Where(task => task.AssignedUserId.HasValue)
            .Select(task => task.AssignedUserId!.Value)
            .Concat(tasks
                .Where(task => task.AssignedUserId.HasValue)
                .Select(task => task.AssignedUserId!.Value))
            .ToHashSet();

        teamUsers = teamUsers
            .Where(user =>
                user.RoleName != "Cliente" ||
                assignedUserIds.Contains(user.Id))
            .ToList();

        var subTaskHistoriesQuery = db.SubTaskStatusHistories
            .AsNoTracking()
            .Where(history =>
                history.SubTask.DeletedAt == null &&
                history.SubTask.TaskItem.DeletedAt == null &&
                history.SubTask.TaskItem.SubProject.DeletedAt == null &&
                history.SubTask.TaskItem.SubProject.Project.DeletedAt == null);
        var subTaskHistories = await authorization
            .FilterSubTaskHistories(subTaskHistoriesQuery)
            .OrderBy(history => history.Timestamp)
            .ToListAsync();

        var taskHistoriesQuery = db.TaskStatusHistories
            .AsNoTracking()
            .Where(history =>
                history.Task.DeletedAt == null &&
                history.Task.SubProject.DeletedAt == null &&
                history.Task.SubProject.Project.DeletedAt == null);
        var taskHistories = await authorization
            .FilterTaskHistories(taskHistoriesQuery)
            .OrderBy(history => history.Timestamp)
            .ToListAsync();

        var activeSubProjectsQuery = db.SubProjects
            .AsNoTracking()
            .Where(subProject =>
                subProject.DeletedAt == null &&
                subProject.Project.DeletedAt == null &&
                subProject.Status != "Completado" &&
                subProject.Status != SubTaskStatuses.Completed);
        var activeSubProjects = await authorization.FilterSubProjects(activeSubProjectsQuery)
            .Select(subProject => new SubProjectSnapshot
            {
                Id = subProject.Id,
                Title = subProject.Title,
                ProjectId = subProject.ProjectId,
                ProjectTitle = subProject.Project.Title,
                ClientCompanyId = subProject.Project.ClientUser != null
                    ? subProject.Project.ClientUser.ClientCompanyId
                    : null,
                DepartmentId = subProject.DepartmentId,
                StartDate = subProject.StartDate,
                EndDate = subProject.EndDate
            })
            .ToListAsync();

        var filteredSubTasks = tasks
            .Where(task => MatchesFilter(
                task.ClientCompanyId,
                task.ProjectId,
                task.SubProjectId,
                task.DepartmentId,
                task.AssignedUserId,
                filter))
            .ToList();
        var filteredParentTasks = parentTasks
            .Where(task => MatchesFilter(
                task.ClientCompanyId,
                task.ProjectId,
                task.SubProjectId,
                task.DepartmentId,
                task.AssignedUserId,
                filter))
            .ToList();

        var totalTasks = filteredSubTasks.Count;
        var completedTasks = filteredSubTasks.Count(task =>
            task.Status == SubTaskStatuses.Completed);
        var overdueTasks = filteredParentTasks.Count(task =>
            task.EndDate.HasValue &&
            task.EndDate.Value < today &&
            !IsCompletedTaskStatus(task.Status));
        var overdueSubTasks = filteredSubTasks.Count(task =>
            task.EndDate.HasValue &&
            task.EndDate.Value < today &&
            task.Status != SubTaskStatuses.Completed);
        var bottlenecks = filteredSubTasks.Count(task =>
            task.Status == SubTaskStatuses.InReview ||
            task.Status == SubTaskStatuses.Paused);

        var historyBySubTask = subTaskHistories
            .GroupBy(history => history.SubTaskId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(history => history.Timestamp).ToList());

        var lastSubTaskStatusChange = historyBySubTask.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Max(history => history.Timestamp));

        var filteredSubTaskIds = filteredSubTasks
            .Select(task => task.Id)
            .ToHashSet();
        var filteredHistoryBySubTask = historyBySubTask
            .Where(pair => filteredSubTaskIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var historyByTask = taskHistories
            .GroupBy(history => history.TaskId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(history => history.Timestamp).ToList());

        var lastTaskStatusChange = historyByTask.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Max(history => history.Timestamp));

        var filteredActiveSubProjects = activeSubProjects
            .Where(subProject => MatchesHierarchyFilter(subProject, filter))
            .Where(subProject =>
                !filter.ResponsibleUserId.HasValue ||
                filteredSubTasks.Any(task => task.SubProjectId == subProject.Id))
            .ToList();
        var atRiskSubProjects = filteredActiveSubProjects
            .Where(subProject => IsSubProjectAtRisk(
                subProject,
                filteredSubTasks,
                today))
            .ToList();
        var availablePersonCount = teamUsers
            .Where(user =>
                !filter.ResponsibleUserId.HasValue ||
                user.Id == filter.ResponsibleUserId.Value)
            .Where(user =>
                !filter.DepartmentId.HasValue ||
                user.DepartmentIds.Contains(filter.DepartmentId.Value))
            .Count(user =>
                !parentTasks.Any(task =>
                    task.AssignedUserId == user.Id &&
                    !IsCompletedTaskStatus(task.Status)) &&
                !tasks.Any(task =>
                    task.AssignedUserId == user.Id &&
                    task.Status != SubTaskStatuses.Completed));

        var result = new DashboardDto
        {
            GeneratedAt = now,
            PausedTaskCount = tasks.Count(task =>
                task.Status == SubTaskStatuses.Paused),
            Kpis = new DashboardKpisDto
            {
                OverdueTaskCount = overdueTasks,
                OverdueSubTaskCount = overdueSubTasks,
                AverageCycleTimeDays = CalculateAverageCycleTime(
                    filteredHistoryBySubTask),
                CompletionRate = totalTasks == 0
                    ? 0
                    : Math.Round(completedTasks * 100.0 / totalTasks, 1),
                CompletedSubTaskCount = completedTasks,
                TotalSubTaskCount = totalTasks,
                BottleneckCount = bottlenecks,
                AtRiskProjectCount = atRiskSubProjects
                    .Select(subProject => subProject.ProjectId)
                    .Distinct()
                    .Count(),
                AtRiskSubProjectCount = atRiskSubProjects.Count,
                AvailablePersonCount = availablePersonCount
            }
        };

        result.OverdueTasks = filteredParentTasks
            .Where(task =>
                task.EndDate.HasValue &&
                task.EndDate.Value < today &&
                !IsCompletedTaskStatus(task.Status))
            .OrderByDescending(task =>
                today.DayNumber - task.EndDate!.Value.DayNumber)
            .ThenBy(task => task.Title)
            .Select(task => new DashboardRiskItemDto
            {
                ItemId = task.Id,
                ParentTaskId = task.Id,
                SubProjectId = task.SubProjectId,
                Title = task.Title,
                ParentTaskTitle = task.Title,
                SubProjectTitle = task.SubProjectTitle,
                ProjectTitle = task.ProjectTitle,
                ClientCompanyName = task.ClientCompanyName,
                AssignedUserName = BuildUserNameOrNull(
                    task.AssignedUserFirstName,
                    task.AssignedUserLastName),
                Status = task.Status,
                DueDate = task.EndDate,
                DaysOverdue = today.DayNumber - task.EndDate!.Value.DayNumber
            })
            .ToList();

        result.BottleneckItems = filteredSubTasks
            .Where(task =>
                task.Status == SubTaskStatuses.InReview ||
                task.Status == SubTaskStatuses.Paused)
            .OrderByDescending(task => task.Status == SubTaskStatuses.Paused)
            .ThenBy(task => task.EndDate ?? DateOnly.MaxValue)
            .ThenBy(task => task.Title)
            .Select(task => new DashboardRiskItemDto
            {
                ItemId = task.Id,
                ParentTaskId = task.ParentTaskId,
                SubProjectId = task.SubProjectId,
                Title = task.Title,
                ParentTaskTitle = task.ParentTaskTitle,
                SubProjectTitle = task.SubProjectTitle,
                ProjectTitle = task.ProjectTitle,
                ClientCompanyName = task.ClientCompanyName,
                AssignedUserName = BuildUserNameOrNull(
                    task.AssignedUserFirstName,
                    task.AssignedUserLastName),
                Status = task.Status,
                DueDate = task.EndDate,
                DaysOverdue = CalculateDaysOverdue(
                    task.EndDate,
                    false,
                    today)
            })
            .ToList();

        result.Workload = teamUsers
            .Select(user =>
            {
                var assignedTasks = parentTasks
                    .Where(task =>
                        task.AssignedUserId == user.Id &&
                        !IsCompletedTaskStatus(task.Status))
                    .Select(task => new WorkloadTaskDto
                    {
                        TaskId = task.Id,
                        SubProjectId = task.SubProjectId,
                        Title = task.Title,
                        ProjectTitle = task.ProjectTitle,
                        SubProjectTitle = task.SubProjectTitle,
                        Status = NormalizeTaskStatus(task.Status),
                        ProgressPercentage = CalculateTaskProgress(
                            task.CompletedSubTaskCount,
                            task.SubTaskCount),
                        DueDate = task.EndDate,
                        DaysOverdue = CalculateDaysOverdue(
                            task.EndDate,
                            false,
                            today)
                    })
                    .OrderByDescending(task => task.DaysOverdue)
                    .ThenBy(task => task.DueDate ?? DateOnly.MaxValue)
                    .ThenBy(task => task.Title)
                    .ToList();

                var assignedSubTasks = tasks
                    .Where(task => task.AssignedUserId == user.Id)
                    .Select(task => new WorkloadSubTaskDto
                    {
                        SubTaskId = task.Id,
                        ParentTaskId = task.ParentTaskId,
                        SubProjectId = task.SubProjectId,
                        Title = task.Title,
                        ParentTaskTitle = task.ParentTaskTitle,
                        Status = task.Status,
                        DueDate = task.EndDate,
                        DaysOverdue = CalculateDaysOverdue(
                            task.EndDate,
                            task.Status == SubTaskStatuses.Completed,
                            today)
                    })
                    .OrderBy(task => task.Status == SubTaskStatuses.Completed)
                    .ThenByDescending(task => task.DaysOverdue)
                    .ThenBy(task => task.DueDate ?? DateOnly.MaxValue)
                    .ThenBy(task => task.Title)
                    .ToList();

                return new WorkloadItemDto
                {
                    UserId = user.Id,
                    UserName = BuildUserName(
                        user.FirstName,
                        user.LastName),
                    RoleName = user.RoleName,
                    ActiveTaskCount = assignedTasks.Count,
                    CompletedTaskCount = parentTasks.Count(task =>
                        task.AssignedUserId == user.Id &&
                        IsCompletedTaskStatus(task.Status)),
                    OverdueTaskCount = assignedTasks.Count(task =>
                        task.DaysOverdue > 0),
                    InReviewTaskCount = assignedTasks.Count(task =>
                        task.Status == "En revisión"),
                    ActiveSubTaskCount = assignedSubTasks.Count(task =>
                        task.Status != SubTaskStatuses.Completed),
                    CompletedSubTaskCount = assignedSubTasks.Count(task =>
                        task.Status == SubTaskStatuses.Completed),
                    OverdueSubTaskCount = assignedSubTasks.Count(task =>
                        task.DaysOverdue > 0),
                    AverageProgressPercentage = CalculateWorkloadProgress(
                        assignedTasks,
                        assignedSubTasks),
                    StatusCounts = assignedTasks
                        .GroupBy(task => task.Status)
                        .Select(statusGroup => new WorkloadStatusCountDto
                        {
                            Status = statusGroup.Key,
                            Count = statusGroup.Count()
                        })
                        .OrderBy(item => TaskStatusOrder(item.Status))
                        .ToList(),
                    Tasks = assignedTasks,
                    SubTasks = assignedSubTasks
                };
            })
            .OrderByDescending(item => item.ActiveTaskCount)
            .ThenByDescending(item => item.OverdueTaskCount)
            .ThenBy(item => item.UserName)
            .ToList();

        var distributionStatuses = new[]
        {
            SubTaskStatuses.Pending,
            SubTaskStatuses.InProgress,
            SubTaskStatuses.InReview,
            SubTaskStatuses.Completed
        };
        var distributionTotal = tasks.Count(task =>
            distributionStatuses.Contains(task.Status));

        result.WorkflowDistribution = distributionStatuses
            .Select(status =>
            {
                var count = tasks.Count(task => task.Status == status);
                return new WorkflowDistributionItemDto
                {
                    Status = status,
                    Count = count,
                    Percentage = distributionTotal == 0
                        ? 0
                        : Math.Round(count * 100.0 / distributionTotal, 1)
                };
            })
            .ToList();

        result.SubProjectHealth = activeSubProjects
            .Select(subProject => BuildSubProjectHealth(
                subProject,
                tasks,
                today))
            .OrderByDescending(item => item.IsAtRisk)
            .ThenBy(item => item.EndDate ?? DateOnly.MaxValue)
            .Take(6)
            .ToList();

        var upcomingLimit = today.AddDays(UpcomingDeadlineDays);
        result.UpcomingDeadlines = tasks
            .Where(task =>
                task.EndDate.HasValue &&
                task.EndDate.Value >= today &&
                task.EndDate.Value <= upcomingLimit &&
                task.Status != SubTaskStatuses.Completed)
            .OrderBy(task => task.EndDate)
            .ThenBy(task => task.Title)
            .Take(6)
            .Select(task => new DashboardTaskItemDto
            {
                TaskId = task.Id,
                ParentTaskId = task.ParentTaskId,
                SubProjectId = task.SubProjectId,
                Title = task.Title,
                ParentTaskTitle = task.ParentTaskTitle,
                AssignedUserName = BuildUserNameOrNull(
                    task.AssignedUserFirstName,
                    task.AssignedUserLastName),
                Status = task.Status,
                DueDate = task.EndDate!.Value,
                DaysRemaining = task.EndDate.Value.DayNumber - today.DayNumber
            })
            .ToList();

        result.StalledTasks = parentTasks
            .Where(task =>
                !IsCompletedTaskStatus(task.Status) &&
                task.StartDate <= today)
            .Select(task =>
            {
                var lastRecordedChange = lastTaskStatusChange.GetValueOrDefault(
                    task.Id,
                    task.UpdatedAt ?? task.CreatedAt);
                var operationalStart = task.StartDate.ToDateTime(
                    TimeOnly.MinValue,
                    DateTimeKind.Utc);
                var stalledSince = lastRecordedChange > operationalStart
                    ? lastRecordedChange
                    : operationalStart;

                return new StalledTaskItemDto
                {
                    TaskId = task.Id,
                    SubProjectId = task.SubProjectId,
                    Title = task.Title,
                    AssignedUserName = BuildUserNameOrNull(
                        task.AssignedUserFirstName,
                        task.AssignedUserLastName),
                    Status = task.Status,
                    DaysOverdue = CalculateDaysOverdue(
                        task.EndDate,
                        IsCompletedTaskStatus(task.Status),
                        today),
                    LastStatusChangeAt = stalledSince,
                    DaysWithoutChange = Math.Max(
                        0,
                        (int)Math.Floor((now - stalledSince).TotalDays))
                };
            })
            .Where(task => task.DaysWithoutChange > 0)
            .OrderByDescending(task => task.DaysWithoutChange)
            .ThenBy(task => task.Title)
            .Take(6)
            .ToList();

        result.StalledSubTasks = tasks
            .Where(task =>
                task.Status != SubTaskStatuses.Completed &&
                task.StartDate <= today)
            .Select(task =>
            {
                var lastRecordedChange = lastSubTaskStatusChange.GetValueOrDefault(
                    task.Id,
                    task.UpdatedAt ?? task.CreatedAt);
                var operationalStart = task.StartDate.ToDateTime(
                    TimeOnly.MinValue,
                    DateTimeKind.Utc);
                var stalledSince = lastRecordedChange > operationalStart
                    ? lastRecordedChange
                    : operationalStart;

                return new StalledSubTaskItemDto
                {
                    SubTaskId = task.Id,
                    ParentTaskId = task.ParentTaskId,
                    SubProjectId = task.SubProjectId,
                    Title = task.Title,
                    ParentTaskTitle = task.ParentTaskTitle,
                    AssignedUserName = BuildUserNameOrNull(
                        task.AssignedUserFirstName,
                        task.AssignedUserLastName),
                    Status = task.Status,
                    DaysOverdue = CalculateDaysOverdue(
                        task.EndDate,
                        task.Status == SubTaskStatuses.Completed,
                        today),
                    LastStatusChangeAt = stalledSince,
                    DaysWithoutChange = Math.Max(
                        0,
                        (int)Math.Floor((now - stalledSince).TotalDays))
                };
            })
            .Where(task => task.DaysWithoutChange > 0)
            .OrderByDescending(task => task.DaysWithoutChange)
            .ThenBy(task => task.Title)
            .Take(6)
            .ToList();

        await managementDashboardService.PopulateAsync(result, filter, now);
        return result;
    }

    private static double? CalculateAverageCycleTime(
        IReadOnlyDictionary<Guid, List<SubTaskStatusHistory>> historyByTask)
    {
        var cycleTimes = new List<double>();

        foreach (var taskHistory in historyByTask.Values)
        {
            var started = taskHistory.FirstOrDefault(history =>
                history.ToStatus == SubTaskStatuses.InProgress);

            if (started == null)
                continue;

            var completed = taskHistory.FirstOrDefault(history =>
                history.ToStatus == SubTaskStatuses.Completed &&
                history.Timestamp >= started.Timestamp);

            if (completed != null)
            {
                cycleTimes.Add(
                    (completed.Timestamp - started.Timestamp).TotalDays);
            }
        }

        return cycleTimes.Count == 0
            ? null
            : Math.Round(cycleTimes.Average(), 1);
    }

    private static bool MatchesFilter(
        Guid? clientCompanyId,
        Guid projectId,
        Guid subProjectId,
        Guid departmentId,
        Guid? responsibleUserId,
        DashboardFilterDto filter)
    {
        return (!filter.ClientCompanyId.HasValue ||
                clientCompanyId == filter.ClientCompanyId.Value) &&
            (!filter.ProjectId.HasValue ||
                projectId == filter.ProjectId.Value) &&
            (!filter.SubProjectId.HasValue ||
                subProjectId == filter.SubProjectId.Value) &&
            (!filter.DepartmentId.HasValue ||
                departmentId == filter.DepartmentId.Value) &&
            (!filter.ResponsibleUserId.HasValue ||
                responsibleUserId == filter.ResponsibleUserId.Value);
    }

    private static bool MatchesHierarchyFilter(
        SubProjectSnapshot subProject,
        DashboardFilterDto filter)
    {
        return (!filter.ClientCompanyId.HasValue ||
                subProject.ClientCompanyId == filter.ClientCompanyId.Value) &&
            (!filter.ProjectId.HasValue ||
                subProject.ProjectId == filter.ProjectId.Value) &&
            (!filter.SubProjectId.HasValue ||
                subProject.Id == filter.SubProjectId.Value) &&
            (!filter.DepartmentId.HasValue ||
                subProject.DepartmentId == filter.DepartmentId.Value);
    }

    private static bool IsSubProjectAtRisk(
        SubProjectSnapshot subProject,
        IReadOnlyCollection<TaskSnapshot> subTasks,
        DateOnly today)
    {
        return BuildSubProjectHealth(subProject, subTasks, today).IsAtRisk;
    }

    private static SubProjectHealthItemDto BuildSubProjectHealth(
        SubProjectSnapshot subProject,
        IReadOnlyCollection<TaskSnapshot> subTasks,
        DateOnly today)
    {
        var scopedSubTasks = subTasks
            .Where(task => task.SubProjectId == subProject.Id)
            .ToList();
        var completed = scopedSubTasks.Count(task =>
            task.Status == SubTaskStatuses.Completed);
        var progress = scopedSubTasks.Count == 0
            ? 0
            : (int)Math.Round(completed * 100.0 / scopedSubTasks.Count);
        var hasOverdueSubTasks = scopedSubTasks.Any(task =>
            task.EndDate.HasValue &&
            task.EndDate.Value < today &&
            task.Status != SubTaskStatuses.Completed);

        return new SubProjectHealthItemDto
        {
            SubProjectId = subProject.Id,
            Title = subProject.Title,
            ProjectTitle = subProject.ProjectTitle,
            StartDate = subProject.StartDate,
            EndDate = subProject.EndDate,
            TotalTaskCount = scopedSubTasks.Count,
            CompletedTaskCount = completed,
            ProgressPercentage = progress,
            IsAtRisk = hasOverdueSubTasks ||
                (subProject.EndDate.HasValue &&
                 subProject.EndDate.Value < today &&
                 progress < 100)
        };
    }

    private static bool IsCompletedTaskStatus(string status)
    {
        return status is "Completado" or "Completada";
    }

    private static int CalculateTaskProgress(int completed, int total)
    {
        return total == 0
            ? 0
            : (int)Math.Round(completed * 100.0 / total);
    }

    internal static int CalculateWorkloadProgress(
        IReadOnlyCollection<WorkloadTaskDto> activeTasks,
        IReadOnlyCollection<WorkloadSubTaskDto> assignedSubTasks)
    {
        var activeTaskIds = activeTasks
            .Select(task => task.TaskId)
            .ToHashSet();
        var independentSubTasks = assignedSubTasks
            .Where(subTask => !activeTaskIds.Contains(subTask.ParentTaskId))
            .ToList();
        var responsibilityCount = activeTasks.Count + independentSubTasks.Count;

        if (responsibilityCount == 0) return 0;

        var totalProgress = activeTasks.Sum(task => task.ProgressPercentage) +
            independentSubTasks.Count(subTask =>
                subTask.Status == SubTaskStatuses.Completed) * 100;

        return (int)Math.Round(totalProgress * 1.0 / responsibilityCount);
    }

    private static string NormalizeTaskStatus(string status)
    {
        return status switch
        {
            TaskItemStatuses.InProgress or "En proceso" => "En proceso",
            TaskItemStatuses.InReview => "En revisión",
            TaskItemStatuses.Completed or "Completada" => "Completada",
            _ => status
        };
    }

    private static int TaskStatusOrder(string status)
    {
        return status switch
        {
            "Pendiente" => 0,
            "En proceso" => 1,
            "En revisión" => 2,
            "Completada" => 3,
            _ => 4
        };
    }

    private static int CalculateDaysOverdue(
        DateOnly? endDate,
        bool isCompleted,
        DateOnly today)
    {
        if (!endDate.HasValue || isCompleted || endDate.Value >= today)
            return 0;

        return today.DayNumber - endDate.Value.DayNumber;
    }

    private static string BuildUserName(string? firstName, string? lastName)
    {
        return BuildUserNameOrNull(firstName, lastName) ?? "Usuario sin nombre";
    }

    private static string? BuildUserNameOrNull(
        string? firstName,
        string? lastName)
    {
        var name = string.Join(
            " ",
            new[] { firstName, lastName }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private sealed class TaskSnapshot
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public Guid ParentTaskId { get; init; }
        public string ParentTaskTitle { get; init; } = string.Empty;
        public Guid SubProjectId { get; init; }
        public string SubProjectTitle { get; init; } = string.Empty;
        public Guid ProjectId { get; init; }
        public string ProjectTitle { get; init; } = string.Empty;
        public Guid? ClientCompanyId { get; init; }
        public string? ClientCompanyName { get; init; }
        public Guid DepartmentId { get; init; }
        public Guid? AssignedUserId { get; init; }
        public string? AssignedUserFirstName { get; init; }
        public string? AssignedUserLastName { get; init; }
    }

    private sealed class ParentTaskSnapshot
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public Guid SubProjectId { get; init; }
        public string SubProjectTitle { get; init; } = string.Empty;
        public Guid ProjectId { get; init; }
        public string ProjectTitle { get; init; } = string.Empty;
        public Guid? ClientCompanyId { get; init; }
        public string? ClientCompanyName { get; init; }
        public Guid DepartmentId { get; init; }
        public Guid? AssignedUserId { get; init; }
        public string? AssignedUserFirstName { get; init; }
        public string? AssignedUserLastName { get; init; }
        public int SubTaskCount { get; init; }
        public int CompletedSubTaskCount { get; init; }
    }

    private sealed class SubProjectSnapshot
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public Guid ProjectId { get; init; }
        public string ProjectTitle { get; init; } = string.Empty;
        public Guid? ClientCompanyId { get; init; }
        public Guid DepartmentId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }

    private sealed class TeamUserSnapshot
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string RoleName { get; init; } = string.Empty;
        public List<Guid> DepartmentIds { get; init; } = new();
    }
}
