using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PresupuestoFamiliarApp.Models
{
    public class MovimientoFijo
    {
        public int Id { get; set; }

        [Required]
        public string Descripcion { get; set; }

        public TipoTransaccion Tipo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Range(1, 31)]
        public int DiaDelMes { get; set; }

        public int CuentaId { get; set; }
        public Cuenta? Cuenta { get; set; }

        public int? CategoriaGastoId { get; set; }
        public CategoriaGasto? Categoria { get; set; }

        public int EspacioId { get; set; }

        public DateTime? UltimaGeneracion { get; set; }

        public DateTime? FechaFin { get; set; }

        public bool Activo { get; set; } = true;

        // NUEVA PROPIEDAD: Moneda del movimiento fijo
        public Moneda MonedaMovimiento { get; set; } = Moneda.Soles;

        // NUEVA PROPIEDAD: Frecuencia de repetición
        public Frecuencia FrecuenciaRepeticion { get; set; } = Frecuencia.Mensual;
    }
}