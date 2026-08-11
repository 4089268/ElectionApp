using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class OprGastoEvento
{
    public int IdGasto { get; set; }
    public int IdEvento { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ValidateNever] public OprEvento Evento { get; set; } = null!;
}
