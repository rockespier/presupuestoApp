using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class TransferenciasController : Controller
    {
        private readonly PresupuestoContext _context;

        public TransferenciasController(PresupuestoContext context) { _context = context; }

        // GET: Mostrar el formulario vacío (con soporte para pre-seleccionar destino)
        public async Task<IActionResult> Create(int? destinoId)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            var cuentas = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
            ViewBag.Cuentas = new SelectList(cuentas, "Id", "Nombre");

            // NUEVO: Obtener la moneda preferida del usuario
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

            // Si el usuario tiene moneda preferida, la usamos; si no, usamos la del espacio
            var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

            // Creamos el modelo con la moneda por defecto
            var modelo = new TransferenciaViewModel
            {
                MonedaTransferencia = monedaPorDefecto,
                Fecha = DateTime.Now
            };

            // Si recibimos un ID por la URL (ej: al hacer clic en "Pagar Tarjeta")
            if (destinoId.HasValue)
            {
                modelo.CuentaDestinoId = destinoId.Value;
                modelo.Descripcion = "Pago de Tarjeta de Crédito";
            }

            // Establecer el símbolo de moneda
            ViewBag.SimboloMoneda = monedaPorDefecto == Moneda.Dolares ? "$" :
                                   (monedaPorDefecto == Moneda.Euros ? "€" : "S/");

            return View(modelo);
        }

        // POST: Procesar la transferencia multimoneda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferenciaViewModel modelo)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            if (modelo.CuentaOrigenId == modelo.CuentaDestinoId)
            {
                ModelState.AddModelError("", "La cuenta de origen y destino no pueden ser la misma.");
            }

            if (ModelState.IsValid)
            {
                var cuentaOrigen = await _context.Cuentas.FindAsync(modelo.CuentaOrigenId);
                var cuentaDestino = await _context.Cuentas.FindAsync(modelo.CuentaDestinoId);
                var espacio = await _context.Espacios.FindAsync(espacioActualId);

                // --- 1. Tasa para la Cuenta ORIGEN (Descuento) ---
                decimal tasaOrigen = 1m;
                if (modelo.MonedaTransferencia != cuentaOrigen.MonedaCuenta)
                {
                    var tcOrigen = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == modelo.MonedaTransferencia && t.MonedaDestino == cuentaOrigen.MonedaCuenta);
                    if (tcOrigen == null) return ErrorDeTasa(modelo, modelo.MonedaTransferencia, cuentaOrigen.MonedaCuenta, espacioActualId);
                    tasaOrigen = tcOrigen.Tasa;
                }
                decimal montoDescontar = Math.Round(modelo.Monto * tasaOrigen, 2);

                // --- 2. Tasa para la Cuenta DESTINO (Aumento) ---
                decimal tasaDestino = 1m;
                if (modelo.MonedaTransferencia != cuentaDestino.MonedaCuenta)
                {
                    var tcDestino = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == modelo.MonedaTransferencia && t.MonedaDestino == cuentaDestino.MonedaCuenta);
                    if (tcDestino == null) return ErrorDeTasa(modelo, modelo.MonedaTransferencia, cuentaDestino.MonedaCuenta, espacioActualId);
                    tasaDestino = tcDestino.Tasa;
                }
                decimal montoSumar = Math.Round(modelo.Monto * tasaDestino, 2);

                // --- 3. Tasa para el ESPACIO (Historial) ---
                decimal tasaEspacio = 1m;
                if (modelo.MonedaTransferencia != espacio.MonedaPrincipal)
                {
                    var tcEspacio = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == modelo.MonedaTransferencia && t.MonedaDestino == espacio.MonedaPrincipal);
                    if (tcEspacio == null) return ErrorDeTasa(modelo, modelo.MonedaTransferencia, espacio.MonedaPrincipal, espacioActualId);
                    tasaEspacio = tcEspacio.Tasa;
                }
                decimal montoPresupuesto = Math.Round(modelo.Monto * tasaEspacio, 2);

                // --- APLICAR MATEMÁTICAS ---
                cuentaOrigen.SaldoActual -= montoDescontar;
                cuentaDestino.SaldoActual += montoSumar;

                // Crear los dos registros para el historial
                var egreso = new Transaccion
                {
                    CuentaId = cuentaOrigen.Id,
                    Tipo = TipoTransaccion.Egreso,
                    MontoOriginal = modelo.Monto,
                    MonedaTransaccion = modelo.MonedaTransferencia,
                    TasaCambioUsada = tasaEspacio,
                    Monto = montoPresupuesto,
                    Descripcion = $"{modelo.Descripcion} (Hacia {cuentaDestino.Nombre})",
                    Fecha = modelo.Fecha,
                    EsTransferencia = true
                };

                var ingreso = new Transaccion
                {
                    CuentaId = cuentaDestino.Id,
                    Tipo = TipoTransaccion.Ingreso,
                    MontoOriginal = modelo.Monto,
                    MonedaTransaccion = modelo.MonedaTransferencia,
                    TasaCambioUsada = tasaEspacio,
                    Monto = montoPresupuesto,
                    Descripcion = $"{modelo.Descripcion} (Desde {cuentaOrigen.Nombre})",
                    Fecha = modelo.Fecha,
                    EsTransferencia = true
                };

                _context.AddRange(egreso, ingreso);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Transferencia realizada exitosamente.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Cuentas = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre");

            // Establecer el símbolo para cuando hay error
            var simbolo = modelo.MonedaTransferencia == Moneda.Dolares ? "$" :
                         (modelo.MonedaTransferencia == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

            return View(modelo);
        }

        // Método auxiliar para no repetir código de error
        private IActionResult ErrorDeTasa(TransferenciaViewModel modelo, Moneda origen, Moneda destino, int espacioId)
        {
            ModelState.AddModelError("", $"Falta tasa de cambio de {origen} a {destino}. Ve a la sección 'Tipos de Cambio' y regístrala.");
            ViewBag.Cuentas = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioId), "Id", "Nombre");

            var simbolo = modelo.MonedaTransferencia == Moneda.Dolares ? "$" :
                         (modelo.MonedaTransferencia == Moneda.Euros ? "€" : "S/");
            ViewBag.SimboloMoneda = simbolo;

            return View(modelo);
        }
    }
}