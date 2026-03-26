# ========================================
# Script de Verificación de Requisitos
# Verifica que el servidor tenga todo lo necesario
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICACIÓN DE REQUISITOS - IIS      " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allOk = $true

# ========================================
# 1. VERIFICAR PERMISOS DE ADMINISTRADOR
# ========================================
Write-Host "[1/8] Verificando permisos de administrador..." -ForegroundColor Yellow
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-Host "? Ejecutando como administrador" -ForegroundColor Green
} else {
    Write-Host "? NO tienes permisos de administrador" -ForegroundColor Red
    Write-Host "   Ejecuta PowerShell como administrador" -ForegroundColor Yellow
    $allOk = $false
}

# ========================================
# 2. VERIFICAR IIS INSTALADO
# ========================================
Write-Host "[2/8] Verificando IIS instalado..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    $iisVersion = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\InetStp" -ErrorAction Stop
    Write-Host "? IIS instalado - Versión $($iisVersion.MajorVersion).$($iisVersion.MinorVersion)" -ForegroundColor Green
} catch {
    Write-Host "? IIS NO está instalado" -ForegroundColor Red
    Write-Host "   Instala con: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    $allOk = $false
}

# ========================================
# 3. VERIFICAR .NET INSTALADO
# ========================================
Write-Host "[3/8] Verificando .NET instalado..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET SDK instalado - Versión $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "?? .NET SDK no encontrado (opcional para servidor)" -ForegroundColor Yellow
}

# ========================================
# 4. VERIFICAR .NET RUNTIME
# ========================================
Write-Host "[4/8] Verificando .NET Runtime..." -ForegroundColor Yellow
try {
    $runtimes = dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 9.0"
    if ($runtimes) {
        Write-Host "? ASP.NET Core Runtime 9.0 instalado" -ForegroundColor Green
        $runtimes | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    } else {
        Write-Host "? ASP.NET Core Runtime 9.0 NO instalado" -ForegroundColor Red
        Write-Host "   Descarga desde: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
        Write-Host "   Busca: 'Hosting Bundle'" -ForegroundColor Yellow
        $allOk = $false
    }
} catch {
    Write-Host "? No se pudo verificar .NET Runtime" -ForegroundColor Red
    $allOk = $false
}

# ========================================
# 5. VERIFICAR MÓDULO ASP.NET CORE
# ========================================
Write-Host "[5/8] Verificando módulo ASP.NET Core..." -ForegroundColor Yellow
$modulePath = "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (Test-Path $modulePath) {
    Write-Host "? Módulo ASP.NET Core V2 instalado" -ForegroundColor Green
} else {
    Write-Host "? Módulo ASP.NET Core NO encontrado" -ForegroundColor Red
    Write-Host "   Instala el .NET Hosting Bundle" -ForegroundColor Yellow
    $allOk = $false
}

# ========================================
# 6. VERIFICAR SQL SERVER
# ========================================
Write-Host "[6/8] Verificando SQL Server..." -ForegroundColor Yellow
try {
    $sqlService = Get-Service | Where-Object { $_.Name -like "MSSQL*" -and $_.Status -eq "Running" }
    if ($sqlService) {
        Write-Host "? SQL Server en ejecución" -ForegroundColor Green
        $sqlService | ForEach-Object { Write-Host "   $($_.DisplayName)" -ForegroundColor Gray }
    } else {
        Write-Host "?? SQL Server no detectado o no está corriendo" -ForegroundColor Yellow
        Write-Host "   Si usas SQL Server remoto, ignora esta advertencia" -ForegroundColor Gray
    }
} catch {
    Write-Host "?? No se pudo verificar SQL Server" -ForegroundColor Yellow
}

# ========================================
# 7. VERIFICAR PUERTOS
# ========================================
Write-Host "[7/8] Verificando puertos disponibles..." -ForegroundColor Yellow

# Puerto 80
$port80 = Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction SilentlyContinue
if ($port80) {
    Write-Host "?? Puerto 80 en uso (puede ser IIS u otro servicio)" -ForegroundColor Cyan
} else {
    Write-Host "? Puerto 80 disponible" -ForegroundColor Green
}

# Puerto 443
$port443 = Get-NetTCPConnection -LocalPort 443 -State Listen -ErrorAction SilentlyContinue
if ($port443) {
    Write-Host "?? Puerto 443 en uso (puede ser IIS u otro servicio)" -ForegroundColor Cyan
} else {
    Write-Host "? Puerto 443 disponible" -ForegroundColor Green
}

# ========================================
# 8. VERIFICAR CARACTERÍSTICAS DE WINDOWS
# ========================================
Write-Host "[8/8] Verificando características de Windows..." -ForegroundColor Yellow

$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-ApplicationInit",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-WebSockets"
)

$missingFeatures = @()
foreach ($feature in $features) {
    $state = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue
    if ($state -and $state.State -eq "Enabled") {
        Write-Host "   ? $feature" -ForegroundColor Green
    } else {
        Write-Host "   ? $feature (faltante)" -ForegroundColor Red
        $missingFeatures += $feature
    }
}

if ($missingFeatures.Count -gt 0) {
    Write-Host ""
    Write-Host "Para habilitar las características faltantes, ejecuta:" -ForegroundColor Yellow
    Write-Host "Enable-WindowsOptionalFeature -Online -FeatureName $($missingFeatures -join ',')" -ForegroundColor White
    $allOk = $false
}

# ========================================
# RESUMEN FINAL
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RESUMEN DE VERIFICACIÓN               " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($allOk) {
    Write-Host "? TODOS LOS REQUISITOS CUMPLIDOS" -ForegroundColor Green
    Write-Host ""
    Write-Host "Puedes proceder con el despliegue ejecutando:" -ForegroundColor White
    Write-Host ".\deploy-to-iis.ps1 -FirstTime" -ForegroundColor Cyan
} else {
    Write-Host "? HAY REQUISITOS FALTANTES" -ForegroundColor Red
    Write-Host ""
    Write-Host "Corrige los errores marcados arriba antes de desplegar" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Pasos recomendados:" -ForegroundColor Yellow
    Write-Host "1. Instalar IIS: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor White
    Write-Host "2. Descargar .NET 9.0 Hosting Bundle desde:" -ForegroundColor White
    Write-Host "   https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
    Write-Host "3. Reiniciar IIS después de instalar: net stop was /y && net start w3svc" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Información adicional
Write-Host ""
Write-Host "INFORMACIÓN DEL SISTEMA:" -ForegroundColor Yellow
Write-Host "Hostname: $env:COMPUTERNAME" -ForegroundColor White
Write-Host "OS: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Caption)" -ForegroundColor White
Write-Host "Versión: $(Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty Version)" -ForegroundColor White
Write-Host "RAM: $([math]::Round((Get-WmiObject Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB" -ForegroundColor White
Write-Host ""

# Guardar reporte
$reportPath = ".\verification-report.txt"
$reportContent = @"
REPORTE DE VERIFICACIÓN - $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
========================================

Permisos Admin: $isAdmin
IIS Instalado: $(Test-Path "HKLM:\SOFTWARE\Microsoft\InetStp")
.NET Runtime 9.0: $($runtimes -ne $null)
ASP.NET Core Module: $(Test-Path $modulePath)
SQL Server: $($sqlService -ne $null)

Estado Final: $(if ($allOk) { "? OK" } else { "? Falta configuración" })
"@

Set-Content -Path $reportPath -Value $reportContent
Write-Host "Reporte guardado en: $reportPath" -ForegroundColor Gray
