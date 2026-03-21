# ?? Guía Completa: GitHub Actions + Self-Hosted Runner + IIS

## ?? **UBICACIONES - ¿DÓNDE HACER CADA PASO?**

Esta guía te indica **EXACTAMENTE** dónde ejecutar cada comando.

---

## ??? **MAPA DE UBICACIONES**

```
???????????????????????????????????????????????????????????????????????
?  ?? TU MÁQUINA LOCAL                                                ?
?  Ubicación: C:\Users\RRamos\source\repos\PresupuestoFamiliarApp\   ?
?                                                                      ?
?  Aquí creas:                                                        ?
?  ??? .github/workflows/deploy-iis.yml ? YA CREADO                 ?
?  ??? Código de tu aplicación                                       ?
?  ??? Commits y push a GitHub                                       ?
???????????????????????????????????????????????????????????????????????
                                ?
                                ? git push
                                ?
???????????????????????????????????????????????????????????????????????
?  ?? GITHUB (github.com)                                             ?
?                                                                      ?
?  Aquí se almacena:                                                  ?
?  ??? Tu código fuente                                              ?
?  ??? Los workflows                                                 ?
?  ??? Secrets y configuraciones                                     ?
?  ??? GitHub Actions ejecuta los workflows                          ?
???????????????????????????????????????????????????????????????????????
                                ?
                                ? GitHub Actions ejecuta
                                ?
???????????????????????????????????????????????????????????????????????
?  ??? SERVIDOR IIS (161.132.56.79)                                    ?
?                                                                      ?
?  Aquí instalas:                                                     ?
?  ??? Self-hosted runner (C:\actions-runner\)                       ?
?  ??? IIS                                                           ?
?  ??? .NET 9.0 Hosting Bundle                                       ?
?  ??? La aplicación se despliega aquí (C:\Publish\...)             ?
???????????????????????????????????????????????????????????????????????
```

---

## ?? **TABLA DE PASOS DETALLADA**

| Paso | Ubicación | Qué hacer | Herramienta |
|------|-----------|-----------|-------------|
| **1** | ??? **SERVIDOR** | Instalar .NET 9.0 Hosting Bundle | PowerShell Admin |
| **2** | ??? **SERVIDOR** | Crear carpeta `C:\actions-runner` | PowerShell Admin |
| **3** | ??? **SERVIDOR** | Descargar GitHub Runner | PowerShell Admin |
| **4** | ??? **SERVIDOR** | Configurar GitHub Runner | PowerShell Admin |
| **5** | ??? **SERVIDOR** | Instalar como servicio Windows | PowerShell Admin |
| **6** | ?? **LOCAL** | Crear `.github/workflows/deploy-iis.yml` | ? **YA CREADO** |
| **7** | ?? **LOCAL** | Commit y push a GitHub | Git / Visual Studio |
| **8** | ?? **GITHUB** | Verificar que el workflow se ejecuta | Navegador web |
| **9** | ??? **SERVIDOR** | Verificar que el deploy funciona | Navegador web |

---

## ?? **PASO 1-5: CONFIGURAR SERVIDOR (161.132.56.79)**

### **??? Conectarse al Servidor**

```powershell
# Opción A: Remote Desktop (RDP)
mstsc /v:161.132.56.79

# Opción B: PowerShell Remoting
Enter-PSSession -ComputerName 161.132.56.79 -Credential (Get-Credential)
```

### **PASO 1: Instalar .NET 9.0 Hosting Bundle**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador

# Verificar si ya está instalado
dotnet --list-runtimes

# Si no aparece "Microsoft.AspNetCore.App 9.0.x", descargar e instalar
# URL: https://dotnet.microsoft.com/download/dotnet/9.0
# Buscar: "Hosting Bundle" (aproximadamente 200 MB)

# Después de instalar, reiniciar IIS
net stop was /y
net start w3svc

