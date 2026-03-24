using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.ViewModels;

namespace PresupuestoFamiliarApp.Test
{
    public class CalculosNegocioTests
    {
        [Fact]
        public void CuentaPorCobrar_SaldoPendienteCalculaCorrectamente()
        {
            // Arrange
            var deuda = new CuentaPorCobrar
            {
                MontoTotal = 1000m,
                MontoPagado = 300m
            };

            // Act
            var saldo = deuda.SaldoPendiente;

            // Assert
            Assert.Equal(700m, saldo);
        }

        [Fact]
        public void CuentaPorCobrar_EstaPagadoReturnaTrueCuandoSaldoEsCero()
        {
            // Arrange
            var deuda = new CuentaPorCobrar
            {
                MontoTotal = 500m,
                MontoPagado = 500m
            };

            // Act
            var estaPagado = deuda.EstaPagado;

            // Assert
            Assert.True(estaPagado);
        }

        [Fact]
        public void CuentaPorCobrar_EstaPagadoRetornaFalseCuandoHaySaldoPendiente()
        {
            // Arrange
            var deuda = new CuentaPorCobrar
            {
                MontoTotal = 500m,
                MontoPagado = 200m
            };

            // Act
            var estaPagado = deuda.EstaPagado;

            // Assert
            Assert.False(estaPagado);
        }

        [Fact]
        public void CuentaPorCobrar_EstaPagadoReturnaTrueCuandoPagadoExcedeTotal()
        {
            // Arrange
            var deuda = new CuentaPorCobrar
            {
                MontoTotal = 500m,
                MontoPagado = 600m
            };

            // Act
            var estaPagado = deuda.EstaPagado;

            // Assert
            Assert.True(estaPagado);
        }

        [Fact]
        public void CuentaPorCobrar_DebeInicializarConValoresPorDefecto()
        {
            // Arrange & Act
            var deuda = new CuentaPorCobrar();
            var ahora = DateTime.Now;

            // Assert
            Assert.Equal(0m, deuda.MontoPagado);
            Assert.False(deuda.EsAlquiler);
            Assert.Equal(Moneda.Soles, deuda.MonedaDeuda);
            Assert.Equal(ahora.Date, deuda.FechaCreacion.Date);
        }

        [Theory]
        [InlineData(1000, 0, 1000)]
        [InlineData(1000, 250, 750)]
        [InlineData(1000, 500, 500)]
        [InlineData(1000, 1000, 0)]
        public void CuentaPorCobrar_SaldoPendienteConDiferentesMontos(
            decimal total, decimal pagado, decimal esperado)
        {
            // Arrange
            var deuda = new CuentaPorCobrar
            {
                MontoTotal = total,
                MontoPagado = pagado
            };

            // Act
            var saldo = deuda.SaldoPendiente;

            // Assert
            Assert.Equal(esperado, saldo);
        }

        [Fact]
        public void Transaccion_ConversionMonetariaBasica()
        {
            // Arrange
            var transaccion = new Transaccion
            {
                MontoOriginal = 100m,
                TasaCambioUsada = 3.75m,
                MonedaTransaccion = Moneda.Dolares
            };

            // Act
            decimal montoConvertido = transaccion.MontoOriginal * transaccion.TasaCambioUsada;

            // Assert
            Assert.Equal(375m, montoConvertido);
        }

        [Fact]
        public void Cuenta_SaldoDebeActualizarseDespuesDeIngreso()
        {
            // Arrange
            var cuenta = new Cuenta
            {
                SaldoActual = 1000m
            };

            decimal ingreso = 500m;

            // Act
            cuenta.SaldoActual += ingreso;

            // Assert
            Assert.Equal(1500m, cuenta.SaldoActual);
        }

        [Fact]
        public void Cuenta_SaldoDebeActualizarseDespuesDeEgreso()
        {
            // Arrange
            var cuenta = new Cuenta
            {
                SaldoActual = 1000m
            };

            decimal egreso = 300m;

            // Act
            cuenta.SaldoActual -= egreso;

            // Assert
            Assert.Equal(700m, cuenta.SaldoActual);
        }

        [Fact]
        public void Cuenta_SaldoPuedeSerNegativo()
        {
            // Arrange
            var cuenta = new Cuenta
            {
                SaldoActual = 500m
            };

            decimal egreso = 800m;

            // Act
            cuenta.SaldoActual -= egreso;

            // Assert
            Assert.Equal(-300m, cuenta.SaldoActual);
        }

        [Theory]
        [InlineData(1000, 100, 10)]
        [InlineData(2000, 500, 25)]
        [InlineData(1500, 1500, 100)]
        [InlineData(1000, 0, 0)]
        public void CategoriaGasto_CalculoPorcentajePresupuesto(
            decimal presupuesto, decimal gastado, int porcentajeEsperado)
        {
            // Arrange
            var categoria = new CategoriaResumen
            {
                PresupuestoMensual = presupuesto,
                GastoReal = gastado
            };

            // Act
            var porcentaje = categoria.PorcentajeConsumido;

            // Assert
            Assert.Equal(porcentajeEsperado, porcentaje);
        }
    }
}
