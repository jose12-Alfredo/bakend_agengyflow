namespace AgencyFlow.DTOs.SubTask;

public class SubTaskDto
{
    public DateTime? ActualStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public int CommentCount { get; set; }

    public Guid TaskItemId { get; set; }
    public string TaskItemTitle { get; set; } = string.Empty;

    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserFirstName { get; set; }
    public string? AssignedUserLastName { get; set; }
}
