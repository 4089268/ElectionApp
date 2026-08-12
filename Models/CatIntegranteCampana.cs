using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

// Afiliacion de un integrante (padron global) a una campaña concreta. El
// rol y el "reporta a" son propios de esta afiliación, no de la persona:
// la misma persona puede tener rol/jerarquía distintos en cada campaña.
public class CatIntegranteCampana
{
    public int IdIntegranteCampana { get; set; }
    public int IdIntegrante { get; set; }
    public int IdCampana { get; set; }
    public int IdRol { get; set; }
    public int? IdIntegranteSuperiorCampana { get; set; }
    public DateTime FechaAfiliacion { get; set; } = DateTime.Now;

    [ValidateNever] public CatIntegrante Integrante { get; set; } = null!;
    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public CatRol Rol { get; set; } = null!;
    [ValidateNever] public CatIntegranteCampana? Superior { get; set; }
    [ValidateNever] public ICollection<CatIntegranteCampana> Subordinados { get; set; } = new List<CatIntegranteCampana>();
}
