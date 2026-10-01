using Microsoft.EntityFrameworkCore;
using FutbolYaAPI.Entidades;
namespace FutbolYaAPI.Datos;

// DbContext de EF Core: expone los DbSet de todas las entidades y concentra en OnModelCreating
// toda la configuración fluent (claves, longitudes, conversiones, HasData) más las relaciones
// entre entidades. El borrado es lógico (columna Activo) en casi todas las tablas; el
// DeleteBehavior de cada FK solo rige el borrado físico de EF Core (Cascade para tablas
// intermedias N:N que no tienen sentido sin su padre, Restrict para no perder historial
// referenciado como Ventas, Reservas o Auditoria).
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<VentaDetallada> VentasDetalladas { get; set; }
    public DbSet<Cancha> Canchas { get; set; }
    public DbSet<HorarioDisponible> HorariosDisponibles { get; set; }
    public DbSet<Cancha_Horario> CanchaHorarios { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<Reserva_Horario> ReservasHorarios { get; set; }
    public DbSet<Material_Deportivo> MaterialesDeportivos { get; set; }
    public DbSet<Reserva_Material> ReservasMateriales { get; set; }
    public DbSet<Auditoria> Auditorias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Usuario ─────────────────────────────────────────

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Cod_Usuario);

            entity.Property(u => u.Rol)
                  .IsRequired();

            entity.Property(u => u.Correo)
                  .IsRequired();

            entity.Property(u => u.Contraseña)
                  .IsRequired();

            entity.Property(u => u.Activo)
                  .HasDefaultValue(true);
        });

        // ── Producto ─────────────────────────────────────────

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(p => p.Cod_Producto);

            entity.Property(p => p.Precio)
                  .HasColumnType("decimal(10,2)");

            // Valores: "Bebida" o "Comida"
            entity.Property(p => p.Tipo)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(p => p.Activo)
                  .HasDefaultValue(true);
        });

        // ── Venta ─────────────────────────────────────────────

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(v => v.Cod_Venta);

            entity.Property(v => v.MontoTotal)
                  .HasColumnType("decimal(10,2)");

            entity.Property(v => v.Activo)
                  .HasDefaultValue(true);

            // Venta → Usuario (N:1)
            entity.HasOne(v => v.Usuario)
                  .WithMany(u => u.Ventas)
                  .HasForeignKey(v => v.Cod_Usuario)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── VentaDetallada ────────────────────────────────────

        modelBuilder.Entity<VentaDetallada>(entity =>
        {
            entity.HasKey(vd => vd.Cod_Venta_Detallada);

            entity.Property(vd => vd.Precio)
                  .HasColumnType("decimal(10,2)");

            entity.Property(vd => vd.SubTotal)
                  .HasColumnType("decimal(10,2)");

            // VentaDetallada → Venta (N:1)
            entity.HasOne(vd => vd.Venta)
                  .WithMany(v => v.VentasDetalladas)
                  .HasForeignKey(vd => vd.Cod_Venta)
                  .OnDelete(DeleteBehavior.Cascade);

            // VentaDetallada → Producto (N:1)
            entity.HasOne(vd => vd.Producto)
                  .WithMany(p => p.VentasDetalladas)
                  .HasForeignKey(vd => vd.Cod_Producto)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Cancha ────────────────────────────────────────────
        modelBuilder.Entity<Cancha>(entity =>
        {
            entity.HasKey(c => c.Cod_Cancha);

            entity.Property(c => c.Nombre)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(c => c.Descripcion)
                  .IsRequired();

            entity.Property(c => c.Estado)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20);
        });

        // ── HorarioDisponible (catálogo global fijo de 24 bloques de 1 hora) ──

        modelBuilder.Entity<HorarioDisponible>(entity =>
        {
            entity.HasKey(h => h.Cod_Horario);

            entity.Property(h => h.Activo)
                  .HasDefaultValue(true);

            entity.HasData(Enumerable.Range(0, 24).Select(hora => new
            {
                Cod_Horario = hora + 1,
                HoraInicio  = TimeSpan.FromHours(hora),
                HoraFin     = TimeSpan.FromHours(hora + 1),
                Activo      = true
            }));
        });

        // ── Cancha_Horario (tabla intermedia N:N) ──────

        modelBuilder.Entity<Cancha_Horario>(entity =>
        {
            entity.HasKey(ch => ch.Cod_Cancha_Horario);

            // Una misma cancha no puede tener el mismo bloque horario repetido
            entity.HasIndex(ch => new { ch.Cod_Cancha, ch.Cod_Horario }).IsUnique();

            // Cancha_Horario → Cancha (N:1)
            entity.HasOne(ch => ch.Cancha)
                  .WithMany(c => c.CanchaHorarios)
                  .HasForeignKey(ch => ch.Cod_Cancha)
                  .OnDelete(DeleteBehavior.Cascade);

            // Cancha_Horario → HorarioDisponible (N:1)
            entity.HasOne(ch => ch.HorarioDisponible)
                  .WithMany(h => h.CanchaHorarios)
                  .HasForeignKey(ch => ch.Cod_Horario)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Reserva ───────────────────────────────────────────

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(r => r.Cod_Reserva);

            entity.Property(r => r.Fecha)
                  .IsRequired();

            entity.Property(r => r.FechaReserva)
                  .IsRequired();

            entity.Property(r => r.Estado)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Reserva → Usuario (N:1)
            entity.HasOne(r => r.Usuario)
                  .WithMany(u => u.Reservas)
                  .HasForeignKey(r => r.Cod_Usuario)
                  .OnDelete(DeleteBehavior.Restrict);

            // Reserva → Cancha (N:1)
            entity.HasOne(r => r.Cancha)
                  .WithMany(c => c.Reservas)
                  .HasForeignKey(r => r.Cod_Cancha)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Reserva_Horario (tabla intermedia N:N) ────

        modelBuilder.Entity<Reserva_Horario>(entity =>
        {
            entity.HasKey(rh => rh.Cod_Reserva_Horario);

            // Un mismo horario, en una misma reserva, no puede repetirse
            entity.HasIndex(rh => new { rh.Cod_Reserva, rh.Cod_Horario }).IsUnique();

            // Reserva_Horario → Reserva (N:1)
            entity.HasOne(rh => rh.Reserva)
                  .WithMany(r => r.ReservaHorarios)
                  .HasForeignKey(rh => rh.Cod_Reserva)
                  .OnDelete(DeleteBehavior.Cascade);

            // Reserva_Horario → HorarioDisponible (N:1)
            entity.HasOne(rh => rh.HorarioDisponible)
                  .WithMany(h => h.ReservaHorarios)
                  .HasForeignKey(rh => rh.Cod_Horario)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Material_Deportivo ────────────────────────────────

        modelBuilder.Entity<Material_Deportivo>(entity =>
        {
            entity.HasKey(m => m.Cod_Material);

            entity.Property(m => m.Activo)
                  .HasDefaultValue(true);
        });

        // ── Reserva_Material (tabla intermedia N:N) ───────────

        modelBuilder.Entity<Reserva_Material>(entity =>
        {
            entity.HasKey(rm => rm.Cod_Reserva_Mat);

            // Reserva_Material → Reserva (N:1)
            entity.HasOne(rm => rm.Reserva)
                  .WithMany(r => r.ReservaMateriales)
                  .HasForeignKey(rm => rm.Cod_Reserva)
                  .OnDelete(DeleteBehavior.Cascade);

            // Reserva_Material → Material_Deportivo (N:1)
            entity.HasOne(rm => rm.Material_Deportivo)
                  .WithMany(m => m.ReservaMateriales)
                  .HasForeignKey(rm => rm.Cod_Material)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Auditoria ─────────────────────────────────

        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(a => a.Cod_Auditoria);

            entity.Property(a => a.Entidad_Afectada)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(a => a.Accion)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Auditoria → Usuario (N:1)
            entity.HasOne(a => a.Usuario)
                  .WithMany(u => u.RegistrosAuditoria)
                  .HasForeignKey(a => a.Cod_Usuario)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
