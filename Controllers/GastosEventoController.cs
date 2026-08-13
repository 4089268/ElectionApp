using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Alta/baja de gastos (Opr_Gastos_Evento) desde la pantalla de detalle del
// evento. No tiene Index/vistas propias: todo se ve embebido en
// Eventos/Details. El costo real del evento se recalcula automaticamente
// como la suma de estos gastos (ver ActualizarCostoRealAsync).
public class GastosEventoController : Controller
{
    private readonly ApplicationDbContext _db;

    public GastosEventoController(ApplicationDbContext db)
    {
        _db = db;
    }

    // POST: /GastosEvento/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int idEvento, string concepto, decimal costoUnitario, int cantidad)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos.FirstOrDefaultAsync(e => e.IdEvento == idEvento && e.IdCampana == idCampanaActual);
        if (evento is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(concepto) || costoUnitario <= 0 || cantidad <= 0)
        {
            TempData["GastoError"] = "Captura un concepto, un costo unitario y una cantidad mayores a cero.";
            return RedirectToAction("Details", "Eventos", new { id = idEvento });
        }

        _db.OprGastosEvento.Add(new OprGastoEvento
        {
            IdEvento = idEvento,
            Concepto = concepto.Trim(),
            CostoUnitario = costoUnitario,
            Cantidad = cantidad,
            Monto = costoUnitario * cantidad,
            IdUsuarioRegistro = ObtenerIdUsuarioActual(),
        });
        await _db.SaveChangesAsync();
        await ActualizarCostoRealAsync(idEvento);

        return RedirectToAction("Details", "Eventos", new { id = idEvento });
    }

    // POST: /GastosEvento/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var gasto = await _db.OprGastosEvento
            .Include(g => g.Evento)
            .FirstOrDefaultAsync(g => g.IdGasto == id);

        if (gasto is null || gasto.Evento.IdCampana != idCampanaActual)
        {
            return NotFound();
        }

        var idEvento = gasto.IdEvento;
        _db.OprGastosEvento.Remove(gasto);
        await _db.SaveChangesAsync();
        await ActualizarCostoRealAsync(idEvento);

        return RedirectToAction("Details", "Eventos", new { id = idEvento });
    }

    private async Task ActualizarCostoRealAsync(int idEvento)
    {
        var evento = await _db.OprEventos.FindAsync(idEvento);
        if (evento is null)
        {
            return;
        }

        evento.CostoReal = await _db.OprGastosEvento
            .Where(g => g.IdEvento == idEvento)
            .SumAsync(g => (decimal?)g.Monto) ?? 0;
        await _db.SaveChangesAsync();
    }

    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;

    private int ObtenerIdUsuarioActual() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
