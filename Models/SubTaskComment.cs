using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class SubTaskComment : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public Guid SubTaskId { get; set; }
    public SubTask SubTask { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
