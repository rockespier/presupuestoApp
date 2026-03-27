using System.Text.RegularExpressions;
using Tesseract;
using PresupuestoFamiliarApp.Models.DTOs;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
                _logger.LogWarning($"Carpeta tessdata creada en: {_tessDataPath}");
            }
            
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
                _logger.LogInformation($"Carpeta uploads/tickets creada en: {_uploadsPath}");
            }
        }

        /// <summary>
        /// Procesa una imagen de ticket y extrae información mediante OCR
        /// Detecta autom�ticamente si usar espa�ol, italiano o ambos
        /// </summary>
        public async Task<TransaccionOcrResult> ProcesarTicket(IFormFile imagen)
        {
            var resultado = new TransaccionOcrResult();

            try
            {
                _logger.LogInformation($"Procesando imagen: {imagen.FileName} ({imagen.Length} bytes)");

                // 1. Validar formato antes de procesar
                if (!EsFormatoSoportado(imagen))
                {
                    resultado.ExitosoExtraccion = false;
                    resultado.Mensajes.Add("⚠️ Formato de imagen no compatible.");
                    resultado.Mensajes.Add("📱 En iPhone: usa la opción 'Más compatible' en Ajustes > Cámara > Formatos, o selecciona la imagen desde la Galería.");
                    resultado.Mensajes.Add("✅ Formatos aceptados: JPG, PNG, WebP, GIF");
                    _logger.LogWarning($"Formato no soportado: {imagen.ContentType} / {Path.GetExtension(imagen.FileName)}");
                    return resultado;
                }

                // 2. Guardar la imagen (nombre saneado para evitar caracteres especiales)
                var nombreSaneado = Regex.Replace(Path.GetFileName(imagen.FileName), @"[^\w\.\-]", "_");
                var nombreArchivo = $"{Guid.NewGuid()}_{nombreSaneado}";
                var rutaCompleta = Path.Combine(_uploadsPath, nombreArchivo);
                
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
                
                resultado.RutaImagen = $"/uploads/tickets/{nombreArchivo}";
                _logger.LogInformation($"Imagen guardada en: {resultado.RutaImagen}");

                // 3. Detectar qué idiomas están disponibles
                var idiomasActivos = DetectarIdiomasDisponibles();
                
                if (idiomasActivos.Count == 0)
                {
                    _logger.LogError("No se encontraron archivos de entrenamiento OCR");
                    resultado.Mensajes.Add("⚠️ No se encontraron archivos de entrenamiento OCR.");
                    resultado.Mensajes.Add("📥 Descarga los archivos necesarios:");
                    resultado.Mensajes.Add("   - Español: https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata");
                    resultado.Mensajes.Add("   - Italiano: https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata");
                    resultado.Mensajes.Add($"📁 Y colócalos en: {_tessDataPath}");
                    resultado.ExitosoExtraccion = false;
                    return resultado;
                }

                // 4. Crear cadena de idiomas para Tesseract (ej: "spa+ita")
                var cadenaIdiomas = string.Join("+", idiomasActivos);
                _logger.LogInformation($"Idiomas activos para OCR: {cadenaIdiomas}");
                resultado.Mensajes.Add($"🌐 Procesando con idiomas: {string.Join(", ", idiomasActivos.Select(ObtenerNombreIdioma))}");

                // 5. Normalizar imagen (auto-rotar según EXIF, convertir a PNG)
                // Esto resuelve problemas con fotos de iPhone que tienen metadatos EXIF de orientación
                // o formatos JPEG progresivos que Leptonica no puede leer correctamente.
                var rutaNormalizada = await NormalizarImagenAsync(rutaCompleta);

                // 6. Ejecutar OCR con múltiples idiomas
                _logger.LogInformation("Ejecutando Tesseract OCR...");
                try
                {
                    using (var engine = new TesseractEngine(_tessDataPath, cadenaIdiomas, EngineMode.Default))
                    {
                        using (var img = Pix.LoadFromFile(rutaNormalizada))
                        {
                            using (var page = engine.Process(img))
                            {
                                resultado.TextoCompleto = page.GetText();
                                resultado.Confianza = page.GetMeanConfidence() * 100;

                                _logger.LogInformation($"OCR completado con confianza: {resultado.Confianza:F1}%");
                                _logger.LogDebug($"Texto extraído: {resultado.TextoCompleto.Substring(0, Math.Min(100, resultado.TextoCompleto.Length))}...");
                            }
                        }
                    }
                }
                finally
                {
                    // Limpiar el archivo PNG temporal si es diferente del original
                    if (rutaNormalizada != rutaCompleta && File.Exists(rutaNormalizada))
                    {
                        try { File.Delete(rutaNormalizada); } catch (Exception ex) { _logger.LogDebug(ex, "No se pudo eliminar la imagen normalizada temporal"); }
                    }
                }

                // 7. Extraer información específica del texto
                ExtraerInformacion(resultado);

                resultado.ExitosoExtraccion = true;
                resultado.Mensajes.Add($"✅ Imagen procesada exitosamente (Confianza: {resultado.Confianza:F1}%)");
                
                if (resultado.Monto.HasValue)
                {
                    resultado.Mensajes.Add($"💰 Monto detectado: {resultado.Monto:F2}");
                    _logger.LogInformation($"Monto extraído: {resultado.Monto:F2}");
                }
                
                if (resultado.Fecha.HasValue)
                {
                    resultado.Mensajes.Add($"📅 Fecha detectada: {resultado.Fecha:dd/MM/yyyy}");
                    _logger.LogInformation($"Fecha extraída: {resultado.Fecha:dd/MM/yyyy}");
                }
                
                if (!string.IsNullOrEmpty(resultado.Establecimiento))
                {
                    resultado.Mensajes.Add($"🏪 Establecimiento: {resultado.Establecimiento}");
                    _logger.LogInformation($"Establecimiento extraído: {resultado.Establecimiento}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar imagen con OCR");
                resultado.ExitosoExtraccion = false;

                // Mostrar el mensaje de error real (la excepción interna contiene el detalle)
                var mensajeError = ex.InnerException?.Message ?? ex.Message;
                resultado.Mensajes.Add($"❌ Error: {mensajeError}");

                if (ex.InnerException?.InnerException != null)
                {
                    _logger.LogError($"Detalle adicional: {ex.InnerException.InnerException.Message}");
                }
            }

            return resultado;
        }

        /// <summary>
        /// Valida si el formato del archivo es compatible con Tesseract OCR.
        /// Detecta HEIC/HEIF (formato nativo de iPhone) y otros formatos no soportados
        /// comprobando tanto el Content-Type como la firma de bytes del archivo.
        /// </summary>
        private bool EsFormatoSoportado(IFormFile imagen)
        {
            // Tipos MIME aceptados
            var tiposAceptados = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // "image/jpg" is non-standard but some browsers send it alongside the correct "image/jpeg"
                "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif", "image/bmp", "image/tiff"
            };

            // Tipos MIME rechazados explícitamente (HEIC/HEIF = formato iPhone)
            var tiposHeic = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/heic", "image/heif", "image/heic-sequence", "image/heif-sequence"
            };

            if (tiposHeic.Contains(imagen.ContentType))
                return false;

            if (!tiposAceptados.Contains(imagen.ContentType))
            {
                // Verificar por extensión como respaldo
                var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
                var extensionesAceptadas = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif" };
                var extensionesHeic = new HashSet<string> { ".heic", ".heif" };

                if (extensionesHeic.Contains(extension))
                    return false;

                if (!extensionesAceptadas.Contains(extension))
                    return false;
            }

            // Verificar firma de bytes (magic bytes) para detectar HEIC aunque venga con Content-Type incorrecto
            using var stream = imagen.OpenReadStream();
            var header = new byte[12];
            var bytesRead = stream.Read(header, 0, header.Length);

            if (bytesRead >= 12)
            {
                // HEIC/HEIF: bytes 4-7 contienen 'ftyp', bytes 8-11 contienen la marca
                if (header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70)
                {
                    // Es un archivo ISOBMFF (MP4/MOV/HEIC/HEIF)
                    var brand = System.Text.Encoding.ASCII.GetString(header, 8, 4);
                    // Known HEIC/HEIF brand codes per ISO/IEC 23008-12:
                    // heic/heix = HEIC image, hevc/hevx = HEVC-based, heim/heis/hevm/hevs = multi-image,
                    // mif1/msf1 = generic HEIF container
                    var heicBrands = new HashSet<string> { "heic", "heix", "hevc", "hevx", "heim", "heis", "hevm", "hevs", "mif1", "msf1" };
                    if (heicBrands.Contains(brand.ToLower()))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Normaliza la imagen para asegurar compatibilidad con Tesseract/Leptonica.
        /// Aplica la orientación EXIF (crítico para fotos de iPhone) y convierte a PNG.
        /// Si la normalización falla, devuelve la ruta original para que Tesseract lo intente igualmente.
        /// </summary>
        private async Task<string> NormalizarImagenAsync(string rutaOriginal)
        {
            try
            {
                var rutaPng = Path.Combine(
                    Path.GetDirectoryName(rutaOriginal) ?? _uploadsPath,
                    Path.GetFileNameWithoutExtension(rutaOriginal) + "_norm.png");
                using var image = await Image.LoadAsync(rutaOriginal);
                // AutoOrient aplica la rotación indicada en los metadatos EXIF (orientación de iPhone)
                image.Mutate(x => x.AutoOrient());
                await image.SaveAsPngAsync(rutaPng);
                _logger.LogInformation("Imagen normalizada a PNG para Tesseract");
                return rutaPng;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo normalizar la imagen; se usará el archivo original");
                return rutaOriginal;
            }
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
                    _logger.LogInformation($"Idioma encontrado: {ObtenerNombreIdioma(idioma)} ({idioma}.traineddata)");
                }
                else
                {
                    _logger.LogWarning($"Idioma NO encontrado: {ObtenerNombreIdioma(idioma)} ({idioma}.traineddata)");
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
                _logger.LogWarning("No hay texto para extraer información");
                return;
            }

            var lineas = resultado.TextoCompleto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogDebug($"Analizando {lineas.Length} líneas de texto");

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
                        _logger.LogDebug($"Monto encontrado con patrón: {patron} → {monto}");
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
                            _logger.LogDebug($"Fecha encontrada: {fechaStr} → {fecha:yyyy-MM-dd}");
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
                _logger.LogDebug("No se encontró fecha en el ticket, usando fecha actual");
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
                        _logger.LogDebug($"Establecimiento encontrado: {lineaLimpia}");
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
                _logger.LogDebug($"Conceptos encontrados: {resultado.Descripcion}");
            }
            else if (!string.IsNullOrEmpty(resultado.Establecimiento))
            {
                resultado.Descripcion = $"Compra en {resultado.Establecimiento}";
                _logger.LogDebug("Descripción generada desde establecimiento");
            }
            else
            {
                resultado.Descripcion = "Compra con ticket";
                _logger.LogDebug("Descripción por defecto");
            }
        }
    }
}
