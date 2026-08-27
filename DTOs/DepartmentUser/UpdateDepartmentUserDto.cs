using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.DepartmentUser;

public class UpdateDepartmentUserDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }
}