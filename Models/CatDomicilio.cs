namespace ElectionApp.Models;

public class CatDomicilio
{
    public int IdDomicilio { get; set; }
    public string? Calle { get; set; }
    public string? Numero { get; set; }
    public string? Cp { get; set; }
    public string? Colonia { get; set; }
    public string? Ciudad { get; set; }
    public string? Estado { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public ICollection<CatIntegrante> Integrantes { get; set; } = new List<CatIntegrante>();
}
