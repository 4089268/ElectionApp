using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

public class IntegrantesController : Controller
{
    private readonly ApplicationDbContext _db;

    public IntegrantesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Integrantes
    public async Task<IActionResult> Index(string? filtro)
    {
        var query = _db.CatIntegrantes
            .Include(i => i.Rol)
            .Include(i => i.Campana)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(i => i.Nombre.Contains(filtro)
                || i.ApellidoPaterno.Contains(filtro)
                || (i.ApellidoMaterno != null && i.ApellidoMaterno.Contains(filtro))
                || (i.Seccion != null && i.Seccion.Contains(filtro))
                || (i.ClaveElector != null && i.ClaveElector.Contains(filtro)));
        }

        ViewBag.Filtro = filtro;
        var lista = await query
            .OrderBy(i => i.ApellidoPaterno).ThenBy(i => i.ApellidoMaterno).ThenBy(i => i.Nombre)
            .ToListAsync();
        return View(lista);
    }

    // GET: /Integrantes/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var integrante = await _db.CatIntegrantes
            .Include(i => i.Rol)
            .Include(i => i.Campana)
            .Include(i => i.EstadoCivil)
            .Include(i => i.Domicilio)
            .Include(i => i.IntegranteSuperior)
            .FirstOrDefaultAsync(i => i.IdIntegrante == id);

        if (integrante is null)
        {
            return NotFound();
        }

        return View(integrante);
    }

    // GET: /Integrantes/Create
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View(new CatIntegrante());
    }

    // POST: /Integrantes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CatIntegrante integrante)
    {
        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(integrante);
        }

        integrante.FechaRegistro = DateTime.Now;
        _db.CatIntegrantes.Add(integrante);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Integrantes/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var integrante = await _db.CatIntegrantes.FindAsync(id);
        if (integrante is null)
        {
            return NotFound();
        }

        await CargarListasAsync();
        return View(integrante);
    }

    // POST: /Integrantes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CatIntegrante integrante)
    {
        if (id != integrante.IdIntegrante)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(integrante);
        }

        _db.CatIntegrantes.Update(integrante);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Integrantes/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var integrante = await _db.CatIntegrantes
            .Include(i => i.Rol)
            .FirstOrDefaultAsync(i => i.IdIntegrante == id);

        if (integrante is null)
        {
            return NotFound();
        }

        return View(integrante);
    }

    // POST: /Integrantes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var integrante = await _db.CatIntegrantes.FindAsync(id);
        if (integrante is not null)
        {
            _db.CatIntegrantes.Remove(integrante);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Roles = await _db.CatRoles.OrderBy(r => r.Descripcion).ToListAsync();
        ViewBag.Campanas = await _db.CatCampanas.OrderBy(c => c.Descripcion).ToListAsync();
        ViewBag.EstadosCiviles = await _db.CatEstadosCiviles.OrderBy(e => e.Descripcion).ToListAsync();
        ViewBag.Domicilios = await _db.CatDomicilios.ToListAsync();
    }
}
