# ?? Diagnóstico: No Llegan Notificaciones Push

## ?? **Checklist de Diagnóstico**

Cuando ejecutas "Trigger now" en Hangfire pero no recibes notificaciones, puede ser por varias razones. Vamos a verificar cada una:

---

## ? **Paso 1: Verificar que la Tabla `PushSubscriptions` Existe**

### **Verificación en Base de Datos:**

```sql
-- Verificar si la tabla existe
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'PushSubscriptions'

-- Si existe, ver si tiene datos
SELECT * FROM PushSubscriptions
```

### **Si NO existe la tabla:**

```bash
# Crear la migración
dotnet ef migrations add AddPushSubscriptions

# Aplicar la migración
dotnet ef database update
```

---

## ? **Paso 2: Verificar que Estás Suscrito a las Notificaciones**

### **Opción A: Verificar en la Base de Datos**

```sql
-- Ver todas las suscripciones
SELECT 
    Id,
    UsuarioId,
    LEFT(Endpoint, 50) as EndpointPreview,
    Activa,
    NotificarVencimientos,
    FechaCreacion
FROM PushSubscriptions
WHERE Activa = 1
```

**Esperado:** Deberías ver AL MENOS 1 registro con `Activa = 1`

**Si NO hay registros:** Necesitas suscribirte primero.

### **Opción B: Verificar en la Aplicación**

1. Ve a: `https://localhost:7036/Configuracion`
2. En la sección "Notificaciones Push", verifica el estado
3. Si dice "?? Notificaciones desactivadas", haz clic en **"Activar Notificaciones"**
4. El navegador pedirá permiso ? Haz clic en **"Permitir"**

---

## ? **Paso 3: Verificar que Hay Datos para Notificar**

El job `NotificarVencimientos` solo envía notificaciones si HAY cuentas por cobrar próximas a vencer.

### **Verificar Cuentas por Cobrar:**

```sql
-- Ver cuentas por cobrar que están próximas a vencer (próximos 3 días)
SELECT 
    cpc.Id,
    d.Nombre as Deudor,
    cpc.Concepto,
    cpc.FechaVencimiento,
    DATEDIFF(DAY, GETDATE(), cpc.FechaVencimiento) as DiasRestantes,
    cpc.SaldoPendiente,
    cpc.EstaPagado
FROM CuentasPorCobrar cpc
INNER JOIN Deudores d ON cpc.DeudorId = d.Id
WHERE cpc.EstaPagado = 0
AND cpc.FechaVencimiento >= CAST(GETDATE() AS DATE)
AND cpc.FechaVencimiento <= DATEADD(DAY, 3, GETDATE())
ORDER BY cpc.FechaVencimiento
```

**Si NO hay resultados:** No hay nada que notificar. Necesitas crear cuentas por cobrar de prueba.

### **Crear Cuenta por Cobrar de Prueba:**

1. Ve a: `https://localhost:7036/Deudores`
2. Crea un deudor nuevo (ej: "Deudor Prueba")
3. Agrega una cuenta por cobrar:
   - Concepto: "Prueba Notificación"
   - Monto: 100
   - **Fecha de Vencimiento**: HOY o MAÑANA
   - Dejar sin pagar

---

## ? **Paso 4: Verificar Logs de Hangfire**

Cuando ejecutas "Trigger now", Hangfire guarda logs de la ejecución.

### **Ver Logs en Hangfire:**

1. Ve a: `https://localhost:7036/hangfire`
2. Haz clic en **"Jobs"** ? **"Succeeded"** (o **"Failed"** si falló)
3. Busca el job `NotificarVencimientos` más reciente
4. Haz clic en él para ver los detalles

**¿Qué buscar?**
- ? **Estado:** "Succeeded" (verde)
- ? **Duración:** Debería tomar algunos segundos
- ?? Si está en **"Failed"** (rojo), revisa el error

---

## ? **Paso 5: Verificar Consola del Servidor**

El servicio `PushNotificationService` escribe logs en la consola.

### **Busca estos mensajes:**

