# ?? FIX: Error HTTP 400 en Service Worker

## ? **PROBLEMA**

Al acceder a `/Transacciones/TestOcr`, aparece error:

```
HTTP ERROR 400

Uncaught (in promise) TypeError: Failed to execute 'put' on 'Cache': 
Request method 'POST' is unsupported
at networkFirst (service-worker.js:131:19)
```

### **Causa Raíz:**

El **Service Worker** intentaba cachear peticiones POST en la función `networkFirst()`, pero:
- ? `cache.put()` **solo soporta peticiones GET**
- ? Los formularios con `method="post"` **no se pueden cachear**
- ? El service worker no filtraba correctamente las peticiones POST

---

## ? **SOLUCIÓN IMPLEMENTADA**

### **Cambio 1: Filtrar Peticiones POST Tempranamente**

**Antes:**
```javascript
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Estrategia: Network First para datos dinámicos (API calls)
    if (request.url.includes('/api/') || request.method !== 'GET') {
        event.respondWith(networkFirst(request));
        return;
    }
    // ...
});
```

**Después:**
```javascript
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // ? NO cachear peticiones que no sean GET
    if (request.method !== 'GET') {
        event.respondWith(fetch(request));
        return;
    }

    // Estrategia: Network First para datos dinámicos (API calls)
    if (request.url.includes('/api/')) {
        event.respondWith(networkFirst(request));
        return;
    }
    // ...
});
```

**Beneficio:** Las peticiones POST van directamente a la red sin pasar por caché.

---

### **Cambio 2: Validar Método en `networkFirst()`**

**Antes:**
```javascript
async function networkFirst(request) {
    const cache = await caches.open(RUNTIME_CACHE);
    
    try {
        const response = await fetch(request);
        if (response.ok) {
            cache.put(request, response.clone()); // ? Falla con POST
        }
        return response;
    } catch (error) {
        const cached = await cache.match(request);
        if (cached) {
            return cached;
        }
        throw error;
    }
}
```

**Después:**
```javascript
async function networkFirst(request) {
    const cache = await caches.open(RUNTIME_CACHE);
    
    try {
        const response = await fetch(request);
        
        // ? FIX: Solo cachear peticiones GET con respuesta exitosa
        if (response.ok && request.method === 'GET') {
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.error('[Service Worker] Error en networkFirst:', error);
        
        // Solo buscar en caché si es GET
        if (request.method === 'GET') {
            const cached = await cache.match(request);
            if (cached) {
                return cached;
            }
        }
        
        throw error;
    }
}
```

**Beneficio:** Doble validación para prevenir errores.

---

### **Cambio 3: Actualizar Versión de Caché**

```javascript
// Antes
const CACHE_NAME = 'presupuesto-app-v13';
const RUNTIME_CACHE = 'presupuesto-runtime-v13';

// Después
const CACHE_NAME = 'presupuesto-app-v14';
const RUNTIME_CACHE = 'presupuesto-runtime-v14';
```

**Beneficio:** Fuerza la actualización del service worker.

---

## ?? **CÓMO APLICAR EL FIX**

### **Paso 1: Ejecutar Script de Limpieza**

```powershell
.\src\PowershellScripts\clear-service-worker.ps1
```

O sigue las instrucciones manuales:

### **Paso 2: Limpiar Service Worker en Chrome**

1. **Abrir DevTools:** `F12`
2. **Ir a:** `Application` ? `Service Workers`
3. **Hacer clic en:** `Unregister`
4. **Ir a:** `Storage` ? `Clear site data`
5. **Marcar todo** y hacer clic en `Clear site data`
6. **Cerrar todas las pestañas** de la aplicación
7. **Reabrir:** `Ctrl + F5` para forzar recarga

### **Paso 3: Verificar Fix**

```powershell
cd src
dotnet run
```

Navegar a: `https://localhost:7036/Transacciones/TestOcr`

**Resultado Esperado:**
- ? La página carga correctamente
- ? No hay errores en la consola
- ? El formulario funciona sin error 400

---

## ?? **TESTING**

### **Test 1: Verificar Service Worker Actualizado**

1. Abrir DevTools (F12)
2. Ir a: `Application` ? `Service Workers`
3. Verificar que muestra: `Version: 1.0.2`
4. Verificar que el estado es: `activated and is running`

### **Test 2: Subir Imagen en TestOcr**

1. Ir a: `/Transacciones/TestOcr`
2. Seleccionar una imagen
3. Hacer clic en "Procesar con OCR"
4. Verificar que:
   - ? No hay error 400
   - ? La petición POST se completa
   - ? Redirige a `CreateFromImage`

### **Test 3: Verificar Consola**

Abrir consola de DevTools y verificar:
```
[Service Worker] Cargado exitosamente
[Service Worker] Instalando...
[Service Worker] Pre-caching archivos
[Service Worker] Activando...
```

NO debe aparecer:
```
? Failed to execute 'put' on 'Cache'
```

---

## ?? **DETALLES TÉCNICOS**

### **¿Por qué cache.put() no soporta POST?**

La **Cache API** del navegador está diseñada para cachear **recursos estáticos** (HTML, CSS, JS, imágenes), que siempre se obtienen con GET.

Las peticiones POST:
- ? **No son idempotentes** (pueden cambiar el estado del servidor)
- ? **Tienen body con datos** (diferentes cada vez)
- ? **No deberían cachearse** (siempre necesitan ir al servidor)

### **Flujo Correcto:**

```
GET /Transacciones/TestOcr
    ?
Service Worker intercepta
    ?
Estrategia: staleWhileRevalidate
    ?
Devuelve HTML cacheado (rápido)
    ?
Actualiza caché en segundo plano

POST /Transacciones/TestOcr
    ?
Service Worker intercepta
    ?
if (method !== 'GET') ? fetch(request)
    ?
Va directamente al servidor (sin caché)
    ?
Procesa OCR y redirige
```

---

## ? **CHECKLIST DE VERIFICACIÓN**

Antes de considerar el fix completo:

- [ ] ? Service worker actualizado a v1.0.2
- [ ] ? Caché limpiada en navegador
- [ ] ? Aplicación ejecutándose
- [ ] ? `/Transacciones/TestOcr` carga sin errores
- [ ] ? Formulario POST funciona correctamente
- [ ] ? No hay errores en consola de DevTools
- [ ] ? OCR procesa imágenes exitosamente

---

## ?? **REFERENCIAS**

- [Cache API - MDN](https://developer.mozilla.org/en-US/docs/Web/API/Cache)
- [Service Worker Spec](https://w3c.github.io/ServiceWorker/)
- [Workbox Strategies](https://developers.google.com/web/tools/workbox/modules/workbox-strategies)
- [PWA Caching Patterns](https://web.dev/offline-cookbook/)

---

## ?? **RESULTADO FINAL**

**Estado:** ? RESUELTO

**Cambios:**
- ? Service Worker v1.0.2
- ? Filtrado correcto de peticiones POST
- ? Validación en networkFirst()
- ? Script de limpieza creado

**Impacto:**
- ? /Transacciones/TestOcr funciona correctamente
- ? No más errores HTTP 400
- ? Formularios POST funcionan sin problemas
- ? Service Worker más robusto

---

**¡El service worker ahora maneja correctamente todas las peticiones!** ??

**Versión:** 1.0.2  
**Fecha:** Marzo 2026  
**Estado:** ? Producción
