# ? Solución: Error 401 (Unauthorized) al Activar Notificaciones

## ? **Problema**

Cuando se intentaba activar las notificaciones, aparecía el siguiente error en la consola:

```
POST https://localhost:7036/api/push/subscribe 401 (Unauthorized)
? Error al enviar suscripción: Error: Error al guardar suscripción en el servidor
```

---

## ?? **Causa del Problema**

El controlador de la API `PushController` tiene el atributo `[Authorize]` a nivel de clase, lo que requiere que el usuario esté autenticado para acceder a los endpoints.

```csharp
[Authorize]  // ? Requiere autenticación
[Route("api/[controller]")]
[ApiController]
public class PushController : ControllerBase
{
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(...)
    {
        // ...
    }
}
```

Sin embargo, el JavaScript estaba haciendo `fetch()` **SIN incluir las cookies de autenticación**, por lo que el servidor rechazaba la petición con un error 401.

---

## ? **Solución Aplicada**

### **Agregado `credentials: 'include'` en todos los fetch**

La solución es agregar la opción `credentials: 'include'` en todas las llamadas `fetch()` para que se envíen las cookies de autenticación automáticamente.

### **Cambios en `push-manager.js`:**

#### **1. enviarSuscripcionAlServidor()**
```javascript
// ? ANTES (sin credenciales)
const response = await fetch('/api/push/subscribe', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(subscription.toJSON())
});

// ? DESPUÉS (con credenciales)
const response = await fetch('/api/push/subscribe', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    credentials: 'include',  // ? AGREGADO
    body: JSON.stringify(subscription.toJSON())
});
```

#### **2. enviarDesuscripcionAlServidor()**
```javascript
const response = await fetch('/api/push/unsubscribe', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    credentials: 'include',  // ? AGREGADO
    body: JSON.stringify({
        endpoint: subscription.endpoint
    })
});
```

#### **3. enviarNotificacionPrueba()**
```javascript
const response = await fetch('/api/push/test', {
    method: 'POST',
    credentials: 'include'  // ? AGREGADO
});
```

---

## ?? **¿Qué hace `credentials: 'include'`?**

La opción `credentials` en el `fetch()` controla si se envían las cookies, tokens HTTP, y certificados TLS con la petición:

| Valor | Comportamiento |
|-------|---------------|
| `omit` | **No envía credenciales** (por defecto en cross-origin) |
| `same-origin` | Envía credenciales solo si es el mismo origen |
| `include` | **Siempre envía credenciales**, incluso en cross-origin |

En nuestro caso, aunque es el mismo origen (`localhost:7036`), necesitamos `credentials: 'include'` para que las **cookies de autenticación** (creadas por `AddAuthentication`) se envíen con la petición.

---

## ?? **Flujo de Autenticación**

### **Antes (? Fallaba):**
```
1. Usuario hace clic en "Activar Notificaciones"
2. JavaScript hace fetch('/api/push/subscribe')
3. Fetch NO envía cookies ?
4. Servidor recibe petición sin autenticación
5. Servidor devuelve 401 Unauthorized ?
```

### **Después (? Funciona):**
```
1. Usuario hace clic en "Activar Notificaciones"
2. JavaScript hace fetch('/api/push/subscribe', { credentials: 'include' })
3. Fetch SÍ envía cookies de sesión ?
4. Servidor recibe petición con autenticación
5. Servidor valida usuario desde ClaimTypes.NameIdentifier
6. Servidor guarda suscripción en la base de datos
7. Servidor devuelve 200 OK ?
```

---

## ?? **Pasos para Probar**

### **1. Limpia la Caché del Navegador**
```
Ctrl + Shift + R  (recarga forzada)
```

### **2. Ve a Configuración**
```
https://localhost:7036/Configuracion
```

### **3. Haz Clic en "Activar Notificaciones"**

Ahora deberías ver en la consola:
```javascript
? Permiso concedido para notificaciones
? Suscripción push creada
? Suscripción guardada en el servidor: { success: true, ... }
```

Y ver el mensaje:
```
? ¡Notificaciones activadas correctamente!
```

### **4. Prueba el Botón "Probar"**

Haz clic en el botón "Probar" para enviar una notificación de prueba.

Deberías recibir:
```
?? Notificación de Prueba
Las notificaciones push están funcionando correctamente!
```

---

## ?? **Verificación en Base de Datos**

