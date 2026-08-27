namespace AgencyFlow.DTOs.ClientCompany;

public class ClientCompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? RepresentativeUserId { get; set; }
    public string? RepresentativeUserName { get; set; }
}
