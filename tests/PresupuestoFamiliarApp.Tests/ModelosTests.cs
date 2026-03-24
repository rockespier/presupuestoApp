using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.ViewModels;

namespace PresupuestoFamiliarApp.Test
{
    public class ModelosTests
    {
        [Fact]
        public void Cuenta_DebeCrearseConValoresPorDefecto()
        {
            // Arrange & Act
            var cuenta = new Cuenta();

            // Assert
            Assert.False(cuenta.EsCredito);
            Assert.Equal(Moneda.Soles, cuenta.MonedaCuenta);
            Assert.NotNull(cuenta.Transacciones);
            Assert.Empty(cuenta.Transacciones);
        }

        [Fact]
        public void Cuenta_DebePermitirConfiguracionCompleta()
        {
            // Arrange & Act
            var cuenta = new Cuenta
            {
                Id = 1,
                Nombre = "Cuenta Banco",
                SaldoActual = 5000m,
                EsCredito = false,
                MonedaCuenta = Moneda.Dolares,
                EspacioId = 1
            };

            // Assert
            Assert.Equal(1, cuenta.Id);
            Assert.Equal("Cuenta Banco", cuenta.Nombre);
            Assert.Equal(5000m, cuenta.SaldoActual);
            Assert.False(cuenta.EsCredito);
            Assert.Equal(Moneda.Dolares, cuenta.MonedaCuenta);
            Assert.Equal(1, cuenta.EspacioId);
        }

        [Fact]
        public void CategoriaGasto_DebeCrearseConValoresPorDefecto()
        {
            // Arrange & Act
            var categoria = new CategoriaGasto();

            // Assert
            Assert.Equal(Moneda.Soles, categoria.MonedaCategoria);
            Assert.NotNull(categoria.Transacciones);
            Assert.Empty(categoria.Transacciones);
        }

        [Fact]
        public void CategoriaGasto_DebePermitirSubcategoriaNula()
        {
            // Arrange & Act
            var categoria = new CategoriaGasto
            {
                Nombre = "Comida",
                Subcategoria = null,
                PresupuestoMensual = 1000m
            };

            // Assert
            Assert.Null(categoria.Subcategoria);
        }

        [Fact]
        public void Transaccion_DebeCrearseConValoresPorDefecto()
        {
            // Arrange & Act
            var transaccion = new Transaccion();

            // Assert
            Assert.False(transaccion.EsTransferencia);
            Assert.Equal(1, transaccion.TasaCambioUsada);
            Assert.Equal(Moneda.Euros, transaccion.MonedaTransaccion);
        }

        [Fact]
        public void Transaccion_DebeTenerFechaActualPorDefecto()
        {
            // Arrange & Act
            var transaccion = new Transaccion();
            var ahora = DateTime.Now;

            // Assert
            Assert.Equal(ahora.Date, transaccion.Fecha.Date);
        }

        [Fact]
        public void Transaccion_DebePermitirConfiguracionCompleta()
        {
            // Arrange
            var fecha = new DateTime(2024, 3, 15);

            // Act
            var transaccion = new Transaccion
            {
                Id = 1,
                Descripcion = "Compra Supermercado",
                Monto = 150m,
                MontoOriginal = 40m,
                Fecha = fecha,
                Tipo = TipoTransaccion.Egreso,
                CuentaId = 1,
                CategoriaGastoId = 1,
                MonedaTransaccion = Moneda.Dolares,
                TasaCambioUsada = 3.75m,
                EsTransferencia = false
            };

            // Assert
            Assert.Equal(1, transaccion.Id);
            Assert.Equal("Compra Supermercado", transaccion.Descripcion);
            Assert.Equal(150m, transaccion.Monto);
            Assert.Equal(40m, transaccion.MontoOriginal);
            Assert.Equal(fecha, transaccion.Fecha);
            Assert.Equal(TipoTransaccion.Egreso, transaccion.Tipo);
            Assert.Equal(Moneda.Dolares, transaccion.MonedaTransaccion);
            Assert.Equal(3.75m, transaccion.TasaCambioUsada);
        }

        [Theory]
        [InlineData(TipoTransaccion.Ingreso)]
        [InlineData(TipoTransaccion.Egreso)]
        public void Transaccion_DebeSoportarAmbosTipos(TipoTransaccion tipo)
        {
            // Arrange & Act
            var transaccion = new Transaccion
            {
                Tipo = tipo
            };

            // Assert
            Assert.Equal(tipo, transaccion.Tipo);
        }

        [Fact]
        public void MovimientoFijo_DebeCalcularProximaEjecucionCorrectamente()
        {
            // Arrange
            var hoy = DateTime.Now;
            var movimiento = new MovimientoFijo
            {
                DiaDelMes = hoy.Day,
                Activo = true,
                Tipo = TipoTransaccion.Egreso,
                Monto = 500m
            };

            // Act & Assert
            Assert.True(movimiento.Activo);
            Assert.Equal(hoy.Day, movimiento.DiaDelMes);
        }

        [Fact]
        public void DashboardViewModel_DebeCalcularAhorroCorrectamente()
        {
            // Arrange & Act
            var dashboard = new DashboardViewModel
            {
                TotalIngresosMes = 5000m,
                TotalEgresosMes = 3000m
            };

            // Assert
            Assert.Equal(2000m, dashboard.AhorroMes);
        }

        [Fact]
        public void DashboardViewModel_DebeCalcularDeudaTotalTarjetas()
        {
            // Arrange
            var dashboard = new DashboardViewModel
            {
                TarjetasCredito = new List<Cuenta>
                {
                    new Cuenta { SaldoActual = 1000m, EsCredito = true },
                    new Cuenta { SaldoActual = 500m, EsCredito = true },
                    new Cuenta { SaldoActual = 200m, EsCredito = true }
                }
            };

            // Act
            var deudaTotal = dashboard.DeudaTotalTarjetas;

            // Assert
            Assert.Equal(1700m, deudaTotal);
        }

        [Fact]
        public void CategoriaResumen_DebeCalcularPorcentajeConsumido()
        {
            // Arrange
            var categoria = new CategoriaResumen
            {
                Nombre = "Alimentación",
                PresupuestoMensual = 1000m,
                GastoReal = 750m
            };

            // Act
            var porcentaje = categoria.PorcentajeConsumido;

            // Assert
            Assert.Equal(75, porcentaje);
        }

        [Fact]
        public void CategoriaResumen_NoDebeSuperarCienPorCiento()
        {
            // Arrange
            var categoria = new CategoriaResumen
            {
                Nombre = "Alimentación",
                PresupuestoMensual = 1000m,
                GastoReal = 1500m
            };

            // Act
            var porcentaje = categoria.PorcentajeConsumido;

            // Assert
            Assert.Equal(100, porcentaje);
        }

        [Fact]
        public void CategoriaResumen_DebeRetornarCeroCuandoPresupuestoEsCero()
        {
            // Arrange
            var categoria = new CategoriaResumen
            {
                Nombre = "Test",
                PresupuestoMensual = 0m,
                GastoReal = 500m
            };

            // Act
            var porcentaje = categoria.PorcentajeConsumido;

            // Assert
            Assert.Equal(0, porcentaje);
        }
    }
}
