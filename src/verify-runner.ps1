# Script de Verificación Rápida para Self-Hosted Runner
# Ejecutar en el SERVIDOR IIS como Administrador

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   VERIFICACIÓN GITHUB ACTIONS RUNNER" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$allGood = $true

# 1. Verificar .NET
Write-Host "1. Verificando .NET 9.0..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion) {
    Write-Host "   ? .NET instalado: $dotnetVersion" -ForegroundColor Green
    
    $runtimes = dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 9"
    if ($runtimes) {
        Write-Host "   ? ASP.NET Core 9.0 Runtime encontrado" -ForegroundColor Green
    } else {
        Write-Host "   ? ASP.NET Core 9.0 Runtime NO encontrado" -ForegroundColor Red
        Write-Host "     Instalar desde: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
        $allGood = $false
    }
} else {
    Write-Host "   ? .NET NO instalado" -ForegroundColor Red
    $allGood = $false
}
Write-Host ""

# 2. Verificar IIS
Write-Host "2. Verificando IIS..." -ForegroundColor Yellow
$iisService = Get-Service W3SVC -ErrorAction SilentlyContinue
if ($iisService) {
    if ($iisService.Status -eq "Running") {
        Write-Host "   ? IIS está corriendo" -ForegroundColor Green
    } else {
        Write-Host "   ? IIS está instalado pero no corriendo" -ForegroundColor Yellow
        Write-Host "     Iniciar con: net start w3svc" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? IIS NO instalado" -ForegroundColor Red
    Write-Host "     Instalar con: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    $allGood = $false
}
Write-Host ""

# 3. Verificar Git
Write-Host "3. Verificando Git..." -ForegroundColor Yellow
$gitVersion = git --version 2>$null
if ($gitVersion) {
    Write-Host "   ? Git instalado: $gitVersion" -ForegroundColor Green
} else {
    Write-Host "   ? Git NO encontrado" -ForegroundColor Yellow
    Write-Host "     Descargar desde: https://git-scm.com/download/win" -ForegroundColor Yellow
}
Write-Host ""

# 4. Verificar Carpeta del Runner
Write-Host "4. Verificando carpeta del runner..." -ForegroundColor Yellow
if (Test-Path "C:\actions-runner") {
    Write-Host "   ? Carpeta C:\actions-runner existe" -ForegroundColor Green
    
    # Verificar archivos del runner
    if (Test-Path "C:\actions-runner\config.cmd") {
        Write-Host "   ? Runner descargado y extraído" -ForegroundColor Green
    } else {
        Write-Host "   ? Runner NO descargado" -ForegroundColor Yellow
        Write-Host "     Descargar desde GitHub Actions" -ForegroundColor Yellow
    }
    
    # Verificar si está configurado
    if (Test-Path "C:\actions-runner\.runner") {
        Write-Host "   ? Runner configurado" -ForegroundColor Green
        
        # Leer configuración
        $config = Get-Content "C:\actions-runner\.runner" | ConvertFrom-Json
        Write-Host "   • Nombre: $($config.agentName)" -ForegroundColor Cyan
        Write-Host "   • Pool: $($config.poolName)" -ForegroundColor Cyan
    } else {
        Write-Host "   ? Runner NO configurado" -ForegroundColor Yellow
        Write-Host "     Ejecutar: C:\actions-runner\config.cmd" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? Carpeta C:\actions-runner NO existe" -ForegroundColor Yellow
    Write-Host "     Crear con: New-Item -Path 'C:\actions-runner' -ItemType Directory -Force" -ForegroundColor Yellow
}
Write-Host ""

# 5. Verificar Servicio del Runner
Write-Host "5. Verificando servicio del runner..." -ForegroundColor Yellow
$runnerService = Get-Service | Where-Object {$_.Name -like "actions.runner*"}
if ($runnerService) {
    $serviceName = $runnerService.Name
    Write-Host "   ? Servicio encontrado: $serviceName" -ForegroundColor Green
    
    if ($runnerService.Status -eq "Running") {
        Write-Host "   ? Servicio está corriendo" -ForegroundColor Green
    } else {
        Write-Host "   ? Servicio está detenido" -ForegroundColor Yellow
        Write-Host "     Iniciar con: Start-Service '$serviceName'" -ForegroundColor Yellow
        $allGood = $false
    }
    
    # Verificar tipo de inicio
    $startType = (Get-Service $serviceName).StartType
    if ($startType -eq "Automatic") {
        Write-Host "   ? Inicio automático configurado" -ForegroundColor Green
    } else {
        Write-Host "   ? Inicio NO es automático: $startType" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? Servicio NO instalado" -ForegroundColor Yellow
    Write-Host "     Instalar con: C:\actions-runner\svc.install.cmd" -ForegroundColor Yellow
}
Write-Host ""

# 6. Verificar Conectividad con GitHub
Write-Host "6. Verificando conectividad con GitHub..." -ForegroundColor Yellow
try {
    $connection = Test-NetConnection github.com -Port 443 -WarningAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "   ? Conexión a github.com:443 exitosa" -ForegroundColor Green
    } else {
        Write-Host "   ? No se puede conectar a github.com:443" -ForegroundColor Red
        Write-Host "     Verificar firewall y conexión a Internet" -ForegroundColor Yellow
        $allGood = $false
    }
} catch {
    Write-Host "   ? No se pudo verificar conectividad" -ForegroundColor Yellow
}
Write-Host ""

# 7. Verificar Carpetas de Deployment
Write-Host "7. Verificando carpetas de deployment..." -ForegroundColor Yellow
$publishPath = "C:\Publish\PresupuestoFamiliarApp"
if (Test-Path $publishPath) {
    Write-Host "   ? Carpeta de publicación existe: $publishPath" -ForegroundColor Green
    
    # Verificar permisos
    $acl = Get-Acl $publishPath
    Write-Host "   • Propietario: $($acl.Owner)" -ForegroundColor Cyan
} else {
    Write-Host "   ? Carpeta de publicación NO existe (se creará en el primer deploy)" -ForegroundColor Yellow
}

$backupPath = "C:\Backups"
if (Test-Path $backupPath) {
    Write-Host "   ? Carpeta de backups existe: $backupPath" -ForegroundColor Green
} else {
    Write-Host "   ? Carpeta de backups NO existe (se creará automáticamente)" -ForegroundColor Yellow
}
Write-Host ""

# 8. Verificar IIS App Pool y Sitio
Write-Host "8. Verificando configuración IIS..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    
    $appPoolName = "PresupuestoFamiliarAppPool"
    if (Test-Path "IIS:\AppPools\$appPoolName") {
        Write-Host "   ? App Pool existe: $appPoolName" -ForegroundColor Green
        
        $poolState = (Get-WebAppPoolState -Name $appPoolName).Value
        Write-Host "   • Estado: $poolState" -ForegroundColor Cyan
    } else {
        Write-Host "   ? App Pool NO existe (se debe crear manualmente)" -ForegroundColor Yellow
    }
    
    $siteName = "PresupuestoFamiliarApp"
    if (Test-Path "IIS:\Sites\$siteName") {
        Write-Host "   ? Sitio web existe: $siteName" -ForegroundColor Green
        
        $site = Get-Website -Name $siteName
        Write-Host "   • Ruta física: $($site.PhysicalPath)" -ForegroundColor Cyan
        Write-Host "   • Bindings: $($site.bindings.Collection.bindingInformation)" -ForegroundColor Cyan
        
        $siteState = (Get-WebsiteState -Name $siteName).Value
        Write-Host "   • Estado: $siteState" -ForegroundColor Cyan
    } else {
        Write-Host "   ? Sitio web NO existe (se debe crear manualmente)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ? No se pudo verificar IIS (puede que no esté instalado o no tenga permisos)" -ForegroundColor Yellow
}
Write-Host ""

# 9. Ver Logs Recientes del Runner
Write-Host "9. Logs recientes del runner..." -ForegroundColor Yellow
if (Test-Path "C:\actions-runner\_diag") {
    $latestLog = Get-ChildItem "C:\actions-runner\_diag\Runner_*.log" -ErrorAction SilentlyContinue | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
    
    if ($latestLog) {
        Write-Host "   ? Log encontrado: $($latestLog.Name)" -ForegroundColor Green
        Write-Host "   • Última modificación: $($latestLog.LastWriteTime)" -ForegroundColor Cyan
        
        Write-Host ""
        Write-Host "   Últimas 5 líneas del log:" -ForegroundColor Cyan
        Get-Content $latestLog.FullName -Tail 5 | ForEach-Object {
            Write-Host "   $_" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ? No se encontraron logs del runner" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? Carpeta de diagnóstico NO existe" -ForegroundColor Yellow
}
Write-Host ""

# Resumen Final
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "             RESUMEN" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

if ($allGood) {
    Write-Host ""
    Write-Host "? TODOS LOS REQUISITOS CUMPLIDOS" -ForegroundColor Green
    Write-Host ""
    Write-Host "El runner está listo para recibir trabajos desde GitHub Actions." -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos pasos:" -ForegroundColor Yellow
    Write-Host "1. Verificar en GitHub que el runner aparece en:" -ForegroundColor White
    Write-Host "   Settings > Actions > Runners" -ForegroundColor Gray
    Write-Host "2. Hacer push a tu repositorio para activar el workflow" -ForegroundColor White
    Write-Host "3. Monitorear en GitHub Actions la ejecución del deployment" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "?? HAY ALGUNOS PROBLEMAS QUE RESOLVER" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Revisa los puntos marcados con ? o ? arriba." -ForegroundColor Yellow
    Write-Host "Sigue las instrucciones indicadas para cada problema." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan

# Información adicional útil
Write-Host ""
Write-Host "COMANDOS ÚTILES:" -ForegroundColor Cyan
Write-Host "• Ver estado del servicio:" -ForegroundColor White
Write-Host "  Get-Service | Where-Object {`$_.Name -like '*actions*'}" -ForegroundColor Gray
Write-Host ""
Write-Host "• Ver logs en tiempo real:" -ForegroundColor White
Write-Host "  Get-Content 'C:\actions-runner\_diag\Runner_*.log' -Wait" -ForegroundColor Gray
Write-Host ""
Write-Host "• Reiniciar servicio:" -ForegroundColor White
Write-Host "  Restart-Service (Get-Service | Where-Object {`$_.Name -like '*actions*'}).Name" -ForegroundColor Gray
Write-Host ""
Write-Host "• Verificar conectividad:" -ForegroundColor White
Write-Host "  Test-NetConnection github.com -Port 443" -ForegroundColor Gray
Write-Host ""
