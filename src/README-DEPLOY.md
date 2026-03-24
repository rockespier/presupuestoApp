# ?? GUÍA RÁPIDA DE DESPLIEGUE A IIS

## ?? **RESUMEN EJECUTIVO**

Esta guía te permite desplegar **PresupuestoFamiliarApp** en un servidor IIS de producción en **3 pasos simples**.

---

## ? **DESPLIEGUE RÁPIDO (3 PASOS)**

### **1?? Verificar Requisitos**

```powershell
# Abrir PowerShell como Administrador
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp
.\verify-requirements.ps1
```

**Resultado esperado:**
```
? TODOS LOS REQUISITOS CUMPLIDOS
```

---

### **2?? Primera Instalación (Solo una vez)**

```powershell
.\deploy-to-iis.ps1 -FirstTime
```

Este comando:
- ? Publica la aplicación
- ? Crea el Application Pool
- ? Crea el sitio web en IIS
- ? Configura permisos
- ? Inicia el sitio

**Tiempo estimado:** 2-3 minutos

---

### **3?? Despliegues Futuros (Actualizaciones)**

```powershell
.\deploy-to-iis.ps1
```

Este comando:
- ? Publica la aplicación
- ? Detiene el sitio temporalmente
- ? Actualiza archivos
- ? Reinicia el sitio

**Tiempo estimado:** 30-60 segundos

---

## ?? **REQUISITOS DEL SERVIDOR**

| Requisito | Descripción |
|-----------|-------------|
| **Windows** | Server 2016/2019/2022 o Windows 10/11 |
| **IIS** | Internet Information Services instalado |
| **.NET 9.0** | Hosting Bundle instalado |
| **SQL Server** | Express, Standard o Enterprise |
| **RAM** | Mínimo 2 GB (Recomendado 4 GB) |
| **Disco** | Mínimo 1 GB libre |

---

## ?? **INSTALACIÓN DE REQUISITOS**

### **Instalar IIS:**

```powershell
# PowerShell como Administrador
Install-WindowsFeature -name Web-Server -IncludeManagementTools
```

### **Instalar .NET 9.0 Hosting Bundle:**

1. Descargar desde: https://dotnet.microsoft.com/download/dotnet/9.0
2. Buscar: **"Hosting Bundle"** (aprox. 200 MB)
3. Ejecutar el instalador
4. Reiniciar IIS:
   ```powershell
   net stop was /y
   net start w3svc
   ```

---

## ?? **ESTRUCTURA DE ARCHIVOS**

```
PresupuestoFamiliarApp/
??? deploy-to-iis.ps1              ? Script de despliegue
??? verify-requirements.ps1        ? Verificar requisitos
??? web.config                     ? Configuración IIS
??? appsettings.Production.json    ? Config producción
??? DEPLOYMENT-IIS-GUIDE.md        ? Guía completa
??? README-DEPLOY.md               ? Este archivo
```

---

## ?? **COMANDOS PRINCIPALES**

### **Verificar Estado del Sitio:**

```powershell
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"
Get-Website -Name "PresupuestoFamiliarApp"
```

### **Reiniciar Sitio:**

```powershell
Restart-WebAppPool -Name "PresupuestoFamiliarAppPool"
```

### **Ver Logs:**

```powershell
Get-Content "C:\Publish\PresupuestoFamiliarApp\logs\stdout.log" -Tail 20
```

### **Detener/Iniciar Sitio:**

```powershell
# Detener
Stop-WebAppPool -Name "PresupuestoFamiliarAppPool"
Stop-Website -Name "PresupuestoFamiliarApp"

# Iniciar
Start-WebAppPool -Name "PresupuestoFamiliarAppPool"
Start-Website -Name "PresupuestoFamiliarApp"
```

---

## ?? **CONFIGURACIÓN SSL (HTTPS)**

### **Opción 1: Let's Encrypt (Gratuito)**

```powershell
# Instalar win-acme
choco install win-acme

# Ejecutar asistente
wacs.exe
```

### **Opción 2: Certificado Comercial**

1. Comprar certificado SSL
2. Importar en IIS: Server Certificates ? Import
3. Agregar binding HTTPS al sitio

---

## ?? **VERIFICACIÓN POST-DESPLIEGUE**

### **? Checklist:**

