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

    public static readonly IReadOnlySet<string> OperatorAllowed = new HashSet<string>
    {
        Pending,
        InProgress,
        InReview,
        Completed
    };

    // Operative users can update the status of their own assigned subtasks.
    // A completed item may be reopened when more work is required.
    public static bool IsAllowedTransition(string current, string next) =>
        OperatorAllowed.Contains(current) && OperatorAllowed.Contains(next);
}
