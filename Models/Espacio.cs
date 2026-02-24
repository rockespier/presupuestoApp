using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class Espacio
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }

        // AÑADIR ESTO: Moneda principal por defecto (Ej. Soles)
        public Moneda MonedaPrincipal { get; set; } = Moneda.Soles;

        public List<Cuenta> Cuentas { get; set; } = new();
        public List<CategoriaGasto> Categorias { get; set; } = new();
        // Un espacio puede tener muchos usuarios asignados
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}