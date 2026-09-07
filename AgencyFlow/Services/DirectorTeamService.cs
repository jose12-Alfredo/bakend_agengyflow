using AgencyFlow.Authorization;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.DepartmentUser;
using AgencyFlow.DTOs.User;
using AgencyFlow.DTOs.Role;
using AgencyFlow.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgencyFlow.Services;

public sealed class DirectorTeamService(AppDbContext db, ResourceAuthorizationService authorization)
{
    private static readonly string[] AllowedRoles = ["Operativo 1", "Operativo 2", "Pasante"];

    public Task<List<RoleDto>> GetAllowedRolesAsync() => db.Roles
        .AsNoTracking()
        .Where(role => role.DeletedAt == null && AllowedRoles.Contains(role.Name))
        .OrderBy(role => role.Name)
        .Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        })
        .ToListAsync();

    public async Task<UserDto> CreateAsync(CreateTeamMemberDto dto)
    {
        await EnsureDirectedDepartmentAsync(dto.DepartmentId);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.DeletedAt == null);
        if (role == null) throw new BusinessValidationException("El rol indicado no existe.");
        if (!AllowedRoles.Contains(role.Name)) throw new ForbiddenException("Un Director solo puede crear Operativo 1, Operativo 2 o Pasante.");
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
        if (email != null && await db.Users.AnyAsync(u => u.Email == email && u.DeletedAt == null))
            throw new BusinessValidationException("Ya existe un usuario con ese correo.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new Models.User { FirstName = dto.FirstName, LastName = dto.LastName, Phone = dto.Phone,
            Email = email, Password = dto.Password == null ? null : BCrypt.Net.BCrypt.HashPassword(dto.Password), RoleId = role.Id };
        db.Users.Add(user);
        db.DepartmentUsers.Add(new Models.DepartmentUser { User = user, DepartmentId = dto.DepartmentId, IsDirector = false });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new UserDto { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Phone = user.Phone,
            Email = user.Email, RoleId = role.Id, RoleName = role.Name };
    }

    public async Task<PagedResultDto<ExistingTeamMemberCandidateDto>> GetCandidatesAsync(
        Guid departmentId,
        string? search,
        int page,
        int pageSize)
    {
        await EnsureDirectedDepartmentAsync(departmentId);

        var query = db.Users
            .AsNoTracking()
            .Where(user =>
                user.DeletedAt == null &&
                user.Role.DeletedAt == null &&
                AllowedRoles.Contains(user.Role.Name) &&
                !user.DepartmentUsers.Any(membership =>
                    membership.DepartmentId == departmentId &&
                    membership.DeletedAt == null));

        var normalizedSearch = search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.FirstName.ToLower().Contains(normalizedSearch) ||
                user.LastName.ToLower().Contains(normalizedSearch) ||
                (user.Email != null && user.Email.ToLower().Contains(normalizedSearch)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new ExistingTeamMemberCandidateDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
                BelongsToOtherDepartments = user.DepartmentUsers.Any(membership =>
                    membership.DeletedAt == null)
            })
            .ToListAsync();

        return new PagedResultDto<ExistingTeamMemberCandidateDto>
        {
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<(DepartmentUserDto? Result, bool Conflict)> AddExistingAsync(
        AddExistingTeamMemberDto dto)
    {
        var department = await EnsureDirectedDepartmentAsync(dto.DepartmentId);
        var user = await db.Users
            .Include(candidate => candidate.Role)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == dto.UserId &&
                candidate.DeletedAt == null &&
                candidate.Role.DeletedAt == null);

        if (user == null)
            throw new BusinessValidationException("El usuario indicado no existe o no esta activo.");
        if (!AllowedRoles.Contains(user.Role.Name))
            throw new ForbiddenException("Un Director solo puede agregar Operativo 1, Operativo 2 o Pasante.");

        var activeMembershipExists = await db.DepartmentUsers.AnyAsync(membership =>
            membership.UserId == dto.UserId &&
            membership.DepartmentId == dto.DepartmentId &&
            membership.DeletedAt == null);
        if (activeMembershipExists) return (null, true);

        var membership = await db.DepartmentUsers
            .Where(item =>
                item.UserId == dto.UserId &&
                item.DepartmentId == dto.DepartmentId &&
                item.DeletedAt != null)
            .OrderByDescending(item => item.DeletedAt)
            .FirstOrDefaultAsync();

        if (membership == null)
        {
            membership = new Models.DepartmentUser
            {
                UserId = user.Id,
                DepartmentId = department.Id,
                IsDirector = false,
                CreatedBy = authorization.UserId.ToString()
            };
            db.DepartmentUsers.Add(membership);
        }
        else
        {
            membership.IsDirector = false;
            membership.DeletedAt = null;
            membership.DeletedBy = null;
            membership.UpdatedAt = DateTime.UtcNow;
            membership.UpdatedBy = authorization.UserId.ToString();
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return (null, true);
        }

        return (new DepartmentUserDto
        {
            Id = membership.Id,
            UserId = user.Id,
            UserFirstName = user.FirstName,
            UserLastName = user.LastName,
            UserRoleName = user.Role.Name,
            DepartmentId = department.Id,
            DepartmentName = department.Name,
            IsDirector = false
        }, false);
    }

    private async Task<Models.Department> EnsureDirectedDepartmentAsync(Guid departmentId)
    {
        if (!authorization.IsDirector)
            throw new ForbiddenException("Solo un Director puede usar este flujo.");

        var department = await db.Departments
            .FirstOrDefaultAsync(item => item.Id == departmentId && item.DeletedAt == null);
        if (department == null)
            throw new BusinessValidationException("El area indicada no existe.");

        var directsDepartment = await db.DepartmentUsers.AnyAsync(membership =>
            membership.UserId == authorization.UserId &&
            membership.DepartmentId == departmentId &&
            membership.IsDirector &&
            membership.DeletedAt == null);
        if (!directsDepartment)
            throw new ForbiddenException("No dirige el area indicada.");

        return department;
    }
}
