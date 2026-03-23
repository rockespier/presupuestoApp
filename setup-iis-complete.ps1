# Script para Configurar IIS Completo para PresupuestoFamiliarApp
# Ejecutar EN EL SERVIDOR como Administrador

$ErrorActionPreference = "Continue"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION COMPLETA DE IIS" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Variables de configuración
$appPoolName = "presupuesto.gestionaminegocio.com"
$siteName = "presupuesto.gestionaminegocio.com"
$physicalPath = "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com"
$hostHeader = "presupuesto.gestionaminegocio.com"
$port = 80

# 1. Verificar que IIS está instalado
Write-Host "1. Verificando IIS..." -ForegroundColor Yellow

try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "   IIS instalado correctamente" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: IIS no esta instalado" -ForegroundColor Red
    Write-Host "   Instala IIS con: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    exit 1
}

# 2. Crear carpeta física del sitio
Write-Host ""
Write-Host "2. Creando carpeta del sitio..." -ForegroundColor Yellow

if (!(Test-Path $physicalPath)) {
    New-Item -Path $physicalPath -ItemType Directory -Force | Out-Null
    Write-Host "   Carpeta creada: $physicalPath" -ForegroundColor Green
} else {
    Write-Host "   Carpeta ya existe: $physicalPath" -ForegroundColor Gray
}

# 3. Verificar si el App Pool ya existe
Write-Host ""
Write-Host "3. Configurando Application Pool..." -ForegroundColor Yellow

if (Test-Path "IIS:\AppPools\$appPoolName") {
    Write-Host "   App Pool ya existe: $appPoolName" -ForegroundColor Gray
    
    $update = Read-Host "   Deseas actualizarlo? (S/N)"
    if ($update -eq "S" -or $update -eq "s") {
        Remove-WebAppPool -Name $appPoolName
        Write-Host "   App Pool eliminado para recrear" -ForegroundColor Yellow
    } else {
        Write-Host "   Manteniendo App Pool existente" -ForegroundColor Cyan
        $skipAppPool = $true
    }
}

if (-not $skipAppPool) {
    # Crear nuevo App Pool
    New-WebAppPool -Name $appPoolName
    Write-Host "   App Pool creado: $appPoolName" -ForegroundColor Green
    
    # Configurar App Pool para .NET Core
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
    Write-Host "   Configurado para .NET Core (No Managed Code)" -ForegroundColor Green
    
    # Configurar modo de pipeline
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode" -Value "Integrated"
    Write-Host "   Modo de pipeline: Integrated" -ForegroundColor Green
    
    # Configurar para que siempre esté corriendo
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "startMode" -Value "AlwaysRunning"
    Write-Host "   Start Mode: AlwaysRunning" -ForegroundColor Green
    
    # Configurar identity
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    Write-Host "   Identity: ApplicationPoolIdentity" -ForegroundColor Green
    
    # Configurar timeout
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))
    Write-Host "   Idle Timeout: 0 (sin timeout)" -ForegroundColor Green
}

# 4. Configurar permisos del App Pool en la carpeta
Write-Host ""
Write-Host "4. Configurando permisos..." -ForegroundColor Yellow

try {
    # Permisos de lectura y ejecución
    icacls $physicalPath /grant "IIS AppPool\${appPoolName}:(OI)(CI)RX" /T | Out-Null
    Write-Host "   Permisos de lectura configurados para App Pool" -ForegroundColor Green
    
    # Crear y configurar carpeta wwwroot
    $wwwrootPath = Join-Path $physicalPath "wwwroot"
    if (!(Test-Path $wwwrootPath)) {
        New-Item -Path $wwwrootPath -ItemType Directory -Force | Out-Null
    }
    icacls $wwwrootPath /grant "IIS AppPool\${appPoolName}:(OI)(CI)M" /T | Out-Null
    Write-Host "   Permisos de escritura en wwwroot configurados" -ForegroundColor Green
    
    # Crear y configurar carpeta logs
    $logsPath = Join-Path $physicalPath "logs"
    if (!(Test-Path $logsPath)) {
        New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
    }
    icacls $logsPath /grant "IIS AppPool\${appPoolName}:(OI)(CI)M" /T | Out-Null
    Write-Host "   Carpeta de logs creada y configurada" -ForegroundColor Green
    
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   Advertencia al configurar permisos: $errorMsg" -ForegroundColor Yellow
}

# 5. Verificar si el sitio ya existe
Write-Host ""
Write-Host "5. Configurando sitio web..." -ForegroundColor Yellow

if (Test-Path "IIS:\Sites\$siteName") {
    Write-Host "   Sitio web ya existe: $siteName" -ForegroundColor Gray
    
    $update = Read-Host "   Deseas actualizarlo? (S/N)"
    if ($update -eq "S" -or $update -eq "s") {
        Remove-Website -Name $siteName
        Write-Host "   Sitio eliminado para recrear" -ForegroundColor Yellow
    } else {
        Write-Host "   Manteniendo sitio existente" -ForegroundColor Cyan
        $skipSite = $true
    }
}

