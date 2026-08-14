using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class OprEvento
{
    public int IdEvento { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public int IdCampana { get; set; }
    public int IdUbicacion { get; set; }
    public string? Lugar { get; set; }
    public decimal? CostoEstimado { get; set; }
    public decimal? CostoReal { get; set; }

    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public CatUbicacion Ubicacion { get; set; } = null!;
    [ValidateNever] public ICollection<OprEventoParticipante> Participantes { get; set; } = new List<OprEventoParticipante>();
    [ValidateNever] public ICollection<OprGastoEvento> Gastos { get; set; } = new List<OprGastoEvento>();
    [ValidateNever] public ICollection<OprPeticion> Peticiones { get; set; } = new List<OprPeticion>();
}