# Verificar instalación
dotnet --list-runtimes
```

**Output esperado:**
```
Microsoft.AspNetCore.App 9.0.x [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
Microsoft.NETCore.App 9.0.x [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
```

---

### **PASO 2: Crear Carpeta para el Runner**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador

# Crear carpeta dedicada
New-Item -Path "C:\actions-runner" -ItemType Directory -Force
cd C:\actions-runner

# Verificar
Get-Location
```

**Output esperado:**
```
Path
----
C:\actions-runner
```

---

### **PASO 3: Descargar GitHub Runner**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador
cd C:\actions-runner

# Descargar última versión del runner
$url = "https://github.com/actions/runner/releases/download/v2.320.0/actions-runner-win-x64-2.320.0.zip"
Invoke-WebRequest -Uri $url -OutFile "actions-runner.zip"

# Extraer archivos
Expand-Archive -Path "actions-runner.zip" -DestinationPath . -Force

# Verificar archivos extraídos
Get-ChildItem
```

**Output esperado:**
```
bin/
config.cmd
run.cmd
...
```

---

### **PASO 4: Configurar el Runner**

**?? IMPORTANTE:** Necesitas obtener el token de GitHub primero.

#### **4.1 Obtener Token de GitHub (En tu navegador web)**

1. Abre tu navegador
2. Ve a: `https://github.com/TU_USUARIO/PresupuestoFamiliarApp`
3. Click en **Settings** (del repositorio)
4. En el menú lateral: **Actions** ? **Runners**
5. Click en **New self-hosted runner**
6. Selecciona: **Windows** + **x64**
7. **Copia el token** que aparece (empieza con `A...`)

#### **4.2 Ejecutar Configuración (En el servidor)**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador
cd C:\actions-runner

# Ejecutar configurador
.\config.cmd
```

**Respuestas a las preguntas:**

```plaintext
Enter the name of the runner group: 
> [PRESIONAR ENTER]

Enter the name of runner:
> PresupuestoFamiliarApp-Runner

Enter any additional labels (comma separated):
> windows,iis,production

Enter name of work folder:
> [PRESIONAR ENTER]

Would you like to replace existing runner registration? (Y/N)
> Y (si ya existe)
```

**Cuando pida el token:**
```plaintext
Enter your runner registration token:
> [PEGAR EL TOKEN DE GITHUB]
```

**Output esperado:**
```
? Runner successfully added
? Runner connection is good
```

---

### **PASO 5: Instalar como Servicio Windows**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador
cd C:\actions-runner

# Instalar como servicio
.\svc.install.cmd

# Configurar para iniciar automáticamente
sc config "actions.runner.TU_USUARIO-PresupuestoFamiliarApp.PresupuestoFamiliarApp-Runner" start=auto

# Iniciar el servicio
.\svc.start.cmd

# Verificar estado
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

**Output esperado:**
```
Status   Name
------   ----
Running  actions.runner.TU_USUARIO-PresupuestoFamiliarApp.PresupuestoFamiliarApp-Runner
```

#### **Verificar en GitHub**

1. Ve a: `https://github.com/TU_USUARIO/PresupuestoFamiliarApp/settings/actions/runners`
2. Deberías ver tu runner con **estado verde (Idle)**:

```
? PresupuestoFamiliarApp-Runner
  Idle
  windows, iis, production
  Self-hosted · Windows · X64
```

---

## ?? **PASO 6-7: CONFIGURAR EN TU MÁQUINA LOCAL**

### **PASO 6: Verificar Workflow (Ya está creado)**

```powershell
# EN TU MÁQUINA LOCAL
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

# Verificar que el archivo existe
Test-Path ".github\workflows\deploy-iis.yml"
```

**Output esperado:**
```
True
```

? **Este archivo YA ESTÁ CREADO** - No necesitas hacer nada más.

---

### **PASO 7: Hacer Commit y Push**

#### **Opción A: Usando Git Bash / PowerShell**

```bash
# EN TU MÁQUINA LOCAL - Git Bash o PowerShell
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

# Verificar estado
git status

# Agregar archivos (si hay cambios)
git add .github/workflows/deploy-iis.yml
git add .

# Hacer commit
git commit -m "Add GitHub Actions workflow for automated IIS deployment"

# Push a GitHub
git push origin main
```

#### **Opción B: Usando Visual Studio**

1. Abre **Visual Studio**
2. En **Solution Explorer**, verás los cambios pendientes
3. Click en **Git Changes** (o `Ctrl + 0, G`)
4. Escribe un mensaje: `"Add GitHub Actions workflow for automated IIS deployment"`
5. Click en **Commit All**
6. Click en **Push**

---

## ?? **PASO 8: VERIFICAR EN GITHUB**

### **Ver la Ejecución del Workflow**

1. Ve a: `https://github.com/TU_USUARIO/PresupuestoFamiliarApp`
2. Click en la pestaña **Actions**
3. Deberías ver el workflow **"Deploy PresupuestoFamiliarApp to IIS"** ejecutándose

**Estados posibles:**

```
?? In Progress  - Se está ejecutando
? Success      - Completado exitosamente
? Failed       - Falló (revisar logs)
```

### **Ver Logs en Tiempo Real**

1. Click en el nombre del workflow en ejecución
2. Click en **build-and-deploy**
3. Verás todos los pasos ejecutándose en tiempo real:

```
?? Checkout code
?? Setup .NET
?? Restore dependencies
??? Build
?? Run tests
?? Publish
?? Stop IIS App Pool
?? Backup Previous Deployment
?? Deploy to IIS
?? Set Permissions
?? Start IIS App Pool
? Verify Deployment
?? Deployment Summary
```

---

## ??? **PASO 9: VERIFICAR EN EL SERVIDOR**

### **Verificar que la Aplicación se Desplegó**

```powershell
# EN EL SERVIDOR IIS - PowerShell
Import-Module WebAdministration

# Verificar App Pool
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"

# Verificar sitio web
Get-Website -Name "PresupuestoFamiliarApp"

# Verificar archivos desplegados
Get-ChildItem "C:\Publish\PresupuestoFamiliarApp" | Select-Object Name, LastWriteTime
```

### **Probar la Aplicación**

```powershell
# EN EL SERVIDOR IIS
# Probar desde el servidor
Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing

# O abrir en navegador
Start-Process "http://localhost"
```

### **Desde Tu Máquina Local**

Abre tu navegador y ve a:
```
http://161.132.56.79
```

Deberías ver la aplicación funcionando.

---

## ?? **FLUJO COMPLETO DE TRABAJO**

### **Desarrollo Normal (Día a día)**

```bash
# 1. EN TU MÁQUINA LOCAL - Hacer cambios en el código
code Pages/Index.cshtml.cs

# 2. Probar localmente
dotnet run

# 3. Commit y push
git add .
git commit -m "Mejora en el dashboard"
git push origin main

# 4. GitHub Actions automáticamente:
#    - Compila el código
#    - Ejecuta tests
#    - Despliega a IIS en tu servidor
#    - Todo automático en 2-3 minutos
```

---

## ?? **PROBAR EL DEPLOYMENT MANUAL**

Si quieres ejecutar el deployment **sin hacer push**, puedes hacerlo manualmente:

### **Desde GitHub**

1. Ve a: **Actions** ? **Deploy PresupuestoFamiliarApp to IIS**
2. Click en **Run workflow**
3. Selecciona el branch: **main**
4. Click en **Run workflow**

GitHub ejecutará el deployment inmediatamente.

---

## ?? **TROUBLESHOOTING**

### **El Runner no aparece en GitHub**

```powershell
# EN EL SERVIDOR IIS
cd C:\actions-runner

# Ver logs del runner
Get-Content "_diag\Runner_*.log" -Tail 50

# Reiniciar el servicio
.\svc.stop.cmd
.\svc.start.cmd

# Verificar conectividad con GitHub
Test-NetConnection github.com -Port 443
```

### **El Workflow falla en "Stop IIS App Pool"**

Significa que IIS no está configurado todavía. Ejecuta primero:

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador
Import-Module WebAdministration

# Crear App Pool
New-WebAppPool -Name "PresupuestoFamiliarAppPool"
Set-ItemProperty IIS:\AppPools\PresupuestoFamiliarAppPool -Name "managedRuntimeVersion" -Value ""

# Crear sitio web
New-Website -Name "PresupuestoFamiliarApp" `
  -PhysicalPath "C:\Publish\PresupuestoFamiliarApp" `
  -ApplicationPool "PresupuestoFamiliarAppPool" `
  -Port 80

# Iniciar
Start-WebAppPool -Name "PresupuestoFamiliarAppPool"
Start-Website -Name "PresupuestoFamiliarApp"
```

### **El Workflow no se ejecuta automáticamente**

Verifica en `.github/workflows/deploy-iis.yml`:

```yaml
on:
  push:
    branches: [ main, master ]  # Asegúrate que coincida con tu branch
```

Si tu branch principal se llama `master` en lugar de `main`, ajusta la configuración.

---

## ?? **MONITOREO**

### **Ver Logs del Runner**

```powershell
# EN EL SERVIDOR IIS
cd C:\actions-runner

# Logs en tiempo real
Get-Content "_diag\Runner_*.log" -Wait

# Logs del worker
Get-Content "_diag\Worker_*.log" -Tail 50
```

### **Ver Logs de IIS**

```powershell
# EN EL SERVIDOR IIS
# Logs de IIS
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50

# Logs de la aplicación
Get-Content "C:\Publish\PresupuestoFamiliarApp\logs\stdout.log" -Tail 50
```

---

## ? **CHECKLIST COMPLETO**

### **En el Servidor (161.132.56.79)**
- [ ] ? .NET 9.0 Hosting Bundle instalado
- [ ] ? IIS instalado y funcionando
- [ ] ? Carpeta `C:\actions-runner` creada
- [ ] ? GitHub Runner descargado
- [ ] ? Runner configurado con token de GitHub
- [ ] ? Runner instalado como servicio Windows
- [ ] ? Servicio iniciado y corriendo
- [ ] ? Runner visible en GitHub con estado "Idle"

### **En Tu Máquina Local**
- [ ] ? Workflow `.github/workflows/deploy-iis.yml` creado
- [ ] ? Commit realizado
- [ ] ? Push a GitHub completado

### **En GitHub**
- [ ] ? Workflow visible en la pestaña Actions
- [ ] ? Workflow se ejecuta correctamente
- [ ] ? Todos los pasos completan exitosamente

### **Verificación Final**
- [ ] ? Aplicación accesible en `http://161.132.56.79`
- [ ] ? Login funciona correctamente
- [ ] ? No hay errores en los logs

---

## ?? **¡FELICIDADES!**

Ahora tienes configurado **CI/CD completo** con:

? **Compilación automática** cada vez que haces push
? **Tests automáticos** antes del deployment
? **Deployment automático** a IIS
? **Backups automáticos** antes de cada deploy
? **Rollback** posible mediante los backups
? **Logs completos** de cada deployment

---

## ?? **RESUMEN RÁPIDO**

| ¿Dónde? | ¿Qué hago? |
|---------|-----------|
| ??? **SERVIDOR** | Instalar runner, IIS, .NET (Pasos 1-5) - **UNA SOLA VEZ** |
| ?? **LOCAL** | El workflow ya está creado - **YA HECHO** |
| ?? **LOCAL** | Desarrollar código normalmente |
| ?? **LOCAL** | `git push` cuando termines |
| ?? **GITHUB** | Automáticamente despliega a tu servidor |

**?? ¡Todo automatizado! Solo haz `git push` y GitHub Actions hace el resto.**
