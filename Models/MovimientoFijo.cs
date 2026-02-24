using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PresupuestoFamiliarApp.Models
{
    public class MovimientoFijo
    {
        public int Id { get; set; }

        [Required]
        public string Descripcion { get; set; } // Ej: "Sueldo Quincenal", "Alquiler", "Netflix"

        public TipoTransaccion Tipo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Range(1, 31)]
        public int DiaDelMes { get; set; } // Día exacto en que se cobra/paga (Ej: 15)

        public int CuentaId { get; set; }
        public Cuenta? Cuenta { get; set; }

        public int? CategoriaGastoId { get; set; }
        public CategoriaGasto? Categoria { get; set; }

        public int EspacioId { get; set; }

        // El cerebro de la operación: sabe cuándo fue la última vez que te lo cobró
        public DateTime? UltimaGeneracion { get; set; }

        // AÑADE ESTA LÍNEA: Fecha límite para generar el cobro
        public DateTime? FechaFin { get; set; }

        public bool Activo { get; set; } = true;
    }
}