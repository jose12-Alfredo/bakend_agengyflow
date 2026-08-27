using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.TaskItem;

public class UpdateTaskItemDto : IValidatableObject
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    [Required]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    [Required]
    public string Status { get; set; } = string.Empty;
    [Required]
    public Guid SubProjectId { get; set; }
    public Guid? AssignedUserId { get; set; }
    [Range(0, 100)]
    public int? OwnProgressPercentage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
        {
            yield return new ValidationResult(
                "EndDate no puede ser anterior a StartDate.",
                new[] { nameof(EndDate) });
        }
    }
}
