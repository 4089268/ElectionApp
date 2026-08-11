namespace ElectionApp.Models;

public class CatEstadoCivil
{
    public int IdEstadoCivil { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<CatIntegrante> Integrantes { get; set; } = new List<CatIntegrante>();
}
