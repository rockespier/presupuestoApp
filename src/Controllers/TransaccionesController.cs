using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using PresupuestoFamiliarApp.Servicios;
using PresupuestoFamiliarApp.Models.DTOs;

namespace PresupuestoFamiliarApp.Controllers
{
    [Authorize]
    public class TransaccionesController : BaseController
    {
        private readonly OcrService _ocrService;

        public TransaccionesController(PresupuestoContext context, OcrService ocrService) : base(context)
        {
            _ocrService = ocrService;
        }

        
        // GET: Muestra el formulario vacío con listas FILTRADAS
        public async Task<IActionResult> Create()
        {
            // Leer la cookie
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            // Traer solo cuentas y categorías del espacio actual
            var cuentasDelEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
            var categoriasDelEspacio = _context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId).ToList();

            // Llenar los ViewBags con las listas filtradas
            ViewBag.CuentaId = new SelectList(cuentasDelEspacio, "Id", "Nombre");
            ViewBag.CategoriaGastoId = new SelectList(categoriasDelEspacio, "Id", "Nombre");

            // Filtrar cuáles de estas cuentas son tarjetas
            var tarjetasIds = cuentasDelEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList();
            ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(tarjetasIds);

            // NUEVO: Obtener la moneda preferida del usuario
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);

            // Si el usuario tiene moneda preferida, la usamos; si no, usamos la del espacio
            var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

            // Crear instancia de transacción con moneda por defecto
            var nuevaTransaccion = new Transaccion
            {
                MonedaTransaccion = monedaPorDefecto,
                Fecha = DateTime.Now
            };

            ViewBag.SimboloMoneda = monedaPorDefecto == Moneda.Dolares ? "$" :
                                   (monedaPorDefecto == Moneda.Euros ? "€" : "S/");

