using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public string? Url { get; set; }
    public string? DedupeKey { get; set; }
    public DateTime? ReadAt { get; set; }
}