```
? Notificación enviada a https://fcm.googleapis.com/fcm/send/...
?? No hay suscripciones activas para el usuario 1
? Error al enviar notificación: ...
```

### **Si ves: "No hay suscripciones activas"**

? El usuario NO está suscrito. Ve al Paso 2.

### **Si ves: "Error al enviar notificación"**

? Hay un problema con las claves VAPID o la suscripción expiró.

---

## ? **Paso 6: Probar con el Endpoint de Prueba**

Vamos a usar el endpoint `/api/push/test` que siempre envía una notificación de prueba.

### **Opción A: Usar Postman/Insomnia**

```
POST https://localhost:7036/api/push/test
Headers:
  Cookie: .AspNetCore.Cookies=<tu_cookie_de_sesion>
```

### **Opción B: Usar el Botón en Configuración**

1. Ve a: `https://localhost:7036/Configuracion`
2. Haz clic en el botón **"Probar"**
3. Deberías recibir una notificación inmediatamente

**Si NO recibes notificación:**
- Problema con el navegador o permisos
- Problema con las claves VAPID
- Service Worker no está registrado

---

## ? **Paso 7: Verificar Permisos del Navegador**

### **Chrome/Edge:**

1. Abre DevTools (F12)
2. Ve a **Application** ? **Service Workers**
3. Verifica que `service-worker.js` esté registrado y activo
4. Ve a **Application** ? **Notifications**
5. Verifica que el permiso sea **"Granted"**

### **Ver Permisos en Chrome:**

```
chrome://settings/content/notifications
```

Busca `localhost:7036` y verifica que esté en **"Permitidos"**.

---

## ? **Paso 8: Verificar Claves VAPID**

### **Ver la Consola al Iniciar la Aplicación:**

Cuando inicias la app con `dotnet run`, deberías ver:

```
?? VAPID Keys generadas automáticamente:
Public Key: BEl62iUYg...
Private Key: p6N0N9K9H...
?? Guarda estas claves en appsettings.json
```

**Si las claves cambian cada vez que inicias**, las suscripciones antiguas no funcionarán.

### **Solución: Guardar las Claves en appsettings.json**

```json
{
  "Vapid": {
    "Subject": "mailto:admin@presupuesto.com",
    "PublicKey": "TU_PUBLIC_KEY_AQUI",
    "PrivateKey": "TU_PRIVATE_KEY_AQUI"
  }
}
```

---

## ?? **Script de Prueba Completo**

Ejecuta estos pasos EN ORDEN:

### **1. Verificar Tabla**
```sql
SELECT COUNT(*) FROM PushSubscriptions WHERE Activa = 1
```
**Esperado:** >= 1

### **2. Verificar Datos para Notificar**
```sql
SELECT COUNT(*) FROM CuentasPorCobrar 
WHERE EstaPagado = 0 
AND FechaVencimiento BETWEEN GETDATE() AND DATEADD(DAY, 3, GETDATE())
```
**Esperado:** >= 1

### **3. Verificar Usuario con Suscripción Y Deudas**
```sql
SELECT 
    u.Id as UsuarioId,
    u.NombreUsuario,
    COUNT(DISTINCT ps.Id) as Suscripciones,
    COUNT(DISTINCT cpc.Id) as DeudasProximas
FROM Usuarios u
LEFT JOIN PushSubscriptions ps ON ps.UsuarioId = u.Id AND ps.Activa = 1
LEFT JOIN Espacios e ON e.Usuarios LIKE '%' + CAST(u.Id AS VARCHAR) + '%'
LEFT JOIN Deudores d ON d.EspacioId = e.Id
LEFT JOIN CuentasPorCobrar cpc ON cpc.DeudorId = d.Id 
    AND cpc.EstaPagado = 0 
    AND cpc.FechaVencimiento BETWEEN GETDATE() AND DATEADD(DAY, 3, GETDATE())
GROUP BY u.Id, u.NombreUsuario
```

**Esperado:** Un usuario con Suscripciones >= 1 Y DeudasProximas >= 1

---

