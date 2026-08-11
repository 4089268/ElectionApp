namespace ElectionApp.Models.Sistema;

// Permiso de acceso a la app (administrador, capturista, consulta).
// NO confundir con ElectionApp.Models.CatRol (rol dentro de la campaña).
public class RolAplicacion
{
    public int IdRolApp { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
