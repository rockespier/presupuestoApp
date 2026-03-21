# Script de Configuración del Sitio IIS para presupuesto.gestionaminegocio.com
# Ejecutar en el SERVIDOR IIS (161.132.56.79) como Administrador

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACIÓN IIS - PRESUPUESTO APP" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$siteName = "PresupuestoFamiliarApp"
$appPoolName = "PresupuestoFamiliarAppPool"
$physicalPath = "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com"
$hostHeader = "presupuesto.gestionaminegocio.com"

# 1. Verificar que IIS está instalado
Write-Host "1. Verificando IIS..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "   ? IIS está instalado" -ForegroundColor Green
} catch {
    Write-Host "   ? IIS NO está instalado" -ForegroundColor Red
    Write-Host "   Instalar con: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# 2. Crear carpeta física si no existe
Write-Host "2. Creando carpeta del sitio..." -ForegroundColor Yellow
if (!(Test-Path $physicalPath)) {
    New-Item -Path $physicalPath -ItemType Directory -Force | Out-Null
    Write-Host "   ? Carpeta creada: $physicalPath" -ForegroundColor Green
} else {
    Write-Host "   ? Carpeta ya existe: $physicalPath" -ForegroundColor Green
}
Write-Host ""

# 3. Crear Application Pool
Write-Host "3. Configurando Application Pool..." -ForegroundColor Yellow
if (!(Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName | Out-Null
    Write-Host "   ? App Pool creado: $appPoolName" -ForegroundColor Green
} else {
    Write-Host "   ?? App Pool ya existe: $appPoolName" -ForegroundColor Yellow
}

# Configurar propiedades del App Pool
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel.idleTimeout" -Value "00:00:00"

Write-Host "   ? App Pool configurado (.NET Core, Always Running)" -ForegroundColor Green
Write-Host ""

# 4. Crear o actualizar el sitio web
Write-Host "4. Configurando sitio web..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$siteName") {
    Write-Host "   ?? Sitio ya existe, actualizando configuración..." -ForegroundColor Yellow
    
    # Detener el sitio
    Stop-Website -Name $siteName -ErrorAction SilentlyContinue
    
    # Actualizar propiedades
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $physicalPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
    
    # Eliminar bindings antiguos
    Get-WebBinding -Name $siteName | Remove-WebBinding
    
    # Crear nuevo binding
    New-WebBinding -Name $siteName -Protocol "http" -Port 80 -HostHeader $hostHeader | Out-Null
    
    Write-Host "   ? Sitio actualizado" -ForegroundColor Green
} else {
    # Crear nuevo sitio
    New-Website -Name $siteName `
        -PhysicalPath $physicalPath `
        -ApplicationPool $appPoolName `
        -Port 80 `
        -HostHeader $hostHeader | Out-Null
    
    Write-Host "   ? Sitio creado: $siteName" -ForegroundColor Green
}
Write-Host ""

# 5. Configurar permisos
Write-Host "5. Configurando permisos..." -ForegroundColor Yellow

# Permisos para el App Pool
icacls $physicalPath /grant "IIS AppPool\$appPoolName:(OI)(CI)RX" /T | Out-Null
Write-Host "   ? Permisos de lectura/ejecución para App Pool" -ForegroundColor Green

# Permisos para el runner (Everyone - temporal, ajustar después)
icacls $physicalPath /grant "Everyone:(OI)(CI)M" /T | Out-Null
Write-Host "   ? Permisos de escritura para GitHub Runner" -ForegroundColor Green

# Crear carpeta de logs con permisos
$logsPath = Join-Path $physicalPath "logs"
New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
icacls $logsPath /grant "IIS AppPool\$appPoolName:(OI)(CI)M" /T | Out-Null
Write-Host "   ? Carpeta de logs creada con permisos" -ForegroundColor Green
Write-Host ""

# 6. Iniciar el sitio
Write-Host "6. Iniciando servicios..." -ForegroundColor Yellow
Start-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
Start-Website -Name $siteName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

$poolState = (Get-WebAppPoolState -Name $appPoolName).Value
$siteState = (Get-WebsiteState -Name $siteName).Value

if ($poolState -eq "Started") {
    Write-Host "   ? App Pool iniciado" -ForegroundColor Green
} else {
    Write-Host "   ?? App Pool estado: $poolState" -ForegroundColor Yellow
}

if ($siteState -eq "Started") {
    Write-Host "   ? Sitio web iniciado" -ForegroundColor Green
} else {
    Write-Host "   ?? Sitio web estado: $siteState" -ForegroundColor Yellow
}
Write-Host ""

# 7. Verificar DNS
Write-Host "7. Verificando configuración DNS..." -ForegroundColor Yellow
try {
    $dnsResult = Resolve-DnsName $hostHeader -ErrorAction Stop
    $ipAddress = $dnsResult | Where-Object {$_.Type -eq 'A'} | Select-Object -First 1 -ExpandProperty IPAddress
    
    if ($ipAddress) {
        Write-Host "   ? DNS configurado: $hostHeader ? $ipAddress" -ForegroundColor Green
        
        # Verificar si apunta a este servidor
        $localIPs = Get-NetIPAddress | Where-Object {$_.AddressFamily -eq 'IPv4'} | Select-Object -ExpandProperty IPAddress
        if ($localIPs -contains $ipAddress) {
            Write-Host "   ? DNS apunta a este servidor" -ForegroundColor Green
        } else {
            Write-Host "   ?? DNS NO apunta a este servidor" -ForegroundColor Yellow
            Write-Host "   DNS apunta a: $ipAddress" -ForegroundColor Yellow
            Write-Host "   IPs de este servidor: $($localIPs -join ', ')" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "   ?? No se pudo resolver DNS para: $hostHeader" -ForegroundColor Yellow
    Write-Host "   Asegúrate de configurar el registro A en tu proveedor de DNS" -ForegroundColor Yellow
}
Write-Host ""

# 8. Probar acceso local
Write-Host "8. Probando acceso al sitio..." -ForegroundColor Yellow
try {
    # Primero probamos por localhost
    $response = Invoke-WebRequest -Uri "http://localhost" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ? Sitio responde en http://localhost (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ?? No se pudo acceder a http://localhost" -ForegroundColor Yellow
    Write-Host "   Esto es normal si aún no has desplegado la aplicación" -ForegroundColor Gray
}
Write-Host ""

# Resumen final
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "             CONFIGURACIÓN COMPLETA" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Sitio Web:" -ForegroundColor White
Write-Host "  Nombre: $siteName" -ForegroundColor Gray
Write-Host "  App Pool: $appPoolName" -ForegroundColor Gray
Write-Host "  Ruta Física: $physicalPath" -ForegroundColor Gray
Write-Host "  Dominio: $hostHeader" -ForegroundColor Gray
Write-Host ""
Write-Host "URLs de Acceso:" -ForegroundColor White
Write-Host "  HTTP:  http://$hostHeader" -ForegroundColor Gray
Write-Host "  HTTPS: https://$hostHeader (configurar SSL después)" -ForegroundColor Gray
Write-Host ""
Write-Host "Estado Actual:" -ForegroundColor White
Write-Host "  App Pool: $poolState" -ForegroundColor Gray
Write-Host "  Sitio: $siteState" -ForegroundColor Gray
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Asegúrate de que el DNS apunte a 161.132.56.79" -ForegroundColor White
Write-Host "2. Configura el self-hosted runner de GitHub" -ForegroundColor White
Write-Host "3. Haz push a GitHub para desplegar la aplicación" -ForegroundColor White
Write-Host "4. Configura SSL/HTTPS con Let's Encrypt o un certificado comercial" -ForegroundColor White
Write-Host ""
Write-Host "Para verificar el estado en cualquier momento:" -ForegroundColor Yellow
Write-Host "  Get-WebAppPoolState -Name '$appPoolName'" -ForegroundColor Gray
Write-Host "  Get-WebsiteState -Name '$siteName'" -ForegroundColor Gray
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
