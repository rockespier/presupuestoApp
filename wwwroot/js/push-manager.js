// Push Notification Manager para PresupuestoFamiliarApp
class PushNotificationManager {
    constructor() {
        this.publicKey = null;
        this.isSubscribed = false;
        this.subscription = null;
    }

    /**
     * Inicializa el sistema de notificaciones push
     */
    async init() {
        try {
            console.log('🔄 Iniciando sistema de notificaciones push...');
            
            // Verificar si el navegador soporta notificaciones
            if (!('serviceWorker' in navigator)) {
                console.warn('⚠️ Service Workers no soportados en este navegador');
                this.mostrarError('Tu navegador no soporta notificaciones push');
                return false;
            }

            if (!('PushManager' in window)) {
                console.warn('⚠️ Push API no soportada en este navegador');
                this.mostrarError('Tu navegador no soporta la API de Push');
                return false;
            }

            console.log('✅ Navegador compatible con Push API');

            // Esperar a que el service worker esté listo
            try {
                const registration = await navigator.serviceWorker.ready;
                console.log('✅ Service Worker listo:', registration.active?.state);
            } catch (swError) {
                console.error('❌ Error con Service Worker:', swError);
                this.mostrarError('Service Worker no disponible. Recarga la página.');
                return false;
            }

            // Obtener la clave pública VAPID del servidor
            try {
                await this.obtenerClavePublica();
                console.log('✅ Clave pública VAPID obtenida');
            } catch (keyError) {
                console.error('❌ Error al obtener clave VAPID:', keyError);
                this.mostrarError('No se pudo conectar con el servidor. Verifica que la app esté corriendo.');
                return false;
            }

            // Verificar el estado de la suscripción
            const registration = await navigator.serviceWorker.ready;
            await this.verificarSuscripcion(registration);

            console.log('✅ Sistema de notificaciones inicializado correctamente');
            return true;
        } catch (error) {
            console.error('❌ Error general al inicializar Push Manager:', error);
            this.mostrarError('Error al inicializar notificaciones: ' + error.message);
            return false;
        }
    }

    /**
     * Obtiene la clave pública VAPID del servidor
     */
    async obtenerClavePublica() {
        try {
            console.log('📡 Obteniendo clave pública VAPID...');
            const response = await fetch('/api/push/public-key');
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const data = await response.json();
            
            if (!data.publicKey) {
                throw new Error('La respuesta no contiene la clave pública');
            }
            
            this.publicKey = data.publicKey;
            console.log('✅ Clave pública VAPID obtenida:', this.publicKey.substring(0, 20) + '...');
        } catch (error) {
            console.error('❌ Error al obtener clave pública:', error);
            throw new Error('No se pudo obtener la clave VAPID del servidor. Verifica que el servidor esté corriendo.');
        }
    }

    /**
     * Verifica si el usuario ya está suscrito
     */
    async verificarSuscripcion(registration) {
        try {
            this.subscription = await registration.pushManager.getSubscription();
            this.isSubscribed = this.subscription !== null;
            
            if (this.isSubscribed) {
                console.log('✅ Usuario ya suscrito a notificaciones');
                this.actualizarUI(true);
            } else {
                console.log('ℹ️ Usuario no suscrito a notificaciones');
                this.actualizarUI(false);
            }
        } catch (error) {
            console.error('❌ Error al verificar suscripción:', error);
            this.actualizarUI(false);
        }
    }

    /**
     * Solicita permiso y suscribe al usuario
     */
    async suscribir() {
        try {
            // Solicitar permiso para notificaciones
            const permission = await Notification.requestPermission();
            
            if (permission !== 'granted') {
                console.warn('⚠️ Permiso denegado para notificaciones');
                this.mostrarMensaje('Permiso denegado para notificaciones', 'warning');
                return false;
            }

            console.log('✅ Permiso concedido para notificaciones');

            // Obtener el service worker registration
            const registration = await navigator.serviceWorker.ready;

            // Convertir la clave pública a Uint8Array
            const applicationServerKey = this.urlBase64ToUint8Array(this.publicKey);

            // Suscribirse a las notificaciones push
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerKey
            });

            console.log('✅ Suscripción push creada');

            // Enviar la suscripción al servidor
            await this.enviarSuscripcionAlServidor(subscription);

            this.subscription = subscription;
            this.isSubscribed = true;
            this.actualizarUI(true);

