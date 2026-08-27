namespace AgencyFlow.Models;

public static class SubTaskStatuses
{
    public const string Pending = "Pendiente";
    public const string InProgress = "En proceso";
    public const string InReview = "En revisión";
    public const string Completed = "Completada";
    public const string Paused = "Pausada";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Pending,
        InProgress,
        InReview,
        Completed,
        Paused
    };
}
