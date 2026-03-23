# Script para Configurar Permisos del GitHub Actions Runner
# Ejecutar EN EL SERVIDOR como Administrador

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION DE PERMISOS DEL RUNNER" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Obtener el nombre del servicio del runner
Write-Host "1. Identificando servicio del runner..." -ForegroundColor Yellow

$runnerService = Get-Service | Where-Object {$_.Name -like "*actions.runner*"}

if (-not $runnerService) {
    Write-Host "ERROR: No se encontro el servicio del runner" -ForegroundColor Red
    Write-Host "Asegurate de que el runner este instalado y configurado" -ForegroundColor Yellow
    exit 1
}

$serviceName = $runnerService.Name
Write-Host "   Servicio encontrado: $serviceName" -ForegroundColor Green

# 2. Obtener la cuenta bajo la cual se ejecuta el servicio
Write-Host ""
Write-Host "2. Obteniendo cuenta del servicio..." -ForegroundColor Yellow

$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='$serviceName'"
$serviceAccount = $serviceInfo.StartName

Write-Host "   Cuenta actual: $serviceAccount" -ForegroundColor Cyan

# 3. Verificar si es cuenta local o de sistema
if ($serviceAccount -eq "LocalSystem" -or $serviceAccount -eq "NT AUTHORITY\SYSTEM") {
    Write-Host "   El servicio ya se ejecuta como LocalSystem" -ForegroundColor Green
    Write-Host "   Esta cuenta tiene permisos suficientes para IIS" -ForegroundColor Green
    $needsGroupConfig = $false
} else {
    Write-Host "   El servicio se ejecuta como cuenta limitada" -ForegroundColor Yellow
    $needsGroupConfig = $true
}

# 4. Si necesita configuracion, agregar a grupos
if ($needsGroupConfig) {
    Write-Host ""
    Write-Host "3. Agregando cuenta a grupos necesarios..." -ForegroundColor Yellow
    
    $groups = @(
        "IIS_IUSRS",           # Grupo de usuarios de IIS
        "Administrators"       # Grupo de administradores (necesario para control completo de IIS)
    )
    
    foreach ($group in $groups) {
        try {
            Write-Host "   Agregando a grupo: $group" -ForegroundColor Cyan
            
            # Verificar si ya esta en el grupo
            $members = net localgroup $group | Where-Object {$_ -like "*$serviceAccount*"}
            
            if ($members) {
                Write-Host "   Ya es miembro de $group" -ForegroundColor Gray
            } else {
                net localgroup $group $serviceAccount /add 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   Agregado exitosamente a $group" -ForegroundColor Green
                } else {
                    Write-Host "   No se pudo agregar a $group" -ForegroundColor Yellow
                }
            }
        } catch {
            $errorMsg = $_.Exception.Message
            Write-Host "   Error al agregar a $group`: $errorMsg" -ForegroundColor Yellow
        }
    }
}

# 5. Configurar permisos especificos en archivos de configuracion de IIS
Write-Host ""
Write-Host "4. Configurando permisos en archivos de IIS..." -ForegroundColor Yellow

$iisConfigPaths = @(
    "$env:SystemRoot\System32\inetsrv\config",
    "$env:SystemRoot\System32\inetsrv\config\applicationHost.config",
    "$env:SystemRoot\System32\inetsrv\config\redirection.config"
)

foreach ($path in $iisConfigPaths) {
    if (Test-Path $path) {
        Write-Host "   Configurando permisos en: $path" -ForegroundColor Cyan
        
        try {
            # Dar permisos de lectura y ejecucion
            icacls $path /grant "${serviceAccount}:(RX)" /T 2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   Permisos configurados correctamente" -ForegroundColor Green
            } else {
                Write-Host "   Advertencia: No se pudieron configurar todos los permisos" -ForegroundColor Yellow
            }
        } catch {
            $errorMsg = $_.Exception.Message
            Write-Host "   Error: $errorMsg" -ForegroundColor Yellow
        }
    }
}

# 6. Dar permisos en la carpeta del sitio web
Write-Host ""
Write-Host "5. Configurando permisos en carpeta de publicacion..." -ForegroundColor Yellow

$webPath = "C:\inetpub\wwwroot\presupuesto.gestionaminegocio.com"

if (-not (Test-Path $webPath)) {
    Write-Host "   Creando carpeta: $webPath" -ForegroundColor Cyan
    New-Item -Path $webPath -ItemType Directory -Force | Out-Null
}

Write-Host "   Configurando permisos en: $webPath" -ForegroundColor Cyan

try {
    # Dar control completo a la cuenta del runner
    icacls $webPath /grant "${serviceAccount}:(OI)(CI)F" /T 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Permisos configurados correctamente" -ForegroundColor Green
    }
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   Error: $errorMsg" -ForegroundColor Yellow
}

