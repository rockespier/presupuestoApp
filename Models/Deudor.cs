using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class Deudor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        public string? Telefono { get; set; } // Para enviarle recordatorios por WhatsApp después

        // Relación con el Espacio (Para saber de qué negocio o presupuesto es este inquilino)
        public int EspacioId { get; set; }
        public Espacio Espacio { get; set; }

        // Relación: Un inquilino puede tener varias deudas (Ej: Alquiler de Enero, Alquiler de Febrero)
        public ICollection<CuentaPorCobrar> CuentasPorCobrar { get; set; }
    }
}