using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using System.Security.Claims;

namespace PresupuestoFamiliarApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly PresupuestoContext _context;

        public AuthController(PresupuestoContext context) { _context = context; }

        // GET: Mostrar pantalla de Login
        public IActionResult Login()
        {
            // Si ya está logueado, lo mandamos al inicio
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: Procesar Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // AÑADE EL INCLUDE AQUÍ:
            var usuario = await _context.Usuarios.Include(u => u.Espacios).FirstOrDefaultAsync(u => u.NombreUsuario == username);

            if (usuario != null && BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            {
                // Obtenemos todos los IDs de los espacios permitidos separados por comas (ej: "1,3,5")
                string espaciosPermitidos = string.Join(",", usuario.Espacios.Select(e => e.Id));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim("EspaciosPermitidos", espaciosPermitidos) // <-- NUEVO CLAIM
                };

                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Iniciar sesión (Crear la cookie segura)
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            // Si falla
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        // GET: Cerrar sesión
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado() => View();
    }
}