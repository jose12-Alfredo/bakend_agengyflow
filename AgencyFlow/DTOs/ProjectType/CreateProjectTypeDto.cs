using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.ProjectType;

public class CreateProjectTypeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}