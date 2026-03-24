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
- [ ] ? Commit realizado
- [ ] ? Push a GitHub completado

### **En GitHub**
- [ ] ? Workflow visible en la pestaña Actions
- [ ] ? Workflow se ejecuta correctamente
- [ ] ? Todos los pasos completan exitosamente

### **Verificación Final**
- [ ] ? Aplicación accesible en `http://presupuesto.gestionaminegocio.com`
- [ ] ? Aplicación accesible en `https://presupuesto.gestionaminegocio.com` (con SSL)
- [ ] ? Login funciona correctamente
- [ ] ? No hay errores en los logs
- [ ] ? PWA instala correctamente

---

## ?? **¡FELICIDADES!**

Ahora tienes configurado **CI/CD completo** con:

? **Compilación automática** cada vez que haces push
? **Tests automáticos** antes del deployment
? **Deployment automático** a IIS
? **Publicación directa en la carpeta del dominio**
? **Backups automáticos** antes de cada deploy
? **Rollback** posible mediante los backups
? **Logs completos** de cada deployment

---

## ?? **RESUMEN RÁPIDO**

| ¿Dónde? | ¿Qué hago? |
|---------|-----------|
| ??? **SERVIDOR** | Instalar runner, IIS, .NET (Pasos 1-5) - **UNA SOLA VEZ** |
| ?? **LOCAL** | El workflow ya está creado y configurado - **YA HECHO** ? |
| ?? **LOCAL** | Desarrollar código normalmente |
| ?? **LOCAL** | `git push` cuando termines |
| ?? **GITHUB** | Automáticamente despliega a tu servidor |
| ?? **WEB** | Accede a `http://presupuesto.gestionaminegocio.com` |

**?? ¡Todo automatizado! Solo haz `git push` y GitHub Actions despliega directamente en tu dominio.**

---

## ?? **FLUJO DE TRABAJO COMPLETO**

```bash
# 1. Hacer cambios en código
code Pages/Dashboard.cshtml
code wwwroot/css/site.css

# 2. Probar localmente
dotnet run

# 3. Commit
git add .
git commit -m "feat: Mejoras en el dashboard"

# 4. Push
git push origin main

# 5. GitHub Actions automáticamente:
#    ? Compila el proyecto
#    ? Ejecuta tests
#    ? Despliega a IIS
#    ? Los usuarios ven los cambios
```

---

## ?? **RUTAS IMPORTANTES**

| Concepto | Ubicación |
|----------|-----------|
| **Runner** | `C:\actions-runner\` |
| **Publicación** | `C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com` |
| **Backups** | `C:\Backups\PresupuestoApp_*` |
| **Logs IIS** | `C:\inetpub\logs\LogFiles\` |
| **Logs App** | `C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com\logs\` |
| **Workflow** | `.github\workflows\deploy-iis.yml` |

---

## ?? **DOCUMENTACIÓN ADICIONAL**

| Documento | Descripción |
|-----------|-------------|
| `GITHUB-ACTIONS-SETUP.md` | Este documento - Guía completa |
| `SERVICE-WORKER-VERSIONING.md` | Guía de versiones del Service Worker (manual) |
| `DEPLOYMENT-IIS-GUIDE.md` | Guía de deployment manual a IIS |
| `setup-iis-site.ps1` | Script de configuración automática de IIS |
| `verify-runner.ps1` | Script de verificación del runner |

---

## ?? **NOTA IMPORTANTE: Service Worker**

El workflow **NO valida automáticamente** la versión del Service Worker. 

**Recuerda actualizar manualmente** la versión cuando modifiques archivos estáticos:

```javascript
// En: wwwroot/service-worker.js
const CACHE_NAME = 'presupuesto-app-v6';  // Incrementar número
const RUNTIME_CACHE = 'presupuesto-runtime-v6';  // Incrementar número
```

**¿Cuándo actualizar?**
- ? Cambios en CSS
- ? Cambios en JavaScript
- ? Cambios en imágenes/iconos
- ? Cambios en HTML/Razor Pages

?? **Guía completa:** Ver `SERVICE-WORKER-VERSIONING.md`

---

**?? ¡Todo listo! Solo haz `git push` y GitHub Actions se encarga del resto.**
