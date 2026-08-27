using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.DepartmentUser;
using AgencyFlow.Exceptions;

namespace AgencyFlow.Services;

public class DepartmentUserService(AppDbContext db)
{
    public async Task<PagedResultDto<DepartmentUserDto>> GetAllAsync(
        int page,
        int pageSize,
        Guid? userId,
        Guid? departmentId)
    {
        var query = db.DepartmentUsers
            .AsNoTracking()
            .Include(du => du.User)
            .Include(du => du.Department)
            .Where(du => du.DeletedAt == null);

        if (userId.HasValue)
        {
            query = query.Where(du =>
                du.UserId == userId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(du =>
                du.DepartmentId == departmentId.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(du => new DepartmentUserDto
            {
                Id = du.Id,
                UserId = du.UserId,
                UserFirstName = du.User.FirstName,
                UserLastName = du.User.LastName,
                DepartmentId = du.DepartmentId,
                DepartmentName = du.Department.Name
            })
            .ToListAsync();

        return new PagedResultDto<DepartmentUserDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<DepartmentUserDto?> GetByIdAsync(Guid id)
    {
        var departmentUser = await db.DepartmentUsers
            .AsNoTracking()
            .Include(du => du.User)
            .Include(du => du.Department)
            .FirstOrDefaultAsync(du =>
                du.Id == id &&
                du.DeletedAt == null);

        if (departmentUser == null)
            return null;

        return new DepartmentUserDto
        {
            Id = departmentUser.Id,
            UserId = departmentUser.UserId,
            UserFirstName = departmentUser.User.FirstName,
            UserLastName = departmentUser.User.LastName,
            DepartmentId = departmentUser.DepartmentId,
            DepartmentName = departmentUser.Department.Name
        };
    }
    public async Task<(
        DepartmentUserDto? result,
        bool conflict)> CreateAsync(
        CreateDepartmentUserDto dto)
    {
        await ValidateRelationsAsync(dto.UserId, dto.DepartmentId);

        var exists = await db.DepartmentUsers
            .AnyAsync(du =>
                du.UserId == dto.UserId &&
                du.DepartmentId == dto.DepartmentId &&
                du.DeletedAt == null);

        if (exists)
            return (null, true);

        var departmentUser = new Models.DepartmentUser
        {
            UserId = dto.UserId,
            DepartmentId = dto.DepartmentId
        };

        db.DepartmentUsers.Add(departmentUser);
        await db.SaveChangesAsync();

        await db.Entry(departmentUser)
            .Reference(du => du.User)
            .LoadAsync();

        await db.Entry(departmentUser)
            .Reference(du => du.Department)
            .LoadAsync();

        return (new DepartmentUserDto
        {
            Id = departmentUser.Id,
            UserId = departmentUser.UserId,
            UserFirstName = departmentUser.User.FirstName,
            UserLastName = departmentUser.User.LastName,
            DepartmentId = departmentUser.DepartmentId,
            DepartmentName = departmentUser.Department.Name
        }, false);
    }
    public async Task<DepartmentUserDto?> UpdateAsync(
        Guid id,
        UpdateDepartmentUserDto dto)
    {
        var departmentUser = await db.DepartmentUsers
            .Include(du => du.User)
            .Include(du => du.Department)
            .FirstOrDefaultAsync(du =>
                du.Id == id &&
                du.DeletedAt == null);

        if (departmentUser == null)
            return null;

        await ValidateRelationsAsync(dto.UserId, dto.DepartmentId);

        departmentUser.UserId = dto.UserId;
        departmentUser.DepartmentId = dto.DepartmentId;
        departmentUser.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await db.Entry(departmentUser)
            .Reference(du => du.User)
            .LoadAsync();

        await db.Entry(departmentUser)
            .Reference(du => du.Department)
            .LoadAsync();

        return new DepartmentUserDto
        {
            Id = departmentUser.Id,
            UserId = departmentUser.UserId,
            UserFirstName = departmentUser.User.FirstName,
            UserLastName = departmentUser.User.LastName,
            DepartmentId = departmentUser.DepartmentId,
            DepartmentName = departmentUser.Department.Name
        };
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var departmentUser = await db.DepartmentUsers
            .FirstOrDefaultAsync(du =>
                du.Id == id &&
                du.DeletedAt == null);

        if (departmentUser == null)
            return false;

        departmentUser.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateRelationsAsync(
        Guid userId,
        Guid departmentId)
    {
        var userExists = await db.Users.AnyAsync(u =>
            u.Id == userId && u.DeletedAt == null);

        if (!userExists)
            throw new BusinessValidationException(
                "El usuario indicado no existe.");

        var departmentExists = await db.Departments.AnyAsync(d =>
            d.Id == departmentId && d.DeletedAt == null);

        if (!departmentExists)
            throw new BusinessValidationException(
                "El departamento indicado no existe.");
    }
}
