namespace AgencyFlow.DTOs.Project;

public class ProjectFullDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid ProjectTypeId { get; set; }
    public string ProjectTypeName { get; set; } = string.Empty;

    public Guid? ClientUserId { get; set; }
    public string? ClientUserFirstName { get; set; }
    public string? ClientUserLastName { get; set; }
    public Guid? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }

    public List<SubProjectSummaryDto> SubProjects
    { get; set; } = new();
}

public class SubProjectSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? AssignedUserFirstName { get; set; }
    public string? AssignedUserLastName { get; set; }
}
