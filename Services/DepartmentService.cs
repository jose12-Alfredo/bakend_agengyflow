using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.Department;

namespace AgencyFlow.Services;

public class DepartmentService(AppDbContext db)
{
    public async Task<PagedResultDto<DepartmentDto>> GetAllAsync(
        int page,
        int pageSize,
        string? nombre)
    {
        var query = db.Departments
            .AsNoTracking()
            .Where(d => d.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(d =>
                d.Name.ToLower().Contains(nombre.ToLower()));

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description
            })
            .ToListAsync();

        return new PagedResultDto<DepartmentDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var department = await db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == id && d.DeletedAt == null);

        if (department == null)
            return null;

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description
        };
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var department = new Models.Department
        {
            Name = dto.Name,
            Description = dto.Description
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description
        };
    }

    public async Task<DepartmentDto?> UpdateAsync(
        Guid id,
        UpdateDepartmentDto dto)
    {
        var department = await db.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == id && d.DeletedAt == null);

        if (department == null)
            return null;

        department.Name = dto.Name;
        department.Description = dto.Description;
        department.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var department = await db.Departments
            .FirstOrDefaultAsync(d =>
                d.Id == id && d.DeletedAt == null);

        if (department == null)
            return false;

        department.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }
}