- [ ] Sitio accesible en: `http://localhost` o `http://tu-dominio.com`
- [ ] Login funciona correctamente
- [ ] Dashboard carga sin errores
- [ ] PWA se puede instalar
- [ ] Service Worker registrado (F12 ? Application)
- [ ] Base de datos conecta correctamente
- [ ] Emails se envían (si configurado)

### **Probar Acceso:**

```powershell
# Desde PowerShell
Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing

# Resultado esperado: StatusCode 200
```

---

## ?? **SOLUCIÓN DE PROBLEMAS COMUNES**

### **Error 500.19 - Configuration Error**

**Causa:** No está instalado el .NET Hosting Bundle

**Solución:**
```powershell
# Verificar instalación
dotnet --list-runtimes

# Buscar: Microsoft.AspNetCore.App 9.0.x
# Si no aparece, instalar Hosting Bundle
```

### **Error 502.5 - Process Failure**

**Causa:** La aplicación no puede iniciarse

**Solución:**
```powershell
# Ver logs detallados
Get-Content "C:\Publish\PresupuestoFamiliarApp\logs\stdout.log"
```

Posibles causas:
- Connection string incorrecto
- Permisos insuficientes
- DLL faltante

### **Error 403 - Forbidden**

**Causa:** Permisos incorrectos

**Solución:**
```powershell
icacls "C:\Publish\PresupuestoFamiliarApp" /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)RX" /T
```

---

## ?? **DOCUMENTACIÓN ADICIONAL**

Para configuración avanzada, consulta:

| Documento | Descripción |
|-----------|-------------|
| **DEPLOYMENT-IIS-GUIDE.md** | Guía completa paso a paso |
| **PWA-README.md** | Configuración PWA |
| **LOGIN-FIX.md** | Troubleshooting de login |
| **web.config** | Configuración IIS |

---

## ?? **FLUJO DE ACTUALIZACIÓN**

```mermaid
graph LR
    A[Código Actualizado] --> B[Publicar]
    B --> C[Detener Sitio]
    C --> D[Copiar Archivos]
    D --> E[Iniciar Sitio]
    E --> F[Verificar]
```

**Comando único:**
```powershell
.\deploy-to-iis.ps1
```

---

## ?? **SOPORTE**

### **Logs a Revisar:**

```powershell
# Logs de aplicación
Get-Content "C:\Publish\PresupuestoFamiliarApp\logs\stdout.log"

# Logs de IIS
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50

# Event Viewer
Get-EventLog -LogName Application -Newest 20 | Where-Object {$_.Source -like "*ASP.NET*"}
```

### **Información del Sistema:**

```powershell
# Versión de .NET
dotnet --info

# Estado de IIS
Get-Service W3SVC

# Versión de Windows
Get-WmiObject Win32_OperatingSystem | Select Caption, Version
```

---

## ?? **DESPUÉS DEL DESPLIEGUE**

### **Acceso a la Aplicación:**

```
URL Local: http://localhost
URL Red Local: http://[IP-SERVIDOR]
URL Pública: http://tu-dominio.com
```

### **Credenciales Admin por Defecto:**

```
Usuario: admin
Password: admin123
Email: admin@presupuesto.com
```

**?? IMPORTANTE:** Cambia la contraseña después del primer login.

---

## ? **CHECKLIST COMPLETO**

### **Pre-Despliegue:**
- [ ] IIS instalado
- [ ] .NET 9.0 Hosting Bundle instalado
- [ ] SQL Server instalado y corriendo
- [ ] Base de datos creada
- [ ] Usuario de BD creado
- [ ] Connection string actualizado en appsettings.Production.json
- [ ] Certificado SSL (si aplica)

### **Despliegue:**
- [ ] Ejecutar `verify-requirements.ps1`
- [ ] Ejecutar `deploy-to-iis.ps1 -FirstTime`
- [ ] Verificar sitio en navegador
- [ ] Probar login
- [ ] Verificar PWA

### **Post-Despliegue:**
- [ ] Configurar backup automático
- [ ] Configurar monitoreo
- [ ] Documentar credenciales
- [ ] Capacitar usuarios
- [ ] Cambiar password admin

---

## ?? **¡LISTO PARA PRODUCCIÓN!**

Tu aplicación ahora está desplegada en IIS y lista para usar.

**Próximos pasos recomendados:**
1. Configurar HTTPS con certificado SSL
2. Configurar backup automático de BD
3. Configurar monitoreo de uptime
4. Documentar procedimientos de mantenimiento

---

**Para soporte adicional, revisa:** `DEPLOYMENT-IIS-GUIDE.md`