# 7. Opcion: Cambiar el servicio para ejecutarse como LocalSystem
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   OPCION: EJECUTAR COMO LOCALSYSTEM" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($serviceAccount -ne "LocalSystem" -and $serviceAccount -ne "NT AUTHORITY\SYSTEM") {
    Write-Host "RECOMENDACION: Para evitar problemas de permisos futuros," -ForegroundColor Yellow
    Write-Host "es mejor ejecutar el servicio como LocalSystem" -ForegroundColor Yellow
    Write-Host ""
    
    $response = Read-Host "Deseas cambiar el servicio a LocalSystem? (S/N)"
    
    if ($response -eq "S" -or $response -eq "s") {
        Write-Host ""
        Write-Host "Cambiando servicio a LocalSystem..." -ForegroundColor Cyan
        
        # Detener servicio
        Write-Host "   Deteniendo servicio..." -ForegroundColor Yellow
        Stop-Service $serviceName -Force
        Start-Sleep -Seconds 3
        
        # Cambiar cuenta de servicio
        Write-Host "   Cambiando cuenta de servicio..." -ForegroundColor Yellow
        sc.exe config $serviceName obj= "LocalSystem" 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Cuenta cambiada exitosamente" -ForegroundColor Green
        } else {
            Write-Host "   Error al cambiar la cuenta" -ForegroundColor Red
        }
        
        # Iniciar servicio
        Write-Host "   Iniciando servicio..." -ForegroundColor Yellow
        Start-Service $serviceName
        Start-Sleep -Seconds 3
        
        # Verificar estado
        $serviceStatus = (Get-Service $serviceName).Status
        
        if ($serviceStatus -eq "Running") {
            Write-Host "   Servicio iniciado correctamente" -ForegroundColor Green
        } else {
            Write-Host "   Advertencia: El servicio no esta corriendo" -ForegroundColor Red
        }
    } else {
        Write-Host "Se mantendra la configuracion actual" -ForegroundColor Cyan
    }
}

# 8. Verificar permisos del App Pool
Write-Host ""
Write-Host "6. Configurando permisos del Application Pool..." -ForegroundColor Yellow

$appPoolName = "presupuesto.gestionaminegocio.com"

try {
    # Verificar si el App Pool existe
    $appPoolExists = & "$env:SystemRoot\System32\inetsrv\appcmd.exe" list apppool "$appPoolName" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   App Pool encontrado: $appPoolName" -ForegroundColor Green
        
        # Dar permisos al App Pool Identity
        Write-Host "   Configurando permisos para App Pool Identity..." -ForegroundColor Cyan
        
        icacls $webPath /grant "IIS AppPool\${appPoolName}:(OI)(CI)RX" /T 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Permisos del App Pool configurados" -ForegroundColor Green
        }
        
        # Permisos de escritura en wwwroot
        $wwwrootPath = Join-Path $webPath "wwwroot"
        if (Test-Path $wwwrootPath) {
            icacls $wwwrootPath /grant "IIS AppPool\${appPoolName}:(OI)(CI)M" /T 2>&1 | Out-Null
            Write-Host "   Permisos de escritura en wwwroot configurados" -ForegroundColor Green
        }
    } else {
        Write-Host "   App Pool no encontrado - se creara en el primer deployment" -ForegroundColor Yellow
    }
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   No se pudo configurar permisos del App Pool: $errorMsg" -ForegroundColor Yellow
}

# 9. Resumen final
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION COMPLETADA" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumen de configuracion:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Servicio: $serviceName" -ForegroundColor Cyan
Write-Host "Cuenta: $serviceAccount" -ForegroundColor Cyan
Write-Host "Estado: $(Get-Service $serviceName | Select-Object -ExpandProperty Status)" -ForegroundColor Cyan
Write-Host ""

# 10. Probar acceso a IIS
Write-Host "7. Probando acceso a IIS..." -ForegroundColor Yellow

try {
    # Intentar listar App Pools
    $appPools = & "$env:SystemRoot\System32\inetsrv\appcmd.exe" list apppool 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   El runner puede acceder a IIS correctamente" -ForegroundColor Green
        $poolCount = ($appPools | Measure-Object).Count
        Write-Host "   App Pools encontrados: $poolCount" -ForegroundColor Cyan
    } else {
        Write-Host "   Advertencia: Puede haber problemas de acceso a IIS" -ForegroundColor Yellow
    }
} catch {
    $errorMsg = $_.Exception.Message
    Write-Host "   Error al probar acceso a IIS: $errorMsg" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "   LISTO PARA DEPLOYMENT" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Yellow
Write-Host "1. Haz push desde tu PC local" -ForegroundColor White
Write-Host "2. El workflow deberia ejecutarse sin errores de permisos" -ForegroundColor White
Write-Host "3. El deployment se completara exitosamente" -ForegroundColor White
Write-Host ""

# 11. Opcional: Reiniciar el servicio para aplicar cambios
$restart = Read-Host "Deseas reiniciar el servicio del runner ahora? (S/N)"

if ($restart -eq "S" -or $restart -eq "s") {
    Write-Host ""
    Write-Host "Reiniciando servicio..." -ForegroundColor Cyan
    
    Restart-Service $serviceName -Force
    Start-Sleep -Seconds 5
    
    $finalStatus = (Get-Service $serviceName).Status
    
    if ($finalStatus -eq "Running") {
        Write-Host "Servicio reiniciado correctamente" -ForegroundColor Green
    } else {
        Write-Host "Advertencia: El servicio no esta corriendo" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Configuracion completada!" -ForegroundColor Green
Write-Host ""
