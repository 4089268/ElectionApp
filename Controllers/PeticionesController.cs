using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Peticiones/solicitudes de simpatizantes (Opr_Peticiones). Por lo general
// se recaban durante un evento, pero no es obligatorio -- mismo criterio
// que GastosApoyoController: siempre pertenece a la campaña activa, y el
// simpatizante/evento a los que se asocia son OPCIONALES.
// Tiene su propia página de listado (Index) y su propia página de alta
// (Create) y edición de estatus (Edit); el registro se puede iniciar desde
// Peticiones/Index, Eventos/Details o Integrantes/Details (por eso
// Create/Edit/Delete aceptan un returnUrl opcional: vuelven a donde se
// llamaron).
public class PeticionesController : Controller
{
    private readonly ApplicationDbContext _db;

    public PeticionesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Peticiones
    public async Task<IActionResult> Index(string? filtro, int? idEstatus)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var query = _db.OprPeticiones
            .Include(p => p.IntegranteCampana).ThenInclude(a => a!.Integrante)
            .Include(p => p.Evento)
            .Include(p => p.Estatus)
            .Include(p => p.UsuarioRegistro)
            .Where(p => p.IdCampana == idCampanaActual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(p => p.Descripcion.Contains(filtro)
                || (p.IntegranteCampana != null && p.IntegranteCampana.Integrante.Nombre.Contains(filtro))
                || (p.IntegranteCampana != null && p.IntegranteCampana.Integrante.ApellidoPaterno.Contains(filtro))
                || (p.IntegranteCampana != null && p.IntegranteCampana.Integrante.ApellidoMaterno != null && p.IntegranteCampana.Integrante.ApellidoMaterno.Contains(filtro))
                || (p.IntegranteCampana != null && p.IntegranteCampana.Integrante.Curp.Contains(filtro))
                || (p.Evento != null && p.Evento.Descripcion.Contains(filtro)));
        }

        if (idEstatus.HasValue)
        {
            query = query.Where(p => p.IdEstatusPeticion == idEstatus.Value);
        }

        ViewBag.Filtro = filtro;
        ViewBag.IdEstatusSeleccionado = idEstatus;
        ViewBag.Estatus = await _db.CatEstatusPeticiones.OrderBy(e => e.IdEstatusPeticion).ToListAsync();

