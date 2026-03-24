using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string NombreUsuario { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Rol { get; set; }

        public List<Espacio> Espacios { get; set; } = new List<Espacio>();

        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; }

        public bool TourCompletado { get; set; } = false;

        // NUEVA PROPIEDAD: Moneda preferida del usuario
        public Moneda MonedaPreferida { get; set; } = Moneda.Soles;
    }
}