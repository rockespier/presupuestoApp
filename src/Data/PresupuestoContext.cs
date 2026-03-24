using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PresupuestoFamiliarApp.Models; // Asegúrate de que este namespace coincida con el tuyo

namespace PresupuestoFamiliarApp.Data
{
    public class PresupuestoContext : DbContext
    {
        public PresupuestoContext(DbContextOptions<PresupuestoContext> options) : base(options)
        {
        }

        // Estas serán tus tablas en SQL Server
        public DbSet<Espacio> Espacios { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<CategoriaGasto> CategoriasGastos { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }

        public DbSet<TipoCambio> TiposCambio { get; set; }

        public DbSet<MovimientoFijo> MovimientosFijos { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Deudor> Deudores { get; set; }
        public DbSet<CuentaPorCobrar> CuentasPorCobrar { get; set; }
        public DbSet<PushSubscription> PushSubscriptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Esto le dice a EF Core que deje de tratar esta advertencia como un error fatal
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configurar reglas adicionales (opcional pero recomendado)
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Monto)
                .HasColumnType("decimal(18,2)"); // Precisión para dinero

            modelBuilder.Entity<CategoriaGasto>()
                .Property(c => c.PresupuestoMensual)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Cuenta>()
                .Property(c => c.SaldoActual)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.MontoOriginal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.TasaCambioUsada)
                .HasColumnType("decimal(18,4)"); // 4 decimales para mayor precisión en el tipo de cambio

            modelBuilder.Entity<TipoCambio>()
                .Property(t => t.Tasa)
                .HasColumnType("decimal(18,4)");

            // Asegurar que los datos existentes no den error al pedir un EspacioId
            modelBuilder.Entity<Cuenta>().Property(c => c.EspacioId).HasDefaultValue(1);
            modelBuilder.Entity<CategoriaGasto>().Property(c => c.EspacioId).HasDefaultValue(1);

            // 2. Sembrar datos iniciales (Seed Data)
            modelBuilder.Entity<Espacio>().HasData(
                new Espacio { Id = 1, Nombre = "Mi Casa" }
            );
            // Cuentas
            modelBuilder.Entity<Cuenta>().HasData(
                new Cuenta { Id = 1, Nombre = "Cuenta Roberto", SaldoActual = 0, EspacioId = 1 },
                new Cuenta { Id = 2, Nombre = "Cuenta Ivette", SaldoActual = 0, EspacioId = 1 },
                new Cuenta { Id = 3, Nombre = "Efectivo", SaldoActual = 0, EspacioId = 1 }
            );

            // Categorías
            modelBuilder.Entity<CategoriaGasto>().HasData(
                new CategoriaGasto { Id = 1, Nombre = "Comida", PresupuestoMensual = 500 },
                new CategoriaGasto { Id = 2, Nombre = "Vivienda", PresupuestoMensual = 1000 },
                new CategoriaGasto { Id = 3, Nombre = "Transporte", PresupuestoMensual = 150 },
                new CategoriaGasto { Id = 4, Nombre = "Gastos personales", Subcategoria = "Peluquería, móvil, deporte", PresupuestoMensual = 200 },
                new CategoriaGasto { Id = 5, Nombre = "Gastos de mascota", PresupuestoMensual = 100 },
                new CategoriaGasto { Id = 6, Nombre = "Servicios de casa", Subcategoria = "Luz, agua, gas, internet", PresupuestoMensual = 200 }
            );

            // Crear el administrador por defecto (Contraseña: Admin123)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    NombreUsuario = "admin",
                    Email = "rtres.info@gmail.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                    Rol = "Administrador"
                }
            );
        }
    }
}