# Script de Diagnostico y Reparacion del Runner
# EJECUTAR EN EL SERVIDOR IIS (161.132.56.79) como Administrador

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   DIAGNOSTICO DEL GITHUB RUNNER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar dotnet
Write-Host "1. Verificando instalacion de .NET..." -ForegroundColor Yellow
$dotnetExe = "C:\Program Files\dotnet\dotnet.exe"

if (Test-Path $dotnetExe) {
    Write-Host "   [OK] dotnet encontrado" -ForegroundColor Green
    try {
        $version = & $dotnetExe --version
        Write-Host "   Version: $version" -ForegroundColor Cyan
        
        Write-Host ""
        Write-Host "   Runtimes instalados:" -ForegroundColor Cyan
        & $dotnetExe --list-runtimes
    } catch {
        Write-Host "   [ERROR] No se pudo ejecutar dotnet" -ForegroundColor Red
        Write-Host "   Error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "   [ERROR] dotnet NO encontrado en: $dotnetExe" -ForegroundColor Red
    Write-Host "   ACCION REQUERIDA: Instalar .NET 9.0 Hosting Bundle" -ForegroundColor Yellow
    Write-Host "   Descargar de: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "2. Verificando PATH del sistema..." -ForegroundColor Yellow
$systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$dotnetDir = "C:\Program Files\dotnet"

if ($systemPath -like "*$dotnetDir*") {
    Write-Host "   [OK] dotnet esta en el PATH del sistema" -ForegroundColor Green
} else {
    Write-Host "   [PROBLEMA] dotnet NO esta en el PATH del sistema" -ForegroundColor Red
    Write-Host "   Agregando dotnet al PATH..." -ForegroundColor Yellow
    
    try {
        $newPath = "$systemPath;$dotnetDir"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
        Write-Host "   [OK] dotnet agregado al PATH del sistema" -ForegroundColor Green
        Write-Host "   NOTA: Es necesario reiniciar el servicio del runner" -ForegroundColor Yellow
    } catch {
        Write-Host "   [ERROR] No se pudo agregar al PATH: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "3. Verificando carpeta del runner..." -ForegroundColor Yellow
$runnerPath = "C:\actions-runner"

if (Test-Path $runnerPath) {
    Write-Host "   [OK] Carpeta del runner encontrada: $runnerPath" -ForegroundColor Green
    
    # Verificar archivos del runner
    $configFile = Join-Path $runnerPath ".runner"
    $serviceFile = Join-Path $runnerPath "svc.sh"
    
    if (Test-Path $configFile) {
        Write-Host "   [OK] Runner configurado (.runner existe)" -ForegroundColor Green
    } else {
        Write-Host "   [PROBLEMA] Runner no esta configurado" -ForegroundColor Red
        Write-Host "   ACCION REQUERIDA: Ejecutar ./config.cmd" -ForegroundColor Yellow
    }
} else {
    Write-Host "   [ERROR] Carpeta del runner NO encontrada" -ForegroundColor Red
    Write-Host "   Ruta esperada: $runnerPath" -ForegroundColor Yellow
    Write-Host "   ACCION REQUERIDA: Instalar el runner" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "4. Verificando servicio del runner..." -ForegroundColor Yellow

$runnerService = Get-Service -Name "*actions*" -ErrorAction SilentlyContinue

if ($runnerService) {
    Write-Host "   [OK] Servicio encontrado: $($runnerService.Name)" -ForegroundColor Green
    Write-Host "   Estado: $($runnerService.Status)" -ForegroundColor Cyan
    Write-Host "   Tipo de inicio: $($runnerService.StartType)" -ForegroundColor Cyan
    
    if ($runnerService.Status -ne "Running") {
        Write-Host "   [PROBLEMA] El servicio no esta corriendo" -ForegroundColor Red
    }
} else {
    Write-Host "   [ERROR] Servicio del runner NO encontrado" -ForegroundColor Red
    Write-Host "   ACCION REQUERIDA: Instalar el servicio con ./svc.install.cmd" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Verificando conectividad con GitHub..." -ForegroundColor Yellow

try {
    $github = Test-NetConnection -ComputerName "github.com" -Port 443 -ErrorAction Stop
    if ($github.TcpTestSucceeded) {
        Write-Host "   [OK] Conectividad con GitHub OK" -ForegroundColor Green
    } else {
        Write-Host "   [PROBLEMA] No se puede conectar a GitHub:443" -ForegroundColor Red
    }
} catch {
    Write-Host "   [ERROR] Error al probar conectividad: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   RESUMEN DEL DIAGNOSTICO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()

if (!(Test-Path $dotnetExe)) {
    $issues += "- dotnet NO instalado"
}

if ($systemPath -notlike "*$dotnetDir*") {
    $issues += "- dotnet NO esta en el PATH"
}

if (!(Test-Path $runnerPath)) {
    $issues += "- Runner NO instalado"
}

if (!$runnerService) {
    $issues += "- Servicio del runner NO instalado"
} elseif ($runnerService.Status -ne "Running") {
    $issues += "- Servicio del runner NO esta corriendo"
}

if ($issues.Count -eq 0) {
    Write-Host "TODO ESTA CONFIGURADO CORRECTAMENTE" -ForegroundColor Green
    Write-Host ""
    Write-Host "Si el workflow sigue fallando:" -ForegroundColor Yellow
    Write-Host "1. Reinicia el servicio del runner" -ForegroundColor White
    Write-Host "2. Verifica los logs en GitHub Actions" -ForegroundColor White
} else {
    Write-Host "SE ENCONTRARON LOS SIGUIENTES PROBLEMAS:" -ForegroundColor Red
    Write-Host ""
    foreach ($issue in $issues) {
        Write-Host $issue -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "RECOMENDACION: Ejecutar el script de reparacion automatica" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Preguntar si quiere reparar automaticamente
$repair = Read-Host "Deseas intentar reparar automaticamente? (S/N)"

if ($repair -eq "S" -or $repair -eq "s") {
    Write-Host ""
    Write-Host "Iniciando reparacion automatica..." -ForegroundColor Cyan
    Write-Host ""
    
    # Agregar dotnet al PATH si no esta
    if ($systemPath -notlike "*$dotnetDir*") {
        Write-Host "Agregando dotnet al PATH del sistema..." -ForegroundColor Yellow
        try {
            $newPath = "$systemPath;$dotnetDir"
            [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
            Write-Host "[OK] dotnet agregado al PATH" -ForegroundColor Green
        } catch {
            Write-Host "[ERROR] No se pudo agregar al PATH: $_" -ForegroundColor Red
        }
    }
    
    # Reiniciar servicio del runner si existe
    if ($runnerService -and (Test-Path $runnerPath)) {
        Write-Host ""
        Write-Host "Reiniciando servicio del runner..." -ForegroundColor Yellow
        
        try {
            Push-Location $runnerPath
            
            if (Test-Path ".\svc.stop.cmd") {
                Write-Host "Deteniendo servicio..." -ForegroundColor Yellow
                cmd /c ".\svc.stop.cmd"
                Start-Sleep -Seconds 3
                Write-Host "[OK] Servicio detenido" -ForegroundColor Green
            }
            
            if (Test-Path ".\svc.start.cmd") {
                Write-Host "Iniciando servicio..." -ForegroundColor Yellow
                cmd /c ".\svc.start.cmd"
                Start-Sleep -Seconds 3
                
                $service = Get-Service -Name "*actions*" -ErrorAction SilentlyContinue
                if ($service -and $service.Status -eq "Running") {
                    Write-Host "[OK] Servicio iniciado correctamente" -ForegroundColor Green
                } else {
                    Write-Host "[PROBLEMA] El servicio no inicio correctamente" -ForegroundColor Red
                }
            }
            
            Pop-Location
        } catch {
            Write-Host "[ERROR] Error al reiniciar el servicio: $_" -ForegroundColor Red
            Pop-Location
        }
    }
    
    Write-Host ""
    Write-Host "Reparacion completada" -ForegroundColor Cyan
    Write-Host "Intenta hacer push nuevamente desde tu PC local" -ForegroundColor White
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
