using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.SubTask;

public class UpdateSubTaskStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
