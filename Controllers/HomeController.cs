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
        ViewBag.TotalIntegrantes = await _db.CatIntegrantes.CountAsync();
        ViewBag.TotalEventos = await _db.OprEventos.CountAsync();
        ViewBag.TotalCampanas = await _db.CatCampanas.CountAsync();

        var eventosConUbicacion = await _db.OprEventos
            .Include(e => e.Ubicacion)
            .Where(e => e.Ubicacion.Latitud != null && e.Ubicacion.Longitud != null)
            .ToListAsync();

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
