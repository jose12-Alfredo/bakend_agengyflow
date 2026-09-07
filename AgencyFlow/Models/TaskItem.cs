using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = TaskItemStatuses.Pending;

    // Actividad operativa propia o propagada desde sus subtareas.
    public DateTime? LastActivityAt { get; set; }
    public DateTime? ActualStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Avance manual solo cuando la tarea no tiene subtareas activas.
    public int? OwnProgressPercentage { get; set; }

    // Subproyecto al que pertenece: obligatorio
    public Guid SubProjectId { get; set; }
    public SubProject SubProject { get; set; } = null!;

    // Usuario responsable: opcional
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
    public ICollection<TaskStatusHistory> StatusHistory { get; set; } =
        new List<TaskStatusHistory>();
}
