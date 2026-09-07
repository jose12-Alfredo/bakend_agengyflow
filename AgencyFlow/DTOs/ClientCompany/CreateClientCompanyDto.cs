using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.ClientCompany;

public class CreateClientCompanyDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? RepresentativeUserId { get; set; }
}
