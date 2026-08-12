using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Authorization;
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

    // GET: /Campanas/Seleccionar
    // Pantalla para elegir con que campaña trabajar (se muestra tras el
    // login, y tambien sirve para cambiar de campaña desde la barra superior).
    public async Task<IActionResult> Seleccionar(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.IdCampanaActual = HttpContext.Session.GetInt32("IdCampanaActual");

        var campanas = await _db.CatCampanas
            .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes)
            .ToListAsync();
        return View(campanas);
    }

    // POST: /Campanas/Establecer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Establecer(int idCampana, string? returnUrl = null)
    {
        var campana = await _db.CatCampanas.FindAsync(idCampana);
        if (campana is null)
        {
            return NotFound();
        }

        HttpContext.Session.SetInt32("IdCampanaActual", campana.IdCampana);
        HttpContext.Session.SetString("NombreCampanaActual", campana.Descripcion);

        // Se guarda también en el usuario para que la próxima vez que inicie
        // sesión se recargue sola esta misma campaña (ver AccountController.Login).
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _db.Usuarios.FindAsync(idUsuario);
        if (usuario is not null)
        {
            usuario.IdCampanaActual = campana.IdCampana;
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // GET: /Campanas
    // El catálogo de campañas (alta/edición/baja) solo lo puede ver y usar
    // el rol Administrador; el resto de usuarios solo eligen campaña desde
    // Seleccionar/Establecer arriba.
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
    public IActionResult Create()
    {
        return View(new CatCampana());
    }

    // POST: /Campanas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
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
    [Authorize(Roles = "Administrador")]
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
