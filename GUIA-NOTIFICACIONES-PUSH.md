# ?? Sistema de Notificaciones Push - Guía de Configuración

## ?? **Resumen**

Se ha implementado un sistema completo de **Notificaciones Push** que permite enviar alertas a los usuarios sobre:
- ?? Vencimientos próximos de cuentas por cobrar
- ?? Presupuestos excedidos
- ?? Movimientos automáticos registrados

---

## ?? **Paso 1: Generar Claves VAPID**

Las claves VAPID son necesarias para autenticar las notificaciones push.

### **Opción A: Generar con web-push (Node.js)**

```bash
# Instalar web-push globalmente
npm install -g web-push

# Generar las claves
web-push generate-vapid-keys
```

Esto generará algo como:

```
=======================================

Public Key:
BEl62iUYgUivxIkv69yViEuiBIa-Ib27SDbQjfTbSFUIR9c5fI8kQ5dB4K3R-aP7p_3Ncxj-eT5I_fxJoD9QDvM

Private Key:
p6N0N9K9HLEf5xfXJ9HLEf5xfXJ9HLEf5xfXJ9HL

=======================================
```

### **Opción B: Generar Automáticamente (El servidor lo hará por ti)**

Si no agregas las claves al `appsettings.json`, el servidor las generará automáticamente al iniciar y las mostrará en la consola.

?? **IMPORTANTE**: Guarda estas claves en un lugar seguro. Si las cambias, todos los usuarios suscritos deberán suscribirse nuevamente.

---

## ?? **Paso 2: Agregar Claves a appsettings.json**

Abre `appsettings.json` y agrega esta sección:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Vapid": {
    "Subject": "mailto:admin@presupuesto.com",
    "PublicKey": "TU_PUBLIC_KEY_AQUI",
    "PrivateKey": "TU_PRIVATE_KEY_AQUI"
  },
  "Logging": {
    ...
  }
}
```

Ejemplo completo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=PresupuestoDb;..."
  },
  "Vapid": {
    "Subject": "mailto:admin@presupuesto.com",
    "PublicKey": "BEl62iUYgUivxIkv69yViEuiBIa-Ib27SDbQjfTbSFUIR9c5fI8kQ5dB4K3R-aP7p_3Ncxj-eT5I_fxJoD9QDvM",
    "PrivateKey": "p6N0N9K9HLEf5xfXJ9HLEf5xfXJ9HLEf5xfXJ9HL"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## ??? **Paso 3: Crear Migración de Base de Datos**

El nuevo modelo `PushSubscription` requiere una nueva tabla en la base de datos.

```bash
# Crear la migración
dotnet ef migrations add AddPushSubscriptions

# Aplicar la migración
dotnet ef database update
```

Esto creará la tabla `PushSubscriptions` con las siguientes columnas:
- `Id` (int, PK)
- `UsuarioId` (int, FK)
- `Endpoint` (string)
- `P256dh` (string)
- `Auth` (string)
- `FechaCreacion` (DateTime)
- `Activa` (bool)
- `NotificarVencimientos` (bool)
- `NotificarPresupuestos` (bool)
- `NotificarMovimientos` (bool)
- `DiasAnticipacion` (int)

---

## ? **Paso 4: Verificar la Instalación**

### **1. Compilar el Proyecto**

```bash
dotnet build
```

Debe compilar sin errores.

### **2. Ejecutar la Aplicación**

```bash
dotnet run
```

Verifica en la consola que aparezcan las claves VAPID (si no las configuraste manualmente):

```
?? VAPID Keys generadas automáticamente:
Public Key: BEl62iUYg...
Private Key: p6N0N9K9H...
?? Guarda estas claves en appsettings.json
```

### **3. Verificar Tareas de Hangfire**

Ve a `https://localhost:7XXX/hangfire` y verifica que existan las siguientes tareas:

