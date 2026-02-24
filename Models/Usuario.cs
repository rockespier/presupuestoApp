using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string NombreUsuario { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Aquí guardaremos la contraseña encriptada

        [Required]
        public string Rol { get; set; } // "Administrador" o "Usuario"

        // El Administrador puede tener esto en null (porque ve todo), 
        // pero el Usuario obligatoriamente pertenece a un solo presupuesto.
        public List<Espacio> Espacios { get; set; } = new List<Espacio>();
    }
}