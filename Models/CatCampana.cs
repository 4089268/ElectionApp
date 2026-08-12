using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class CatCampana
{
    public int IdCampana { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(255)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El político es obligatorio")]
    [StringLength(200)]
    [Display(Name = "Político")]
    public string Politico { get; set; } = string.Empty;

    [Required(ErrorMessage = "El puesto es obligatorio")]
    [StringLength(150)]
    [Display(Name = "Puesto")]
    public string Puesto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El año es obligatorio")]
    [Range(2000, 2100, ErrorMessage = "Año fuera de rango")]
    [Display(Name = "Año")]
    public short Anio { get; set; }

    [Required(ErrorMessage = "El mes es obligatorio")]
    [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12")]
    [Display(Name = "Mes")]
    public byte Mes { get; set; }

    [ValidateNever] public ICollection<CatIntegranteCampana> Afiliaciones { get; set; } = new List<CatIntegranteCampana>();
    [ValidateNever] public ICollection<OprEvento> Eventos { get; set; } = new List<OprEvento>();
}