- ? `ProcesarFijosDiarios` - Cada día a la 01:00 AM
- ? `ResumenMensual` - Día 1 de cada mes a las 08:00 AM
- ? `GenerarAlquileres` - Día 1 de cada mes a las 01:00 AM
- ? **`NotificarVencimientos`** - Cada día a las 09:00 AM ? NUEVO
- ? **`NotificarPresupuestos`** - Cada día a las 20:00 ? NUEVO

---

## ?? **Paso 5: Probar las Notificaciones**

### **1. Activar Notificaciones**

1. Inicia sesión en la aplicación
2. Ve a **Configuración** (menú de perfil ? ?? Configuración)
3. En la sección "Notificaciones Push", haz clic en **"Activar Notificaciones"**
4. El navegador pedirá permiso ? Haz clic en **"Permitir"**
5. Verás el mensaje: "¡Notificaciones activadas correctamente!"

### **2. Enviar Notificación de Prueba**

En la misma página de Configuración, haz clic en **"Probar"**

Deberías recibir una notificación que dice:
```
?? Notificación de Prueba
Las notificaciones push están funcionando correctamente!
```

### **3. Probar con Vencimientos Reales**

Para probar las notificaciones de vencimientos:

1. Ve a **Deudores**
2. Crea un deudor de prueba
3. Agrega una cuenta por cobrar con fecha de vencimiento en 2-3 días
4. Espera hasta las 9:00 AM del día siguiente (o ejecuta manualmente en Hangfire)
5. Deberías recibir una notificación automática

---

## ?? **Paso 6: Probar Manualmente en Hangfire**

No quieres esperar hasta mañana? Ejecuta las tareas manualmente:

1. Ve a `https://localhost:7XXX/hangfire`
2. En la sección **"Recurring jobs"**, busca **"NotificarVencimientos"**
3. Haz clic en **"Trigger now"**
4. Ve a la pestaña **"Succeeded jobs"** para ver el resultado

---

## ?? **Funcionalidades Implementadas**

### **1. API Endpoints**

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/push/public-key` | GET | Obtiene la clave pública VAPID |
| `/api/push/subscribe` | POST | Suscribe al usuario |
| `/api/push/unsubscribe` | POST | Desuscribe al usuario |
| `/api/push/test` | POST | Envía notificación de prueba |

### **2. Servicios Backend**

- ? `PushNotificationService` - Maneja suscripciones y envío
- ? `NotificarVencimientosProximos()` - Notifica vencimientos
- ? `NotificarPresupuestosExcedidos()` - Notifica presupuestos
- ? `EnviarNotificacion()` - Envía notificación personalizada

### **3. Frontend**

- ? `push-manager.js` - Maneja suscripciones del navegador
- ? Sección en `/Configuracion` para gestionar notificaciones
- ? Service Worker actualizado con manejo de push

### **4. Tareas Programadas (Hangfire)**

- ? Vencimientos: Diario a las 9:00 AM
- ? Presupuestos: Diario a las 20:00 (8:00 PM)

---

## ?? **Configuración Avanzada**

### **Cambiar Horarios de Notificaciones**

Edita `Program.cs`:

```csharp
// Notificar a las 8:00 AM en lugar de 9:00 AM
RecurringJob.AddOrUpdate("NotificarVencimientos", 
    () => pushService.NotificarVencimientosProximos(), 
    "0 8 * * *");  // ? Cambiar aquí

// Notificar a las 18:00 en lugar de 20:00
RecurringJob.AddOrUpdate("NotificarPresupuestos", 
    () => pushService.NotificarPresupuestosExcedidos(), 
    "0 18 * * *");  // ? Cambiar aquí
