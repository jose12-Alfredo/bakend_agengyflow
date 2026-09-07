using AgencyFlow.Data;
using AgencyFlow.DTOs.Dashboard;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using AgencyFlow.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

/// <summary>Reglas jerárquicas del centro de control gerencial.</summary>
public class ManagementDashboardService(
    AppDbContext db,
    ResourceAuthorizationService authorization)
{
    public const int StalledThresholdDays = 3;

    public async Task PopulateAsync(DashboardDto result, DashboardFilterDto filter, DateTime now)
    {
        await ValidateHierarchyAsync(filter);
        var period = GetPeriod(filter, now);
        var today = DateOnly.FromDateTime(now);

        var tasksQuery = db.TaskItems.AsNoTracking()
            .Include(t => t.AssignedUser)
            .Include(t => t.SubTasks.Where(s => s.DeletedAt == null)).ThenInclude(s => s.AssignedUser)
            .Include(t => t.SubProject).ThenInclude(sp => sp.Department)
            .Include(t => t.SubProject).ThenInclude(sp => sp.Project).ThenInclude(p => p.ClientUser).ThenInclude(u => u!.ClientCompany).ThenInclude(c => c!.RepresentativeUser)
            .Where(t => t.DeletedAt == null && t.SubProject.DeletedAt == null && t.SubProject.Project.DeletedAt == null);
        var tasks = await authorization.FilterTasks(tasksQuery)
            .ToListAsync();
        var allClientCompanies = await db.ClientCompanies.AsNoTracking()
            .Include(company => company.RepresentativeUser)
            .Where(company => company.DeletedAt == null)
            .ToListAsync();

        var scoped = tasks.Where(t => Matches(t, filter) && Overlaps(t.StartDate, t.EndDate, period.Start, period.End)).ToList();
        // El periodo limita la operación mensual (atrasos, carga y actividad),
        // pero el avance es acumulado. Una tarea futura de un subproyecto activo
        // también forma parte del trabajo total y no puede desaparecer del divisor.
        var progressScope = tasks.Where(t =>
            Matches(t, filter) &&
            Overlaps(
                t.SubProject.Project.StartDate,
                t.SubProject.Project.EndDate,
                period.Start,
                period.End) &&
            Overlaps(
                t.SubProject.StartDate,
                t.SubProject.EndDate,
                period.Start,
                period.End))
            .ToList();
        var hasWorkScopeFilter = filter.ProjectId.HasValue ||
            filter.SubProjectId.HasValue ||
            filter.DepartmentId.HasValue ||
            filter.ResponsibleUserId.HasValue;
        var companiesForTable = filter.ClientCompanyId.HasValue
            ? allClientCompanies.Where(company => company.Id == filter.ClientCompanyId.Value)
            : hasWorkScopeFilter || authorization.IsDirector
                ? allClientCompanies.Where(company => progressScope.Any(task =>
                    task.SubProject.Project.ClientUser?.ClientCompanyId == company.Id))
                : allClientCompanies;
        var activeTasks = scoped.Where(t => !IsCompleted(t.Status)).ToList();
        var activeSubTasks = activeTasks.SelectMany(t => t.SubTasks.Where(s => !IsCompleted(s.Status) && Matches(s, t, filter) && Overlaps(s.StartDate, s.EndDate, period.Start, period.End))).ToList();
        var overdueTasks = activeTasks.Where(t => t.EndDate is { } due && due < today).ToList();
        var overdueSubTasks = activeSubTasks.Where(s => s.EndDate is { } due && due < today).ToList();
        var unassignedTasks = activeTasks.Where(t => t.AssignedUserId == null).ToList();
        var unassignedSubTasks = activeSubTasks.Where(s => s.AssignedUserId == null).ToList();
        var stalledTasks = activeTasks.Where(t => !IsPaused(t.Status) && DaysSince(t.LastActivityAt ?? t.UpdatedAt ?? t.CreatedAt, now) >= StalledThresholdDays).ToList();
        var stalledSubTasks = activeSubTasks.Where(s => !IsPaused(s.Status) && DaysSince(s.LastActivityAt ?? s.UpdatedAt ?? s.CreatedAt, now) >= StalledThresholdDays).ToList();

        result.Kpis.UnassignedTaskCount = unassignedTasks.Count;
        result.Kpis.UnassignedSubTaskCount = unassignedSubTasks.Count;
        result.Kpis.StalledTaskCount = stalledTasks.Count;
        result.Kpis.StalledSubTaskCount = stalledSubTasks.Count;
        result.Kpis.GlobalProgressPercentage = Average(progressScope.Select(TaskProgress));
        result.UnassignedTasks = unassignedTasks.Select(t => ToRisk(t, null, today)).ToList();
        result.UnassignedSubTasks = unassignedSubTasks.Select(s => ToRisk(scoped.Single(t => t.Id == s.TaskItemId), s, today)).ToList();
        result.OverdueSubTasks = overdueSubTasks.Select(s => ToRisk(scoped.Single(t => t.Id == s.TaskItemId), s, today)).ToList();

        result.StalledTasks = stalledTasks.Select(t => ToStalled(t, now, today)).ToList();
        result.StalledSubTasks = stalledSubTasks.Select(s => ToStalled(s, scoped.Single(t => t.Id == s.TaskItemId), now, today)).ToList();
        result.ClientHealth = BuildClientHealth(companiesForTable, progressScope, overdueTasks, overdueSubTasks, unassignedTasks, unassignedSubTasks, stalledTasks, stalledSubTasks);
        if (filter.ClientCompanyId.HasValue)
            result.ClientDetail = BuildClientDetail(progressScope, filter.ClientCompanyId.Value);

        var taskIds = scoped.Select(t => t.Id).ToHashSet();
        var subTaskIds = scoped.SelectMany(t => t.SubTasks).Select(s => s.Id).ToHashSet();
        var taskCompletions = await db.TaskStatusHistories.AsNoTracking().Where(h => taskIds.Contains(h.TaskId) && h.ToStatus == TaskItemStatuses.Completed && h.Timestamp >= period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && h.Timestamp < period.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToListAsync();
        var subTaskCompletions = await db.SubTaskStatusHistories.AsNoTracking().Where(h => subTaskIds.Contains(h.SubTaskId) && h.ToStatus == SubTaskStatuses.Completed && h.Timestamp >= period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && h.Timestamp < period.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToListAsync();
        result.MonthlyMetrics = new DashboardMonthlyMetricsDto
        {
            CreatedTaskCount = scoped.Count(t => t.CreatedAt >= period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && t.CreatedAt < period.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            CreatedSubTaskCount = scoped.SelectMany(t => t.SubTasks).Count(s => s.CreatedAt >= period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && s.CreatedAt < period.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            CompletedTaskCount = taskCompletions.Select(h => h.TaskId).Distinct().Count(),
            CompletedSubTaskCount = subTaskCompletions.Select(h => h.SubTaskId).Distinct().Count(),
            AverageCycleTimeDays = await CalculateMonthlyCycleTimeAsync(subTaskIds, period)
        };
    }

    /// <summary>
    /// Carga bajo demanda el árbol de un cliente con un número fijo de consultas,
    /// evitando una consulta por cada nodo de la jerarquía.
    /// </summary>
    public async Task<ClientHierarchyDto?> GetClientHierarchyAsync(
        Guid clientId,
        ClientHierarchyFilterDto filter)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .Include(client => client.RepresentativeUser)
            .FirstOrDefaultAsync(client =>
                client.Id == clientId && client.DeletedAt == null);

        if (company == null)
            return null;

        var periodStart = new DateOnly(filter.Year, filter.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projectsQuery = db.Projects.AsNoTracking()
            .Where(project => project.DeletedAt == null &&
                project.ClientUser != null &&
                project.ClientUser.ClientCompanyId == clientId &&
                project.StartDate <= periodEnd &&
                (!project.EndDate.HasValue || project.EndDate.Value >= periodStart));
        var projects = await authorization.FilterProjects(projectsQuery)
            .ToListAsync();

        var projectIds = projects.Select(project => project.Id).ToHashSet();
        var subProjectsQuery = db.SubProjects.AsNoTracking()
            .Include(subProject => subProject.Department)
            .Where(subProject => subProject.DeletedAt == null &&
                projectIds.Contains(subProject.ProjectId) &&
                (!filter.DepartmentId.HasValue ||
                 subProject.DepartmentId == filter.DepartmentId.Value) &&
                subProject.StartDate <= periodEnd &&
                (!subProject.EndDate.HasValue || subProject.EndDate.Value >= periodStart));
        var subProjects = await authorization.FilterSubProjects(subProjectsQuery)
            .ToListAsync();

        if (authorization.IsDirector)
        {
            var visibleProjectIds = subProjects
                .Select(subProject => subProject.ProjectId)
                .ToHashSet();
            projects = projects
                .Where(project => visibleProjectIds.Contains(project.Id))
                .ToList();
            if (projects.Count == 0)
                return null;
        }

        var subProjectIds = subProjects.Select(subProject => subProject.Id).ToHashSet();
        var tasks = await db.TaskItems.AsNoTracking()
            .Include(task => task.AssignedUser)
            .Include(task => task.SubTasks.Where(subTask =>
                subTask.DeletedAt == null))
            .ThenInclude(subTask => subTask.AssignedUser)
            .Where(task => task.DeletedAt == null &&
                subProjectIds.Contains(task.SubProjectId))
            .ToListAsync();

        var hierarchy = new ClientHierarchyDto
        {
            ClientId = company.Id,
            ClientName = company.Name,
            RepresentativeUserId = company.RepresentativeUserId,
            RepresentativeUserName = Name(company.RepresentativeUser)
        };

        foreach (var project in projects)
        {
            var projectSubProjects = subProjects
                .Where(subProject => subProject.ProjectId == project.Id)
                .Select(subProject => BuildHierarchySubProject(
                    subProject,
                    tasks.Where(task => task.SubProjectId == subProject.Id),
                    filter.ResponsibleUserId))
                .Where(item => item != null)
                .Cast<ClientHierarchySubProjectDto>()
                .OrderByDescending(item => IsAtRisk(item, today))
                .ThenBy(item => item.Title)
                .ToList();

            hierarchy.Projects.Add(new ClientHierarchyProjectDto
            {
                ProjectId = project.Id,
                Title = project.Title,
                // Project no tiene responsable propio en el modelo actual.
                AssignedUserId = null,
                AssignedUserName = null,
                ProgressPercentage = Average(projectSubProjects.Select(item => item.ProgressPercentage)),
                SubProjects = projectSubProjects
            });
        }

        hierarchy.Projects = hierarchy.Projects
            .OrderByDescending(project => project.SubProjects.Any(item => IsAtRisk(item, today)))
            .ThenBy(project => project.Title)
            .ToList();
        hierarchy.ProgressPercentage = Average(
            hierarchy.Projects.Select(project => project.ProgressPercentage));
        return hierarchy;
    }

    private static ClientHierarchySubProjectDto? BuildHierarchySubProject(
        SubProject subProject,
        IEnumerable<TaskItem> sourceTasks,
        Guid? responsibleUserId)
    {
        var tasks = sourceTasks
            .Select(task => BuildHierarchyTask(task, responsibleUserId))
            .Where(item => item != null)
            .Cast<ClientHierarchyTaskDto>()
            .ToList();

        // El filtro mantiene el padre contextual, pero no muestra áreas sin
        // trabajo elegible cuando se solicitó un responsable concreto.
        if (responsibleUserId.HasValue && tasks.Count == 0)
            return null;

        return new ClientHierarchySubProjectDto
        {
            SubProjectId = subProject.Id,
            Title = subProject.Title,
            DepartmentId = subProject.DepartmentId,
            DepartmentName = subProject.Department?.Name,
            ProgressPercentage = Average(tasks.Select(task => task.ProgressPercentage)),
            Tasks = tasks
                .OrderByDescending(task => IsAtRisk(task))
                .ThenBy(task => task.Title)
                .ToList()
        };
    }

    private static ClientHierarchyTaskDto? BuildHierarchyTask(
        TaskItem task,
        Guid? responsibleUserId)
    {
        var subTasks = task.SubTasks
            .Where(subTask => !responsibleUserId.HasValue ||
                subTask.AssignedUserId == responsibleUserId.Value)
            .Select(subTask => new ClientHierarchySubTaskDto
            {
                SubTaskId = subTask.Id,
                Title = subTask.Title,
                Status = subTask.Status,
                ProgressPercentage = subTask.Status == SubTaskStatuses.Completed ? 100 : 0,
                AssignedUserId = subTask.AssignedUserId,
                AssignedUserName = Name(subTask.AssignedUser)
            })
            .ToList();

        var matchesTask = !responsibleUserId.HasValue ||
            task.AssignedUserId == responsibleUserId.Value;
        if (!matchesTask && subTasks.Count == 0)
            return null;

        return new ClientHierarchyTaskDto
        {
            TaskId = task.Id,
            Title = task.Title,
            Status = task.Status,
            ProgressPercentage = TaskProgress(task, subTasks),
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = Name(task.AssignedUser),
            SubTasks = subTasks
        };
    }

    private static double? TaskProgress(
        TaskItem task,
        IReadOnlyCollection<ClientHierarchySubTaskDto> subTasks)
    {
        if (subTasks.Count == 0)
            return task.OwnProgressPercentage;

        return Math.Round(
            subTasks.Count(item => item.Status == SubTaskStatuses.Completed) *
            100.0 / subTasks.Count, 1);
    }

    private static bool IsAtRisk(ClientHierarchySubProjectDto item, DateOnly today) =>
        item.Tasks.Any(task => IsAtRisk(task));

    private static bool IsAtRisk(ClientHierarchyTaskDto item) =>
        item.SubTasks.Any(subTask => subTask.Status != SubTaskStatuses.Completed);

    private async Task ValidateHierarchyAsync(DashboardFilterDto filter)
    {
        if (filter.ProjectId.HasValue && filter.ClientCompanyId.HasValue && !await db.Projects.AnyAsync(p => p.Id == filter.ProjectId && p.DeletedAt == null && p.ClientUser!.ClientCompanyId == filter.ClientCompanyId))
            throw new BusinessValidationException("El proyecto no pertenece al cliente indicado.");
        if (filter.SubProjectId.HasValue && filter.ProjectId.HasValue && !await db.SubProjects.AnyAsync(s => s.Id == filter.SubProjectId && s.DeletedAt == null && s.ProjectId == filter.ProjectId))
            throw new BusinessValidationException("El subproyecto no pertenece al proyecto indicado.");
        if (filter.SubProjectId.HasValue && filter.ClientCompanyId.HasValue && !await db.SubProjects.AnyAsync(s => s.Id == filter.SubProjectId && s.DeletedAt == null && s.Project.ClientUser!.ClientCompanyId == filter.ClientCompanyId))
            throw new BusinessValidationException("El subproyecto no pertenece al cliente indicado.");
    }

    private async Task<double?> CalculateMonthlyCycleTimeAsync(HashSet<Guid> subTaskIds, (DateOnly Start, DateOnly End) period)
    {
        var histories = await db.SubTaskStatusHistories.AsNoTracking().Where(h => subTaskIds.Contains(h.SubTaskId)).OrderBy(h => h.Timestamp).ToListAsync();
        var periodStart = period.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodEnd = period.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var durations = histories.GroupBy(h => h.SubTaskId).Select(group => new { Started = group.FirstOrDefault(h => h.ToStatus == SubTaskStatuses.InProgress), Completed = group.FirstOrDefault(h => h.ToStatus == SubTaskStatuses.Completed && h.Timestamp >= periodStart && h.Timestamp < periodEnd) }).Where(x => x.Started != null && x.Completed != null && x.Completed.Timestamp >= x.Started.Timestamp).Select(x => (x.Completed!.Timestamp - x.Started!.Timestamp).TotalDays).ToList();
        return durations.Count == 0 ? null : Math.Round(durations.Average(), 1);
    }

    private static (DateOnly Start, DateOnly End) GetPeriod(DashboardFilterDto filter, DateTime now)
    {
        var year = filter.Year ?? now.Year;
        var month = filter.Month ?? now.Month;
        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    // El alcance mensual se define por solapamiento del intervalo planificado.
    private static bool Overlaps(DateOnly start, DateOnly? end, DateOnly periodStart, DateOnly periodEnd) =>
        start <= periodEnd && (!end.HasValue || end.Value >= periodStart);

    private static bool Matches(TaskItem task, DashboardFilterDto f) =>
        (!f.ClientCompanyId.HasValue || task.SubProject.Project.ClientUser?.ClientCompanyId == f.ClientCompanyId) &&
        (!f.ProjectId.HasValue || task.SubProject.ProjectId == f.ProjectId) &&
        (!f.SubProjectId.HasValue || task.SubProjectId == f.SubProjectId) &&
        (!f.DepartmentId.HasValue || task.SubProject.DepartmentId == f.DepartmentId) &&
        (!f.ResponsibleUserId.HasValue || task.AssignedUserId == f.ResponsibleUserId);

    private static bool Matches(SubTask subTask, TaskItem parent, DashboardFilterDto f) =>
        !f.ResponsibleUserId.HasValue || subTask.AssignedUserId == f.ResponsibleUserId;

    private static bool IsCompleted(string status) => status is TaskItemStatuses.Completed or SubTaskStatuses.Completed or "Cancelado" or "Cancelada";
    private static bool IsPaused(string status) => status == SubTaskStatuses.Paused || status == "Pausada";
    private static int DaysSince(DateTime value, DateTime now) => Math.Max(0, (int)Math.Floor((now - value).TotalDays));
    private static double? Average(IEnumerable<double?> values) { var v = values.Where(x => x.HasValue).Select(x => x!.Value).ToList(); return v.Count == 0 ? null : Math.Round(v.Average(), 1); }
    private static double? TaskProgress(TaskItem task) { var active = task.SubTasks.Where(s => s.DeletedAt == null).ToList(); return active.Count > 0 ? Math.Round(active.Count(s => s.Status == SubTaskStatuses.Completed) * 100.0 / active.Count, 1) : task.OwnProgressPercentage; }

    private static DashboardRiskItemDto ToRisk(TaskItem task, SubTask? subTask, DateOnly today) => new()
    {
        ItemId = subTask?.Id ?? task.Id, ParentTaskId = task.Id, SubProjectId = task.SubProjectId, ProjectId = task.SubProject.ProjectId,
        ClientCompanyId = task.SubProject.Project.ClientUser?.ClientCompanyId, DepartmentId = task.SubProject.DepartmentId,
        Title = subTask?.Title ?? task.Title, ParentTaskTitle = task.Title, SubProjectTitle = task.SubProject.Title, ProjectTitle = task.SubProject.Project.Title,
        ClientCompanyName = task.SubProject.Project.ClientUser?.ClientCompany?.Name, DepartmentName = task.SubProject.Department.Name,
        AssignedUserId = subTask?.AssignedUserId ?? task.AssignedUserId, AssignedUserName = Name(subTask?.AssignedUser ?? task.AssignedUser), Status = subTask?.Status ?? task.Status,
        DueDate = subTask?.EndDate ?? task.EndDate, DaysOverdue = DaysOverdue(subTask?.EndDate ?? task.EndDate, today), LastActivityAt = subTask?.LastActivityAt ?? task.LastActivityAt,
        ProgressPercentage = subTask == null ? (int?)TaskProgress(task) : (subTask.Status == SubTaskStatuses.Completed ? 100 : 0)
    };

    private static StalledTaskItemDto ToStalled(TaskItem task, DateTime now, DateOnly today) => new() { TaskId = task.Id, SubProjectId = task.SubProjectId, Title = task.Title, AssignedUserName = Name(task.AssignedUser), Status = task.Status, LastStatusChangeAt = task.LastActivityAt ?? task.UpdatedAt ?? task.CreatedAt, DaysWithoutChange = DaysSince(task.LastActivityAt ?? task.UpdatedAt ?? task.CreatedAt, now), DaysOverdue = DaysOverdue(task.EndDate, today) };
    private static StalledSubTaskItemDto ToStalled(SubTask subTask, TaskItem task, DateTime now, DateOnly today) => new() { SubTaskId = subTask.Id, ParentTaskId = task.Id, SubProjectId = task.SubProjectId, Title = subTask.Title, ParentTaskTitle = task.Title, AssignedUserName = Name(subTask.AssignedUser), Status = subTask.Status, LastStatusChangeAt = subTask.LastActivityAt ?? subTask.UpdatedAt ?? subTask.CreatedAt, DaysWithoutChange = DaysSince(subTask.LastActivityAt ?? subTask.UpdatedAt ?? subTask.CreatedAt, now), DaysOverdue = DaysOverdue(subTask.EndDate, today) };
    private static int DaysOverdue(DateOnly? due, DateOnly today) => due is { } date && date < today ? today.DayNumber - date.DayNumber : 0;
    private static string? Name(User? user) => user == null ? null : $"{user.FirstName} {user.LastName}".Trim();

    private static List<ClientHealthDto> BuildClientHealth(
        IEnumerable<ClientCompany> companies,
        List<TaskItem> tasks,
        List<TaskItem> overdue,
        List<SubTask> overdueSubs,
        List<TaskItem> unassigned,
        List<SubTask> unassignedSubs,
        List<TaskItem> stalled,
        List<SubTask> stalledSubs) => companies
        .Select(company =>
        {
            var clientTasks = tasks.Where(task =>
                task.SubProject.Project.ClientUser?.ClientCompanyId == company.Id)
                .ToList();
            var taskIds = clientTasks.Select(task => task.Id).ToHashSet();

            return new ClientHealthDto
            {
                ClientId = company.Id,
                ClientName = company.Name,
                RepresentativeUserId = company.RepresentativeUserId,
                RepresentativeUserName = Name(company.RepresentativeUser),
                ProgressPercentage = Average(clientTasks.Select(TaskProgress)),
                ActiveProjectCount = clientTasks
                    .Select(task => task.SubProject.ProjectId)
                    .Distinct()
                    .Count(),
                OverdueTaskCount = overdue.Count(task => taskIds.Contains(task.Id)),
                OverdueSubTaskCount = overdueSubs.Count(subTask => taskIds.Contains(subTask.TaskItemId)),
                UnassignedTaskCount = unassigned.Count(task => taskIds.Contains(task.Id)),
                UnassignedSubTaskCount = unassignedSubs.Count(subTask => taskIds.Contains(subTask.TaskItemId)),
                StalledTaskCount = stalled.Count(task => taskIds.Contains(task.Id)),
                StalledSubTaskCount = stalledSubs.Count(subTask => taskIds.Contains(subTask.TaskItemId))
            };
        })
        .OrderBy(client => client.ClientName)
        .ToList();

    private static ClientDashboardDetailDto? BuildClientDetail(List<TaskItem> tasks, Guid clientId)
    {
        var clientTasks = tasks.Where(t => t.SubProject.Project.ClientUser?.ClientCompanyId == clientId).ToList();
        var company = clientTasks.FirstOrDefault()?.SubProject.Project.ClientUser?.ClientCompany;
        if (company == null) return null;
        return new ClientDashboardDetailDto { ClientId = company.Id, ClientName = company.Name, RepresentativeUserId = company.RepresentativeUserId, RepresentativeUserName = Name(company.RepresentativeUser), ProgressPercentage = Average(clientTasks.Select(TaskProgress)), ActiveProjectCount = clientTasks.Select(t => t.SubProject.ProjectId).Distinct().Count(), Projects = clientTasks.GroupBy(t => t.SubProject.Project).Select(p => new DashboardProjectDetailDto { ProjectId = p.Key.Id, Title = p.Key.Title, ProgressPercentage = Average(p.Select(t => TaskProgress(t))), SubProjects = p.GroupBy(t => t.SubProject).Select(sp => new DashboardSubProjectDetailDto { SubProjectId = sp.Key.Id, Title = sp.Key.Title, DepartmentId = sp.Key.DepartmentId, DepartmentName = sp.Key.Department.Name, ProgressPercentage = Average(sp.Select(t => TaskProgress(t))), Tasks = sp.Select(t => new DashboardWorkDetailDto { Id = t.Id, Title = t.Title, Status = t.Status, AssignedUserId = t.AssignedUserId, AssignedUserName = Name(t.AssignedUser), DueDate = t.EndDate, ProgressPercentage = TaskProgress(t), SubTasks = t.SubTasks.Select(s => new DashboardWorkDetailDto { Id = s.Id, Title = s.Title, Status = s.Status, AssignedUserId = s.AssignedUserId, AssignedUserName = Name(s.AssignedUser), DueDate = s.EndDate, ProgressPercentage = s.Status == SubTaskStatuses.Completed ? 100 : 0 }).ToList() }).ToList() }).ToList() }).ToList() };
    }
}
