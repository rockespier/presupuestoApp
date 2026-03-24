using System.ComponentModel.DataAnnotations;

namespace PresupuestoFamiliarApp.Models
{
    public class PushSubscription
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        public string? P256dh { get; set; }

        public string? Auth { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activa { get; set; } = true;

        // Preferencias de notificación
        public bool NotificarVencimientos { get; set; } = true;
        public bool NotificarPresupuestos { get; set; } = true;
        public bool NotificarMovimientos { get; set; } = true;

        // Días de anticipación para notificar vencimientos
        public int DiasAnticipacion { get; set; } = 3;
    }
}
