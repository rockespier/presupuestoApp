# ?? Guía Rápida: Probar Share Target + OCR

## ? Todo está listo. Ahora puedes probar:

---

## ??? **WINDOWS** (Recomendado para Testing)

### Opción 1: Vista de Prueba TestOcr ?

```
1. Ejecutar: dotnet run --project src
2. Navegar: https://localhost:5001/Transacciones/TestOcr
3. Click en "Seleccionar Imagen del Ticket"
4. Elegir una foto de un ticket
5. Click en "Procesar con OCR"
6. Revisar resultados en CreateFromImage
7. Guardar transacción
```

**? FUNCIONA:** Share Target NO está disponible en Windows Desktop, pero esta vista simula toda la funcionalidad.

---

## ?? **iPHONE/iOS**

### Acceso desde tu iPhone:

```
1. En tu PC, obtener IP local:
   ipconfig
   (Ej: 192.168.1.100)

2. En tu PC, ejecutar:
   cd src
   dotnet run --urls="https://0.0.0.0:5001"

3. En tu iPhone (mismo WiFi):
   Safari ? https://192.168.1.100:5001/Transacciones/TestOcr
   
4. Aceptar certificado autofirmado

5. Tocar campo de archivo ? Elegir:
   ?? "Tomar foto" (escanear ticket ahora)
   ??? "Elegir de galería" (foto existente)

6. Tocar "Procesar con OCR"

7. Revisar y guardar
```

**?? LIMITACIÓN:** iOS Safari NO soporta Share Target API. Usa la vista TestOcr en su lugar.

---

## ?? **ANDROID** (Share Target Funcional)

### Opción A: Instalar PWA (Share Target Real)

```
1. Acceder desde Chrome Android
2. Instalar PWA (botón de instalación)
3. Tomar foto de un ticket desde Galería/Cámara
4. Compartir ? "PresupuestosApp"
5. ? Share Target funciona nativamente
```

### Opción B: Vista TestOcr (Como Windows/iPhone)

```
Navega a: /Transacciones/TestOcr
Igual que Windows
```

---

## ?? **ANTES DE PROBAR**

### 1. Descargar Archivo OCR (SI NO LO HICISTE)

```powershell
# Desde la raíz del proyecto
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

**Verificar:**
```powershell
Test-Path "src\tessdata\spa.traineddata"  # True
(Get-Item "src\tessdata\spa.traineddata").Length / 1MB  # ~11 MB
```

### 2. Ejecutar Aplicación

```powershell
cd src
dotnet run
```

O para acceso desde móvil:
```powershell
dotnet run --urls="https://0.0.0.0:5001"
```

---

## ?? **RUTAS DISPONIBLES**

| Ruta | Descripción | Uso |
|------|-------------|-----|
| `/Transacciones/TestOcr` | Vista de prueba | Windows, iPhone, Android |
| `/Transacciones/CreateFromShare` | Share Target endpoint | Solo Android PWA instalada |
| `/Transacciones/CreateFromImage` | Revisar datos OCR | Automático después de OCR |
| `/Transacciones/Create` | Formulario normal | Siempre disponible |

---

## ?? **RESULTADO ESPERADO**

Después de procesar una imagen:

```
? Imagen procesada exitosamente (Confianza: 89.5%)
?? Monto detectado: 45.50
?? Fecha detectada: 26/03/2024
?? Establecimiento: Supermercado Metro
```

Formulario pre-llenado listo para revisar y guardar.

---

## ?? **TROUBLESHOOTING RÁPIDO**

### "No se encontró el archivo de entrenamiento OCR"

```powershell
# Descargar spa.traineddata
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "src\tessdata\spa.traineddata"
```

### "No puedo acceder desde mi iPhone"

```powershell
# 1. Obtener IP
ipconfig

# 2. Ejecutar con bind a todas las interfaces
dotnet run --urls="https://0.0.0.0:5001"

# 3. Permitir en firewall
netsh advfirewall firewall add rule name="ASP.NET Core" dir=in action=allow protocol=TCP localport=5001
```

### "El OCR no extrae datos"

- Verifica calidad de imagen (buena luz, enfoque nítido)
- Prueba con otro ticket
- Revisa logs en consola (emojis: ??, ??, ?, ?)

---

## ?? **DOCUMENTACIÓN COMPLETA**

- **Setup Tesseract:** `src/Documentacion/TESSERACT-SETUP.md`
- **Guía Detallada:** `src/Documentacion/TESTING-OCR-WINDOWS-IPHONE.md`
- **Funcionalidad OCR:** `src/Documentacion/SHARE-TARGET-OCR-README.md`
- **Implementación:** `src/Documentacion/IMPLEMENTACION-SHARE-TARGET-OCR-COMPLETA.md`

---

## ? **CHECKLIST RÁPIDO**

- [ ] `spa.traineddata` descargado
- [ ] Aplicación ejecutándose
- [ ] Acceso a `/Transacciones/TestOcr`
- [ ] Imagen de ticket lista
- [ ] OCR procesando correctamente
- [ ] Datos extraídos visibles
- [ ] Transacción guardada exitosamente

---

## ?? **¡LISTO!**

**Siguiente paso:** Abre tu navegador y ve a:

```
https://localhost:5001/Transacciones/TestOcr
```

Selecciona una imagen de ticket y haz clic en "Procesar con OCR".

**¡Disfruta tu nueva funcionalidad de OCR!** ?????
