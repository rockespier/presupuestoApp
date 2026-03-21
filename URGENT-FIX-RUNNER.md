# ?? SOLUCIÓN URGENTE: Workflow Sigue Fallando

## ? **El Problema Continúa**

El workflow sigue fallando porque **aún no has configurado el servidor correctamente**.

---

## ? **SOLUCIÓN EN 3 PASOS SIMPLES**

### **?? PASO 1: Conectarse al Servidor**

Necesitas acceder físicamente o remotamente al servidor **161.132.56.79**.

**Opción A - Remote Desktop (Más fácil):**
```powershell
# En tu PC local, presiona Win+R y escribe:
mstsc /v:161.132.56.79
```

**Opción B - PowerShell Remoting:**
```powershell
# En tu PC local, abre PowerShell como Administrador:
Enter-PSSession -ComputerName 161.132.56.79 -Credential (Get-Credential)
# Ingresa las credenciales del servidor cuando te las pida
```

**Opción C - Acceso físico:**
- Ve físicamente al servidor y ábrelo

---

### **?? PASO 2: Ejecutar el Script de Diagnóstico**

Una vez que estés **DENTRO DEL SERVIDOR** (no en tu PC):

1. **Abrir PowerShell como Administrador** (en el servidor)
   - Click derecho en el botón de Windows
   - Selecciona "Windows PowerShell (Admin)" o "Terminal (Admin)"

2. **Descargar y ejecutar el script de diagnóstico:**

```powershell
# Copiar este bloque completo y pegarlo en PowerShell del SERVIDOR

# Crear carpeta temporal
New-Item -Path "C:\Temp" -ItemType Directory -Force | Out-Null

# Descargar el script desde tu repositorio O copiarlo manualmente
$scriptContent = @'
# Aqui va el contenido de diagnose-runner.ps1
# Por ahora, ejecuta manualmente:

Write-Host "DIAGNOSTICO MANUAL" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar dotnet
Write-Host "1. Verificando dotnet..." -ForegroundColor Yellow
if (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
    & "C:\Program Files\dotnet\dotnet.exe" --version
    Write-Host "[OK] dotnet encontrado" -ForegroundColor Green
} else {
    Write-Host "[ERROR] dotnet NO encontrado" -ForegroundColor Red
}

# 2. Verificar PATH
Write-Host ""
Write-Host "2. Verificando PATH..." -ForegroundColor Yellow
$path = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($path -like "*C:\Program Files\dotnet*") {
    Write-Host "[OK] dotnet en el PATH" -ForegroundColor Green
} else {
    Write-Host "[ERROR] dotnet NO esta en el PATH" -ForegroundColor Red
    Write-Host "Agregando al PATH..." -ForegroundColor Yellow
    $newPath = "$path;C:\Program Files\dotnet"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
    Write-Host "[OK] Agregado" -ForegroundColor Green
}

# 3. Verificar servicio
Write-Host ""
Write-Host "3. Verificando servicio del runner..." -ForegroundColor Yellow
$service = Get-Service -Name "*actions*" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "[OK] Servicio: $($service.Name)" -ForegroundColor Green
    Write-Host "Estado: $($service.Status)" -ForegroundColor Cyan
} else {
    Write-Host "[ERROR] Servicio NO encontrado" -ForegroundColor Red
}

# 4. Reiniciar servicio
Write-Host ""
Write-Host "4. Reiniciando servicio..." -ForegroundColor Yellow
cd C:\actions-runner
if (Test-Path ".\svc.stop.cmd") {
    cmd /c ".\svc.stop.cmd"
    Start-Sleep -Seconds 3
    cmd /c ".\svc.start.cmd"
    Start-Sleep -Seconds 3
    Write-Host "[OK] Servicio reiniciado" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Archivos del runner NO encontrados" -ForegroundColor Red
}

Write-Host ""
Write-Host "DIAGNOSTICO COMPLETADO" -ForegroundColor Cyan
Write-Host "Ahora intenta hacer push desde tu PC local" -ForegroundColor White
'@

Set-Content -Path "C:\Temp\diagnostico.ps1" -Value $scriptContent
& "C:\Temp\diagnostico.ps1"
```

---

