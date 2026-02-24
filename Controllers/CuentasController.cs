using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class CuentasController : Controller
    {
        private readonly PresupuestoContext _context;

        public CuentasController(PresupuestoContext context) { _context = context; }

        // GET: Lista de Cuentas (FILTRADAS)
        public async Task<IActionResult> Index()
        {
            // 1. Leer la cookie
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            // 2. Traer solo las cuentas de este espacio
            var cuentas = await _context.Cuentas
                .Where(c => c.EspacioId == espacioActualId)
                .ToListAsync();

            return View(cuentas);
        }

        // GET & POST: Crear Cuenta
        public IActionResult Create() => View();

        // POST: Crear Cuenta (ASIGNANDO EL ESPACIO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,SaldoActual,EsCredito,MonedaCuenta")] Cuenta cuenta)        
        {
            // 1. Leer la cookie
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            // 2. Asignar el Espacio a la nueva cuenta
            cuenta.EspacioId = espacioActualId;

            // 3. Ignorar la validación del objeto 'Espacio' completo para que no dé error
            ModelState.Remove("Espacio");

            if (ModelState.IsValid)
            {
                _context.Add(cuenta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cuenta);
        }

        // GET & POST: Editar Cuenta
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cuenta = await _context.Cuentas.FindAsync(id);
            if (cuenta == null) return NotFound();
            return View(cuenta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Nombre,SaldoActual,EsCredito,MonedaCuenta")] Cuenta cuenta)
        {
            if (id != cuenta.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(cuenta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cuenta);
        }

        // GET & POST: Eliminar Cuenta
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var cuenta = await _context.Cuentas.FindAsync(id);
            if (cuenta == null) return NotFound();
            return View(cuenta);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cuenta = await _context.Cuentas.FindAsync(id);
            if (cuenta != null)
            {
                _context.Cuentas.Remove(cuenta);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}