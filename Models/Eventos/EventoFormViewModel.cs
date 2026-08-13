using System.ComponentModel.DataAnnotations;

namespace ElectionApp.Models.Eventos;

// Forma de captura para Create/Edit de Opr_Eventos. Junta los campos del
// evento con los de la ubicación (Cat_Ubicacion) para no obligar a crear
// la ubicación desde otra pantalla antes de poder registrar un evento.
public class EventoFormViewModel
{
    public int IdEvento { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(255)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria")]
    [Display(Name = "Fecha")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "La hora es obligatoria")]
    [Display(Name = "Hora")]
    public TimeOnly Hora { get; set; }

    // La fija el servidor desde la campaña activa en sesión (ver
    // EventosController.ObtenerIdCampanaActual); no se captura en el form.
    public int IdCampana { get; set; }

    [StringLength(255)]
    [Display(Name = "Lugar")]
    public string? Lugar { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Costo fuera de rango")]
    [Display(Name = "Costo estimado")]
    public decimal? CostoEstimado { get; set; }

    // El costo real NO se captura aquí: se recalcula automáticamente como
    // la suma de los gastos registrados en Opr_Gastos_Evento (ver
    // GastosEventoController.ActualizarCostoRealAsync).

    [Required(ErrorMessage = "El CP es obligatorio")]
    [StringLength(10)]
    [Display(Name = "CP")]
    public string Cp { get; set; } = string.Empty;

    [Required(ErrorMessage = "La colonia es obligatoria")]
    [StringLength(150)]
    [Display(Name = "Colonia")]
    public string Colonia { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Localidad")]
    public string? Localidad { get; set; }

    [Required(ErrorMessage = "El municipio es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Municipio")]
    public string Municipio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = string.Empty;

    // Se llenan al hacer clic en el pushpin del mapa (Create/Edit.cshtml).
    // Si vienen con valor, mandan sobre el geocoding automático (ver
    // EventosController.ObtenerOCrearUbicacionAsync).
    [Range(-90, 90)]
    public decimal? Latitud { get; set; }

    [Range(-180, 180)]
    public decimal? Longitud { get; set; }
}
