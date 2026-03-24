using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Test
{
    public class PresupuestoContextTests : IDisposable
    {
        private readonly PresupuestoContext _context;

        public PresupuestoContextTests()
        {
            var options = new DbContextOptionsBuilder<PresupuestoContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PresupuestoContext(options);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarEspacio()
        {
            // Arrange
            var espacio = new Espacio
            {
                Nombre = "Hogar",
                MonedaPrincipal = Moneda.Soles
            };

            // Act
            _context.Espacios.Add(espacio);
            await _context.SaveChangesAsync();

            var espacioRecuperado = await _context.Espacios.FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(espacioRecuperado);
            Assert.Equal("Hogar", espacioRecuperado.Nombre);
            Assert.Equal(Moneda.Soles, espacioRecuperado.MonedaPrincipal);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarCuenta()
        {
            // Arrange
            var espacio = new Espacio { Nombre = "Test", MonedaPrincipal = Moneda.Soles };
            var cuenta = new Cuenta
            {
                Nombre = "Banco BCP",
                SaldoActual = 5000m,
                MonedaCuenta = Moneda.Soles,
                EsCredito = false,
                Espacio = espacio
            };

            // Act
            _context.Cuentas.Add(cuenta);
            await _context.SaveChangesAsync();

            var cuentaRecuperada = await _context.Cuentas
                .Include(c => c.Espacio)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(cuentaRecuperada);
            Assert.Equal("Banco BCP", cuentaRecuperada.Nombre);
            Assert.Equal(5000m, cuentaRecuperada.SaldoActual);
            Assert.NotNull(cuentaRecuperada.Espacio);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarCategoriaGasto()
        {
            // Arrange
            var espacio = new Espacio { Nombre = "Test" };
            var categoria = new CategoriaGasto
            {
                Nombre = "Alimentación",
                Subcategoria = "Supermercado",
                PresupuestoMensual = 1500m,
                MonedaCategoria = Moneda.Soles,
                Espacio = espacio
            };

            // Act
            _context.CategoriasGastos.Add(categoria);
            await _context.SaveChangesAsync();

            var categoriaRecuperada = await _context.CategoriasGastos.FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(categoriaRecuperada);
            Assert.Equal("Alimentación", categoriaRecuperada.Nombre);
            Assert.Equal("Supermercado", categoriaRecuperada.Subcategoria);
            Assert.Equal(1500m, categoriaRecuperada.PresupuestoMensual);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarTransaccion()
        {
            // Arrange
            var espacio = new Espacio { Nombre = "Test" };
            var cuenta = new Cuenta { Nombre = "Test", SaldoActual = 1000m, Espacio = espacio };
            var categoria = new CategoriaGasto { Nombre = "Test", PresupuestoMensual = 500m, Espacio = espacio };

            var transaccion = new Transaccion
            {
                Descripcion = "Compra de prueba",
                Monto = 150m,
                MontoOriginal = 40m,
                Tipo = TipoTransaccion.Egreso,
                MonedaTransaccion = Moneda.Dolares,
                TasaCambioUsada = 3.75m,
                Cuenta = cuenta,
                Categoria = categoria
            };

            // Act
            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();

            var transaccionRecuperada = await _context.Transacciones
                .Include(t => t.Cuenta)
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(transaccionRecuperada);
            Assert.Equal("Compra de prueba", transaccionRecuperada.Descripcion);
            Assert.Equal(150m, transaccionRecuperada.Monto);
            Assert.Equal(TipoTransaccion.Egreso, transaccionRecuperada.Tipo);
            Assert.NotNull(transaccionRecuperada.Cuenta);
            Assert.NotNull(transaccionRecuperada.Categoria);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarMovimientoFijo()
        {
            // Arrange
            var espacio = new Espacio { Nombre = "Test" };
            var cuenta = new Cuenta { Nombre = "Test", SaldoActual = 1000m, Espacio = espacio };

            var movimientoFijo = new MovimientoFijo
            {
                Descripcion = "Netflix",
                Monto = 35m,
                DiaDelMes = 15,
                Tipo = TipoTransaccion.Egreso,
                Activo = true,
                MonedaMovimiento = Moneda.Dolares,
                FrecuenciaRepeticion = Frecuencia.Mensual,
                Cuenta = cuenta,
                EspacioId = espacio.Id
            };

            // Act
            _context.MovimientosFijos.Add(movimientoFijo);
            await _context.SaveChangesAsync();

            var movimientoRecuperado = await _context.MovimientosFijos
                .Include(m => m.Cuenta)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(movimientoRecuperado);
            Assert.Equal("Netflix", movimientoRecuperado.Descripcion);
            Assert.Equal(35m, movimientoRecuperado.Monto);
            Assert.Equal(15, movimientoRecuperado.DiaDelMes);
            Assert.True(movimientoRecuperado.Activo);
            Assert.Equal(Frecuencia.Mensual, movimientoRecuperado.FrecuenciaRepeticion);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarDeudorConCuentasPorCobrar()
        {
            // Arrange
            var espacio = new Espacio { Nombre = "Test" };
            var deudor = new Deudor
            {
                Nombre = "Juan Pérez",
                Telefono = "123456789",
                Espacio = espacio
            };

            var deuda = new CuentaPorCobrar
            {
                Concepto = "Alquiler Marzo",
                MontoTotal = 500m,
                MontoPagado = 0m,
                FechaVencimiento = DateTime.Now.AddDays(5),
                EsAlquiler = true,
                MonedaDeuda = Moneda.Soles,
                Deudor = deudor
            };

            // Act
            _context.Deudores.Add(deudor);
            _context.CuentasPorCobrar.Add(deuda);
            await _context.SaveChangesAsync();

            var deudorRecuperado = await _context.Deudores
                .Include(d => d.CuentasPorCobrar)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(deudorRecuperado);
            Assert.Equal("Juan Pérez", deudorRecuperado.Nombre);
            Assert.Single(deudorRecuperado.CuentasPorCobrar);

            var deudaRecuperada = deudorRecuperado.CuentasPorCobrar.First();
            Assert.Equal("Alquiler Marzo", deudaRecuperada.Concepto);
            Assert.Equal(500m, deudaRecuperada.MontoTotal);
            Assert.True(deudaRecuperada.EsAlquiler);
        }

        [Fact]
        public async Task DebeGuardarYRecuperarTipoCambio()
        {
            // Arrange
            var tipoCambio = new TipoCambio
            {
                MonedaOrigen = Moneda.Dolares,
                MonedaDestino = Moneda.Soles,
                Tasa = 3.75m,
                FechaActualizacion = DateTime.Now
            };

            // Act
            _context.TiposCambio.Add(tipoCambio);
            await _context.SaveChangesAsync();

            var tipoCambioRecuperado = await _context.TiposCambio.FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(tipoCambioRecuperado);
            Assert.Equal(Moneda.Dolares, tipoCambioRecuperado.MonedaOrigen);
            Assert.Equal(Moneda.Soles, tipoCambioRecuperado.MonedaDestino);
            Assert.Equal(3.75m, tipoCambioRecuperado.Tasa);
        }

        [Fact]
        public async Task DebeEliminarEntidadCorrectamente()
        {
            // Arrange
            var cuenta = new Cuenta
            {
                Nombre = "Test",
                SaldoActual = 1000m,
                EspacioId = 1
            };

            _context.Cuentas.Add(cuenta);
            await _context.SaveChangesAsync();

            // Act
            _context.Cuentas.Remove(cuenta);
            await _context.SaveChangesAsync();

            var cuentaEliminada = await _context.Cuentas.FirstOrDefaultAsync();

            // Assert
            Assert.Null(cuentaEliminada);
        }

        [Fact]
        public async Task DebeActualizarEntidadCorrectamente()
        {
            // Arrange
            var categoria = new CategoriaGasto
            {
                Nombre = "Original",
                PresupuestoMensual = 1000m,
                EspacioId = 1
            };

            _context.CategoriasGastos.Add(categoria);
            await _context.SaveChangesAsync();

            // Act
            categoria.Nombre = "Actualizado";
            categoria.PresupuestoMensual = 1500m;
            await _context.SaveChangesAsync();

            var categoriaActualizada = await _context.CategoriasGastos.FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(categoriaActualizada);
            Assert.Equal("Actualizado", categoriaActualizada.Nombre);
            Assert.Equal(1500m, categoriaActualizada.PresupuestoMensual);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
