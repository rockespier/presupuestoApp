using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class EspaciosController : Controller
    {
        private readonly PresupuestoContext _context;

        public EspaciosController(PresupuestoContext context) { 
            _context = context; 
        }

        private async Task<Usuario> ObtenerUsuarioActual()
        {
            // Buscamos al usuario en la BD usando el nombre/email de su sesión activa
            return await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.Email == User.Identity.Name || u.NombreUsuario == User.Identity.Name);
        }

        public async Task<IActionResult> Index()
        {
            var usuarioActual = await ObtenerUsuarioActual();
            if (usuarioActual == null) return RedirectToAction("Login", "Auth"); // Por seguridad

            // LA MAGIA DE LA SEGURIDAD: 
            // "Tráeme los espacios DONDE al menos uno (.Any) de sus usuarios tenga mi ID"
            var misEspacios = await _context.Espacios
                .Where(e => e.Usuarios.Any(u => u.Id == usuarioActual.Id))
                .ToListAsync();

            return View(misEspacios);
        }

        // Acción mágica: Cambia la cookie y recarga la página
        public IActionResult Cambiar(int id)
        {
            // Guardamos el EspacioId en una cookie que dura 1 año
            Response.Cookies.Append("EspacioActivoId", id.ToString(), new CookieOptions { Expires = DateTimeOffset.Now.AddYears(1) });
            return RedirectToAction("Index", "Home");
        }

        // GET: Formulario para crear un nuevo presupuesto/espacio
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,MonedaPrincipal")] Espacio espacio)
        {
            ModelState.Remove("Usuarios");

            if (ModelState.IsValid)
            {
                var usuarioActual = await ObtenerUsuarioActual();

                // ¡Súper importante! Metemos al usuario actual como dueño de este nuevo espacio
                espacio.Usuarios.Add(usuarioActual);

                _context.Add(espacio);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Espacio creado correctamente.";
                // Al crearlo, nos cambiamos a él automáticamente
                //return RedirectToAction("Cambiar", new { id = espacio.Id });
                return RedirectToAction(nameof(Index));
            }
            return View(espacio);
        }

        // GET: Editar Espacio
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var espacio = await _context.Espacios.FindAsync(id);
            if (espacio == null) return NotFound();

            return View(espacio);
        }

        // POST: Guardar cambios del Espacio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,MonedaPrincipal")] Espacio espacio)
        {
            if (id != espacio.Id) return NotFound();

            ModelState.Remove("Usuarios");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(espacio);
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Espacio actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await EspacioExists(espacio.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(espacio);
        }

        private async Task<bool> EspacioExists(int id)
        {
            return await _context.Espacios.AnyAsync(e => e.Id == id);
        }
    }
}