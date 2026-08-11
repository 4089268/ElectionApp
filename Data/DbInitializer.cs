using ElectionApp.Models;
using ElectionApp.Models.Sistema;
using ElectionApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Data;

// Siembra los catalogos minimos de dbo.* y sistema.* la primera vez que
// corre la app contra una base vacia (Election2 recien creada con
// DDL_Election.sql). No usa Migrations: las tablas ya deben existir.
public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext db, PasswordService passwordService, ILogger logger)
    {
        if (!await db.CatEstadosCiviles.AnyAsync())
        {
            db.CatEstadosCiviles.AddRange(
                new CatEstadoCivil { Descripcion = "Soltero(a)" },
                new CatEstadoCivil { Descripcion = "Casado(a)" },
                new CatEstadoCivil { Descripcion = "Unión libre" },
                new CatEstadoCivil { Descripcion = "Divorciado(a)" },
                new CatEstadoCivil { Descripcion = "Viudo(a)" });
            await db.SaveChangesAsync();
        }

        if (!await db.CatRoles.AnyAsync())
        {
            db.CatRoles.AddRange(
                new CatRol { Descripcion = "JEFE DE CAMPAÑA" },
                new CatRol { Descripcion = "JEFE DE SECCION" },
                new CatRol { Descripcion = "JEFE DE MANZANA" },
                new CatRol { Descripcion = "SIMPATIZANTE" });
            await db.SaveChangesAsync();
        }

        if (!await db.RolesAplicacion.AnyAsync())
        {
            db.RolesAplicacion.AddRange(
                new RolAplicacion { Descripcion = "Administrador" },
                new RolAplicacion { Descripcion = "Capturista" },
                new RolAplicacion { Descripcion = "Consulta" });
            await db.SaveChangesAsync();
        }

        if (!await db.Usuarios.AnyAsync())
        {
            var rolAdmin = await db.RolesAplicacion.FirstAsync(r => r.Descripcion == "Administrador");

            var admin = new Usuario
            {
                UsuarioNombre = "admin",
                Correo = "admin@election.local",
                IdRolApp = rolAdmin.IdRolApp,
                Activo = true,
            };
            admin.PasswordHash = passwordService.HashPassword(admin, "a123456");

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            logger.LogWarning(
                "Se creó el usuario 'admin' con contraseña temporal 'CambiaEsto#2026'. Cámbiala en cuanto inicies sesión.");
        }
    }
}
