using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using ElectionApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Evidencia (fotos y documentos) de la campaña activa. Siempre pertenece a
// la campaña; evento, gasto de evento y gasto de apoyo son OPCIONALES
// (mismo criterio que GastosApoyoController). El guardado en disco lo hace
// DocumentoService (compartido con GastosApoyoController, que también deja
// adjuntar evidencia al registrar un apoyo). Descargar valida que el
// documento pertenezca a la campaña activa antes de servirlo.
public class DocumentosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly DocumentoService _documentos;

    public DocumentosController(ApplicationDbContext db, DocumentoService documentos)
    {
        _db = db;
        _documentos = documentos;
    }

    // GET: /Documentos
    public async Task<IActionResult> Index(string? filtro, int? idTipoDocumento)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var query = _db.OprDocumentos
            .Include(d => d.TipoDocumento)
            .Include(d => d.Evento)
            .Include(d => d.Gasto)
            .Include(d => d.GastoApoyo)
            .Include(d => d.UsuarioRegistro)
            .Where(d => d.IdCampana == idCampanaActual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(d => d.NombreArchivo.Contains(filtro)
                || (d.Descripcion != null && d.Descripcion.Contains(filtro))
                || (d.Evento != null && d.Evento.Descripcion.Contains(filtro)));
        }

        if (idTipoDocumento.HasValue)
        {
            query = query.Where(d => d.IdTipoDocumento == idTipoDocumento.Value);
        }

        ViewBag.Filtro = filtro;
        ViewBag.IdTipoDocumento = idTipoDocumento;
        var lista = await query.OrderByDescending(d => d.FechaRegistro).ToListAsync();

        ViewBag.TiposDocumento = await _db.CatTiposDocumento.OrderBy(t => t.Descripcion).ToListAsync();

        ViewBag.Eventos = await _db.OprEventos
            .Where(e => e.IdCampana == idCampanaActual)
            .OrderByDescending(e => e.Fecha)
            .Select(e => new { e.IdEvento, e.Descripcion })
            .ToListAsync();

        ViewBag.GastosEvento = await _db.OprGastosEvento
            .Where(g => g.Evento.IdCampana == idCampanaActual)
            .OrderByDescending(g => g.FechaRegistro)
            .Select(g => new { g.IdGasto, g.Concepto })
            .ToListAsync();

        ViewBag.GastosApoyo = await _db.OprGastosApoyo
            .Where(g => g.IdCampana == idCampanaActual)
            .OrderByDescending(g => g.FechaRegistro)
            .Select(g => new { g.IdGastoApoyo, g.Concepto })
            .ToListAsync();

        return View(lista);
    }

    // POST: /Documentos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(DocumentoService.TamanoMaximoBytes + 1024)]
    public async Task<IActionResult> Create(IFormFile archivo, int idTipoDocumento, int? idEvento, int? idGasto, int? idGastoApoyo, string? descripcion)
    {
        var idCampanaActual = ObtenerIdCampanaActual();

        if (archivo is null)
        {
            TempData["DocumentoError"] = "Selecciona un archivo para subir.";
            return RedirectToAction(nameof(Index));
        }

        var error = _documentos.Validar(archivo);
        if (error is not null)
        {
            TempData["DocumentoError"] = error;
            return RedirectToAction(nameof(Index));
        }

        if (!await _db.CatTiposDocumento.AnyAsync(t => t.IdTipoDocumento == idTipoDocumento))
        {
            return NotFound();
        }

        if (idEvento.HasValue && !await _db.OprEventos.AnyAsync(e => e.IdEvento == idEvento.Value && e.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        if (idGasto.HasValue && !await _db.OprGastosEvento.AnyAsync(g => g.IdGasto == idGasto.Value && g.Evento.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        if (idGastoApoyo.HasValue && !await _db.OprGastosApoyo.AnyAsync(g => g.IdGastoApoyo == idGastoApoyo.Value && g.IdCampana == idCampanaActual))
        {
            return NotFound();
        }

        var documento = await _documentos.GuardarAsync(
            archivo, idCampanaActual, idTipoDocumento, ObtenerIdUsuarioActual(),
            idEvento, idGasto, idGastoApoyo, descripcion);

        _db.OprDocumentos.Add(documento);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Documentos/Descargar/5
    public async Task<IActionResult> Descargar(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var documento = await _db.OprDocumentos.FirstOrDefaultAsync(d => d.IdDocumento == id && d.IdCampana == idCampanaActual);
        if (documento is null)
        {
            return NotFound();
        }

        var rutaCompleta = _documentos.RutaFisica(documento.RutaArchivo);
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound();
        }

        // Sin fileDownloadName a propósito: así el navegador decide cómo
        // mostrarlo (imágenes y PDF se ven inline; Word/Excel se descargan).
        return PhysicalFile(rutaCompleta, documento.ContentType);
    }

    // POST: /Documentos/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var documento = await _db.OprDocumentos.FirstOrDefaultAsync(d => d.IdDocumento == id && d.IdCampana == idCampanaActual);
        if (documento is null)
        {
            return NotFound();
        }

        _db.OprDocumentos.Remove(documento);
        await _db.SaveChangesAsync();
        _documentos.Eliminar(documento.RutaArchivo);

        return RedirectToAction(nameof(Index));
    }

    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;

    private int ObtenerIdUsuarioActual() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
