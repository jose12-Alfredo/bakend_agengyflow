namespace AgencyFlow.DTOs.Productivity;

public class DelayReportFilterDto
{
    public Guid? ClientCompanyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SubProjectId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public string? Level { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public bool OnlyDelayed { get; set; }
}

public class DelayReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DelaySummaryDto Summary { get; set; } = new();
    public List<DelayItemDto> Items { get; set; } = new();
}

public class DelaySummaryDto
{
    public int TotalItemCount { get; set; }
    public int CompletedItemCount { get; set; }
    public int DelayedItemCount { get; set; }
    public double AverageDelayDays { get; set; }
    public int MaximumDelayDays { get; set; }
    public double OnTimePercentage { get; set; }
    public double AverageCycleDays { get; set; }
    public int CurrentlyOverdueCount { get; set; }
}

public class DelayItemDto
{
    public string Level { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? SubProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ActualStartedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDelayed { get; set; }
    public int? DelayDays { get; set; }
    public int? PlannedDurationDays { get; set; }
    public int? ActualDurationDays { get; set; }
    public double? ActualDurationHours { get; set; }
    public int? StartDelayDays { get; set; }
    public string DataQuality { get; set; } = "Estimated";
    public Dictionary<string, double> TimeByStatusHours { get; set; } = new();
    public string CompletionSource { get; set; } = string.Empty;
}
