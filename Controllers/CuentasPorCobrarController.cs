using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Controllers;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;

[Authorize]
public class CuentasPorCobrarController : BaseController
{
    public CuentasPorCobrarController(PresupuestoContext context) : base(context) { }


    // 1. LISTA DE INQUILINOS / DEUDORES
    public async Task<IActionResult> Index()
    {
        int espacioId = ObtenerEspacioActivoId();


        var deudores = await _context.Deudores
            .Include(d => d.CuentasPorCobrar)
            .Where(d => d.EspacioId == espacioId)
            .ToListAsync();

        return View(deudores);
    }

    // 2. CREAR NUEVO INQUILINO
    public IActionResult CrearDeudor() => View();

    [HttpPost]
    public async Task<IActionResult> CrearDeudor(Deudor deudor)
    {
        // 1. Le decimos a .NET que no espere estos datos desde el formulario HTML
        ModelState.Remove("Espacio");
        ModelState.Remove("CuentasPorCobrar");

        if (ModelState.IsValid)
        {
            deudor.EspacioId = ObtenerEspacioActivoId();
            _context.Add(deudor);
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Deudor registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        return View(deudor);
    }

    // 3. VER DETALLE DEL INQUILINO (Sus deudas)
    public async Task<IActionResult> Detalle(int id)
    {
        int espacioId = ObtenerEspacioActivoId();

        // AÑADIDO: Obtener el símbolo de moneda del espacio
        var espacioActivo = await _context.Espacios.FindAsync(espacioId);
        ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal switch
        {
            Moneda.Dolares => "$",
            Moneda.Euros => "€",
            _ => "S/"
        };

        var deudor = await _context.Deudores
            .Include(d => d.CuentasPorCobrar)
            .FirstOrDefaultAsync(d => d.Id == id && d.EspacioId == espacioId);

        if (deudor == null) return NotFound();
        return View(deudor);
    }

    // 4. AGREGAR UNA NUEVA DEUDA (Ej: Alquiler de Abril)
    public async Task<IActionResult> NuevaDeuda(int deudorId)
    {
        // Obtener el usuario actual
        var nombreUsuario = User.Identity?.Name;
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        // Obtener el espacio actual
        int espacioActualId = ObtenerEspacioActivoId();
        var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

        // Usar la moneda preferida del usuario, o la del espacio como fallback
        var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

        // Crear instancia de deuda con moneda por defecto
        var nuevaDeuda = new CuentaPorCobrar
        {
            DeudorId = deudorId,
            MonedaDeuda = monedaPorDefecto,
            FechaVencimiento = DateTime.Now.AddMonths(1) // Sugerir vencimiento 1 mes adelante
        };

        ViewBag.DeudorId = deudorId;
        ViewBag.SimboloMoneda = monedaPorDefecto switch
        {
            Moneda.Dolares => "$",
            Moneda.Euros => "€",
            _ => "S/"
        };

        return View(nuevaDeuda);
    }

    [HttpPost]
    public async Task<IActionResult> NuevaDeuda(CuentaPorCobrar cuenta)
    {
        ModelState.Remove("Deudor");

        if (ModelState.IsValid)
        {
            _context.Add(cuenta);
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Deuda asignada correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = cuenta.DeudorId });
        }

        ViewBag.DeudorId = cuenta.DeudorId;
        ViewBag.SimboloMoneda = cuenta.MonedaDeuda switch
        {
            Moneda.Dolares => "$",
            Moneda.Euros => "€",
            _ => "S/"
        };

        return View(cuenta);
    }

    // 5. VISTA PARA REGISTRAR PAGO (GET)
    public async Task<IActionResult> RegistrarPago(int id)
    {
        var deuda = await _context.CuentasPorCobrar
            .Include(c => c.Deudor)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (deuda == null || deuda.EstaPagado) return NotFound();

        // Necesitamos una lista de tus cuentas (Efectivo, Banco, etc.) para saber dónde entra el dinero
        int espacioId = ObtenerEspacioActivoId();

        // AÑADIDO: Obtener el símbolo de moneda del espacio
        var espacioActivo = await _context.Espacios.FindAsync(espacioId);
        ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal switch
        {
            Moneda.Dolares => "$",
            Moneda.Euros => "€",
            _ => "S/"
        };

        ViewBag.CuentasDestino = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioId), "Id", "Nombre");

        return View(deuda);
    }

    // 6. PROCESAR EL PAGO (POST)
    [HttpPost]
    public async Task<IActionResult> RegistrarPago(int id, decimal montoPago, int cuentaDestinoId)
    {
        var deuda = await _context.CuentasPorCobrar
            .Include(c => c.Deudor)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (deuda == null) return NotFound();

        // Validar que no nos pague más de lo que debe o números negativos
        if (montoPago <= 0 || montoPago > deuda.SaldoPendiente)
        {
            TempData["Error"] = "El monto ingresado no es válido.";
            return RedirectToAction(nameof(RegistrarPago), new { id });
        }

        // PASO 1: Actualizar cuánto ha pagado de esta deuda
        deuda.MontoPagado += montoPago;

        // PASO 2: Crear el Ingreso Automático en nuestro presupuesto
        var ingreso = new Transaccion
        {
            CuentaId = cuentaDestinoId,
            Monto = montoPago,
            Fecha = DateTime.Now,
            Tipo = TipoTransaccion.Ingreso,
            Descripcion = $"Pago de inquilino: {deuda.Deudor.Nombre} ({deuda.Concepto})"
        };

        _context.Transacciones.Add(ingreso);

        // 3. ¡EL PASO FALTANTE!: Actualizar el saldo de la Billetera/Cuenta
        var cuentaDestino = await _context.Cuentas.FindAsync(cuentaDestinoId);
        if (cuentaDestino != null)
        {
            cuentaDestino.SaldoActual += montoPago; // Le sumamos el dinero físicamente a la cuenta
        }

        await _context.SaveChangesAsync();

        TempData["Exito"] = $"¡Pago de {montoPago:C} registrado! El dinero ya está en tu cuenta.";
        return RedirectToAction(nameof(Detalle), new { id = deuda.DeudorId });
    }
    // 7. ELIMINAR UNA DEUDA (POST)
    [HttpPost]
    public async Task<IActionResult> EliminarDeuda(int id, int deudorId)
    {
        var deuda = await _context.CuentasPorCobrar.FindAsync(id);
        if (deuda != null)
        {
            // Regla de negocio: No eliminar si ya hay dinero ingresado
            if (deuda.MontoPagado > 0)
            {
                TempData["Error"] = "No puedes eliminar una deuda que ya tiene pagos. Mejor regístrala como pagada o ajusta el saldo.";
                return RedirectToAction(nameof(Detalle), new { id = deudorId });
            }

            _context.CuentasPorCobrar.Remove(deuda);
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Deuda eliminada correctamente.";
        }
        return RedirectToAction(nameof(Detalle), new { id = deudorId });
    }
}