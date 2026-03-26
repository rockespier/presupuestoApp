# ?? Progressive Web App (PWA) - Presupuesto Familiar App

## ? ¿Qué es una PWA?

Una **Progressive Web App** es una aplicación web que se puede instalar en dispositivos como si fuera una app nativa, pero sin necesidad de descargarla desde una tienda de aplicaciones (App Store o Google Play).

### Beneficios de PWA:
- ? **Instalable**: Se agrega al menú de inicio como una app nativa
- ?? **Rápida**: Carga más rápido gracias al caché
- ?? **Funciona Offline**: Acceso básico sin conexión a internet
- ?? **Notificaciones**: Puede enviar notificaciones push (opcional)
- ?? **Actualización Automática**: Se actualiza en segundo plano
- ??? **Multiplataforma**: Funciona en Windows, Mac, Android, iOS
- ?? **Sin costos**: No necesitas pagar por publicar en tiendas

---

## ?? Configuración Inicial

### 1. Generar Iconos

Los iconos son **esenciales** para que la PWA funcione. Sigue la guía en:
```
wwwroot/icons/README.md
```

**Iconos mínimos requeridos:**
- `wwwroot/icons/icon-192x192.png`
- `wwwroot/icons/icon-512x512.png`

### 2. Verificar Archivos PWA

Asegúrate de que estos archivos existen en tu proyecto:

```
wwwroot/
??? manifest.json          ? (Configuración de la PWA)
??? service-worker.js      ? (Caché y offline)
??? offline.html           ? (Página sin conexión)
??? js/
?   ??? pwa-installer.js   ? (Script de instalación)
??? icons/
    ??? icon-72x72.png
    ??? icon-96x96.png
    ??? icon-128x128.png
    ??? icon-144x144.png
    ??? icon-152x152.png
    ??? icon-192x192.png   ? IMPORTANTE
    ??? icon-384x384.png
    ??? icon-512x512.png   ? IMPORTANTE
```

### 3. Configurar HTTPS (Requerido)

Las PWA **solo funcionan con HTTPS** (excepto en localhost para desarrollo).

#### En Desarrollo (Localhost):
```bash
dotnet run --urls=https://localhost:5001
```

#### En Producción:
- Usa un certificado SSL válido
- Servicios como Let's Encrypt ofrecen SSL gratuito
- Hospedajes como Azure, AWS, Heroku incluyen SSL

---

## ?? Cómo Instalar la App

### En Windows/Mac (Escritorio):

1. Abre la app en Chrome, Edge o Brave
2. Mira la barra de direcciones (esquina derecha)
3. Haz clic en el icono de instalación (? o ?)
4. Click en "Instalar"
5. ¡Listo! La app aparecerá en tu escritorio

### En Android:

1. Abre la app en Chrome
2. Aparecerá un banner en la parte inferior: **"Agregar a la pantalla de inicio"**
3. Toca "Agregar"
4. La app se instalará en tu pantalla de inicio
5. Ábrela como cualquier otra app

### En iOS (iPhone/iPad):

?? iOS tiene soporte limitado de PWA, pero funciona:

1. Abre la app en Safari (debe ser Safari, no Chrome)
2. Toca el botón de compartir (cuadrado con flecha hacia arriba)
3. Desplázate y toca **"Agregar a la pantalla de inicio"**
4. Personaliza el nombre y toca "Agregar"
5. La app aparecerá en tu pantalla de inicio

**Nota**: iOS no soporta todas las funciones PWA (notificaciones push, background sync), pero la instalación y offline sí funcionan.

---

## ?? Personalización

### Modificar Colores y Nombres

Edita `wwwroot/manifest.json`:

```json
{
  "name": "Tu Nombre de App",              // Nombre completo
  "short_name": "AppCorto",                 // Nombre corto (máx 12 caracteres)
  "description": "Tu descripción",
  "theme_color": "#0ea5e9",                 // Color de la barra de navegación
  "background_color": "#667eea",            // Color de fondo al abrir
  "display": "standalone"                   // Modo de visualización
}
```

### Modos de Visualización (`display`):

- **`standalone`** ? (Recomendado): Se ve como app nativa, sin barra del navegador
- **`fullscreen`**: Ocupa toda la pantalla (como un juego)
- **`minimal-ui`**: Mínimos controles de navegación
- **`browser`**: Se abre en el navegador normal

### Agregar Atajos Rápidos

Los atajos permiten acciones rápidas desde el icono de la app (click derecho):

```json
"shortcuts": [
  {
    "name": "Nueva Transacción",
    "url": "/Transacciones/Create",
    "icons": [{ "src": "/icons/shortcut-transaction.png", "sizes": "96x96" }]
  }
]
```

---

## ??? Estrategias de Caché

El Service Worker usa 3 estrategias automáticas:

### 1. **Cache First** (Assets estáticos):
- CSS, JavaScript, imágenes, fonts
- Se carga del caché si existe, sino de la red

### 2. **Network First** (API/Datos dinámicos):
- Transacciones, cuentas, categorías
- Intenta red primero, usa caché como respaldo

### 3. **Stale While Revalidate** (Páginas HTML):
- Muestra contenido en caché inmediatamente
- Actualiza en segundo plano

---

## ?? Funcionamiento Offline

### ¿Qué funciona sin internet?

? **Navegación básica**: Puedes ver páginas ya visitadas  
? **Lectura de datos**: Datos en caché siguen disponibles  
? **Interfaz completa**: Diseño y estilos funcionan  

? **No funciona**:
- Crear/editar transacciones nuevas
- Actualizar saldos en tiempo real
- Login/Logout
- Sincronización con servidor

### Página de Offline

Si navegas sin conexión a una página no cacheada, verás:
```
wwwroot/offline.html
```

