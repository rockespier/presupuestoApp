# ?? Script de Migración a Azure OCR

Write-Host "?? Iniciando migración a Azure Computer Vision OCR..." -ForegroundColor Cyan

# 1. Verificar que estamos en el directorio correcto
if (!(Test-Path "src/PresupuestoFamiliarApp.csproj")) {
    Write-Host "? Error: Ejecuta este script desde la raíz del proyecto" -ForegroundColor Red
    exit 1
}

# 2. Verificar configuración de Azure
Write-Host "`n?? Verificando configuración de Azure..." -ForegroundColor Yellow

$appSettings = Get-Content "src/appsettings.json" | ConvertFrom-Json

if ($null -eq $appSettings.AzureComputerVision) {
    Write-Host "??  AzureComputerVision no encontrado en appsettings.json" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? Necesitas agregar esta sección a appsettings.json:" -ForegroundColor Cyan
    Write-Host @"
{
  "AzureComputerVision": {
    "Endpoint": "https://TU-RECURSO.cognitiveservices.azure.com/",
    "Key": "TU-CLAVE-DE-SUSCRIPCION"
  }
}
"@ -ForegroundColor White
    
    $response = Read-Host "`n¿Ya tienes credenciales de Azure? (S/N)"
    if ($response -eq "N" -or $response -eq "n") {
        Write-Host ""
        Write-Host "?? Crea tu recurso gratuito aquí:" -ForegroundColor Green
        Write-Host "   https://portal.azure.com" -ForegroundColor White
        Write-Host ""
        Write-Host "?? Guía completa en: src/Documentacion/MIGRACION-AZURE-OCR-V3.md" -ForegroundColor Cyan
        exit 0
    }
    
    # Pedir credenciales
    Write-Host ""
    $endpoint = Read-Host "Ingresa tu Azure Endpoint"
    $key = Read-Host "Ingresa tu Azure Key" -AsSecureString
    $keyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($key))
    
    # Agregar a appsettings.json
    $appSettings | Add-Member -Type NoteProperty -Name "AzureComputerVision" -Value @{
        Endpoint = $endpoint
        Key = $keyPlain
    }
    
    $appSettings | ConvertTo-Json -Depth 10 | Set-Content "src/appsettings.json"
    Write-Host "? Configuración agregada a appsettings.json" -ForegroundColor Green
} else {
    Write-Host "? Configuración de Azure encontrada" -ForegroundColor Green
    Write-Host "   Endpoint: $($appSettings.AzureComputerVision.Endpoint)" -ForegroundColor White
}

# 3. Verificar que AzureOcrService existe
Write-Host "`n?? Verificando archivos..." -ForegroundColor Yellow

if (!(Test-Path "src/Servicios/AzureOcrService.cs")) {
    Write-Host "? AzureOcrService.cs no encontrado" -ForegroundColor Red
    Write-Host "   Copia el archivo desde la documentación" -ForegroundColor Yellow
    exit 1
}
Write-Host "? AzureOcrService.cs encontrado" -ForegroundColor Green

# 4. Compilar proyecto
Write-Host "`n?? Compilando proyecto..." -ForegroundColor Yellow
$buildResult = dotnet build src/PresupuestoFamiliarApp.csproj 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Error de compilación" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "? Compilación exitosa" -ForegroundColor Green

# 5. Verificar que appsettings.json está en .gitignore
Write-Host "`n?? Verificando seguridad..." -ForegroundColor Yellow

if (Test-Path ".gitignore") {
    $gitignore = Get-Content ".gitignore"
    if ($gitignore -notcontains "appsettings.json") {
        Write-Host "??  Agregando appsettings.json a .gitignore..." -ForegroundColor Yellow
        Add-Content ".gitignore" "`nappsettings.json`nappsettings.*.json"
        Write-Host "? .gitignore actualizado" -ForegroundColor Green
    } else {
        Write-Host "? appsettings.json ya está en .gitignore" -ForegroundColor Green
    }
}

# 6. Test de conexión a Azure (opcional)
Write-Host "`n?? ¿Deseas probar la conexión con Azure? (S/N)" -ForegroundColor Cyan
$testResponse = Read-Host

if ($testResponse -eq "S" -or $testResponse -eq "s") {
    Write-Host ""
    Write-Host "?? Ejecutando aplicación..." -ForegroundColor Yellow
    Write-Host "   Ve a: https://localhost:5001/Transacciones/TestOcr" -ForegroundColor Cyan
    Write-Host "   Sube un ticket y verifica los resultados" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Presiona Ctrl+C para detener" -ForegroundColor Yellow
    
    Start-Process "https://localhost:5001/Transacciones/TestOcr"
    dotnet run --project src/PresupuestoFamiliarApp.csproj
}

Write-Host ""
Write-Host "? ¡Migración completada!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Ejecutar: dotnet run --project src" -ForegroundColor White
Write-Host "   2. Ir a: https://localhost:5001/Transacciones/TestOcr" -ForegroundColor White
Write-Host "   3. Subir imagen de ticket" -ForegroundColor White
Write-Host "   4. Verificar resultados (95%+ precisión)" -ForegroundColor White
Write-Host ""
Write-Host "?? Documentación completa: src/Documentacion/MIGRACION-AZURE-OCR-V3.md" -ForegroundColor Cyan
Write-Host ""
