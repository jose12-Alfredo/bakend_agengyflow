namespace AgencyFlow.DTOs.Dashboard;

public class DashboardFilterDto
{
    public Guid? ClientCompanyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SubProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(2000, 9999)]
    public int? Year { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 12)]
    public int? Month { get; set; }
}

public class DashboardDto
{
    public DashboardKpisDto Kpis { get; set; } = new();
    public List<WorkloadItemDto> Workload { get; set; } = new();
    public List<WorkflowDistributionItemDto> WorkflowDistribution { get; set; } = new();
    public int PausedTaskCount { get; set; }
    public List<SubProjectHealthItemDto> SubProjectHealth { get; set; } = new();
    public List<DashboardTaskItemDto> UpcomingDeadlines { get; set; } = new();
    public List<StalledTaskItemDto> StalledTasks { get; set; } = new();
    public List<StalledSubTaskItemDto> StalledSubTasks { get; set; } = new();
    public List<DashboardRiskItemDto> OverdueTasks { get; set; } = new();
    public List<DashboardRiskItemDto> OverdueSubTasks { get; set; } = new();
    public List<DashboardRiskItemDto> UnassignedTasks { get; set; } = new();
    public List<DashboardRiskItemDto> UnassignedSubTasks { get; set; } = new();
    public List<ClientHealthDto> ClientHealth { get; set; } = new();
    public ClientDashboardDetailDto? ClientDetail { get; set; }
    public DashboardMonthlyMetricsDto MonthlyMetrics { get; set; } = new();
    public List<DashboardRiskItemDto> BottleneckItems { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class DashboardKpisDto
{
    public int OverdueTaskCount { get; set; }
    public int OverdueSubTaskCount { get; set; }
    public double? AverageCycleTimeDays { get; set; }
    public double CompletionRate { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public int TotalSubTaskCount { get; set; }
    public int BottleneckCount { get; set; }
    public int AtRiskProjectCount { get; set; }
    public int AtRiskSubProjectCount { get; set; }
    public int AvailablePersonCount { get; set; }
    public int UnassignedTaskCount { get; set; }
    public int UnassignedSubTaskCount { get; set; }
    public double? GlobalProgressPercentage { get; set; }
    public int StalledTaskCount { get; set; }
    public int StalledSubTaskCount { get; set; }
}

public class WorkloadItemDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int ActiveTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int InReviewTaskCount { get; set; }
    public int ActiveSubTaskCount { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public int OverdueSubTaskCount { get; set; }
    public int AverageProgressPercentage { get; set; }
    public List<WorkloadStatusCountDto> StatusCounts { get; set; } = new();
    public List<WorkloadTaskDto> Tasks { get; set; } = new();
    public List<WorkloadSubTaskDto> SubTasks { get; set; } = new();
}

public class WorkloadStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WorkloadTaskDto
{
    public Guid TaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string SubProjectTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public DateOnly? DueDate { get; set; }
    public int DaysOverdue { get; set; }
}

public class WorkloadSubTaskDto
{
    public Guid SubTaskId { get; set; }
    public Guid ParentTaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ParentTaskTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public int DaysOverdue { get; set; }
}

public class WorkflowDistributionItemDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class SubProjectHealthItemDto
{
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int TotalTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int ProgressPercentage { get; set; }
    public bool IsAtRisk { get; set; }
}

public class DashboardTaskItemDto
{
    public Guid TaskId { get; set; }
    public Guid ParentTaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ParentTaskTitle { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int DaysRemaining { get; set; }
}

public class StalledTaskItemDto
{
    public Guid TaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysWithoutChange { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime LastStatusChangeAt { get; set; }
}

public class StalledSubTaskItemDto
{
    public Guid SubTaskId { get; set; }
    public Guid ParentTaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ParentTaskTitle { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysWithoutChange { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime LastStatusChangeAt { get; set; }
}

public class DashboardRiskItemDto
{
    public Guid ItemId { get; set; }
    public Guid ParentTaskId { get; set; }
    public Guid SubProjectId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ClientCompanyId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ParentTaskTitle { get; set; } = string.Empty;
    public string SubProjectTitle { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string? ClientCompanyName { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int? ProgressPercentage { get; set; }
}

public class ClientHealthDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public Guid? RepresentativeUserId { get; set; }
    public string? RepresentativeUserName { get; set; }
    public double? ProgressPercentage { get; set; }
    public int ActiveProjectCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int OverdueSubTaskCount { get; set; }
    public int UnassignedTaskCount { get; set; }
    public int UnassignedSubTaskCount { get; set; }
    public int StalledTaskCount { get; set; }
    public int StalledSubTaskCount { get; set; }
}

public class DashboardMonthlyMetricsDto
{
    public int CreatedTaskCount { get; set; }
    public int CreatedSubTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public double? AverageCycleTimeDays { get; set; }
}

public class ClientDashboardDetailDto : ClientHealthDto
{
    public List<DashboardProjectDetailDto> Projects { get; set; } = new();
}

public class DashboardProjectDetailDto
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double? ProgressPercentage { get; set; }
    public List<DashboardSubProjectDetailDto> SubProjects { get; set; } = new();
}

public class DashboardSubProjectDetailDto
{
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public double? ProgressPercentage { get; set; }
    public List<DashboardWorkDetailDto> Tasks { get; set; } = new();
}

public class DashboardWorkDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public DateOnly? DueDate { get; set; }
    public double? ProgressPercentage { get; set; }
    public List<DashboardWorkDetailDto> SubTasks { get; set; } = new();
}
