using System.ComponentModel.DataAnnotations;

namespace ElectionApp.Models.Integrantes;

// Forma de captura para Create/Edit de Cat_Integrantes. Junta los campos
// globales de la persona con el Rol que tiene en la campaña ACTIVA (la
// campaña en sí no se captura aquí: la fija el servidor desde la sesión).
public class IntegranteFormViewModel
{
    public int IdIntegrante { get; set; }

    [Required(ErrorMessage = "El CURP es obligatorio")]
    [StringLength(18, MinimumLength = 18, ErrorMessage = "El CURP debe tener 18 caracteres")]
    [Display(Name = "CURP")]
    public string Curp { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido paterno es obligatorio")]
    [StringLength(100)]
    [Display(Name = "Apellido paterno")]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Apellido materno")]
    public string? ApellidoMaterno { get; set; }

    [StringLength(150)]
    [Display(Name = "Ocupación")]
    public string? Ocupacion { get; set; }

    [Display(Name = "Estado civil")]
    public int? IdEstadoCivil { get; set; }

    [Range(0, 120)]
    [Display(Name = "Edad")]
    public byte? Edad { get; set; }

    [Display(Name = "Hijos mayores de edad")]
    public byte? HijosMayoresEdad { get; set; }

    [Display(Name = "Domicilio")]
    public int? IdDomicilio { get; set; }

    [StringLength(20)]
    [Display(Name = "Clave de elector")]
    public string? ClaveElector { get; set; }

    [StringLength(10)]
    [Display(Name = "Sección")]
    public string? Seccion { get; set; }

    [StringLength(15)]
    [Display(Name = "Celular")]
    public string? Celular { get; set; }

    [StringLength(15)]
    [Display(Name = "WhatsApp")]
    public string? Whatsapp { get; set; }

    [StringLength(150)]
    [Display(Name = "Facebook")]
    public string? Facebook { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    [Display(Name = "Rol en esta campaña")]
    public int IdRol { get; set; }
}
