using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.Servicios;
using System.Security.Claims;

namespace PresupuestoFamiliarApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly PresupuestoContext _context;
        // Aggiungi il campo privato per EmailService
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env; // Para acceder a las rutas físicas

        public AuthController(PresupuestoContext context, EmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _env = env;
        }

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
        public async Task<IActionResult> Login(string nombreUsuario, string password)
        {
            // CAMBIO: Buscar por email O nombre de usuario
            var usuario = await _context.Usuarios
                .Include(u => u.Espacios)
                .FirstOrDefaultAsync(u => u.Email == nombreUsuario || u.NombreUsuario == nombreUsuario);

            if (usuario != null && BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            {
                // Obtenemos todos los IDs de los espacios permitidos separados por comas (ej: "1,3,5")
                string espaciosPermitidos = string.Join(",", usuario.Espacios.Select(e => e.Id));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim("EspaciosPermitidos", espaciosPermitidos)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Iniciar sesión (Crear la cookie segura)
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            // Si falla
            TempData["Error"] = "Usuario/Email o contraseña incorrectos.";
            return RedirectToAction("Login");
        }

        // GET: Cerrar sesión
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado() => View();

        // GET: Vista para pedir el correo
        [AllowAnonymous]
        public IActionResult OlvidePassword() => View();

        // POST: Generar token y enviar correo
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> OlvidePassword(string email)
        {
            // Depuración rápida: Verás esto en la consola "Salida" de Visual Studio
            Console.WriteLine($"Buscando recuperación para: {email}");

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Por favor, ingresa un correo electrónico válido.";
                return View();
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario != null)
            {
                usuario.PasswordResetToken = Guid.NewGuid().ToString();
                usuario.ResetTokenExpires = DateTime.Now.AddHours(2);
                await _context.SaveChangesAsync();

                var link = Url.Action("ResetPassword", "Auth", new { token = usuario.PasswordResetToken }, Request.Scheme);

                try
                {
                    // 1. Leer la plantilla HTML desde el disco
                    string rutaPlantilla = Path.Combine(_env.ContentRootPath, "Html", "reset_password.html");
                    string plantillaHtml = await System.IO.File.ReadAllTextAsync(rutaPlantilla);

                    // 2. Reemplazar los placeholders con los valores reales
                    plantillaHtml = plantillaHtml.Replace("{UsuarioNombre}", usuario.NombreUsuario);
                    plantillaHtml = plantillaHtml.Replace("{enlace_reset_password}", link);

                    // 3. Enviar el correo con la plantilla HTML procesada
                    await _emailService.EnviarCorreo(usuario.Email, "Recuperar Contraseña", plantillaHtml);

                    TempData["Exito"] = "Revisa tu bandeja de entrada, hemos enviado el enlace.";
                }
                catch (Exception ex)
                {
                    // Si el SMTP falla, lo sabremos aquí
                    TempData["Error"] = "Hubo un problema enviando el correo. Revisa la configuración SMTP.";
                    Console.WriteLine($"ERROR al enviar correo: {ex.Message}");
                }
            }
            else
            {
                // Seguridad: No confirmamos si el correo existe o no para evitar rastreo
                TempData["Exito"] = "Si el correo está registrado, recibirás un enlace en breve.";
            }

            return RedirectToAction("Login");
        }

        // GET: Vista para poner la nueva contraseña
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.PasswordResetToken == token && u.ResetTokenExpires > DateTime.Now);
            if (usuario == null) return Content("El enlace expiró o es inválido.");

            // CORRECCIÓN: Usar ViewBag en lugar de un objeto anónimo
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarResetPassword(string token, string newPassword)
        {
            // Agregar validación de entrada
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "Token o contraseña no válidos.";
                return RedirectToAction("Login");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.ResetTokenExpires > DateTime.Now);

            if (usuario != null)
            {
                // Hash de la nueva contraseña
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                usuario.PasswordResetToken = null; // Limpiar el token
                usuario.ResetTokenExpires = null;

                // Marcar la entidad como modificada
                _context.Entry(usuario).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Contraseña actualizada. Ya puedes iniciar sesión.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al guardar la contraseña: {ex.Message}");
                    TempData["Error"] = "Hubo un error al actualizar la contraseña. Intenta de nuevo.";
                    return RedirectToAction("ResetPassword", new { token });
                }
            }

            TempData["Error"] = "El enlace no es válido o ha expirado.";
            return RedirectToAction("Login");
        }

        // 1. Mostrar la vista de Registro
        [AllowAnonymous]
        public IActionResult Registro() => View();

        // 2. Procesar el formulario de Registro
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Registro(string nombre, string email, string password)
        {
            // Verificar si el correo ya existe
            if (await _context.Usuarios.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(string.Empty, "Este correo electrónico ya está registrado.");
                return View();
            }

            // A. CREAR EL USUARIO
            var nuevoUsuario = new Usuario
            {
                NombreUsuario = nombre,
                Email = email,
                // Nota: En producción, aquí debes encriptar la contraseña (ej. con BCrypt)
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "Usuario" // Le damos el rol normal, no Administrador
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync(); // Guardamos para que se genere el nuevoUsuario.Id

            // B. CREAR SU PRIMER ESPACIO AUTOMÁTICAMENTE
            var primerEspacio = new Espacio
            {
                Nombre = "Mi Presupuesto Personal"
            };

            // ¡LA MAGIA SUCEDE AQUÍ! 
            // Metemos al nuevo usuario en la lista del espacio.
            primerEspacio.Usuarios.Add(nuevoUsuario);

            // Al guardar el espacio, Entity Framework guardará también al usuario 
            // y hará la conexión en la base de datos automáticamente.
            _context.Espacios.Add(primerEspacio);
            await _context.SaveChangesAsync();

            // C. CREARLE UNA CUENTA POR DEFECTO PARA QUE PUEDA EMPEZAR
            var cuentaInicial = new Cuenta
            {
                Nombre = "Billetera / Efectivo",
                SaldoActual = 0,
                EspacioId = primerEspacio.Id
            };

            _context.Cuentas.Add(cuentaInicial);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "¡Cuenta creada con éxito! Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }
    }
}