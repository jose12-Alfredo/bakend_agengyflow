namespace AgencyFlow.Models;

public class SubTaskStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubTaskId { get; set; }
    public SubTask SubTask { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
