using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    // Definimos las monedas fijas de tu sistema
    public enum Moneda
    {
        Soles,
        Dolares,
        Euros
    }

    public class TipoCambio
    {
        public int Id { get; set; }

        [Required]
        public Moneda MonedaOrigen { get; set; }

        [Required]
        public Moneda MonedaDestino { get; set; }

        [Required]
        public decimal Tasa { get; set; } // Ej: 1 Dólar = 3.75 Soles (La tasa sería 3.75)

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}