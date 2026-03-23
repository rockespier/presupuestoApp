using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Models;
using ScottPlot; // Librería para el gráfico

namespace PresupuestoFamiliarApp.Servicios
{
    public class AutomatizacionService
    {
        private readonly PresupuestoContext _context;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public AutomatizacionService(PresupuestoContext context, EmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _env = env;
        }

        public async Task ProcesarMovimientosFijos()
        {
            var hoy = DateTime.Now;

            // Buscar movimientos fijos activos que coincidan con el día de hoy
            var movimientos = await _context.MovimientosFijos
                .Where(m => m.Activo && m.DiaDelMes == hoy.Day)
                .ToListAsync();

            foreach (var mf in movimientos)
            {
                // Crear la transacción real basada en el movimiento fijo
                var nuevaTransaccion = new Transaccion
                {
                    Fecha = hoy,
                    Monto = mf.Monto,
                    MontoOriginal = mf.Monto,
                    Tipo = mf.Tipo,
                    Descripcion = $"{mf.Descripcion} (Automático)",
                    CuentaId = mf.CuentaId,
                    CategoriaGastoId = mf.CategoriaGastoId,
                    EsTransferencia = false
                };

                _context.Transacciones.Add(nuevaTransaccion);

                // Actualizar saldo de la cuenta
                var cuenta = await _context.Cuentas.FindAsync(mf.CuentaId);
                if (cuenta != null)
                {
                    if (mf.Tipo == TipoTransaccion.Ingreso) cuenta.SaldoActual += mf.Monto;
                    else cuenta.SaldoActual -= mf.Monto;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task EnviarResumenMensual()
        {
            var mesPasado = DateTime.Now.AddMonths(-1);

            // Obtener todos los espacios con sus usuarios
            var espacios = await _context.Espacios
                .Include(e => e.Usuarios)
                .ToListAsync();

            foreach (var espacio in espacios)
            {
                // 1. Agrupar gastos por categoría para este espacio específico
                var datosGrafico = await _context.Transacciones
                    .Include(t => t.Cuenta)
                    .Include(t => t.Categoria)
                    .Where(t => t.Cuenta.EspacioId == espacio.Id &&
                                t.Fecha.Month == mesPasado.Month &&
                                t.Fecha.Year == mesPasado.Year &&
                                t.Tipo == TipoTransaccion.Egreso &&
                                !t.EsTransferencia &&
                                !t.Cuenta.EsCredito)
                    .GroupBy(t => t.Categoria.Nombre)
                    .Select(g => new { Categoria = g.Key, Total = (double)g.Sum(t => t.Monto) })
                    .ToListAsync();

                // Si no hay datos para este espacio, continuamos con el siguiente
                if (!datosGrafico.Any()) continue;

                // --- CALCULAR EL TOTAL PARA LOS PORCENTAJES ---
                double totalGastos = datosGrafico.Sum(d => d.Total);

                // --- 1. PREPARAR LOS DATOS Y COLORES CON PORCENTAJES ---
                var paletaColores = new ScottPlot.Palettes.Category10();

                var slices = datosGrafico.Select((d, index) =>
                {
                    double porcentaje = (d.Total / totalGastos) * 100;
                    string nombreCategoria = string.IsNullOrEmpty(d.Categoria) ? "Sin Categoría" : d.Categoria;

                    return new ScottPlot.PieSlice
                    {
                        Value = d.Total,
                        Label = $"{nombreCategoria}\n{porcentaje:F1}%",
                        FillColor = paletaColores.GetColor(index)
                    };
                }).ToList();

                // --- 2. CREAR EL GRÁFICO ---
                var plt = new ScottPlot.Plot();
                var pie = plt.Add.Pie(slices);

                pie.ExplodeFraction = 0.1;
                pie.SliceLabelDistance = 1.4;

                plt.Legend.IsVisible = true;
                plt.Legend.Alignment = Alignment.LowerCenter;
                plt.Legend.FontSize = 12;
                plt.ShowLegend();

                plt.Title($"Gastos por Categoría - {espacio.Nombre} - {mesPasado:MMMM yyyy}");
                plt.Axes.Title.Label.FontSize = 16;
                plt.Axes.Title.Label.Bold = true;

                plt.HideGrid();
                plt.Axes.Frameless();

                byte[] imagenBytes = plt.GetImageBytes(900, 600, ScottPlot.ImageFormat.Png);

                // --- 3. CONSTRUIR EL CONTENIDO HTML ---
                string simboloMoneda = espacio.MonedaPrincipal == Moneda.Dolares ? "$" :
                                      (espacio.MonedaPrincipal == Moneda.Euros ? "€" : "S/");

                string contenidoDatos = $@"
<h2 style='color: #2c3e50;'>Resumen de Gastos: {mesPasado:MMMM yyyy}</h2>
<h3 style='color: #34495e;'>Espacio: {espacio.Nombre}</h3>
<p>Este es el resumen de gastos de este mes para el espacio <b>{espacio.Nombre}</b>:</p>                
<img src='cid:graficoID' alt='Gráfico de Gastos' style='width: 100%; max-width: 900px; height: auto; margin: 20px 0;' />
<br><br>

<table border='1' style='border-collapse: collapse; width: 100%; text-align: left; margin-top: 20px;'>
    <tr style='background-color: #f2f2f2;'>
        <th style='padding: 10px;'>Categoría</th>
        <th style='padding: 10px;'>Monto</th>
        <th style='padding: 10px;'>Porcentaje</th>
    </tr>";

                foreach (var item in datosGrafico)
                {
                    string nombreCategoria = string.IsNullOrEmpty(item.Categoria) ? "Sin Categoría" : item.Categoria;
                    double porcentaje = (item.Total / totalGastos) * 100;
                    contenidoDatos += $@"
    <tr>
        <td style='padding: 8px;'>{nombreCategoria}</td>
        <td style='padding: 8px;'>{simboloMoneda} {item.Total:N2}</td>
        <td style='padding: 8px;'>{porcentaje:F1}%</td>
    </tr>";
                }

                contenidoDatos += $@"
    <tr style='background-color: #e8f4f8; font-weight: bold;'>
        <td style='padding: 10px;'>TOTAL</td>
        <td style='padding: 10px;'>{simboloMoneda} {totalGastos:N2}</td>
        <td style='padding: 10px;'>100.0%</td>
    </tr>
</table>";

                // --- 4. LEER LA PLANTILLA HTML ---
                string rutaPlantilla = Path.Combine(_env.ContentRootPath, "Html", "reporte.html");
                string plantillaHtml = await File.ReadAllTextAsync(rutaPlantilla);
                plantillaHtml = plantillaHtml.Replace("{Datos}", contenidoDatos);

                // --- 5. ENVIAR EL CORREO A TODOS LOS USUARIOS DEL ESPACIO ---
                foreach (var usuario in espacio.Usuarios)
                {
                    try
                    {
                        await _emailService.EnviarCorreo(
                            usuario.Email,
                            $"📊 Reporte de {espacio.Nombre} - {mesPasado:MMMM yyyy}",
                            plantillaHtml,
                            imagenBytes
                        );

                        Console.WriteLine($"✅ Correo enviado a {usuario.Email} para el espacio '{espacio.Nombre}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error al enviar correo a {usuario.Email}: {ex.Message}");
                    }
                }
            }
        }

        public async Task GenerarDeudasMensuales()
        {
            var mesActual = DateTime.Now;

            // Traemos a todos los inquilinos con sus deudas
            var deudores = await _context.Deudores.Include(d => d.CuentasPorCobrar).ToListAsync();

            foreach (var deudor in deudores)
            {
                // Buscamos la deuda más reciente de este inquilino para copiar el monto
                var ultimaDeuda = deudor.CuentasPorCobrar
                    .Where(c => c.EsAlquiler) // Solo consideramos las deudas que son alquileres
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefault();

                // Si no tiene ningún alquiler registrado, saltamos a la siguiente persona
                if (ultimaDeuda == null) continue;

                // Verificamos si ya generamos el alquiler de este mes
                var yaExisteAlquilerDelMes = deudor.CuentasPorCobrar
                    .Any(c => c.EsAlquiler &&
                             c.FechaCreacion.Year == mesActual.Year &&
                             c.FechaCreacion.Month == mesActual.Month);

                // Si ya existe, no lo generamos de nuevo
                if (yaExisteAlquilerDelMes) continue;

                var nuevaDeuda = new CuentaPorCobrar
                    {
                        DeudorId = deudor.Id,
                        Concepto = $"Alquiler {mesActual:MMMM yyyy}", // Ej: "Alquiler marzo 2026"
                        MontoTotal = ultimaDeuda.MontoTotal, // Copiamos el monto del mes anterior
                        MontoPagado = 0,
                        FechaCreacion = DateTime.Now,
                        // Le damos 5 días de margen para pagar (puedes cambiar este número)
                        FechaVencimiento = DateTime.Now.AddDays(5),
                        EsAlquiler = true
                };

                    _context.CuentasPorCobrar.Add(nuevaDeuda);
                
            }

            // Guardamos todos los nuevos alquileres de golpe en la base de datos
            await _context.SaveChangesAsync();
        }
    }
}