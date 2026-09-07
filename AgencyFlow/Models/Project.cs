using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class Project : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "Pendiente";
    public DateTime? ActualStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Tipo de proyecto: obligatorio
    public Guid ProjectTypeId { get; set; }
    public ProjectType ProjectType { get; set; } = null!;

    // Usuario cliente que solicita: opcional
    public Guid? ClientUserId { get; set; }
    public User? ClientUser { get; set; }

    public ICollection<SubProject> SubProjects
    { get; set; } = new List<SubProject>();
    public ICollection<ProjectStatusHistory> StatusHistory { get; set; } = new List<ProjectStatusHistory>();
}
