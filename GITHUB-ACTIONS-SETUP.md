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
?  ??? La aplicación se despliega en:                                ?
?      C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com          ?
???????????????????????????????????????????????????????????????????????
```

---

## ?? **CONFIGURACIÓN DEL SITIO WEB**

### **Ruta de Publicación:**
```
C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com
```

### **URL del Sitio:**
```
http://presupuesto.gestionaminegocio.com
https://presupuesto.gestionaminegocio.com (con SSL)
```

### **Configuración IIS:**
- **Nombre del Sitio:** PresupuestoFamiliarApp
- **App Pool:** PresupuestoFamiliarAppPool
- **Binding:** presupuesto.gestionaminegocio.com (puerto 80/443)

---

## ?? **TROUBLESHOOTING**

### **? ERROR: "The current user doesn't have write access to C:\Program Files\dotnet"**

**Causa:** El runner está intentando instalar .NET pero no tiene permisos.

**Solución:** ? **YA SOLUCIONADO** - El workflow ha sido actualizado para NO instalar .NET porque ya está instalado en tu servidor.

Si vuelves a ver este error, verifica que:
1. .NET 9.0 está instalado en el servidor:
   ```powershell
   dotnet --list-runtimes
   ```
2. El workflow NO incluye el paso `Setup .NET`

---

### **? ERROR: No se puede acceder a la ruta de publicación**

**Causa:** La carpeta de destino no existe o no tiene permisos.

**Solución:**

```powershell
# EN EL SERVIDOR IIS - PowerShell como Administrador

# Crear la carpeta si no existe
New-Item -Path "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com" -ItemType Directory -Force

# Dar permisos al runner
icacls "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com" /grant "Everyone:(OI)(CI)F" /T

# Dar permisos al App Pool
icacls "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com" /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)RX" /T
```

---

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
  -PhysicalPath "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com" `
  -ApplicationPool "PresupuestoFamiliarAppPool" `
  -Port 80 `
  -HostHeader "presupuesto.gestionaminegocio.com"

# Iniciar
Start-WebAppPool -Name "PresupuestoFamiliarAppPool"
Start-Website -Name "PresupuestoFamiliarApp"
```

### **El sitio no responde en el dominio**

**Verificaciones:**

1. **DNS configurado correctamente:**
   ```powershell
   nslookup presupuesto.gestionaminegocio.com
   ```
   Debe apuntar a: `161.132.56.79`

2. **Binding configurado en IIS:**
   ```powershell
   Import-Module WebAdministration
   Get-WebBinding -Name "PresupuestoFamiliarApp"
   ```

3. **Firewall permite puerto 80/443:**
   ```powershell
   Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*World Wide Web*"}
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

## ? **NUEVA FUNCIONALIDAD: Validación de Service Worker**

### **?? Verificación Automática de Versiones**

El workflow ahora **valida automáticamente** si actualizaste la versión del Service Worker cuando haces cambios en archivos estáticos.

**¿Por qué es importante?**
- ? Los usuarios reciben las últimas actualizaciones de CSS/JS
- ? La PWA funciona correctamente con los nuevos cambios
- ? Evita bugs causados por archivos cacheados antiguos

### **Ejemplo de Validación:**

#### **? Si actualizaste la versión:**
```
============================================
   VERIFICACIÓN DE VERSIÓN SERVICE WORKER
============================================

?? CACHE_NAME actual: presupuesto-app-v3
?? RUNTIME_CACHE actual: presupuesto-runtime-v3

?? CACHE_NAME anterior: presupuesto-app-v2
?? RUNTIME_CACHE anterior: presupuesto-runtime-v2

? VERSIÓN ACTUALIZADA CORRECTAMENTE

   ? CACHE_NAME cambió: presupuesto-app-v2 ? presupuesto-app-v3
   ? RUNTIME_CACHE cambió: presupuesto-runtime-v2 ? presupuesto-runtime-v3

?? El Service Worker será actualizado en los clientes

============================================
```

#### **?? Si olvidaste actualizar:**
```
?? ADVERTENCIA: Las versiones del Service Worker NO han cambiado

Si has modificado archivos estáticos (CSS, JS, imágenes),
debes actualizar la versión en service-worker.js:

  const CACHE_NAME = 'presupuesto-app-v3';
  const RUNTIME_CACHE = 'presupuesto-runtime-v3';

? Continuando con el deployment...
```

### **Cómo Actualizar la Versión:**

```javascript
// En: wwwroot/service-worker.js

// ANTES
const CACHE_NAME = 'presupuesto-app-v2';
const RUNTIME_CACHE = 'presupuesto-runtime-v2';

// DESPUÉS (incrementar número)
const CACHE_NAME = 'presupuesto-app-v3';
const RUNTIME_CACHE = 'presupuesto-runtime-v3';
```

