using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.DepartmentUser;

public class CreateDepartmentUserDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    public bool IsDirector { get; set; }
}
