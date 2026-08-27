namespace AgencyFlow.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public Guid? ClientCompanyId { get; set; }
    public string? ClientCompanyName { get; set; }
}