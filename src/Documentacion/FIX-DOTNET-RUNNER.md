# ?? Guía de Solución: dotnet no encontrado en el Runner

## ? **Problema**

```
The command could not be loaded, possibly because:
  * No .NET SDKs were found.
```

## ?? **Causa**

El servicio de GitHub Actions Runner **no tiene acceso** al comando `dotnet` porque no está en el PATH del servicio.

---

## ?? **IMPORTANTE: Dónde Ejecutar Cada Comando**

### **??? EN EL SERVIDOR IIS (161.132.56.79)**

**Método 1: Remote Desktop (RDP)**
```powershell
# Desde tu PC, conectarte al servidor
mstsc /v:161.132.56.79
```

**Método 2: PowerShell Remoting**
```powershell
# Desde tu PC, ejecutar comandos remotos
Enter-PSSession -ComputerName 161.132.56.79 -Credential (Get-Credential)
```

---

## ? **Solución - Pasos Detallados**

### **PASO 1: Conectarse al Servidor**

```powershell
# Opción A: RDP
mstsc /v:161.132.56.79

# Opción B: PowerShell Remoting
$cred = Get-Credential
Enter-PSSession -ComputerName 161.132.56.79 -Credential $cred
```

---

### **PASO 2: Ejecutar el Script de Configuración**

**?? EN EL SERVIDOR (no en tu PC)**

```powershell
# 1. Abrir PowerShell como Administrador EN EL SERVIDOR

# 2. Navegar a la carpeta del runner
cd C:\actions-runner

# 3. Si tienes el script configure-runner-dotnet.ps1, ejecutarlo:
# (Si no lo tienes, copia el contenido manualmente)

# 4. Verificar que dotnet está instalado
& "C:\Program Files\dotnet\dotnet.exe" --version

# 5. Agregar dotnet al PATH del sistema
$path = [Environment]::GetEnvironmentVariable("Path", "Machine")
$dotnetPath = "C:\Program Files\dotnet"

if ($path -notlike "*$dotnetPath*") {
    $newPath = "$path;$dotnetPath"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
    Write-Host "Dotnet agregado al PATH"
}

# 6. Reiniciar el servicio del runner
.\svc.stop.cmd
Start-Sleep -Seconds 3
.\svc.start.cmd

# 7. Verificar estado
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

---

### **PASO 3: Verificar la Configuración**

**?? Todavía EN EL SERVIDOR**

```powershell
# Verificar PATH del sistema
[Environment]::GetEnvironmentVariable("Path", "Machine")

# Debe incluir: C:\Program Files\dotnet

# Verificar que el servicio está corriendo
Get-Service | Where-Object {$_.Name -like "*actions*"}

# Estado esperado: Running
```

---

### **PASO 4: Hacer Push desde tu PC Local**

**? AHORA SÍ en tu PC (C:\Users\RRamos\source\repos\PresupuestoFamiliarApp)**

```powershell
# 1. Verificar cambios
git status

# 2. Agregar archivos
git add .github/workflows/deploy-iis.yml
git add wwwroot/service-worker.js

# 3. Commit
git commit -m "fix: Configure dotnet paths for self-hosted runner"

# 4. Push
git push origin main

