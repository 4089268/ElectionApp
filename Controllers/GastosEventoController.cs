using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using ElectionApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Alta/baja de gastos (Opr_Gastos_Evento). El registro tiene su propia
// página (Create), separada de Eventos/Details, para no saturar la
// pantalla de detalle del evento; al terminar (o cancelar) vuelve ahí.
// El costo real del evento se recalcula automaticamente como la suma de
// estos gastos (ver ActualizarCostoRealAsync).
// Al registrar un gasto se puede adjuntar evidencia (foto/documento) de una
// vez, mismo patrón que GastosApoyoController: usa DocumentoService para
// guardarla como Opr_Documentos ligado a este gasto (IdGasto).
public class GastosEventoController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly DocumentoService _documentos;

    public GastosEventoController(ApplicationDbContext db, DocumentoService documentos)
    {
        _db = db;
        _documentos = documentos;
    }

    // GET: /GastosEvento/Create?idEvento=5
    public async Task<IActionResult> Create(int idEvento)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos.FirstOrDefaultAsync(e => e.IdEvento == idEvento && e.IdCampana == idCampanaActual);
        if (evento is null)
        {
            return NotFound();
        }

        ViewBag.Evento = evento;
        ViewBag.TiposDocumento = await _db.CatTiposDocumento.OrderBy(t => t.Descripcion).ToListAsync();
        return View();
    }

    // POST: /GastosEvento/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(DocumentoService.TamanoMaximoBytes + 1024)]
    public async Task<IActionResult> Create(
        int idEvento, string concepto, decimal costoUnitario, int cantidad,
        IFormFile? evidencia, int? idTipoDocumento)
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
            return RedirectToAction(nameof(Create), new { idEvento });
        }

        var hayEvidencia = evidencia is not null && evidencia.Length > 0;
        if (hayEvidencia)
        {
            if (!idTipoDocumento.HasValue)
            {
                TempData["GastoError"] = "Selecciona el tipo de documento de la evidencia que adjuntaste.";
                return RedirectToAction(nameof(Create), new { idEvento });
            }

            var errorArchivo = _documentos.Validar(evidencia!);
            if (errorArchivo is not null)
            {
                TempData["GastoError"] = errorArchivo;
                return RedirectToAction(nameof(Create), new { idEvento });
            }

            if (!await _db.CatTiposDocumento.AnyAsync(t => t.IdTipoDocumento == idTipoDocumento.Value))
            {
                return NotFound();
            }
        }

        var gasto = new OprGastoEvento
        {
            IdEvento = idEvento,
            Concepto = concepto.Trim(),
            CostoUnitario = costoUnitario,
            Cantidad = cantidad,
            Monto = costoUnitario * cantidad,
            IdUsuarioRegistro = ObtenerIdUsuarioActual(),
        };
        _db.OprGastosEvento.Add(gasto);
        await _db.SaveChangesAsync();
        await ActualizarCostoRealAsync(idEvento);

        if (hayEvidencia)
        {
            var documento = await _documentos.GuardarAsync(
                evidencia!, idCampanaActual, idTipoDocumento!.Value, gasto.IdUsuarioRegistro,
                idEvento: idEvento, idGasto: gasto.IdGasto);
            _db.OprDocumentos.Add(documento);
            await _db.SaveChangesAsync();
        }

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
