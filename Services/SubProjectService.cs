using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Common;
using AgencyFlow.DTOs.SubProject;
using AgencyFlow.Exceptions;

namespace AgencyFlow.Services;

public class SubProjectService
{
    private readonly AppDbContext _db;

    public SubProjectService(AppDbContext db)
    {
        _db = db;
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
        await ValidateRelationsAsync(
            dto.ProjectId,
            dto.DepartmentId,
            dto.AssignedUserId);

        var sp = new Models.SubProject
        {
            Title = dto.Title,
            Detail = dto.Detail,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            ProjectId = dto.ProjectId,
            DepartmentId = dto.DepartmentId,
            AssignedUserId = dto.AssignedUserId
        };

        _db.SubProjects.Add(sp);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(sp.Id))!;
    }

    public async Task<SubProjectDto?> UpdateAsync(Guid id, UpdateSubProjectDto dto)
    {
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

        sp.Title = dto.Title;
        sp.Detail = dto.Detail;
        sp.StartDate = dto.StartDate;
        sp.EndDate = dto.EndDate;
        sp.Status = dto.Status;
        sp.ProjectId = dto.ProjectId;
        sp.DepartmentId = dto.DepartmentId;
        sp.AssignedUserId = dto.AssignedUserId;
        sp.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(sp.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
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
        }
    }
}
