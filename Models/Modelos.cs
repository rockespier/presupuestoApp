using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    
    // 1. Cuentas (Fuentes de ingreso/dinero)
    public class Cuenta
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; } // "Cuenta Roberto", "Cuenta Ivette", "Efectivo"

        // El saldo se puede calcular dinámicamente o mantener actualizado aquí
        public decimal SaldoActual { get; set; }
        // AÑADE ESTA LÍNEA: Identifica si es tarjeta de crédito
        public bool EsCredito { get; set; } = false;
        // AÑADIR ESTA LÍNEA:
        public Moneda MonedaCuenta { get; set; } = Moneda.Soles;
        public List<Transaccion> Transacciones { get; set; } = new();
        // AÑADIR ESTO: Relación con el Espacio de trabajo
        public int EspacioId { get; set; } = 1;
        public Espacio? Espacio { get; set; } // El '?' es importante para que no falle al guardar
    }

    // 2. Categorías de Gastos
    public class CategoriaGasto
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        // ¡Aquí está el cambio! Añadimos el '?' para hacerla opcional (puede ser nula)
        public string? Subcategoria { get; set; }

        [Required]
        public decimal PresupuestoMensual { get; set; }

        public List<Transaccion> Transacciones { get; set; } = new();
        // AÑADIR ESTO: Relación con el Espacio de trabajo
        public int EspacioId { get; set; } = 1;
        public Espacio? Espacio { get; set; }
    }

    // 3. Transacciones (Ingresos y Egresos)
    public enum TipoTransaccion { Ingreso, Egreso }

    public class Transaccion
    {
        public int Id { get; set; }
        [Required]
        public string Descripcion { get; set; }
        [Required]
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public TipoTransaccion Tipo { get; set; }

        // ¡AÑADIR ESTA LÍNEA!
        public bool EsTransferencia { get; set; } = false;

        public int CuentaId { get; set; }
        public Cuenta Cuenta { get; set; }

        public int? CategoriaGastoId { get; set; }
        public CategoriaGasto Categoria { get; set; }
        
        public Moneda MonedaTransaccion { get; set; } = Moneda.Euros; // Moneda en la que compraste

        public decimal MontoOriginal { get; set; } // Ej: 15.00 (Dólares)

        public decimal TasaCambioUsada { get; set; } = 1; // Ej: 3.75 (Si es la misma moneda, es 1)

        // NOTA: Tu propiedad original "public decimal Monto { get; set; }" se mantiene. 
        // Esa representará el valor ya convertido a la Moneda del Espacio (Ej: 15 * 3.75 = 56.25 Soles) 
        // para que las sumas de tu dashboard sigan funcionando perfecto.
    }
}
