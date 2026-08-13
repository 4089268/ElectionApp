using ElectionApp.Data;
using ElectionApp.Models;
using ElectionApp.Models.Integrantes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Cat_Integrantes es un padrón GLOBAL (una persona = un registro,
// identificado por CURP). El alta empieza siempre buscando por CURP
// (ver Buscar): si la persona ya existe se afilia a la campaña activa
// sin duplicarla; si no existe, Create la registra y la afilia de una vez.
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
        var idCampanaActual = ObtenerIdCampanaActual();
        var query = _db.CatIntegranteCampanas
            .Include(a => a.Integrante)
            .Include(a => a.Rol)
            .Where(a => a.IdCampana == idCampanaActual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(a => a.Integrante.Nombre.Contains(filtro)
                || a.Integrante.ApellidoPaterno.Contains(filtro)
                || (a.Integrante.ApellidoMaterno != null && a.Integrante.ApellidoMaterno.Contains(filtro))
                || a.Integrante.Curp.Contains(filtro)
                || (a.Integrante.Seccion != null && a.Integrante.Seccion.Contains(filtro))
                || (a.Integrante.ClaveElector != null && a.Integrante.ClaveElector.Contains(filtro)));
        }

        ViewBag.Filtro = filtro;
        var lista = await query
            .OrderBy(a => a.Integrante.ApellidoPaterno).ThenBy(a => a.Integrante.ApellidoMaterno).ThenBy(a => a.Integrante.Nombre)
            .ToListAsync();
        return View(lista);
    }

    // GET: /Integrantes/Buscar
    public IActionResult Buscar()
    {
        return View();
    }

    // POST: /Integrantes/Buscar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buscar(string curp)
    {
        var curpNormalizado = (curp ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(curpNormalizado))
        {
            ModelState.AddModelError(string.Empty, "Captura un CURP para buscar.");
            return View();
        }

        var integrante = await _db.CatIntegrantes.FirstOrDefaultAsync(i => i.Curp == curpNormalizado);
        if (integrante is null)
        {
            return RedirectToAction(nameof(Create), new { curp = curpNormalizado });
        }

        var idCampanaActual = ObtenerIdCampanaActual();
        var afiliacion = await _db.CatIntegranteCampanas
            .FirstOrDefaultAsync(a => a.IdIntegrante == integrante.IdIntegrante && a.IdCampana == idCampanaActual);

        if (afiliacion is null)
        {
            var idRolSimpatizante = await _db.CatRoles
                .Where(r => r.Descripcion == "SIMPATIZANTE")
                .Select(r => r.IdRol)
                .FirstOrDefaultAsync();

            _db.CatIntegranteCampanas.Add(new CatIntegranteCampana
            {
                IdIntegrante = integrante.IdIntegrante,
                IdCampana = idCampanaActual,
                IdRol = idRolSimpatizante,
            });
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Ya existía en el padrón global: se vinculó a esta campaña.";
        }

        return RedirectToAction(nameof(Edit), new { id = integrante.IdIntegrante });
    }

    // GET: /Integrantes/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var afiliacion = await _db.CatIntegranteCampanas
            .Include(a => a.Integrante).ThenInclude(i => i.EstadoCivil)
            .Include(a => a.Integrante).ThenInclude(i => i.Domicilio)
            .Include(a => a.Rol)
            .Include(a => a.GastosApoyo).ThenInclude(g => g.UsuarioRegistro)
            .Include(a => a.GastosApoyo).ThenInclude(g => g.Evento)
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (afiliacion is null)
        {
            return NotFound();
        }

        ViewBag.Eventos = await _db.OprEventos
            .Where(e => e.IdCampana == idCampanaActual)
            .OrderByDescending(e => e.Fecha)
            .Select(e => new { e.IdEvento, e.Descripcion })
            .ToListAsync();

        return View(afiliacion);
    }

    // GET: /Integrantes/Create
    public async Task<IActionResult> Create(string? curp)
    {
        await CargarListasAsync();
        return View(new IntegranteFormViewModel { Curp = curp ?? string.Empty });
    }

    // POST: /Integrantes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IntegranteFormViewModel modelo)
    {
        modelo.Curp = modelo.Curp.Trim().ToUpperInvariant();

        if (await _db.CatIntegrantes.AnyAsync(i => i.Curp == modelo.Curp))
        {
            ModelState.AddModelError(nameof(modelo.Curp), "Ya existe una persona con este CURP en el padrón global. Usa \"Agregar integrante\" y busca primero por CURP.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var integrante = new CatIntegrante
        {
            Curp = modelo.Curp,
            Nombre = modelo.Nombre,
            ApellidoPaterno = modelo.ApellidoPaterno,
            ApellidoMaterno = modelo.ApellidoMaterno,
            Ocupacion = modelo.Ocupacion,
            IdEstadoCivil = modelo.IdEstadoCivil,
            Edad = modelo.Edad,
            HijosMayoresEdad = modelo.HijosMayoresEdad,
            IdDomicilio = modelo.IdDomicilio,
            ClaveElector = modelo.ClaveElector,
            Seccion = modelo.Seccion,
            Celular = modelo.Celular,
            Whatsapp = modelo.Whatsapp,
            Facebook = modelo.Facebook,
            FechaRegistro = DateTime.Now,
        };
        _db.CatIntegrantes.Add(integrante);
        await _db.SaveChangesAsync();

        _db.CatIntegranteCampanas.Add(new CatIntegranteCampana
        {
            IdIntegrante = integrante.IdIntegrante,
            IdCampana = ObtenerIdCampanaActual(),
            IdRol = modelo.IdRol,
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Integrantes/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var integrante = await _db.CatIntegrantes.FindAsync(id);
        var afiliacion = await _db.CatIntegranteCampanas
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (integrante is null || afiliacion is null)
        {
            return NotFound();
        }

        await CargarListasAsync();
        return View(new IntegranteFormViewModel
        {
            IdIntegrante = integrante.IdIntegrante,
            Curp = integrante.Curp,
            Nombre = integrante.Nombre,
            ApellidoPaterno = integrante.ApellidoPaterno,
            ApellidoMaterno = integrante.ApellidoMaterno,
            Ocupacion = integrante.Ocupacion,
            IdEstadoCivil = integrante.IdEstadoCivil,
            Edad = integrante.Edad,
            HijosMayoresEdad = integrante.HijosMayoresEdad,
            IdDomicilio = integrante.IdDomicilio,
            ClaveElector = integrante.ClaveElector,
            Seccion = integrante.Seccion,
            Celular = integrante.Celular,
            Whatsapp = integrante.Whatsapp,
            Facebook = integrante.Facebook,
            IdRol = afiliacion.IdRol,
        });
    }

    // POST: /Integrantes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, IntegranteFormViewModel modelo)
    {
        if (id != modelo.IdIntegrante)
        {
            return NotFound();
        }

        modelo.Curp = modelo.Curp.Trim().ToUpperInvariant();

        if (await _db.CatIntegrantes.AnyAsync(i => i.Curp == modelo.Curp && i.IdIntegrante != id))
        {
            ModelState.AddModelError(nameof(modelo.Curp), "Ya existe otra persona con este CURP en el padrón global.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var idCampanaActual = ObtenerIdCampanaActual();
        var integrante = await _db.CatIntegrantes.FindAsync(id);
        var afiliacion = await _db.CatIntegranteCampanas
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (integrante is null || afiliacion is null)
        {
            return NotFound();
        }

        integrante.Curp = modelo.Curp;
        integrante.Nombre = modelo.Nombre;
        integrante.ApellidoPaterno = modelo.ApellidoPaterno;
        integrante.ApellidoMaterno = modelo.ApellidoMaterno;
        integrante.Ocupacion = modelo.Ocupacion;
        integrante.IdEstadoCivil = modelo.IdEstadoCivil;
        integrante.Edad = modelo.Edad;
        integrante.HijosMayoresEdad = modelo.HijosMayoresEdad;
        integrante.IdDomicilio = modelo.IdDomicilio;
        integrante.ClaveElector = modelo.ClaveElector;
        integrante.Seccion = modelo.Seccion;
        integrante.Celular = modelo.Celular;
        integrante.Whatsapp = modelo.Whatsapp;
        integrante.Facebook = modelo.Facebook;

        afiliacion.IdRol = modelo.IdRol;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Integrantes/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var afiliacion = await _db.CatIntegranteCampanas
            .Include(a => a.Integrante)
            .Include(a => a.Rol)
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (afiliacion is null)
        {
            return NotFound();
        }

        return View(afiliacion);
    }

    // POST: /Integrantes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var afiliacion = await _db.CatIntegranteCampanas
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (afiliacion is not null)
        {
            _db.CatIntegranteCampanas.Remove(afiliacion);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListasAsync()
    {
        ViewBag.Roles = await _db.CatRoles.OrderBy(r => r.Descripcion).ToListAsync();
        ViewBag.EstadosCiviles = await _db.CatEstadosCiviles.OrderBy(e => e.Descripcion).ToListAsync();
        ViewBag.Domicilios = await _db.CatDomicilios.ToListAsync();
        ViewBag.NombreCampanaActual = HttpContext.Session.GetString("NombreCampanaActual");
    }

    // El middleware global en Program.cs ya garantiza que cualquier usuario
    // autenticado que llegue hasta aquí tiene una campaña elegida en sesión.
    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;
}
