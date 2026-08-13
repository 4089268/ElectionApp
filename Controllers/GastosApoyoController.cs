using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Alta/baja/listado de gastos por ayudas/donaciones (Opr_Gastos_Apoyo).
// Siempre pertenece a la campaña activa; el simpatizante y el evento a los
// que se asocia son OPCIONALES (un apoyo puede ser una ayuda general, y
// puede o no haberse dado durante un evento concreto).
// Tiene su propia página (Index) para ver y registrar apoyos de toda la
// campaña, y además se puede seguir registrando desde Integrantes/Details
// (por eso Create/Delete aceptan un returnUrl opcional: vuelven a donde se
// llamaron). Separado a propósito de GastosEventoController: son dos
// rubros distintos de la contabilidad de la campaña.
public class GastosApoyoController : Controller
{
    private readonly ApplicationDbContext _db;

    public GastosApoyoController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /GastosApoyo
    public async Task<IActionResult> Index(string? filtro)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var query = _db.OprGastosApoyo
            .Include(g => g.IntegranteCampana).ThenInclude(a => a!.Integrante)
            .Include(g => g.Evento)
            .Include(g => g.UsuarioRegistro)
            .Where(g => g.IdCampana == idCampanaActual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(g => g.Concepto.Contains(filtro)
                || (g.IntegranteCampana != null && g.IntegranteCampana.Integrante.Nombre.Contains(filtro))
                || (g.IntegranteCampana != null && g.IntegranteCampana.Integrante.ApellidoPaterno.Contains(filtro))
                || (g.IntegranteCampana != null && g.IntegranteCampana.Integrante.ApellidoMaterno != null && g.IntegranteCampana.Integrante.ApellidoMaterno.Contains(filtro))
                || (g.IntegranteCampana != null && g.IntegranteCampana.Integrante.Curp.Contains(filtro))
                || (g.Evento != null && g.Evento.Descripcion.Contains(filtro)));
        }

        ViewBag.Filtro = filtro;
        var lista = await query.OrderByDescending(g => g.FechaRegistro).ToListAsync();
        ViewBag.TotalApoyos = lista.Sum(g => g.Monto);

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

        return View(lista);
    }

    // POST: /GastosApoyo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? idIntegranteCampana, int? idEvento, string concepto, decimal costoUnitario, int cantidad, string? returnUrl = null)
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

        if (string.IsNullOrWhiteSpace(concepto) || costoUnitario <= 0 || cantidad <= 0)
        {
            TempData["GastoError"] = "Captura un concepto, un costo unitario y una cantidad mayores a cero.";
            return await Redirigir(returnUrl, idIntegranteCampana);
        }

        _db.OprGastosApoyo.Add(new OprGastoApoyo
        {
            IdCampana = idCampanaActual,
            IdIntegranteCampana = idIntegranteCampana,
            IdEvento = idEvento,
            Concepto = concepto.Trim(),
            CostoUnitario = costoUnitario,
            Cantidad = cantidad,
            Monto = costoUnitario * cantidad,
            IdUsuarioRegistro = ObtenerIdUsuarioActual(),
        });
        await _db.SaveChangesAsync();

        return await Redirigir(returnUrl, idIntegranteCampana);
    }

    // POST: /GastosApoyo/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var gasto = await _db.OprGastosApoyo.FirstOrDefaultAsync(g => g.IdGastoApoyo == id);

        if (gasto is null || gasto.IdCampana != idCampanaActual)
        {
            return NotFound();
        }

        var idIntegranteCampana = gasto.IdIntegranteCampana;
        _db.OprGastosApoyo.Remove(gasto);
        await _db.SaveChangesAsync();

        return await Redirigir(returnUrl, idIntegranteCampana);
    }

    // Si viene un returnUrl local (p.ej. de GastosApoyo/Index) vuelve ahí.
    // Si no: si el apoyo está ligado a un integrante, vuelve a su ficha
    // (comportamiento original, para cuando se registra desde
    // Integrantes/Details); si no está ligado a nadie, vuelve al listado
    // general de Apoyos.
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
