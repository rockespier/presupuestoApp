# ?? Share Target + OCR - Documentación de Implementación

## ?? ¿Qué es esta funcionalidad?

**Share Target + OCR** permite a los usuarios **compartir fotos de tickets/facturas** directamente desde otras aplicaciones (Galería, Cámara, WhatsApp, etc.) hacia tu PWA, y automáticamente **extraer información** como:

- ?? **Monto** del ticket
- ?? **Fecha** de la compra
- ?? **Establecimiento** o comercio
- ?? **Descripción** de conceptos

---

## ? Funcionalidades Implementadas

### 1?? **Share Target API**
- Usuarios pueden compartir imágenes desde cualquier app hacia tu PWA
- Compatible con Android (funciona como app nativa)
- Soporte para compartir texto también

### 2?? **OCR (Reconocimiento Óptico de Caracteres)**
- Procesa imágenes de tickets/facturas usando **Tesseract OCR**
- Extrae automáticamente: monto, fecha, establecimiento
- Nivel de confianza del reconocimiento (0-100%)
- Texto completo extraído disponible para revisión

### 3?? **Vista Especial de Revisión**
- Muestra la imagen procesada
- Información extraída editable
- Permite correcciones manuales antes de guardar
- Formulario pre-llenado con datos del OCR

---

## ?? Componentes Creados

### **Backend (.NET)**

#### 1. **OcrService.cs** (`Services/OcrService.cs`)
Servicio principal de procesamiento OCR:
```csharp
- ProcesarTicket(IFormFile imagen) ? TransaccionOcrResult
- ExtraerInformacion(resultado) ? void
```

**Funcionalidades:**
- Guarda la imagen en `wwwroot/uploads/tickets/`
- Ejecuta Tesseract OCR sobre la imagen
- Extrae información estructurada (monto, fecha, establecimiento)
- Retorna objeto con datos procesados y nivel de confianza

#### 2. **TransaccionOcrResult.cs** (`Models/DTOs/TransaccionOcrResult.cs`)
Modelo DTO para resultados OCR:
```csharp
public class TransaccionOcrResult
{
    public decimal? Monto { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Establecimiento { get; set; }
    public string? Descripcion { get; set; }
    public string TextoCompleto { get; set; }
    public float Confianza { get; set; }
    public string? RutaImagen { get; set; }
    public bool ExitosoExtraccion { get; set; }
    public List<string> Mensajes { get; set; }
}
```

#### 3. **TransaccionesController.cs** - Nuevas acciones:

**a) `CreateFromShare()` [POST]**
```csharp
[HttpPost]
public async Task<IActionResult> CreateFromShare(
    string? descripcion, 
    string? nota, 
    string? referencia, 
    IFormFile? imagen)
```
- Endpoint que recibe contenido compartido desde otras apps
- Procesa imágenes con OCR
- Redirige a vista de revisión con datos extraídos

**b) `CreateFromImage()` [GET]**
```csharp
public async Task<IActionResult> CreateFromImage(
    decimal? monto, 
    string? fecha, 
    string? descripcionOcr, 
    ...)
```
- Vista especial para revisar datos extraídos
- Pre-llena formulario con información del OCR
- Permite edición manual antes de guardar

### **Frontend (Razor Pages)**

#### 4. **CreateFromImage.cshtml** (`Views/Transacciones/CreateFromImage.cshtml`)
Vista de dos columnas:

**Columna Izquierda:**
- ??? Imagen del ticket procesada
- ?? Resultados del OCR
- ?? Nivel de confianza
- ?? Texto completo extraído (colapsable)

**Columna Derecha:**
- ?? Formulario editable
- ?? Alerta de revisión
- ?? Botones de acción (Guardar/Volver)

