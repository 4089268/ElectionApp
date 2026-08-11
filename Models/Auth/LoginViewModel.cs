using System.ComponentModel.DataAnnotations;

namespace ElectionApp.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
