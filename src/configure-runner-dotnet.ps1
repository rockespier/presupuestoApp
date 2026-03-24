# Script para Configurar dotnet en el PATH del Runner
# Ejecutar EN EL SERVIDOR IIS (161.132.56.79) como Administrador

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION DE DOTNET PARA RUNNER" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que dotnet está instalado
Write-Host "1. Verificando instalacion de .NET..." -ForegroundColor Yellow
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"

if (Test-Path $dotnetPath) {
    Write-Host "   .NET encontrado en: $dotnetPath" -ForegroundColor Green
    
    # Mostrar versión
    & $dotnetPath --version
    Write-Host ""
    
    # Mostrar runtimes
    Write-Host "   Runtimes instalados:" -ForegroundColor Cyan
    & $dotnetPath --list-runtimes
} else {
    Write-Host "   ERROR: .NET no esta instalado en la ruta esperada" -ForegroundColor Red
    Write-Host "   Instala .NET 9.0 Hosting Bundle desde:" -ForegroundColor Yellow
    Write-Host "   https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "2. Verificando PATH del sistema..." -ForegroundColor Yellow

# Obtener PATH actual del sistema
$systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$dotnetDir = "C:\Program Files\dotnet"

if ($systemPath -like "*$dotnetDir*") {
    Write-Host "   .NET ya esta en el PATH del sistema" -ForegroundColor Green
} else {
    Write-Host "   .NET NO esta en el PATH del sistema" -ForegroundColor Yellow
    Write-Host "   Agregando .NET al PATH..." -ForegroundColor Yellow
    
    # Agregar al PATH del sistema
    $newPath = "$systemPath;$dotnetDir"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "Machine")
    
    Write-Host "   .NET agregado al PATH del sistema" -ForegroundColor Green
}

Write-Host ""
Write-Host "3. Verificando servicio del runner..." -ForegroundColor Yellow

# Buscar el servicio del runner
$runnerService = Get-Service | Where-Object {$_.Name -like "*actions*"}

if ($runnerService) {
    Write-Host "   Servicio encontrado: $($runnerService.Name)" -ForegroundColor Green
    Write-Host "   Estado actual: $($runnerService.Status)" -ForegroundColor Cyan
    
    # Detener el servicio si está corriendo
    if ($runnerService.Status -eq "Running") {
        Write-Host ""
        Write-Host "4. Deteniendo servicio del runner..." -ForegroundColor Yellow
        
        cd C:\actions-runner
        .\svc.stop.cmd
        
        Start-Sleep -Seconds 3
        Write-Host "   Servicio detenido" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "5. Iniciando servicio del runner..." -ForegroundColor Yellow
    
    cd C:\actions-runner
    .\svc.start.cmd
    
    Start-Sleep -Seconds 3
    
    # Verificar que inició correctamente
    $runnerService = Get-Service | Where-Object {$_.Name -like "*actions*"}
    if ($runnerService.Status -eq "Running") {
        Write-Host "   Servicio iniciado correctamente" -ForegroundColor Green
    } else {
        Write-Host "   Advertencia: El servicio no esta corriendo" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ERROR: No se encontro el servicio del runner" -ForegroundColor Red
    Write-Host "   Asegurate de que el runner esta instalado y configurado" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "6. Verificando acceso a dotnet desde el runner..." -ForegroundColor Yellow

# Crear un script de prueba
$testScript = @"
`$env:PATH = 'C:\Program Files\dotnet;' + `$env:PATH
& 'C:\Program Files\dotnet\dotnet.exe' --version
"@

$testScriptPath = "C:\actions-runner\test-dotnet.ps1"
Set-Content -Path $testScriptPath -Value $testScript

Write-Host "   Script de prueba creado: $testScriptPath" -ForegroundColor Cyan
Write-Host "   El runner ahora deberia poder ejecutar comandos dotnet" -ForegroundColor Green

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   CONFIGURACION COMPLETADA" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Yellow
Write-Host "1. Haz push de tus cambios a GitHub" -ForegroundColor White
Write-Host "2. El workflow deberia ejecutarse correctamente ahora" -ForegroundColor White
Write-Host "3. Si aun hay problemas, revisa los logs en GitHub Actions" -ForegroundColor White
Write-Host ""
Write-Host "PATH actual del sistema:" -ForegroundColor Cyan
Write-Host ([Environment]::GetEnvironmentVariable("Path", "Machine")) -ForegroundColor Gray
Write-Host ""
