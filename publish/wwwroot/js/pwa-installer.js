// PWA Installer - Gestiona la instalación de la app como PWA
// PresupuestoFamiliarApp

let deferredPrompt;
let installButton;

// Registrar Service Worker
if ('serviceWorker' in navigator) {
    window.addEventListener('load', async () => {
        try {
            const registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });
            
            console.log('Service Worker registrado:', registration.scope);

            // Verificar actualizaciones cada 60 minutos
            setInterval(() => {
                registration.update();
            }, 60 * 60 * 1000);

            // Escuchar actualizaciones del Service Worker
            registration.addEventListener('updatefound', () => {
                const newWorker = registration.installing;
                
                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        // Nueva versión disponible
                        showUpdateNotification();
                    }
                });
            });

        } catch (error) {
            console.error('Error al registrar Service Worker:', error);
        }
    });
}

// Mostrar notificación de actualización disponible
function showUpdateNotification() {
    if (confirm('¡Nueva versión disponible! ¿Actualizar ahora?')) {
        window.location.reload();
    }
}

// Detectar evento de instalación (beforeinstallprompt)
window.addEventListener('beforeinstallprompt', (e) => {
    console.log('Evento de instalación detectado');
    
    // Prevenir que el navegador muestre su propio prompt automáticamente
    e.preventDefault();
    
    // Guardar el evento para usarlo más tarde
    deferredPrompt = e;
    
    // Mostrar botón de instalación personalizado
    showInstallButton();
});

// Mostrar botón de instalación
function showInstallButton() {
    // Buscar el botón de instalación en el DOM
    installButton = document.getElementById('install-button');
    
    if (installButton) {
        installButton.style.display = 'block';
        installButton.addEventListener('click', installApp);
    } else {
        // Si no existe el botón, crear uno dinámico
        createFloatingInstallButton();
    }
}

// Crear botón flotante de instalación
function createFloatingInstallButton() {
    const button = document.createElement('button');
    button.id = 'floating-install-button';
    button.innerHTML = `
        <svg class="w-6 h-6 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"></path>
        </svg>
        <span>Instalar App</span>
    `;
    button.className = 'fixed bottom-6 right-6 z-50 flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-primary-500 to-blue-600 text-white font-bold rounded-full shadow-2xl hover:from-primary-600 hover:to-blue-700 transform hover:scale-110 transition duration-300 animate-bounce';
    
    button.addEventListener('click', installApp);
    document.body.appendChild(button);
    
    installButton = button;
}

// Instalar la aplicación
async function installApp() {
    if (!deferredPrompt) {
        console.warn('No hay prompt de instalación disponible');
        return;
    }

    // Mostrar el prompt de instalación nativo
    deferredPrompt.prompt();

    // Esperar respuesta del usuario
    const { outcome } = await deferredPrompt.userChoice;
    
    console.log(`Resultado de instalación: ${outcome}`);

    if (outcome === 'accepted') {
        console.log('Usuario aceptó instalar la app');
        
        // Opcional: Trackear el evento de instalación
        if (typeof gtag !== 'undefined') {
            gtag('event', 'pwa_install', {
                event_category: 'engagement',
                event_label: 'PWA Installation'
            });
        }
    } else {
        console.log('Usuario rechazó instalar la app');
    }

    // Limpiar el prompt usado
    deferredPrompt = null;
    
    // Ocultar botón de instalación
    if (installButton) {
        installButton.style.display = 'none';
    }
}

// Detectar cuando la app fue instalada exitosamente
window.addEventListener('appinstalled', (e) => {
    console.log('¡App instalada exitosamente!');
    
    // Ocultar botón de instalación
    if (installButton) {
        installButton.style.display = 'none';
    }

    // Mostrar mensaje de bienvenida
    showWelcomeMessage();

    // Trackear instalación
    if (typeof gtag !== 'undefined') {
        gtag('event', 'app_installed', {
            event_category: 'engagement',
            event_label: 'PWA Installation Completed'
        });
    }
});

// Mostrar mensaje de bienvenida después de instalar
function showWelcomeMessage() {
    const notification = document.createElement('div');
    notification.innerHTML = `
        <div class="fixed top-4 right-4 z-50 bg-green-500 text-white px-6 py-4 rounded-xl shadow-2xl animate-slide-in">
            <div class="flex items-center gap-3">
                <span class="text-2xl"></span>
                <div>
                    <p class="font-bold">¡App instalada!</p>
                    <p class="text-sm">Ahora puedes usarla como aplicación nativa</p>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(notification);

    // Remover notificación después de 5 segundos
    setTimeout(() => {
        notification.remove();
    }, 5000);
}

// Detectar si la app está corriendo en modo standalone (instalada)
function isRunningStandalone() {
    return (window.matchMedia('(display-mode: standalone)').matches) || 
           (window.navigator.standalone) || 
           document.referrer.includes('android-app://');
}

// Ejecutar al cargar la página
document.addEventListener('DOMContentLoaded', () => {
    if (isRunningStandalone()) {
        console.log('App corriendo en modo standalone');
        
        // Ocultar elementos específicos del navegador
        const browserOnlyElements = document.querySelectorAll('.browser-only');
        browserOnlyElements.forEach(el => el.style.display = 'none');

        // Añadir clase para estilos específicos de PWA
        document.body.classList.add('pwa-installed');
    }

    // Verificar soporte de notificaciones
    if ('Notification' in window && navigator.serviceWorker) {
        console.log('Soporte de notificaciones disponible');
    }

    // Verificar soporte de Background Sync
    if ('sync' in navigator.serviceWorker.constructor.prototype) {
        console.log('Background Sync disponible');
    }

    // Verificar soporte de Periodic Background Sync
    if ('periodicSync' in navigator.serviceWorker.constructor.prototype) {
        console.log('Periodic Background Sync disponible');
    }
});

// Función para solicitar permisos de notificaciones
async function requestNotificationPermission() {
    if (!('Notification' in window)) {
        console.warn('Este navegador no soporta notificaciones');
        return false;
    }

    if (Notification.permission === 'granted') {
        return true;
    }

    if (Notification.permission !== 'denied') {
        const permission = await Notification.requestPermission();
        return permission === 'granted';
    }

    return false;
}

// Función para enviar notificación local
async function sendLocalNotification(title, options = {}) {
    const hasPermission = await requestNotificationPermission();
    
    if (hasPermission && navigator.serviceWorker) {
        const registration = await navigator.serviceWorker.ready;
        
        registration.showNotification(title, {
            icon: '/icons/icon-192x192.png',
            badge: '/icons/badge-72x72.png',
            vibrate: [200, 100, 200],
            ...options
        });
    }
}

// Exportar funciones para uso global
window.PWAInstaller = {
    install: installApp,
    isStandalone: isRunningStandalone,
    requestNotifications: requestNotificationPermission,
    sendNotification: sendLocalNotification
};

console.log('PWA Installer cargado correctamente');
