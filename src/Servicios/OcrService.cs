using System.Text.RegularExpressions;
using Tesseract;
using PresupuestoFamiliarApp.Models.DTOs;
using System.Globalization;

namespace PresupuestoFamiliarApp.Servicios
{
    /// <summary>
    /// Servicio para procesar imágenes de tickets/facturas y extraer información
    /// mediante OCR (Optical Character Recognition)
    /// Soporta múltiples idiomas: Español e Italiano
    /// </summary>
    public class OcrService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<OcrService> _logger;
        private readonly string _tessDataPath;
        private readonly string _uploadsPath;
        private readonly string[] _idiomasDisponibles;

        public OcrService(IWebHostEnvironment environment, ILogger<OcrService> logger)
        {
            _environment = environment;
            _logger = logger;
            
            // Ruta donde están los archivos de entrenamiento de Tesseract
            _tessDataPath = Path.Combine(_environment.ContentRootPath, "tessdata");
            
            // Ruta donde se guardarán las imágenes subidas
            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "tickets");
            
            // Idiomas que buscamos (orden de prioridad)
            _idiomasDisponibles = new[] { "spa", "ita", "eng" };
            
            // Crear directorios si no existen
            if (!Directory.Exists(_tessDataPath))
            {
                Directory.CreateDirectory(_tessDataPath);
                _logger.LogWarning($"?? Carpeta tessdata creada en: {_tessDataPath}");
            }
            
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
                _logger.LogInformation($"?? Carpeta uploads/tickets creada en: {_uploadsPath}");
            }
        }

        /// <summary>
        /// Procesa una imagen de ticket y extrae información mediante OCR
        /// Detecta automáticamente si usar español, italiano o ambos
        /// </summary>
        public async Task<TransaccionOcrResult> ProcesarTicket(IFormFile imagen)
        {
            var resultado = new TransaccionOcrResult();

            try
            {
                _logger.LogInformation($"?? Procesando imagen: {imagen.FileName} ({imagen.Length} bytes)");

                // 1. Guardar la imagen
                var nombreArchivo = $"{Guid.NewGuid()}_{Path.GetFileName(imagen.FileName)}";
                var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
                
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
                
                resultado.RutaImagen = $"/uploads/tickets/{nombreArchivo}";
                _logger.LogInformation($"?? Imagen guardada en: {resultado.RutaImagen}");

                // 2. Detectar qué idiomas están disponibles
                var idiomasActivos = DetectarIdiomasDisponibles();
                
                if (idiomasActivos.Count == 0)
                {
                    _logger.LogError($"? No se encontraron archivos de entrenamiento OCR");
                    resultado.Mensajes.Add("?? No se encontraron archivos de entrenamiento OCR.");
                    resultado.Mensajes.Add("?? Descarga los archivos necesarios:");
                    resultado.Mensajes.Add("   - Español: https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata");
                    resultado.Mensajes.Add("   - Italiano: https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata");
                    resultado.Mensajes.Add($"?? Y colócalos en: {_tessDataPath}");
                    resultado.ExitosoExtraccion = false;
                    return resultado;
                }

                // 3. Crear cadena de idiomas para Tesseract (ej: "spa+ita")
                var cadenaIdiomas = string.Join("+", idiomasActivos);
                _logger.LogInformation($"?? Idiomas activos para OCR: {cadenaIdiomas}");
                resultado.Mensajes.Add($"?? Procesando con idiomas: {string.Join(", ", idiomasActivos.Select(ObtenerNombreIdioma))}");

                // 4. Ejecutar OCR con múltiples idiomas
                _logger.LogInformation("?? Ejecutando Tesseract OCR...");
                using (var engine = new TesseractEngine(_tessDataPath, cadenaIdiomas, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(rutaCompleta))
                    {
                        using (var page = engine.Process(img))
                        {
                            resultado.TextoCompleto = page.GetText();
                            resultado.Confianza = page.GetMeanConfidence() * 100;
                            
                            _logger.LogInformation($"? OCR completado con confianza: {resultado.Confianza:F1}%");
                            _logger.LogDebug($"?? Texto extraído: {resultado.TextoCompleto.Substring(0, Math.Min(100, resultado.TextoCompleto.Length))}...");
                        }
                    }
                }

                // 5. Extraer información específica del texto
                ExtraerInformacion(resultado);

                resultado.ExitosoExtraccion = true;
                resultado.Mensajes.Add($"? Imagen procesada exitosamente (Confianza: {resultado.Confianza:F1}%)");
                
                if (resultado.Monto.HasValue)
                {
                    resultado.Mensajes.Add($"?? Monto detectado: {resultado.Monto:F2}");
                    _logger.LogInformation($"?? Monto extraído: {resultado.Monto:F2}");
                }
                
                if (resultado.Fecha.HasValue)
                {
                    resultado.Mensajes.Add($"?? Fecha detectada: {resultado.Fecha:dd/MM/yyyy}");
                    _logger.LogInformation($"?? Fecha extraída: {resultado.Fecha:dd/MM/yyyy}");
                }
                
                if (!string.IsNullOrEmpty(resultado.Establecimiento))
                {
                    resultado.Mensajes.Add($"?? Establecimiento: {resultado.Establecimiento}");
                    _logger.LogInformation($"?? Establecimiento extraído: {resultado.Establecimiento}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error al procesar imagen con OCR");
                resultado.ExitosoExtraccion = false;
                resultado.Mensajes.Add($"? Error: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError($"   Detalle: {ex.InnerException.Message}");
                }
            }

            return resultado;
        }

        /// <summary>
        /// Detecta qué archivos de idioma están disponibles en el sistema
        /// </summary>
        private List<string> DetectarIdiomasDisponibles()
        {
            var idiomasEncontrados = new List<string>();

            foreach (var idioma in _idiomasDisponibles)
            {
                var archivoIdioma = Path.Combine(_tessDataPath, $"{idioma}.traineddata");
                if (File.Exists(archivoIdioma))
                {
                    idiomasEncontrados.Add(idioma);
                    _logger.LogInformation($"? Idioma encontrado: {ObtenerNombreIdioma(idioma)} ({idioma}.traineddata)");
                }
                else
                {
                    _logger.LogWarning($"?? Idioma NO encontrado: {ObtenerNombreIdioma(idioma)} ({idioma}.traineddata)");
                }
            }

            return idiomasEncontrados;
        }

        /// <summary>
        /// Obtiene el nombre completo del idioma desde su código
        /// </summary>
        private string ObtenerNombreIdioma(string codigo)
        {
            return codigo switch
            {
                "spa" => "Español",
                "ita" => "Italiano",
                "eng" => "Inglés",
                _ => codigo.ToUpper()
            };
        }

        /// <summary>
        /// Extrae información estructurada del texto OCR
        /// Soporta patrones en español e italiano
        /// </summary>
        private void ExtraerInformacion(TransaccionOcrResult resultado)
        {
            if (string.IsNullOrWhiteSpace(resultado.TextoCompleto))
            {
                _logger.LogWarning("?? No hay texto para extraer información");
                return;
            }

            var lineas = resultado.TextoCompleto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogDebug($"?? Analizando {lineas.Length} líneas de texto");

            // 1. Extraer MONTO (patrones en español e italiano)
            var patronesMonto = new[]
            {
                // Español
                @"(?:total|importe|monto|precio|pagar|pagado|subtotal|neto)[\s:]*[S/$€]?\s*(\d+[.,]\d{2})",
                // Italiano
                @"(?:totale|importo|prezzo|pagare|subtotale|netto)[\s:]*[S/$€]?\s*(\d+[.,]\d{2})",
                // Genérico
                @"[S/$€]\s*(\d+[.,]\d{2})",
                @"(\d+[.,]\d{2})\s*(?:soles|dolares|usd|pen|eur|euro|dollars)",
                @"\b(\d{1,5}[.,]\d{2})\b"
            };

            foreach (var patron in patronesMonto)
            {
                var match = Regex.Match(resultado.TextoCompleto, patron, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var montoStr = match.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var monto))
                    {
                        resultado.Monto = monto;
                        _logger.LogDebug($"?? Monto encontrado con patrón: {patron} ? {monto}");
                        break;
                    }
                }
            }

            // 2. Extraer FECHA (formatos españoles e italianos)
            var patronesFecha = new[]
            {
                @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})", // DD/MM/YYYY (común en ambos)
                @"(\d{4}[/-]\d{1,2}[/-]\d{1,2})", // YYYY-MM-DD
            };

            foreach (var patron in patronesFecha)
            {
                var match = Regex.Match(resultado.TextoCompleto, patron);
                if (match.Success)
                {
                    var fechaStr = match.Groups[1].Value;
                    
                    var formatos = new[] { "dd/MM/yyyy", "dd-MM-yyyy", "dd/MM/yy", "dd-MM-yy", "yyyy-MM-dd", "yyyy/MM/dd" };
                    
                    foreach (var formato in formatos)
                    {
                        if (DateTime.TryParseExact(fechaStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                        {
                            resultado.Fecha = fecha;
                            _logger.LogDebug($"?? Fecha encontrada: {fechaStr} ? {fecha:yyyy-MM-dd}");
                            break;
                        }
                    }
                    
                    if (resultado.Fecha.HasValue)
                        break;
                }
            }

            if (!resultado.Fecha.HasValue)
            {
                resultado.Fecha = DateTime.Now;
                _logger.LogDebug("?? No se encontró fecha en el ticket, usando fecha actual");
            }

            // 3. Extraer ESTABLECIMIENTO
            if (lineas.Length > 0)
            {
                var primerasLineas = lineas.Take(5).ToList();
                foreach (var linea in primerasLineas)
                {
                    var lineaLimpia = linea.Trim();
                    
                    // Palabras a ignorar (español e italiano)
                    var palabrasIgnorar = new[] { 
                        "ticket", "factura", "boleta", "ruc", "fecha", "hora", "nit", "tel", "phone",
                        "scontrino", "fattura", "ricevuta", "data", "ora", "tel", "telefono" 
                    };
                    
                    if (lineaLimpia.Length >= 3 
                        && lineaLimpia.Length <= 50 
                        && !Regex.IsMatch(lineaLimpia, @"^\d+$")
                        && !palabrasIgnorar.Any(p => lineaLimpia.ToLower().Contains(p)))
                    {
                        resultado.Establecimiento = lineaLimpia;
                        _logger.LogDebug($"?? Establecimiento encontrado: {lineaLimpia}");
                        break;
                    }
                }
            }

            // 4. Extraer DESCRIPCIÓN (palabras clave en español e italiano)
            var conceptos = new List<string>();
            var palabrasClave = new[] { 
                // Español
                "producto", "servicio", "artículo", "item", "concepto", "descripción",
                // Italiano
                "prodotto", "servizio", "articolo", "voce", "descrizione" 
            };
            
            foreach (var linea in lineas)
            {
                var lineaLower = linea.ToLower();
                if (palabrasClave.Any(p => lineaLower.Contains(p)))
                {
                    var concepto = linea.Trim();
                    if (concepto.Length > 3 && concepto.Length < 100)
                    {
                        conceptos.Add(concepto);
                    }
                }
            }

            if (conceptos.Any())
            {
                resultado.Descripcion = string.Join(", ", conceptos.Take(3));
                _logger.LogDebug($"?? Conceptos encontrados: {resultado.Descripcion}");
            }
            else if (!string.IsNullOrEmpty(resultado.Establecimiento))
            {
                resultado.Descripcion = $"Compra en {resultado.Establecimiento}";
                _logger.LogDebug($"?? Descripción generada desde establecimiento");
            }
            else
            {
                resultado.Descripcion = "Compra con ticket";
                _logger.LogDebug($"?? Descripción por defecto");
            }
        }
    }
}
