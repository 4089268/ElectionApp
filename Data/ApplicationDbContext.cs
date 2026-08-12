using ElectionApp.Models;
using ElectionApp.Models.Sistema;
using Microsoft.EntityFrameworkCore;

namespace ElectionApp.Data;

// Este contexto mapea EXPLICITAMENTE contra el esquema ya creado por
// DDL_Election.sql (base de datos Election2). No usa Migrations porque
// las tablas ya existen; solo describe la forma en que EF debe leerlas.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<CatCampana> CatCampanas => Set<CatCampana>();
    public DbSet<CatRol> CatRoles => Set<CatRol>();
    public DbSet<CatEstadoCivil> CatEstadosCiviles => Set<CatEstadoCivil>();
    public DbSet<CatDomicilio> CatDomicilios => Set<CatDomicilio>();
    public DbSet<CatUbicacion> CatUbicaciones => Set<CatUbicacion>();
    public DbSet<CatIntegrante> CatIntegrantes => Set<CatIntegrante>();
    public DbSet<CatIntegranteCampana> CatIntegranteCampanas => Set<CatIntegranteCampana>();
    public DbSet<OprEvento> OprEventos => Set<OprEvento>();
    public DbSet<OprEventoParticipante> OprEventoParticipantes => Set<OprEventoParticipante>();
    public DbSet<OprGastoEvento> OprGastosEvento => Set<OprGastoEvento>();
    public DbSet<RolAplicacion> RolesAplicacion => Set<RolAplicacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- dbo.Cat_Campañas ----------
        modelBuilder.Entity<CatCampana>(e =>
        {
            e.ToTable("Cat_Campañas", "dbo");
            e.HasKey(x => x.IdCampana);
            e.Property(x => x.IdCampana).HasColumnName("id_campaña");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(255).IsRequired();
            e.Property(x => x.Politico).HasColumnName("politico").HasMaxLength(200).IsRequired();
            e.Property(x => x.Puesto).HasColumnName("puesto").HasMaxLength(150).IsRequired();
            e.Property(x => x.Anio).HasColumnName("año");
            e.Property(x => x.Mes).HasColumnName("mes");
        });

        // ---------- dbo.Cat_Roles ----------
        modelBuilder.Entity<CatRol>(e =>
        {
            e.ToTable("Cat_Roles", "dbo");
            e.HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(150).IsRequired();
        });

        // ---------- dbo.Cat_Estado_Civil ----------
        modelBuilder.Entity<CatEstadoCivil>(e =>
        {
            e.ToTable("Cat_Estado_Civil", "dbo");
            e.HasKey(x => x.IdEstadoCivil);
            e.Property(x => x.IdEstadoCivil).HasColumnName("id_estado_civil");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(50).IsRequired();
        });

        // ---------- dbo.Cat_Domicilios ----------
        modelBuilder.Entity<CatDomicilio>(e =>
        {
            e.ToTable("Cat_Domicilios", "dbo");
            e.HasKey(x => x.IdDomicilio);
            e.Property(x => x.IdDomicilio).HasColumnName("id_domicilio");
            e.Property(x => x.Calle).HasColumnName("calle").HasMaxLength(200);
            e.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(20);
            e.Property(x => x.Cp).HasColumnName("cp").HasMaxLength(10);
            e.Property(x => x.Colonia).HasColumnName("colonia").HasMaxLength(150);
            e.Property(x => x.Ciudad).HasColumnName("ciudad").HasMaxLength(150);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(150);
            e.Property(x => x.Latitud).HasColumnName("latitud").HasColumnType("decimal(10,7)");
            e.Property(x => x.Longitud).HasColumnName("longitud").HasColumnType("decimal(10,7)");
        });

        // ---------- dbo.Cat_Ubicacion ----------
        modelBuilder.Entity<CatUbicacion>(e =>
        {
            e.ToTable("Cat_Ubicacion", "dbo");
            e.HasKey(x => x.IdUbicacion);
            e.Property(x => x.IdUbicacion).HasColumnName("id_ubicacion");
            e.Property(x => x.Cp).HasColumnName("cp").HasMaxLength(10).IsRequired();
            e.Property(x => x.Colonia).HasColumnName("colonia").HasMaxLength(150).IsRequired();
            e.Property(x => x.Localidad).HasColumnName("localidad").HasMaxLength(150);
            e.Property(x => x.Municipio).HasColumnName("municipio").HasMaxLength(150).IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(150).IsRequired();
            e.Property(x => x.Latitud).HasColumnName("latitud").HasColumnType("decimal(10,7)");
            e.Property(x => x.Longitud).HasColumnName("longitud").HasColumnType("decimal(10,7)");
        });

        // ---------- dbo.Cat_Integrantes ----------
        modelBuilder.Entity<CatIntegrante>(e =>
        {
            e.ToTable("Cat_Integrantes", "dbo");
            e.HasKey(x => x.IdIntegrante);
            e.Property(x => x.IdIntegrante).HasColumnName("id_integrante");
            e.Property(x => x.Curp).HasColumnName("curp").HasMaxLength(18).IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(x => x.ApellidoPaterno).HasColumnName("apellido_paterno").HasMaxLength(100).IsRequired();
            e.Property(x => x.ApellidoMaterno).HasColumnName("apellido_materno").HasMaxLength(100);
            e.Ignore(x => x.NombreCompleto);
            e.Property(x => x.Ocupacion).HasColumnName("ocupacion").HasMaxLength(150);
            e.Property(x => x.IdEstadoCivil).HasColumnName("id_estado_civil");
            e.Property(x => x.Edad).HasColumnName("edad");
            e.Property(x => x.HijosMayoresEdad).HasColumnName("hijos_mayores_edad");
            e.Property(x => x.IdDomicilio).HasColumnName("id_domicilio");
            e.Property(x => x.ClaveElector).HasColumnName("clave_elector").HasMaxLength(20);
            e.Property(x => x.Seccion).HasColumnName("seccion").HasMaxLength(10);
            e.Property(x => x.Celular).HasColumnName("celular").HasMaxLength(15);
            e.Property(x => x.Whatsapp).HasColumnName("whatsapp").HasMaxLength(15);
            e.Property(x => x.Facebook).HasColumnName("facebook").HasMaxLength(150);
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro");

            e.HasIndex(x => x.Curp).IsUnique();

            e.HasOne(x => x.EstadoCivil).WithMany(x => x.Integrantes)
                .HasForeignKey(x => x.IdEstadoCivil).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Domicilio).WithMany(x => x.Integrantes)
                .HasForeignKey(x => x.IdDomicilio).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- dbo.Cat_Integrante_Campañas ----------
        modelBuilder.Entity<CatIntegranteCampana>(e =>
        {
            e.ToTable("Cat_Integrante_Campañas", "dbo");
            e.HasKey(x => x.IdIntegranteCampana);
            e.Property(x => x.IdIntegranteCampana).HasColumnName("id_integrante_campana");
            e.Property(x => x.IdIntegrante).HasColumnName("id_integrante");
            e.Property(x => x.IdCampana).HasColumnName("id_campaña");
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.IdIntegranteSuperiorCampana).HasColumnName("id_integrante_superior_campana");
            e.Property(x => x.FechaAfiliacion).HasColumnName("fecha_afiliacion");

            e.HasIndex(x => new { x.IdIntegrante, x.IdCampana }).IsUnique();

            e.HasOne(x => x.Integrante).WithMany(x => x.Afiliaciones)
                .HasForeignKey(x => x.IdIntegrante).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Campana).WithMany(x => x.Afiliaciones)
                .HasForeignKey(x => x.IdCampana).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Rol).WithMany(x => x.Afiliaciones)
                .HasForeignKey(x => x.IdRol).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Superior).WithMany(x => x.Subordinados)
                .HasForeignKey(x => x.IdIntegranteSuperiorCampana).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- dbo.Opr_Eventos ----------
        modelBuilder.Entity<OprEvento>(e =>
        {
            e.ToTable("Opr_Eventos", "dbo");
            e.HasKey(x => x.IdEvento);
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(255).IsRequired();
            e.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("date");
            e.Property(x => x.Hora).HasColumnName("hora").HasColumnType("time");
            e.Property(x => x.IdCampana).HasColumnName("id_campaña");
            e.Property(x => x.IdUbicacion).HasColumnName("id_ubicacion");
            e.Property(x => x.Lugar).HasColumnName("lugar").HasMaxLength(255);
            e.Property(x => x.CostoEstimado).HasColumnName("costo_estimado").HasColumnType("decimal(12,2)");
            e.Property(x => x.CostoReal).HasColumnName("costo_real").HasColumnType("decimal(12,2)");

            e.HasOne(x => x.Campana).WithMany(x => x.Eventos)
                .HasForeignKey(x => x.IdCampana).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Ubicacion).WithMany(x => x.Eventos)
                .HasForeignKey(x => x.IdUbicacion).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- dbo.Opr_Evento_Participantes ----------
        modelBuilder.Entity<OprEventoParticipante>(e =>
        {
            e.ToTable("Opr_Evento_Participantes", "dbo");
            e.HasKey(x => x.IdEventoParticipante);
            e.Property(x => x.IdEventoParticipante).HasColumnName("id_evento_participante");
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.IdIntegrante).HasColumnName("id_integrante");
            e.Property(x => x.Rol).HasColumnName("rol").HasMaxLength(100);
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro");

            e.HasIndex(x => new { x.IdEvento, x.IdIntegrante }).IsUnique();

            e.HasOne(x => x.Evento).WithMany(x => x.Participantes)
                .HasForeignKey(x => x.IdEvento).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Integrante).WithMany(x => x.Participaciones)
                .HasForeignKey(x => x.IdIntegrante).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- dbo.Opr_Gastos_Evento ----------
        modelBuilder.Entity<OprGastoEvento>(e =>
        {
            e.ToTable("Opr_Gastos_Evento", "dbo");
            e.HasKey(x => x.IdGasto);
            e.Property(x => x.IdGasto).HasColumnName("id_gasto");
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.Concepto).HasColumnName("concepto").HasMaxLength(150).IsRequired();
            e.Property(x => x.Monto).HasColumnName("monto").HasColumnType("decimal(12,2)");
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro");

            e.HasOne(x => x.Evento).WithMany(x => x.Gastos)
                .HasForeignKey(x => x.IdEvento).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- sistema.Roles_Aplicacion ----------
        modelBuilder.Entity<RolAplicacion>(e =>
        {
            e.ToTable("Roles_Aplicacion", "sistema");
            e.HasKey(x => x.IdRolApp);
            e.Property(x => x.IdRolApp).HasColumnName("id_rol_app");
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(100).IsRequired();
        });

        // ---------- sistema.Usuarios ----------
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios", "sistema");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.IdIntegrante).HasColumnName("id_integrante");
            e.Property(x => x.UsuarioNombre).HasColumnName("usuario").HasMaxLength(100).IsRequired();
            e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("varbinary(256)").IsRequired();
            e.Property(x => x.IdRolApp).HasColumnName("id_rol_app");
            e.Property(x => x.IdCampanaActual).HasColumnName("id_campaña_actual");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.UltimoAcceso).HasColumnName("ultimo_acceso");
            e.Property(x => x.IntentosFallidos).HasColumnName("intentos_fallidos");
            e.Property(x => x.FechaBloqueo).HasColumnName("fecha_bloqueo");

            e.HasIndex(x => x.UsuarioNombre).IsUnique();
            e.HasIndex(x => x.Correo).IsUnique();

            e.HasOne(x => x.Integrante).WithMany()
                .HasForeignKey(x => x.IdIntegrante).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RolApp).WithMany(x => x.Usuarios)
                .HasForeignKey(x => x.IdRolApp).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CampanaActual).WithMany()
                .HasForeignKey(x => x.IdCampanaActual).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
