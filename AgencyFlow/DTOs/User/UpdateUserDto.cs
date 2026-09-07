using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.User;

public class UpdateUserDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    public string? Email { get; set; }

    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    public Guid? ClientCompanyId { get; set; }
}
