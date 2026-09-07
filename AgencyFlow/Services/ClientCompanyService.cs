using Microsoft.EntityFrameworkCore;
using AgencyFlow.Data;
using AgencyFlow.DTOs.ClientCompany;
using AgencyFlow.DTOs.Common;
using AgencyFlow.Exceptions;

namespace AgencyFlow.Services;

public class ClientCompanyService(AppDbContext db)
{
    public async Task<PagedResultDto<ClientCompanyDto>> GetAllAsync(
        int page,
        int pageSize,
        string? nombre,
        string? email)
    {
        var query = db.ClientCompanies
            .AsNoTracking()
            .Include(c => c.RepresentativeUser)
            .Where(c => c.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(c =>
                c.Name.ToLower().Contains(nombre.ToLower()));

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(c =>
                c.Email != null &&
                c.Email.ToLower().Contains(email.ToLower()));

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientCompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Email = c.Email,
                Phone = c.Phone,
                RepresentativeUserId = c.RepresentativeUserId,
                RepresentativeUserName = c.RepresentativeUser == null
                    ? null
                    : c.RepresentativeUser.FirstName + " " + c.RepresentativeUser.LastName
            })
            .ToListAsync();

        return new PagedResultDto<ClientCompanyDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<ClientCompanyDto?> GetByIdAsync(Guid id)
    {
        var company = await db.ClientCompanies
            .AsNoTracking()
            .Include(c => c.RepresentativeUser)
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.DeletedAt == null);

        if (company == null)
            return null;

        return new ClientCompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            Email = company.Email,
            Phone = company.Phone,
            RepresentativeUserId = company.RepresentativeUserId,
            RepresentativeUserName = company.RepresentativeUser == null
                ? null
                : company.RepresentativeUser.FirstName + " " + company.RepresentativeUser.LastName
        };
    }

    public async Task<ClientCompanyDto> CreateAsync(
        CreateClientCompanyDto dto)
    {
        await ValidateRepresentativeAsync(dto.RepresentativeUserId);
        var company = new Models.ClientCompany
        {
            Name = dto.Name,
            Description = dto.Description,
            Email = dto.Email,
            Phone = dto.Phone,
            RepresentativeUserId = dto.RepresentativeUserId
        };

        db.ClientCompanies.Add(company);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(company.Id))!;
    }

    public async Task<ClientCompanyDto?> UpdateAsync(
        Guid id,
        UpdateClientCompanyDto dto)
    {
        var company = await db.ClientCompanies
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.DeletedAt == null);

        if (company == null)
            return null;

        await ValidateRepresentativeAsync(dto.RepresentativeUserId);

        company.Name = dto.Name;
        company.Description = dto.Description;
        company.Email = dto.Email;
        company.Phone = dto.Phone;
        company.RepresentativeUserId = dto.RepresentativeUserId;
        company.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return await GetByIdAsync(company.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await db.ClientCompanies
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.DeletedAt == null);

        if (company == null)
            return false;

        company.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }

    private async Task ValidateRepresentativeAsync(Guid? representativeUserId)
    {
        if (!representativeUserId.HasValue)
            return;

        var isInternalActiveUser = await db.Users.AnyAsync(user =>
            user.Id == representativeUserId.Value &&
            user.DeletedAt == null &&
            user.Role.DeletedAt == null &&
            user.Role.Name != "Cliente");

        if (!isInternalActiveUser)
            throw new BusinessValidationException(
                "El representante indicado no pertenece al equipo activo.");
    }
}
