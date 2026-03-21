using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly PresupuestoContext _context;

        public ConfiguracionController(PresupuestoContext context)
        {
            _context = context;
        }

        // GET: Configuración del usuario
        public async Task<IActionResult> Index()
        {
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Actualizar configuración
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarMonedaPreferida(Moneda monedaPreferida)
        {
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

            if (usuario == null) return NotFound();

            usuario.MonedaPreferida = monedaPreferida;
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Moneda preferida actualizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}