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

        public EspaciosController(PresupuestoContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var espacios = await _context.Espacios.AsNoTracking().ToListAsync();
            return View(espacios);
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
        public async Task<IActionResult> Create([Bind("Nombre")] Espacio espacio)
        {
            if (ModelState.IsValid)
            {
                _context.Add(espacio);
                await _context.SaveChangesAsync();

                // Al crearlo, nos cambiamos a él automáticamente
                return RedirectToAction("Cambiar", new { id = espacio.Id });
            }
            return View(espacio);
        }
    }
}