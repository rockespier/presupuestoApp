using PresupuestoFamiliarApp.Models;
using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class CuentaPorCobrar
    {
        public int Id { get; set; }

        [Required]
        public string Concepto { get; set; } // Ej: "Alquiler Marzo" o "Préstamo personal"

        [Required]
        public decimal MontoTotal { get; set; }

        public decimal MontoPagado { get; set; } = 0; // Empieza en 0

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaVencimiento { get; set; } // Cuándo te debe pagar

        // NUEVA PROPIEDAD: Indica si esta deuda es un alquiler recurrente mensual
        public bool EsAlquiler { get; set; } = false;

        // NUEVA PROPIEDAD: Moneda de la deuda
        public Moneda MonedaDeuda { get; set; } = Moneda.Soles;

        // Relación con el Deudor
        public int DeudorId { get; set; }
        public Deudor Deudor { get; set; }

        // Propiedades calculadas (No se guardan en la BD, se calculan al vuelo)
        public decimal SaldoPendiente => MontoTotal - MontoPagado;
        public bool EstaPagado => SaldoPendiente <= 0;
    }
}