Personalízala editando ese archivo.

---

## ?? Notificaciones Push (Opcional)

Para habilitar notificaciones:

### 1. Solicitar Permisos:

```javascript
await window.PWAInstaller.requestNotifications();
```

### 2. Enviar Notificación Local:

```javascript
await window.PWAInstaller.sendNotification('Nuevo Movimiento', {
    body: 'Se registró un gasto de S/ 150.00',
    icon: '/icons/icon-192x192.png',
    badge: '/icons/badge-72x72.png',
    tag: 'transaction-alert',
    requireInteraction: false,
    actions: [
        { action: 'view', title: 'Ver Detalle' },
        { action: 'dismiss', title: 'Descartar' }
    ]
});
```

### 3. Para Notificaciones desde el Servidor:

Necesitarás implementar **Web Push Protocol** con VAPID keys. Consulta la documentación de ASP.NET Core Push Notifications.

---

## ?? Testing y Validación

### 1. Chrome DevTools

1. Abre DevTools (F12)
2. Ve a la pestaña **Application**
3. Revisa:
   - **Manifest**: Verifica configuración y iconos
   - **Service Workers**: Estado del SW
   - **Storage ? Cache Storage**: Archivos cacheados
   - **Offline**: Simula sin conexión

### 2. Lighthouse Audit

1. F12 ? **Lighthouse**
2. Selecciona **Progressive Web App**
3. Click en **Generate Report**
4. Objetivo: **90+ puntos** ?

### 3. PWA Builder

https://www.pwabuilder.com/
- Ingresa tu URL
- Analiza tu PWA
- Te da recomendaciones

---

## ?? Métricas PWA

### Puntuación Lighthouse Objetivo:

| Categoría | Objetivo | Actual |
|-----------|----------|--------|
| PWA Score | 90+ | ????? |
| Performance | 85+ | - |
| Accessibility | 90+ | - |
| Best Practices | 90+ | - |
| SEO | 90+ | - |

---

## ?? Troubleshooting

### "No se muestra el botón de instalación"

**Posibles causas:**
1. ? No estás usando HTTPS (excepto localhost)
2. ? Faltan iconos obligatorios (192px y 512px)
3. ? El manifest.json tiene errores de sintaxis
4. ? El Service Worker no se registró correctamente
5. ? Ya instalaste la app previamente

**Solución:**
```javascript
// Abre la consola y verifica:
console.log('Service Worker:', navigator.serviceWorker);
console.log('Manifest:', document.querySelector('link[rel="manifest"]'));
```

### "La app no funciona offline"

1. Verifica que el Service Worker esté activo:
   - F12 ? Application ? Service Workers
   - Estado debe ser "activated and running"

2. Revisa la caché:
   - F12 ? Application ? Cache Storage
   - Debe haber archivos cacheados

3. Fuerza actualización:
   - En Service Workers, click en "Update"
   - O desregistra y registra nuevamente

### "Los iconos no se ven"

1. Verifica que los archivos existen físicamente
2. Revisa las rutas en manifest.json
3. Verifica que los tamaños coinciden
4. Usa PNG (no JPG)

### "Error en iOS"

iOS tiene limitaciones:
- Solo funciona en Safari (no Chrome/Firefox)
- No soporta notificaciones push
- Caché limitada a 50MB
- Se limpia después de 2 semanas sin usar

---

## ?? Recursos Adicionales

### Documentación Oficial:
- [MDN - Progressive Web Apps](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps)
- [Google - PWA Checklist](https://web.dev/pwa-checklist/)
- [Microsoft - PWA Builder](https://www.pwabuilder.com/)

### Herramientas:
- [Workbox](https://developers.google.com/web/tools/workbox) - Biblioteca para Service Workers
- [PWA Asset Generator](https://github.com/onderceylan/pwa-asset-generator)
- [RealFaviconGenerator](https://realfavicongenerator.net/)

### Ejemplos:
- [Twitter PWA](https://mobile.twitter.com)
- [Starbucks PWA](https://app.starbucks.com)
- [Uber PWA](https://m.uber.com)

---

## ?? Siguiente Nivel

### Funciones Avanzadas para Implementar:

1. **Background Sync**
   - Sincronizar transacciones cuando vuelve la conexión
   - Enviar datos pendientes automáticamente

2. **Periodic Background Sync**
   - Actualizar datos cada X horas en segundo plano
   - Notificar sobre próximos vencimientos

3. **Share Target API**
   - Compartir capturas a la app desde otras aplicaciones
   - Convertir imágenes de tickets en transacciones

4. **Web Bluetooth**
   - Conectar con dispositivos bluetooth
   - Leer datos de tarjetas NFC

5. **File System Access API**
   - Exportar/importar datos
   - Guardar reportes directamente

---

## ? Checklist de Implementación

- [x] Crear manifest.json
- [x] Crear service-worker.js
- [x] Crear pwa-installer.js
- [x] Agregar meta tags PWA al layout
- [x] Crear página offline
- [ ] Generar todos los iconos necesarios
- [ ] Probar instalación en Chrome Desktop
- [ ] Probar instalación en Android
- [ ] Probar instalación en iOS Safari
- [ ] Ejecutar Lighthouse audit
- [ ] Verificar funcionamiento offline
- [ ] (Opcional) Configurar notificaciones push
- [ ] (Opcional) Implementar background sync

---

## ?? ¡Listo para Producción!

Tu app ahora es una **Progressive Web App** completa que los usuarios pueden:
- ?? Instalar en sus dispositivos
- ?? Usar como app nativa
- ?? Acceder sin conexión (parcialmente)
- ?? Recibir notificaciones (si implementas)

**¡Felicitaciones! Tu app está al nivel de Twitter, Instagram y Uber en tecnología PWA.** ??
