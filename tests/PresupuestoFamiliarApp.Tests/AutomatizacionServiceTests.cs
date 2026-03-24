using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.Servicios;
using Moq;
using Microsoft.AspNetCore.Hosting;

namespace PresupuestoFamiliarApp.Test
{
    public class AutomatizacionServiceTests : IDisposable
    {
        private readonly PresupuestoContext _context;
        private readonly AutomatizacionService _service;

        public AutomatizacionServiceTests()
        {
            var options = new DbContextOptionsBuilder<PresupuestoContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PresupuestoContext(options);
            
            // Crear un EmailService real pero sin usarlo (solo para pruebas)
            var mockEmailService = new EmailService();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            
            _service = new AutomatizacionService(_context, mockEmailService, mockEnvironment.Object);
        }

        [Fact]
        public async Task ProcesarMovimientosFijos_DebeCrearTransaccionParaMovimientoActivo()
        {
            // Arrange
            var hoy = DateTime.Now;
            var espacio = new Espacio { Id = 1, Nombre = "Casa", MonedaPrincipal = Moneda.Soles };
            var cuenta = new Cuenta 
            { 
                Id = 1, 
                Nombre = "Banco", 
                SaldoActual = 5000m, 
                EspacioId = espacio.Id 
            };
            var categoria = new CategoriaGasto 
            { 
                Id = 1, 
                Nombre = "Servicios", 
                PresupuestoMensual = 1000m, 
                EspacioId = espacio.Id 
            };
            var movimientoFijo = new MovimientoFijo
            {
                Id = 1,
                Descripcion = "Luz",
                Monto = 100m,
                DiaDelMes = hoy.Day,
                Tipo = TipoTransaccion.Egreso,
                Activo = true,
                CuentaId = cuenta.Id,
                CategoriaGastoId = categoria.Id
            };

            _context.Espacios.Add(espacio);
            _context.Cuentas.Add(cuenta);
            _context.CategoriasGastos.Add(categoria);
            _context.MovimientosFijos.Add(movimientoFijo);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcesarMovimientosFijos();

            // Assert
            var transaccion = await _context.Transacciones.FirstOrDefaultAsync();
            Assert.NotNull(transaccion);
            Assert.Contains("Automático", transaccion.Descripcion);
            Assert.Equal(100m, transaccion.Monto);
            Assert.Equal(TipoTransaccion.Egreso, transaccion.Tipo);

            var cuentaActualizada = await _context.Cuentas.FindAsync(cuenta.Id);
            Assert.Equal(4900m, cuentaActualizada!.SaldoActual);
        }

        [Fact]
        public async Task ProcesarMovimientosFijos_NoDebeCrearTransaccionParaMovimientoInactivo()
        {
            // Arrange
            var hoy = DateTime.Now;
            var cuenta = new Cuenta { Id = 1, Nombre = "Banco", SaldoActual = 5000m };
            var movimientoFijo = new MovimientoFijo
            {
                Id = 1,
                Descripcion = "Servicio",
                Monto = 100m,
                DiaDelMes = hoy.Day,
                Tipo = TipoTransaccion.Egreso,
                Activo = false,
                CuentaId = cuenta.Id
            };

            _context.Cuentas.Add(cuenta);
            _context.MovimientosFijos.Add(movimientoFijo);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcesarMovimientosFijos();

            // Assert
            var transacciones = await _context.Transacciones.ToListAsync();
            Assert.Empty(transacciones);
        }

        [Fact]
        public async Task ProcesarMovimientosFijos_DebeActualizarSaldoCorrectamenteParaIngreso()
        {
            // Arrange
            var hoy = DateTime.Now;
            var cuenta = new Cuenta { Id = 1, Nombre = "Banco", SaldoActual = 1000m };
            var movimientoFijo = new MovimientoFijo
            {
                Id = 1,
                Descripcion = "Salario",
                Monto = 3000m,
                DiaDelMes = hoy.Day,
                Tipo = TipoTransaccion.Ingreso,
                Activo = true,
                CuentaId = cuenta.Id
            };

            _context.Cuentas.Add(cuenta);
            _context.MovimientosFijos.Add(movimientoFijo);
            await _context.SaveChangesAsync();

            // Act
            await _service.ProcesarMovimientosFijos();

            // Assert
            var cuentaActualizada = await _context.Cuentas.FindAsync(cuenta.Id);
            Assert.Equal(4000m, cuentaActualizada!.SaldoActual);
        }

