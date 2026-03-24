using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly PresupuestoContext _context;

        public CategoriasController(PresupuestoContext context)
        {
            _context = context;
        }

        // GET: Lista de Categorías (FILTRADAS)
        public async Task<IActionResult> Index()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            var categorias = await _context.CategoriasGastos
                .Where(c => c.EspacioId == espacioActualId)
                .ToListAsync();

            return View(categorias);
        }

        // GET: Mostrar formulario de crear
        public async Task<IActionResult> Create()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

            // Obtener la moneda preferida del usuario
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

            // Si el usuario tiene moneda preferida, la usamos; si no, usamos la del espacio
            var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

            // CAMBIO IMPORTANTE: Crear una instancia del modelo con el valor por defecto
            var nuevaCategoria = new CategoriaGasto
            {
                MonedaCategoria = monedaPorDefecto
            };

            ViewBag.SimboloMoneda = monedaPorDefecto == Moneda.Dolares ? "$" : (monedaPorDefecto == Moneda.Euros ? "€" : "S/");

            return View(nuevaCategoria);
        }

        // POST: Crear Categoría (ASIGNANDO EL ESPACIO Y MONEDA)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Subcategoria,PresupuestoMensual,MonedaCategoria")] CategoriaGasto categoria)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            categoria.EspacioId = espacioActualId;
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Categoría creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MonedaPorDefecto = categoria.MonedaCategoria;
            return View(categoria);
        }

        // GET & POST: Eliminar Categoría
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var categoria = await _context.CategoriasGastos.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.CategoriasGastos.FindAsync(id);
            if (categoria != null)
            {
                _context.CategoriasGastos.Remove(categoria);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Muestra el formulario para editar una categoría específica
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.CategoriasGastos.FindAsync(id);
            if (categoria == null) return NotFound();

            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

            // El símbolo se calcula basado en la moneda de la categoría actual
            var simbolo = categoria.MonedaCategoria == Moneda.Dolares ? "$" :
                          (categoria.MonedaCategoria == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

            return View(categoria);
        }

        // POST: Guarda los cambios del presupuesto en la base de datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Subcategoria,PresupuestoMensual,MonedaCategoria,EspacioId")] CategoriaGasto categoria)
        {
            if (id != categoria.Id) return NotFound();

            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Categoría actualizada exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CategoriasGastos.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }
    }
}