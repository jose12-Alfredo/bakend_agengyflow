using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.SubProject;

public class UpdateSubProjectDto : IValidatableObject
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
    public Guid ProjectId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    public Guid? AssignedUserId { get; set; }

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