        [Fact]
        public async Task GenerarDeudasMensuales_DebeCrearNuevaDeudaSiNoExiste()
        {
            // Arrange
            var mesActual = DateTime.Now;
            var deudor = new Deudor
            {
                Id = 1,
                Nombre = "Juan Pérez",
                Telefono = "123456789"
            };

            var deudaAnterior = new CuentaPorCobrar
            {
                Id = 1,
                DeudorId = deudor.Id,
                Concepto = "Alquiler mes anterior",
                MontoTotal = 500m,
                MontoPagado = 500m,
                FechaCreacion = mesActual.AddMonths(-1),
                FechaVencimiento = mesActual.AddMonths(-1).AddDays(5),
                EsAlquiler = true
            };

            _context.Deudores.Add(deudor);
            _context.CuentasPorCobrar.Add(deudaAnterior);
            await _context.SaveChangesAsync();

            // Act
            await _service.GenerarDeudasMensuales();

            // Assert
            var deudas = await _context.CuentasPorCobrar.Where(d => d.DeudorId == deudor.Id).ToListAsync();
            Assert.Equal(2, deudas.Count);

            var nuevaDeuda = deudas.FirstOrDefault(d => d.FechaCreacion.Month == mesActual.Month);
            Assert.NotNull(nuevaDeuda);
            Assert.Equal(500m, nuevaDeuda.MontoTotal);
            Assert.Equal(0m, nuevaDeuda.MontoPagado);
            Assert.True(nuevaDeuda.EsAlquiler);
        }

        [Fact]
        public async Task GenerarDeudasMensuales_NoDebeCrearDeudaDuplicada()
        {
            // Arrange
            var mesActual = DateTime.Now;
            var deudor = new Deudor
            {
                Id = 1,
                Nombre = "María García",
                Telefono = "987654321"
            };

            var deudaActual = new CuentaPorCobrar
            {
                Id = 1,
                DeudorId = deudor.Id,
                Concepto = $"Alquiler {mesActual:MMMM yyyy}",
                MontoTotal = 500m,
                MontoPagado = 0m,
                FechaCreacion = mesActual,
                FechaVencimiento = mesActual.AddDays(5),
                EsAlquiler = true
            };

            _context.Deudores.Add(deudor);
            _context.CuentasPorCobrar.Add(deudaActual);
            await _context.SaveChangesAsync();

            // Act
            await _service.GenerarDeudasMensuales();

            // Assert
            var deudas = await _context.CuentasPorCobrar.Where(d => d.DeudorId == deudor.Id).ToListAsync();
            Assert.Single(deudas);
        }

        [Fact]
        public async Task GenerarDeudasMensuales_NoDebeCrearDeudaSiNoHayAlquileresAnteriores()
        {
            // Arrange
            var deudor = new Deudor
            {
                Id = 1,
                Nombre = "Pedro López",
                Telefono = "555555555"
            };

            var deudaNoAlquiler = new CuentaPorCobrar
            {
                Id = 1,
                DeudorId = deudor.Id,
                Concepto = "Préstamo",
                MontoTotal = 1000m,
                MontoPagado = 0m,
                FechaCreacion = DateTime.Now.AddMonths(-1),
                FechaVencimiento = DateTime.Now.AddDays(30),
                EsAlquiler = false
            };

            _context.Deudores.Add(deudor);
            _context.CuentasPorCobrar.Add(deudaNoAlquiler);
            await _context.SaveChangesAsync();

            // Act
            await _service.GenerarDeudasMensuales();

            // Assert
            var deudas = await _context.CuentasPorCobrar.Where(d => d.DeudorId == deudor.Id).ToListAsync();
            Assert.Single(deudas);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
