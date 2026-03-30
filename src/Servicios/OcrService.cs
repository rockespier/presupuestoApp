using System.Text.RegularExpressions;
using Tesseract;
using PresupuestoFamiliarApp.Models.DTOs;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

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
                _logger.LogInformation($"? Imagen guardada en: {resultado.RutaImagen}");

                // 2. Preprocesar imagen para mejorar OCR
                var rutaProcesada = await PreprocesarImagen(rutaCompleta);

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

                // 5. Ejecutar OCR con múltiples idiomas y configuraciones mejoradas
                _logger.LogInformation("?? Ejecutando Tesseract OCR...");
                using (var engine = new TesseractEngine(_tessDataPath, cadenaIdiomas, EngineMode.Default))
                {
                    // Configuraciones para mejorar precisión
                    engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,/-:€$£ áéíóúÁÉÍÓÚñÑàèìòùÀÈÌÒÙ");
                    engine.SetVariable("preserve_interword_spaces", "1");
                    
                    using (var img = Pix.LoadFromFile(rutaProcesada))
                    {
                        using (var page = engine.Process(img, PageSegMode.Auto))
                        {
                            resultado.TextoCompleto = page.GetText();
                            resultado.Confianza = page.GetMeanConfidence() * 100;
                            
                            _logger.LogInformation($"? OCR completado con confianza: {resultado.Confianza:F1}%");
                            _logger.LogDebug($"?? Texto extraído:\n{resultado.TextoCompleto}");
                        }
                    }
                }

                // 6. Limpiar imagen procesada temporal
                if (rutaProcesada != rutaCompleta && File.Exists(rutaProcesada))
                {
                    File.Delete(rutaProcesada);
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
        /// Preprocesa la imagen para mejorar la calidad del OCR
        /// </summary>
        private async Task<string> PreprocesarImagen(string rutaOriginal)
        {
            try
            {
                var rutaProcesada = rutaOriginal.Replace(Path.GetExtension(rutaOriginal), "_processed.png");
                
                _logger.LogInformation("?? Preprocesando imagen para mejorar OCR...");
                
                using (var image = await Image.LoadAsync<Rgba32>(rutaOriginal))
                {
                    // Aplicar mejoras a la imagen
                    image.Mutate(x => x
                        .Grayscale()                          // Convertir a escala de grises
                        .GaussianSharpen(1.5f)               // Mejorar nitidez
                        .Contrast(1.2f)                       // Aumentar contraste
                        .BinaryThreshold(0.5f)                // Binarización para mejor contraste
                    );
                    
                    // Escalar si es muy pequeña (mínimo 1000px de ancho)
                    if (image.Width < 1000)
                    {
                        var escala = 1000f / image.Width;
                        image.Mutate(x => x.Resize((int)(image.Width * escala), (int)(image.Height * escala)));
                        _logger.LogDebug($"?? Imagen escalada a: {image.Width}x{image.Height}");
                    }
                    
                    await image.SaveAsPngAsync(rutaProcesada);
                }
                
                _logger.LogInformation("? Imagen preprocesada correctamente");
                return rutaProcesada;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"?? No se pudo preprocesar imagen: {ex.Message}. Usando original.");
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

            var texto = resultado.TextoCompleto;
            var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            
            _logger.LogDebug($"?? Analizando {lineas.Length} líneas de texto");

            // 1. Extraer ESTABLECIMIENTO (primeras líneas significativas)
            ExtraerEstablecimiento(lineas, resultado);

            // 2. Extraer FECHA (patrones mejorados)
            ExtraerFecha(texto, resultado);

            // 3. Extraer MONTO (patrones mejorados con contexto)
            ExtraerMonto(texto, lineas, resultado);

            // 4. Extraer DESCRIPCIÓN
            GenerarDescripcion(lineas, resultado);
        }

        /// <summary>
        /// Extrae el nombre del establecimiento de las primeras líneas
        /// </summary>
        private void ExtraerEstablecimiento(string[] lineas, TransaccionOcrResult resultado)
        {
            // Buscar en las primeras 10 líneas
            var candidatos = new List<string>();
            
            for (int i = 0; i < Math.Min(10, lineas.Length); i++)
            {
                var linea = lineas[i];
                
                // Palabras a ignorar (español e italiano)
                var palabrasIgnorar = new[] { 
                    "ticket", "factura", "boleta", "ruc", "fecha", "hora", "nit", "tel", "phone", "fax",
                    "scontrino", "fattura", "ricevuta", "data", "ora", "telefono", "partita", "iva",
                    "cod", "p.iva", "reg", "via", "piazza", "corso"
                };
                
                var lineaLower = linea.ToLower();
                
                // Filtrar líneas que son solo números, fechas o tienen palabras a ignorar
                if (linea.Length >= 5 
                    && linea.Length <= 60 
                    && !Regex.IsMatch(linea, @"^\d+$")
                    && !Regex.IsMatch(linea, @"\d{2}[/-]\d{2}[/-]\d{2,4}")
                    && !palabrasIgnorar.Any(p => lineaLower.Contains(p))
                    && Regex.IsMatch(linea, @"[a-zA-Z]{3,}")) // Al menos 3 letras consecutivas
                {
                    candidatos.Add(linea);
                }
            }
            
            if (candidatos.Any())
            {
                // Preferir líneas con mayúsculas (típico de nombres de establecimientos)
                var mejorCandidato = candidatos
                    .OrderByDescending(c => c.Count(char.IsUpper))
                    .ThenByDescending(c => c.Length)
                    .First();
                
                resultado.Establecimiento = mejorCandidato;
                _logger.LogDebug($"?? Establecimiento encontrado: {mejorCandidato}");
            }
        }

        /// <summary>
        /// Extrae la fecha del texto con patrones mejorados
        /// </summary>
        private void ExtraerFecha(string texto, TransaccionOcrResult resultado)
        {
            // Patrones de fecha más específicos
            var patronesFecha = new[]
            {
                // DD/MM/YYYY o DD-MM-YYYY (con variantes)
                @"(?:data|fecha|date)[\s:]*(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4})",
                @"\b(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{4})\b",
                @"\b(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2})\b",
                // YYYY-MM-DD
                @"\b(\d{4}[-/\.]\d{1,2}[-/\.]\d{1,2})\b"
            };

            foreach (var patron in patronesFecha)
            {
                var match = Regex.Match(texto, patron, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var fechaStr = match.Groups[1].Value;
                    
                    // Normalizar separadores
                    fechaStr = fechaStr.Replace('.', '/').Replace('-', '/');
                    
                    var formatos = new[] 
                    { 
                        "dd/MM/yyyy", "dd/MM/yy", 
                        "yyyy/MM/dd", 
                        "d/M/yyyy", "d/M/yy" 
                    };
                    
                    foreach (var formato in formatos)
                    {
                        if (DateTime.TryParseExact(fechaStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                        {
                            // Validar que la fecha sea razonable (no futura, no muy antigua)
                            if (fecha <= DateTime.Now && fecha >= DateTime.Now.AddYears(-5))
                            {
                                resultado.Fecha = fecha;
                                _logger.LogDebug($"?? Fecha encontrada: {fechaStr} ? {fecha:yyyy-MM-dd}");
                                return;
                            }
                        }
                    }
                }
            }

            // Si no se encontró fecha válida, usar fecha actual
            resultado.Fecha = DateTime.Now;
            _logger.LogDebug("?? No se encontró fecha en el ticket, usando fecha actual");
        }

        /// <summary>
        /// Extrae el monto del ticket con patrones mejorados
        /// </summary>
        private void ExtraerMonto(string texto, string[] lineas, TransaccionOcrResult resultado)
        {
            var montosEncontrados = new List<(decimal monto, int prioridad, string contexto)>();
            
            // Patrones de monto con contexto (español e italiano)
            var patronesConContexto = new[]
            {
                // Palabras clave de total
                (@"(?:total|totale|importe|importo|pagar|pagare|a\s+pagar|da\s+pagare)[\s:€$]*(\d{1,6}[.,]\d{2})", 10),
                (@"(?:tot|ttl|sum|suma)[\s:€$]*(\d{1,6}[.,]\d{2})", 9),
                (@"(?:neto|netto|subtotal)[\s:€$]*(\d{1,6}[.,]\d{2})", 8),
                // Formato con símbolo de moneda
                (@"[€$£]\s*(\d{1,6}[.,]\d{2})", 7),
                (@"(\d{1,6}[.,]\d{2})\s*[€$£]", 7),
                // Montos al final de línea (típico de totales)
                (@"^\s*(\d{1,6}[.,]\d{2})\s*$", 6),
                // Formato genérico
                (@"\b(\d{1,6}[.,]\d{2})\b", 3)
            };

            foreach (var (patron, prioridad) in patronesConContexto)
            {
                var matches = Regex.Matches(texto, patron, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var montoStr = match.Groups[1].Value.Replace(",", ".");
                    if (decimal.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var monto))
                    {
                        // Filtrar montos muy pequeños o muy grandes (probablemente errores)
                        if (monto >= 0.01m && monto <= 999999.99m)
                        {
                            montosEncontrados.Add((monto, prioridad, match.Value));
                            _logger.LogDebug($"?? Monto candidato: {monto:F2} (prioridad: {prioridad}, contexto: '{match.Value.Trim()}')");
                        }
                    }
                }
            }

            // Seleccionar el monto con mayor prioridad
            if (montosEncontrados.Any())
            {
                var mejorMonto = montosEncontrados
                    .OrderByDescending(m => m.prioridad)
                    .ThenByDescending(m => m.monto) // En caso de empate, el monto mayor suele ser el total
                    .First();
                
                resultado.Monto = mejorMonto.monto;
                _logger.LogDebug($"?? Monto seleccionado: {mejorMonto.monto:F2} (contexto: '{mejorMonto.contexto}')");
            }
            else
            {
                _logger.LogWarning("?? No se pudo extraer el monto del ticket");
            }
        }

        /// <summary>
        /// Genera una descripción basada en el contenido del ticket
        /// </summary>
        private void GenerarDescripcion(string[] lineas, TransaccionOcrResult resultado)
        {
            if (!string.IsNullOrEmpty(resultado.Establecimiento))
            {
                resultado.Descripcion = $"Compra en {resultado.Establecimiento}";
            }
            else
            {
                // Buscar líneas que parezcan productos/servicios
                var productosEncontrados = lineas
                    .Where(l => l.Length > 5 && l.Length < 50)
                    .Where(l => !Regex.IsMatch(l, @"^\d+$"))
                    .Where(l => !Regex.IsMatch(l, @"\d{2}[/-]\d{2}"))
                    .Take(2)
                    .ToList();
                
                if (productosEncontrados.Any())
                {
                    resultado.Descripcion = string.Join(", ", productosEncontrados);
                }
                else
                {
                    resultado.Descripcion = "Compra con ticket";
                }
            }
            
            _logger.LogDebug($"?? Descripción generada: {resultado.Descripcion}");
        }
    }
}
