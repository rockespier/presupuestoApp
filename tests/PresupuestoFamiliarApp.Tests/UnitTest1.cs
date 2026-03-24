using PresupuestoFamiliarApp.Helpers;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Test
{
    public class MonedaHelperTests
    {
        [Fact]
        public void ObtenerSimbolo_DebeRetornarDolarParaDolares()
        {
            // Arrange
            var moneda = Moneda.Dolares;

            // Act
            var resultado = MonedaHelper.ObtenerSimbolo(moneda);

            // Assert
            Assert.Equal("$", resultado);
        }

        [Fact]
        public void ObtenerSimbolo_DebeRetornarEuroParaEuros()
        {
            // Arrange
            var moneda = Moneda.Euros;

            // Act
            var resultado = MonedaHelper.ObtenerSimbolo(moneda);

            // Assert
            Assert.Equal("€", resultado);
        }

        [Fact]
        public void ObtenerSimbolo_DebeRetornarSolParaSoles()
        {
            // Arrange
            var moneda = Moneda.Soles;

            // Act
            var resultado = MonedaHelper.ObtenerSimbolo(moneda);

            // Assert
            Assert.Equal("S/", resultado);
        }

        [Fact]
        public void FormatearMonto_DebeFormatearCorrectamenteConDolares()
        {
            // Arrange
            decimal monto = 1500.50m;
            var moneda = Moneda.Dolares;

            // Act
            var resultado = MonedaHelper.FormatearMonto(monto, moneda);

            // Assert
            Assert.StartsWith("$", resultado);
            Assert.Contains("1", resultado);
            Assert.Contains("500", resultado);
            Assert.Contains("50", resultado);
        }

        [Fact]
        public void FormatearMonto_DebeFormatearCorrectamenteConEuros()
        {
            // Arrange
            decimal monto = 2000.75m;
            var moneda = Moneda.Euros;

            // Act
            var resultado = MonedaHelper.FormatearMonto(monto, moneda);

            // Assert
            Assert.StartsWith("€", resultado);
            Assert.Contains("2", resultado);
            Assert.Contains("000", resultado);
            Assert.Contains("75", resultado);
        }

        [Fact]
        public void FormatearMonto_DebeFormatearCorrectamenteConSoles()
        {
            // Arrange
            decimal monto = 3500.25m;
            var moneda = Moneda.Soles;

            // Act
            var resultado = MonedaHelper.FormatearMonto(monto, moneda);

            // Assert
            Assert.StartsWith("S/", resultado);
            Assert.Contains("3", resultado);
            Assert.Contains("500", resultado);
            Assert.Contains("25", resultado);
        }
    }
}
