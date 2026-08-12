using System.Diagnostics;
using System.Text.Json;
using ElectionApp.Data;
using ElectionApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var idCampanaActual = HttpContext.Session.GetInt32("IdCampanaActual") ?? 0;

        ViewBag.TotalIntegrantes = await _db.CatIntegranteCampanas.CountAsync(a => a.IdCampana == idCampanaActual);
        ViewBag.TotalEventos = await _db.OprEventos.CountAsync(e => e.IdCampana == idCampanaActual);
        ViewBag.TotalCampanas = await _db.CatCampanas.CountAsync();

        var eventosCampana = await _db.OprEventos
            .Include(e => e.Ubicacion)
            .Where(e => e.IdCampana == idCampanaActual)
            .OrderBy(e => e.Fecha).ThenBy(e => e.Hora)
            .ToListAsync();

        // ---------- Resumen de gastos (costo estimado vs. real de los eventos) ----------
        ViewBag.TotalCostoEstimado = eventosCampana.Sum(e => e.CostoEstimado ?? 0);
        ViewBag.TotalCostoReal = eventosCampana.Sum(e => e.CostoReal ?? 0);

        // ---------- Resumen de simpatizantes (afiliados a la campaña actual, por rol) ----------
        var resumenRoles = await _db.CatIntegranteCampanas
            .Where(a => a.IdCampana == idCampanaActual)
            .GroupBy(a => a.Rol.Descripcion)
            .Select(g => new { Rol = g.Key, Total = g.Count() })
            .OrderByDescending(g => g.Total)
            .ToListAsync();
        ViewBag.ResumenRoles = resumenRoles;
        ViewBag.TotalAfiliados = resumenRoles.Sum(g => g.Total);

        // ---------- Calendario de próximos eventos ----------
        var eventosCalendario = eventosCampana.Select(e => new
        {
            title = e.Descripcion,
            start = e.Fecha.ToDateTime(e.Hora).ToString("yyyy-MM-ddTHH:mm:ss"),
            lugar = e.Lugar,
        });
        ViewBag.EventosCalendarioJson = JsonSerializer.Serialize(eventosCalendario);

        // ---------- Mapa de eventos ----------
        var eventosConUbicacion = eventosCampana
            .Where(e => e.Ubicacion.Latitud != null && e.Ubicacion.Longitud != null)
            .ToList();

        var eventosMapa = eventosConUbicacion.Select(e => new
        {
            descripcion = e.Descripcion,
            fecha = e.Fecha.ToString("dd/MM/yyyy"),
            hora = e.Hora.ToString("HH:mm"),
            lugar = e.Lugar,
            municipio = e.Ubicacion.Municipio,
            estado = e.Ubicacion.Estado,
            lat = e.Ubicacion.Latitud,
            lng = e.Ubicacion.Longitud,
        });

        ViewBag.EventosConMapa = eventosConUbicacion.Count;
        ViewBag.EventosMapaJson = JsonSerializer.Serialize(eventosMapa);
        return View();
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
