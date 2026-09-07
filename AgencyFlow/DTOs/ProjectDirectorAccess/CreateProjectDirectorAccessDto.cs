using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.ProjectDirectorAccess;

public sealed class CreateProjectDirectorAccessDto
{
    [Required] public Guid ProjectId { get; set; }
    [Required] public Guid DirectorUserId { get; set; }
}
