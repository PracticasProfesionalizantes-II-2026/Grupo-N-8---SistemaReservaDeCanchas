using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sistema_de_reserva_Futbol_Ya.Models;

namespace Sistema_de_reserva_Futbol_Ya.Data
{
    public class ApplicationDbContext : DbContext
    {
        // DbSets tablas.

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<Venta_Detallada> VentasDetalladas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cancha> Canchas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Material_Deportivo> MaterialesDeportivos { get; set; }
        public DbSet<Reserva_Material> ReservaMateriales { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=.;Database=Futbol-Ya;Trusted_Connection=True;TrustServerCertificate=True;"
            );
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Apartado para construir las relaciones entre tablas.

            modelBuilder.Entity<Usuario>() //Un usuario tiene muchas ventas y una venta le pertenece a un usuario.
                .HasMany(u => u.Ventas)
                .WithOne(v => v.Usuario)
                .HasForeignKey(v => v.CodigoUsuario);

            modelBuilder.Entity<Usuario>() //un usuario puede hacer muchas reservas y una reserva le pertenece a un usuario.
                .HasMany(u => u.Reservas)
                .WithOne(r => r.Usuario)
                .HasForeignKey(r => r.CodigoUsuario);

            modelBuilder.Entity<Venta>()  //Un venta tiene muchos detalles y un detalle le pertenece a una venta.
                .HasMany(v => v.Venta_Detalladas)
                .WithOne(d => d.Venta)
                .HasForeignKey(d => d.CodigoVenta);

            modelBuilder.Entity<Producto>() //Un producto puede estar en muchos detalles y un detalle le pertenece a un producto.
                .HasMany(p => p.VentasDetalladas)
                .WithOne(vd => vd.Producto)
                .HasForeignKey(vd => vd.CodigoProducto);

            modelBuilder.Entity<Cancha>()  //Una cancha puede tener muchas reservas y una reserva le pertenece a una cancha.
                .HasMany(c => c.Reservas)
                .WithOne(r => r.Cancha)
                .HasForeignKey(r => r.CodigoCancha);

            modelBuilder.Entity<Reserva>() //Una reserva puede aparecer un varias conbinaciones de reserva-materiales y una combinacion le pertenece a una reserva.
                .HasMany(r => r.Reserva_Materiales)
                .WithOne(rm => rm.Reserva)
                .HasForeignKey(rm => rm.CodigoReserva);

            modelBuilder.Entity<Material_Deportivo>() //Un material deportivo puede aparecer en varias combinaciones de reserva-materiales y una combinacion le pertenece a un material deportivo.
                .HasMany(m => m.Reserva_Materiales)
                .WithOne(rm => rm.Material_Deportivo)
                .HasForeignKey(rm => rm.CodigoMaterial);
        }
    }
}
