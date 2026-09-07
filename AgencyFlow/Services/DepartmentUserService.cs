using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.DepartmentUser;
using AgencyFlow.Exceptions;
using AgencyFlow.Authorization;

namespace AgencyFlow.Services;

public class DepartmentUserService(AppDbContext db, ResourceAuthorizationService? authorization = null)
{
    public async Task<PagedResultDto<DepartmentUserDto>> GetAllAsync(
        int page,
        int pageSize,
        Guid? userId,
        Guid? departmentId,
        bool? isDirector)
    {
        var query = db.DepartmentUsers
            .AsNoTracking()
            .Include(du => du.User)
            .ThenInclude(user => user.Role)
            .Include(du => du.Department)
            .Where(du => du.DeletedAt == null);

        if (authorization != null && !authorization.IsAdministrator)
        {
            if (!authorization.IsDirector)
                throw new ForbiddenException("No tiene permiso para consultar integrantes de departamentos.");

            query = query.Where(du => db.DepartmentUsers.Any(directorship =>
                directorship.UserId == authorization.UserId &&
                directorship.DepartmentId == du.DepartmentId &&
                directorship.IsDirector &&
                directorship.DeletedAt == null));
        }

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

        if (isDirector.HasValue)
        {
            query = query.Where(du =>
                du.IsDirector == isDirector.Value);
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
                UserRoleName = du.User.Role.Name,
                DepartmentId = du.DepartmentId,
                DepartmentName = du.Department.Name,
                IsDirector = du.IsDirector
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
            .ThenInclude(user => user.Role)
            .Include(du => du.Department)
            .FirstOrDefaultAsync(du =>
                du.Id == id &&
                du.DeletedAt == null &&
                (authorization == null || authorization.IsAdministrator || db.DepartmentUsers.Any(directorship =>
                    directorship.UserId == authorization.UserId &&
                    directorship.DepartmentId == du.DepartmentId &&
                    directorship.IsDirector &&
                    directorship.DeletedAt == null)));

        if (departmentUser == null)
            return null;

        return new DepartmentUserDto
        {
            Id = departmentUser.Id,
            UserId = departmentUser.UserId,
            UserFirstName = departmentUser.User.FirstName,
            UserLastName = departmentUser.User.LastName,
            UserRoleName = departmentUser.User.Role.Name,
            DepartmentId = departmentUser.DepartmentId,
            DepartmentName = departmentUser.Department.Name,
            IsDirector = departmentUser.IsDirector
        };
    }
    public async Task<(
        DepartmentUserDto? result,
        bool conflict)> CreateAsync(
        CreateDepartmentUserDto dto)
    {
        await ValidateRelationsAsync(
            dto.UserId, dto.DepartmentId, dto.IsDirector);

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
            DepartmentId = dto.DepartmentId,
            IsDirector = dto.IsDirector
        };

        db.DepartmentUsers.Add(departmentUser);
        await db.SaveChangesAsync();

        await db.Entry(departmentUser)
            .Reference(du => du.User)
            .LoadAsync();

        await db.Entry(departmentUser.User)
            .Reference(user => user.Role)
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
            UserRoleName = departmentUser.User.Role.Name,
            DepartmentId = departmentUser.DepartmentId,
            DepartmentName = departmentUser.Department.Name,
            IsDirector = departmentUser.IsDirector
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

        await ValidateRelationsAsync(
            dto.UserId, dto.DepartmentId, dto.IsDirector);

        departmentUser.UserId = dto.UserId;
        departmentUser.DepartmentId = dto.DepartmentId;
        departmentUser.IsDirector = dto.IsDirector;
        departmentUser.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return await GetByIdAsync(id);
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
        Guid departmentId,
        bool isDirector)
    {
        var userRoleName = await db.Users
            .Where(user =>
                user.Id == userId && user.DeletedAt == null)
            .Select(user => user.Role.Name)
            .FirstOrDefaultAsync();

        if (userRoleName == null)
            throw new BusinessValidationException(
                "El usuario indicado no existe.");

        if (isDirector &&
            !string.Equals(
                userRoleName,
                "Director",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException(
                "Solo un usuario con rol Director puede ser director de un departamento.");
        }

        var departmentExists = await db.Departments.AnyAsync(d =>
            d.Id == departmentId && d.DeletedAt == null);

        if (!departmentExists)
            throw new BusinessValidationException(
                "El departamento indicado no existe.");
    }
}
