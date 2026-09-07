using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    // Rol: obligatorio
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Empresa cliente: opcional
    public Guid? ClientCompanyId { get; set; }
    public ClientCompany? ClientCompany { get; set; }

    public ICollection<DepartmentUser> DepartmentUsers
    { get; set; } = new List<DepartmentUser>();
}