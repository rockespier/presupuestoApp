// Service Worker para PresupuestoFamiliarApp
// Versión: 1.0.2

const CACHE_NAME = 'presupuesto-app-v14';
const RUNTIME_CACHE = 'presupuesto-runtime-v14';

// Archivos esenciales para cachear en la instalación
const PRECACHE_URLS = [
    '/',
    '/Home/Index',
    '/Auth/Login',
    '/manifest.json',
    '/icons/icon-192x192.png',
    '/icons/icon-512x512.png'
];

// Instalación del Service Worker
self.addEventListener('install', event => {
    console.log('[Service Worker] Instalando...');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Pre-caching archivos');
                // Intentar cachear cada archivo individualmente para evitar que un error detenga todo
                return Promise.allSettled(
                    PRECACHE_URLS.map(url => 
                        cache.add(url).catch(err => {
                            console.warn(`[Service Worker] No se pudo cachear: ${url}`, err);
                        })
                    )
                );
            })
            .then(() => {
                console.log('[Service Worker] Pre-cache completado');
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('[Service Worker] Error en instalación:', error);
            })
    );
});

// Activación del Service Worker
self.addEventListener('activate', event => {
    console.log('[Service Worker] Activando...');
    
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME && cacheName !== RUNTIME_CACHE) {
                        console.log('[Service Worker] Eliminando caché antigua:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

// Intercepción de peticiones (Fetch)
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

    // Estrategia: Cache First para assets estáticos
    if (request.url.includes('.css') || 
        request.url.includes('.js') || 
        request.url.includes('.png') || 
        request.url.includes('.jpg') || 
        request.url.includes('.svg') ||
        request.url.includes('font-awesome')) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // Estrategia: Stale While Revalidate para páginas HTML
    event.respondWith(staleWhileRevalidate(request));
});

// Estrategia: Cache First (primero busca en caché, luego en red)
async function cacheFirst(request) {
    // No intentar cachear recursos de dominios externos con CORS
    const url = new URL(request.url);
    if (url.origin !== location.origin && !url.origin.includes('localhost')) {
        try {
            return await fetch(request);
        } catch (error) {
            console.warn('[Service Worker] Error al cargar recurso externo:', url.href);
            return new Response('', { status: 503, statusText: 'External resource unavailable' });
        }
    }

    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    
    if (cached) {
        return cached;
    }

    try {
        const response = await fetch(request);
        if (response.ok && response.status === 200) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        console.warn('[Service Worker] Error en cacheFirst:', error);
        // Retornar respuesta vacía en lugar de fallar
        return new Response('', { 
            status: 503,
            statusText: 'Service Unavailable'
        });
    }
}

// Estrategia: Network First (primero intenta red, luego caché)
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

// Estrategia: Stale While Revalidate (devuelve caché pero actualiza en segundo plano)
async function staleWhileRevalidate(request) {
    const cache = await caches.open(RUNTIME_CACHE);
    const cached = await cache.match(request);

    const fetchPromise = fetch(request).then(response => {
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    }).catch(() => cached);

    return cached || fetchPromise;
}

// Notificaciones Push
self.addEventListener('push', event => {
    console.log('[Service Worker] Push recibido');
    
    let notificationData = {
        title: 'Presupuesto Familiar App',
        body: 'Nueva notificación',
        icon: '/icons/icon-192x192.png',
        badge: '/icons/badge-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            url: '/',
            dateOfArrival: Date.now()
        },
        actions: [
            { action: 'view', title: '??? Ver', icon: '/icons/checkmark.png' },
            { action: 'close', title: '?? Cerrar', icon: '/icons/cross.png' }
        ]
    };

    // Si hay datos en el push, usarlos
    if (event.data) {
        try {
            const payload = event.data.json();
            if (payload.notification) {
                notificationData = {
                    ...notificationData,
                    ...payload.notification
                };
            }
        } catch (e) {
            // Si no es JSON, usar el texto directamente
            notificationData.body = event.data.text();
        }
    }

    event.waitUntil(
        self.registration.showNotification(notificationData.title, notificationData)
    );
});

// Click en notificación
self.addEventListener('notificationclick', event => {
    console.log('[Service Worker] Notification click recibido');
    
    event.notification.close();

    // Obtener la URL de los datos de la notificación
    const urlToOpen = event.notification.data?.url || '/';

    if (event.action === 'view' || !event.action) {
        event.waitUntil(
            clients.matchAll({ type: 'window', includeUncontrolled: true })
                .then(clientList => {
                    // Si ya hay una ventana abierta, enfocarla
                    for (let i = 0; i < clientList.length; i++) {
                        const client = clientList[i];
                        if (client.url === urlToOpen && 'focus' in client) {
                            return client.focus();
                        }
                    }
                    // Si no hay ventana abierta, abrir una nueva
                    if (clients.openWindow) {
                        return clients.openWindow(urlToOpen);
                    }
                })
        );
    } else if (event.action === 'close') {
        // Solo cerrar la notificación (ya se hizo arriba)
        console.log('[Service Worker] Notificación cerrada');
    }
});

// Sincronización en segundo plano (Background Sync)
self.addEventListener('sync', event => {
    if (event.tag === 'sync-transactions') {
        event.waitUntil(syncTransactions());
    }
});

async function syncTransactions() {
    console.log('[Service Worker] Sincronizando transacciones...');
    // Aquí podrías implementar lógica para sincronizar datos pendientes
    // cuando se recupera la conexión
}

// Mensaje desde el cliente
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

console.log('[Service Worker] Cargado exitosamente');
