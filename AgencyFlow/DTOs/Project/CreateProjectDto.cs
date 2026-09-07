using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.Project;

public class CreateProjectDto : IValidatableObject
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Required]
    public Guid ProjectTypeId { get; set; }

    public Guid? ClientUserId { get; set; }

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
