# ?? Instrucciones para Descargar Tesseract Training Data

## ? Descarga Rápida (Recomendado)

### ???????? Opción 1: Español + Italiano (Multiidioma)

Ejecuta estos comandos desde la raíz del proyecto:

```powershell
# Descargar archivo de entrenamiento en español
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"

# Descargar archivo de entrenamiento en italiano
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "src\tessdata\ita.traineddata"
```

**?? Tip:** El sistema detecta automáticamente ambos idiomas y los usa simultáneamente para mejor precisión.

---

### ???? Opción 2: Solo Español

```powershell
# Descargar archivo de entrenamiento en español
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

---

### ???? Opción 3: Solo Italiano

```powershell
# Descargar archivo de entrenamiento en italiano
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "src\tessdata\ita.traineddata"
```

---

### ?? Opción 4: Descarga desde el Navegador

**Español:**
1. Ve a: https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata
2. Guarda el archivo como `spa.traineddata`
3. Colócalo en: `src\tessdata\spa.traineddata`

**Italiano:**
1. Ve a: https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata
2. Guarda el archivo como `ita.traineddata`
3. Colócalo en: `src\tessdata\ita.traineddata`

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

### Alemán (German):
```powershell
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/deu.traineddata" -OutFile "src\tessdata\deu.traineddata"
```

---

## ? Verificar Instalación

Después de descargar, verifica que los archivos existen:

```powershell
# Verificar español
Test-Path "src\tessdata\spa.traineddata"  # Debe retornar: True

# Verificar italiano
Test-Path "src\tessdata\ita.traineddata"  # Debe retornar: True

# Listar todos los archivos descargados
Get-ChildItem "src\tessdata\" -Filter "*.traineddata"
```

Verifica el tamaño de los archivos:

```powershell
# Español
(Get-Item "src\tessdata\spa.traineddata").Length / 1MB  # ~11 MB

# Italiano
(Get-Item "src\tessdata\ita.traineddata").Length / 1MB  # ~15 MB
```

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
curl -L -o "src\tessdata\ita.traineddata" "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata"
```

**Solución 3:** Descarga manual desde navegador

### Problema: "El archivo está corrupto"

**Verificar integridad:**
```powershell
Get-FileHash "src\tessdata\spa.traineddata" -Algorithm SHA256
Get-FileHash "src\tessdata\ita.traineddata" -Algorithm SHA256
```

Si el hash no coincide con el oficial, borra el archivo y descarga nuevamente.

---

## ?? Archivos de Entrenamiento Disponibles

| Idioma | Código | Archivo | Tamaño Aprox. | Prioridad |
|--------|--------|---------|---------------|-----------|
| ???? Español | spa | spa.traineddata | 11 MB | ? Obligatorio |
| ???? Italiano | ita | ita.traineddata | 15 MB | ? Obligatorio |
| ???? Inglés | eng | eng.traineddata | 24 MB | ?? Opcional |
| ???? Portugués | por | por.traineddata | 15 MB | ?? Opcional |
| ???? Francés | fra | fra.traineddata | 18 MB | ?? Opcional |
| ???? Alemán | deu | deu.traineddata | 20 MB | ?? Opcional |

**Repositorio oficial:**
https://github.com/tesseract-ocr/tessdata

---

## ?? Uso en la Aplicación

Una vez descargados los archivos, la aplicación automáticamente:

1. ? Detecta los archivos disponibles en `src/tessdata/`
2. ? Inicializa el motor de Tesseract OCR
3. ? Usa **todos los idiomas disponibles simultáneamente**
4. ? Extrae información de tickets en español e italiano
5. ? Logs informativos sobre idiomas activos

**Ejemplo de log:**
```
?? Idiomas activos para OCR: spa+ita
? Idioma encontrado: Español (spa.traineddata)
? Idioma encontrado: Italiano (ita.traineddata)
```

**No se requiere configuración adicional.**

---

## ?? Actualización de Archivos

Para actualizar a una versión más reciente:

```powershell
# Respaldar archivos actuales
Copy-Item "src\tessdata\spa.traineddata" "src\tessdata\spa.traineddata.backup"
Copy-Item "src\tessdata\ita.traineddata" "src\tessdata\ita.traineddata.backup"

# Descargar nuevas versiones
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "src\tessdata\ita.traineddata"
```

---

## ?? Tamaño Total de la Carpeta tessdata

| Configuración | Tamaño Total |
|---------------|--------------|
| Solo español | ~11 MB |
| Solo italiano | ~15 MB |
| **Español + Italiano** | **~26 MB** ? |
| Español + Italiano + Inglés | ~50 MB |
| Todos los idiomas (6) | ~120 MB |

**Recomendación:** Descarga español + italiano para cobertura completa.

---

## ?? Verificación Post-Instalación

Ejecuta la aplicación y comprueba los logs:

```
? Carpeta tessdata encontrada
? Idioma encontrado: Español (spa.traineddata)
? Idioma encontrado: Italiano (ita.traineddata)
?? Idiomas activos para OCR: spa+ita
?? Motor Tesseract inicializado
```

Si ves estos mensajes, ¡todo está listo! ??

---

## ?? Nota Importante

Los archivos `.traineddata` **NO deben** incluirse en el control de versiones (Git) debido a su tamaño.

Ya están agregados al `.gitignore`:
```
src/tessdata/*.traineddata
```

Cada desarrollador debe descargarlos localmente.

En producción/servidor, incluir los archivos como parte del proceso de deployment.

---

## ?? Soporte

Si tienes problemas con la descarga o instalación:

1. Revisa los logs de la aplicación (busca emojis: ??, ?, ??)
2. Verifica permisos de escritura en `src/tessdata/`
3. Comprueba tu conexión a internet
4. Consulta `OCR-MULTIIDIOMA.md` para más detalles

---

## ?? Enlaces Útiles

- [Tesseract GitHub](https://github.com/tesseract-ocr/tesseract)
- [Tessdata Repository](https://github.com/tesseract-ocr/tessdata)
- [Tesseract Documentation](https://tesseract-ocr.github.io/)
- [Language Codes](https://tesseract-ocr.github.io/tessdoc/Data-Files-in-different-versions.html)
- [Multi-language OCR Guide](https://tesseract-ocr.github.io/tessdoc/Data-Files.html#multi-language-data)

---

## ?? Listo para Empezar

**Comando rápido para configuración completa:**

```powershell
# Descargar español + italiano
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "src\tessdata\ita.traineddata"

# Verificar instalación
Test-Path "src\tessdata\spa.traineddata"
Test-Path "src\tessdata\ita.traineddata"

# Ejecutar aplicación
cd src
dotnet run
```

**¡Disfruta tu OCR multiidioma!** ???????????
