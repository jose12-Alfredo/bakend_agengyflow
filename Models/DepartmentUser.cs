using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class DepartmentUser : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}