## ?? **Soluciones Rápidas**

### **Problema 1: Tabla no existe**
```bash
dotnet ef migrations add AddPushSubscriptions
dotnet ef database update
```

### **Problema 2: No estás suscrito**
1. Ve a `/Configuracion`
2. Clic en "Activar Notificaciones"
3. Permitir en el navegador

### **Problema 3: No hay datos para notificar**
1. Ve a `/Deudores`
2. Crea deudor de prueba
3. Agrega cuenta por cobrar con vencimiento en 1-2 días

### **Problema 4: Claves VAPID cambian**
Agrega las claves a `appsettings.json`:
```json
{
  "Vapid": {
    "Subject": "mailto:admin@presupuesto.com",
    "PublicKey": "COPIA_LA_PUBLIC_KEY_DE_LA_CONSOLA",
    "PrivateKey": "COPIA_LA_PRIVATE_KEY_DE_LA_CONSOLA"
  }
}
```

### **Problema 5: Permisos denegados**
1. Chrome ? `chrome://settings/content/notifications`
2. Busca `localhost:7036`
3. Cambia a "Permitir"
4. Recarga la página
5. Vuelve a suscribirte en `/Configuracion`

---

## ?? **Dashboard de Diagnóstico**

Ejecuta este query para ver un resumen completo:

```sql
-- Dashboard de Diagnóstico
SELECT 
    'Usuarios Totales' as Metrica,
    COUNT(*) as Valor
FROM Usuarios

UNION ALL

SELECT 
    'Suscripciones Activas',
    COUNT(*)
FROM PushSubscriptions
WHERE Activa = 1

UNION ALL

SELECT 
    'Deudas Próximas a Vencer (3 días)',
    COUNT(*)
FROM CuentasPorCobrar
WHERE EstaPagado = 0
AND FechaVencimiento BETWEEN GETDATE() AND DATEADD(DAY, 3, GETDATE())

UNION ALL

SELECT 
    'Deudas que Vencen HOY',
    COUNT(*)
FROM CuentasPorCobrar
WHERE EstaPagado = 0
AND CAST(FechaVencimiento AS DATE) = CAST(GETDATE() AS DATE)
```

---

## ? **Checklist Final**

Antes de ejecutar "Trigger now" nuevamente, verifica:

- [ ] La tabla `PushSubscriptions` existe
- [ ] Tienes AL MENOS 1 suscripción con `Activa = 1`
- [ ] Tienes AL MENOS 1 cuenta por cobrar con vencimiento próximo (0-3 días)
- [ ] Las claves VAPID están guardadas en `appsettings.json`
- [ ] El navegador tiene permisos de notificación
- [ ] El Service Worker está registrado y activo
- [ ] Has probado el endpoint `/api/push/test` y funciona

---

## ?? **Próximo Paso**

Si TODOS los checks anteriores pasan pero TODAVÍA no recibes notificaciones:

1. Abre la consola del navegador (F12 ? Console)
2. Ejecuta "Trigger now" en Hangfire
3. Busca errores en la consola
4. Comparte el error para ayuda específica

---

## ?? **Tip: Prueba Fácil**

Para probar rápidamente SIN esperar vencimientos reales:

```csharp
// Modifica temporalmente el método NotificarVencimientosProximos
// en PushNotificationService.cs

public async Task NotificarVencimientosProximos()
{
    // PRUEBA: Enviar notificación a TODOS los usuarios suscritos
    var suscripciones = await _context.PushSubscriptions
        .Include(s => s.Usuario)
        .Where(s => s.Activa)
        .ToListAsync();

    foreach (var suscripcion in suscripciones)
    {
        await EnviarNotificacion(
            suscripcion.UsuarioId,
            "?? Prueba de Notificación",
            "Esta es una notificación de prueba desde Hangfire",
            "/",
            "/icons/icon-192x192.png"
        );
    }
}
```

Ejecuta "Trigger now" ? Deberías recibir la notificación inmediatamente.

Si esto funciona, el problema es que no tienes cuentas por cobrar próximas a vencer.
