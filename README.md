# ElectionApp

Aplicación web ASP.NET Core (MVC) para el sistema de gestión de campañas y
eventos, sobre la base de datos SQL Server `Election2` (ver `DDL_Election.sql`
en la raíz del repositorio).

## Importante: build no verificado en este entorno

Este proyecto se generó a mano, archivo por archivo. El entorno donde se
generó no tiene acceso a internet hacia los servidores de descarga de
Microsoft ni permisos de administrador para instalar el SDK de .NET 10, así
que **no se pudo correr `dotnet restore` / `dotnet build` para validarlo**.
El código sigue los patrones estándar de ASP.NET Core + EF Core y debería
compilar sin problemas con el SDK de .NET 10 instalado, pero corre
`dotnet build` en tu máquina antes de darlo por bueno y avísame si sale
algún error para corregirlo.

## Requisitos

- .NET 10 SDK
- Acceso a un SQL Server con la base `Election2` ya creada (correr primero
  `DDL_Election.sql`)

## Configuración

1. Edita `appsettings.json` (o mejor, usa `dotnet user-secrets` para no
   guardar la contraseña en el repo) y reemplaza los datos de conexión:

   ```
   dotnet user-secrets set "ConnectionStrings:Election2" "Server=TU_SERVIDOR;Database=Election2;User Id=...;Password=...;TrustServerCertificate=True"
   ```

2. Restaura paquetes y compila:

   ```
   dotnet restore
   dotnet build
   ```

3. Corre la app:

   ```
   dotnet run
   ```

## Primer inicio de sesión

Al arrancar por primera vez contra una base `Election2` vacía en las tablas
`sistema.*`, la app siembra automáticamente:

- Catálogo `sistema.Roles_Aplicacion`: Administrador, Capturista, Consulta.
- Usuario `admin` con contraseña temporal `CambiaEsto#2026`.

Inicia sesión con ese usuario y cámbialo cuanto antes (por ahora no hay
pantalla de "cambiar contraseña" — es lo siguiente a construir).

## Estructura

- `Models/` — entidades que reflejan 1:1 las tablas de `dbo.*`.
- `Models/Sistema/` — entidades de `sistema.*` (autenticación).
- `Data/ApplicationDbContext.cs` — mapeo Fluent API a las tablas/columnas
  existentes (no usa Migrations; las tablas ya las crea `DDL_Election.sql`).
- `Data/DbInitializer.cs` — seed inicial de roles de app y usuario admin.
- `Services/PasswordService.cs` — hash de contraseñas (PBKDF2 vía
  `PasswordHasher<T>` de ASP.NET Core Identity).
- `Controllers/AccountController.cs` — login/logout con cookies.
- `Controllers/IntegrantesController.cs` — CRUD completo de integrantes.
- `Controllers/HomeController.cs` — panel con conteos generales.

## Pendiente / siguientes pasos sugeridos

- CRUD de Eventos, Gastos y Participantes.
- Pantalla de cambio de contraseña y gestión de usuarios (solo Administrador).
- Autorización por rol de app (`[Authorize(Roles = "Administrador")]`) en
  las acciones que lo requieran — ahora mismo cualquier usuario autenticado
  puede entrar a todo.
- Validaciones de negocio (por ejemplo, que `id_integrante_superior` no
  forme ciclos).
- El Create/Edit de Integrantes recibe la entidad de EF Core directo del
  formulario (simple para arrancar, pero vulnerable a over-posting). Para
  producción, cámbialo a un ViewModel/DTO explícito.
