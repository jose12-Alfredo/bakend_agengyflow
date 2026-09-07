using System.ComponentModel.DataAnnotations;

namespace AgencyFlow.DTOs.User;

public class LoginDto
{
    [Required]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
