using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class ClientCompany : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Responsable interno de la cuenta; nunca se infiere desde el trabajo.
    public Guid? RepresentativeUserId { get; set; }
    public User? RepresentativeUser { get; set; }
}
