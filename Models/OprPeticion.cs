using ElectionApp.Models.Sistema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

// Peticion/solicitud de un simpatizante. Por lo general se recaba durante
// un evento, pero no necesariamente -- mismo criterio que OprGastoApoyo:
// siempre pertenece a una campaña (IdCampana, el scope), pero el
// simpatizante y el evento son OPCIONALES. Tiene un estatus
// (CatEstatusPeticion) para dar seguimiento hasta que se concluya o
// cancele; FechaConclusion se llena cuando el estatus cambia a Concluida
// o Cancelada (ver PeticionesController).
public class OprPeticion
{
    public int IdPeticion { get; set; }
    public int IdCampana { get; set; }
    public int? IdIntegranteCampana { get; set; }
    public int? IdEvento { get; set; }
    public int IdEstatusPeticion { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public DateTime? FechaConclusion { get; set; }
    public string? Observaciones { get; set; }
    public int IdUsuarioRegistro { get; set; }

    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public CatIntegranteCampana? IntegranteCampana { get; set; }
    [ValidateNever] public OprEvento? Evento { get; set; }
    [ValidateNever] public CatEstatusPeticion Estatus { get; set; } = null!;
    [ValidateNever] public Usuario UsuarioRegistro { get; set; } = null!;
}
