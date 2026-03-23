# ? CONFIGURACIÓN ACTUALIZADA - Nombres Correctos de IIS

## ?? **Cambios Realizados**

He actualizado **TODOS los archivos** con los nombres correctos de tu configuración IIS:

### **? Nombres Actualizados:**

| Concepto | ? Nombre Anterior | ? Nombre Correcto |
|----------|-------------------|-------------------|
| **App Pool** | PresupuestoFamiliarAppPool | **presupuesto.gestionaminegocio.com** |
| **Sitio Web** | PresupuestoFamiliarApp | **presupuesto.gestionaminegocio.com** |
| **Ruta Física** | (sin cambio) | C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com |

---

## ?? **Archivos Actualizados**

| Archivo | Cambios |
|---------|---------|
| `.github/workflows/deploy-iis.yml` | ? IIS_APP_POOL y IIS_SITE_NAME actualizados |
| `setup-iis-complete.ps1` | ? Variables $appPoolName y $siteName actualizadas |
| `configure-runner-permissions.ps1` | ? Variable $appPoolName actualizada |
| `wwwroot/service-worker.js` | ? Versión actualizada a v12 |

---

## ?? **Próximos Pasos**

### **PASO 1: Ejecutar Script de Configuración IIS (EN EL SERVIDOR)**

```powershell
# EN EL SERVIDOR - PowerShell como Administrador

# Si el sitio y pool ya existen con estos nombres, saltea esto
# Si no, ejecuta:
cd C:\tools  # O donde hayas guardado el script
.\setup-iis-complete.ps1
```

**El script:**
- ? Verifica si el App Pool "presupuesto.gestionaminegocio.com" existe
- ? Verifica si el sitio "presupuesto.gestionaminegocio.com" existe
- ? Configura permisos correctamente
- ? Crea carpetas necesarias

---

### **PASO 2: Configurar Permisos del Runner (EN EL SERVIDOR)**

```powershell
# EN EL SERVIDOR - PowerShell como Administrador
cd C:\tools
.\configure-runner-permissions.ps1

# Cuando te pregunte si quieres cambiar a LocalSystem, responde: S
```

---

### **PASO 3: Hacer Commit y Push (EN TU PC)**

```powershell
# En tu PC: C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

git add .
git commit -m "fix: Update IIS names to match actual configuration (SW v12)"
git push origin main
```

---

## ?? **Verificar Configuración Actual en el Servidor**

**Antes de ejecutar los scripts, verifica qué tienes actualmente:**

```powershell
# EN EL SERVIDOR - PowerShell
Import-Module WebAdministration

# Ver todos los App Pools
Get-WebAppPool | Select-Object Name, State

# Ver todos los sitios
Get-Website | Select-Object Name, State, PhysicalPath

# Ver bindings del sitio
Get-WebBinding -Name "presupuesto.gestionaminegocio.com"
```

**Output esperado:**
```
App Pools:
Name: presupuesto.gestionaminegocio.com
State: Started

Sitios:
Name: presupuesto.gestionaminegocio.com
State: Started
PhysicalPath: C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com

Bindings:
protocol: http
bindingInformation: *:80:presupuesto.gestionaminegocio.com
```

---

## ? **Si el Sitio YA Existe**

Si ya tienes el sitio y pool configurados con estos nombres, **solo necesitas**:

### **1. Configurar Permisos del Runner:**

```powershell
# EN EL SERVIDOR
cd C:\tools
.\configure-runner-permissions.ps1
```

### **2. Hacer Push:**

```powershell
# EN TU PC
git add .
git commit -m "fix: Update workflow with correct IIS names (SW v12)"
git push origin main
```

---

## ?? **Configuración Final del Workflow**

El workflow ahora usa:

```yaml
env:
  IIS_SITE_NAME: 'presupuesto.gestionaminegocio.com'
  IIS_APP_POOL: 'presupuesto.gestionaminegocio.com'
  IIS_PHYSICAL_PATH: 'C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com'
```

**Esto coincide exactamente con tu configuración IIS actual.** ?

---

## ?? **Si Tienes Nombres Diferentes**

Si tu configuración actual usa nombres diferentes, hay 2 opciones:

### **Opción A: Cambiar IIS para que coincida**

```powershell
# EN EL SERVIDOR
Import-Module WebAdministration

# Detener sitio antiguo
Stop-Website -Name "NOMBRE_ANTIGUO"
Stop-WebAppPool -Name "NOMBRE_ANTIGUO"

# Renombrar (o crear nuevo con nombre correcto)
# ... ejecutar setup-iis-complete.ps1
```

### **Opción B: Cambiar el Workflow para que coincida**

Edita `.github/workflows/deploy-iis.yml`:

```yaml
env:
  IIS_SITE_NAME: 'TU_NOMBRE_ACTUAL'
  IIS_APP_POOL: 'TU_NOMBRE_ACTUAL'
```

---

## ?? **Resultado Esperado del Workflow**

Después de hacer push, deberías ver en GitHub Actions:

```
? Checkout code
? Verify dotnet installation
? Restore dependencies
? Build
? Publish
? Stop IIS App Pool
  App Pool: presupuesto.gestionaminegocio.com
  Current state: Started
  App Pool stopped successfully ?
? Backup Previous Deployment
? Deploy to IIS
  [OK] PresupuestoFamiliarApp.dll
  [OK] web.config
  [OK] appsettings.json
? Set Permissions
  Read/Execute permissions set for App Pool ?
? Start IIS App Pool
  App Pool: presupuesto.gestionaminegocio.com
  App Pool started successfully ?
? Verify Deployment
  App Pool State: Started
  Website State: Started
  Application is responding (HTTP 200 OK)
  DEPLOYMENT COMPLETED SUCCESSFULLY!
```

---

## ?? **Comandos de Verificación Rápida**

### **En el Servidor:**

```powershell
# Verificar App Pool
Get-WebAppPoolState -Name "presupuesto.gestionaminegocio.com"

# Verificar Sitio
Get-Website -Name "presupuesto.gestionaminegocio.com"

# Ver permisos
icacls "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com"

# Probar sitio
Invoke-WebRequest -Uri "http://presupuesto.gestionaminegocio.com" -UseBasicParsing
```

---

## ? **Checklist Final**

### **En el Servidor:**
- [ ] Sitio "presupuesto.gestionaminegocio.com" existe y está iniciado
- [ ] App Pool "presupuesto.gestionaminegocio.com" existe y está iniciado
- [ ] Carpeta C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com existe
- [ ] Runner configurado con permisos (LocalSystem)
- [ ] Runner está corriendo

### **En tu PC:**
- [ ] Todos los archivos actualizados
- [ ] Service Worker actualizado a v12
- [ ] Commit realizado
- [ ] Push a GitHub completado

### **En GitHub:**
- [ ] Workflow se ejecuta sin errores
- [ ] Todos los pasos completan exitosamente
- [ ] Aplicación desplegada correctamente

---

## ?? **¡TODO LISTO!**

Una vez que ejecutes los scripts en el servidor y hagas push, tu CI/CD estará completamente funcional con:

? **Nombres correctos** de IIS (presupuesto.gestionaminegocio.com)
? **Permisos configurados** correctamente
? **Deployment automático** funcionando
? **Backup automático** antes de cada deploy
? **Verificación automática** después del deploy

---

**?? Ejecuta los scripts en el servidor y luego haz push!**
