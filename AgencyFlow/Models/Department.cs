
using AgencyFlow.Models.Common;

namespace AgencyFlow.Models;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}