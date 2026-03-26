namespace PresupuestoFamiliarApp.Models.DTOs
{
    /// <summary>
    /// Resultado del procesamiento OCR de un ticket/factura
    /// </summary>
    public class TransaccionOcrResult
    {
        /// <summary>
        /// Monto extraído del ticket
        /// </summary>
        public decimal? Monto { get; set; }

        /// <summary>
        /// Fecha extraída del ticket
        /// </summary>
        public DateTime? Fecha { get; set; }

        /// <summary>
        /// Establecimiento o comercio extraído
        /// </summary>
        public string? Establecimiento { get; set; }

        /// <summary>
        /// Descripción o conceptos extraídos
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Texto completo extraído del OCR
        /// </summary>
        public string TextoCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Nivel de confianza del OCR (0-100)
        /// </summary>
        public float Confianza { get; set; }

        /// <summary>
        /// Ruta donde se guardó la imagen
        /// </summary>
        public string? RutaImagen { get; set; }

        /// <summary>
        /// Indica si se pudo extraer información útil
        /// </summary>
        public bool ExitosoExtraccion { get; set; }

        /// <summary>
        /// Mensajes de error o advertencia
        /// </summary>
        public List<string> Mensajes { get; set; } = new List<string>();
    }
}
