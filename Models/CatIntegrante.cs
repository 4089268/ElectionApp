using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ElectionApp.Models;

public class CatIntegrante
{
    public int IdIntegrante { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string? ApellidoMaterno { get; set; }
    public string? Ocupacion { get; set; }
    public int? IdEstadoCivil { get; set; }
    public byte? Edad { get; set; }
    public byte? HijosMayoresEdad { get; set; }
    public int? IdDomicilio { get; set; }
    public string? ClaveElector { get; set; }
    public string? Seccion { get; set; }
    public string? Celular { get; set; }
    public string? Whatsapp { get; set; }
    public string? Facebook { get; set; }
    public int IdRol { get; set; }
    public int IdCampana { get; set; }
    public int? IdIntegranteSuperior { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ValidateNever]
    public string NombreCompleto => string.Join(" ", new[] { Nombre, ApellidoPaterno, ApellidoMaterno }
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    [ValidateNever] public CatEstadoCivil? EstadoCivil { get; set; }
    [ValidateNever] public CatDomicilio? Domicilio { get; set; }
    [ValidateNever] public CatRol Rol { get; set; } = null!;
    [ValidateNever] public CatCampana Campana { get; set; } = null!;
    [ValidateNever] public CatIntegrante? IntegranteSuperior { get; set; }
    [ValidateNever] public ICollection<CatIntegrante> Subordinados { get; set; } = new List<CatIntegrante>();
    [ValidateNever] public ICollection<OprEventoParticipante> Participaciones { get; set; } = new List<OprEventoParticipante>();
}
