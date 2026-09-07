using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.SubTaskComment;

public class CreateSubTaskCommentDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}
