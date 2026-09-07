using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.Project;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;

namespace AgencyFlow.Services;

public class ProjectService(AppDbContext db, ResourceAuthorizationService authorization)
{
    public async Task<PagedResultDto<ProjectDto>> GetAllAsync(
        int page,
        int pageSize,
        string? titulo,
        Guid? projectTypeId,
        Guid? clientUserId,
        DateOnly? startDateFrom,
        DateOnly? startDateTo)
    {
        var query = db.Projects
            .AsNoTracking()
            .Include(p => p.ProjectType)
            .Include(p => p.ClientUser)
            .ThenInclude(clientUser => clientUser!.ClientCompany)
            .Where(p => p.DeletedAt == null);
        query = authorization.FilterProjects(query);

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            query = query.Where(p =>
                p.Title.ToLower().Contains(titulo.ToLower()));
        }

        if (projectTypeId.HasValue)
        {
            query = query.Where(p =>
                p.ProjectTypeId == projectTypeId.Value);
        }

        if (clientUserId.HasValue)
        {
            query = query.Where(p =>
                p.ClientUserId == clientUserId.Value);
        }

        if (startDateFrom.HasValue)
        {
            query = query.Where(p =>
                p.StartDate >= startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            query = query.Where(p =>
                p.StartDate <= startDateTo.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                Detail = p.Detail,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                ProjectTypeId = p.ProjectTypeId,
                ProjectTypeName = p.ProjectType.Name,
                ClientUserId = p.ClientUserId,
                ClientUserFirstName = p.ClientUser != null
                    ? p.ClientUser.FirstName
                    : null,
                ClientUserLastName = p.ClientUser != null
                    ? p.ClientUser.LastName
                    : null,
                ClientCompanyId = p.ClientUser != null
                    ? p.ClientUser.ClientCompanyId
                    : null,
                ClientCompanyName = p.ClientUser != null &&
                    p.ClientUser.ClientCompany != null
                        ? p.ClientUser.ClientCompany.Name
                        : null
            })
            .ToListAsync();

        return new PagedResultDto<ProjectDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        if (await db.Projects.AnyAsync(p => p.Id == id && p.DeletedAt == null))
            await authorization.EnsureProjectAsync(id);
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.ProjectType)
            .Include(p => p.ClientUser)
            .ThenInclude(clientUser => clientUser!.ClientCompany)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.DeletedAt == null);

        if (project == null)
            return null;

        return new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            Detail = project.Detail,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectTypeId = project.ProjectTypeId,
            ProjectTypeName = project.ProjectType.Name,
            ClientUserId = project.ClientUserId,
            ClientUserFirstName = project.ClientUser?.FirstName,
            ClientUserLastName = project.ClientUser?.LastName,
            ClientCompanyId = project.ClientUser?.ClientCompanyId,
            ClientCompanyName = project.ClientUser?.ClientCompany?.Name
        };
    }
    public async Task<ProjectFullDto?> GetFullAsync(Guid id)
    {
        if (await db.Projects.AnyAsync(p => p.Id == id && p.DeletedAt == null))
            await authorization.EnsureProjectAsync(id);
        var project = await db.Projects
            .AsNoTracking()
            .Include(p => p.ProjectType)
            .Include(p => p.ClientUser)
            .ThenInclude(clientUser => clientUser!.ClientCompany)
            .Include(p => p.SubProjects
                .Where(sp => sp.DeletedAt == null))
            .ThenInclude(sp => sp.Department)
            .Include(p => p.SubProjects
                .Where(sp => sp.DeletedAt == null))
            .ThenInclude(sp => sp.AssignedUser)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.DeletedAt == null);

        if (project == null)
            return null;

        return new ProjectFullDto
        {
            Id = project.Id,
            Title = project.Title,
            Detail = project.Detail,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectTypeId = project.ProjectTypeId,
            ProjectTypeName = project.ProjectType.Name,
            ClientUserId = project.ClientUserId,
            ClientUserFirstName =
                project.ClientUser?.FirstName,
            ClientUserLastName =
                project.ClientUser?.LastName,
            ClientCompanyId = project.ClientUser?.ClientCompanyId,
            ClientCompanyName = project.ClientUser?.ClientCompany?.Name,

            SubProjects = (authorization.IsAdministrator ? project.SubProjects : project.SubProjects.Where(sp =>
                    db.DepartmentUsers.Any(m => m.UserId == authorization.UserId && m.DepartmentId == sp.DepartmentId && m.IsDirector && m.DeletedAt == null)))
                .Select(sp => new SubProjectSummaryDto
                {
                    Id = sp.Id,
                    Title = sp.Title,
                    Detail = sp.Detail,
                    StartDate = sp.StartDate,
                    EndDate = sp.EndDate,
                    Status = sp.Status,
                    DepartmentName = sp.Department.Name,
                    AssignedUserFirstName =
                        sp.AssignedUser?.FirstName,
                    AssignedUserLastName =
                        sp.AssignedUser?.LastName
                })
                .ToList()
        };
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        await ValidateRelationsAsync(dto.ProjectTypeId, dto.ClientUserId);

        var project = new Models.Project
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ProjectTypeId = dto.ProjectTypeId,
            ClientUserId = dto.ClientUserId
        };

        db.Projects.Add(project);
        db.ProjectStatusHistories.Add(new Models.ProjectStatusHistory
        {
            ProjectId = project.Id, FromStatus = null, ToStatus = project.Status,
            ChangedByUserId = authorization.UserId
        });
        await db.SaveChangesAsync();

        return (await GetByIdAsync(project.Id))!;
    }

    public async Task<ProjectDto?> UpdateAsync(
        Guid id,
        UpdateProjectDto dto)
    {
        var project = await db.Projects
            .Include(p => p.ProjectType)
            .Include(p => p.ClientUser)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.DeletedAt == null);

        if (project == null)
            return null;

        await ValidateRelationsAsync(dto.ProjectTypeId, dto.ClientUserId);

        project.Title = dto.Title;
        project.Detail = dto.Detail;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.ProjectTypeId = dto.ProjectTypeId;
        project.ClientUserId = dto.ClientUserId;
        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return await GetByIdAsync(project.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var project = await db.Projects
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.DeletedAt == null);

        if (project == null)
            return false;

        project.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateRelationsAsync(
        Guid projectTypeId,
        Guid? clientUserId)
    {
        var projectTypeExists = await db.ProjectTypes.AnyAsync(pt =>
            pt.Id == projectTypeId && pt.DeletedAt == null);

        if (!projectTypeExists)
            throw new BusinessValidationException(
                "El tipo de proyecto indicado no existe.");

        if (clientUserId.HasValue)
        {
            var clientUserExists = await db.Users.AnyAsync(u =>
                u.Id == clientUserId.Value && u.DeletedAt == null);

            if (!clientUserExists)
                throw new BusinessValidationException(
                    "El usuario cliente indicado no existe.");
        }
    }
}