**?? Documentación completa:** Ver `SERVICE-WORKER-VERSIONING.md`

---

## ? **CHECKLIST COMPLETO**

### **En el Servidor (161.132.56.79)**
- [ ] ? .NET 9.0 Hosting Bundle instalado
- [ ] ? IIS instalado y funcionando
- [ ] ? Carpeta `C:\actions-runner` creada
- [ ] ? Carpeta `C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com` creada
- [ ] ? GitHub Runner descargado
- [ ] ? Runner configurado con token de GitHub
- [ ] ? Runner instalado como servicio Windows
- [ ] ? Servicio iniciado y corriendo
- [ ] ? Runner visible en GitHub con estado "Idle"
- [ ] ? DNS apunta a 161.132.56.79
- [ ] ? Sitio IIS configurado con binding del dominio

### **En Tu Máquina Local**
- [ ] ? Workflow `.github/workflows/deploy-iis.yml` creado
- [ ] ? Workflow configurado con ruta correcta
- [ ] ? Workflow con validación de Service Worker ? **NUEVO**
- [ ] ? Commit realizado
- [ ] ? Push a GitHub completado

### **Antes de cada Push**
- [ ] ?? ¿Modifiqué CSS, JS o imágenes?
- [ ] ?? ¿Actualicé la versión del Service Worker? ? **IMPORTANTE**

### **En GitHub**
- [ ] ? Workflow visible en la pestaña Actions
- [ ] ? Workflow se ejecuta correctamente
- [ ] ? Validación de Service Worker pasa ? **NUEVO**
- [ ] ? Todos los pasos completan exitosamente

### **Verificación Final**
- [ ] ? Aplicación accesible en `http://presupuesto.gestionaminegocio.com`
- [ ] ? Aplicación accesible en `https://presupuesto.gestionaminegocio.com` (con SSL)
- [ ] ? Login funciona correctamente
- [ ] ? No hay errores en los logs
- [ ] ? PWA instala correctamente
- [ ] ? Service Worker actualizado en clientes ? **NUEVO**

---

## ?? **¡FELICIDADES!**

Ahora tienes configurado **CI/CD completo** con:

? **Compilación automática** cada vez que haces push
? **Tests automáticos** antes del deployment
? **Validación de versión del Service Worker** ? **NUEVO**
? **Deployment automático** a IIS
? **Publicación directa en la carpeta del dominio**
? **Backups automáticos** antes de cada deploy
? **Rollback** posible mediante los backups
? **Logs completos** de cada deployment

---

## ?? **RUTAS Y ARCHIVOS IMPORTANTES**

| Concepto | Ubicación |
|----------|-----------|
| **Runner** | `C:\actions-runner\` |
| **Publicación** | `C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com` |
| **Backups** | `C:\Backups\PresupuestoApp_*` |
| **Logs IIS** | `C:\inetpub\logs\LogFiles\` |
| **Logs App** | `C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com\logs\` |
| **Service Worker** | `wwwroot\service-worker.js` ? |
| **Workflow** | `.github\workflows\deploy-iis.yml` |

---

## ?? **DOCUMENTACIÓN ADICIONAL**

| Documento | Descripción |
|-----------|-------------|
| `GITHUB-ACTIONS-SETUP.md` | Este documento - Guía completa |
| `SERVICE-WORKER-VERSIONING.md` | ? Guía de versiones del Service Worker |
| `DEPLOYMENT-IIS-GUIDE.md` | Guía de deployment manual a IIS |
| `setup-iis-site.ps1` | Script de configuración automática de IIS |
| `verify-runner.ps1` | Script de verificación del runner |

---

## ?? **FLUJO DE TRABAJO COMPLETO**

```bash
# 1. Hacer cambios en código
code Pages/Dashboard.cshtml
code wwwroot/css/site.css  # ? Cambios en CSS

# 2. Actualizar Service Worker (IMPORTANTE si modificaste CSS/JS)
code wwwroot/service-worker.js
# Cambiar: v2 ? v3

# 3. Probar localmente
dotnet run

# 4. Commit
git add .
git commit -m "feat: Mejoras en el dashboard (SW v3)"

# 5. Push
git push origin main

# 6. GitHub Actions automáticamente:
#    ? Valida versión del Service Worker ? NUEVO
#    ? Compila el proyecto
#    ? Ejecuta tests
#    ? Despliega a IIS
#    ? Usuarios reciben la actualización
```

---

**?? ¡Todo automatizado! Solo haz `git push` y GitHub Actions se encarga del resto, incluyendo validar que el Service Worker esté actualizado.**
