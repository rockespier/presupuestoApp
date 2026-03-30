# ?? Script para Limpiar Service Worker

Write-Host "?? Limpiando Service Worker y caché..." -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Instrucciones para limpiar el Service Worker:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1??  Abre Chrome DevTools (F12)" -ForegroundColor White
Write-Host ""
Write-Host "2??  Ve a la pestaña 'Application'" -ForegroundColor White
Write-Host ""
Write-Host "3??  En el panel izquierdo, busca 'Service Workers'" -ForegroundColor White
Write-Host ""
Write-Host "4??  Haz clic en 'Unregister' en el service worker activo" -ForegroundColor White
Write-Host ""
Write-Host "5??  Ve a 'Storage' ? 'Clear site data'" -ForegroundColor White
Write-Host ""
Write-Host "6??  Marca todas las opciones y haz clic en 'Clear site data'" -ForegroundColor White
Write-Host ""
Write-Host "7??  Cierra todas las pestañas de la aplicación" -ForegroundColor White
Write-Host ""
Write-Host "8??  Vuelve a abrir la aplicación (Ctrl+F5 para forzar recarga)" -ForegroundColor White
Write-Host ""

Write-Host "? Alternativa Rápida:" -ForegroundColor Green
Write-Host "   1. Abre la aplicación" -ForegroundColor White
Write-Host "   2. Presiona: Ctrl + Shift + Delete" -ForegroundColor White
Write-Host "   3. Selecciona 'Todo el tiempo' y 'Archivos e imágenes almacenados en caché'" -ForegroundColor White
Write-Host "   4. Haz clic en 'Borrar datos'" -ForegroundColor White
Write-Host "   5. Recarga la página (Ctrl + F5)" -ForegroundColor White
Write-Host ""

$continuar = Read-Host "¿Ejecutar la aplicación ahora? (S/N)"

if ($continuar -eq "S" -or $continuar -eq "s") {
    Write-Host ""
    Write-Host "?? Iniciando aplicación..." -ForegroundColor Green
    Write-Host "   Recuerda limpiar el service worker siguiendo los pasos anteriores" -ForegroundColor Yellow
    Write-Host ""
    
    Start-Process "https://localhost:7036/Transacciones/TestOcr"
    
    Set-Location -Path "src"
    dotnet run
}

Write-Host ""
Write-Host "? ¡Listo!" -ForegroundColor Green
Write-Host "   El service worker ha sido actualizado en el código." -ForegroundColor White
Write-Host "   Sigue los pasos para limpiar la caché en el navegador." -ForegroundColor White
Write-Host ""
