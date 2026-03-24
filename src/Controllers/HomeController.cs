using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using PresupuestoFamiliarApp.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace PresupuestoFamiliarApp.Controllers;

[Authorize]
public class HomeController : BaseController
{
    public HomeController(PresupuestoContext context) : base(context) { }

    private async Task<Usuario> ObtenerUsuarioActual()
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u =>
            u.Email == User.Identity.Name || u.NombreUsuario == User.Identity.Name);
    }

    public async Task<IActionResult> Index(int? mes, int? anio)
    {
        var usuarioActual = await ObtenerUsuarioActual();
        if (usuarioActual == null) return RedirectToAction("Login", "Auth");

        // NUEVO: Le pasamos a la vista si debe o no mostrar el tour
        ViewBag.MostrarTour = !usuarioActual.TourCompletado;

        // 2. Traer SOLO los espacios a los que este usuario tiene acceso
        var misEspacios = await _context.Espacios
            .Where(e => e.Usuarios.Any(u => u.Id == usuarioActual.Id))
            .ToListAsync();

        int espacioActualId = ObtenerEspacioActivoId();

        // 4. EL CANDADO DE SEGURIDAD: 
        // Si el usuario no tiene la cookie, o si la cookie tiene un ID de un espacio que NO le pertenece...
        if (!misEspacios.Any(e => e.Id == espacioActualId))
        {
            // ...le asignamos autom�ticamente el primer espacio de su lista
            var primerEspacio = misEspacios.FirstOrDefault();
            if (primerEspacio != null)
            {
                espacioActualId = primerEspacio.Id;
                // Actualizamos la cookie en su navegador para corregir el error
                Response.Cookies.Append("EspacioActivoId", espacioActualId.ToString());
            }
        }

        // 5. Enviamos la lista de sus espacios a la vista para construir el men� desplegable
        ViewBag.MisEspacios = misEspacios;
        ViewBag.EspacioActivoNombre = misEspacios.FirstOrDefault(e => e.Id == espacioActualId)?.Nombre;

        var hoy = DateTime.Now;
        int mesConsulta = mes ?? hoy.Month;
        int anioConsulta = anio ?? hoy.Year;
        var inicioMes = new DateTime(anioConsulta, mesConsulta, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);

        // 1. FILTRAR CUENTAS POR ESPACIO
        var todasLasCuentas = await _context.Cuentas
            .Where(c => c.EspacioId == espacioActualId) // <-- FILTRO CLAVE
            .ToListAsync();

        // 2. FILTRAR TRANSACCIONES (Incluyendo la cuenta para saber de qu� espacio es)
        var transaccionesMes = await _context.Transacciones
            .Include(t => t.Cuenta)
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha >= inicioMes && t.Fecha <= finMes) // <-- FILTRO CLAVE
            .ToListAsync();

        // 3. FILTRAR CATEGOR�AS POR ESPACIO
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

        // --- NUEVO: DATA PARA EL GR�FICO DE BARRAS (BALANCE ANUAL) ---
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
        // --- FIN DATA GR�FICOS ---

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
            AnioSeleccionado = anioConsulta,    // <-- Guardamos el a�o
                                                // A�ADE ESTA L�NEA AL FINAL DEL VIEWMODEL:

            SimboloMonedaEspacio = ViewBag.SimboloMoneda,
            BalancesHistoricos = balancesHistoricos // A�ADE ESTA L�NEA

        };

        // --- NUEVO: C�LCULO ANUAL PARA EL GR�FICO DE BARRAS ---
        // Agrupamos los ingresos por mes
        var ingresosPorMes = await _context.Transacciones
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha.Year == anioConsulta && t.Tipo == TipoTransaccion.Ingreso && !t.EsTransferencia && !t.Cuenta.EsCredito)
            .GroupBy(t => t.Fecha.Month)
            .Select(g => new { Mes = g.Key, Total = g.Sum(t => t.Monto) })
            .ToDictionaryAsync(g => g.Mes, g => g.Total);

        // Agrupamos los gastos por mes
        var gastosPorMes = await _context.Transacciones
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha.Year == anioConsulta && t.Tipo == TipoTransaccion.Egreso && !t.Cuenta.EsCredito)
            .GroupBy(t => t.Fecha.Month)
            .Select(g => new { Mes = g.Key, Total = g.Sum(t => t.Monto) })
            .ToDictionaryAsync(g => g.Mes, g => g.Total);

        // Creamos arreglos de 12 posiciones (Enero a Diciembre) y los llenamos
        decimal[] ingresosAnual = new decimal[12];
        decimal[] gastosAnual = new decimal[12];

        for (int i = 1; i <= 12; i++)
        {
            ingresosAnual[i - 1] = ingresosPorMes.ContainsKey(i) ? ingresosPorMes[i] : 0;
            gastosAnual[i - 1] = gastosPorMes.ContainsKey(i) ? gastosPorMes[i] : 0;
        }

        ViewBag.IngresosAnual = JsonSerializer.Serialize(ingresosAnual);
        ViewBag.GastosAnual = JsonSerializer.Serialize(gastosAnual);

        // --- SE MANTIENE: C�LCULO MENSUAL PARA EL GR�FICO DE DONA (Categor�as) ---
        var gastosPorCategoria = await _context.Transacciones
            .Where(t => t.Cuenta.EspacioId == espacioActualId && t.Fecha.Month == mesConsulta && t.Fecha.Year == anioConsulta && t.Tipo == TipoTransaccion.Egreso && !t.EsTransferencia)
            .GroupBy(t => t.Categoria.Nombre)
            .Select(g => new {
                Categoria = string.IsNullOrEmpty(g.Key) ? "Sin Categor�a" : g.Key,
                Total = g.Sum(t => t.Monto)
            })
            .ToListAsync();

        ViewBag.LabelsCategorias = JsonSerializer.Serialize(gastosPorCategoria.Select(g => g.Categoria));
        ViewBag.DatosCategorias = JsonSerializer.Serialize(gastosPorCategoria.Select(g => g.Total));

        // --- NUEVO: DEUDA TOTAL POR DEUDOR (INQUILINO) ---
        var deudasPorDeudor = await _context.Deudores
            .Where(d => d.EspacioId == espacioActualId)
            .Select(d => new {
                Nombre = d.Nombre,
                // Sumamos el total menos lo pagado para obtener el saldo pendiente
                DeudaTotal = d.CuentasPorCobrar.Sum(c => c.MontoTotal - c.MontoPagado)
            })
            .Where(d => d.DeudaTotal > 0) // Filtramos a los que s� deben dinero
            .OrderByDescending(d => d.DeudaTotal) // Ordenamos del que debe m�s al que debe menos
            .ToListAsync();

        // Enviamos los datos a la vista en formato JSON
        ViewBag.LabelsDeudores = JsonSerializer.Serialize(deudasPorDeudor.Select(d => d.Nombre));
        ViewBag.DatosDeudores = JsonSerializer.Serialize(deudasPorDeudor.Select(d => d.DeudaTotal));

        // --- 1. EVENTOS DE INGRESOS (Cuentas por Cobrar) ---
        var eventosCobros = await _context.CuentasPorCobrar
            .Include(c => c.Deudor)
            .Where(c => c.Deudor.EspacioId == espacioActualId && c.MontoPagado < c.MontoTotal)
            .Select(c => new
            {
                title = $"Cobrar a {c.Deudor.Nombre}: {(c.MontoTotal - c.MontoPagado).ToString("C")}",
                start = c.FechaVencimiento.ToString("yyyy-MM-dd"),
                color = c.FechaVencimiento < DateTime.Now ? "#dc3545" : "#198754", // Rojo (Vencido) o Verde (Pendiente)
                allDay = true,
                url = $"/CuentasPorCobrar/Detalle/{c.DeudorId}"
            })
            .ToListAsync();

        // --- 2. EVENTOS DE MOVIMIENTOS FIJOS (Ingresos y Egresos Recurrentes) ---
        var suscripciones = await _context.MovimientosFijos
            .Where(s => s.EspacioId == espacioActualId)
            .ToListAsync();

        var eventosPagos = new List<object>();

        foreach (var sub in suscripciones)
        {
            // Validación de seguridad: Si el mes tiene 28 días y la suscripción se cobra el 31, lo ajustamos al día 28.
            int diaValido = Math.Min(sub.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
            DateTime fechaDelPago = new DateTime(anioConsulta, mesConsulta, diaValido);

            // Diferenciar entre Ingreso y Egreso
            string accion = sub.Tipo == TipoTransaccion.Ingreso ? "Cobrar" : "Pagar";
            string color = sub.Tipo == TipoTransaccion.Ingreso ? "#198754" : "#0dcaf0"; // Verde para ingresos, Azul para egresos

            eventosPagos.Add(new
            {
                title = $"{accion} {sub.Descripcion}: {sub.Monto.ToString("C")}",
                start = fechaDelPago.ToString("yyyy-MM-dd"),
                color = color,
                allDay = true,
                // url = "/MovimientosFijos/Index" // Opcional: Enlace para ir a editar
            });
        }

        // --- 3. FUSIONAR AMBAS LISTAS ---
        // Usamos .Cast<object>() para que C# permita mezclar las dos listas en una sola
        var todosLosEventos = eventosCobros.Cast<object>().Concat(eventosPagos).ToList();

        // Enviamos el s�per-arreglo combinado al JavaScript
        ViewBag.EventosCalendario = JsonSerializer.Serialize(todosLosEventos);

        // --- NUEVO: CALCULAR PAGOS PENDIENTES DEL MES RESTANTE ---
        var hoyReal = DateTime.Now;
        var finDelMes = new DateTime(anioConsulta, mesConsulta, DateTime.DaysInMonth(anioConsulta, mesConsulta));

        // 1. Cuentas por cobrar pendientes desde hoy hasta fin de mes
        var cobrosPendientes = await _context.CuentasPorCobrar
            .Include(c => c.Deudor)
            .Where(c => c.Deudor.EspacioId == espacioActualId
                && c.FechaVencimiento >= hoyReal
                && c.FechaVencimiento <= finDelMes
                && c.MontoPagado < c.MontoTotal)
            .ToListAsync();

        decimal totalCobrosPendientes = cobrosPendientes.Sum(c => c.SaldoPendiente);

        // NUEVO: Sumar también los Movimientos Fijos de tipo INGRESO pendientes del mes
        var ingresosFijosPendientes = suscripciones
            .Where(s => s.Activo && s.Tipo == TipoTransaccion.Ingreso)
            .Select(s => {
                int diaValido = Math.Min(s.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
                DateTime fechaPago = new DateTime(anioConsulta, mesConsulta, diaValido);
                return new { Fecha = fechaPago, Monto = s.Monto };
            })
            .Where(p => p.Fecha >= hoyReal && p.Fecha <= finDelMes)
            .Sum(p => p.Monto);

        // Sumar ambos conceptos: cuentas por cobrar + ingresos fijos
        totalCobrosPendientes += ingresosFijosPendientes;

        // 2. Suscripciones/Pagos fijos pendientes desde hoy hasta fin de mes (solo EGRESOS)
        var pagosFijosPendientes = suscripciones
            .Where(s => s.Activo && s.Tipo == TipoTransaccion.Egreso) // AGREGAR filtro de tipo Egreso
            .Select(s => {
                int diaValido = Math.Min(s.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
                DateTime fechaPago = new DateTime(anioConsulta, mesConsulta, diaValido);
                return new { Fecha = fechaPago, Monto = s.Monto };
            })
            .Where(p => p.Fecha >= hoyReal && p.Fecha <= finDelMes)
            .Sum(p => p.Monto);

        // Pasar los datos a la vista
        ViewBag.TotalCobrosPendientes = totalCobrosPendientes;
        ViewBag.TotalPagosFijosPendientes = pagosFijosPendientes;
        ViewBag.CantidadCobrosPendientes = cobrosPendientes.Count;

        // CORREGIR: Contar solo movimientos fijos de tipo INGRESO para la cantidad de cobros
        ViewBag.CantidadCobrosPendientes += suscripciones.Count(s => {
            int diaValido = Math.Min(s.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
            DateTime fechaPago = new DateTime(anioConsulta, mesConsulta, diaValido);
            return s.Activo && s.Tipo == TipoTransaccion.Ingreso && fechaPago >= hoyReal && fechaPago <= finDelMes;
        });

        ViewBag.CantidadPagosFijosPendientes = suscripciones.Count(s => {
            int diaValido = Math.Min(s.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
            DateTime fechaPago = new DateTime(anioConsulta, mesConsulta, diaValido);
            return s.Activo && s.Tipo == TipoTransaccion.Egreso && fechaPago >= hoyReal && fechaPago <= finDelMes; // AGREGAR filtro de tipo Egreso
        });

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
        // Guardamos el nuevo espacio en la cookie por 30 d�as
        Response.Cookies.Append("EspacioActivoId", id.ToString(), new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30) });

        // Recargamos la p�gina donde estaba el usuario
        return LocalRedirect(returnUrl ?? "/");
    }
    [HttpPost]
    public async Task<IActionResult> MarcarTourCompletado()
    {
        var usuarioActual = await ObtenerUsuarioActual(); // Usamos el m�todo ayudante que ya ten�as
        if (usuarioActual != null && !usuarioActual.TourCompletado)
        {
            usuarioActual.TourCompletado = true;
            _context.Update(usuarioActual);
            await _context.SaveChangesAsync();
        }
        return Ok(); // Respondemos a JavaScript que todo sali� bien
    }
}