### **?? PASO 3: Hacer Push desde Tu PC**

**IMPORTANTE:** Ahora sí, **desde tu PC local** (NO en el servidor):

```powershell
# En tu PC: C:\Users\RRamos\source\repos\PresupuestoFamiliarApp
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

# Ver cambios
git status

# Agregar TODO
git add .

# Commit
git commit -m "fix: Configure runner environment (SW v6)"

# Push
git push origin main
```

---

## ?? **Verificar que Funciona**

1. Ve a GitHub Actions: `https://github.com/TU_USUARIO/PresupuestoFamiliarApp/actions`
2. Deberías ver el workflow ejecutándose
3. Debería completar exitosamente ?

---

## ?? **SI AÚN FALLA**

### **Alternativa 1: Reinstalar el Runner**

**EN EL SERVIDOR:**

```powershell
cd C:\actions-runner

# Desinstalar
.\svc.stop.cmd
.\svc.uninstall.cmd

# Volver a instalar
.\svc.install.cmd
.\svc.start.cmd

# Verificar
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

### **Alternativa 2: Usar GitHub Hosted Runner (Temporal)**

Si no puedes configurar el servidor ahora, temporalmente cambia el workflow:

```yaml
# En .github/workflows/deploy-iis.yml
jobs:
  build-and-deploy:
    runs-on: windows-latest  # Cambiar de "self-hosted" a "windows-latest"
```

**NOTA:** Esto solo compila y prueba, NO despliega a tu servidor.

---

## ?? **Resumen Visual**

```
TU PC LOCAL                    SERVIDOR IIS
???????????????               ????????????????
?             ?               ?              ?
? 1. Conecta  ???????????????>? RDP/SSH      ?
?    al       ?               ?              ?
?    servidor ?               ? 2. Ejecuta   ?
?             ?               ?    script    ?
?             ?               ?    diagnose  ?
?             ?<???????????????              ?
? 3. Haz push ?               ? Runner OK    ?
?             ?               ?              ?
???????????????               ????????????????
```

---

## ? **SCRIPT DE EMERGENCIA**

Si nada funciona, ejecuta esto **EN EL SERVIDOR**:

```powershell
# SCRIPT DE EMERGENCIA - Ejecutar EN EL SERVIDOR como Admin

Write-Host "CONFIGURACION DE EMERGENCIA" -ForegroundColor Red
Write-Host ""

# 1. Agregar dotnet al PATH
$path = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($path -notlike "*C:\Program Files\dotnet*") {
    [Environment]::SetEnvironmentVariable("Path", "$path;C:\Program Files\dotnet", "Machine")
    Write-Host "[OK] PATH actualizado" -ForegroundColor Green
}

# 2. Reiniciar runner
cd C:\actions-runner
cmd /c ".\svc.stop.cmd"
Start-Sleep -Seconds 5
cmd /c ".\svc.start.cmd"
Start-Sleep -Seconds 5

# 3. Verificar
$service = Get-Service -Name "*actions*"
Write-Host "Estado del servicio: $($service.Status)" -ForegroundColor Cyan

Write-Host ""
Write-Host "LISTO - Intenta push nuevamente" -ForegroundColor Green
```

---

## ?? **Comandos de Verificación**

**EN EL SERVIDOR**, verifica que todo esté bien:

```powershell
# 1. Verificar dotnet
& "C:\Program Files\dotnet\dotnet.exe" --version

# 2. Verificar PATH
[Environment]::GetEnvironmentVariable("Path", "Machine")
# Debe incluir: C:\Program Files\dotnet

# 3. Verificar servicio
Get-Service | Where-Object {$_.Name -like "*actions*"}
# Estado debe ser: Running

# 4. Verificar runner en GitHub
# Ve a: Settings ? Actions ? Runners
# Debe aparecer "Idle" (verde)
```

---

## ?? **LO MÁS IMPORTANTE**

1. **TODOS los comandos con `.\svc.*` se ejecutan EN EL SERVIDOR**
2. **SOLO los comandos `git` se ejecutan en tu PC local**
3. **Si no puedes acceder al servidor, contacta al administrador de sistemas**

---

**¿Tienes acceso al servidor 161.132.56.79? Ese es el primer paso crítico.**