```

### **Cambiar Días de Anticipación**

Por defecto, notifica 3 días antes. Para cambiar:

1. Ve a `/Configuracion`
2. La propiedad `DiasAnticipacion` se puede extender a la UI
3. O modifica directamente en la base de datos la tabla `PushSubscriptions`

---

## ?? **Compatibilidad de Navegadores**

| Navegador | Desktop | Android | iOS |
|-----------|---------|---------|-----|
| **Chrome** | ? Sí | ? Sí | ? No |
| **Edge** | ? Sí | ? Sí | ? No |
| **Firefox** | ? Sí | ? Sí | ? No |
| **Safari** | ?? Limitado | ? No | ?? iOS 16.4+ |
| **Opera** | ? Sí | ? Sí | ? No |

**Nota sobre iOS**: Safari en iOS 16.4+ soporta notificaciones push solo si la PWA está instalada en la pantalla de inicio.

---

## ?? **Estructura de Base de Datos**

```sql
CREATE TABLE PushSubscriptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UsuarioId INT NOT NULL,
    Endpoint NVARCHAR(MAX) NOT NULL,
    P256dh NVARCHAR(500),
    Auth NVARCHAR(500),
    FechaCreacion DATETIME2 NOT NULL,
    Activa BIT NOT NULL DEFAULT 1,
    NotificarVencimientos BIT NOT NULL DEFAULT 1,
    NotificarPresupuestos BIT NOT NULL DEFAULT 1,
    NotificarMovimientos BIT NOT NULL DEFAULT 1,
    DiasAnticipacion INT NOT NULL DEFAULT 3,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
```

---

## ?? **Troubleshooting**

### **Problema: "No hay suscripciones activas"**

**Solución**:
1. Verifica que el usuario haya activado las notificaciones en `/Configuracion`
2. Verifica en la base de datos que existe un registro en `PushSubscriptions` con `Activa = 1`

### **Problema: Notificación no llega**

**Solución**:
1. Verifica que el navegador tenga permisos de notificación activados
2. Verifica los logs del servidor en la consola
3. Ejecuta la tarea manualmente en Hangfire para ver errores
4. Verifica que las claves VAPID estén correctas

### **Problema: "Error 410 Gone"**

**Solución**:
- La suscripción expiró o fue revocada
- El sistema la marca automáticamente como inactiva
- El usuario debe volver a suscribirse

### **Problema: Service Worker no se registra**

**Solución**:
1. Verifica que estés usando HTTPS (requerido para PWA)
2. Limpia la caché del navegador
3. Desregistra el service worker anterior en DevTools ? Application ? Service Workers

---

## ?? **Próximas Mejoras**

Ideas para extender la funcionalidad:

1. **Personalización por usuario**:
   - Permitir elegir qué tipos de notificaciones recibir
   - Configurar horarios preferidos
   - Ajustar días de anticipación

2. **Más tipos de notificaciones**:
   - Movimientos fijos procesados
   - Transferencias completadas
   - Nuevos deudores agregados

3. **Notificaciones con acciones**:
   - "Marcar como pagado" directamente desde la notificación
   - "Ver detalles" con deep linking

4. **Historial de notificaciones**:
   - Ver notificaciones pasadas
   - Reenviar notificaciones

---

## ? **Checklist de Implementación**

- [x] Instalar paquete `WebPush`
- [x] Crear modelo `PushSubscription`
- [x] Crear servicio `PushNotificationService`
- [x] Crear controlador API `PushController`
- [x] Crear `push-manager.js`
- [x] Actualizar Service Worker
- [x] Agregar sección en `/Configuracion`
- [x] Registrar servicio en `Program.cs`
- [x] Crear tareas de Hangfire
- [ ] Generar claves VAPID
- [ ] Agregar claves a `appsettings.json`
- [ ] Crear migración de base de datos
- [ ] Aplicar migración
- [ ] Probar suscripción
- [ ] Probar notificación de prueba
- [ ] Probar con datos reales

---

## ?? **¡Sistema de Notificaciones Push Completado!**

Con esta implementación, tu aplicación ahora puede:
- ? Enviar notificaciones push a los usuarios
- ? Notificar sobre vencimientos próximos
- ? Alertar sobre presupuestos excedidos
- ? Gestionar suscripciones automáticamente
- ? Funcionar como una PWA completa

**¡Tu app ahora tiene el mismo nivel de notificaciones que aplicaciones profesionales como Twitter, Todoist o Google Calendar!** ??
