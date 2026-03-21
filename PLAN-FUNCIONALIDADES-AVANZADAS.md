# ?? PLAN MAESTRO: Funcionalidades Avanzadas PWA
## PresupuestoFamiliarApp - Roadmap de Implementación

---

## ?? **ÍNDICE**

1. [Visión General](#visión-general)
2. [Arquitectura Propuesta](#arquitectura-propuesta)
3. [Fase 1: Background Sync](#fase-1-background-sync)
4. [Fase 2: Notificaciones Push](#fase-2-notificaciones-push)
5. [Fase 3: Share Target API](#fase-3-share-target-api)
6. [Fase 4: Sonido e i18n](#fase-4-sonido-e-i18n)
7. [Fase 5: Changelog](#fase-5-changelog)
8. [Timeline y Priorización](#timeline-y-priorización)
9. [Stack Tecnológico](#stack-tecnológico)

---

## ?? **VISIÓN GENERAL**

### **Objetivo Principal:**
Transformar PresupuestoFamiliarApp en una PWA de nivel enterprise con capacidades offline-first, notificaciones inteligentes y procesamiento de imágenes.

### **Funcionalidades a Implementar:**

| # | Funcionalidad | Prioridad | Complejidad | Impacto |
|---|---------------|-----------|-------------|---------|
| 1 | **Background Sync** | ?? Alta | Media | Alto |
| 2 | **Notificaciones Push** | ?? Alta | Alta | Muy Alto |
| 3 | **Share Target API** | ?? Media | Alta | Alto |
| 4 | **Sonido + i18n** | ?? Baja | Baja | Medio |
| 5 | **Changelog** | ?? Baja | Baja | Medio |

### **Beneficios Esperados:**
- ? **Experiencia Offline Completa**: Crear transacciones sin conexión
- ? **Recordatorios Automáticos**: Notificar vencimientos de pagos/cobros
- ? **Digitalización de Tickets**: Compartir fotos de recibos a la app
- ? **UX Internacional**: Soporte multiidioma
- ? **Transparencia**: Changelog visible para usuarios

---

## ??? **ARQUITECTURA PROPUESTA**

### **Componentes Clave:**

```
???????????????????????????????????????????????????????????????
?                    FRONTEND (PWA)                           ?
???????????????????????????????????????????????????????????????
?  ????????????????  ????????????????  ????????????????     ?
?  ?   IndexedDB  ?  ? Service      ?  ? Notification ?     ?
?  ?   (Local DB) ?  ? Worker       ?  ? Manager      ?     ?
?  ????????????????  ????????????????  ????????????????     ?
?         ?                 ?                  ?              ?
?         ?                 ?                  ?              ?
?  ??????????????????????????????????????????????????        ?
?  ?          Background Sync Manager               ?        ?
?  ??????????????????????????????????????????????????        ?
???????????????????????????????????????????????????????????????
                            ?
                            ? HTTP/WebSocket
                            ?
???????????????????????????????????????????????????????????????
?                   BACKEND (.NET 9)                          ?
???????????????????????????????????????????????????????????????
?  ????????????????  ????????????????  ????????????????     ?
?  ? Push         ?  ? OCR/AI       ?  ? Scheduler    ?     ?
?  ? Notification ?  ? Service      ?  ? (Hangfire)   ?     ?
?  ? Service      ?  ? (Tesseract)  ?  ?              ?     ?
?  ????????????????  ????????????????  ????????????????     ?
???????????????????????????????????????????????????????????????
                            ?
                            ?
                            ?
                    ???????????????
                    ?  SQL Server ?
                    ???????????????
```

---

## ?? **FASE 1: BACKGROUND SYNC**

### **Objetivo:**
Permitir crear/editar transacciones sin conexión y sincronizarlas automáticamente cuando vuelve internet.

### **Componentes a Desarrollar:**

#### **1.1 IndexedDB Manager**
```javascript
// wwwroot/js/indexeddb-manager.js
class IndexedDBManager {
    constructor() {
        this.dbName = 'PresupuestoAppDB';
        this.version = 1;
        this.db = null;
    }

    async init() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);
            
            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                this.db = request.result;
                resolve(this.db);
            };
            
            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                
                // Store para transacciones pendientes
                if (!db.objectStoreNames.contains('pendingTransactions')) {
                    const store = db.createObjectStore('pendingTransactions', { 
                        keyPath: 'id', 
                        autoIncrement: true 
                    });
                    store.createIndex('timestamp', 'timestamp', { unique: false });
                    store.createIndex('synced', 'synced', { unique: false });
                }
                
                // Store para cuentas (caché)
                if (!db.objectStoreNames.contains('cachedCuentas')) {
                    db.createObjectStore('cachedCuentas', { keyPath: 'id' });
                }
                
                // Store para categorías (caché)
                if (!db.objectStoreNames.contains('cachedCategorias')) {
                    db.createObjectStore('cachedCategorias', { keyPath: 'id' });
                }
            };
        });
    }

    async addPendingTransaction(transaction) {
        const tx = this.db.transaction(['pendingTransactions'], 'readwrite');
        const store = tx.objectStore('pendingTransactions');
        
        const data = {
            ...transaction,
            timestamp: Date.now(),
            synced: false,
            retries: 0
        };
        
        return store.add(data);
    }

    async getPendingTransactions() {
        const tx = this.db.transaction(['pendingTransactions'], 'readonly');
        const store = tx.objectStore('pendingTransactions');
        const index = store.index('synced');
        
        return new Promise((resolve, reject) => {
            const request = index.getAll(false);
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    async markAsSynced(id) {
        const tx = this.db.transaction(['pendingTransactions'], 'readwrite');
        const store = tx.objectStore('pendingTransactions');
        
        const transaction = await store.get(id);
        transaction.synced = true;
        transaction.syncedAt = Date.now();
        
        return store.put(transaction);
    }

    async deleteSyncedTransaction(id) {
        const tx = this.db.transaction(['pendingTransactions'], 'readwrite');
        const store = tx.objectStore('pendingTransactions');
        return store.delete(id);
    }
}

// Instancia global
window.dbManager = new IndexedDBManager();
```

#### **1.2 Background Sync Manager**
```javascript
// wwwroot/js/background-sync.js
class BackgroundSyncManager {
    constructor() {
        this.syncInProgress = false;
    }

    async registerSync(tag = 'sync-transactions') {
        if ('serviceWorker' in navigator && 'sync' in navigator.serviceWorker) {
            const registration = await navigator.serviceWorker.ready;
            
            try {
                await registration.sync.register(tag);
                console.log('? Background sync registrado:', tag);
                return true;
            } catch (error) {
                console.error('? Error registrando sync:', error);
                // Fallback: intentar sync inmediato
                await this.syncNow();
                return false;
            }
        } else {
            console.warn('?? Background Sync no soportado');
            await this.syncNow();
            return false;
        }
    }

    async syncNow() {
        if (this.syncInProgress) {
            console.log('? Sync ya en progreso...');
            return;
        }

        this.syncInProgress = true;
        console.log('?? Iniciando sincronización manual...');

        try {
            const pending = await window.dbManager.getPendingTransactions();
            
            if (pending.length === 0) {
                console.log('? No hay transacciones pendientes');
                this.syncInProgress = false;
                return;
            }

            console.log(`?? Sincronizando ${pending.length} transacciones...`);

            for (const transaction of pending) {
                try {
                    await this.syncSingleTransaction(transaction);
                } catch (error) {
                    console.error('? Error sincronizando transacción:', error);
                    // Incrementar contador de reintentos
                    transaction.retries = (transaction.retries || 0) + 1;
                    
                    if (transaction.retries > 3) {
                        // Marcar como fallida después de 3 intentos
                        console.error('?? Transacción fallida después de 3 intentos:', transaction.id);
                    }
                }
            }

            console.log('? Sincronización completada');
            this.showSyncNotification(pending.length);
            
        } catch (error) {
            console.error('? Error en sincronización:', error);
        } finally {
            this.syncInProgress = false;
        }
    }

    async syncSingleTransaction(transaction) {
        const response = await fetch('/api/Transacciones/SyncOffline', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                descripcion: transaction.descripcion,
                monto: transaction.monto,
                fecha: transaction.fecha,
                tipo: transaction.tipo,
                cuentaId: transaction.cuentaId,
                categoriaId: transaction.categoriaId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        
        // Marcar como sincronizada
        await window.dbManager.markAsSynced(transaction.id);
        
        // Opcional: eliminar después de sincronizar
        // await window.dbManager.deleteSyncedTransaction(transaction.id);
        
        return result;
    }

    showSyncNotification(count) {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Sincronización completada', {
                body: `${count} transacción${count > 1 ? 'es' : ''} sincronizada${count > 1 ? 's' : ''}`,
                icon: '/icons/icon-192x192.png',
                badge: '/icons/badge-72x72.png'
            });
        }
    }
}

// Instancia global
window.syncManager = new BackgroundSyncManager();

// Auto-sync cuando vuelve la conexión
window.addEventListener('online', () => {
    console.log('?? Conexión restaurada');
    window.syncManager.syncNow();
});
```

#### **1.3 Service Worker - Background Sync**
```javascript
// Agregar al wwwroot/service-worker.js
self.addEventListener('sync', event => {
    if (event.tag === 'sync-transactions') {
        event.waitUntil(syncTransactions());
    }
});

async function syncTransactions() {
    console.log('[Service Worker] Sincronizando transacciones...');
    
    try {
        // Obtener transacciones pendientes desde IndexedDB
        const db = await openIndexedDB();
        const transactions = await getPendingTransactions(db);
        
        for (const transaction of transactions) {
            try {
                const response = await fetch('/api/Transacciones/SyncOffline', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(transaction)
                });
                
                if (response.ok) {
                    await markAsSynced(db, transaction.id);
                    console.log('? Transacción sincronizada:', transaction.id);
                }
            } catch (error) {
                console.error('? Error sincronizando:', error);
            }
        }
        
        // Notificar al cliente
        const clients = await self.clients.matchAll();
        clients.forEach(client => {
            client.postMessage({
                type: 'SYNC_COMPLETE',
                count: transactions.length
            });
        });
        
    } catch (error) {
        console.error('[Service Worker] Error en sync:', error);
        throw error;
    }
}

function openIndexedDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('PresupuestoAppDB', 1);
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function getPendingTransactions(db) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(['pendingTransactions'], 'readonly');
        const store = tx.objectStore('pendingTransactions');
        const index = store.index('synced');
        const request = index.getAll(false);
        
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function markAsSynced(db, id) {
    return new Promise((resolve, reject) => {
        const tx = db.transaction(['pendingTransactions'], 'readwrite');
        const store = tx.objectStore('pendingTransactions');
        const getRequest = store.get(id);
        
        getRequest.onsuccess = () => {
            const transaction = getRequest.result;
            transaction.synced = true;
            transaction.syncedAt = Date.now();
            
            const putRequest = store.put(transaction);
            putRequest.onsuccess = () => resolve();
            putRequest.onerror = () => reject(putRequest.error);
        };
        
        getRequest.onerror = () => reject(getRequest.error);
    });
}
```

#### **1.4 Backend API - Sync Endpoint**
```csharp
// Controllers/Api/TransaccionesController.cs
[ApiController]
[Route("api/[controller]")]
public class TransaccionesApiController : ControllerBase
{
    private readonly PresupuestoContext _context;
    
    public TransaccionesApiController(PresupuestoContext context)
    {
        _context = context;
    }
    
    [HttpPost("SyncOffline")]
    [Authorize]
    public async Task<IActionResult> SyncOffline([FromBody] TransaccionOfflineDto dto)
    {
        try
        {
            // Validar datos
            if (string.IsNullOrEmpty(dto.Descripcion) || dto.Monto <= 0)
            {
                return BadRequest(new { error = "Datos inválidos" });
            }
            
            // Obtener usuario actual
            var nombreUsuario = User.Identity.Name;
            var usuario = await _context.Usuarios
                .Include(u => u.Espacios)
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
            
            if (usuario == null)
            {
                return Unauthorized();
            }
            
            // Verificar que la cuenta pertenece al usuario
            var cuenta = await _context.Cuentas
                .FirstOrDefaultAsync(c => c.Id == dto.CuentaId && 
                                         usuario.Espacios.Any(e => e.Id == c.EspacioId));
            
            if (cuenta == null)
            {
                return BadRequest(new { error = "Cuenta no válida" });
            }
            
            // Crear transacción
            var transaccion = new Transaccion
            {
                Descripcion = dto.Descripcion,
                Monto = dto.Monto,
                Fecha = dto.Fecha,
                Tipo = dto.Tipo,
                CuentaId = dto.CuentaId,
                CategoriaId = dto.CategoriaId,
                EspacioId = cuenta.EspacioId,
                CreadoOffline = true,
                SincronizadoEn = DateTime.Now
            };
            
            _context.Transacciones.Add(transaccion);
            
            // Actualizar saldo de cuenta
            if (dto.Tipo == 0) // Ingreso
            {
                cuenta.SaldoActual += dto.Monto;
            }
            else // Egreso
            {
                cuenta.SaldoActual -= dto.Monto;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new 
            { 
                success = true, 
                id = transaccion.Id,
                mensaje = "Transacción sincronizada correctamente"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// Models/DTOs/TransaccionOfflineDto.cs
public class TransaccionOfflineDto
{
    public string Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int Tipo { get; set; } // 0 = Ingreso, 1 = Egreso
    public int CuentaId { get; set; }
    public int? CategoriaId { get; set; }
}
```

### **Archivos a Crear:**
- ? `wwwroot/js/indexeddb-manager.js`
- ? `wwwroot/js/background-sync.js`
- ? `Controllers/Api/TransaccionesApiController.cs`
- ? `Models/DTOs/TransaccionOfflineDto.cs`

### **Modificaciones:**
- ? `wwwroot/service-worker.js` - Agregar handlers de sync
- ? `Views/Transacciones/Create.cshtml` - Detectar offline y guardar en IndexedDB
- ? `Models/Transaccion.cs` - Agregar campos `CreadoOffline` y `SincronizadoEn`

### **Testing:**
1. Desconectar internet (DevTools ? Network ? Offline)
2. Crear una transacción
3. Verificar que se guarda en IndexedDB
4. Reconectar internet
5. Verificar sincronización automática

---

## ?? **FASE 2: NOTIFICACIONES PUSH**

### **Objetivo:**
Notificar a usuarios sobre vencimientos de pagos, cobros pendientes y alertas de presupuesto.

### **Componentes a Desarrollar:**

#### **2.1 VAPID Keys Generation**
```powershell
# Script: generate-vapid-keys.ps1
# Generar claves VAPID para Web Push

npm install -g web-push
web-push generate-vapid-keys

# Output:
# Public Key: BDxxxxxxxxxxxxxxxxxxxxxx...
# Private Key: xyzzzzzzzzzzzzzzzzzzz...
```

#### **2.2 Backend - Push Notification Service**
```csharp
// Services/PushNotificationService.cs
using WebPush;

public class PushNotificationService
{
    private readonly PresupuestoContext _context;
    private readonly IConfiguration _configuration;
    private readonly WebPushClient _pushClient;
    
    public PushNotificationService(
        PresupuestoContext context, 
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _pushClient = new WebPushClient();
        
        // Configurar claves VAPID
        var vapidPublicKey = configuration["VapidKeys:PublicKey"];
        var vapidPrivateKey = configuration["VapidKeys:PrivateKey"];
        var vapidSubject = configuration["VapidKeys:Subject"]; // mailto:tu-email@ejemplo.com
        
        _pushClient.SetVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
    }
    
    public async Task EnviarNotificacionVencimiento(int cuentaPorCobrarId)
    {
        var cuentaPorCobrar = await _context.CuentasPorCobrar
            .Include(c => c.Espacio)
                .ThenInclude(e => e.Usuarios)
                    .ThenInclude(u => u.PushSubscriptions)
            .FirstOrDefaultAsync(c => c.Id == cuentaPorCobrarId);
        
        if (cuentaPorCobrar == null) return;
        
        var mensaje = new
        {
            title = "?? Recordatorio de Cobro",
            body = $"{cuentaPorCobrar.Deudor} - Vence: {cuentaPorCobrar.FechaVencimiento:dd/MM/yyyy}",
            icon = "/icons/icon-192x192.png",
            badge = "/icons/badge-72x72.png",
            tag = $"cobro-{cuentaPorCobrarId}",
            data = new
            {
                url = $"/CuentasPorCobrar/Details/{cuentaPorCobrarId}",
                type = "vencimiento-cobro",
                id = cuentaPorCobrarId
            },
            actions = new[]
            {
                new { action = "view", title = "Ver Detalle" },
                new { action = "dismiss", title = "Descartar" }
            }
        };
        
        var payload = JsonSerializer.Serialize(mensaje);
        
        // Enviar a todos los usuarios del espacio
        foreach (var usuario in cuentaPorCobrar.Espacio.Usuarios)
        {
            foreach (var subscription in usuario.PushSubscriptions)
            {
                try
                {
                    var pushSubscription = new PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );
                    
                    await _pushClient.SendNotificationAsync(pushSubscription, payload);
                    Console.WriteLine($"? Notificación enviada a {usuario.NombreUsuario}");
                }
                catch (WebPushException ex)
                {
                    Console.WriteLine($"? Error enviando notificación: {ex.Message}");
                    
                    // Si el subscription expiró, eliminarlo
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        _context.PushSubscriptions.Remove(subscription);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
    
    public async Task EnviarNotificacionPresupuestoExcedido(int categoriaId, decimal porcentaje)
    {
        var categoria = await _context.Categorias
            .Include(c => c.Espacio)
                .ThenInclude(e => e.Usuarios)
                    .ThenInclude(u => u.PushSubscriptions)
            .FirstOrDefaultAsync(c => c.Id == categoriaId);
        
        if (categoria == null) return;
        
        var mensaje = new
        {
            title = "?? Alerta de Presupuesto",
            body = $"{categoria.Nombre}: Has gastado el {porcentaje:F1}% de tu presupuesto",
            icon = "/icons/icon-192x192.png",
            badge = "/icons/badge-72x72.png",
            tag = $"presupuesto-{categoriaId}",
            requireInteraction = true,
            data = new
            {
                url = $"/Categorias/Details/{categoriaId}",
                type = "presupuesto-excedido",
                id = categoriaId,
                porcentaje = porcentaje
            },
            actions = new[]
            {
                new { action = "view", title = "Ver Categoría" },
                new { action = "dismiss", title = "Entendido" }
            }
        };
        
        var payload = JsonSerializer.Serialize(mensaje);
        
        // Enviar a todos los usuarios del espacio
        foreach (var usuario in categoria.Espacio.Usuarios)
        {
            foreach (var subscription in usuario.PushSubscriptions)
            {
                try
                {
                    var pushSubscription = new PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth
                    );
                    
                    await _pushClient.SendNotificationAsync(pushSubscription, payload);
                }
                catch (WebPushException ex)
                {
                    Console.WriteLine($"? Error: {ex.Message}");
                    
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        _context.PushSubscriptions.Remove(subscription);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
```

#### **2.3 Database - Push Subscriptions**
```csharp
// Models/PushSubscription.cs
public class PushSubscription
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Endpoint { get; set; }
    public string P256dh { get; set; }
    public string Auth { get; set; }
    public DateTime CreadoEn { get; set; }
    
    // Navigation
    public Usuario Usuario { get; set; }
}

// Data/PresupuestoContext.cs - Agregar DbSet
public DbSet<PushSubscription> PushSubscriptions { get; set; }

// Migración
dotnet ef migrations add AddPushSubscriptions
dotnet ef database update
```

#### **2.4 Frontend - Push Subscription Manager**
```javascript
// wwwroot/js/push-manager.js
class PushManager {
    constructor() {
        this.publicVapidKey = null;
    }

    async init() {
        // Obtener clave pública del servidor
        const response = await fetch('/api/Push/GetPublicKey');
        const data = await response.json();
        this.publicVapidKey = data.publicKey;
    }

    async subscribe() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
            console.error('? Push no soportado');
            return false;
        }

        try {
            // Solicitar permisos
            const permission = await Notification.requestPermission();
            
            if (permission !== 'granted') {
                console.log('? Permisos de notificación denegados');
                return false;
            }

            // Obtener service worker
            const registration = await navigator.serviceWorker.ready;

            // Suscribirse a push
            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: this.urlBase64ToUint8Array(this.publicVapidKey)
            });

            console.log('? Suscripción creada:', subscription);

            // Enviar subscription al servidor
            await this.sendSubscriptionToServer(subscription);

            return true;
        } catch (error) {
            console.error('? Error en suscripción:', error);
            return false;
        }
    }

    async sendSubscriptionToServer(subscription) {
        const response = await fetch('/api/Push/Subscribe', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(subscription)
        });

        if (!response.ok) {
            throw new Error('Error guardando subscription');
        }

        console.log('? Subscription guardada en servidor');
    }

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

    async unsubscribe() {
        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();

        if (subscription) {
            await subscription.unsubscribe();
            
            // Notificar al servidor
            await fetch('/api/Push/Unsubscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ endpoint: subscription.endpoint })
            });

            console.log('? Desuscrito de notificaciones');
            return true;
        }

        return false;
    }
}

// Instancia global
window.pushManager = new PushManager();
```

#### **2.5 Backend API - Push Endpoints**
```csharp
// Controllers/Api/PushController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly PresupuestoContext _context;
    private readonly IConfiguration _configuration;
    
    public PushController(PresupuestoContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    [HttpGet("GetPublicKey")]
    public IActionResult GetPublicKey()
    {
        var publicKey = _configuration["VapidKeys:PublicKey"];
        return Ok(new { publicKey });
    }
    
    [HttpPost("Subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        var nombreUsuario = User.Identity.Name;
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        
        if (usuario == null)
        {
            return Unauthorized();
        }
        
        // Verificar si ya existe
        var existente = await _context.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint);
        
        if (existente != null)
        {
            return Ok(new { mensaje = "Subscription ya existe" });
        }
        
        // Crear nueva subscription
        var subscription = new PushSubscription
        {
            UsuarioId = usuario.Id,
            Endpoint = dto.Endpoint,
            P256dh = dto.Keys.P256dh,
            Auth = dto.Keys.Auth,
            CreadoEn = DateTime.Now
        };
        
        _context.PushSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        
        return Ok(new { mensaje = "Subscription guardada" });
    }
    
    [HttpPost("Unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint);
        
        if (subscription != null)
        {
            _context.PushSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();
        }
        
        return Ok(new { mensaje = "Desuscrito correctamente" });
    }
    
    [HttpPost("TestNotification")]
    public async Task<IActionResult> TestNotification()
    {
        var nombreUsuario = User.Identity.Name;
        var usuario = await _context.Usuarios
            .Include(u => u.PushSubscriptions)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        
        if (usuario == null || !usuario.PushSubscriptions.Any())
        {
            return BadRequest(new { error = "No hay subscriptions activas" });
        }
        
        var mensaje = new
        {
            title = "?? Notificación de Prueba",
            body = "Las notificaciones push están funcionando correctamente",
            icon = "/icons/icon-192x192.png",
            badge = "/icons/badge-72x72.png"
        };
        
        var payload = JsonSerializer.Serialize(mensaje);
        var pushClient = new WebPushClient();
        
        var vapidPublicKey = _configuration["VapidKeys:PublicKey"];
        var vapidPrivateKey = _configuration["VapidKeys:PrivateKey"];
        var vapidSubject = _configuration["VapidKeys:Subject"];
        
        pushClient.SetVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        
        foreach (var sub in usuario.PushSubscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(
                    sub.Endpoint,
                    sub.P256dh,
                    sub.Auth
                );
                
                await pushClient.SendNotificationAsync(pushSubscription, payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        return Ok(new { mensaje = "Notificación enviada" });
    }
}

// DTOs
public class PushSubscriptionDto
{
    public string Endpoint { get; set; }
    public long? ExpirationTime { get; set; }
    public PushKeysDto Keys { get; set; }
}

public class PushKeysDto
{
    public string P256dh { get; set; }
    public string Auth { get; set; }
}

public class UnsubscribeDto
{
    public string Endpoint { get; set; }
}
```

#### **2.6 Hangfire - Scheduled Notifications**
```csharp
// Services/NotificationSchedulerService.cs
public class NotificationSchedulerService
{
    private readonly PresupuestoContext _context;
    private readonly PushNotificationService _pushService;
    
    public NotificationSchedulerService(
        PresupuestoContext context,
        PushNotificationService pushService)
    {
        _context = context;
        _pushService = pushService;
    }
    
    [AutomaticRetry(Attempts = 3)]
    public async Task VerificarVencimientos()
    {
        Console.WriteLine("?? Verificando vencimientos...");
        
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        
        // Cuentas por cobrar que vencen mañana
        var cobrosProximos = await _context.CuentasPorCobrar
            .Where(c => !c.Completado && 
                       c.FechaVencimiento.Date == manana)
            .ToListAsync();
        
        foreach (var cobro in cobrosProximos)
        {
            await _pushService.EnviarNotificacionVencimiento(cobro.Id);
        }
        
        Console.WriteLine($"? {cobrosProximos.Count} notificaciones enviadas");
    }
    
    [AutomaticRetry(Attempts = 3)]
    public async Task VerificarPresupuestos()
    {
        Console.WriteLine("?? Verificando presupuestos...");
        
        var categorias = await _context.Categorias
            .Where(c => c.PresupuestoMensual > 0)
            .ToListAsync();
        
        var mesActual = DateTime.Now.Month;
        var añoActual = DateTime.Now.Year;
        
        foreach (var categoria in categorias)
        {
            var gastadoMes = await _context.Transacciones
                .Where(t => t.CategoriaId == categoria.Id &&
                           t.Tipo == 1 && // Egreso
                           t.Fecha.Month == mesActual &&
                           t.Fecha.Year == añoActual)
                .SumAsync(t => t.Monto);
            
            var porcentaje = (gastadoMes / categoria.PresupuestoMensual) * 100;
            
            // Notificar si se excedió el 80% o 100%
            if (porcentaje >= 80)
            {
                await _pushService.EnviarNotificacionPresupuestoExcedido(
                    categoria.Id, 
                    porcentaje
                );
            }
        }
        
        Console.WriteLine($"? Verificación de presupuestos completada");
    }
}

// Program.cs - Configurar jobs de Hangfire
RecurringJob.AddOrUpdate<NotificationSchedulerService>(
    "VerificarVencimientos",
    service => service.VerificarVencimientos(),
    "0 9 * * *" // Todos los días a las 9:00 AM
);

RecurringJob.AddOrUpdate<NotificationSchedulerService>(
    "VerificarPresupuestos",
    service => service.VerificarPresupuestos(),
    "0 20 * * *" // Todos los días a las 8:00 PM
);
```

### **appsettings.json - VAPID Configuration**
```json
{
  "VapidKeys": {
    "PublicKey": "TU_CLAVE_PUBLICA_AQUI",
    "PrivateKey": "TU_CLAVE_PRIVADA_AQUI",
    "Subject": "mailto:tu-email@ejemplo.com"
  }
}
```

### **Archivos a Crear:**
- ? `wwwroot/js/push-manager.js`
- ? `Services/PushNotificationService.cs`
- ? `Services/NotificationSchedulerService.cs`
- ? `Controllers/Api/PushController.cs`
- ? `Models/PushSubscription.cs`
- ? `Models/DTOs/PushSubscriptionDto.cs`

### **Paquetes NuGet:**
```powershell
dotnet add package WebPush
```

### **Testing:**
1. Inicializar push manager en la app
2. Solicitar permisos de notificación
3. Suscribirse a notificaciones
4. Probar con endpoint `/api/Push/TestNotification`
5. Verificar notificaciones en diferentes navegadores

---

## ?? **FASE 3: SHARE TARGET API**

### **Objetivo:**
Permitir compartir imágenes de tickets/recibos desde la galería o cámara directamente a la app y procesarlas con OCR.

### **Componentes a Desarrollar:**

#### **3.1 Manifest - Share Target**
```json
// wwwroot/manifest.json - Agregar share_target
{
  "share_target": {
    "action": "/Transacciones/CreateFromImage",
    "method": "POST",
    "enctype": "multipart/form-data",
    "params": {
      "title": "descripcion",
      "text": "nota",
      "files": [
        {
          "name": "imagen",
          "accept": ["image/*"]
        }
      ]
    }
  }
}
```

#### **3.2 Backend - OCR Service**
```csharp
// Services/OcrService.cs
using Tesseract;
using System.Text.RegularExpressions;

public class OcrService
{
    private readonly IWebHostEnvironment _env;
    
    public OcrService(IWebHostEnvironment env)
    {
        _env = env;
    }
    
    public async Task<TransaccionOcrResult> ProcessTicket(IFormFile imagen)
    {
        // Guardar imagen temporalmente
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
        
        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await imagen.CopyToAsync(stream);
        }
        
        try
        {
            // Procesar con Tesseract
            var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");
            
            using (var engine = new TesseractEngine(tessDataPath, "spa", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(tempPath))
                {
                    using (var page = engine.Process(img))
                    {
                        var text = page.GetText();
                        
                        // Extraer información del texto
                        var result = ExtractTransactionData(text);
                        result.TextoCompleto = text;
                        result.Confianza = page.GetMeanConfidence();
                        
                        return result;
                    }
                }
            }
        }
        finally
        {
            // Limpiar archivo temporal
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
    
    private TransaccionOcrResult ExtractTransactionData(string text)
    {
        var result = new TransaccionOcrResult();
        
        // Extraer monto (buscar patrón: S/ 150.00, $150.00, etc)
        var montoRegex = new Regex(@"(?:S\/|USD|\$)\s*(\d+(?:\.\d{2})?)");
        var montoMatch = montoRegex.Match(text);
        
        if (montoMatch.Success && decimal.TryParse(montoMatch.Groups[1].Value, out decimal monto))
        {
            result.Monto = monto;
        }
        
        // Extraer fecha (dd/mm/yyyy, dd-mm-yyyy, etc)
        var fechaRegex = new Regex(@"(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})");
        var fechaMatch = fechaRegex.Match(text);
        
        if (fechaMatch.Success)
        {
            try
            {
                var dia = int.Parse(fechaMatch.Groups[1].Value);
                var mes = int.Parse(fechaMatch.Groups[2].Value);
                var año = int.Parse(fechaMatch.Groups[3].Value);
                
                if (año < 100)
                {
                    año += 2000;
                }
                
                result.Fecha = new DateTime(año, mes, dia);
            }
            catch
            {
                result.Fecha = DateTime.Now;
            }
        }
        else
        {
            result.Fecha = DateTime.Now;
        }
        
        // Extraer descripción (primera línea no vacía)
        var lineas = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        result.Descripcion = lineas.FirstOrDefault()?.Trim() ?? "Gasto desde imagen";
        
        // Detectar establecimiento común
        var establecimientos = new Dictionary<string, string>
        {
            { "wong", "Wong" },
            { "metro", "Metro" },
            { "plaza vea", "Plaza Vea" },
            { "tottus", "Tottus" },
            { "makro", "Makro" }
        };
        
        var textLower = text.ToLower();
        foreach (var est in establecimientos)
        {
            if (textLower.Contains(est.Key))
            {
                result.Establecimiento = est.Value;
                if (string.IsNullOrEmpty(result.Descripcion) || result.Descripcion == "Gasto desde imagen")
                {
                    result.Descripcion = $"Compra en {est.Value}";
                }
                break;
            }
        }
        
        return result;
    }
}

// Models/TransaccionOcrResult.cs
public class TransaccionOcrResult
{
    public decimal? Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; }
    public string Establecimiento { get; set; }
    public string TextoCompleto { get; set; }
    public float Confianza { get; set; }
}
```

#### **3.3 Controller - Share Target Handler**
```csharp
// Controllers/TransaccionesController.cs
[HttpPost("CreateFromImage")]
[Authorize]
public async Task<IActionResult> CreateFromImage([FromForm] IFormFile imagen, string descripcion, string nota)
{
    if (imagen == null || imagen.Length == 0)
    {
        TempData["Error"] = "No se recibió ninguna imagen";
        return RedirectToAction("Create");
    }
    
    try
    {
        // Procesar imagen con OCR
        var ocrService = new OcrService(_env);
        var resultado = await ocrService.ProcessTicket(imagen);
        
        // Guardar imagen
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "tickets");
        Directory.CreateDirectory(uploadsFolder);
        
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imagen.CopyToAsync(stream);
        }
        
        // Pre-llenar el formulario con datos extraídos
        ViewBag.MontoExtraido = resultado.Monto;
        ViewBag.FechaExtraida = resultado.Fecha;
        ViewBag.DescripcionExtraida = resultado.Descripcion;
        ViewBag.Establecimiento = resultado.Establecimiento;
        ViewBag.ImagenPath = $"/uploads/tickets/{fileName}";
        ViewBag.ConfianzaOcr = resultado.Confianza;
        ViewBag.TextoCompleto = resultado.TextoCompleto;
        
        // Obtener cuentas y categorías
        var nombreUsuario = User.Identity.Name;
        var usuario = await _context.Usuarios
            .Include(u => u.Espacios)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        
        var espacioActivo = usuario.Espacios.FirstOrDefault();
        
        ViewBag.Cuentas = await _context.Cuentas
            .Where(c => c.EspacioId == espacioActivo.Id)
            .ToListAsync();
        
        ViewBag.Categorias = await _context.Categorias
            .Where(c => c.EspacioId == espacioActivo.Id && c.Tipo == 1)
            .ToListAsync();
        
        return View("CreateFromOcr");
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Error procesando imagen: {ex.Message}";
        return RedirectToAction("Create");
    }
}
```

#### **3.4 View - Create From OCR**
```razor
@* Views/Transacciones/CreateFromOcr.cshtml *@
@{
    ViewData["Title"] = "Nueva Transacción desde Imagen";
}

<div class="max-w-4xl mx-auto">
    <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-xl p-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-6">
            ?? Nueva Transacción desde Imagen
        </h1>

        @if (ViewBag.ImagenPath != null)
        {
            <div class="mb-6 bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 rounded-lg p-4">
                <div class="flex items-start gap-4">
                    <div class="text-3xl">??</div>
                    <div class="flex-1">
                        <h4 class="text-lg font-bold text-blue-900 dark:text-blue-300 mb-2">
                            Datos Extraídos Automáticamente
                        </h4>
                        <p class="text-blue-800 dark:text-blue-200 text-sm mb-2">
                            Confianza del OCR: <strong>@((ViewBag.ConfianzaOcr * 100).ToString("F1"))%</strong>
                        </p>
                        <p class="text-xs text-blue-700 dark:text-blue-300">
                            Revisa y ajusta los datos si es necesario
                        </p>
                    </div>
                </div>
            </div>

            <!-- Imagen del ticket -->
            <div class="mb-6">
                <img src="@ViewBag.ImagenPath" 
                     alt="Ticket escaneado" 
                     class="max-w-full h-auto rounded-lg shadow-lg border-2 border-gray-200 dark:border-gray-700" />
            </div>
        }

        <!-- Formulario pre-llenado -->
        <form asp-action="Create" method="post" class="space-y-6">
            <input type="hidden" name="ImagenPath" value="@ViewBag.ImagenPath" />
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Descripción -->
                <div class="md:col-span-2">
                    <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                        Descripción
                    </label>
                    <input type="text" 
                           name="Descripcion" 
                           value="@ViewBag.DescripcionExtraida"
                           class="w-full px-4 py-3 border-2 border-gray-200 dark:border-gray-700 rounded-xl focus:border-blue-500 focus:ring-4 focus:ring-blue-100 dark:bg-slate-700 dark:text-white transition"
                           required />
                    @if (ViewBag.Establecimiento != null)
                    {
                        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
                            ?? Establecimiento detectado: <strong>@ViewBag.Establecimiento</strong>
                        </p>
                    }
                </div>

                <!-- Monto -->
                <div>
                    <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                        Monto
                    </label>
                    <input type="number" 
                           name="Monto" 
                           value="@ViewBag.MontoExtraido"
                           step="0.01" 
                           min="0"
                           class="w-full px-4 py-3 border-2 border-gray-200 dark:border-gray-700 rounded-xl focus:border-blue-500 focus:ring-4 focus:ring-blue-100 dark:bg-slate-700 dark:text-white transition"
                           required />
                </div>

                <!-- Fecha -->
                <div>
                    <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                        Fecha
                    </label>
                    <input type="date" 
                           name="Fecha" 
                           value="@ViewBag.FechaExtraida?.ToString("yyyy-MM-dd")"
                           class="w-full px-4 py-3 border-2 border-gray-200 dark:border-gray-700 rounded-xl focus:border-blue-500 focus:ring-4 focus:ring-blue-100 dark:bg-slate-700 dark:text-white transition"
                           required />
                </div>

                <!-- Cuenta -->
                <div>
                    <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                        Cuenta
                    </label>
                    <select name="CuentaId" 
                            class="w-full px-4 py-3 border-2 border-gray-200 dark:border-gray-700 rounded-xl focus:border-blue-500 focus:ring-4 focus:ring-blue-100 dark:bg-slate-700 dark:text-white transition"
                            required>
                        <option value="">Seleccionar cuenta</option>
                        @foreach (var cuenta in ViewBag.Cuentas)
                        {
                            <option value="@cuenta.Id">@cuenta.Nombre</option>
                        }
                    </select>
                </div>

                <!-- Categoría -->
                <div>
                    <label class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                        Categoría
                    </label>
                    <select name="CategoriaId" 
                            class="w-full px-4 py-3 border-2 border-gray-200 dark:border-gray-700 rounded-xl focus:border-blue-500 focus:ring-4 focus:ring-blue-100 dark:bg-slate-700 dark:text-white transition">
                        <option value="">Sin categoría</option>
                        @foreach (var categoria in ViewBag.Categorias)
                        {
                            <option value="@categoria.Id">@categoria.Nombre</option>
                        }
                    </select>
                </div>
            </div>

            <!-- Texto completo extraído (colapsable) -->
            @if (ViewBag.TextoCompleto != null)
            {
                <details class="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
                    <summary class="cursor-pointer font-semibold text-gray-700 dark:text-gray-300">
                        ?? Ver texto completo extraído
                    </summary>
                    <pre class="mt-4 text-xs text-gray-600 dark:text-gray-400 whitespace-pre-wrap">@ViewBag.TextoCompleto</pre>
                </details>
            }

            <!-- Botones -->
            <div class="flex gap-4">
                <button type="submit" 
                        class="flex-1 py-3 bg-gradient-to-r from-green-500 to-emerald-600 text-white font-bold rounded-xl hover:from-green-600 hover:to-emerald-700 transition shadow-lg">
                    Guardar Transacción
                </button>
                <a asp-action="Create" 
                   class="px-6 py-3 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 font-semibold rounded-xl hover:bg-gray-300 dark:hover:bg-gray-600 transition">
                    Cancelar
                </a>
            </div>
        </form>
    </div>
</div>
```

### **Paquetes NuGet:**
```powershell
dotnet add package Tesseract
```

### **Descargar tessdata:**
```powershell
# Crear carpeta tessdata en la raíz del proyecto
mkdir tessdata

# Descargar datos de lenguaje español
# https://github.com/tesseract-ocr/tessdata/blob/main/spa.traineddata
# Copiar spa.traineddata a /tessdata/
```

### **Archivos a Crear:**
- ? `Services/OcrService.cs`
- ? `Models/TransaccionOcrResult.cs`
- ? `Views/Transacciones/CreateFromOcr.cshtml`
- ? `wwwroot/uploads/tickets/` (carpeta)
- ? `tessdata/spa.traineddata` (archivo de datos)

### **Modificaciones:**
- ? `wwwroot/manifest.json` - Agregar share_target
- ? `Controllers/TransaccionesController.cs` - Agregar CreateFromImage

### **Testing:**
1. Instalar la PWA en móvil Android
2. Tomar foto de un ticket con la cámara
3. Abrir la galería y compartir la imagen
4. Seleccionar "Presupuesto Familiar App"
5. Verificar que los datos se extraen correctamente

---

## ?? **FASE 4: SONIDO E INTERNACIONALIZACIÓN**

### **Objetivo:**
Agregar sonidos de notificación y soporte multiidioma (Español/Inglés).

### **Componentes a Desarrollar:**

#### **4.1 Sound Manager**
```javascript
// wwwroot/js/sound-manager.js
class SoundManager {
    constructor() {
        this.sounds = {
            notification: new Audio('/sounds/notification.mp3'),
            success: new Audio('/sounds/success.mp3'),
            error: new Audio('/sounds/error.mp3'),
            warning: new Audio('/sounds/warning.mp3'),
            sync: new Audio('/sounds/sync.mp3')
        };
        
        // Volumen por defecto
        Object.values(this.sounds).forEach(sound => {
            sound.volume = 0.3;
        });
    }

    play(soundName) {
        if (this.sounds[soundName]) {
            this.sounds[soundName].currentTime = 0;
            this.sounds[soundName].play().catch(err => {
                console.warn('No se pudo reproducir sonido:', err);
            });
        }
    }

    setVolume(volume) {
        Object.values(this.sounds).forEach(sound => {
            sound.volume = Math.max(0, Math.min(1, volume));
        });
    }
}

// Instancia global
window.soundManager = new SoundManager();
```

#### **4.2 Sonidos**
Descargar sonidos gratuitos de:
- https://mixkit.co/free-sound-effects/notification/
- https://freesound.org/

Archivos necesarios:
- `wwwroot/sounds/notification.mp3` (notificación general)
- `wwwroot/sounds/success.mp3` (acción exitosa)
- `wwwroot/sounds/error.mp3` (error)
- `wwwroot/sounds/warning.mp3` (advertencia)
- `wwwroot/sounds/sync.mp3` (sincronización)

#### **4.3 i18n Manager**
```javascript
// wwwroot/js/i18n-manager.js
class I18nManager {
    constructor() {
        this.currentLocale = localStorage.getItem('locale') || 'es';
        this.translations = {};
        this.loadTranslations();
    }

    async loadTranslations() {
        try {
            const response = await fetch(`/locales/${this.currentLocale}.json`);
            this.translations = await response.json();
            this.applyTranslations();
        } catch (error) {
            console.error('Error cargando traducciones:', error);
        }
    }

    async setLocale(locale) {
        this.currentLocale = locale;
        localStorage.setItem('locale', locale);
        await this.loadTranslations();
        
        // Recargar página
        window.location.reload();
    }

    translate(key) {
        return this.translations[key] || key;
    }

    applyTranslations() {
        document.querySelectorAll('[data-i18n]').forEach(element => {
            const key = element.getAttribute('data-i18n');
            element.textContent = this.translate(key);
        });
    }
}

// Instancia global
window.i18n = new I18nManager();
```

#### **4.4 Translation Files**
```json
// wwwroot/locales/es.json
{
  "app.title": "Presupuesto Familiar",
  "menu.dashboard": "Dashboard",
  "menu.accounts": "Cuentas",
  "menu.transactions": "Historial",
  "menu.budgets": "Presupuestos",
  "menu.subscriptions": "Suscripciones",
  "menu.debtors": "Deudores",
  "menu.transfer": "Transferir",
  "menu.new": "Nuevo",
  "notification.sync.complete": "Sincronización completada",
  "notification.update.available": "¡Nueva versión disponible!",
  "notification.transaction.created": "Transacción creada exitosamente",
  "button.save": "Guardar",
  "button.cancel": "Cancelar",
  "button.update": "Actualizar ahora",
  "button.later": "Más tarde",
  "form.description": "Descripción",
  "form.amount": "Monto",
  "form.date": "Fecha",
  "form.account": "Cuenta",
  "form.category": "Categoría"
}
```

```json
// wwwroot/locales/en.json
{
  "app.title": "Family Budget",
  "menu.dashboard": "Dashboard",
  "menu.accounts": "Accounts",
  "menu.transactions": "History",
  "menu.budgets": "Budgets",
  "menu.subscriptions": "Subscriptions",
  "menu.debtors": "Debtors",
  "menu.transfer": "Transfer",
  "menu.new": "New",
  "notification.sync.complete": "Synchronization completed",
  "notification.update.available": "New version available!",
  "notification.transaction.created": "Transaction created successfully",
  "button.save": "Save",
  "button.cancel": "Cancel",
  "button.update": "Update now",
  "button.later": "Later",
  "form.description": "Description",
  "form.amount": "Amount",
  "form.date": "Date",
  "form.account": "Account",
  "form.category": "Category"
}
```

#### **4.5 Language Selector Component**
```html
<!-- Views/Shared/_LanguageSelector.cshtml -->
<div class="relative" x-data="{ open: false }">
    <button @@click="open = !open" 
            class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition">
        <span id="current-locale" class="text-xl">??</span>
        <span class="hidden sm:inline font-medium" data-i18n="language.current">ES</span>
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
        </svg>
    </button>
    
    <div x-show="open" 
         @@click.away="open = false"
         class="absolute right-0 mt-2 w-48 bg-white dark:bg-slate-800 rounded-xl shadow-xl border border-slate-200 dark:border-slate-700 overflow-hidden z-50">
        <button onclick="window.i18n.setLocale('es')" 
                class="w-full text-left px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-700 transition flex items-center gap-3">
            <span class="text-xl">????</span>
            <span>Español</span>
        </button>
        <button onclick="window.i18n.setLocale('en')" 
                class="w-full text-left px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-700 transition flex items-center gap-3">
            <span class="text-xl">????</span>
            <span>English</span>
        </button>
    </div>
</div>
```

### **Integración con Notificaciones:**
```javascript
// Modificar showUpdateNotification en pwa-installer.js
function showUpdateNotification() {
    // Reproducir sonido
    window.soundManager.play('notification');
    
    // Obtener texto traducido
    const title = window.i18n.translate('notification.update.available');
    const body = window.i18n.translate('notification.update.description');
    const updateBtn = window.i18n.translate('button.update');
    const laterBtn = window.i18n.translate('button.later');
    
    // ... resto del código con textos traducidos
}
```

### **Archivos a Crear:**
- ? `wwwroot/js/sound-manager.js`
- ? `wwwroot/js/i18n-manager.js`
- ? `wwwroot/locales/es.json`
- ? `wwwroot/locales/en.json`
- ? `wwwroot/sounds/notification.mp3`
- ? `wwwroot/sounds/success.mp3`
- ? `wwwroot/sounds/error.mp3`
- ? `wwwroot/sounds/warning.mp3`
- ? `wwwroot/sounds/sync.mp3`
- ? `Views/Shared/_LanguageSelector.cshtml`

---

## ?? **FASE 5: CHANGELOG**

### **Objetivo:**
Mostrar historial de versiones y cambios en el footer de la aplicación.

### **Componentes a Desarrollar:**

#### **5.1 Changelog Data**
```json
// wwwroot/data/changelog.json
{
  "versions": [
    {
      "version": "2.0.0",
      "date": "2025-01-25",
      "type": "major",
      "changes": [
        {
          "type": "feature",
          "description": "Notificaciones push para vencimientos"
        },
        {
          "type": "feature",
          "description": "Sincronización automática en segundo plano"
        },
        {
          "type": "feature",
          "description": "OCR para procesar tickets desde imágenes"
        },
        {
          "type": "improvement",
          "description": "Soporte multiidioma (Español/Inglés)"
        },
        {
          "type": "improvement",
          "description": "Sonidos de notificación"
        }
      ]
    },
    {
      "version": "1.1.0",
      "date": "2025-01-18",
      "type": "minor",
      "changes": [
        {
          "type": "feature",
          "description": "Sistema de notificaciones PWA mejorado"
        },
        {
          "type": "improvement",
          "description": "Diseño de toast de actualización renovado"
        },
        {
          "type": "fix",
          "description": "Corregido error en login con email"
        }
      ]
    },
    {
      "version": "1.0.0",
      "date": "2025-01-01",
      "type": "major",
      "changes": [
        {
          "type": "feature",
          "description": "Lanzamiento inicial de la aplicación"
        },
        {
          "type": "feature",
          "description": "Gestión de cuentas y transacciones"
        },
        {
          "type": "feature",
          "description": "Presupuestos por categoría"
        },
        {
          "type": "feature",
          "description": "Control de cuentas por cobrar"
        }
      ]
    }
  ]
}
```

#### **5.2 Changelog Component**
```razor
@* Views/Shared/_Changelog.cshtml *@
<div class="bg-white dark:bg-slate-800 rounded-2xl shadow-xl p-8 max-w-4xl mx-auto">
    <div class="flex items-center gap-3 mb-6">
        <span class="text-4xl">??</span>
        <h2 class="text-3xl font-bold text-gray-900 dark:text-white">Historial de Versiones</h2>
    </div>
    
    <div id="changelog-container" class="space-y-6">
        <!-- Se llenará dinámicamente con JavaScript -->
    </div>
</div>

<script>
    async function loadChangelog() {
        try {
            const response = await fetch('/data/changelog.json');
            const data = await response.json();
            
            const container = document.getElementById('changelog-container');
            
            data.versions.forEach(version => {
                const versionDiv = document.createElement('div');
                versionDiv.className = 'border-l-4 border-blue-500 pl-6 pb-6';
                
                // Header de versión
                const header = document.createElement('div');
                header.className = 'flex items-center gap-3 mb-3';
                header.innerHTML = `
                    <span class="text-2xl font-bold text-gray-900 dark:text-white">v${version.version}</span>
                    <span class="px-3 py-1 bg-${getTypeColor(version.type)}-100 dark:bg-${getTypeColor(version.type)}-900/30 text-${getTypeColor(version.type)}-700 dark:text-${getTypeColor(version.type)}-300 text-xs font-semibold rounded-full">
                        ${getTypeLabel(version.type)}
                    </span>
                    <span class="text-sm text-gray-500 dark:text-gray-400">${formatDate(version.date)}</span>
                `;
                
                // Lista de cambios
                const changesList = document.createElement('ul');
                changesList.className = 'space-y-2 mt-3';
                
                version.changes.forEach(change => {
                    const li = document.createElement('li');
                    li.className = 'flex items-start gap-2 text-gray-700 dark:text-gray-300';
                    li.innerHTML = `
                        <span class="text-lg">${getChangeIcon(change.type)}</span>
                        <span>${change.description}</span>
                    `;
                    changesList.appendChild(li);
                });
                
                versionDiv.appendChild(header);
                versionDiv.appendChild(changesList);
                container.appendChild(versionDiv);
            });
        } catch (error) {
            console.error('Error cargando changelog:', error);
        }
    }
    
    function getTypeColor(type) {
        const colors = {
            'major': 'purple',
            'minor': 'blue',
            'patch': 'green'
        };
        return colors[type] || 'gray';
    }
    
    function getTypeLabel(type) {
        const labels = {
            'major': 'Mayor',
            'minor': 'Menor',
            'patch': 'Parche'
        };
        return labels[type] || type;
    }
    
    function getChangeIcon(type) {
        const icons = {
            'feature': '?',
            'improvement': '??',
            'fix': '??',
            'security': '??',
            'performance': '?',
            'breaking': '??'
        };
        return icons[type] || '??';
    }
    
    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('es-ES', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    }
    
    // Cargar al iniciar
    document.addEventListener('DOMContentLoaded', loadChangelog);
</script>
```

#### **5.3 Footer con Link a Changelog**
```razor
@* Views/Shared/_Layout.cshtml - Modificar footer *@
<footer class="border-t border-slate-200 dark:border-slate-700 bg-white/50 dark:bg-slate-900/50 backdrop-blur-sm mt-12">
    <div class="container mx-auto px-4 lg:px-6 py-6">
        <div class="flex flex-col md:flex-row items-center justify-between gap-4">
            <p class="text-center text-slate-600 dark:text-slate-400 text-sm">
                &copy; 2026 - PresupuestoFamiliarApp
            </p>
            
            <div class="flex items-center gap-4">
                <span class="text-xs text-slate-500 dark:text-slate-400">
                    v2.0.0
                </span>
                <button onclick="showChangelogModal()" 
                        class="text-sm text-blue-600 dark:text-blue-400 hover:underline font-semibold">
                    ?? Ver Cambios
                </button>
                <a href="#" class="text-sm text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white">
                    Privacidad
                </a>
                <a href="#" class="text-sm text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white">
                    Términos
                </a>
            </div>
        </div>
    </div>
</footer>

<!-- Changelog Modal -->
<div id="changelog-modal" class="hidden fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
    <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-2xl max-w-4xl w-full max-h-[80vh] overflow-y-auto p-8">
        <div class="flex items-center justify-between mb-6">
            <h2 class="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
                <span>??</span> Historial de Versiones
            </h2>
            <button onclick="hideChangelogModal()" 
                    class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                </svg>
            </button>
        </div>
        
        @await Html.PartialAsync("_Changelog")
    </div>
</div>

<script>
    function showChangelogModal() {
        document.getElementById('changelog-modal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
    
    function hideChangelogModal() {
        document.getElementById('changelog-modal').classList.add('hidden');
        document.body.style.overflow = '';
    }
</script>
```

### **Archivos a Crear:**
- ? `wwwroot/data/changelog.json`
- ? `Views/Shared/_Changelog.cshtml`

### **Modificaciones:**
- ? `Views/Shared/_Layout.cshtml` - Agregar link y modal de changelog

---

## ?? **TIMELINE Y PRIORIZACIÓN**

### **Sprint 1 (Semana 1-2): Background Sync**
| Día | Tarea | Estimación |
|-----|-------|------------|
| 1-2 | Implementar IndexedDB Manager | 8h |
| 3-4 | Implementar Background Sync Manager | 8h |
| 5 | Modificar Service Worker | 4h |
| 6-7 | Crear API de sincronización | 6h |
| 8 | Modificar formularios para offline | 4h |
| 9-10 | Testing y ajustes | 6h |

**Total: 36 horas (~1.5 semanas)**

### **Sprint 2 (Semana 3-4): Notificaciones Push**
| Día | Tarea | Estimación |
|-----|-------|------------|
| 1 | Generar VAPID keys y configurar | 2h |
| 2-3 | Implementar PushNotificationService | 8h |
| 4-5 | Crear modelos y migraciones BD | 4h |
| 6-7 | Implementar Push Manager frontend | 6h |
| 8-9 | Crear API endpoints | 6h |
| 10-11 | Configurar Hangfire jobs | 4h |
| 12-14 | Testing exhaustivo | 10h |

**Total: 40 horas (~2 semanas)**

### **Sprint 3 (Semana 5-6): Share Target & OCR**
| Día | Tarea | Estimación |
|-----|-------|------------|
| 1 | Configurar Tesseract y tessdata | 4h |
| 2-3 | Implementar OCR Service | 10h |
| 4-5 | Crear Share Target handler | 6h |
| 6-7 | Diseñar vista CreateFromOcr | 6h |
| 8-9 | Optimizar extracción de datos | 8h |
| 10-12 | Testing con diferentes tickets | 12h |

**Total: 46 horas (~2 semanas)**

### **Sprint 4 (Semana 7): Sonido + i18n**
| Día | Tarea | Estimación |
|-----|-------|------------|
| 1 | Descargar/crear sonidos | 2h |
| 2 | Implementar Sound Manager | 4h |
| 3-4 | Crear archivos de traducción | 6h |
| 5 | Implementar i18n Manager | 4h |
| 6 | Crear selector de idioma | 3h |
| 7 | Integrar sonidos en notificaciones | 3h |
| 8-9 | Traducir toda la interfaz | 8h |
| 10 | Testing | 4h |

**Total: 34 horas (~1.5 semanas)**

### **Sprint 5 (Semana 8): Changelog**
| Día | Tarea | Estimación |
|-----|-------|------------|
| 1 | Crear estructura JSON changelog | 2h |
| 2 | Diseñar componente visual | 4h |
| 3 | Implementar modal de changelog | 3h |
| 4 | Integrar en footer | 2h |
| 5 | Documentar proceso de actualización | 3h |
| 6-7 | Testing y pulido final | 6h |

**Total: 20 horas (~1 semana)**

---

## **TIEMPO TOTAL ESTIMADO: 176 horas (~8 semanas)**

---

## ?? **STACK TECNOLÓGICO**

### **Frontend:**
- ? **IndexedDB** - Base de datos local
- ? **Service Worker API** - Background sync y caché
- ? **Notification API** - Notificaciones locales
- ? **Web Push API** - Notificaciones push del servidor
- ? **Share Target API** - Recibir imágenes compartidas
- ? **Web Audio API** - Reproducción de sonidos

### **Backend:**
- ? **ASP.NET Core 9** - Framework principal
- ? **Entity Framework Core** - ORM
- ? **Hangfire** - Jobs programados
- ? **WebPush** (NuGet) - Envío de push notifications
- ? **Tesseract** (NuGet) - OCR para procesar imágenes

### **Base de Datos:**
- ? **SQL Server** - Base de datos principal
- ? **IndexedDB** - Almacenamiento local del cliente

### **Herramientas:**
- ? **npm/web-push** - Generación de VAPID keys
- ? **Tesseract traineddata** - Datos de entrenamiento OCR

---

## ?? **MÉTRICAS DE ÉXITO**

### **KPIs a Medir:**

| Métrica | Objetivo | Herramienta |
|---------|----------|-------------|
| **Tiempo de respuesta offline** | < 200ms | Lighthouse |
| **Tasa de sincronización exitosa** | > 95% | Analytics |
| **Precisión OCR** | > 80% | Testing manual |
| **Tasa de instalación PWA** | > 30% usuarios | Analytics |
| **Engagement con notificaciones** | > 50% click-through | Push Analytics |
| **Lighthouse PWA Score** | > 90 | Lighthouse |

---

## ?? **PRÓXIMOS PASOS INMEDIATOS**

### **1. Decisión de Priorización**
¿Qué fase quieres implementar primero?
- **Opción A**: Background Sync (más impacto en UX offline)
- **Opción B**: Notificaciones Push (más engagement)
- **Opción C**: Share Target + OCR (más innovador)

### **2. Preparación del Entorno**
```powershell
# Instalar dependencias
dotnet add package WebPush
dotnet add package Tesseract
npm install -g web-push
```

### **3. Configuración Inicial**
```powershell
# Generar VAPID keys
web-push generate-vapid-keys

# Agregar al appsettings.json
# Descargar tessdata
```

---

## ?? **CONCLUSIÓN**

Este plan proporciona una hoja de ruta completa para transformar tu aplicación en una PWA de nivel enterprise con:

? **Funcionalidad Offline Completa**  
? **Notificaciones Inteligentes**  
? **Procesamiento de Imágenes**  
? **Experiencia Multiidioma**  
? **Transparencia de Versiones**  

**¿Quieres que empecemos con alguna fase específica?** ??

Puedo generar el código completo para cualquiera de las 5 fases de inmediato.
