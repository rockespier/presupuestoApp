# ========================================
# Script de Despliegue Automatizado
# PresupuestoFamiliarApp - IIS
# ========================================

param(
    [string]$PublishPath = "C:\Publish\PresupuestoFamiliarApp",
    [string]$SiteName = "PresupuestoFamiliarApp",
    [string]$AppPoolName = "PresupuestoFamiliarAppPool",
    [switch]$FirstTime = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE A IIS - PRESUPUESTOAPP    " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar permisos de administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Este script requiere permisos de administrador" -ForegroundColor Red
    Write-Host "Haz click derecho en PowerShell y selecciona 'Ejecutar como administrador'" -ForegroundColor Yellow
    exit 1
}

# Importar módulo de IIS
Import-Module WebAdministration -ErrorAction Stop

# ========================================
# PASO 1: PUBLICAR APLICACIÓN
# ========================================
Write-Host "[1/7] Publicando aplicación..." -ForegroundColor Yellow

$projectPath = Split-Path -Parent $PSScriptRoot
Set-Location $projectPath

try {
    dotnet publish -c Release -o $PublishPath --self-contained false
    Write-Host "? Aplicación publicada exitosamente" -ForegroundColor Green
} catch {
    Write-Host "? Error al publicar: $_" -ForegroundColor Red
    exit 1
}

# ========================================
# PASO 2: CONFIGURAR IIS (Primera vez)
# ========================================
if ($FirstTime) {
    Write-Host "[2/7] Configurando IIS (Primera instalación)..." -ForegroundColor Yellow
    
    # Crear Application Pool si no existe
    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        New-WebAppPool -Name $AppPoolName
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"
        Write-Host "? Application Pool creado" -ForegroundColor Green
    }
    
    # Crear sitio web si no existe
    if (-not (Test-Path "IIS:\Sites\$SiteName")) {
        New-Website -Name $SiteName `
                    -ApplicationPool $AppPoolName `
                    -PhysicalPath $PublishPath `
                    -Port 80
        Write-Host "? Sitio web creado" -ForegroundColor Green
    }
} else {
    Write-Host "[2/7] Omitiendo configuración inicial de IIS..." -ForegroundColor Gray
}

# ========================================
# PASO 3: DETENER SITIO
# ========================================
Write-Host "[3/7] Deteniendo sitio web..." -ForegroundColor Yellow

try {
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    Write-Host "? Sitio detenido" -ForegroundColor Green
} catch {
    Write-Host "?? Advertencia al detener sitio: $_" -ForegroundColor Yellow
}

# ========================================
# PASO 4: CONFIGURAR PERMISOS
# ========================================
Write-Host "[4/7] Configurando permisos..." -ForegroundColor Yellow

try {
    $identity = "IIS AppPool\$AppPoolName"
    
    # Permisos de lectura/ejecución en carpeta principal
    icacls $PublishPath /grant "${identity}:(OI)(CI)RX" /T /Q
    
    # Permisos de escritura en wwwroot
    $wwwrootPath = Join-Path $PublishPath "wwwroot"
    if (Test-Path $wwwrootPath) {
        icacls $wwwrootPath /grant "${identity}:(OI)(CI)M" /T /Q
    }
    
    # Crear y dar permisos a carpeta de logs
    $logsPath = Join-Path $PublishPath "logs"
    if (-not (Test-Path $logsPath)) {
        New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
    }
    icacls $logsPath /grant "${identity}:(OI)(CI)M" /T /Q
    
    Write-Host "? Permisos configurados" -ForegroundColor Green
} catch {
    Write-Host "? Error configurando permisos: $_" -ForegroundColor Red
    exit 1
}

# ========================================
# PASO 5: VERIFICAR WEB.CONFIG
# ========================================
Write-Host "[5/7] Verificando web.config..." -ForegroundColor Yellow

$webConfigPath = Join-Path $PublishPath "web.config"
if (Test-Path $webConfigPath) {
    Write-Host "? web.config presente" -ForegroundColor Green
} else {
    Write-Host "?? web.config no encontrado - se generará automáticamente" -ForegroundColor Yellow
}

# ========================================
# PASO 6: INICIAR SITIO
# ========================================
Write-Host "[6/7] Iniciando sitio web..." -ForegroundColor Yellow

try {
    Start-WebAppPool -Name $AppPoolName
    Start-Website -Name $SiteName
    Start-Sleep -Seconds 2
    Write-Host "? Sitio iniciado" -ForegroundColor Green
} catch {
    Write-Host "? Error al iniciar sitio: $_" -ForegroundColor Red
    exit 1
}

# ========================================
# PASO 7: VERIFICAR ESTADO
# ========================================
Write-Host "[7/7] Verificando estado..." -ForegroundColor Yellow

$poolState = Get-WebAppPoolState -Name $AppPoolName
$siteState = Get-Website -Name $SiteName | Select-Object -ExpandProperty State

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RESULTADO DEL DESPLIEGUE              " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Application Pool: $($poolState.Value)" -ForegroundColor $(if ($poolState.Value -eq "Started") { "Green" } else { "Red" })
Write-Host "Sitio Web: $siteState" -ForegroundColor $(if ($siteState -eq "Started") { "Green" } else { "Red" })
Write-Host "Ruta: $PublishPath" -ForegroundColor White
Write-Host ""

# Verificar acceso HTTP
Write-Host "Verificando acceso HTTP..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "? Sitio accesible (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "?? No se pudo verificar acceso HTTP: $_" -ForegroundColor Yellow
    Write-Host "Verifica manualmente en: http://localhost" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ¡DESPLIEGUE COMPLETADO!               " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Verificar en navegador: http://localhost" -ForegroundColor White
Write-Host "2. Revisar logs en: $PublishPath\logs" -ForegroundColor White
Write-Host "3. Configurar SSL/HTTPS si no está configurado" -ForegroundColor White
Write-Host "4. Probar login con usuario admin" -ForegroundColor White
Write-Host ""

# Preguntar si abrir el navegador
$openBrowser = Read-Host "¿Deseas abrir el sitio en el navegador? (S/N)"
if ($openBrowser -eq "S" -or $openBrowser -eq "s") {
    Start-Process "http://localhost"
}

# Preguntar si mostrar logs
$showLogs = Read-Host "¿Deseas ver los logs más recientes? (S/N)"
if ($showLogs -eq "S" -or $showLogs -eq "s") {
    $stdoutLog = Join-Path $logsPath "stdout.log"
    if (Test-Path $stdoutLog) {
        Write-Host ""
        Write-Host "Últimas 20 líneas del log:" -ForegroundColor Yellow
        Get-Content $stdoutLog -Tail 20
    } else {
        Write-Host "No hay logs disponibles aún" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Script finalizado exitosamente" -ForegroundColor Green
