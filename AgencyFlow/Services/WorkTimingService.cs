using AgencyFlow.Data;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public sealed class WorkTimingService(AppDbContext db)
{
    public static void ApplyTransition(TaskItem item, string previous, string next,
        DateTime at, Guid? actorId)
    {
        if (next == TaskItemStatuses.InProgress && item.ActualStartedAt == null)
            item.ActualStartedAt = at;
        item.CompletedAt = next == TaskItemStatuses.Completed ? at
            : previous == TaskItemStatuses.Completed ? null : item.CompletedAt;
        item.StatusHistory.Add(new TaskStatusHistory
        {
            Id = Guid.Empty,
            TaskId = item.Id, FromStatus = previous, ToStatus = next,
            Timestamp = at, ChangedByUserId = actorId
        });
    }

    public static void ApplyTransition(SubTask item, string previous, string next,
        DateTime at, Guid? actorId)
    {
        if (next == SubTaskStatuses.InProgress && item.ActualStartedAt == null)
            item.ActualStartedAt = at;
        item.CompletedAt = next == SubTaskStatuses.Completed ? at
            : previous == SubTaskStatuses.Completed ? null : item.CompletedAt;
        item.StatusHistory.Add(new SubTaskStatusHistory
        {
            Id = Guid.Empty,
            SubTaskId = item.Id, FromStatus = previous, ToStatus = next,
            Timestamp = at, ChangedByUserId = actorId
        });
    }

    public async Task SyncHierarchyAsync(Guid taskId, DateTime at, Guid? actorId)
    {
        var task = await db.TaskItems.Include(item => item.SubTasks.Where(sub => sub.DeletedAt == null))
            .Include(item => item.StatusHistory)
            .FirstAsync(item => item.Id == taskId);
        var subProject = await db.SubProjects.Include(item => item.StatusHistory)
            .Include(item => item.Project).ThenInclude(project => project.StatusHistory)
            .FirstAsync(item => item.Id == task.SubProjectId);

        SyncSubProject(subProject, await db.TaskItems.Where(item => item.SubProjectId == subProject.Id && item.DeletedAt == null)
            .Select(item => item.Status).ToListAsync(), at, actorId);
        var project = subProject.Project;
        SyncProject(project, await db.SubProjects.Where(item => item.ProjectId == project.Id && item.DeletedAt == null)
            .Select(item => item.Status).ToListAsync(), at, actorId);
        await db.SaveChangesAsync();
    }

    private static void SyncSubProject(SubProject item, IReadOnlyCollection<string> children,
        DateTime at, Guid? actorId)
    {
        var next = Aggregate(children, TaskItemStatuses.Completed, TaskItemStatuses.InProgress);
        if (next == item.Status) return;
        var previous = item.Status;
        item.Status = next;
        if (next == TaskItemStatuses.InProgress && item.ActualStartedAt == null) item.ActualStartedAt = at;
        item.CompletedAt = next == TaskItemStatuses.Completed ? at : null;
        item.StatusHistory.Add(new SubProjectStatusHistory
            { Id = Guid.Empty, SubProjectId = item.Id, FromStatus = previous, ToStatus = next, Timestamp = at, ChangedByUserId = actorId });
    }

    private static void SyncProject(Project item, IReadOnlyCollection<string> children,
        DateTime at, Guid? actorId)
    {
        var next = Aggregate(children, TaskItemStatuses.Completed, TaskItemStatuses.InProgress);
        if (next == item.Status) return;
        var previous = item.Status;
        item.Status = next;
        if (next == TaskItemStatuses.InProgress && item.ActualStartedAt == null) item.ActualStartedAt = at;
        item.CompletedAt = next == TaskItemStatuses.Completed ? at : null;
        item.StatusHistory.Add(new ProjectStatusHistory
            { Id = Guid.Empty, ProjectId = item.Id, FromStatus = previous, ToStatus = next, Timestamp = at, ChangedByUserId = actorId });
    }

    private static string Aggregate(IReadOnlyCollection<string> statuses, string completed, string inProgress) =>
        statuses.Count == 0 ? TaskItemStatuses.Pending
        : statuses.All(status => status is "Completado" or "Completada") ? completed
        : statuses.Any(status => status != "Pendiente") ? inProgress
        : TaskItemStatuses.Pending;
}