            return View(nuevaTransaccion);
        }

        // POST: Recibe y guarda los datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Añadimos el parámetro "numeroCuotas" que vendrá del formulario
        public async Task<IActionResult> Create(Transaccion transaccion, int numeroCuotas = 1)
        {
            ModelState.Remove("Cuenta");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                var cuenta = await _context.Cuentas.FindAsync(transaccion.CuentaId);

                // --- NUEVA LÓGICA DE DOBLE CONVERSIÓN ---
                int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
                var espacioActual = await _context.Espacios.FindAsync(espacioActualId);

                transaccion.MontoOriginal = transaccion.Monto;

                // 1. CÁLCULO PARA EL ESPACIO / PRESUPUESTO (Ej: Todo a Soles)
                if (transaccion.MonedaTransaccion != cuenta.MonedaCuenta)
                {
                    var tasaEspacio = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == espacioActual.MonedaPrincipal);
                    if (tasaEspacio == null)
                    {
                        ModelState.AddModelError("", $"ERROR: Falta tasa de cambio de {transaccion.MonedaTransaccion} a {espacioActual.MonedaPrincipal} (Para tu presupuesto).");
                        // NUEVO: Recargar ViewBags necesarios (incluido TarjetasIds)
                        var cuentasDelEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
                        ViewBag.CuentaId = new SelectList(cuentasDelEspacio, "Id", "Nombre", transaccion.CuentaId);
                        ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);
                        ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(cuentasDelEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList());
                        ViewBag.SimboloMoneda = transaccion.MonedaTransaccion == Moneda.Dolares ? "$" : (transaccion.MonedaTransaccion == Moneda.Euros ? "€" : "S/");
                        return View(transaccion);
                    }
                    transaccion.TasaCambioUsada = tasaEspacio.Tasa;
                    transaccion.Monto = Math.Round(transaccion.MontoOriginal * tasaEspacio.Tasa, 2);
                }
                else
                {
                    transaccion.TasaCambioUsada = 1m;
                }

                // 2. CÁLCULO PARA LA CUENTA BANCARIA (Ej: Si pagaste en Soles pero la cuenta es en Dólares)
                decimal montoParaLaCuenta = transaccion.MontoOriginal;

                if (transaccion.MonedaTransaccion != cuenta.MonedaCuenta)
                {
                    var tasaCuenta = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == cuenta.MonedaCuenta);
                    if (tasaCuenta == null)
                    {
                        ModelState.AddModelError("", $"ERROR: Falta tasa de cambio de {transaccion.MonedaTransaccion} a {cuenta.MonedaCuenta} (Para actualizar tu banco).");
                        // NUEVO: Recargar ViewBags necesarios (incluido TarjetasIds)
                        var cuentasDelEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
                        ViewBag.CuentaId = new SelectList(cuentasDelEspacio, "Id", "Nombre", transaccion.CuentaId);
                        ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);
                        ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(cuentasDelEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList());
                        ViewBag.SimboloMoneda = transaccion.MonedaTransaccion == Moneda.Dolares ? "$" : (transaccion.MonedaTransaccion == Moneda.Euros ? "€" : "S/");
                        return View(transaccion);
                    }
                    montoParaLaCuenta = Math.Round(transaccion.MontoOriginal * tasaCuenta.Tasa, 2);
                }
                // --- FIN LÓGICA DE CONVERSIÓN ---

                if (cuenta != null)
                {
                    // NUEVA VALIDACIÓN: Verificar saldo insuficiente solo para cuentas normales (no tarjetas de crédito)
                    if (transaccion.Tipo == TipoTransaccion.Egreso && !cuenta.EsCredito)
                    {
                        if (cuenta.SaldoActual < montoParaLaCuenta)
                        {
                            ModelState.AddModelError("", $"ERROR: Saldo insuficiente. La cuenta '{cuenta.Nombre}' tiene {cuenta.MonedaCuenta} {cuenta.SaldoActual:N2}, pero intentas gastar {cuenta.MonedaCuenta} {montoParaLaCuenta:N2}.");
                            // NUEVO: Recargar ViewBags necesarios (incluido TarjetasIds)
                            var cuentasDelEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
                            ViewBag.CuentaId = new SelectList(cuentasDelEspacio, "Id", "Nombre", transaccion.CuentaId);
                            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);
                            ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(cuentasDelEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList());
                            ViewBag.SimboloMoneda = transaccion.MonedaTransaccion == Moneda.Dolares ? "$" : (transaccion.MonedaTransaccion == Moneda.Euros ? "€" : "S/");
                            return View(transaccion);
                        }
                    }

                    // Si no es tarjeta de crédito o no es un egreso, forzamos a 1 cuota
                    if (!cuenta.EsCredito || transaccion.Tipo != TipoTransaccion.Egreso)
                    {
                        numeroCuotas = 1;
                    }

                    if (numeroCuotas == 1)
                    {
                        // LÓGICA NORMAL (1 pago o Ingreso)
                        if (transaccion.Tipo == TipoTransaccion.Ingreso)
                        {
                            cuenta.SaldoActual += montoParaLaCuenta;
                            transaccion.CategoriaGastoId = null;
                        }
                        else
                        {
                            cuenta.SaldoActual -= montoParaLaCuenta;
                        }
                        _context.Add(transaccion);
                    }
                    else
                    {
                        // LÓGICA DE CUOTAS (Ej: 3 Cuotas)

                        // 1. Restamos el monto total de la tarjeta de crédito (la deuda es total)
                        cuenta.SaldoActual -= montoParaLaCuenta;

                        // 2. Calculamos cuánto es cada cuota (redondeado a 2 decimales)
                        decimal montoCuota = Math.Round(transaccion.Monto / numeroCuotas, 2);

                        // 3. Calculamos si sobra algún céntimo para sumarlo a la última cuota
                        decimal montoUltimaCuota = transaccion.Monto - (montoCuota * (numeroCuotas - 1));

                        // 4. Creamos las 3 transacciones viajando en el tiempo
                        for (int i = 0; i < numeroCuotas; i++)
                        {
                            decimal montoActual = (i == numeroCuotas - 1) ? montoUltimaCuota : montoCuota;

                            var nuevaTransaccion = new Transaccion
                            {
                                // Le añadimos el texto "(Cuota 1/3)" a la descripción
                                Descripcion = $"{transaccion.Descripcion} (Cuota {i + 1}/{numeroCuotas})",
                                Monto = montoActual,
                                // Sumamos los meses a la fecha seleccionada
                                Fecha = transaccion.Fecha.AddMonths(i),
                                Tipo = transaccion.Tipo,
                                CuentaId = transaccion.CuentaId,
                                CategoriaGastoId = transaccion.CategoriaGastoId,
                                EsTransferencia = false,
                                MontoOriginal = Math.Round(transaccion.MontoOriginal / numeroCuotas, 2),
                                MonedaTransaccion = transaccion.MonedaTransaccion,
                                TasaCambioUsada = transaccion.TasaCambioUsada
                            };

                            _context.Add(nuevaTransaccion);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "¡Movimiento registrado con éxito!";
                    return RedirectToAction("Index", "Home");
                }
            }

            // Si hay error, recargamos la vista con todos los ViewBags necesarios
            int espacioActId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idC) ? idC : 1;
            var cuentasEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActId).ToList();
            ViewBag.CuentaId = new SelectList(cuentasEspacio, "Id", "Nombre", transaccion.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActId), "Id", "Nombre", transaccion.CategoriaGastoId);
            ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(cuentasEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList());
            ViewBag.SimboloMoneda = transaccion.MonedaTransaccion == Moneda.Dolares ? "$" : (transaccion.MonedaTransaccion == Moneda.Euros ? "€" : "S/");

            return View(transaccion);
        }

        // GET: Muestra la lista de movimientos FILTRADOS y ORDENADOS
        public async Task<IActionResult> Index(string sortOrder, int? mes, int? anio, TipoTransaccion? tipo, int? categoriaId, int? cuentaId, int pagina = 1, int tamanioPagina = 10)
        {
            // --- NUEVO: Filtro por defecto al mes y año actual ---
            if (!mes.HasValue && !anio.HasValue)
            {
                mes = DateTime.Now.Month;
                anio = DateTime.Now.Year;
            }
            // -----------------------------------------------------

            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            ViewBag.SimboloMoneda = espacioActivo?.MonedaPrincipal == Moneda.Dolares ? "$" : (espacioActivo?.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

            // --- CONFIGURACIÓN DE ORDENAMIENTO (Toggles) ---
            // Si sortOrder está vacío, el default es ordenar por fecha descendente. 
            // Si haces clic en Fecha, cambiará a "fecha_asc".
            ViewBag.FechaSortParm = String.IsNullOrEmpty(sortOrder) ? "fecha_asc" : "";
            ViewBag.MontoSortParm = sortOrder == "monto_asc" ? "monto_desc" : "monto_asc";
            ViewBag.DescSortParm = sortOrder == "desc_asc" ? "desc_desc" : "desc_asc";
            ViewBag.CuentaSortParm = sortOrder == "cuenta_asc" ? "cuenta_desc" : "cuenta_asc";
            ViewBag.CatSortParm = sortOrder == "cat_asc" ? "cat_desc" : "cat_asc";
            ViewBag.TipoSortParm = sortOrder == "tipo_asc" ? "tipo_desc" : "tipo_asc";
            // Guardamos el orden current para que los filtros no lo borren
            ViewBag.CurrentSort = sortOrder;

            // Consulta base
            var query = _context.Transacciones
                .Include(t => t.Cuenta)
                .Include(t => t.Categoria)
                .Where(t => t.Cuenta.EspacioId == espacioActualId)
                .AsQueryable();

            // Filtros dinámicos
            if (mes.HasValue) query = query.Where(t => t.Fecha.Month == mes.Value);
            if (anio.HasValue) query = query.Where(t => t.Fecha.Year == anio.Value);
            if (tipo.HasValue) query = query.Where(t => t.Tipo == tipo.Value);
            if (categoriaId.HasValue) query = query.Where(t => t.CategoriaGastoId == categoriaId.Value);
            if (cuentaId.HasValue) query = query.Where(t => t.CuentaId == cuentaId.Value); // NUEVO: Filtro por cuenta

            // --- APLICAR EL ORDENAMIENTO ---
            query = sortOrder switch
            {
                "fecha_asc" => query.OrderBy(t => t.Fecha),
                "monto_asc" => query.OrderBy(t => t.Monto),
                "monto_desc" => query.OrderByDescending(t => t.Monto),
                "desc_asc" => query.OrderBy(t => t.Descripcion),
                "desc_desc" => query.OrderByDescending(t => t.Descripcion),
                "cuenta_asc" => query.OrderBy(t => t.Cuenta.Nombre),
                "cuenta_desc" => query.OrderByDescending(t => t.Cuenta.Nombre),
                "cat_asc" => query.OrderBy(t => t.Categoria.Nombre),
                "cat_desc" => query.OrderByDescending(t => t.Categoria.Nombre),
                "tipo_asc" => query.OrderBy(t => t.Tipo),
                "tipo_desc" => query.OrderByDescending(t => t.Tipo),
                _ => query.OrderByDescending(t => t.Fecha), // Default
            };



            int registrosPorPagina = tamanioPagina;

            // 1. Contar total de registros filtrados para saber cuántas páginas hay
            var totalRegistros = await query.CountAsync();

            // --- LA CORRECCIÓN: Calcular totales GLOBALES usando 'query' ---
            // Usamos 'await' y 'SumAsync' para que la suma se haga directamente en la Base de Datos
            decimal totalIngresos = await query
                .Where(t => t.Tipo == TipoTransaccion.Ingreso && !t.EsTransferencia && !t.Cuenta.EsCredito)
                .SumAsync(t => t.Monto);

            decimal totalEgresos = await query
                .Where(t => t.Tipo == TipoTransaccion.Egreso && !t.Cuenta.EsCredito)
                .SumAsync(t => t.Monto);

            ViewBag.TotalIngresos = totalIngresos;
            ViewBag.TotalEgresos = totalEgresos;
            ViewBag.BalanceTotal = totalIngresos - totalEgresos;
            // ---------------------------------------------------------------

            // 2. Aplicar el salto (Skip) y la toma (Take)
            var transacciones = await query.AsNoTracking()
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // 3. Enviar datos de paginación a la vista
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / registrosPorPagina);
            ViewBag.TamanioPagina = tamanioPagina; // NUEVO: Pasar el tamaño de página actual

            // Preparar Listas y preservar filtros
            var categoriasDelEspacio = await _context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId).ToListAsync();
            ViewBag.Categorias = new SelectList(categoriasDelEspacio, "Id", "Nombre", categoriaId);

            // NUEVO: Preparar lista de cuentas para el filtro
            var cuentasDelEspacio = await _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToListAsync();
            ViewBag.Cuentas = new SelectList(cuentasDelEspacio, "Id", "Nombre", cuentaId);

            ViewBag.MesSeleccionado = mes;
            ViewBag.AnioSeleccionado = anio;
            ViewBag.TipoSeleccionado = tipo;
            ViewBag.CategoriaSeleccionada = categoriaId;
            ViewBag.CuentaSeleccionada = cuentaId; // NUEVO

            return View(transacciones);
        }

        // GET: Muestra la pantalla de confirmación para eliminar
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transaccion = await _context.Transacciones
                .Include(t => t.Cuenta)
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transaccion == null) return NotFound();

            return View(transaccion);
        }

        // POST: Procesa la eliminación y revierte el saldo
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaccion = await _context.Transacciones.FindAsync(id);
            if (transaccion != null)
            {
                // Buscar la cuenta afectada
                var cuenta = await _context.Cuentas.FindAsync(transaccion.CuentaId);
                if (cuenta != null)
                {
                    // Operación INVERSA para restaurar el saldo
                    if (transaccion.Tipo == TipoTransaccion.Ingreso)
                    {
                        cuenta.SaldoActual -= transaccion.Monto; // Revertir ingreso
                    }
                    else if (transaccion.Tipo == TipoTransaccion.Egreso)
                    {
                        cuenta.SaldoActual += transaccion.Monto; // Revertir gasto
                    }
                }

                // Eliminar la transacción
                _context.Transacciones.Remove(transaccion);
                TempData["Exito"] = "El registro ha sido eliminado.";
                await _context.SaveChangesAsync();
            }

            // Volver a la lista de historial
            return RedirectToAction(nameof(Index));
        }

        // GET: Mostrar el formulario de Edición
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transaccion = await _context.Transacciones.FindAsync(id);
            if (transaccion == null) return NotFound();

            // TRUCO VISUAL: Ponemos el "MontoOriginal" en la casilla de "Monto" para que 
            // edites el número exacto que digitaste (ej. 15 Dólares), y no el que el sistema 
            // convirtió a Soles para el presupuesto.
            transaccion.Monto = transaccion.MontoOriginal;

            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);

            return View(transaccion);
        }

        // POST: Recibir los datos editados y hacer los recálculos contables
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaccion transaccion)
        {
            if (id != transaccion.Id) return NotFound();

            ModelState.Remove("Cuenta");
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                // 1. OBTENER LA TRANSACCIÓN ANTIGUA (Sin rastrearla para evitar conflictos)
                var transaccionAntigua = await _context.Transacciones.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                var cuentaAntigua = await _context.Cuentas.FindAsync(transaccionAntigua.CuentaId);

                // --- FASE 1: REVERTIR EL EFECTO ANTIGUO ---
                // Averiguamos cuánto se le sumó/restó a la cuenta en el pasado
                decimal montoRevertir = transaccionAntigua.MontoOriginal;
                if (transaccionAntigua.MonedaTransaccion != cuentaAntigua.MonedaCuenta)
                {
                    var tasaAntigua = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccionAntigua.MonedaTransaccion && t.MonedaDestino == cuentaAntigua.MonedaCuenta);
                    if (tasaAntigua != null) montoRevertir = Math.Round(transaccionAntigua.MontoOriginal * tasaAntigua.Tasa, 2);
                }

                // Deshacemos la operación matemática antigua
                if (transaccionAntigua.Tipo == TipoTransaccion.Ingreso)
                    cuentaAntigua.SaldoActual -= montoRevertir;
                else
                    cuentaAntigua.SaldoActual += montoRevertir;

                // --- FASE 2: APLICAR EL NUEVO EFECTO ---
                var cuentaNueva = await _context.Cuentas.FindAsync(transaccion.CuentaId);
                int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
                var espacioActual = await _context.Espacios.FindAsync(espacioActualId);

                // El nuevo Monto digitado se vuelve nuestro nuevo MontoOriginal
                transaccion.MontoOriginal = transaccion.Monto;

                // A. Nuevo cálculo para el Espacio (Dashboard)
                if (transaccion.MonedaTransaccion != espacioActual.MonedaPrincipal)
                {
                    var tasaEspacio = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == espacioActual.MonedaPrincipal);
                    if (tasaEspacio == null)
                    {
                        ModelState.AddModelError("", $"Falta tasa de cambio de {transaccion.MonedaTransaccion} a {espacioActual.MonedaPrincipal}.");
                        ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CuentaId);
                        ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);
                        return View(transaccion);
                    }
                    transaccion.TasaCambioUsada = tasaEspacio.Tasa;
                    transaccion.Monto = Math.Round(transaccion.MontoOriginal * tasaEspacio.Tasa, 2);
                }
                else
                {
                    transaccion.TasaCambioUsada = 1m;
                }

                // B. Nuevo cálculo para la Cuenta
                decimal montoAplicar = transaccion.MontoOriginal;
                if (transaccion.MonedaTransaccion != cuentaNueva.MonedaCuenta)
                {
                    var tasaCuenta = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == cuentaNueva.MonedaCuenta);
                    if (tasaCuenta == null)
                    {
                        ModelState.AddModelError("", $"Falta tasa de cambio de {transaccion.MonedaTransaccion} a {cuentaNueva.MonedaCuenta}.");
                        ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CuentaId);
                        ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId), "Id", "Nombre", transaccion.CategoriaGastoId);
                        return View(transaccion);
                    }
                    montoAplicar = Math.Round(transaccion.MontoOriginal * tasaCuenta.Tasa, 2);
                }

                // Limpiar categoría si es ingreso
                if (transaccion.Tipo == TipoTransaccion.Ingreso) transaccion.CategoriaGastoId = null;

                // Aplicar la nueva operación matemática
                if (transaccion.Tipo == TipoTransaccion.Ingreso)
                    cuentaNueva.SaldoActual += montoAplicar;
                else
                    cuentaNueva.SaldoActual -= montoAplicar;

                // 3. GUARDAR TODO
                _context.Update(transaccion);
                await _context.SaveChangesAsync();

                // --- NUEVA LÍNEA PARA DISPARAR LA ALERTA VISUAL ---
                TempData["Exito"] = "Los cambios se guardaron correctamente.";

                return RedirectToAction(nameof(Index));
            }

            // Si hay error, recargar listas
            int espacioAct = int.TryParse(Request.Cookies["EspacioActivoId"], out int idC) ? idC : 1;
            ViewBag.CuentaId = new SelectList(_context.Cuentas.Where(c => c.EspacioId == espacioAct), "Id", "Nombre", transaccion.CuentaId);
            ViewBag.CategoriaGastoId = new SelectList(_context.CategoriasGastos.Where(c => c.EspacioId == espacioAct), "Id", "Nombre", transaccion.CategoriaGastoId);
            return View(transaccion);
        }

        // GET: Descargar Plantilla Excel
        public IActionResult DescargarPlantilla()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Movimientos");

            // Cabeceras (¡No cambiar estos nombres en el Excel!)
            worksheet.Cell(1, 1).Value = "Fecha (DD/MM/YYYY)";
            worksheet.Cell(1, 2).Value = "Tipo (Ingreso/Egreso)";
            worksheet.Cell(1, 3).Value = "Monto Original";
            worksheet.Cell(1, 4).Value = "Moneda (Soles/Dolares/Euros)";
            worksheet.Cell(1, 5).Value = "Descripcion";
            worksheet.Cell(1, 6).Value = "Nombre Cuenta Exacto";
            worksheet.Cell(1, 7).Value = "Nombre Categoria Exacto (Opcional)";

            // Fila de ejemplo
            worksheet.Cell(2, 1).Value = DateTime.Now.ToString("dd/MM/yyyy");
            worksheet.Cell(2, 2).Value = "Egreso";
            worksheet.Cell(2, 3).Value = 150.50;
            worksheet.Cell(2, 4).Value = "Soles";
            worksheet.Cell(2, 5).Value = "Compra de supermercado";
            worksheet.Cell(2, 6).Value = "Efectivo"; // Debe coincidir con el nombre en la BD
            worksheet.Cell(2, 7).Value = "Comida";

            // Dar formato visual a la cabecera
            worksheet.Range("A1:G1").Style.Font.Bold = true;
            worksheet.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Importacion.xlsx");
        }

        // POST: Importar datos desde Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarExcel(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
                return RedirectToAction(nameof(Index));

            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;
            var espacio = await _context.Espacios.FindAsync(espacioActualId);

            using var stream = new MemoryStream();
            await archivoExcel.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            // Omitimos la cabecera (fila 1) y leemos las filas con datos
            var filas = worksheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var fila in filas)
            {
                try
                {
                    string tipoStr = fila.Cell(2).GetString();
                    string monedaStr = fila.Cell(4).GetString();
                    string cuentaNombre = fila.Cell(6).GetString();
                    string categoriaNombre = fila.Cell(7).GetString();

                    // 1. Buscar la Cuenta en este espacio por su Nombre
                    var cuenta = await _context.Cuentas.FirstOrDefaultAsync(c => c.Nombre.ToLower() == cuentaNombre.ToLower() && c.EspacioId == espacioActualId);
                    if (cuenta == null) continue; // Si no existe la cuenta, saltamos esta fila

                    // 2. Buscar la Categoría (si se ingresó una)
                    int? catId = null;
                    if (!string.IsNullOrEmpty(categoriaNombre))
                    {
                        var categoria = await _context.CategoriasGastos.FirstOrDefaultAsync(c => c.Nombre.ToLower() == categoriaNombre.ToLower() && c.EspacioId == espacioActualId);
                        if (categoria != null) catId = categoria.Id;
                    }

                    // 3. Crear el nuevo movimiento
                    var transaccion = new Transaccion
                    {
                        Fecha = fila.Cell(1).GetDateTime(),
                        Tipo = tipoStr.Equals("Ingreso", StringComparison.OrdinalIgnoreCase) ? TipoTransaccion.Ingreso : TipoTransaccion.Egreso,
                        MontoOriginal = fila.Cell(3).GetValue<decimal>(),
                        MonedaTransaccion = Enum.Parse<Moneda>(monedaStr, true),
                        Descripcion = fila.Cell(5).GetString(),
                        CuentaId = cuenta.Id,
                        CategoriaGastoId = catId
                    };

                    // --- LÓGICA DE CONVERSIÓN SIMPLIFICADA PARA IMPORTACIÓN ---
                    // A. Convertir para el espacio (Historial)
                    if (transaccion.MonedaTransaccion != espacio.MonedaPrincipal)
                    {
                        var tc = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == espacio.MonedaPrincipal);
                        transaccion.TasaCambioUsada = tc != null ? tc.Tasa : 1m;
                        transaccion.Monto = Math.Round(transaccion.MontoOriginal * transaccion.TasaCambioUsada, 2);
                    }
                    else { transaccion.Monto = transaccion.MontoOriginal; transaccion.TasaCambioUsada = 1m; }

                    // B. Aplicar a la cuenta bancaria
                    decimal montoAplicar = transaccion.MontoOriginal;
                    if (transaccion.MonedaTransaccion != cuenta.MonedaCuenta)
                    {
                        var tcC = await _context.TiposCambio.FirstOrDefaultAsync(t => t.MonedaOrigen == transaccion.MonedaTransaccion && t.MonedaDestino == cuenta.MonedaCuenta);
                        if (tcC != null) montoAplicar = Math.Round(transaccion.MontoOriginal * tcC.Tasa, 2);
                    }

                    if (transaccion.Tipo == TipoTransaccion.Ingreso) cuenta.SaldoActual += montoAplicar;
                    else cuenta.SaldoActual -= montoAplicar;

                    _context.Add(transaccion);
                }
                catch (Exception)
                {
                    // Si una fila tiene un error (fecha mal formateada, etc), la saltamos
                    continue;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Exito"] = "¡Excel importado correctamente!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Recibe contenido compartido desde otras apps (Share Target API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromShare(string? descripcion, string? nota, string? referencia, IFormFile? imagen)
        {
            // Si viene una imagen, procesarla con OCR
            if (imagen != null && imagen.Length > 0)
            {
                var resultadoOcr = await _ocrService.ProcesarTicket(imagen);
                
                // Redirigir a la vista de creación con los datos extraídos
                return RedirectToAction(nameof(CreateFromImage), new { 
                    monto = resultadoOcr.Monto,
                    fecha = resultadoOcr.Fecha?.ToString("yyyy-MM-dd"),
                    descripcionOcr = resultadoOcr.Descripcion ?? descripcion,
                    establecimiento = resultadoOcr.Establecimiento,
                    rutaImagen = resultadoOcr.RutaImagen,
                    textoCompleto = resultadoOcr.TextoCompleto,
                    confianza = resultadoOcr.Confianza,
                    mensajes = string.Join("|", resultadoOcr.Mensajes)
                });
            }

            // Si solo viene texto, redirigir al formulario normal con los datos
            return RedirectToAction(nameof(Create), new { descripcion, nota });
        }

        // GET: Vista especial para transacciones desde imágenes compartidas
        public async Task<IActionResult> CreateFromImage(
            decimal? monto, 
            string? fecha, 
            string? descripcionOcr, 
            string? establecimiento,
            string? rutaImagen,
            string? textoCompleto,
            float? confianza,
            string? mensajes)
        {
            int espacioActualId = int.TryParse(Request.Cookies["EspacioActivoId"], out int idCookie) ? idCookie : 1;

            var cuentasDelEspacio = _context.Cuentas.Where(c => c.EspacioId == espacioActualId).ToList();
            var categoriasDelEspacio = _context.CategoriasGastos.Where(c => c.EspacioId == espacioActualId).ToList();

            ViewBag.CuentaId = new SelectList(cuentasDelEspacio, "Id", "Nombre");
            ViewBag.CategoriaGastoId = new SelectList(categoriasDelEspacio, "Id", "Nombre");

            var tarjetasIds = cuentasDelEspacio.Where(c => c.EsCredito).Select(c => c.Id).ToList();
            ViewBag.TarjetasIds = System.Text.Json.JsonSerializer.Serialize(tarjetasIds);

            // Obtener moneda preferida
            var nombreUsuario = User.Identity?.Name;
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            var espacioActivo = await _context.Espacios.FindAsync(espacioActualId);
            var monedaPorDefecto = usuario?.MonedaPreferida ?? espacioActivo?.MonedaPrincipal ?? Moneda.Soles;

            ViewBag.SimboloMoneda = monedaPorDefecto == Moneda.Dolares ? "$" :
                                   (monedaPorDefecto == Moneda.Euros ? "€" : "S/");

            // Crear transacción con datos del OCR
            var nuevaTransaccion = new Transaccion
            {
                MonedaTransaccion = monedaPorDefecto,
                Fecha = !string.IsNullOrEmpty(fecha) && DateTime.TryParse(fecha, out var parsedDate) 
                    ? parsedDate 
                    : DateTime.Now,
                Monto = monto ?? 0,
                Descripcion = descripcionOcr ?? "Compra con ticket",
                Tipo = TipoTransaccion.Egreso
            };

            // Pasar información del OCR a la vista
            ViewBag.RutaImagen = rutaImagen;
            ViewBag.TextoCompleto = textoCompleto;
            ViewBag.Confianza = confianza;
            ViewBag.Establecimiento = establecimiento;
            
            if (!string.IsNullOrEmpty(mensajes))
            {
                ViewBag.MensajesOcr = mensajes.Split('|').ToList();
            }

            return View(nuevaTransaccion);
        }

        // GET: Vista de prueba para OCR (Testing en Windows/iPhone)
        [HttpGet]
        public IActionResult TestOcr()
        {
            return View();
        }

        // POST: Procesar imagen de prueba con OCR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestOcr(IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
            {
                TempData["Error"] = "Por favor selecciona una imagen";
                return View();
            }

            try
            {
                var resultado = await _ocrService.ProcesarTicket(imagen);
                
                // Redirigir a la vista de creación con los datos extraídos
                return RedirectToAction(nameof(CreateFromImage), new { 
                    monto = resultado.Monto,
                    fecha = resultado.Fecha?.ToString("yyyy-MM-dd"),
                    descripcionOcr = resultado.Descripcion,
                    establecimiento = resultado.Establecimiento,
                    rutaImagen = resultado.RutaImagen,
                    textoCompleto = resultado.TextoCompleto,
                    confianza = resultado.Confianza,
                    mensajes = string.Join("|", resultado.Mensajes)
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la imagen: {ex.Message}";
                return View();
            }
        }
    }
}