#### 5. **manifest.json** - Share Target actualizado
```json
"share_target": {
  "action": "/Transacciones/CreateFromShare",
  "method": "POST",
  "enctype": "multipart/form-data",
  "params": {
    "title": "descripcion",
    "text": "nota",
    "url": "referencia",
    "files": [
      {
        "name": "imagen",
        "accept": ["image/*"]
      }
    ]
  }
}
```

---

## ?? Configuración Requerida

### 1. **Instalar Tesseract OCR** ? (Ya hecho)
```bash
dotnet add package Tesseract --version 5.2.0
```

### 2. **Descargar Archivos de Entrenamiento**

Descargar `spa.traineddata` (español) desde:
https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata

**Colocar en:**
```
src/tessdata/spa.traineddata
```

> ?? **Importante:** La app buscará este archivo en `tessdata/` dentro de la raíz del proyecto.

### 3. **Crear Carpetas Necesarias**

```bash
# En src/
mkdir tessdata
mkdir wwwroot/uploads
mkdir wwwroot/uploads/tickets
```

Alternativamente, el servicio OCR las creará automáticamente al primer uso.

### 4. **Registrar Servicio en DI** ? (Ya hecho)
```csharp
// Program.cs
builder.Services.AddScoped<OcrService>();
```

---

## ?? Cómo Usar

### **Desde Android (Usuario Final)**

1. **Instalar la PWA** en tu dispositivo Android
2. Abrir cualquier app con imágenes (Galería, Cámara, WhatsApp, etc.)
3. Seleccionar una foto de un ticket
4. Tocar el botón **"Compartir"** ??
5. Elegir **"PresupuestosApp"** en el menú de compartir
6. La imagen se procesará automáticamente
7. Revisar y ajustar los datos extraídos
8. Guardar la transacción

### **Desde Navegador (Testing)**

1. Navegar a `/Transacciones/CreateFromShare`
2. Subir una imagen de prueba
3. La app procesará y mostrará resultados

---

## ?? Testing

### **1. Probar OCR con imagen local**

Crear una acción de prueba temporal:

```csharp
public async Task<IActionResult> TestOcr()
{
    var rutaImagen = Path.Combine(_environment.WebRootPath, "test-ticket.jpg");
    
    using var stream = new FileStream(rutaImagen, FileMode.Open);
    var archivo = new FormFile(stream, 0, stream.Length, "imagen", "test-ticket.jpg");
    
    var resultado = await _ocrService.ProcesarTicket(archivo);
    
    return Json(resultado);
}
```

### **2. Probar Share Target en Chrome Desktop**

1. Abrir DevTools ? Application ? Manifest
2. Verificar que `share_target` esté configurado correctamente
3. Usar **Web Share Target API tester** online

### **3. Probar en Android**

1. Instalar la PWA en Android
2. Tomar foto de un ticket real
3. Compartir a la app
4. Verificar extracción de datos

---

## ?? Precisión del OCR

### **Factores que afectan la precisión:**

? **Mejoran precisión:**
- Imagen con buena iluminación
- Texto claro y legible
- Foto directa (sin ángulo)
- Contraste alto
- Resolución adecuada (no muy baja)

? **Reducen precisión:**
- Imagen borrosa
- Tickets arrugados
- Poca luz
- Ángulo inclinado
- Texto pequeño

### **Niveles de confianza típicos:**

- **90-100%**: Excelente (datos muy confiables)
- **70-89%**: Buena (revisar monto)
- **50-69%**: Regular (revisar todos los datos)
- **0-49%**: Baja (editar manualmente)

---

## ?? Diseño Responsive

### **Desktop (1024px+):**
- Layout de 2 columnas
- Imagen a la izquierda
- Formulario a la derecha

### **Tablet (768px-1023px):**
- 2 columnas ajustadas

### **Mobile (<768px):**
- 1 columna (stacked)
- Imagen arriba
- Formulario abajo

---

## ?? Seguridad

### **Validaciones implementadas:**

