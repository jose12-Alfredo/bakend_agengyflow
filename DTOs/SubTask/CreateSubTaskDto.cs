using System.ComponentModel.DataAnnotations;
using AgencyFlow.Models;

namespace AgencyFlow.DTOs.SubTask;

public class CreateSubTaskDto : IValidatableObject
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    [Required]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = SubTaskStatuses.Pending;
    [Required]
    public Guid TaskItemId { get; set; }
    public Guid? AssignedUserId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
        {
            yield return new ValidationResult(
                "EndDate no puede ser anterior a StartDate.",
                new[] { nameof(EndDate) });
        }

        if (!SubTaskStatuses.All.Contains(Status))
        {
            yield return new ValidationResult(
                "El estado de la subtarea no es válido.",
                new[] { nameof(Status) });
        }
    }
}
