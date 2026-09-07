namespace AgencyFlow.Models;

public class SubProjectStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubProjectId { get; set; }
    public SubProject SubProject { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? ChangedByUserId { get; set; }
}
