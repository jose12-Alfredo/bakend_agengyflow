using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.ProjectType;

namespace AgencyFlow.Services;

public class ProjectTypeService(AppDbContext db)
{
    public async Task<PagedResultDto<ProjectTypeDto>> GetAllAsync(
        int page,
        int pageSize,
        string? nombre)
    {
        var query = db.ProjectTypes
            .AsNoTracking()
            .Where(p => p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(p =>
                p.Name.ToLower().Contains(nombre.ToLower()));

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectTypeDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();

        return new PagedResultDto<ProjectTypeDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<ProjectTypeDto?> GetByIdAsync(Guid id)
    {
        var projectType = await db.ProjectTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.DeletedAt == null);

        if (projectType == null)
            return null;

        return new ProjectTypeDto
        {
            Id = projectType.Id,
            Name = projectType.Name,
            Description = projectType.Description
        };
    }

    public async Task<ProjectTypeDto> CreateAsync(
        CreateProjectTypeDto dto)
    {
        var projectType = new Models.ProjectType
        {
            Name = dto.Name,
            Description = dto.Description
        };

        db.ProjectTypes.Add(projectType);
        await db.SaveChangesAsync();

        return new ProjectTypeDto
        {
            Id = projectType.Id,
            Name = projectType.Name,
            Description = projectType.Description
        };
    }

    public async Task<ProjectTypeDto?> UpdateAsync(
        Guid id,
        UpdateProjectTypeDto dto)
    {
        var projectType = await db.ProjectTypes
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.DeletedAt == null);

        if (projectType == null)
            return null;

        projectType.Name = dto.Name;
        projectType.Description = dto.Description;
        projectType.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ProjectTypeDto
        {
            Id = projectType.Id,
            Name = projectType.Name,
            Description = projectType.Description
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var projectType = await db.ProjectTypes
            .FirstOrDefaultAsync(p =>
                p.Id == id && p.DeletedAt == null);

        if (projectType == null)
            return false;

        projectType.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }
}