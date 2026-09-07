using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.User;

public sealed class AddExistingTeamMemberDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }
}
