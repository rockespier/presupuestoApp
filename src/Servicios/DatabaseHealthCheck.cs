using Microsoft.Extensions.Diagnostics.HealthChecks;
using PresupuestoFamiliarApp.Data;
using Microsoft.EntityFrameworkCore;

namespace PresupuestoFamiliarApp.Servicios
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly PresupuestoContext _context;

        public DatabaseHealthCheck(PresupuestoContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Intenta ejecutar una consulta simple
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                
                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("No se puede conectar a la base de datos");
                }

                // Opcional: verificar que hay al menos un usuario
                var hasData = await _context.Usuarios.AnyAsync(cancellationToken);
                
                return HealthCheckResult.Healthy(
                    $"Base de datos conectada correctamente. Usuarios: {(hasData ? "?" : "?")}"
                );
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Error al conectar con la base de datos: {ex.Message}",
                    exception: ex
                );
            }
        }
    }
}