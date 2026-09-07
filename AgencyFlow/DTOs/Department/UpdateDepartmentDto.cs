using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.Department;

public class UpdateDepartmentDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}