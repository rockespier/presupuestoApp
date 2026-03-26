# ?? Guía de Testing: Share Target + OCR en Windows e iPhone

## ?? Índice

1. [Testing en Windows](#testing-en-windows)
2. [Testing en iPhone/iOS](#testing-en-iphone-ios)
3. [Testing Alternativo: Vista TestOcr](#testing-alternativo-vista-testocr)
4. [Preparación Previa](#preparación-previa)

---

## ??? Testing en Windows

### ?? Limitación Importante

**Windows Desktop NO soporta Share Target API** directamente porque:
- Share Target requiere que la PWA esté **instalada**
- Chrome/Edge en Windows solo permite compartir **DESDE** tu PWA (Web Share API)
- NO permite compartir **HACIA** tu PWA (Share Target API)

### ? Alternativas para Testing en Windows

#### **Opción 1: Usar la Vista de Prueba `/Transacciones/TestOcr`** ? (RECOMENDADO)

Ya tienes creada la vista `TestOcr.cshtml`. Vamos a mejorarla:

1. **Asegúrate de tener las acciones en el Controller:**

```csharp
// GET: Vista de prueba
[HttpGet]
public IActionResult TestOcr()
{
    return View();
}

// POST: Procesar imagen de prueba
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TestOcr(IFormFile imagen)
{
    if (imagen == null || imagen.Length == 0)
    {
        TempData["Error"] = "Por favor selecciona una imagen";
        return View();
    }

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
```

2. **Accede a:** `https://localhost:5001/Transacciones/TestOcr`

3. **Selecciona una imagen** de tu PC (un ticket escaneado o foto)

4. **Haz clic en "Procesar"**

5. **Verás la vista `CreateFromImage`** con los datos extraídos

#### **Opción 2: Usar DevTools para Simular**

1. Abre Chrome DevTools (F12)
2. Ve a: **Application ? Manifest**
3. Verifica que `share_target` esté configurado
4. NO hay forma de simular el share desde aquí, pero puedes verificar la configuración

#### **Opción 3: Usar Postman/Thunder Client**

```http
POST https://localhost:5001/Transacciones/CreateFromShare
Content-Type: multipart/form-data

Body (form-data):
- imagen: [Seleccionar archivo de imagen]
- descripcion: "Test desde Postman"
- nota: "Nota de prueba"
```

#### **Opción 4: Usar Android Emulator en Windows**

1. Instala **Android Studio**
2. Crea un **Android Virtual Device (AVD)**
3. Ejecuta el emulador
4. Accede a tu app desde el navegador del emulador
5. Instala la PWA
6. Ahora SÍ podrás usar Share Target

---

## ?? Testing en iPhone/iOS

### ?? Limitación Importante

**iOS Safari NO soporta Share Target API** completamente:
- iOS soporta PWAs pero con limitaciones
- NO soporta: Share Target, Push Notifications, Background Sync
- SÍ soporta: Instalación, Offline, Service Workers (parcial)

### ? Alternativas para Testing en iPhone

#### **Opción 1: Usar Safari con Vista de Prueba** ? (RECOMENDADO)

1. **En tu iPhone, abre Safari**
2. **Navega a:** `https://[tu-servidor]/Transacciones/TestOcr`
   - Desarrollo: `https://192.168.1.X:5001/Transacciones/TestOcr`
   - Producción: `https://tudominio.com/Transacciones/TestOcr`

3. **Toca el campo de archivo** - Se abrirá el selector de fotos
4. **Selecciona:**
   - ?? "Tomar foto" (para escanear un ticket ahora)
   - ??? "Biblioteca de fotos" (para usar una foto existente)

5. **Toca "Procesar"**

6. **Verás los resultados del OCR** en la vista `CreateFromImage`

#### **Opción 2: Acceder Directamente a CreateFromShare (Simulación Manual)**

Como iOS no soporta Share Target, simula el proceso manualmente:

1. **Sube tu imagen a algún servicio temporal** (ej: Imgur, Dropbox)
2. **Copia la URL de la imagen**
3. **Navega a:** `https://[tu-servidor]/Transacciones/Create`
4. **Usa el formulario normal** y adjunta la URL

#### **Opción 3: Testing en Simulador de iOS en Mac**

Si tienes una Mac:

1. Abre **Xcode**
2. Ve a: **Xcode ? Open Developer Tool ? Simulator**
3. Elige un dispositivo iOS (ej: iPhone 15 Pro)
4. Abre Safari en el simulador
5. Navega a tu app
6. Usa la vista `TestOcr`

---

## ?? Testing Alternativo: Vista TestOcr

### Mejora la Vista TestOcr para Testing Completo

Actualiza `Views/Transacciones/TestOcr.cshtml`:

```razor
@{
    ViewData["Title"] = "?? Probar OCR";
}

<div class="max-w-2xl mx-auto p-6">
    <div class="bg-white dark:bg-slate-800 rounded-xl shadow-lg border border-slate-200 dark:border-slate-700 p-8">
        <h2 class="text-3xl font-bold text-slate-800 dark:text-white mb-2">?? Probar OCR</h2>
        <p class="text-slate-600 dark:text-slate-400 mb-6">
            Vista de prueba para testear el procesamiento OCR sin necesidad de Share Target API
        </p>

        @if (TempData["Error"] != null)
        {
            <div class="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 rounded-lg p-4 mb-6">
                <p class="text-red-700 dark:text-red-400">?? @TempData["Error"]</p>
            </div>
        }

        <form method="post" enctype="multipart/form-data" class="space-y-6">
            @Html.AntiForgeryToken()

            <!-- Instrucciones -->
            <div class="bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 rounded-lg p-4">
                <h3 class="font-semibold text-blue-900 dark:text-blue-100 mb-2">?? Instrucciones:</h3>
                <ol class="list-decimal list-inside text-sm text-blue-800 dark:text-blue-200 space-y-1">
                    <li>Selecciona una imagen de un ticket o factura</li>
                    <li>Asegúrate de que el texto sea legible</li>
                    <li>Haz clic en "Procesar con OCR"</li>
                    <li>Revisa los datos extraídos</li>
                </ol>
            </div>

            <!-- Selector de Archivo -->
            <div>
                <label class="block text-sm font-semibold text-slate-700 dark:text-slate-300 mb-2">
                    ?? Seleccionar Imagen del Ticket
                </label>
                <input type="file" 
                       name="imagen" 
                       accept="image/*" 
                       capture="environment"
                       class="w-full px-4 py-3 border-2 border-slate-200 dark:border-slate-600 rounded-xl bg-white dark:bg-slate-700 text-slate-800 dark:text-slate-200 focus:border-primary-500 focus:ring-4 focus:ring-primary-100 transition outline-none file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100" 
                       required />
                <p class="text-xs text-slate-500 dark:text-slate-400 mt-2">
                    ?? Formatos aceptados: JPG, PNG, JPEG, WebP
                </p>
            </div>

            <!-- Vista Previa (Opcional) -->
            <div id="preview" class="hidden">
                <label class="block text-sm font-semibold text-slate-700 dark:text-slate-300 mb-2">
                    ??? Vista Previa
                </label>
                <img id="previewImage" 
                     class="w-full h-auto max-h-64 object-contain rounded-lg border-2 border-slate-200 dark:border-slate-600" 
                     alt="Vista previa" />
            </div>

            <!-- Botón de Envío -->
            <button type="submit" 
                    class="w-full py-3.5 bg-gradient-to-r from-primary-500 to-blue-600 text-white font-bold rounded-xl hover:from-primary-600 hover:to-blue-700 transform hover:scale-[1.02] transition shadow-lg hover:shadow-xl flex items-center justify-center gap-2">
                ?? Procesar con OCR
            </button>

            <!-- Link de vuelta -->
            <div class="text-center">
                <a asp-action="Index" 
                   class="text-slate-600 dark:text-slate-400 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition">
                    ? Volver al historial
                </a>
            </div>
        </form>

        <!-- Información Técnica -->
        <div class="mt-8 p-4 bg-slate-50 dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-700">
            <h4 class="font-semibold text-slate-800 dark:text-slate-200 mb-2">?? Información Técnica</h4>
            <ul class="text-xs text-slate-600 dark:text-slate-400 space-y-1">
                <li>? Motor OCR: Tesseract 5.2.0</li>
                <li>? Idioma: Español (spa.traineddata)</li>
                <li>? Endpoint: POST /Transacciones/TestOcr</li>
                <li>? Redirección: /Transacciones/CreateFromImage</li>
            </ul>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // Vista previa de la imagen seleccionada
        document.querySelector('input[type="file"]').addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function(event) {
                    const preview = document.getElementById('preview');
                    const previewImage = document.getElementById('previewImage');
                    previewImage.src = event.target.result;
                    preview.classList.remove('hidden');
                };
                reader.readAsDataURL(file);
            }
        });
    </script>
}
```

---

## ?? Preparación Previa

### 1. Descargar Archivo OCR (SI NO LO HAS HECHO)

```powershell
# Desde la raíz del proyecto
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

Verificar:
```powershell
Test-Path "src\tessdata\spa.traineddata"  # Debe retornar True
(Get-Item "src\tessdata\spa.traineddata").Length / 1MB  # Debe ser ~11 MB
```

### 2. Ejecutar la Aplicación

```powershell
cd src
dotnet run
```

### 3. Verificar que las Acciones Existen

Abre `Controllers/TransaccionesController.cs` y verifica que existen:

```csharp
[HttpGet]
public IActionResult TestOcr() { ... }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TestOcr(IFormFile imagen) { ... }
```

---

## ?? Acceso desde Dispositivos Móviles (iPhone/Android)

### Opción A: Usar tu IP Local

1. **Obtén tu IP local:**
   ```powershell
   ipconfig
   # Busca "Dirección IPv4" (ej: 192.168.1.100)
   ```

2. **Configura HTTPS en `appsettings.json`:**
   ```json
   "Kestrel": {
     "Endpoints": {
       "Https": {
         "Url": "https://0.0.0.0:5001"
       }
     }
   }
   ```

3. **Ejecuta:**
   ```powershell
   dotnet run --urls="https://0.0.0.0:5001"
   ```

4. **Desde tu iPhone/Android:**
   - Conecta al mismo WiFi que tu PC
   - Abre Safari/Chrome
   - Navega a: `https://192.168.1.100:5001/Transacciones/TestOcr`
   - Acepta el certificado autofirmado

### Opción B: Usar ngrok (Túnel Público)

1. **Descarga ngrok:** https://ngrok.com/download

2. **Ejecuta:**
   ```powershell
   ngrok http https://localhost:5001
   ```

3. **Copia la URL pública** (ej: `https://abc123.ngrok.io`)

4. **Desde tu iPhone/Android:**
   - Abre Safari/Chrome
   - Navega a: `https://abc123.ngrok.io/Transacciones/TestOcr`

---

## ?? Flujo de Testing Recomendado

### Para Windows Desktop:

```
1. Ejecutar: dotnet run
2. Navegar: https://localhost:5001/Transacciones/TestOcr
3. Seleccionar imagen de ticket
4. Clic en "Procesar"
5. Revisar datos extraídos en CreateFromImage
6. Ajustar manualmente si es necesario
7. Guardar transacción
```

### Para iPhone:

```
1. Conectar al mismo WiFi que tu PC
2. Obtener IP local: ipconfig
3. Ejecutar: dotnet run --urls="https://0.0.0.0:5001"
4. En Safari: https://192.168.1.X:5001/Transacciones/TestOcr
5. Aceptar certificado
6. Tocar campo de archivo ? Tomar foto o elegir de galería
7. Tocar "Procesar"
8. Revisar resultados
```

---

## ? Checklist de Testing

- [ ] ? `spa.traineddata` descargado
- [ ] ? Aplicación ejecutándose
- [ ] ? Acciones `TestOcr` en controller
- [ ] ? Vista `TestOcr.cshtml` mejorada
- [ ] ? Imagen de prueba lista (ticket/factura)
- [ ] ? Acceso desde navegador local
- [ ] ? (Opcional) Acceso desde dispositivo móvil
- [ ] ? OCR procesando correctamente
- [ ] ? Datos extraídos visibles
- [ ] ? Transacción guardada

---

## ?? Ejemplos de Tickets para Probar

### Tickets Ideales (Alta Precisión):

- ? Tickets de supermercado
- ? Facturas electrónicas impresas
- ? Recibos de restaurantes
- ? Boletas de servicios

### Tickets Difíciles (Baja Precisión):

- ? Tickets arrugados o doblados
- ? Fotos con poca luz
- ? Tickets con tinta desvanecida
- ? Ángulos muy inclinados

---

## ?? Troubleshooting

### Problema: "No se encontró el archivo de entrenamiento OCR"

```powershell
# Verificar si existe
Test-Path "src\tessdata\spa.traineddata"

# Si no existe, descargarlo
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

### Problema: "No puedo acceder desde mi iPhone"

```powershell
# Verificar que el firewall permite conexiones
netsh advfirewall firewall add rule name="ASP.NET Core" dir=in action=allow protocol=TCP localport=5001

# Ejecutar con bind a todas las interfaces
dotnet run --urls="https://0.0.0.0:5001"
```

### Problema: "El OCR no extrae nada"

- Verifica la calidad de la imagen
- Prueba con una imagen más clara
- Revisa los logs de la aplicación
- Verifica que `spa.traineddata` no esté corrupto

---

## ?? Resultado Esperado

Después de procesar una imagen, deberías ver:

```
? Imagen procesada exitosamente (Confianza: 89.5%)
?? Monto detectado: 45.50
?? Fecha detectada: 26/03/2024
?? Establecimiento: Supermercado Metro
```

Y un formulario pre-llenado con estos datos listos para revisar y guardar.

---

## ?? Próximos Pasos

1. ? Probar con varios tipos de tickets
2. ? Ajustar patrones de extracción si es necesario
3. ? Implementar mejoras basadas en resultados
4. ? Documentar tickets problemáticos
5. ? (Opcional) Agregar soporte para más idiomas

---

**¡Listo para probar!** ????
