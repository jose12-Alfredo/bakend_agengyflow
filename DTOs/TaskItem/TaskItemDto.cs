namespace AgencyFlow.DTOs.TaskItem;

public class TaskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public Guid SubProjectId { get; set; }
    public string SubProjectTitle { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;

    public Guid? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }

    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserFirstName { get; set; }
    public string? AssignedUserLastName { get; set; }

    public int SubTaskCount { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public int ProgressPercentage { get; set; }
    public int? OwnProgressPercentage { get; set; }
}
