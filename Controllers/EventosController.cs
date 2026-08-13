using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ElectionApp.Data;
using ElectionApp.Models;
using ElectionApp.Models.Eventos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

public class EventosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public EventosController(ApplicationDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    // GET: /Eventos
    public async Task<IActionResult> Index(string? filtro)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var query = _db.OprEventos
            .Include(e => e.Campana)
            .Include(e => e.Ubicacion)
            .Where(e => e.IdCampana == idCampanaActual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(e => e.Descripcion.Contains(filtro)
                || (e.Lugar != null && e.Lugar.Contains(filtro))
                || e.Ubicacion.Municipio.Contains(filtro)
                || e.Ubicacion.Colonia.Contains(filtro));
        }

        ViewBag.Filtro = filtro;
        var lista = await query
            .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Hora)
            .ToListAsync();
        return View(lista);
    }

    // GET: /Eventos/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos
            .Include(e => e.Campana)
            .Include(e => e.Ubicacion)
            .Include(e => e.Gastos).ThenInclude(g => g.UsuarioRegistro)
            .FirstOrDefaultAsync(e => e.IdEvento == id && e.IdCampana == idCampanaActual);

        if (evento is null)
        {
            return NotFound();
        }

        return View(evento);
    }

    // GET: /Eventos/Create
    public IActionResult Create()
    {
        ViewBag.NombreCampanaActual = HttpContext.Session.GetString("NombreCampanaActual");
        return View(new EventoFormViewModel
        {
            IdCampana = ObtenerIdCampanaActual(),
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            Hora = TimeOnly.FromDateTime(DateTime.Now),
        });
    }

    // POST: /Eventos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventoFormViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.NombreCampanaActual = HttpContext.Session.GetString("NombreCampanaActual");
            return View(modelo);
        }

        var ubicacion = await ObtenerOCrearUbicacionAsync(modelo);

        var evento = new OprEvento
        {
            Descripcion = modelo.Descripcion,
            Fecha = modelo.Fecha,
            Hora = modelo.Hora,
            IdCampana = ObtenerIdCampanaActual(),
            IdUbicacion = ubicacion.IdUbicacion,
            Lugar = modelo.Lugar,
            CostoEstimado = modelo.CostoEstimado,
        };

        _db.OprEventos.Add(evento);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Eventos/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos
            .Include(e => e.Ubicacion)
            .FirstOrDefaultAsync(e => e.IdEvento == id && e.IdCampana == idCampanaActual);

        if (evento is null)
        {
            return NotFound();
        }

        ViewBag.NombreCampanaActual = HttpContext.Session.GetString("NombreCampanaActual");
        return View(new EventoFormViewModel
        {
            IdEvento = evento.IdEvento,
            Descripcion = evento.Descripcion,
            Fecha = evento.Fecha,
            Hora = evento.Hora,
            IdCampana = evento.IdCampana,
            Lugar = evento.Lugar,
            CostoEstimado = evento.CostoEstimado,
            Cp = evento.Ubicacion.Cp,
            Colonia = evento.Ubicacion.Colonia,
            Localidad = evento.Ubicacion.Localidad,
            Municipio = evento.Ubicacion.Municipio,
            Estado = evento.Ubicacion.Estado,
            Latitud = evento.Ubicacion.Latitud,
            Longitud = evento.Ubicacion.Longitud,
        });
    }

    // POST: /Eventos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventoFormViewModel modelo)
    {
        if (id != modelo.IdEvento)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.NombreCampanaActual = HttpContext.Session.GetString("NombreCampanaActual");
            return View(modelo);
        }

        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos.FirstOrDefaultAsync(e => e.IdEvento == id && e.IdCampana == idCampanaActual);
        if (evento is null)
        {
            return NotFound();
        }

        var ubicacion = await ObtenerOCrearUbicacionAsync(modelo);

        evento.Descripcion = modelo.Descripcion;
        evento.Fecha = modelo.Fecha;
        evento.Hora = modelo.Hora;
        evento.IdUbicacion = ubicacion.IdUbicacion;
        evento.Lugar = modelo.Lugar;
        evento.CostoEstimado = modelo.CostoEstimado;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Eventos/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos
            .Include(e => e.Campana)
            .Include(e => e.Ubicacion)
            .FirstOrDefaultAsync(e => e.IdEvento == id && e.IdCampana == idCampanaActual);

        if (evento is null)
        {
            return NotFound();
        }

        return View(evento);
    }

    // POST: /Eventos/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var idCampanaActual = ObtenerIdCampanaActual();
        var evento = await _db.OprEventos.FirstOrDefaultAsync(e => e.IdEvento == id && e.IdCampana == idCampanaActual);
        if (evento is not null)
        {
            _db.OprEventos.Remove(evento);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // El middleware global en Program.cs ya garantiza que cualquier usuario
    // autenticado que llegue hasta aquí tiene una campaña elegida en sesión.
    private int ObtenerIdCampanaActual() => HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;

    private async Task<CatUbicacion> ObtenerOCrearUbicacionAsync(EventoFormViewModel modelo)
    {
        var ubicacion = await _db.CatUbicaciones.FirstOrDefaultAsync(u =>
            u.Cp == modelo.Cp && u.Colonia == modelo.Colonia
            && u.Municipio == modelo.Municipio && u.Estado == modelo.Estado);

        if (ubicacion is null)
        {
            ubicacion = new CatUbicacion
            {
                Cp = modelo.Cp,
                Colonia = modelo.Colonia,
                Localidad = modelo.Localidad,
                Municipio = modelo.Municipio,
                Estado = modelo.Estado,
            };
            _db.CatUbicaciones.Add(ubicacion);
        }

        if (modelo.Latitud.HasValue && modelo.Longitud.HasValue)
        {
            // El usuario ubicó el pushpin a mano en el mapa: siempre manda,
            // incluso si la ubicación ya existía con otras coordenadas.
            ubicacion.Latitud = modelo.Latitud;
            ubicacion.Longitud = modelo.Longitud;
        }
        else if (ubicacion.Latitud is null || ubicacion.Longitud is null)
        {
            // Sin pin manual y sin coordenadas previas: geocoding automático
            // como respaldo (p.ej. si el usuario no interactuó con el mapa).
            var coordenadas = await GeocodificarAsync(modelo);
            if (coordenadas is not null)
            {
                ubicacion.Latitud = coordenadas.Value.Lat;
                ubicacion.Longitud = coordenadas.Value.Lng;
            }
        }

        await _db.SaveChangesAsync();
        return ubicacion;
    }

    // Geocoding via Nominatim (OpenStreetMap) — gratuito, sin API key. Si
    // falla o no hay internet, simplemente no se guardan coordenadas y el
    // evento no aparece en el mapa del dashboard, pero se sigue creando.
    private async Task<(decimal Lat, decimal Lng)?> GeocodificarAsync(EventoFormViewModel modelo)
    {
        try
        {
            var direccion = $"{modelo.Colonia}, {modelo.Municipio}, {modelo.Estado}, México";
            var cliente = _httpClientFactory.CreateClient();
            cliente.Timeout = TimeSpan.FromSeconds(5);
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("ElectionApp/1.0 (uso interno)");

            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(direccion)}";
            var resultados = await cliente.GetFromJsonAsync<List<NominatimResultado>>(url);

            var primero = resultados?.FirstOrDefault();
            if (primero is null)
            {
                return null;
            }

            return (
                decimal.Parse(primero.Lat, CultureInfo.InvariantCulture),
                decimal.Parse(primero.Lon, CultureInfo.InvariantCulture));
        }
        catch
        {
            return null;
        }
    }

    private class NominatimResultado
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}
