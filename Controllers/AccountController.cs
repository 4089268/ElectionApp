using System.Security.Claims;
using ElectionApp.Data;
using ElectionApp.Models.Auth;
using ElectionApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Controllers;

public class AccountController : Controller
{
    private const byte MaxIntentosFallidos = 5;

    private readonly ApplicationDbContext _db;
    private readonly PasswordService _passwordService;

    public AccountController(ApplicationDbContext db, PasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _db.Usuarios
            .Include(u => u.RolApp)
            .Include(u => u.CampanaActual)
            .FirstOrDefaultAsync(u => u.UsuarioNombre == model.Usuario);

        if (usuario is null || !usuario.Activo)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        if (usuario.FechaBloqueo is not null)
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta está bloqueada por intentos fallidos. Contacta al administrador.");
            return View(model);
        }

        if (!_passwordService.VerifyPassword(usuario, model.Password))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                usuario.FechaBloqueo = DateTime.Now;
            }
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        usuario.IntentosFallidos = 0;
        usuario.UltimoAcceso = DateTime.Now;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.UsuarioNombre),
            new(ClaimTypes.Role, usuario.RolApp.Descripcion),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        var destino = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? model.ReturnUrl
            : Url.Action("Index", "Home");

        // Si el usuario ya había elegido una campaña en una sesión anterior,
        // se recarga sola y nos saltamos la pantalla de selección.
        if (usuario.CampanaActual is not null)
        {
            HttpContext.Session.SetInt32("IdCampanaActual", usuario.CampanaActual.IdCampana);
            HttpContext.Session.SetString("NombreCampanaActual", usuario.CampanaActual.Descripcion);
            return Redirect(destino!);
        }

        return RedirectToAction("Seleccionar", "Campanas", new { returnUrl = destino });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
