using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace PresupuestoFamiliarApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly PresupuestoContext _context;

   
    public HomeController(PresupuestoContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? mes, int? anio)
    {
        // LEER LA COOKIE EN EL CONTROLADOR
        int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

        // AÑADE ESTAS LÍNEAS: Buscar el espacio para saber su moneda
        var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
        string simboloEspacio = espacioActivo?.MonedaPrincipal switch
        {
            Moneda.Dolares => "$",
            Moneda.Euros => "€",
            _ => "S/"
        };

        // --- INICIO DEL MOTOR AUTOMÁTICO DE FIJOS ---
        var hoyMotor = DateTime.Now.Date;

        // Traemos todos los movimientos fijos activos de este espacio
        var fijos = await _context.MovimientosFijos
            .Include(m => m.Cuenta)
            .Where(m => m.Activo && m.EspacioId == espacioActualId)
            .ToListAsync();

        bool huboCambiosAutomáticos = false;

        foreach (var fijo in fijos)
        {
            // Si nunca se ha generado, empezamos a evaluar desde el mes actual
            DateTime siguienteCobro;

            if (fijo.UltimaGeneracion == null)
            {
                // Intentamos crear la fecha para este mes
                int diaReal = Math.Min(fijo.DiaDelMes, DateTime.DaysInMonth(hoyMotor.Year, hoyMotor.Month));
                siguienteCobro = new DateTime(hoyMotor.Year, hoyMotor.Month, diaReal);

                // Si el día ya pasó este mes (ej. lo creas el día 20 pero se cobra los 15), lo generamos de inmediato
            }
            else
            {
                // Si ya se generó antes, calculamos el día del PRÓXIMO mes
                var mesSiguiente = fijo.UltimaGeneracion.Value.AddMonths(1);
                int diaReal = Math.Min(fijo.DiaDelMes, DateTime.DaysInMonth(mesSiguiente.Year, mesSiguiente.Month));
                siguienteCobro = new DateTime(mesSiguiente.Year, mesSiguiente.Month, diaReal);
            }

            // Bucle: Mientras la fecha de cobro sea HOY o en el PASADO, generamos la transacción
            // (Es un bucle por si dejas de abrir la app por 3 meses, al entrar te cobrará los 3 meses de golpe)
            while (siguienteCobro <= hoyMotor)
            {
                // --- AÑADE ESTA NUEVA VERIFICACIÓN DE FECHA DE FIN ---
                if (fijo.FechaFin.HasValue && siguienteCobro > fijo.FechaFin.Value.Date)
                {
                    fijo.Activo = false; // Lo desactivamos automáticamente para el futuro
                    huboCambiosAutomáticos = true;
                    break; // Rompemos el bucle, ya no generamos este cobro
                }
                // --- FIN DE LA NUEVA VERIFICACIÓN ---
                // 1. Crear el registro en el Historial
                var nuevaTransaccion = new Transaccion
                {
                    Descripcion = $"{fijo.Descripcion} (Automático)",
                    Monto = fijo.Monto,
                    MontoOriginal = fijo.Monto, // Asumimos misma moneda por simplicidad en fijos
                    TasaCambioUsada = 1,
                    MonedaTransaccion = fijo.Cuenta.MonedaCuenta, // Usa la moneda de la cuenta
                    Fecha = siguienteCobro,
                    Tipo = fijo.Tipo,
                    CuentaId = fijo.CuentaId,
                    CategoriaGastoId = fijo.CategoriaGastoId,
                    EsTransferencia = false
                };
                _context.Transacciones.Add(nuevaTransaccion);

                // 2. Actualizar el saldo del banco real
                if (fijo.Tipo == TipoTransaccion.Ingreso)
                    fijo.Cuenta.SaldoActual += fijo.Monto;
                else
                    fijo.Cuenta.SaldoActual -= fijo.Monto;

                // 3. Marcar que ya se cobró esta fecha y preparar la próxima
                fijo.UltimaGeneracion = siguienteCobro;

                var proxMes = siguienteCobro.AddMonths(1);
                int diaRealProx = Math.Min(fijo.DiaDelMes, DateTime.DaysInMonth(proxMes.Year, proxMes.Month));
                siguienteCobro = new DateTime(proxMes.Year, proxMes.Month, diaRealProx);

                huboCambiosAutomáticos = true;
            }
        }

        // Si el motor detectó y creó cosas nuevas, guardamos en la base de datos silenciosamente
        if (huboCambiosAutomáticos)
        {
            await _context.SaveChangesAsync();
        }
        // --- FIN DEL MOTOR AUTOMÁTICO ---

        var hoy = DateTime.Now;
        int mesConsulta = mes ?? hoy.Month;
        int anioConsulta = anio ?? hoy.Year;
        var inicioMes = new DateTime(anioConsulta, mesConsulta, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);

        // 1. FILTRAR CUENTAS POR ESPACIO
        var todasLasCuentas = await _context.Cuentas
            .Where(c => c.EspacioId == espacioActualId) // <-- FILTRO CLAVE
            .ToListAsync();

        // 2. FILTRAR TRANSACCIONES (Incluyendo la cuenta para saber de qué espacio es)
        var transaccionesMes = await _context.Transacciones
            .Include(t => t.Cuenta)
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha >= inicioMes && t.Fecha <= finMes) // <-- FILTRO CLAVE
            .ToListAsync();

        // 3. FILTRAR CATEGORÍAS POR ESPACIO
        var categoriasDb = await _context.CategoriasGastos
            .Where(c => c.EspacioId == espacioActualId) // <-- FILTRO CLAVE
            .ToListAsync();

        // 3. Calcular totales (ignorar transferencias si lo implementaste)
        var totalIngresos = transaccionesMes
            .Where(t => t.Tipo == TipoTransaccion.Ingreso && !t.EsTransferencia)
            .Sum(t => t.Monto);

        var totalEgresos = transaccionesMes
            .Where(t => t.Tipo == TipoTransaccion.Egreso && !t.EsTransferencia)
            .Sum(t => t.Monto);

       
        var gastosAgrupados = transaccionesMes
            .Where(t => t.Tipo == TipoTransaccion.Egreso && t.CategoriaGastoId != null && !t.EsTransferencia)
            .GroupBy(t => t.CategoriaGastoId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Monto));

        var resumenCategorias = categoriasDb.Select(c => new CategoriaResumen
        {
            Nombre = c.Nombre,
            PresupuestoMensual = c.PresupuestoMensual,
            GastoReal = gastosAgrupados.ContainsKey(c.Id) ? gastosAgrupados[c.Id] : 0
        }).ToList();

        // --- NUEVO: DATA PARA EL GRÁFICO DE BARRAS (BALANCE ANUAL) ---
        var transaccionesAnio = await _context.Transacciones
            .Include(t => t.Cuenta)
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha.Year == anioConsulta)
            .ToListAsync();

        var balancesHistoricos = new List<BalanceMensual>();
        string[] nombresMeses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        for (int i = 1; i <= 12; i++)
        {
            var txMes = transaccionesAnio.Where(t => t.Fecha.Month == i).ToList();
            decimal ingMes = txMes.Where(t => t.Tipo == TipoTransaccion.Ingreso && !t.EsTransferencia && !t.Cuenta.EsCredito).Sum(t => t.Monto);
            decimal egrMes = txMes.Where(t => t.Tipo == TipoTransaccion.Egreso && !t.Cuenta.EsCredito).Sum(t => t.Monto);

            balancesHistoricos.Add(new BalanceMensual
            {
                MesNombre = nombresMeses[i - 1],
                Balance = ingMes - egrMes
            });
        }
        // --- FIN DATA GRÁFICOS ---

        // 5. Llenar el ViewModel con los datos y las fechas seleccionadas
        var viewModel = new DashboardViewModel
        {
            // ASIGNAR LAS CUENTAS SEPARADAS:
            CuentasDinero = todasLasCuentas.Where(c => !c.EsCredito).ToList(),
            TarjetasCredito = todasLasCuentas.Where(c => c.EsCredito).ToList(),
            TotalIngresosMes = totalIngresos,
            TotalEgresosMes = totalEgresos,
            ResumenCategorias = resumenCategorias,
            MesSeleccionado = mesConsulta,      // <-- Guardamos el mes
            AnioSeleccionado = anioConsulta ,    // <-- Guardamos el año
                                                 // AÑADE ESTA LÍNEA AL FINAL DEL VIEWMODEL:
           
            SimboloMonedaEspacio = simboloEspacio,
            BalancesHistoricos = balancesHistoricos // AÑADE ESTA LÍNEA

    };



        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult CambiarEspacio(int id, string returnUrl)
    {
        // Guardamos el nuevo espacio en la cookie por 30 días
        Response.Cookies.Append("EspacioActivoId", id.ToString(), new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30) });

        // Recargamos la página donde estaba el usuario
        return LocalRedirect(returnUrl ?? "/");
    }
}