1. ? **AntiForgeryToken** en formulario POST
2. ? **Autorización** requerida (`[Authorize]`)
3. ? **Validación de archivo** (solo imágenes)
4. ? **Nombres únicos** (GUID) para evitar sobrescritura
5. ? **Rutas seguras** (sin acceso directo a filesystem)

### **Recomendaciones adicionales:**

- [ ] Limitar tamaño máximo de imagen (ej. 5 MB)
- [ ] Validar tipos MIME permitidos
- [ ] Implementar limpieza automática de imágenes antiguas
- [ ] Agregar rate limiting para prevenir abuso

---

## ?? Troubleshooting

### **Problema:** "No se encontró el archivo de entrenamiento OCR"

**Solución:**
1. Verificar que `tessdata/spa.traineddata` existe
2. Descargar desde: https://github.com/tesseract-ocr/tessdata
3. Colocar en la carpeta correcta

### **Problema:** "OCR no extrae el monto correctamente"

**Soluciones:**
- Ajustar los patrones regex en `ExtraerInformacion()`
- Agregar más patrones de detección
- Mejorar calidad de la imagen

### **Problema:** "Share Target no aparece en Android"

**Verificar:**
1. La PWA está instalada (no solo abierta en navegador)
2. `manifest.json` tiene `share_target` correcto
3. Reiniciar la app después de actualizar manifest

### **Problema:** "Las imágenes no se guardan"

**Soluciones:**
- Verificar permisos de escritura en `wwwroot/uploads/`
- Verificar que la carpeta existe
- Revisar logs de errores en servidor

---

## ?? Métricas de Éxito

### **KPIs a medir:**

- ?? **Tasa de uso:** % de transacciones creadas desde imágenes
- ?? **Tiempo de registro:** Comparar manual vs OCR
- ?? **Precisión:** % de datos correctos sin edición
- ?? **Adopción:** Usuarios que usan la función
- ? **Satisfacción:** Feedback de usuarios

---

## ?? Mejoras Futuras

### **Versión 1.1:**
- [ ] Soporte para múltiples idiomas en OCR
- [ ] Detección de moneda automática
- [ ] OCR en lote (múltiples tickets a la vez)

### **Versión 1.2:**
- [ ] Machine Learning para mejorar extracción
- [ ] Detección de categoría automática
- [ ] Integración con plantillas de establecimientos conocidos

### **Versión 2.0:**
- [ ] OCR directamente en el navegador (WebAssembly)
- [ ] Caché de resultados OCR
- [ ] Historial de tickets procesados

---

## ?? Referencias

- [Share Target API](https://web.dev/web-share-target/)
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract)
- [Traineddata Files](https://github.com/tesseract-ocr/tessdata)
- [Progressive Web Apps](https://web.dev/progressive-web-apps/)

---

## ? Checklist de Implementación

- [x] Instalar paquete Tesseract NuGet
- [x] Crear modelo `TransaccionOcrResult`
- [x] Implementar `OcrService`
- [x] Agregar acciones en controller
- [x] Crear vista `CreateFromImage.cshtml`
- [x] Actualizar `manifest.json` con share_target
- [x] Registrar servicio en DI
- [ ] Descargar `spa.traineddata`
- [ ] Crear carpetas `tessdata/` y `uploads/tickets/`
- [ ] Probar en Chrome Desktop
- [ ] Probar en Android
- [ ] Documentar para usuarios finales

---

## ?? Resultado Final

Tu aplicación ahora permite:

? Compartir fotos desde cualquier app
? Procesamiento automático con OCR
? Extracción de datos del ticket
? Formulario pre-llenado editable
? UX innovadora y profesional

**Siguiente paso:** Descargar el archivo de entrenamiento OCR y probarlo en un dispositivo real.

---

## ?? Soporte

¿Necesitas ayuda?
- Revisa los logs del servidor para errores de OCR
- Verifica la configuración de manifest.json
- Comprueba permisos de carpetas
- Testea con imágenes de alta calidad primero
