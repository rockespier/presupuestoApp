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
            // BUSCAR LA MONEDA DEL ESPACIO
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            var categorias = await _context.CategoriasGastos
                .Where(c => c.EspacioId == espacioActualId)
                .ToListAsync();

            return View(categorias);
        }

        // GET & POST: Crear Categoría
        // GET: Mostrar formulario de crear
        public async Task<IActionResult> Create()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            return View();
        }

        // POST: Crear Categoría (ASIGNANDO EL ESPACIO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Subcategoria,PresupuestoMensual")] CategoriaGasto categoria)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            categoria.EspacioId = espacioActualId;
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
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
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            return View(categoria);
        }

        // POST: Guarda los cambios del presupuesto en la base de datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Subcategoria,PresupuestoMensual")] CategoriaGasto categoria)
        {
            if (id != categoria.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CategoriasGastos.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                // Si todo sale bien, regresamos a la lista de presupuestos
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }
    }
}