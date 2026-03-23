# ?? GUÍA COMPLETA: Configurar Permisos del Runner en el Servidor

## ?? **PASOS A SEGUIR**

Esta guía te indica **EXACTAMENTE** qué hacer en el servidor para que el runner tenga los permisos necesarios.

---

## ? **PASO 1: Conectarse al Servidor**

**Desde tu PC:**

```powershell
# Remote Desktop
mstsc /v:161.132.56.79

# O PowerShell Remoting
Enter-PSSession -ComputerName 161.132.56.79 -Credential (Get-Credential)
```

---

## ? **PASO 2: Ejecutar el Script de Configuración**

**?? EN EL SERVIDOR - PowerShell como Administrador:**

### **Opción A: Ejecutar el Script Automático**

```powershell
# 1. Navegar a la carpeta del runner
cd C:\actions-runner

# 2. Si tienes el script configure-runner-permissions.ps1:
.\configure-runner-permissions.ps1

# 3. Sigue las instrucciones en pantalla
```

### **Opción B: Configuración Manual**

Si no tienes el script, ejecuta estos comandos:

```powershell
# 1. Identificar el servicio del runner
$runnerService = Get-Service | Where-Object {$_.Name -like "*actions.runner*"}
$serviceName = $runnerService.Name
Write-Host "Servicio: $serviceName"

# 2. Obtener la cuenta del servicio
$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'"
$serviceAccount = $serviceInfo.StartName
Write-Host "Cuenta actual: $serviceAccount"

# 3. OPCIÓN RECOMENDADA: Cambiar a LocalSystem
Write-Host "Deteniendo servicio..."
Stop-Service $serviceName -Force
Start-Sleep -Seconds 3

Write-Host "Cambiando a LocalSystem..."
sc.exe config $serviceName obj= "LocalSystem"

Write-Host "Iniciando servicio..."
Start-Service $serviceName
Start-Sleep -Seconds 3

# 4. Verificar
$status = (Get-Service $serviceName).Status
Write-Host "Estado: $status"

# 5. Configurar permisos en carpeta de publicación
$webPath = "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com"
New-Item -Path $webPath -ItemType Directory -Force | Out-Null

Write-Host "Configurando permisos en: $webPath"
icacls $webPath /grant "LocalSystem:(OI)(CI)F" /T

# 6. Configurar permisos del App Pool
icacls $webPath /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)RX" /T

Write-Host "Configuracion completada!"
```

---

## ?? **¿Qué Hace la Configuración?**

| Acción | Descripción |
|--------|-------------|
| **1. Identificar servicio** | Encuentra el servicio del runner |
| **2. Cambiar a LocalSystem** | La cuenta del sistema tiene permisos completos |
| **3. Reiniciar servicio** | Aplica los cambios |
| **4. Configurar permisos IIS** | Acceso a configuración de IIS |
| **5. Configurar permisos carpeta** | Acceso a carpeta de publicación |
| **6. Configurar App Pool** | Permisos para IIS App Pool Identity |

---

## ?? **Verificación**

Después de ejecutar la configuración, verifica:

### **1. Estado del Servicio:**

```powershell
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

**Output esperado:**
```
Status   Name
------   ----
Running  actions.runner.USUARIO-REPO...
```

### **2. Cuenta del Servicio:**

```powershell
$service = Get-Service | Where-Object {$_.Name -like "*actions*"}
$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='$($service.Name)'"
$serviceInfo.StartName
```

**Output esperado:**
```
LocalSystem
```

### **3. Acceso a IIS:**

```powershell
Import-Module WebAdministration
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"
```

**Si funciona:** ? Los permisos están correctos
**Si falla:** ? Repite la configuración

---

## ?? **PASO 3: Hacer Push desde Tu PC**

Una vez configurado el servidor, **desde tu PC local**:

```powershell
# En: C:\Users\RRamos\source\repos\PresupuestoFamiliarApp
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

git add .
git commit -m "fix: Update workflow with proper IIS permissions (SW v11)"
git push origin main
```

---

## ?? **Resultado Esperado en GitHub Actions**

```
? Checkout code
? Verify dotnet installation
? Restore dependencies
? Build
? Publish
? Stop IIS App Pool
  Current state: Started
  App Pool stopped successfully
? Backup Previous Deployment
? Deploy to IIS
  [OK] PresupuestoFamiliarApp.dll
  [OK] web.config
  [OK] appsettings.json
? Set Permissions
? Start IIS App Pool
  App Pool started successfully
? Verify Deployment
  App Pool State: Started
  Website State: Started
  Application is responding (HTTP 200 OK)
  DEPLOYMENT COMPLETED SUCCESSFULLY!
