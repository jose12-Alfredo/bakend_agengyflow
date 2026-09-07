namespace AgencyFlow.DTOs.User;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<string> Departments { get; set; } = new();
}