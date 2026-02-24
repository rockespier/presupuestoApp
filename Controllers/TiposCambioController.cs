using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class TiposCambioController : Controller
    {
        private readonly PresupuestoContext _context;

        public TiposCambioController(PresupuestoContext context) { _context = context; }

        // GET: Lista de tasas de cambio
        public async Task<IActionResult> Index()
        {
            return View(await _context.TiposCambio.OrderByDescending(t => t.FechaActualizacion).ToListAsync());
        }

        // GET: Formulario para registrar o actualizar una tasa
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MonedaOrigen,MonedaDestino,Tasa")] TipoCambio tipoCambio)
        {
            if (ModelState.IsValid)
            {
                // Buscar si ya existe esta combinación (Ej: Dólares a Soles) para actualizarla en lugar de duplicarla
                var existe = await _context.TiposCambio
                    .FirstOrDefaultAsync(t => t.MonedaOrigen == tipoCambio.MonedaOrigen && t.MonedaDestino == tipoCambio.MonedaDestino);

                if (existe != null)
                {
                    existe.Tasa = tipoCambio.Tasa;
                    existe.FechaActualizacion = DateTime.Now;
                    _context.Update(existe);
                }
                else
                {
                    tipoCambio.FechaActualizacion = DateTime.Now;
                    _context.Add(tipoCambio);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipoCambio);
        }

        // GET: Eliminar una tasa
        public async Task<IActionResult> Delete(int? id)
        {
            var tc = await _context.TiposCambio.FindAsync(id);
            if (tc != null)
            {
                _context.TiposCambio.Remove(tc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}