using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class ProjectDirectorAccess : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid DirectorUserId { get; set; }
    public User DirectorUser { get; set; } = null!;
    public Guid GrantedByUserId { get; set; }
    public User GrantedByUser { get; set; } = null!;
}
