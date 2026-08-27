namespace AgencyFlow.DTOs.SubProject;

public class SubProjectDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;

    public Guid? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }

    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserFirstName { get; set; }
    public string? AssignedUserLastName { get; set; }
}
