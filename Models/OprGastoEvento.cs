using ElectionApp.Models.Sistema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class OprGastoEvento
{
    public int IdGasto { get; set; }
    public int IdEvento { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal CostoUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Monto { get; set; } // = CostoUnitario * Cantidad, calculado por el controller al guardar
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public int IdUsuarioRegistro { get; set; }

    [ValidateNever] public OprEvento Evento { get; set; } = null!;
    [ValidateNever] public Usuario UsuarioRegistro { get; set; } = null!;
    [ValidateNever] public ICollection<OprDocumento> Documentos { get; set; } = new List<OprDocumento>();
}
