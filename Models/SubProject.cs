using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class SubProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = "Pendiente";

    // Proyecto al que pertenece: obligatorio
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Departamento responsable: obligatorio
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // Usuario asignado: opcional
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
}