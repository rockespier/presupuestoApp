using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class MovimientosFijosController : Controller
    {
        private readonly PresupuestoContext _context;

        public MovimientosFijosController(PresupuestoContext context) { _context = context; }

        // GET: Listar los movimientos fijos del espacio actual
        public async Task<IActionResult> Index()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            var fijos = await _context.MovimientosFijos
                .Include(m => m.Cuenta)
                .Include(m => m.Categoria)
                .Where(m => m.EspacioId == espacioActualId)
                .ToListAsync();

            return View(fijos);
        }

        // GET: Mostrar formulario de creación
        public IActionResult Create()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre");
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre");

            return View();
        }

        // POST: Guardar el nuevo movimiento fijo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Descripcion,Tipo,Monto,DiaDelMes,CuentaId,CategoriaGastoId,Activo,FechaFin")] MovimientoFijo movimientoFijo)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            movimientoFijo.EspacioId = espacioActualId;

            // Evitar errores de validación de los objetos completos
            ModelState.Remove("Cuenta");
            ModelState.Remove("Categoria");
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                // Limpiar la categoría si es un ingreso
                if (movimientoFijo.Tipo == TipoTransaccion.Ingreso)
                {
                    movimientoFijo.CategoriaGastoId = null;
                }

                _context.Add(movimientoFijo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si hay error, recargamos las listas
            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CategoriaGastoId);
            return View(movimientoFijo);
        }

        // GET: Mostrar formulario de edición
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movimientoFijo = await _context.MovimientosFijos.FindAsync(id);
            if (movimientoFijo == null) return NotFound();

            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CategoriaGastoId);

            return View(movimientoFijo);
        }

        // POST: Guardar edición
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Descripcion,Tipo,Monto,DiaDelMes,CuentaId,CategoriaGastoId,Activo,UltimaGeneracion,EspacioId,FechaFin")] MovimientoFijo movimientoFijo)
        {
            if (id != movimientoFijo.Id) return NotFound();

            ModelState.Remove("Cuenta");
            ModelState.Remove("Categoria");
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                if (movimientoFijo.Tipo == TipoTransaccion.Ingreso)
                    movimientoFijo.CategoriaGastoId = null;

                _context.Update(movimientoFijo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            int espacioActualId = movimientoFijo.EspacioId;
            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CategoriaGastoId);
            return View(movimientoFijo);
        }

        // GET & POST: Eliminar
        public async Task<IActionResult> Delete(int? id)
        {
            var mov = await _context.MovimientosFijos.FindAsync(id);
            if (mov != null)
            {
                _context.MovimientosFijos.Remove(mov);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}