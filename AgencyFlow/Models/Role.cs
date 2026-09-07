using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}