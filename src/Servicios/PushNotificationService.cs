using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using WebPush;
using Newtonsoft.Json;
using PushSubscriptionModel = PresupuestoFamiliarApp.Models.PushSubscription;

namespace PresupuestoFamiliarApp.Servicios
{
    public class PushNotificationService
    {
        private readonly PresupuestoContext _context;
        private readonly IConfiguration _configuration;
        private readonly VapidDetails _vapidDetails;

        public PushNotificationService(PresupuestoContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // Obtener las claves VAPID desde la configuración
            _vapidDetails = new VapidDetails(
                subject: _configuration["Vapid:Subject"] ?? "mailto:mispresupuestos.app@gmail.com",
                publicKey: _configuration["Vapid:PublicKey"] ?? GenerateVapidKeys().PublicKey,
                privateKey: _configuration["Vapid:PrivateKey"] ?? GenerateVapidKeys().PrivateKey
            );
        }

        /// <summary>
        /// Genera claves VAPID si no existen
        /// </summary>
        private VapidDetails GenerateVapidKeys()
        {
            var keys = VapidHelper.GenerateVapidKeys();
            Console.WriteLine("?? VAPID Keys generadas automáticamente:");
            Console.WriteLine($"Public Key: {keys.PublicKey}");
            Console.WriteLine($"Private Key: {keys.PrivateKey}");
            Console.WriteLine("?? Guarda estas claves en appsettings.json");
            
            return new VapidDetails("mailto:admin@presupuesto.com", keys.PublicKey, keys.PrivateKey);
        }

        /// <summary>
        /// Suscribe a un usuario a las notificaciones push
        /// </summary>
        public async Task<PushSubscriptionModel> SuscribirUsuario(int usuarioId, string endpoint, string p256dh, string auth)
        {
            // Verificar si ya existe una suscripción activa para este endpoint
            var suscripcionExistente = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint && s.UsuarioId == usuarioId);

            if (suscripcionExistente != null)
            {
                // Actualizar la suscripción existente
                suscripcionExistente.P256dh = p256dh;
                suscripcionExistente.Auth = auth;
                suscripcionExistente.Activa = true;
                suscripcionExistente.FechaCreacion = DateTime.Now;
                await _context.SaveChangesAsync();
                return suscripcionExistente;
            }

            // Crear nueva suscripción
            var nuevaSuscripcion = new PushSubscriptionModel
            {
                UsuarioId = usuarioId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                Activa = true
            };

            _context.PushSubscriptions.Add(nuevaSuscripcion);
            await _context.SaveChangesAsync();

            return nuevaSuscripcion;
        }

