# ?? Instrucciones para Descargar Tesseract Training Data

## ? Descarga Rápida (Recomendado)

### Opción 1: Descarga Manual con PowerShell

Ejecuta este comando desde la raíz del proyecto:

```powershell
# Descargar archivo de entrenamiento en español
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

### Opción 2: Descarga desde el Navegador

1. Ve a: https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata
2. Guarda el archivo como `spa.traineddata`
3. Colócalo en: `src\tessdata\spa.traineddata`

---

## ?? Archivos de Entrenamiento Adicionales (Opcional)

Si deseas procesar tickets en otros idiomas:

### Inglés (English):
```powershell
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "src\tessdata\eng.traineddata"
```

### Portugués (Portuguese):
```powershell
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/por.traineddata" -OutFile "src\tessdata\por.traineddata"
```

### Francés (French):
```powershell
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata" -OutFile "src\tessdata\fra.traineddata"
```

---

## ? Verificar Instalación

Después de descargar, verifica que el archivo existe:

```powershell
Test-Path "src\tessdata\spa.traineddata"
```

Debería retornar: `True`

Verifica el tamaño del archivo:

```powershell
(Get-Item "src\tessdata\spa.traineddata").Length / 1MB
```

Debería ser aproximadamente: **11-12 MB**

---

## ?? Troubleshooting

### Problema: "No se puede descargar el archivo"

**Solución 1:** Deshabilitar verificación SSL temporalmente (no recomendado en producción)
```powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata" -UseBasicParsing
```

**Solución 2:** Usar curl (si está disponible)
```powershell
curl -L -o "src\tessdata\spa.traineddata" "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata"
```

**Solución 3:** Descarga manual desde navegador

### Problema: "El archivo está corrupto"

**Verificar integridad:**
```powershell
Get-FileHash "src\tessdata\spa.traineddata" -Algorithm SHA256
```

Hash esperado (puede variar con actualizaciones):
```
SHA256: (consultar repositorio oficial)
```

Si el hash no coincide, borra el archivo y descarga nuevamente.

---

## ?? Archivos de Entrenamiento Disponibles

| Idioma | Código | Archivo | Tamaño Aprox. |
|--------|--------|---------|---------------|
| Español | spa | spa.traineddata | 11 MB |
| Inglés | eng | eng.traineddata | 24 MB |
| Portugués | por | por.traineddata | 15 MB |
| Francés | fra | fra.traineddata | 18 MB |
| Alemán | deu | deu.traineddata | 20 MB |
| Italiano | ita | ita.traineddata | 17 MB |

**Repositorio oficial:**
https://github.com/tesseract-ocr/tessdata

---

## ?? Uso en la Aplicación

Una vez descargado el archivo, la aplicación automáticamente:

1. ? Detecta el archivo `spa.traineddata` en `src/tessdata/`
2. ? Inicializa el motor de Tesseract OCR
3. ? Procesa las imágenes compartidas
4. ? Extrae información de tickets en español

**No se requiere configuración adicional.**

---

## ?? Actualización de Archivos

Para actualizar a una versión más reciente:

```powershell
# Respaldar archivo actual
Copy-Item "src\tessdata\spa.traineddata" "src\tessdata\spa.traineddata.backup"

# Descargar nueva versión
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

---

## ?? Tamaño Total de la Carpeta tessdata

Solo español: **~11 MB**

Con múltiples idiomas (esp + eng + por): **~50 MB**

**Recomendación:** Solo descarga los idiomas que realmente necesites.

---

## ?? Verificación Post-Instalación

Ejecuta la aplicación y comprueba los logs:

```
? Carpeta tessdata encontrada
? Archivo spa.traineddata cargado correctamente
?? Motor Tesseract inicializado
```

Si ves estos mensajes, ¡todo está listo! ??

---

## ?? Nota Importante

El archivo `spa.traineddata` **NO debe** incluirse en el control de versiones (Git) debido a su tamaño.

Ya está agregado al `.gitignore`:
```
src/tessdata/*.traineddata
```

Cada desarrollador debe descargarlo localmente.

En producción/servidor, incluir el archivo como parte del proceso de deployment.

---

## ?? Soporte

Si tienes problemas con la descarga o instalación:

1. Revisa los logs de la aplicación
2. Verifica permisos de escritura en `src/tessdata/`
3. Comprueba tu conexión a internet
4. Consulta el README principal del proyecto

---

## ?? Enlaces Útiles

- [Tesseract GitHub](https://github.com/tesseract-ocr/tesseract)
- [Tessdata Repository](https://github.com/tesseract-ocr/tessdata)
- [Tesseract Documentation](https://tesseract-ocr.github.io/)
- [Language Codes](https://tesseract-ocr.github.io/tessdoc/Data-Files-in-different-versions.html)
