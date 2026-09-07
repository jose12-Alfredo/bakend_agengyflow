namespace AgencyFlow.DTOs.DepartmentUser;

public class DepartmentUserDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public string UserFirstName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public string UserRoleName { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsDirector { get; set; }
}