# 5. Ver el workflow en GitHub
# Ve a: https://github.com/TU_USUARIO/PresupuestoFamiliarApp/actions
```

---

## ?? **Verificación del Workflow**

### **Ver Logs en GitHub Actions:**

1. Ve a tu repositorio en GitHub
2. Click en **Actions**
3. Selecciona el workflow en ejecución
4. Click en **build-and-deploy**
5. Expande el paso **Setup Environment**

**Output esperado:**
```
Setting up environment variables
Verifying dotnet installation
9.0.x
Microsoft.NETCore.App 9.0.x [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
Microsoft.AspNetCore.App 9.0.x [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
```

---

## ?? **Troubleshooting**

### **? Error: "svc.stop.cmd no se reconoce"**

**Causa:** Estás ejecutando el comando en tu PC local, no en el servidor.

**Solución:** 
1. Conéctate al servidor vía RDP o PowerShell Remoting
2. Ejecuta los comandos EN EL SERVIDOR

---

### **? Error: "dotnet no se encuentra"**

**Causa:** .NET no está instalado en el servidor.

**Solución EN EL SERVIDOR:**

```powershell
# 1. Verificar si está instalado
Test-Path "C:\Program Files\dotnet\dotnet.exe"

# Si retorna False, instalar:
# 1. Descargar Hosting Bundle: https://dotnet.microsoft.com/download/dotnet/9.0
# 2. Ejecutar el instalador
# 3. Reiniciar IIS:
net stop was /y
net start w3svc
```

---

### **? Workflow sigue fallando**

**Causa:** El servicio del runner no se reinició correctamente.

**Solución EN EL SERVIDOR:**

```powershell
cd C:\actions-runner

# Desinstalar servicio
.\svc.uninstall.cmd

# Volver a instalar
.\svc.install.cmd

# Iniciar
.\svc.start.cmd

# Verificar
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

---

## ?? **Resumen de Ubicaciones**

| Acción | Ubicación | Comando |
|--------|-----------|---------|
| **Ver error** | GitHub Actions | Ve a Actions ? Logs |
| **Configurar PATH** | ??? Servidor IIS | `$env:PATH = "C:\Program Files\dotnet;$env:PATH"` |
| **Reiniciar runner** | ??? Servidor IIS | `cd C:\actions-runner` ? `.\svc.stop.cmd` ? `.\svc.start.cmd` |
| **Hacer push** | ?? PC Local | `git push origin main` |
| **Ver logs** | GitHub Actions | Actions ? build-and-deploy ? Logs |

---

## ? **Checklist de Verificación**

### **En el Servidor IIS:**
- [ ] ? .NET 9.0 Hosting Bundle instalado
- [ ] ? `C:\Program Files\dotnet\dotnet.exe` existe
- [ ] ? Dotnet está en el PATH del sistema
- [ ] ? Servicio del runner está corriendo
- [ ] ? Runner visible en GitHub con estado "Idle"

### **En tu PC Local:**
- [ ] ? Workflow actualizado con rutas completas
- [ ] ? Commit realizado
- [ ] ? Push a GitHub completado

### **En GitHub:**
- [ ] ? Workflow se ejecuta sin error de "dotnet not found"
- [ ] ? Paso "Setup Environment" muestra versión de dotnet
- [ ] ? Deployment completa exitosamente

---

## ?? **Script Completo de Configuración**

**?? Ejecutar EN EL SERVIDOR IIS como Administrador:**

```powershell
# Script de configuración completa
# Guardar como: C:\actions-runner\configure-dotnet.ps1

Write-Host "Configurando dotnet para GitHub Actions Runner..." -ForegroundColor Cyan

# 1. Verificar instalación
$dotnetExe = "C:\Program Files\dotnet\dotnet.exe"
if (!(Test-Path $dotnetExe)) {
    Write-Host "ERROR: dotnet no encontrado" -ForegroundColor Red
    Write-Host "Instala .NET 9.0 Hosting Bundle" -ForegroundColor Yellow
    exit 1
}

Write-Host "dotnet encontrado: $dotnetExe" -ForegroundColor Green
& $dotnetExe --version

# 2. Agregar al PATH del sistema
$path = [Environment]::GetEnvironmentVariable("Path", "Machine")
$dotnetDir = "C:\Program Files\dotnet"

if ($path -notlike "*$dotnetDir*") {
    Write-Host "Agregando dotnet al PATH del sistema..." -ForegroundColor Yellow
    $newPath = "$path;$dotnetDir"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
    Write-Host "dotnet agregado al PATH" -ForegroundColor Green
} else {
    Write-Host "dotnet ya esta en el PATH" -ForegroundColor Green
}

# 3. Reiniciar servicio del runner
Write-Host "Reiniciando servicio del runner..." -ForegroundColor Yellow
cd C:\actions-runner

.\svc.stop.cmd
Start-Sleep -Seconds 3
.\svc.start.cmd
Start-Sleep -Seconds 3

# 4. Verificar estado
$service = Get-Service | Where-Object {$_.Name -like "*actions*"}
if ($service.Status -eq "Running") {
    Write-Host "Runner configurado correctamente" -ForegroundColor Green
} else {
    Write-Host "Advertencia: El servicio no esta corriendo" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Configuracion completa!" -ForegroundColor Cyan
Write-Host "Ahora puedes hacer push desde tu PC local" -ForegroundColor White
```

---

## ?? **Resumen Ejecutivo**

1. **??? EN EL SERVIDOR:** Ejecuta `configure-dotnet.ps1`
2. **?? EN TU PC:** Haz `git push origin main`
3. **?? EN GITHUB:** Verifica que el workflow se ejecuta correctamente

**?? El problema se soluciona configurando el servidor, NO tu PC local.**

---

**¿Necesitas ayuda para conectarte al servidor? Usa RDP o PowerShell Remoting según tu configuración.**