        var lista = await query.OrderByDescending(p => p.FechaRegistro).ToListAsync();
        return View(lista);
    }

    // GET: /Peticiones/Create
    public async Task<IActionResult> Create(int? idIntegranteCampana, int? idEvento, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();

        if (idIntegranteCampana.HasValue
            && !await _db.CatIntegranteCampanas.AnyAsync(a => a.IdIntegranteCampana == idIntegranteCampana.Value && a.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        if (idEvento.HasValue
            && !await _db.OprEventos.AnyAsync(e => e.IdEvento == idEvento.Value && e.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        await CargarListasAsync(idCampanaActual);
        ViewBag.IdIntegranteCampanaSeleccionado = idIntegranteCampana;
        ViewBag.IdEventoSeleccionado = idEvento;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: /Peticiones/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int? idIntegranteCampana, int? idEvento, string descripcion, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();

        if (idIntegranteCampana.HasValue
            && !await _db.CatIntegranteCampanas.AnyAsync(a => a.IdIntegranteCampana == idIntegranteCampana.Value && a.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        if (idEvento.HasValue
            && !await _db.OprEventos.AnyAsync(e => e.IdEvento == idEvento.Value && e.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["PeticionError"] = "Describe en qué consiste la petición.";
            return RedirectToAction(nameof(Create), new { idIntegranteCampana, idEvento, returnUrl });
        }

        var idEstatusPendiente = await _db.CatEstatusPeticiones
            .Where(e => e.Descripcion == "Pendiente")
            .Select(e => e.IdEstatusPeticion)
            .FirstAsync();

        _db.OprPeticiones.Add(new OprPeticion
        {
            IdCampana = idCampanaActual,
            IdIntegranteCampana = idIntegranteCampana,
            IdEvento = idEvento,
            IdEstatusPeticion = idEstatusPendiente,
            Descripcion = descripcion.Trim(),
            IdUsuarioRegistro = ObtenerIdUsuarioActual(),
        });
        await _db.SaveChangesAsync();

        return await Redirigir(returnUrl, idIntegranteCampana);
    }

    // GET: /Peticiones/Edit/5
    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var peticion = await _db.OprPeticiones
            .Include(p => p.IntegranteCampana).ThenInclude(a => a!.Integrante)
            .Include(p => p.Evento)
            .FirstOrDefaultAsync(p => p.IdPeticion == id && p.IdCampana == idCampanaActual);

        if (peticion is null)
        {
            return NotFound();
        }

        ViewBag.Estatus = await _db.CatEstatusPeticiones.OrderBy(e => e.IdEstatusPeticion).ToListAsync();
        ViewBag.ReturnUrl = returnUrl;
        return View(peticion);
    }

    // POST: /Peticiones/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, string descripcion, int idEstatusPeticion, string? observaciones, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var peticion = await _db.OprPeticiones.FirstOrDefaultAsync(p => p.IdPeticion == id && p.IdCampana == idCampanaActual);
        if (peticion is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["PeticionError"] = "Describe en qué consiste la petición.";
            return RedirectToAction(nameof(Edit), new { id, returnUrl });
        }

        if (!await _db.CatEstatusPeticiones.AnyAsync(e => e.IdEstatusPeticion == idEstatusPeticion))
        {
            return NotFound();
        }

        if (peticion.IdEstatusPeticion != idEstatusPeticion)
        {
            peticion.FechaConclusion = await EsEstatusFinalAsync(idEstatusPeticion) ? DateTime.Now : null;
        }

        peticion.Descripcion = descripcion.Trim();
        peticion.IdEstatusPeticion = idEstatusPeticion;
        peticion.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        await _db.SaveChangesAsync();

        return await Redirigir(returnUrl, peticion.IdIntegranteCampana);
    }

    // POST: /Peticiones/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var peticion = await _db.OprPeticiones.FirstOrDefaultAsync(p => p.IdPeticion == id && p.IdCampana == idCampanaActual);
        if (peticion is null)
        {
            return NotFound();
        }

        var idIntegranteCampana = peticion.IdIntegranteCampana;
        _db.OprPeticiones.Remove(peticion);
        await _db.SaveChangesAsync();

        return await Redirigir(returnUrl, idIntegranteCampana);
    }

    // "Concluida"/"Cancelada" son estatus finales: al entrar a uno de esos
    // se fija FechaConclusion; al salir (revertir a Pendiente/En proceso)
    // se limpia.
    private async Task<bool> EsEstatusFinalAsync(int idEstatusPeticion)
    {
        var descripcion = await _db.CatEstatusPeticiones
            .Where(e => e.IdEstatusPeticion == idEstatusPeticion)
            .Select(e => e.Descripcion)
            .FirstOrDefaultAsync();
        return descripcion is "Concluida" or "Cancelada";
    }

    // Carga los combos (integrantes/eventos) que usa el formulario de
    // Peticiones/Create.
    private async Task CargarListasAsync(int idCampanaActual)
    {
        var afiliaciones = await _db.CatIntegranteCampanas
            .Include(a => a.Integrante)
            .Where(a => a.IdCampana == idCampanaActual)
            .OrderBy(a => a.Integrante.ApellidoPaterno).ThenBy(a => a.Integrante.ApellidoMaterno).ThenBy(a => a.Integrante.Nombre)
            .ToListAsync();

        // NombreCompleto es una propiedad calculada en C# (no una columna),
        // así que la proyección se hace en memoria, después de traer los datos.
        ViewBag.Integrantes = afiliaciones
            .Select(a => new { a.IdIntegranteCampana, a.Integrante.NombreCompleto })
            .ToList();

        ViewBag.Eventos = await _db.OprEventos
            .Where(e => e.IdCampana == idCampanaActual)
            .OrderByDescending(e => e.Fecha)
            .Select(e => new { e.IdEvento, e.Descripcion })
            .ToListAsync();
    }

    // Si viene un returnUrl local vuelve ahí. Si no: si la petición está
    // ligada a un integrante, vuelve a su ficha; si no, vuelve al listado
    // general de Peticiones.
    private async Task<IActionResult> Redirigir(string? returnUrl, int? idIntegranteCampana)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (idIntegranteCampana.HasValue)
        {
            var afiliacion = await _db.CatIntegranteCampanas.FindAsync(idIntegranteCampana.Value);
            if (afiliacion is not null)
            {
                return RedirectToAction("Details", "Integrantes", new { id = afiliacion.IdIntegrante });
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;

    private int ObtenerIdUsuarioActual() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
