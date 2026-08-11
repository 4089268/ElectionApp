using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class OprEventoParticipante
{
    public int IdEventoParticipante { get; set; }
    public int IdEvento { get; set; }
    public int IdIntegrante { get; set; }
    public string? Rol { get; set; } // rol en el evento: asistente, organizador, orador, etc.
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ValidateNever] public OprEvento Evento { get; set; } = null!;
    [ValidateNever] public CatIntegrante Integrante { get; set; } = null!;
}