if (-not $skipSite) {
    # Crear sitio web
    New-Website -Name $siteName `
        -PhysicalPath $physicalPath `
        -ApplicationPool $appPoolName `
        -Port $port `
        -HostHeader $hostHeader `
        -Force
    
    Write-Host "   Sitio web creado: $siteName" -ForegroundColor Green
    Write-Host "   Puerto: $port" -ForegroundColor Cyan
    Write-Host "   Host Header: $hostHeader" -ForegroundColor Cyan
}

# 6. Iniciar App Pool y Sitio
Write-Host ""
Write-Host "6. Iniciando servicios..." -ForegroundColor Yellow

try {
    # Iniciar App Pool
    $poolState = (Get-WebAppPoolState -Name $appPoolName).Value
    if ($poolState -ne "Started") {
        Start-WebAppPool -Name $appPoolName
        Write-Host "   App Pool iniciado" -ForegroundColor Green
    } else {
        Write-Host "   App Pool ya esta corriendo" -ForegroundColor Gray
    }
    
    # Iniciar Sitio
    $siteState = (Get-WebsiteState -Name $siteName).Value
    if ($siteState -ne "Started") {
        Start-Website -Name $siteName
        Write-Host "   Sitio web iniciado" -ForegroundColor Green
    } else {
        Write-Host "   Sitio web ya esta corriendo" -ForegroundColor Gray
    }
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   Error al iniciar servicios: $errorMsg" -ForegroundColor Red
}

# 7. Verificar configuración
Write-Host ""
Write-Host "7. Verificando configuracion..." -ForegroundColor Yellow

try {
    # Estado del App Pool
    $poolState = (Get-WebAppPoolState -Name $appPoolName).Value
    Write-Host "   App Pool State: $poolState" -ForegroundColor Cyan
    
    # Estado del Sitio
    $siteState = (Get-WebsiteState -Name $siteName).Value
    Write-Host "   Website State: $siteState" -ForegroundColor Cyan
    
    # Bindings del sitio
    $bindings = Get-WebBinding -Name $siteName
    Write-Host "   Bindings:" -ForegroundColor Cyan
    foreach ($binding in $bindings) {
        Write-Host "     $($binding.protocol)://$($binding.bindingInformation)" -ForegroundColor Gray
    }
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   Error al verificar: $errorMsg" -ForegroundColor Yellow
}

# 8. Configurar archivo hosts (opcional para pruebas locales)
Write-Host ""
Write-Host "8. Configuracion de DNS local..." -ForegroundColor Yellow

$hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
$hostEntry = "127.0.0.1    $hostHeader"

$hostsContent = Get-Content $hostsFile -ErrorAction SilentlyContinue
if ($hostsContent -notcontains $hostEntry) {
    $addHosts = Read-Host "   Deseas agregar entrada en hosts para pruebas locales? (S/N)"
    
    if ($addHosts -eq "S" -or $addHosts -eq "s") {
        Add-Content -Path $hostsFile -Value "`n$hostEntry"
        Write-Host "   Entrada agregada al archivo hosts" -ForegroundColor Green
        Write-Host "   Podras probar en: http://$hostHeader" -ForegroundColor Cyan
    }
} else {
    Write-Host "   Entrada ya existe en archivo hosts" -ForegroundColor Gray
}

# 9. Resumen final
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION COMPLETADA" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumen de configuracion:" -ForegroundColor Yellow
Write-Host ""
Write-Host "App Pool:" -ForegroundColor Cyan
Write-Host "  Nombre: $appPoolName" -ForegroundColor White
Write-Host "  Estado: $(try {(Get-WebAppPoolState -Name $appPoolName).Value} catch {'Error'})" -ForegroundColor White
Write-Host ""
Write-Host "Sitio Web:" -ForegroundColor Cyan
Write-Host "  Nombre: $siteName" -ForegroundColor White
Write-Host "  Estado: $(try {(Get-WebsiteState -Name $siteName).Value} catch {'Error'})" -ForegroundColor White
Write-Host "  Ruta fisica: $physicalPath" -ForegroundColor White
Write-Host ""
Write-Host "URLs de acceso:" -ForegroundColor Cyan
Write-Host "  Local: http://localhost:$port" -ForegroundColor White
Write-Host "  Dominio: http://$hostHeader" -ForegroundColor White
Write-Host ""

# 10. Probar acceso al sitio
Write-Host "10. Probando acceso al sitio..." -ForegroundColor Yellow

try {
    $testUrl = "http://localhost:$port"
    $response = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    
    Write-Host "   Sitio responde correctamente (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   El sitio aun no responde (normal si no hay archivos desplegados)" -ForegroundColor Yellow
    Write-Host "   Esto es esperado antes del primer deployment" -ForegroundColor Gray
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "   IIS CONFIGURADO CORRECTAMENTE" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Yellow
Write-Host "1. Ejecuta el script configure-runner-permissions.ps1" -ForegroundColor White
Write-Host "2. Haz push desde tu PC local" -ForegroundColor White
Write-Host "3. El workflow desplegara automaticamente" -ForegroundColor White
Write-Host ""

# 11. Crear archivo de prueba
$createTestFile = Read-Host "Deseas crear un archivo index.html de prueba? (S/N)"

if ($createTestFile -eq "S" -or $createTestFile -eq "s") {
    $testHtml = @"
<!DOCTYPE html>
<html>
<head>
    <title>PresupuestoFamiliarApp - Configuracion OK</title>
    <style>
        body { font-family: Arial; text-align: center; padding: 50px; background: #f0f0f0; }
        .success { color: green; font-size: 24px; }
    </style>
</head>
<body>
    <h1 class="success">? IIS Configurado Correctamente</h1>
    <p>El sitio esta listo para recibir el primer deployment</p>
    <hr>
    <p><strong>App Pool:</strong> $appPoolName</p>
    <p><strong>Sitio:</strong> $siteName</p>
    <p><strong>Host:</strong> $hostHeader</p>
</body>
</html>
"@
    
    $testFilePath = Join-Path $physicalPath "index.html"
    Set-Content -Path $testFilePath -Value $testHtml
    Write-Host ""
    Write-Host "Archivo de prueba creado en: $testFilePath" -ForegroundColor Green
    Write-Host "Accede a: http://localhost:$port o http://$hostHeader" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Configuracion completada!" -ForegroundColor Green
Write-Host ""
