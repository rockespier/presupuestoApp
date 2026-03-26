# ? WORKFLOW SIMPLIFICADO - SDK Instalado en Servidor

## ?? **¡Problema Resuelto!**

Ahora que **instalaste el SDK en el servidor**, el workflow es mucho más simple:

- ? **Un solo job** (no dos)
- ? **Compila directamente en el servidor**
- ? **Más rápido** (sin transferencia de archivos)
- ? **Más simple** de mantener

---

## ?? **Comparación: Antes vs Ahora**

### **? Antes (Sin SDK en servidor):**
```yaml
jobs:
  build:  # Job 1 - En GitHub Cloud
    - Checkout
    - Setup .NET
    - Restore
    - Build
    - Publish
    - Upload artifact ??
    
  deploy:  # Job 2 - En tu servidor
    - Download artifact ??
    - Stop IIS
    - Deploy
    - Start IIS
```

### **? Ahora (Con SDK en servidor):**
```yaml
jobs:
  build-and-deploy:  # Un solo job - En tu servidor
    - Checkout
    - Verify dotnet
    - Restore
    - Build
    - Publish
    - Stop IIS
    - Deploy
    - Start IIS
```

---

## ?? **Ventajas del Workflow Simplificado**

| Ventaja | Descripción |
|---------|-------------|
| **? Más Rápido** | No hay transferencia de artifacts |
| **?? Más Simple** | Un solo job, más fácil de entender |
| **?? Menos Almacenamiento** | No se suben/descargan artifacts |
| **?? Más Control** | Todo pasa en tu servidor |
| **?? Logs Más Claros** | Todo en un solo lugar |

---

## ?? **Cómo Funciona Ahora**

### **Paso 1: Verificación del SDK**

```powershell
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
```

Si el SDK no está disponible, el workflow falla inmediatamente con un mensaje claro.

### **Paso 2: Compilación**

```powershell
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
```

Todo se ejecuta directamente en tu servidor.

### **Paso 3: Deployment**

```powershell
Stop-WebAppPool
Copy-Item -Path ./publish/* -Destination C:\inetpub\wwwroot\...
Start-WebAppPool
```

---

## ?? **Workflow Completo**

```
? Checkout code
? Verify dotnet installation
  - SDK version: 9.0.x
  - Installed SDKs: [lista de SDKs]
  - Installed Runtimes: [lista de runtimes]
? Restore dependencies
? Build (Release)
? Run tests (if available)
? Publish
? Stop IIS App Pool
? Backup Previous Deployment
? Deploy to IIS
  - [OK] PresupuestoFamiliarApp.dll
  - [OK] web.config
  - [OK] appsettings.json
? Set Permissions
? Start IIS App Pool
? Verify Deployment
  - Application is responding (HTTP 200 OK)
? Deployment Summary
```

---

## ?? **Requisitos**

Para que este workflow funcione correctamente:

- [x] ? .NET SDK 9.0 instalado en el servidor
- [x] ? SDK en el PATH del sistema
- [x] ? Runner de GitHub instalado y corriendo
- [x] ? IIS configurado
- [x] ? App Pool y sitio creados

---

## ?? **Troubleshooting**

### **Error: "dotnet SDK not found"**

Esto significa que aunque instalaste el SDK, no está en el PATH del servicio del runner.

**Solución:**

```powershell
# EN EL SERVIDOR - PowerShell como Administrador

# 1. Verificar que el SDK está instalado
dotnet --list-sdks

# Si aparece el SDK, entonces solo necesitas reiniciar el runner:

cd C:\actions-runner
.\svc.stop.cmd
Start-Sleep -Seconds 3
.\svc.start.cmd

# Verificar que el servicio está corriendo
Get-Service | Where-Object {$_.Name -like "*actions*"}
```

### **Error en "Restore dependencies"**

**Posibles causas:**
1. Sin conexión a internet (necesita acceso a nuget.org)
2. Firewall bloqueando NuGet
3. Archivo .csproj corrupto

**Verificación:**
```powershell
# En el servidor, prueba manualmente:
cd C:\actions-runner\_work\PresupuestoFamiliarApp\PresupuestoFamiliarApp
dotnet restore
```

### **Workflow más lento que antes**

**Normal si:**
- Primera ejecución (descarga paquetes NuGet)
- Muchas dependencias

**Optimización opcional:**
Agregar caché de NuGet (en el futuro)

---

## ?? **Diferencias con la Versión de 2 Jobs**

| Aspecto | 2 Jobs (GitHub + Servidor) | 1 Job (Solo Servidor) |
|---------|---------------------------|----------------------|
| **Velocidad** | 3-4 minutos | 2-3 minutos ? |
| **Complejidad** | Media | Baja ? |
| **Uso de GitHub Actions** | ~4 minutos | ~2 minutos ?? |
| **Transferencia de datos** | Sí (artifacts) | No ? |
| **Requiere SDK en servidor** | No | Sí |

---

## ?? **Flujo Visual**

```
git push
    ?
GitHub Actions Inicia
    ?
[Tu Servidor - Self-Hosted Runner]
    ?
Checkout code
    ?
Verificar SDK ?
    ?
dotnet restore ?
    ?
dotnet build ?
    ?
dotnet publish ?
    ?
Stop IIS ? Backup ? Deploy ? Start IIS
    ?
? Aplicación desplegada en:
http://presupuesto.gestionaminegocio.com
```

---

## ?? **Hacer Push Ahora**

```powershell
# En tu PC local:
git add .
git commit -m "fix: Simplify workflow to single job with SDK on server (SW v9)"
git push origin main
```

**Resultado esperado:**

```
Running workflow: Deploy PresupuestoFamiliarApp to IIS

Jobs:
  ? build-and-deploy (self-hosted, ~2-3 min)

All jobs completed successfully!
```

---

## ?? **Notas Importantes**

### **1. SDK vs Runtime**

**Ahora tienes instalado:**
- ? .NET SDK 9.0 ? Para `dotnet build`, `dotnet publish`
- ? .NET Runtime 9.0 ? Para ejecutar la aplicación

**Antes solo tenías:**
- ? .NET Runtime 9.0
- ? No tenías SDK

### **2. PATH del Sistema**

El servicio del runner ahora puede acceder a `dotnet` porque:
1. El SDK está instalado
2. El instalador del SDK agregó dotnet al PATH del sistema
3. Reiniciaste el servicio del runner (o reiniciaste el servidor)

### **3. Verificación**

Después de hacer push, verifica en los logs que aparezca:

```
Verify dotnet installation
  SDK version: 9.0.x
  Installed SDKs:
    9.0.x [C:\Program Files\dotnet\sdk]
```

Si aparece esto, todo está correcto ?

---

## ? **Checklist de Verificación**

Antes de hacer push:

- [x] ? SDK instalado en el servidor
- [x] ? Runner reiniciado después de instalar SDK
- [x] ? Workflow simplificado a 1 job
- [x] ? Service Worker actualizado a v9
- [ ] ? Hacer push y verificar

---

## ?? **Resultado Final**

Tu proyecto ahora tiene:

? **Workflow simplificado** (1 job en lugar de 2)
? **Compilación en el servidor** (más rápido)
? **Sin dependencia de GitHub-hosted runners**
? **Deployment automático** completo
? **CI/CD funcional** end-to-end

---

**?? ¡Haz push y disfruta de tu CI/CD automatizado!**
