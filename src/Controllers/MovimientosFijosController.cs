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
        public async Task<IActionResult> Create()
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            // Obtener la moneda preferida del usuario
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

            // Si el usuario tiene moneda preferida, la usamos; si no, usamos la del espacio
            var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

            // Crear instancia con moneda por defecto
            var nuevoMovimiento = new MovimientoFijo
            {
                MonedaMovimiento = monedaPorDefecto,
                Activo = true
            };

            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre");
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre");
            ViewBag.SimboloMoneda = monedaPorDefecto == Moneda.Dolares ? "$" : (monedaPorDefecto == Moneda.Euros ? "€" : "S/");

            return View(nuevoMovimiento);
        }

        // POST: Guardar el nuevo movimiento fijo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Descripcion,Tipo,Monto,DiaDelMes,CuentaId,CategoriaGastoId,Activo,FechaFin,MonedaMovimiento,FrecuenciaRepeticion")] MovimientoFijo movimientoFijo)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            movimientoFijo.EspacioId = espacioActualId;

            ModelState.Remove("Cuenta");
            ModelState.Remove("Categoria");
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                if (movimientoFijo.Tipo == TipoTransaccion.Ingreso)
                {
                    movimientoFijo.CategoriaGastoId = null;
                }

                _context.Add(movimientoFijo);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Movimiento automático creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CategoriaGastoId);

            var simbolo = movimientoFijo.MonedaMovimiento == Moneda.Dolares ? "$" : (movimientoFijo.MonedaMovimiento == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

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

            var simbolo = movimientoFijo.MonedaMovimiento == Moneda.Dolares ? "$" : (movimientoFijo.MonedaMovimiento == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

            return View(movimientoFijo);
        }

        // POST: Guardar edición
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Descripcion,Tipo,Monto,DiaDelMes,CuentaId,CategoriaGastoId,Activo,UltimaGeneracion,EspacioId,FechaFin,MonedaMovimiento")] MovimientoFijo movimientoFijo)
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
                TempData["Exito"] = "Movimiento automático actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            int espacioActualId = movimientoFijo.EspacioId;
            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", movimientoFijo.CategoriaGastoId);

            var simbolo = movimientoFijo.MonedaMovimiento == Moneda.Dolares ? "$" : (movimientoFijo.MonedaMovimiento == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

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
                TempData["Exito"] = "Movimiento automático eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}