? Deployment Summary
```

---

## ?? **Troubleshooting**

### **Error: "Access Denied"**

**Causa:** El servicio no tiene permisos suficientes

**Solución:**
```powershell
# Asegúrate de que el servicio se ejecuta como LocalSystem
$service = Get-Service | Where-Object {$_.Name -like "*actions*"}
$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='$($service.Name)'"

if ($serviceInfo.StartName -ne "LocalSystem") {
    Write-Host "Cambiando a LocalSystem..."
    Stop-Service $service.Name -Force
    sc.exe config $service.Name obj= "LocalSystem"
    Start-Service $service.Name
}
```

### **Error: "App Pool not found"**

**Causa:** El App Pool no está creado

**Solución EN EL SERVIDOR:**
```powershell
Import-Module WebAdministration

# Crear App Pool
New-WebAppPool -Name "PresupuestoFamiliarAppPool"
Set-ItemProperty IIS:\AppPools\PresupuestoFamiliarAppPool -Name "managedRuntimeVersion" -Value ""

# Crear sitio
New-Website -Name "PresupuestoFamiliarApp" `
  -PhysicalPath "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com" `
  -ApplicationPool "PresupuestoFamiliarAppPool" `
  -Port 80 `
  -HostHeader "presupuesto.gestionaminegocio.com"

# Iniciar
Start-WebAppPool -Name "PresupuestoFamiliarAppPool"
Start-Website -Name "PresupuestoFamiliarApp"
```

### **El servicio no inicia después de cambiar a LocalSystem**

**Causa:** Posible conflicto con otros servicios

**Solución:**
```powershell
# Ver logs del servicio
Get-EventLog -LogName Application -Newest 50 | Where-Object {$_.Source -like "*actions*"}

# Ver logs del runner
Get-Content "C:\actions-runner\_diag\Runner_*.log" -Tail 50

# Intentar iniciar manualmente
cd C:\actions-runner
.\run.cmd
```

---

## ?? **¿Por Qué LocalSystem?**

| Cuenta | Permisos | Recomendación |
|--------|----------|---------------|
| **LocalSystem** | ? Permisos completos | ? Recomendado para CI/CD |
| **NetworkService** | ?? Limitados | ? Puede dar problemas |
| **Usuario específico** | ?? Depende de config | ?? Requiere configuración adicional |

**LocalSystem** es la cuenta del sistema operativo y tiene:
- ? Acceso completo a IIS
- ? Acceso al sistema de archivos
- ? Sin problemas de contraseñas
- ? No requiere configuración adicional

---

## ?? **Checklist de Configuración**

### **En el Servidor:**
- [ ] ? Conectado al servidor
- [ ] ? PowerShell como Administrador abierto
- [ ] ? Script ejecutado o comandos manuales aplicados
- [ ] ? Servicio del runner corriendo como LocalSystem
- [ ] ? Permisos en carpeta de publicación configurados
- [ ] ? Permisos del App Pool configurados
- [ ] ? Servicio reiniciado

### **En tu PC:**
- [ ] ? Workflow actualizado
- [ ] ? Service Worker actualizado a v11
- [ ] ? Commit realizado
- [ ] ? Push a GitHub completado

### **En GitHub:**
- [ ] ? Workflow se ejecuta sin errores de permisos
- [ ] ? Todos los pasos completan exitosamente
- [ ] ? Aplicación desplegada correctamente

---

## ?? **Resultado Final**

Después de esta configuración:

? **El runner tiene permisos completos** para IIS
? **Puede detener/iniciar App Pools** sin problemas
? **Puede leer configuración de IIS** (redirection.config)
? **Puede desplegar archivos** correctamente
? **No habrá más errores de permisos**

---

## ?? **Comandos de Verificación Rápida**

**EN EL SERVIDOR:**

```powershell
# 1. Ver cuenta del servicio
$s = Get-Service | Where-Object {$_.Name -like "*actions*"}
(Get-WmiObject -Class Win32_Service -Filter "Name='$($s.Name)'").StartName

# 2. Ver estado del servicio
Get-Service | Where-Object {$_.Name -like "*actions*"}

# 3. Probar acceso a IIS
Import-Module WebAdministration
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"

# 4. Ver runner en GitHub
# Ve a: Settings ? Actions ? Runners
# Debe aparecer "Idle" en verde
```

---

## ?? **Resumen en 3 Pasos**

1. **??? EN EL SERVIDOR:** Ejecuta `configure-runner-permissions.ps1`
   - O manualmente: Cambia servicio a LocalSystem
2. **?? EN TU PC:** Haz `git push origin main`
3. **?? EN GITHUB:** Verifica que el workflow completa exitosamente

---

**?? Una vez configurado, el CI/CD funcionará perfectamente sin errores de permisos!**
