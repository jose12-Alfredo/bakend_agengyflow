using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.SubProject;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;

namespace AgencyFlow.Services;

public class SubProjectService
{
    private readonly AppDbContext _db;
    private readonly ResourceAuthorizationService _authorization;

    public SubProjectService(AppDbContext db, ResourceAuthorizationService authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<PagedResultDto<SubProjectDto>> GetAllAsync(
        int page, int pageSize,
        string? titulo, Guid? projectId, Guid? departmentId,
        string? status, Guid? assignedUserId)
    {
        var query = _db.SubProjects
            .AsNoTracking()
            .Include(sp => sp.Project)
            .Include(sp => sp.Department)
            .Include(sp => sp.AssignedUser)
            .Where(sp =>
                sp.DeletedAt == null &&
                sp.Project.DeletedAt == null);
        query = _authorization.FilterSubProjects(query);

        if (!string.IsNullOrWhiteSpace(titulo))
            query = query.Where(sp => sp.Title.ToLower().Contains(titulo.ToLower()));

        if (projectId.HasValue)
            query = query.Where(sp => sp.ProjectId == projectId.Value);

        if (departmentId.HasValue)
            query = query.Where(sp => sp.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sp => sp.Status == status);

        if (assignedUserId.HasValue)
            query = query.Where(sp => sp.AssignedUserId == assignedUserId.Value);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new SubProjectDto
            {
                Id = sp.Id,
                Title = sp.Title,
                Detail = sp.Detail,
                StartDate = sp.StartDate,
                EndDate = sp.EndDate,
                Status = sp.Status,
                ProjectId = sp.ProjectId,
                ProjectTitle = sp.Project.Title,
                ClientCompanyId = sp.Project.ClientUser != null
                    ? sp.Project.ClientUser.ClientCompanyId
                    : null,
                ClientCompanyName = sp.Project.ClientUser != null &&
                    sp.Project.ClientUser.ClientCompany != null
                        ? sp.Project.ClientUser.ClientCompany.Name
                        : null,
                DepartmentId = sp.DepartmentId,
                DepartmentName = sp.Department.Name,
                AssignedUserId = sp.AssignedUserId,
                AssignedUserFirstName = sp.AssignedUser != null ? sp.AssignedUser.FirstName : null,
                AssignedUserLastName = sp.AssignedUser != null ? sp.AssignedUser.LastName : null
            })
            .ToListAsync();

        return new PagedResultDto<SubProjectDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<SubProjectDto?> GetByIdAsync(Guid id)
    {
        var scope = await _db.SubProjects.Where(s => s.Id == id && s.DeletedAt == null)
            .Select(s => new { s.ProjectId, s.DepartmentId }).FirstOrDefaultAsync();
        if (scope != null) await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
        var sp = await _db.SubProjects
            .AsNoTracking()
            .Include(s => s.Project)
            .ThenInclude(project => project.ClientUser)
            .ThenInclude(clientUser => clientUser!.ClientCompany)
            .Include(s => s.Department)
            .Include(s => s.AssignedUser)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.Project.DeletedAt == null);

        if (sp == null) return null;

        return new SubProjectDto
        {
            Id = sp.Id,
            Title = sp.Title,
            Detail = sp.Detail,
            StartDate = sp.StartDate,
            EndDate = sp.EndDate,
            Status = sp.Status,
            ProjectId = sp.ProjectId,
            ProjectTitle = sp.Project.Title,
            ClientCompanyId = sp.Project.ClientUser?.ClientCompanyId,
            ClientCompanyName = sp.Project.ClientUser?.ClientCompany?.Name,
            DepartmentId = sp.DepartmentId,
            DepartmentName = sp.Department.Name,
            AssignedUserId = sp.AssignedUserId,
            AssignedUserFirstName = sp.AssignedUser?.FirstName,
            AssignedUserLastName = sp.AssignedUser?.LastName
        };
    }

    public async Task<SubProjectDto> CreateAsync(CreateSubProjectDto dto)
    {
        await _authorization.EnsureAreaAsync(dto.ProjectId, dto.DepartmentId);
        var assignedUserId = _authorization.IsDirector
            ? _authorization.UserId
            : dto.AssignedUserId;
        await ValidateRelationsAsync(
            dto.ProjectId,
            dto.DepartmentId,
            assignedUserId);

        var sp = new Models.SubProject
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            ProjectId = dto.ProjectId,
            DepartmentId = dto.DepartmentId,
            AssignedUserId = assignedUserId
        };

