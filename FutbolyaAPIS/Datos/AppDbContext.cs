using Microsoft.EntityFrameworkCore;
using FutbolyaAPIS.Entidades;
namespace FutbolyaAPIS.Datos;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<VentaDetallada> VentasDetalladas { get; set; }
    public DbSet<Cancha> Canchas { get; set; }
    public DbSet<HorarioDisponible> HorariosDisponibles { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<Material_Deportivo> MaterialesDeportivos { get; set; }
    public DbSet<Reserva_Material> ReservasMateriales { get; set; }

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
        });

        // ── Venta ─────────────────────────────────────────────

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(v => v.Cod_Venta);

            entity.Property(v => v.MontoTotal)
                  .HasColumnType("decimal(10,2)");

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
                  .IsRequired();
        });

        // ── HorarioDisponible ─────────────────────────────────

        modelBuilder.Entity<HorarioDisponible>(entity =>
        {
            entity.HasKey(h => h.Cod_Horario);
        });

        // ── Reserva ───────────────────────────────────────────

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(r => r.Cod_Reserva);

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

            // Reserva → HorarioDisponible (N:1)
            entity.HasOne(r => r.HorarioDisponible)
                  .WithMany(h => h.Reservas)
                  .HasForeignKey(r => r.Cod_Horario)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Material_Deportivo ────────────────────────────────

        modelBuilder.Entity<Material_Deportivo>(entity =>
        {
            entity.HasKey(m => m.Cod_Material);
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
    }
}