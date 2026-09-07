namespace AgencyFlow.DTOs.User;

public sealed class ExistingTeamMemberCandidateDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool BelongsToOtherDepartments { get; set; }
}
