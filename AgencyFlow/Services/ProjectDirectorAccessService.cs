using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.ProjectDirectorAccess;
using AgencyFlow.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Services;

public sealed class ProjectDirectorAccessService(AppDbContext db)
{
    public async Task<PagedResultDto<ProjectDirectorAccessDto>> GetAllAsync(int page, int pageSize, Guid? projectId, Guid? directorUserId)
    {
        var query = db.ProjectDirectorAccesses.AsNoTracking().Where(a => a.DeletedAt == null);
        if (projectId.HasValue) query = query.Where(a => a.ProjectId == projectId);
        if (directorUserId.HasValue) query = query.Where(a => a.DirectorUserId == directorUserId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ProjectDirectorAccessDto { Id = a.Id, ProjectId = a.ProjectId, ProjectTitle = a.Project.Title,
                DirectorUserId = a.DirectorUserId, DirectorName = a.DirectorUser.FirstName + " " + a.DirectorUser.LastName,
                GrantedByUserId = a.GrantedByUserId, CreatedAt = a.CreatedAt }).ToListAsync();
        return new() { Items = items, TotalItems = total, Page = page, PageSize = pageSize };
    }

    public async Task<(ProjectDirectorAccessDto? Result, bool Conflict)> CreateAsync(CreateProjectDirectorAccessDto dto, Guid grantedByUserId)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == dto.ProjectId && p.DeletedAt == null))
            throw new BusinessValidationException("El proyecto indicado no existe.");
        if (!await db.Users.AnyAsync(u => u.Id == dto.DirectorUserId && u.DeletedAt == null && u.Role.Name == "Director" && u.Role.DeletedAt == null))
            throw new BusinessValidationException("El destinatario debe ser un usuario activo con rol Director.");
        if (await db.ProjectDirectorAccesses.AnyAsync(a => a.ProjectId == dto.ProjectId && a.DirectorUserId == dto.DirectorUserId && a.DeletedAt == null))
            return (null, true);
        var access = new Models.ProjectDirectorAccess { ProjectId = dto.ProjectId, DirectorUserId = dto.DirectorUserId,
            GrantedByUserId = grantedByUserId, CreatedBy = grantedByUserId.ToString() };
        db.Add(access); await db.SaveChangesAsync();
        return ((await GetAllAsync(1, 1, dto.ProjectId, dto.DirectorUserId)).Items.Single(), false);
    }

    public async Task<bool> RevokeAsync(Guid id, Guid revokedByUserId)
    {
        var access = await db.ProjectDirectorAccesses.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);
        if (access == null) return false;
        access.DeletedAt = access.UpdatedAt = DateTime.UtcNow; access.DeletedBy = revokedByUserId.ToString();
        await db.SaveChangesAsync(); return true;
    }
}
