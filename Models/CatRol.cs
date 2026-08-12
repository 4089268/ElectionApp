namespace ElectionApp.Models;

// Rol dentro de la estructura de campaña (simpatizante, jefe de sector,
// jefe de campaña, etc.). NO confundir con Sistema.RolAplicacion.
public class CatRol
{
    public int IdRol { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<CatIntegranteCampana> Afiliaciones { get; set; } = new List<CatIntegranteCampana>();
}
