// Service Worker para PresupuestoFamiliarApp
// Versión: 1.0.0

const CACHE_NAME = 'presupuesto-app-v9';
const RUNTIME_CACHE = 'presupuesto-runtime-v9';

// Archivos esenciales para cachear en la instalación
const PRECACHE_URLS = [
    '/',
    '/Home/Index',
    '/Auth/Login',
    '/css/site.css',
    '/js/site.js',
    '/font-awesome/css/font-awesome.css',
    '/font-awesome/css/font-awesome.min.css',
    '/manifest.json',
    '/icons/icon-192x192.png',
    '/icons/icon-512x512.png',
    'https://cdn.tailwindcss.com'
];

// Instalación del Service Worker
self.addEventListener('install', event => {
    console.log('[Service Worker] Instalando...');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Pre-caching archivos');
                return cache.addAll(PRECACHE_URLS);
            })
            .then(() => self.skipWaiting())
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

    // Estrategia: Network First para datos dinámicos (API calls)
    if (request.url.includes('/api/') || request.method !== 'GET') {
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
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    
    if (cached) {
        return cached;
    }

    try {
        const response = await fetch(request);
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        console.error('[Service Worker] Error en cacheFirst:', error);
        // Retornar página offline si está disponible
        return cache.match('/offline.html') || new Response('Offline', {
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
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        console.error('[Service Worker] Error en networkFirst:', error);
        const cached = await cache.match(request);
        if (cached) {
            return cached;
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

// Notificaciones Push (opcional para futuras implementaciones)
self.addEventListener('push', event => {
    const options = {
        body: event.data ? event.data.text() : 'Nueva notificación',
        icon: '/icons/icon-192x192.png',
        badge: '/icons/badge-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            dateOfArrival: Date.now(),
            primaryKey: 1
        },
        actions: [
            {
                action: 'explore',
                title: 'Ver más',
                icon: '/icons/checkmark.png'
            },
            {
                action: 'close',
                title: 'Cerrar',
                icon: '/icons/cross.png'
            }
        ]
    };

    event.waitUntil(
        self.registration.showNotification('Presupuesto Familiar App', options)
    );
});

// Click en notificación
self.addEventListener('notificationclick', event => {
    event.notification.close();

    if (event.action === 'explore') {
        event.waitUntil(
            clients.openWindow('/')
        );
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
