using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.Role;

namespace AgencyFlow.Services;

public class RoleService(AppDbContext db)
{
    public async Task<PagedResultDto<RoleDto>> GetAllAsync(int page, int pageSize, string? nombre)
    {
        var query = db.Roles.AsNoTracking().Where(r => r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(r => r.Name.ToLower().Contains(nombre.ToLower()));

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            })
            .ToListAsync();

        return new PagedResultDto<RoleDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (role == null) return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var role = new Models.Role
        {
            Name = dto.Name,
            Description = dto.Description
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }

    public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleDto dto)
    {
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (role == null) return null;

        role.Name = dto.Name;
        role.Description = dto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

        if (role == null) return false;

        role.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }
}