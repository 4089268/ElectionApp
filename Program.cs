using ElectionApp.Data;
using ElectionApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servicios ----------
// Por defecto TODA la app requiere sesion iniciada; los endpoints publicos
// (login) se marcan con [AllowAnonymous] explicitamente.
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Election2")));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddHttpClient();

// Sesion en memoria: aqui se guarda la campaña con la que esta trabajando
// el usuario (IdCampanaActual / NombreCampanaActual). Ver Campanas/Seleccionar.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ---------- Seed inicial (roles de app + usuario admin temporal) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.SeedAsync(db, passwordService, logger);
}

// ---------- Pipeline HTTP ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Toda la app trabaja "dentro" de una campaña. Si el usuario ya inicio
// sesion pero todavia no eligio con cual campaña trabajar, se le manda a
// elegirla (excepto en las rutas de login/logout y la de seleccion misma).
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path;
        var esRutaExenta = path.StartsWithSegments("/Account")
            || path.StartsWithSegments("/Campanas/Seleccionar")
            || path.StartsWithSegments("/Campanas/Establecer");

        if (!esRutaExenta && context.Session.GetInt32("IdCampanaActual") is null)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/Campanas/Seleccionar?returnUrl={returnUrl}");
            return;
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