            return true;
        } catch (error) {
            console.error('❌ Error al suscribir:', error);
            this.mostrarMensaje('Error al activar notificaciones: ' + error.message, 'error');
            return false;
        }
    }

    /**
     * Desuscribe al usuario de las notificaciones
     */
    async desuscribir() {
        try {
            if (!this.subscription) {
                console.warn('⚠️ No hay suscripción activa');
                return false;
            }

            // Desuscribir del navegador
            await this.subscription.unsubscribe();
            console.log('✅ Desuscripción local exitosa');

            // Notificar al servidor
            await this.enviarDesuscripcionAlServidor(this.subscription);

            this.subscription = null;
            this.isSubscribed = false;
            this.actualizarUI(false);

            this.mostrarMensaje('Notificaciones desactivadas', 'info');
            return true;
        } catch (error) {
            console.error('❌ Error al desuscribir:', error);
            this.mostrarMensaje('Error al desactivar notificaciones', 'error');
            return false;
        }
    }

    /**
     * Envía la suscripción al servidor
     */
    async enviarSuscripcionAlServidor(subscription) {
        try {
            const response = await fetch('/api/push/subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include', // ⭐ IMPORTANTE: Incluir cookies de autenticación
                body: JSON.stringify(subscription.toJSON())
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || `Error al guardar suscripción (${response.status})`);
            }

            const data = await response.json();
            console.log('✅ Suscripción guardada en el servidor:', data);
            this.mostrarMensaje('¡Notificaciones activadas correctamente!', 'success');
        } catch (error) {
            console.error('❌ Error al enviar suscripción:', error);
            throw error;
        }
    }

    /**
     * Envía la desuscripción al servidor
     */
    async enviarDesuscripcionAlServidor(subscription) {
        try {
            const response = await fetch('/api/push/unsubscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include', // ⭐ IMPORTANTE: Incluir cookies de autenticación
                body: JSON.stringify({
                    endpoint: subscription.endpoint
                })
            });

            if (response.ok) {
                console.log('✅ Desuscripción confirmada por el servidor');
            }
        } catch (error) {
            console.error('❌ Error al enviar desuscripción:', error);
        }
    }

    /**
     * Envía una notificación de prueba
     */
    async enviarNotificacionPrueba() {
        try {
            if (!this.isSubscribed) {
                this.mostrarMensaje('Primero debes activar las notificaciones', 'warning');
                return;
            }

            const response = await fetch('/api/push/test', {
                method: 'POST',
                credentials: 'include' // ⭐ IMPORTANTE: Incluir cookies de autenticación
            });

            if (response.ok) {
                console.log('✅ Notificación de prueba enviada');
                this.mostrarMensaje('Notificación de prueba enviada!', 'info');
            } else {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Error al enviar notificación de prueba');
            }
        } catch (error) {
            console.error('❌ Error al enviar notificación de prueba:', error);
            this.mostrarMensaje('Error al enviar notificación de prueba', 'error');
        }
    }

    /**
     * Convierte la clave pública de base64 a Uint8Array
     */
    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    /**
     * Actualiza la UI según el estado de suscripción
     */
    actualizarUI(suscrito) {
        const btnSuscribir = document.getElementById('btn-suscribir-push');
        const btnDesuscribir = document.getElementById('btn-desuscribir-push');
        const btnPrueba = document.getElementById('btn-prueba-push');
        const estadoTexto = document.getElementById('estado-push');

        if (btnSuscribir) {
            btnSuscribir.style.display = suscrito ? 'none' : 'inline-flex';
        }

        if (btnDesuscribir) {
            btnDesuscribir.style.display = suscrito ? 'inline-flex' : 'none';
        }

        if (btnPrueba) {
            btnPrueba.style.display = suscrito ? 'inline-flex' : 'none';
        }

        if (estadoTexto) {
            estadoTexto.textContent = suscrito 
                ? '🔔 Notificaciones activadas' 
                : '🔕 Notificaciones desactivadas';
            estadoTexto.className = suscrito
                ? 'text-sm font-semibold text-green-600 dark:text-green-400'
                : 'text-sm font-semibold text-gray-600 dark:text-gray-400';
        }
    }

    /**
     * Muestra un mensaje toast
     */
    mostrarMensaje(mensaje, tipo = 'info') {
        if (typeof Swal !== 'undefined') {
            const iconos = {
                success: 'success',
                error: 'error',
                warning: 'warning',
                info: 'info'
            };

            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: iconos[tipo] || 'info',
                title: mensaje,
                showConfirmButton: false,
                timer: 3000,
                timerProgressBar: true
            });
        } else {
            console.log(`[${tipo.toUpperCase()}] ${mensaje}`);
        }
    }

    /**
     * Muestra un mensaje de error en la UI
     */
    mostrarError(mensaje) {
        const estadoTexto = document.getElementById('estado-push');
        if (estadoTexto) {
            estadoTexto.textContent = '❌ ' + mensaje;
            estadoTexto.className = 'text-sm font-semibold text-red-600 dark:text-red-400';
        }
        
        // Ocultar todos los botones
        const btnSuscribir = document.getElementById('btn-suscribir-push');
        const btnDesuscribir = document.getElementById('btn-desuscribir-push');
        const btnPrueba = document.getElementById('btn-prueba-push');
        
        if (btnSuscribir) btnSuscribir.style.display = 'none';
        if (btnDesuscribir) btnDesuscribir.style.display = 'none';
        if (btnPrueba) btnPrueba.style.display = 'none';
    }
}

// Crear instancia global
const pushNotificationManager = new PushNotificationManager();

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', async () => {
    console.log('🔔 Inicializando Push Notification Manager...');
    await pushNotificationManager.init();
});

// Exponer métodos globales
window.suscribirPush = () => pushNotificationManager.suscribir();
window.desuscribirPush = () => pushNotificationManager.desuscribir();
window.probarNotificacion = () => pushNotificationManager.enviarNotificacionPrueba();
