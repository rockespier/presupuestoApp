using System.ComponentModel.DataAnnotations;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.ViewModels
{
    public class TransferenciaViewModel
    {
        [Required]
        public int CuentaOrigenId { get; set; }

        [Required]
        public int CuentaDestinoId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; } // El monto en la moneda elegida

        [Required]
        public Moneda MonedaTransferencia { get; set; }

        public string Descripcion { get; set; } = "Transferencia entre cuentas";

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}