        _db.SubProjects.Add(sp);
        var now = DateTime.UtcNow;
        if (sp.Status != "Pendiente") sp.ActualStartedAt = now;
        if (sp.Status is "Completado" or "Completada") sp.CompletedAt = now;
        _db.SubProjectStatusHistories.Add(new Models.SubProjectStatusHistory
        {
            SubProjectId = sp.Id, FromStatus = null, ToStatus = sp.Status,
            Timestamp = now, ChangedByUserId = _authorization.UserId
        });
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(sp.Id))!;
    }

    public async Task<SubProjectDto?> UpdateAsync(Guid id, UpdateSubProjectDto dto)
    {
        var currentScope = await _db.SubProjects.Where(s => s.Id == id && s.DeletedAt == null).Select(s => new { s.ProjectId, s.DepartmentId }).FirstOrDefaultAsync();
        if (currentScope != null) await _authorization.EnsureAreaAsync(currentScope.ProjectId, currentScope.DepartmentId);
        await _authorization.EnsureAreaAsync(dto.ProjectId, dto.DepartmentId);
        var sp = await _db.SubProjects
            .Include(s => s.Project)
            .Include(s => s.Department)
            .Include(s => s.AssignedUser)
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.Project.DeletedAt == null);

        if (sp == null) return null;

        await ValidateRelationsAsync(
            dto.ProjectId,
            dto.DepartmentId,
            dto.AssignedUserId);

        var previousStatus = sp.Status;
        sp.Title = dto.Title;
        sp.Detail = dto.Detail;
        sp.StartDate = dto.StartDate;
        sp.EndDate = dto.EndDate;
        sp.Status = dto.Status;
        sp.ProjectId = dto.ProjectId;
        sp.DepartmentId = dto.DepartmentId;
        sp.AssignedUserId = dto.AssignedUserId;
        sp.UpdatedAt = DateTime.UtcNow;

        if (previousStatus != sp.Status)
        {
            if (sp.Status != "Pendiente" && sp.ActualStartedAt == null)
                sp.ActualStartedAt = sp.UpdatedAt;
            sp.CompletedAt = sp.Status is "Completado" or "Completada"
                ? sp.UpdatedAt : null;
            _db.SubProjectStatusHistories.Add(new Models.SubProjectStatusHistory
            {
                SubProjectId = sp.Id, FromStatus = previousStatus, ToStatus = sp.Status,
                Timestamp = sp.UpdatedAt.Value, ChangedByUserId = _authorization.UserId
            });
        }

        await _db.SaveChangesAsync();

        return await GetByIdAsync(sp.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var scope = await _db.SubProjects.Where(s => s.Id == id && s.DeletedAt == null).Select(s => new { s.ProjectId, s.DepartmentId }).FirstOrDefaultAsync();
        if (scope != null) await _authorization.EnsureAreaAsync(scope.ProjectId, scope.DepartmentId);
        var sp = await _db.SubProjects
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DeletedAt == null &&
                s.Project.DeletedAt == null);

        if (sp == null) return false;

        sp.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateRelationsAsync(
        Guid projectId,
        Guid departmentId,
        Guid? assignedUserId)
    {
        var projectExists = await _db.Projects.AnyAsync(p =>
            p.Id == projectId && p.DeletedAt == null);

        if (!projectExists)
            throw new BusinessValidationException(
                "El proyecto indicado no existe.");

        var departmentExists = await _db.Departments.AnyAsync(d =>
            d.Id == departmentId && d.DeletedAt == null);

        if (!departmentExists)
            throw new BusinessValidationException(
                "El departamento indicado no existe.");

        if (assignedUserId.HasValue)
        {
            var userExists = await _db.Users.AnyAsync(u =>
                u.Id == assignedUserId.Value && u.DeletedAt == null);

            if (!userExists)
                throw new BusinessValidationException(
                    "El usuario asignado no existe.");
            await _authorization.EnsureAssigneeAsync(assignedUserId, departmentId);
        }
    }
}
