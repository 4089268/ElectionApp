using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

public class CampanasController : Controller
{
    private readonly ApplicationDbContext _db;

    public CampanasController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Campanas
    public async Task<IActionResult> Index(string? filtro)
    {
        var query = _db.CatCampanas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(c => c.Descripcion.Contains(filtro)
                || c.Politico.Contains(filtro)
                || c.Puesto.Contains(filtro));
        }

        ViewBag.Filtro = filtro;
        var lista = await query.OrderByDescending(c => c.Anio).ThenBy(c => c.Mes).ToListAsync();
        return View(lista);
    }

    // GET: /Campanas/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var campana = await _db.CatCampanas.FirstOrDefaultAsync(c => c.IdCampana == id);
        if (campana is null)
        {
            return NotFound();
        }

        return View(campana);
    }

    // GET: /Campanas/Create
    public IActionResult Create()
    {
        return View(new CatCampana());
    }

    // POST: /Campanas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CatCampana campana)
    {
        if (!ModelState.IsValid)
        {
            return View(campana);
        }

        _db.CatCampanas.Add(campana);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Campanas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var campana = await _db.CatCampanas.FindAsync(id);
        if (campana is null)
        {
            return NotFound();
        }

        return View(campana);
    }

    // POST: /Campanas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CatCampana campana)
    {
        if (id != campana.IdCampana)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(campana);
        }

        _db.CatCampanas.Update(campana);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Campanas/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var campana = await _db.CatCampanas.FirstOrDefaultAsync(c => c.IdCampana == id);
        if (campana is null)
        {
            return NotFound();
        }

        return View(campana);
    }

    // POST: /Campanas/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var campana = await _db.CatCampanas.FindAsync(id);
        if (campana is not null)
        {
            _db.CatCampanas.Remove(campana);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
