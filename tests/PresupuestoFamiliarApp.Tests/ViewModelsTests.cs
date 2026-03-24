using PresupuestoFamiliarApp.ViewModels;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Test
{
    public class ViewModelsTests
    {
        [Fact]
        public void DashboardViewModel_InicializaListasVacias()
        {
            // Arrange & Act
            var viewModel = new DashboardViewModel();

            // Assert
            Assert.NotNull(viewModel.CuentasDinero);
            Assert.NotNull(viewModel.TarjetasCredito);
            Assert.NotNull(viewModel.ResumenCategorias);
            Assert.NotNull(viewModel.BalancesHistoricos);
            Assert.Empty(viewModel.CuentasDinero);
            Assert.Empty(viewModel.TarjetasCredito);
            Assert.Empty(viewModel.ResumenCategorias);
            Assert.Empty(viewModel.BalancesHistoricos);
        }

        [Fact]
        public void DashboardViewModel_AhorroMesCalculaCorrectamente()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                TotalIngresosMes = 10000m,
                TotalEgresosMes = 6500m
            };

            // Act
            var ahorro = viewModel.AhorroMes;

            // Assert
            Assert.Equal(3500m, ahorro);
        }

        [Fact]
        public void DashboardViewModel_AhorroMesPuedeSerNegativo()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                TotalIngresosMes = 5000m,
                TotalEgresosMes = 7000m
            };

            // Act
            var ahorro = viewModel.AhorroMes;

            // Assert
            Assert.Equal(-2000m, ahorro);
        }

        [Fact]
        public void DashboardViewModel_DeudaTotalTarjetasCalculaSuma()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                TarjetasCredito = new List<Cuenta>
                {
                    new Cuenta { SaldoActual = -1500m, EsCredito = true },
                    new Cuenta { SaldoActual = -800m, EsCredito = true },
                    new Cuenta { SaldoActual = -350m, EsCredito = true }
                }
            };

            // Act
            var deudaTotal = viewModel.DeudaTotalTarjetas;

            // Assert
            Assert.Equal(-2650m, deudaTotal);
        }

        [Fact]
        public void DashboardViewModel_DeudaTotalCeroCuandoNoHayTarjetas()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                TarjetasCredito = new List<Cuenta>()
            };

            // Act
            var deudaTotal = viewModel.DeudaTotalTarjetas;

            // Assert
            Assert.Equal(0m, deudaTotal);
        }

        [Fact]
        public void TransferenciaViewModel_DebeInicializarPropiedadesBasicas()
        {
            // Arrange & Act
            var transferencia = new TransferenciaViewModel
            {
                CuentaOrigenId = 1,
                CuentaDestinoId = 2,
                Monto = 500m,
                Descripcion = "Transferencia de prueba",
                Fecha = DateTime.Now
            };

            // Assert
            Assert.Equal(1, transferencia.CuentaOrigenId);
            Assert.Equal(2, transferencia.CuentaDestinoId);
            Assert.Equal(500m, transferencia.Monto);
            Assert.Equal("Transferencia de prueba", transferencia.Descripcion);
        }

        [Fact]
        public void BalanceMensual_DebeAlmacenarDatosCorrectamente()
        {
            // Arrange & Act
            var balance = new BalanceMensual
            {
                MesNombre = "Marzo",
                Balance = 2500m
            };

            // Assert
            Assert.Equal("Marzo", balance.MesNombre);
            Assert.Equal(2500m, balance.Balance);
        }

        [Theory]
        [InlineData(0, 1000, 0)]
        [InlineData(500, 1000, 50)]
        [InlineData(750, 1000, 75)]
        [InlineData(1000, 1000, 100)]
        [InlineData(1500, 1000, 100)]
        public void CategoriaResumen_PorcentajeConsumidoCalculaCorrectamente(
            decimal gastoReal, decimal presupuesto, int esperado)
        {
            // Arrange
            var categoria = new CategoriaResumen
            {
                Nombre = "Test",
                GastoReal = gastoReal,
                PresupuestoMensual = presupuesto
            };

            // Act
            var porcentaje = categoria.PorcentajeConsumido;

            // Assert
            Assert.Equal(esperado, porcentaje);
        }
    }
}
