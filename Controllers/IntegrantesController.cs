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
    // Valor fijo guardado en Opr_Evento_Participantes.rol para las personas
    // vinculadas a un evento desde este flujo de búsqueda (distinto de
    // "Organizador", que se asigna desde EventosController).
    private const string RolParticipanteEvento = "Participante";

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
    // idEvento: si viene, el flujo se usa para registrar participantes de
    // un evento concreto (ver Eventos/Details) en vez del alta normal de
    // integrantes — al terminar, se regresa a Eventos/Details/{idEvento}.
    public IActionResult Buscar(int? idEvento)
    {
        ViewBag.IdEvento = idEvento;
        return View();
    }

    // POST: /Integrantes/Buscar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buscar(string curp, int? idEvento)
    {
        ViewBag.IdEvento = idEvento;
        var valorNormalizado = (curp ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(valorNormalizado))
        {
            ModelState.AddModelError(string.Empty, "Captura un CURP o Clave de Elector para buscar.");
            return View();
        }

        var integrante = await _db.CatIntegrantes
            .FirstOrDefaultAsync(i => i.Curp == valorNormalizado || i.ClaveElector == valorNormalizado);
        if (integrante is null)
        {
            return RedirectToAction(nameof(Create), new { curp = valorNormalizado, idEvento });
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

        if (idEvento.HasValue)
        {
            await RegistrarParticipacionEventoAsync(idEvento.Value, integrante.IdIntegrante, idCampanaActual);
            return RedirectToAction("Details", "Eventos", new { id = idEvento.Value });
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
            .Include(a => a.GastosApoyo).ThenInclude(g => g.Documentos)
            .Include(a => a.Peticiones).ThenInclude(p => p.Evento)
            .Include(a => a.Peticiones).ThenInclude(p => p.Estatus)
            .FirstOrDefaultAsync(a => a.IdIntegrante == id && a.IdCampana == idCampanaActual);

        if (afiliacion is null)
        {
            return NotFound();
        }

        return View(afiliacion);
    }

    // GET: /Integrantes/Create
    public async Task<IActionResult> Create(string? curp, int? idEvento)
    {
        await CargarListasAsync();
        ViewBag.IdEvento = idEvento;
        return View(new IntegranteFormViewModel { Curp = curp ?? string.Empty });
    }

    // POST: /Integrantes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IntegranteFormViewModel modelo, int? idEvento)
    {
        modelo.Curp = modelo.Curp.Trim().ToUpperInvariant();

        if (await _db.CatIntegrantes.AnyAsync(i => i.Curp == modelo.Curp))
        {
            ModelState.AddModelError(nameof(modelo.Curp), "Ya existe una persona con este CURP en el padrón global. Usa \"Agregar integrante\" y busca primero por CURP.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            ViewBag.IdEvento = idEvento;
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

        var idCampanaActual = ObtenerIdCampanaActual();
        _db.CatIntegranteCampanas.Add(new CatIntegranteCampana
        {
            IdIntegrante = integrante.IdIntegrante,
            IdCampana = idCampanaActual,
            IdRol = modelo.IdRol,
        });
        await _db.SaveChangesAsync();

        if (idEvento.HasValue)
        {
            await RegistrarParticipacionEventoAsync(idEvento.Value, integrante.IdIntegrante, idCampanaActual);
            return RedirectToAction("Details", "Eventos", new { id = idEvento.Value });
        }

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

    // Vincula al integrante como participante de un evento (ver
    // Eventos/Details, sección "Participantes del evento"). Revalida en
    // servidor que el evento pertenezca a la campaña activa (nunca confiar
    // en el idEvento recibido de un form/querystring). Opr_Evento_Participantes
    // tiene UNIQUE (id_evento, id_integrante): si la persona ya estaba
    // registrada (p.ej. como Organizador), no se duplica la fila.
    private async Task RegistrarParticipacionEventoAsync(int idEvento, int idIntegrante, int idCampanaActual)
    {
        var eventoValido = await _db.OprEventos.AnyAsync(e => e.IdEvento == idEvento && e.IdCampana == idCampanaActual);
        if (!eventoValido)
        {
            return;
        }

        var existente = await _db.OprEventoParticipantes
            .FirstOrDefaultAsync(p => p.IdEvento == idEvento && p.IdIntegrante == idIntegrante);

        if (existente is not null)
        {
            TempData["Mensaje"] = $"Esta persona ya estaba registrada en el evento (rol: {existente.Rol}).";
            return;
        }

        _db.OprEventoParticipantes.Add(new OprEventoParticipante
        {
            IdEvento = idEvento,
            IdIntegrante = idIntegrante,
            Rol = RolParticipanteEvento,
        });
        await _db.SaveChangesAsync();
        TempData["Mensaje"] = "Persona registrada como participante del evento.";
    }

    // El middleware global en Program.cs ya garantiza que cualquier usuario
    // autenticado que llegue hasta aquí tiene una campaña elegida en sesión.
    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;
}
