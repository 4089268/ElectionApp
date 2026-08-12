using System.ComponentModel.DataAnnotations;

namespace ElectionApp.Models.Sistema;

// Forma de captura para Create/Edit de sistema.Usuarios. La contraseña es
// obligatoria solo al crear; en edición, dejarla en blanco conserva la
// contraseña actual (ver UsuariosController).
public class UsuarioFormViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Usuario")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Correo inválido")]
    [StringLength(200)]
    [Display(Name = "Correo")]
    public string? Correo { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "El rol de acceso es obligatorio")]
    [Display(Name = "Rol de acceso")]
    public int IdRolApp { get; set; }

    [Display(Name = "Integrante vinculado")]
    public int? IdIntegrante { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
