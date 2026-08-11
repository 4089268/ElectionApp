namespace ElectionApp.Models;

// Catalogo general de ubicacion usado por Opr_Eventos (cp/colonia/municipio/estado).
public class CatUbicacion
{
    public int IdUbicacion { get; set; }
    public string Cp { get; set; } = string.Empty;
    public string Colonia { get; set; } = string.Empty;
    public string? Localidad { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public ICollection<OprEvento> Eventos { get; set; } = new List<OprEvento>();
}
