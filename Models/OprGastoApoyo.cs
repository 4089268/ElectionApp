using ElectionApp.Models.Sistema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

// Gasto por una ayuda/donacion (despensa, apoyo economico, medicamento,
// etc). Separado de OprGastoEvento a proposito: son dos rubros distintos
// de la contabilidad de la campaña y se llevan por separado. Siempre
// pertenece a una campaña (IdCampana, es el scope que usa toda la app),
// pero el simpatizante beneficiado y el evento en el que se otorgo son
// OPCIONALES: un apoyo puede ser una ayuda general (sin persona asociada)
// y puede o no haberse dado durante un evento concreto.
public class OprGastoApoyo
{
    public int IdGastoApoyo { get; set; }
    public int IdCampana { get; set; }
    public int? IdIntegranteCampana { get; set; }
    public int? IdEvento { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal CostoUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Monto { get; set; } // = CostoUnitario * Cantidad, calculado por el controller al guardar
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public int IdUsuarioRegistro { get; set; }

    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public CatIntegranteCampana? IntegranteCampana { get; set; }
    [ValidateNever] public OprEvento? Evento { get; set; }
    [ValidateNever] public Usuario UsuarioRegistro { get; set; } = null!;
}
