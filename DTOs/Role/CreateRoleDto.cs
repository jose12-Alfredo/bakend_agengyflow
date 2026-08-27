using System.ComponentModel.DataAnnotations;
namespace AgencyFlow.DTOs.Role;
public class CreateRoleDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}