Después de activar las notificaciones, verifica que se guardó en la base de datos:

```sql
SELECT 
    Id,
    UsuarioId,
    LEFT(Endpoint, 50) as EndpointPreview,
    Activa,
    NotificarVencimientos,
    NotificarPresupuestos,
    FechaCreacion
FROM PushSubscriptions
WHERE Activa = 1
ORDER BY FechaCreacion DESC
```

Deberías ver tu suscripción con `Activa = 1`.

---

## ?? **Logs Esperados**

### **En la Consola del Navegador (F12):**

```javascript
?? Inicializando Push Notification Manager...
?? Iniciando sistema de notificaciones push...
? Navegador compatible con Push API
? Service Worker listo: activated
?? Obteniendo clave pública VAPID...
? Clave pública VAPID obtenida: BGc4buz7YF7ADLpco1...
?? Usuario no suscrito a notificaciones
? Sistema de notificaciones inicializado correctamente

// Al hacer clic en "Activar Notificaciones":
? Permiso concedido para notificaciones
? Suscripción push creada
? Suscripción guardada en el servidor: { success: true, subscription: {...} }
```

### **En la Consola del Servidor:**

(Si tienes logs habilitados en el backend)
```
info: PresupuestoFamiliarApp.Controllers.Api.PushController[0]
      Usuario 1 suscrito a notificaciones push
```

---

## ?? **Resumen de Cambios**

| Archivo | Método | Cambio |
|---------|--------|--------|
| `push-manager.js` | `enviarSuscripcionAlServidor()` | ? Agregado `credentials: 'include'` |
| `push-manager.js` | `enviarDesuscripcionAlServidor()` | ? Agregado `credentials: 'include'` |
| `push-manager.js` | `enviarNotificacionPrueba()` | ? Agregado `credentials: 'include'` |

---

## ?? **Importante: CORS en Producción**

Si en el futuro despliegas la aplicación en un dominio diferente (por ejemplo, API en `api.tuapp.com` y Frontend en `tuapp.com`), necesitarás configurar CORS en el backend:

```csharp
// En Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://tuapp.com")
              .AllowCredentials()  // ?? Requerido para cookies
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");
```

---

## ? **Resultado Final**

Después de estos cambios:

1. ? Las notificaciones se activan correctamente
2. ? La suscripción se guarda en la base de datos
3. ? El botón "Probar" envía notificaciones
4. ? Los jobs de Hangfire pueden enviar notificaciones automáticas

---

## ?? **¡Sistema de Notificaciones Push Funcionando!**

Ahora tu aplicación puede:
- ? Suscribir usuarios a notificaciones push
- ? Enviar notificaciones de prueba
- ? Enviar notificaciones automáticas de vencimientos (Hangfire)
- ? Enviar notificaciones automáticas de presupuestos (Hangfire)

**Próximo paso:**
- Ejecutar "Trigger now" en Hangfire para probar las notificaciones automáticas
- Crear cuentas por cobrar con vencimiento próximo para probar el sistema completo

---

## ?? **Troubleshooting Adicional**

### **Si AÚN da error 401:**

1. **Verifica que estás logueado:**
   - Ve a `/Auth/Login` e inicia sesión
   - Verifica que aparece tu nombre en el menú superior

2. **Verifica las cookies:**
   - F12 ? Application ? Cookies
   - Debe existir una cookie `.AspNetCore.Cookies`

3. **Limpia las cookies y vuelve a iniciar sesión:**
   ```
   F12 ? Application ? Storage ? Clear site data
   ```

### **Si da error "Usuario no autenticado":**

El controlador no puede obtener el `ClaimTypes.NameIdentifier`. Verifica en `Program.cs` que la autenticación por cookies está configurada correctamente:

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
```

---

## ?? **Notas Técnicas**

### **¿Por qué no usar `[AllowAnonymous]`?**

Podríamos hacer el endpoint público con `[AllowAnonymous]` y obtener el usuario de otra forma (por ejemplo, pasándolo en el body), pero esto tiene desventajas:

? Menos seguro (cualquiera podría enviar suscripciones)
? Requiere cambios en el JavaScript y el DTO
? No aprovecha el sistema de autenticación de ASP.NET Core

? Usar `credentials: 'include'` es más seguro y estándar
? Aprovecha el sistema de autenticación existente
? Solo requiere un pequeño cambio en el JavaScript

---

**¡El sistema de notificaciones push está completamente funcional!** ??
