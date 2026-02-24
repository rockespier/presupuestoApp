using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize(Roles = "Administrador")] // <-- CANDADO NIVEL DIOS
    public class UsuariosController : Controller
    {
        private readonly PresupuestoContext _context;

        public UsuariosController(PresupuestoContext context) { _context = context; }

        // GET: Listar Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.Include(u => u.Espacios).AsNoTracking().ToListAsync();
            return View(usuarios);
        }

        // GET: Crear Usuario
        public IActionResult Create()
        {
            // Enviamos la lista completa de espacios para mostrar casillas (checkboxes)
            ViewBag.Espacios = _context.Espacios.ToList();
            return View();
        }

        // POST: Guardar Usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, string PasswordPlain, int[] espaciosSeleccionados)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Espacios");

            if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
            {
                ModelState.AddModelError("NombreUsuario", "Este usuario ya existe.");
            }

            if (ModelState.IsValid && !string.IsNullOrEmpty(PasswordPlain))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordPlain);

                // ASIGNAR LOS ESPACIOS MÚLTIPLES
                if (espaciosSeleccionados != null && espaciosSeleccionados.Length > 0)
                {
                    var espacios = await _context.Espacios.Where(e => espaciosSeleccionados.Contains(e.Id)).ToListAsync();
                    usuario.Espacios = espacios;
                }

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Espacios = _context.Espacios.ToList();
            return View(usuario);
        }
        // GET: Mostrar formulario de Edición
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Traemos al usuario INCLUYENDO los espacios que ya tiene asignados
            var usuario = await _context.Usuarios.Include(u => u.Espacios).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();

            ViewBag.Espacios = await _context.Espacios.ToListAsync();
            return View(usuario);
        }

        // POST: Guardar los cambios del Usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string NombreUsuario, string Rol, string? PasswordPlain, int[] espaciosSeleccionados)
        {
            // 1. Buscamos el usuario original en la base de datos
            var usuarioDb = await _context.Usuarios.Include(u => u.Espacios).FirstOrDefaultAsync(u => u.Id == id);
            if (usuarioDb == null) return NotFound();

            // 2. Validar que no estemos duplicando un nombre de usuario
            if (usuarioDb.NombreUsuario != NombreUsuario && await _context.Usuarios.AnyAsync(u => u.NombreUsuario == NombreUsuario))
            {
                ModelState.AddModelError("NombreUsuario", "Este nombre de usuario ya está en uso.");
                ViewBag.Espacios = await _context.Espacios.ToListAsync();
                return View(usuarioDb);
            }

            // 3. Actualizar datos básicos
            usuarioDb.NombreUsuario = NombreUsuario;
            usuarioDb.Rol = Rol;

            // 4. Actualizar contraseña SOLO si escribió una nueva
            if (!string.IsNullOrEmpty(PasswordPlain))
            {
                usuarioDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordPlain);
            }

            // 5. Actualizar los espacios (Limpiamos los viejos y asignamos los nuevos)
            usuarioDb.Espacios.Clear();
            if (espaciosSeleccionados != null && espaciosSeleccionados.Length > 0)
            {
                var espaciosNuevos = await _context.Espacios.Where(e => espaciosSeleccionados.Contains(e.Id)).ToListAsync();
                usuarioDb.Espacios.AddRange(espaciosNuevos);
            }

            // 6. Guardar en BD
            _context.Update(usuarioDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Eliminar Usuario
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                // SEGURIDAD: Evitar que se elimine el administrador principal por accidente
                if (usuario.Id == 1 || usuario.NombreUsuario.ToLower() == "admin")
                {
                    // Redirigimos sin eliminar
                    return RedirectToAction(nameof(Index));
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}