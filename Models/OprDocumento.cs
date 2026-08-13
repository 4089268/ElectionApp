using ElectionApp.Models.Sistema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

// Evidencia (fotos y documentos) subida por el usuario. Siempre pertenece
// a una campaña; el evento, el gasto de evento y el gasto de apoyo a los
// que se asocia son OPCIONALES -- mismo criterio que OprGastoApoyo: puede
// ser evidencia general de la campaña, de un evento concreto, o evidencia
// puntual de un gasto/apoyo (comprobante, factura, foto, etc).
//
// El archivo en si NO se guarda en la base de datos: se guarda en disco
// (fuera de wwwroot) y aqui solo se guarda la ruta relativa + metadatos.
// Ver DocumentosController.
public class OprDocumento
{
    public int IdDocumento { get; set; }
    public int IdCampana { get; set; }
    public int? IdEvento { get; set; }
    public int? IdGasto { get; set; }
    public int? IdGastoApoyo { get; set; }
    public int IdTipoDocumento { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public int IdUsuarioRegistro { get; set; }

    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public OprEvento? Evento { get; set; }
    [ValidateNever] public OprGastoEvento? Gasto { get; set; }
    [ValidateNever] public OprGastoApoyo? GastoApoyo { get; set; }
    [ValidateNever] public CatTipoDocumento TipoDocumento { get; set; } = null!;
    [ValidateNever] public Usuario UsuarioRegistro { get; set; } = null!;
}
