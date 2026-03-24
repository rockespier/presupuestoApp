# ? Correcciones Aplicadas al Service Worker

## ?? **Problemas Identificados**

### **1. Error en Service Worker (Línea 30)**
```
Uncaught (in promise) TypeError: Failed to fetch at service-worker.js:30:30
```

**Causa**: El Service Worker intentaba cachear `https://cdn.tailwindcss.com` durante la instalación, pero fallaba por CORS.

---

## ? **Soluciones Aplicadas**

### **1. Eliminado Tailwind CDN del Pre-Cache**
**Antes:**
```javascript
const PRECACHE_URLS = [
    '/',
    '/Home/Index',
    '/Auth/Login',
    '/css/site.css',
    '/js/site.js',
    'https://cdn.tailwindcss.com'  // ? Causaba error CORS
];
```

**Después:**
```javascript
const PRECACHE_URLS = [
    '/',
    '/Home/Index',
    '/Auth/Login',
    '/manifest.json',
    '/icons/icon-192x192.png',
    '/icons/icon-512x512.png'
];
```

### **2. Mejorado el Manejo de Errores**
**Antes:**
```javascript
return cache.addAll(PRECACHE_URLS);  // ? Si falla uno, fallan todos
```

**Después:**
```javascript
return Promise.allSettled(
    PRECACHE_URLS.map(url => 
        cache.add(url).catch(err => {
            console.warn(`[Service Worker] No se pudo cachear: ${url}`, err);
        })
    )
);
```

### **3. Protección contra Recursos Externos con CORS**
Agregado en `cacheFirst()`:
```javascript
const url = new URL(request.url);
if (url.origin !== location.origin && !url.origin.includes('localhost')) {
    try {
        return await fetch(request);
    } catch (error) {
        console.warn('[Service Worker] Error al cargar recurso externo:', url.href);
        return new Response('', { status: 503, statusText: 'External resource unavailable' });
    }
}
```

### **4. Actualizada la Versión del Caché**
```javascript
const CACHE_NAME = 'presupuesto-app-v13';  // Incrementado de v12 a v13
const RUNTIME_CACHE = 'presupuesto-runtime-v13';
```

---

## ?? **Pasos para Aplicar los Cambios**

### **1. Desregistrar el Service Worker Anterior**

1. Abre DevTools (F12)
2. Ve a **Application** ? **Service Workers**
3. Haz clic en **"Unregister"** en todos los service workers
4. Cierra la pestaña de DevTools

### **2. Limpiar Caché del Navegador**

**Opción A: Caché Completa**
1. Presiona `Ctrl + Shift + Delete`
2. Marca "Archivos e imágenes en caché"
3. Haz clic en "Borrar datos"

**Opción B: Solo Service Worker Cache**
1. F12 ? Application ? Cache Storage
2. Haz clic derecho en cada caché y selecciona "Delete"

### **3. Reiniciar la Aplicación**

```bash
# Detener el servidor
Ctrl + C

# Volver a iniciar
dotnet run
```

### **4. Recargar la Página**

```
Ctrl + Shift + R  (recarga forzada sin caché)
```

---

## ?? **Verificación**

### **1. Verifica en la Consola del Navegador (F12 ? Console)**

Deberías ver:
```javascript
[Service Worker] Cargado exitosamente
[Service Worker] Instalando...
[Service Worker] Pre-caching archivos
[Service Worker] Pre-cache completado
?? Inicializando Push Notification Manager...
?? Iniciando sistema de notificaciones push...
? Navegador compatible con Push API
? Service Worker listo: activated
?? Obteniendo clave pública VAPID...
```

### **2. Verifica el Service Worker (F12 ? Application ? Service Workers)**

- Estado: **activated and is running**
- No debe haber errores en rojo

### **3. Verifica el Estado de Notificaciones**

En `/Configuracion` deberías ver:
- **"?? Notificaciones desactivadas"** (si aún no las has activado)
- O **"? Notificaciones activadas"** (si ya las activaste)

**Ya NO debería decir "Verificando..."**

---

## ?? **Logs Esperados**

### **? Logs Correctos (Sin Errores)**

```javascript
[Service Worker] Cargado exitosamente
[Service Worker] Instalando...
[Service Worker] Pre-caching archivos
[Service Worker] Pre-cache completado
[Service Worker] Evento de instalación detectado
?? Inicializando Push Notification Manager...
?? Iniciando sistema de notificaciones push...
? Navegador compatible con Push API
? Service Worker listo: activated
?? Obteniendo clave pública VAPID...
? Clave pública VAPID obtenida: BEl62iUYgUivxIkv69y...
?? Usuario no suscrito a notificaciones
? Sistema de notificaciones inicializado correctamente
```

### **? Logs Incorrectos (Con Errores)**

Si aún ves:
```
Uncaught (in promise) TypeError: Failed to fetch
```

**Solución**:
1. Verifica que desregistraste el service worker anterior
2. Limpia la caché completamente
3. Cierra y vuelve a abrir el navegador
4. Vuelve a la página

---

## ?? **Próximos Pasos**

Una vez que el Service Worker esté funcionando correctamente (sin errores en la consola), el siguiente paso es:

### **Verificar el Endpoint del Backend**

Abre en el navegador:
```
https://localhost:7036/api/push/public-key
```

**Resultado Esperado:**
```json
{
  "publicKey": "BEl62iUYgUivxIkv69yViEuiBIa..."
}
```

**Si ves error 500:**
- La tabla `PushSubscriptions` no existe
- Ejecuta: `dotnet ef migrations add AddPushSubscriptions`
- Luego: `dotnet ef database update`

---

## ?? **Resumen de Cambios**

| Archivo | Cambios |
|---------|---------|
| `service-worker.js` | ? Eliminado Tailwind CDN del pre-cache |
| `service-worker.js` | ? Mejorado manejo de errores con `Promise.allSettled` |
| `service-worker.js` | ? Agregada protección contra CORS en recursos externos |
| `service-worker.js` | ? Actualizada versión del caché a v13 |
| `push-manager.js` | ? Corregido nombre de clase (PushNotificationManager) |
| `push-manager.js` | ? Corregido error de sintaxis en `urlBase64ToUint8Array` |

---

## ? **Checklist Final**

Antes de continuar, verifica:

- [ ] Service Worker desregistrado
- [ ] Caché del navegador limpiada
- [ ] Aplicación reiniciada (`dotnet run`)
- [ ] Página recargada con `Ctrl + Shift + R`
- [ ] Consola del navegador sin errores rojos
- [ ] Service Worker muestra estado "activated"
- [ ] Estado de notificaciones ya NO dice "Verificando..."

---

## ?? **Resultado Esperado**

En `/Configuracion` deberías ver:

```
??????????????????????????????????????
?  Estado de Notificaciones          ?
?  ?? Notificaciones desactivadas    ?
?                                    ?
?  [Botón: Activar Notificaciones]  ?
??????????????????????????????????????
```

¡Si ves esto, el sistema está funcionando correctamente! ??

El siguiente paso será crear la migración de la base de datos si aún no existe la tabla `PushSubscriptions`.
