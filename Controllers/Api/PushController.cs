using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresupuestoFamiliarApp.Servicios;
using System.Security.Claims;

namespace PresupuestoFamiliarApp.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly PushNotificationService _pushService;

        public PushController(PushNotificationService pushService)
        {
            _pushService = pushService;
        }

        /// <summary>
        /// Obtiene la clave pública VAPID
        /// </summary>
        [HttpGet("public-key")]
        [AllowAnonymous]
        public IActionResult GetPublicKey()
        {
            try
            {
                var publicKey = _pushService.ObtenerClavePublicaVapid();
                return Ok(new { publicKey });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Suscribe al usuario a las notificaciones push
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                // Obtener el ID del usuario autenticado
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                var suscripcion = await _pushService.SuscribirUsuario(
                    usuarioId,
                    request.Endpoint,
                    request.Keys.P256dh,
                    request.Keys.Auth
                );

                return Ok(new
                {
                    success = true,
                    message = "Suscripción exitosa a las notificaciones",
                    subscription = new
                    {
                        suscripcion.Id,
                        suscripcion.FechaCreacion
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Desuscribe al usuario de las notificaciones
        /// </summary>
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            try
            {
                var resultado = await _pushService.DesuscribirUsuario(request.Endpoint);
                
                if (resultado)
                {
                    return Ok(new { success = true, message = "Desuscripción exitosa" });
                }
                
                return NotFound(new { message = "Suscripción no encontrada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envía una notificación de prueba
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioId))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                var resultado = await _pushService.EnviarNotificacion(
                    usuarioId,
                    "?? Notificación de Prueba",
                    "Las notificaciones push están funcionando correctamente!",
                    "/",
                    "/icons/icon-192x192.png"
                );

                if (resultado)
                {
                    return Ok(new { success = true, message = "Notificación de prueba enviada" });
                }

                return BadRequest(new { message = "No se pudo enviar la notificación. Verifica que estés suscrito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTOs para las solicitudes
    public class SubscribeRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public PushKeys Keys { get; set; } = new();
    }

    public class PushKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class UnsubscribeRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
