using System.Security.Claims;
using AgencyFlow.Data;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Authorization;

public sealed class ResourceAuthorizationService(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor)
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var id))
                throw new UnauthorizedAccessException("El token no contiene un usuario valido.");
            return id;
        }
    }

    public string Role => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException("El token no contiene un rol valido.");

    public bool IsAdministrator => Role is RoleNames.Manager or RoleNames.SuperUser;
    public bool IsDirector => Role == RoleNames.Director;
    public bool IsOperationalUser => Role is "Operativo 1" or "Operativo 2" or "Pasante";

    public IQueryable<Project> FilterProjects(IQueryable<Project> query) =>
        IsAdministrator ? query : query.Where(project =>
            db.ProjectDirectorAccesses.Any(access => access.ProjectId == project.Id &&
                access.DirectorUserId == UserId && access.DeletedAt == null));

    public IQueryable<SubProject> FilterSubProjects(IQueryable<SubProject> query) =>
        IsAdministrator ? query : query.Where(subProject =>
            db.ProjectDirectorAccesses.Any(access => access.ProjectId == subProject.ProjectId &&
                access.DirectorUserId == UserId && access.DeletedAt == null) &&
            db.DepartmentUsers.Any(membership => membership.DepartmentId == subProject.DepartmentId &&
                membership.UserId == UserId && membership.IsDirector && membership.DeletedAt == null));

    public IQueryable<TaskItem> FilterTasks(IQueryable<TaskItem> query) =>
        IsAdministrator ? query : IsOperationalUser
            ? query.Where(task => task.AssignedUserId == UserId)
            : query.Where(task =>
            db.ProjectDirectorAccesses.Any(access => access.ProjectId == task.SubProject.ProjectId &&
                access.DirectorUserId == UserId && access.DeletedAt == null) &&
            db.DepartmentUsers.Any(membership => membership.DepartmentId == task.SubProject.DepartmentId &&
                membership.UserId == UserId && membership.IsDirector && membership.DeletedAt == null));

    public IQueryable<SubTask> FilterSubTasks(IQueryable<SubTask> query) =>
        IsAdministrator ? query : IsOperationalUser
            ? query.Where(subTask => subTask.AssignedUserId == UserId)
            : query.Where(subTask =>
            db.ProjectDirectorAccesses.Any(access => access.ProjectId == subTask.TaskItem.SubProject.ProjectId &&
                access.DirectorUserId == UserId && access.DeletedAt == null) &&
            db.DepartmentUsers.Any(membership => membership.DepartmentId == subTask.TaskItem.SubProject.DepartmentId &&
                membership.UserId == UserId && membership.IsDirector && membership.DeletedAt == null));

    public IQueryable<TaskStatusHistory> FilterTaskHistories(
        IQueryable<TaskStatusHistory> query) =>
        IsAdministrator ? query : query.Where(history =>
            db.ProjectDirectorAccesses.Any(access =>
                access.ProjectId == history.Task.SubProject.ProjectId &&
                access.DirectorUserId == UserId && access.DeletedAt == null) &&
            db.DepartmentUsers.Any(membership =>
                membership.DepartmentId == history.Task.SubProject.DepartmentId &&
                membership.UserId == UserId && membership.IsDirector &&
                membership.DeletedAt == null));

    public IQueryable<SubTaskStatusHistory> FilterSubTaskHistories(
        IQueryable<SubTaskStatusHistory> query) =>
        IsAdministrator ? query : query.Where(history =>
            db.ProjectDirectorAccesses.Any(access =>
                access.ProjectId == history.SubTask.TaskItem.SubProject.ProjectId &&
                access.DirectorUserId == UserId && access.DeletedAt == null) &&
            db.DepartmentUsers.Any(membership =>
                membership.DepartmentId == history.SubTask.TaskItem.SubProject.DepartmentId &&
                membership.UserId == UserId && membership.IsDirector &&
                membership.DeletedAt == null));

    public async Task EnsureProjectAsync(Guid projectId)
    {
        if (IsAdministrator) return;
        if (!IsDirector || !await db.ProjectDirectorAccesses.AnyAsync(a => a.ProjectId == projectId && a.DirectorUserId == UserId && a.DeletedAt == null))
            throw new ForbiddenException("No tiene acceso a este proyecto.");
    }

    public async Task EnsureAreaAsync(Guid projectId, Guid departmentId)
    {
        await EnsureProjectAsync(projectId);
        if (IsAdministrator) return;
        if (!await db.DepartmentUsers.AnyAsync(d => d.UserId == UserId && d.DepartmentId == departmentId && d.IsDirector && d.DeletedAt == null))
            throw new ForbiddenException("No dirige el area de este recurso.");
    }

    public async Task EnsureAssigneeAsync(Guid? userId, Guid departmentId)
    {
        if (!userId.HasValue) return;
        var eligible = await db.Users.AnyAsync(user => user.Id == userId && user.DeletedAt == null &&
            user.Role.DeletedAt == null && user.Role.Name != "Cliente" &&
            (IsAdministrator || user.DepartmentUsers.Any(m => m.DepartmentId == departmentId && m.DeletedAt == null)));
        if (!eligible) throw new BusinessValidationException("El responsable indicado no pertenece al equipo activo del area.");
    }
}
