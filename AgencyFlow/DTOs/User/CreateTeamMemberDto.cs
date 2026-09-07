using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.User;

public sealed class CreateTeamMemberDto : CreateUserDto
{
    [Required] public Guid DepartmentId { get; set; }
}