        /// <summary>
        /// Desuscribe a un usuario de las notificaciones
        /// </summary>
        public async Task<bool> DesuscribirUsuario(string endpoint)
        {
            var suscripcion = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (suscripcion != null)
            {
                suscripcion.Activa = false;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Envía una notificación push a un usuario específico
        /// </summary>
        public async Task<bool> EnviarNotificacion(int usuarioId, string titulo, string mensaje, string? url = null, string? icono = null)
        {
            var suscripciones = await _context.PushSubscriptions
                .Where(s => s.UsuarioId == usuarioId && s.Activa)
                .ToListAsync();

            if (!suscripciones.Any())
            {
                Console.WriteLine($"?? No hay suscripciones activas para el usuario {usuarioId}");
                return false;
            }

            var payload = new
            {
                notification = new
                {
                    title = titulo,
                    body = mensaje,
                    icon = icono ?? "/icons/icon-192x192.png",
                    badge = "/icons/badge-72x72.png",
                    vibrate = new[] { 100, 50, 100 },
                    data = new
                    {
                        url = url ?? "/",
                        dateOfArrival = DateTime.Now
                    },
                    actions = new[]
                    {
                        new { action = "view", title = "Ver" },
                        new { action = "close", title = "Cerrar" }
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var webPushClient = new WebPushClient();
            bool enviadoExitosamente = false;

            foreach (var suscripcion in suscripciones)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        suscripcion.Endpoint,
                        suscripcion.P256dh,
                        suscripcion.Auth
                    );

                    await webPushClient.SendNotificationAsync(pushSubscription, jsonPayload, _vapidDetails);
                    enviadoExitosamente = true;
                    Console.WriteLine($"? Notificación enviada a {suscripcion.Endpoint.Substring(0, 50)}...");
                }
                catch (WebPushException ex)
                {
                    Console.WriteLine($"? Error al enviar notificación: {ex.Message}");
                    
                    // Si la suscripción expiró o es inválida, la marcamos como inactiva
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        suscripcion.Activa = false;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error inesperado: {ex.Message}");
                }
            }

            return enviadoExitosamente;
        }

        /// <summary>
        /// Envía notificaciones de vencimientos próximos
        /// </summary>
        public async Task NotificarVencimientosProximos()
        {
            var hoy = DateTime.Now.Date;

            // 🔍 DIAGNÓSTICO: Log para ver qué está pasando
            Console.WriteLine("========================================");
            Console.WriteLine("🔔 Iniciando NotificarVencimientosProximos");
            Console.WriteLine($"📅 Fecha actual: {hoy:yyyy-MM-dd}");
            Console.WriteLine("========================================");

            // Obtener todas las suscripciones activas con sus usuarios
            var suscripciones = await _context.PushSubscriptions
                .Include(s => s.Usuario)
                .Where(s => s.Activa && s.NotificarVencimientos)
                .ToListAsync();

            Console.WriteLine($"👥 Suscripciones activas encontradas: {suscripciones.Count}");

            if (!suscripciones.Any())
            {
                Console.WriteLine("⚠️ NO HAY SUSCRIPCIONES ACTIVAS");
                Console.WriteLine("   Solución: Ve a /Configuracion y activa las notificaciones");
                Console.WriteLine("========================================");
                return;
            }

            foreach (var suscripcion in suscripciones)
            {
                var usuario = suscripcion.Usuario;
                if (usuario == null)
                {
                    Console.WriteLine($"⚠️ Suscripción {suscripcion.Id} no tiene usuario asociado");
                    continue;
                }

                Console.WriteLine($"\n📧 Procesando usuario: {usuario.NombreUsuario} (ID: {usuario.Id})");

                // Obtener espacios del usuario
                var espaciosUsuario = await _context.Espacios
                    .Where(e => e.Usuarios.Any(u => u.Id == usuario.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                Console.WriteLine($"   🏠 Espacios del usuario: {espaciosUsuario.Count}");

                // Buscar cuentas por cobrar próximas a vencer
                var fechaLimite = hoy.AddDays(suscripcion.DiasAnticipacion);
                Console.WriteLine($"   📅 Buscando vencimientos desde {hoy:yyyy-MM-dd} hasta {fechaLimite:yyyy-MM-dd}");
                
                var deudasProximas = await _context.CuentasPorCobrar
                    .Include(c => c.Deudor)
                    .Where(c => espaciosUsuario.Contains(c.Deudor.EspacioId)
                    && (c.MontoTotal - c.MontoPagado) > 0
                    && c.FechaVencimiento <= fechaLimite
                    && c.FechaVencimiento >= hoy)
                    .ToListAsync();

                Console.WriteLine($"   💰 Deudas próximas encontradas: {deudasProximas.Count}");

                if (!deudasProximas.Any())
                {
                    Console.WriteLine($"   ⚠️ No hay deudas próximas a vencer para {usuario.NombreUsuario}");
                    continue;
                }

                foreach (var deuda in deudasProximas)
                {
                    var diasRestantes = (deuda.FechaVencimiento - hoy).Days;
                    var mensaje = diasRestantes == 0
                        ? $"{deuda.Deudor.Nombre} - {deuda.Concepto} vence HOY. Pendiente: {deuda.MonedaDeuda} {deuda.SaldoPendiente:N2}"
                        : $"{deuda.Deudor.Nombre} - {deuda.Concepto} vence en {diasRestantes} día(s). Pendiente: {deuda.MonedaDeuda} {deuda.SaldoPendiente:N2}";

                    Console.WriteLine($"   📨 Enviando notificación: {mensaje}");

                    var enviado = await EnviarNotificacion(
                        usuario.Id,
                        "💰 Vencimiento Próximo",
                        mensaje,
                        $"/CuentasPorCobrar/Detalle/{deuda.DeudorId}",
                        "/icons/icon-192x192.png"
                    );

                    if (enviado)
                    {
                        Console.WriteLine($"   ✅ Notificación enviada exitosamente");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Error al enviar notificación");
                    }
                }
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("✅ NotificarVencimientosProximos completado");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// Envía notificaciones de presupuestos excedidos
        /// </summary>
        public async Task NotificarPresupuestosExcedidos()
        {
            var mesActual = DateTime.Now.Month;
            var anioActual = DateTime.Now.Year;

            // Cargar todos los gastos del mes en una sola consulta y agrupar en memoria
            // para evitar el problema N+1 de ejecutar una query por cada categoría
            var gastosPorCategoria = await _context.Transacciones
                .Where(t => t.Fecha.Month == mesActual && t.Fecha.Year == anioActual
                         && t.Tipo == TipoTransaccion.Egreso && !t.EsTransferencia
                         && t.CategoriaGastoId != null)
                .GroupBy(t => t.CategoriaGastoId)
                .Select(g => new { CategoriaId = g.Key, Total = g.Sum(t => t.Monto) })
                .ToDictionaryAsync(g => g.CategoriaId!.Value, g => g.Total);

            var suscripciones = await _context.PushSubscriptions
                .Include(s => s.Usuario)
                .Where(s => s.Activa && s.NotificarPresupuestos)
                .ToListAsync();

            foreach (var suscripcion in suscripciones)
            {
                var usuario = suscripcion.Usuario;
                if (usuario == null) continue;

                // Obtener espacios del usuario
                var espaciosUsuario = await _context.Espacios
                    .Where(e => e.Usuarios.Any(u => u.Id == usuario.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                // Obtener categorías con presupuesto del mes actual
                var categorias = await _context.CategoriasGastos
                    .Where(c => espaciosUsuario.Contains(c.EspacioId))
                    .ToListAsync();

                foreach (var categoria in categorias)
                {
                    // Consultar el total desde el diccionario en memoria (sin roundtrip a la BD)
                    var gastosMes = gastosPorCategoria.TryGetValue(categoria.Id, out var totalGastos) ? totalGastos : 0m;

                    // Notificar si se excedió el 90% del presupuesto
                    if (gastosMes >= categoria.PresupuestoMensual * 0.9m)
                    {
                        if (categoria.PresupuestoMensual == 0) continue;
                        var porcentaje = (int)(gastosMes / categoria.PresupuestoMensual * 100);
                        var mensaje = porcentaje >= 100
                            ? $"Has excedido el presupuesto de {categoria.Nombre} en un {porcentaje}%"
                            : $"Has gastado el {porcentaje}% del presupuesto de {categoria.Nombre}";

                        await EnviarNotificacion(
                            usuario.Id,
                            "?? Alerta de Presupuesto",
                            mensaje,
                            "/Categorias/Index",
                            "/icons/icon-192x192.png"
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene la clave pública VAPID para el frontend
        /// </summary>
        public string ObtenerClavePublicaVapid()
        {
            return _vapidDetails.PublicKey;
        }
    }
}
