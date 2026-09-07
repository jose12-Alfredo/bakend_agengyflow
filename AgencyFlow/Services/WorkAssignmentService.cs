using AgencyFlow.Data;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

/// <summary>Centraliza la elegibilidad del responsable y la actividad operativa.</summary>
public class WorkAssignmentService(AppDbContext db)
{
    public const string IneligibleAssigneeMessage =
        "El responsable indicado no pertenece al equipo activo.";

    public async Task ValidateAssigneeAsync(Guid? assignedUserId)
    {
        if (!assignedUserId.HasValue)
            return;

        var isEligible = await db.Users.AnyAsync(user =>
            user.Id == assignedUserId.Value &&
            user.DeletedAt == null &&
            user.Role.DeletedAt == null &&
            user.Role.Name != "Cliente");

        if (!isEligible)
            throw new BusinessValidationException(IneligibleAssigneeMessage);
    }

    public static void EnsureUnassignedWorkIsPending(
        Guid? assignedUserId,
        string status,
        string workItemName)
    {
        if (!assignedUserId.HasValue && status != TaskItemStatuses.Pending)
        {
            throw new BusinessValidationException(
                $"La {workItemName} sin responsable solo puede permanecer en estado Pendiente.");
        }
    }

    public async Task TouchParentTaskAsync(Guid taskItemId, DateTime occurredAt)
    {
        var task = await db.TaskItems.FirstOrDefaultAsync(item =>
            item.Id == taskItemId && item.DeletedAt == null);

        if (task != null)
            task.LastActivityAt = occurredAt;
    }
}
