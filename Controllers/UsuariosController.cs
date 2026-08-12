using ElectionApp.Data;
using ElectionApp.Models.Sistema;
using ElectionApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

// Gestión de usuarios de la app (sistema.Usuarios) — solo Administrador.
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordService _passwordService;

    public UsuariosController(ApplicationDbContext db, PasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    // GET: /Usuarios
    public async Task<IActionResult> Index(string? filtro)
    {
        var query = _db.Usuarios
            .Include(u => u.RolApp)
            .Include(u => u.Integrante)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(u => u.UsuarioNombre.Contains(filtro)
                || (u.Correo != null && u.Correo.Contains(filtro)));
        }

        ViewBag.Filtro = filtro;
        var lista = await query.OrderBy(u => u.UsuarioNombre).ToListAsync();
        return View(lista);
    }

    // GET: /Usuarios/Create
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View(new UsuarioFormViewModel());
    }

    // POST: /Usuarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.Password))
        {
            ModelState.AddModelError(nameof(modelo.Password), "La contraseña es obligatoria para un usuario nuevo.");
        }

        await ValidarUnicidadAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var usuario = new Usuario
        {
            UsuarioNombre = modelo.UsuarioNombre.Trim(),
            Correo = string.IsNullOrWhiteSpace(modelo.Correo) ? null : modelo.Correo.Trim(),
            IdRolApp = modelo.IdRolApp,
            IdIntegrante = modelo.IdIntegrante,
            Activo = modelo.Activo,
            FechaCreacion = DateTime.Now,
        };
        usuario.PasswordHash = _passwordService.HashPassword(usuario, modelo.Password!);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        TempData["Mensaje"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Usuarios/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        await CargarListasAsync();
        return View(new UsuarioFormViewModel
        {
            IdUsuario = usuario.IdUsuario,
            UsuarioNombre = usuario.UsuarioNombre,
            Correo = usuario.Correo,
            IdRolApp = usuario.IdRolApp,
            IdIntegrante = usuario.IdIntegrante,
            Activo = usuario.Activo,
        });
    }

    // POST: /Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioFormViewModel modelo)
    {
        if (id != modelo.IdUsuario)
        {
            return NotFound();
        }

        await ValidarUnicidadAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.UsuarioNombre = modelo.UsuarioNombre.Trim();
        usuario.Correo = string.IsNullOrWhiteSpace(modelo.Correo) ? null : modelo.Correo.Trim();
        usuario.IdRolApp = modelo.IdRolApp;
        usuario.IdIntegrante = modelo.IdIntegrante;
        usuario.Activo = modelo.Activo;

        if (!string.IsNullOrWhiteSpace(modelo.Password))
        {
            usuario.PasswordHash = _passwordService.HashPassword(usuario, modelo.Password);
        }

        await _db.SaveChangesAsync();

        TempData["Mensaje"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUnicidadAsync(UsuarioFormViewModel modelo)
    {
        if (await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == modelo.UsuarioNombre && u.IdUsuario != modelo.IdUsuario))
        {
            ModelState.AddModelError(nameof(modelo.UsuarioNombre), "Ya existe un usuario con este nombre.");
        }

        if (!string.IsNullOrWhiteSpace(modelo.Correo)
            && await _db.Usuarios.AnyAsync(u => u.Correo == modelo.Correo && u.IdUsuario != modelo.IdUsuario))
        {
            ModelState.AddModelError(nameof(modelo.Correo), "Ya existe un usuario con este correo.");
        }
    }

    private async Task CargarListasAsync()
    {
        ViewBag.RolesApp = await _db.RolesAplicacion.OrderBy(r => r.Descripcion).ToListAsync();
        ViewBag.Integrantes = await _db.CatIntegrantes
            .OrderBy(i => i.ApellidoPaterno).ThenBy(i => i.Nombre)
            .Select(i => new
            {
                i.IdIntegrante,
                NombreCompleto = i.Nombre + " " + i.ApellidoPaterno + (i.ApellidoMaterno != null ? " " + i.ApellidoMaterno : ""),
            })
            .ToListAsync();
    }
}
