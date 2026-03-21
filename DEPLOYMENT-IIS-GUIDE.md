# ?? Guía Completa de Despliegue a IIS (Producción)

## ?? **TABLA DE CONTENIDOS**

1. [Requisitos Previos](#requisitos-previos)
2. [Preparación del Proyecto](#preparación-del-proyecto)
3. [Publicación con Visual Studio](#publicación-con-visual-studio)
4. [Publicación con Línea de Comandos](#publicación-con-línea-de-comandos)
5. [Configuración de IIS](#configuración-de-iis)
6. [Configuración de Base de Datos](#configuración-de-base-de-datos)
7. [Configuración SSL/HTTPS](#configuración-ssl-https)
8. [Verificación y Testing](#verificación-y-testing)
9. [Troubleshooting](#troubleshooting)

---

## ?? **REQUISITOS PREVIOS**

### **En el Servidor de Producción:**

1. **Windows Server** (2016, 2019, 2022) o **Windows 10/11**
2. **IIS (Internet Information Services)** instalado
3. **.NET 9.0 Hosting Bundle** instalado
4. **SQL Server** (Express, Standard o Enterprise)
5. **Certificado SSL** (Let's Encrypt, Cloudflare, o comercial)

### **Verificar IIS Instalado:**

```powershell
# PowerShell como Administrador
Get-WindowsFeature -Name Web-Server
```

Si no está instalado:
```powershell
Install-WindowsFeature -name Web-Server -IncludeManagementTools
```

### **Instalar .NET 9.0 Hosting Bundle:**

1. Descarga desde: https://dotnet.microsoft.com/download/dotnet/9.0
2. Busca: **"Hosting Bundle"** (no SDK, no Runtime)
3. Instala: `dotnet-hosting-9.0.x-win.exe`
4. **Reinicia IIS:**
   ```cmd
   net stop was /y
   net start w3svc
   ```

---

## ?? **PREPARACIÓN DEL PROYECTO**

### **1. Actualizar appsettings.json**

Crea `appsettings.Production.json` en la raíz del proyecto:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=PresupuestoFamiliarDB;User Id=TU_USUARIO;Password=TU_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "tu-email@gmail.com",
    "Password": "tu-app-password",
    "FromEmail": "tu-email@gmail.com",
    "FromName": "PresupuestoFamiliar App"
  }
}
```

**?? IMPORTANTE:** Nunca subas este archivo a Git. Agrégalo a `.gitignore`:

```gitignore
appsettings.Production.json
appsettings.*.json
```

### **2. Verificar web.config**

El archivo `web.config` ya está creado en la raíz del proyecto con la configuración óptima.

---

## ??? **PUBLICACIÓN CON VISUAL STUDIO**

### **Método 1: Publicación a Carpeta (Recomendado)**

1. **Click derecho** en el proyecto ? **Publish...**
2. Selecciona: **Folder**
3. Ubicación: `C:\Publish\PresupuestoFamiliarApp`
4. Click en **Show all settings**
5. Configuración:
   ```
   Configuration: Release
   Target Framework: net9.0
   Deployment Mode: Self-contained (opcional) o Framework-dependent
   Target Runtime: win-x64
   File Publish Options:
   ? Delete all existing files prior to publish
   ? Exclude files from App_Data folder
   ```
6. Click en **Save**
7. Click en **Publish**

### **Resultado:**

```
? Publish succeeded.
   Files published to: C:\Publish\PresupuestoFamiliarApp
```

---

## ?? **PUBLICACIÓN CON LÍNEA DE COMANDOS**

### **Comando Básico:**

```bash
dotnet publish -c Release -o C:\Publish\PresupuestoFamiliarApp
```

### **Comando Completo (Recomendado):**

```bash
dotnet publish `
  --configuration Release `
  --output "C:\Publish\PresupuestoFamiliarApp" `
  --runtime win-x64 `
  --self-contained false `
  /p:EnvironmentName=Production `
  /p:PublishSingleFile=false `
  /p:PublishTrimmed=false
```

### **Explicación de Parámetros:**

| Parámetro | Descripción |
|-----------|-------------|
| `-c Release` | Compilación optimizada para producción |
| `-o` | Carpeta de salida |
| `--runtime win-x64` | Para Windows 64-bit |
| `--self-contained false` | Requiere .NET instalado en el servidor (más liviano) |
| `/p:EnvironmentName=Production` | Usa appsettings.Production.json |

### **Self-Contained vs Framework-Dependent:**

**Framework-Dependent (Recomendado):**
```bash
dotnet publish -c Release -o C:\Publish\PresupuestoFamiliarApp --self-contained false
```
- ? Tamaño pequeño (~50-100 MB)
- ? Requiere .NET Hosting Bundle en servidor
- ? Actualizaciones de .NET automáticas

**Self-Contained:**
```bash
dotnet publish -c Release -o C:\Publish\PresupuestoFamiliarApp --self-contained true
```
- ? No requiere .NET en servidor
- ? Tamaño grande (~150-200 MB)
- ? Debes actualizar manualmente

---

## ?? **CONFIGURACIÓN DE IIS**

### **1. Crear Application Pool**

1. Abre **IIS Manager** (inetmgr)
2. Click en **Application Pools** ? **Add Application Pool**
3. Configuración:
   ```
   Name: PresupuestoFamiliarAppPool
   .NET CLR Version: No Managed Code
   Managed Pipeline Mode: Integrated
   ```
4. Click **OK**
5. **Click derecho** en el pool ? **Advanced Settings**
6. Configurar:
   ```
   General
     Start Mode: AlwaysRunning
   
   Process Model
     Identity: ApplicationPoolIdentity
     Idle Time-out (minutes): 0 (para que no se detenga)
   
   Recycling
     Regular Time Interval (minutes): 1740 (29 horas)
   ```

### **2. Crear Sitio Web**

1. Click en **Sites** ? **Add Website**
2. Configuración:
   ```
   Site Name: PresupuestoFamiliarApp
   Application Pool: PresupuestoFamiliarAppPool
   Physical Path: C:\Publish\PresupuestoFamiliarApp
   
   Binding:
     Type: http
     IP Address: All Unassigned
     Port: 80
     Host Name: (dejar vacío o tu dominio)
   ```
3. Click **OK**

### **3. Configurar Permisos**

```powershell
# PowerShell como Administrador
$path = "C:\Publish\PresupuestoFamiliarApp"

# Dar permisos al Application Pool Identity
icacls $path /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)RX" /T

# Dar permisos de escritura a wwwroot (para logs, uploads)
icacls "$path\wwwroot" /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)M" /T

# Crear carpeta de logs
New-Item -Path "$path\logs" -ItemType Directory -Force
icacls "$path\logs" /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)M" /T
```

### **4. Habilitar Características de IIS**

```powershell
# Características necesarias
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationInit
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets
```

---

## ??? **CONFIGURACIÓN DE BASE DE DATOS**

### **1. Crear Base de Datos en SQL Server**

```sql
-- Crear base de datos
CREATE DATABASE PresupuestoFamiliarDB;
GO

-- Crear login para la aplicación
CREATE LOGIN PresupuestoAppUser WITH PASSWORD = 'TuPasswordSeguro123!';
GO

-- Usar la base de datos
USE PresupuestoFamiliarDB;
GO

-- Crear usuario
CREATE USER PresupuestoAppUser FOR LOGIN PresupuestoAppUser;
GO

-- Dar permisos
ALTER ROLE db_owner ADD MEMBER PresupuestoAppUser;
GO
```

### **2. Aplicar Migraciones**

**Opción A - Desde Visual Studio:**

1. Abre **Package Manager Console**
2. Ejecuta:
   ```powershell
   Update-Database
   ```

**Opción B - Desde Línea de Comandos:**

```bash
# Navegar a la carpeta del proyecto
cd C:\Users\RRamos\source\repos\PresupuestoFamiliarApp

# Aplicar migraciones
dotnet ef database update --connection "Server=TU_SERVIDOR;Database=PresupuestoFamiliarDB;User Id=PresupuestoAppUser;Password=TuPasswordSeguro123!;Encrypt=True;TrustServerCertificate=True;"
```

**Opción C - Script SQL (Si no tienes EF Tools):**

1. Genera el script:
   ```bash
   dotnet ef migrations script -o deploy.sql
   ```
2. Ejecuta el script en SQL Server Management Studio

### **3. Crear Usuario Admin Inicial**

```sql
USE PresupuestoFamiliarDB;
GO

-- Insertar espacio por defecto
INSERT INTO Espacios (Nombre, MonedaPrincipal)
VALUES ('Espacio Principal', 0); -- 0 = Soles
GO

-- Insertar usuario admin (contraseña: admin123)
-- Password hash generado con BCrypt
INSERT INTO Usuarios (NombreUsuario, Email, PasswordHash, Rol)
VALUES ('admin', 'admin@presupuesto.com', '$2a$11$qQ9Z7P.Jx7O0L5Q5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5', 'Administrador');
GO

-- Vincular usuario con espacio
DECLARE @UsuarioId INT = (SELECT Id FROM Usuarios WHERE NombreUsuario = 'admin');
DECLARE @EspacioId INT = (SELECT Id FROM Espacios WHERE Nombre = 'Espacio Principal');

INSERT INTO EspacioUsuario (EspaciosId, UsuariosId)
VALUES (@EspacioId, @UsuarioId);
GO
```

**Nota:** Deberás generar el hash de BCrypt correcto para tu contraseña.

---

## ?? **CONFIGURACIÓN SSL/HTTPS**

### **Opción 1: Certificado Let's Encrypt (Gratuito)**

1. Instala **win-acme**:
   ```powershell
   # Descargar desde: https://www.win-acme.com/
   # O usar Chocolatey
   choco install win-acme
   ```

2. Ejecuta win-acme:
   ```cmd
   wacs.exe
   ```

3. Sigue el asistente:
   - Selecciona tu sitio IIS
   - Ingresa tu dominio
   - Acepta términos
   - El certificado se instalará automáticamente

### **Opción 2: Certificado Comercial**

1. Genera CSR (Certificate Signing Request)
2. Compra certificado (GoDaddy, Namecheap, etc.)
3. Importa certificado en IIS:
   - IIS Manager ? Server Certificates
   - Import ? Selecciona .pfx
   - Ingresa contraseña

### **Configurar Binding HTTPS en IIS:**

1. IIS Manager ? Sites ? PresupuestoFamiliarApp
2. **Bindings** ? **Add**
3. Configuración:
   ```
   Type: https
   IP Address: All Unassigned
   Port: 443
   SSL Certificate: [Selecciona tu certificado]
   ```
4. Click **OK**

### **Forzar HTTPS (Opcional):**

El `web.config` ya incluye la regla de rewrite para forzar HTTPS.

---

## ? **VERIFICACIÓN Y TESTING**

### **1. Verificar Estado del Sitio**

```powershell
# PowerShell
Import-Module WebAdministration
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"
Get-Website -Name "PresupuestoFamiliarApp"
```

### **2. Revisar Logs**

```powershell
# Logs de IIS
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50

# Logs de la aplicación
Get-Content "C:\Publish\PresupuestoFamiliarApp\logs\stdout.log" -Tail 50
```

### **3. Probar la Aplicación**

1. Abre navegador
2. Ve a: `http://localhost` o `http://tu-dominio.com`
3. Deberías ver la página de login
4. Intenta iniciar sesión con usuario admin

### **4. Verificar PWA**

1. Chrome DevTools (F12)
2. Application ? Manifest
3. Application ? Service Workers
4. Lighthouse ? Progressive Web App ? Generate Report

---

## ?? **TROUBLESHOOTING**

### **Error: 500.19 - Configuration Error**

**Causa:** No está instalado el .NET Hosting Bundle

**Solución:**
```powershell
# Verificar instalación
dotnet --list-runtimes
dotnet --list-sdks

# Si no aparece ASP.NET Core Runtime 9.0.x, instalar Hosting Bundle
# Descargar de: https://dotnet.microsoft.com/download/dotnet/9.0
```

### **Error: 502.5 - Process Failure**

**Causa:** La aplicación no puede iniciarse

**Solución:**
1. Verificar logs en `logs\stdout.log`
2. Verificar connection string en `appsettings.Production.json`
3. Verificar permisos del Application Pool Identity
4. Verificar que web.config apunta al DLL correcto

### **Error: Database connection failed**

**Causa:** Connection string incorrecto o SQL Server no accesible

**Solución:**
```powershell
# Probar conexión desde el servidor
sqlcmd -S TU_SERVIDOR -U PresupuestoAppUser -P TuPassword -Q "SELECT @@VERSION"
```

### **Error: 403 - Forbidden**

**Causa:** Permisos incorrectos

**Solución:**
```powershell
icacls "C:\Publish\PresupuestoFamiliarApp" /grant "IIS AppPool\PresupuestoFamiliarAppPool:(OI)(CI)RX" /T
```

### **La aplicación funciona pero es lenta**

**Soluciones:**
1. Habilitar compresión en IIS
2. Configurar Application Initialization
3. Aumentar recursos del servidor
4. Optimizar queries de BD

---

## ?? **MONITOREO Y MANTENIMIENTO**

### **1. Habilitar Application Initialization**

```xml
<!-- En web.config, dentro de <system.webServer> -->
<applicationInitialization>
  <add initializationPage="/" />
</applicationInitialization>
```

### **2. Configurar Health Checks**

En el servidor, crea un script PowerShell:

```powershell
# health-check.ps1
$url = "https://tu-dominio.com"
$response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
if ($response.StatusCode -eq 200) {
    Write-Host "OK: Aplicación funcionando"
    exit 0
} else {
    Write-Host "ERROR: Status Code $($response.StatusCode)"
    exit 1
}
```

Programa con Task Scheduler para ejecutar cada 5 minutos.

### **3. Backup Automático**

```powershell
# backup.ps1
$date = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Backups\PresupuestoApp_$date"

# Backup de archivos
Copy-Item -Path "C:\Publish\PresupuestoFamiliarApp" -Destination $backupPath -Recurse

# Backup de BD
sqlcmd -S localhost -Q "BACKUP DATABASE PresupuestoFamiliarDB TO DISK='C:\Backups\DB_$date.bak'"

# Limpiar backups antiguos (más de 7 días)
Get-ChildItem "C:\Backups" -Recurse | Where-Object {$_.CreationTime -lt (Get-Date).AddDays(-7)} | Remove-Item -Recurse
```

---

## ?? **CHECKLIST DE DESPLIEGUE**

Antes de ir a producción:

- [ ] ? .NET 9.0 Hosting Bundle instalado
- [ ] ? IIS configurado correctamente
- [ ] ? Application Pool creado y configurado
- [ ] ? Sitio web creado en IIS
- [ ] ? Permisos de carpeta configurados
- [ ] ? Base de datos creada en SQL Server
- [ ] ? Migraciones aplicadas
- [ ] ? Usuario admin creado
- [ ] ? appsettings.Production.json configurado
- [ ] ? Connection string actualizado
- [ ] ? Certificado SSL instalado
- [ ] ? HTTPS binding configurado
- [ ] ? Aplicación publicada a carpeta
- [ ] ? web.config presente
- [ ] ? Sitio accesible desde navegador
- [ ] ? Login funciona correctamente
- [ ] ? PWA instala correctamente
- [ ] ? Service Worker registrado
- [ ] ? Emails se envían correctamente
- [ ] ? Hangfire funciona
- [ ] ? Logs configurados
- [ ] ? Backup configurado

---

## ?? **COMANDO RÁPIDO DE DESPLIEGUE**

Para despliegues futuros (después de la configuración inicial):

```powershell
# Script de despliegue rápido
# deploy.ps1

# 1. Publicar aplicación
dotnet publish -c Release -o C:\Publish\PresupuestoFamiliarApp --self-contained false

# 2. Detener sitio
Stop-WebAppPool -Name "PresupuestoFamiliarAppPool"
Stop-Website -Name "PresupuestoFamiliarApp"

# 3. Esperar 5 segundos
Start-Sleep -Seconds 5

# 4. Iniciar sitio
Start-Website -Name "PresupuestoFamiliarApp"
Start-WebAppPool -Name "PresupuestoFamiliarAppPool"

# 5. Verificar estado
Get-WebAppPoolState -Name "PresupuestoFamiliarAppPool"

Write-Host "Despliegue completado!" -ForegroundColor Green
```

---

**¡Tu aplicación ahora está desplegada en producción!** ??
