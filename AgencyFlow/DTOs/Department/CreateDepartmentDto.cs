using System.ComponentModel.DataAnnotations;
namespace AgencyFlow.DTOs.Department;
public class CreateDepartmentDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}