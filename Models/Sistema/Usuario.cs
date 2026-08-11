using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models.Sistema;

public class Usuario
{
    public int IdUsuario { get; set; }
    public int? IdIntegrante { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty; // columna [usuario]
    public string? Correo { get; set; }
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public int IdRolApp { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? UltimoAcceso { get; set; }
    public byte IntentosFallidos { get; set; }
    public DateTime? FechaBloqueo { get; set; }

    [ValidateNever] public CatIntegrante? Integrante { get; set; }
    [ValidateNever] public RolAplicacion RolApp { get; set; } = null!;
}
