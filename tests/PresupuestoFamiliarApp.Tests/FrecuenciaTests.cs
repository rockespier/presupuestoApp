using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Test
{
    public class FrecuenciaTests
    {
        [Fact]
        public void Frecuencia_DebeTenerValorDiaria()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Diaria;

            // Assert
            Assert.Equal(0, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorSemanal()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Semanal;

            // Assert
            Assert.Equal(1, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorQuincenal()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Quincenal;

            // Assert
            Assert.Equal(2, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorMensual()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Mensual;

            // Assert
            Assert.Equal(3, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorBimestral()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Bimestral;

            // Assert
            Assert.Equal(4, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorTrimestral()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Trimestral;

            // Assert
            Assert.Equal(5, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorSemestral()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Semestral;

            // Assert
            Assert.Equal(6, (int)frecuencia);
        }

        [Fact]
        public void Frecuencia_DebeTenerValorAnual()
        {
            // Arrange & Act
            var frecuencia = Frecuencia.Anual;

            // Assert
            Assert.Equal(7, (int)frecuencia);
        }

        [Theory]
        [InlineData(Frecuencia.Diaria, 0)]
        [InlineData(Frecuencia.Semanal, 1)]
        [InlineData(Frecuencia.Quincenal, 2)]
        [InlineData(Frecuencia.Mensual, 3)]
        [InlineData(Frecuencia.Bimestral, 4)]
        [InlineData(Frecuencia.Trimestral, 5)]
        [InlineData(Frecuencia.Semestral, 6)]
        [InlineData(Frecuencia.Anual, 7)]
        public void Frecuencia_DebeTenerValoresCorrectos(Frecuencia frecuencia, int valorEsperado)
        {
            // Arrange & Act
            var valor = (int)frecuencia;

            // Assert
            Assert.Equal(valorEsperado, valor);
        }
    }
}
