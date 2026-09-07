namespace AgencyFlow.DTOs.ProjectDirectorAccess;

public sealed class ProjectDirectorAccessDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public Guid DirectorUserId { get; set; }
    public string DirectorName { get; set; } = string.Empty;
    public Guid GrantedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
