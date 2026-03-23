# ?? Solución: Jobs Recurrentes No Aparecen en Hangfire

## ? **Problema**

Los jobs recurrentes no aparecían en `localhost:7036/hangfire/recurring` aunque estaban configurados en `Program.cs`.

---

## ?? **Causa del Problema**

El código para registrar los jobs recurrentes estaba **DESPUÉS** de `app.Run()`:

```csharp
// ? INCORRECTO
var app = builder.Build();

app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(...);

app.Run();  // ? Esta línea BLOQUEA la ejecución

// ?? Este código NUNCA se ejecuta
using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate("ProcesarFijosDiarios", ...);
    RecurringJob.AddOrUpdate("NotificarVencimientos", ...);
}
```

**¿Por qué?** Porque `app.Run()` es una llamada **BLOQUEANTE** que inicia el servidor web y espera indefinidamente. Todo lo que esté después nunca se ejecuta.

---

## ? **Solución**

Mover el código de registro de jobs recurrentes **ANTES** de `app.Run()`:

```csharp
// ? CORRECTO
var app = builder.Build();

app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(...);

// ? Registrar jobs recurrentes ANTES de app.Run()
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<AutomatizacionService>();
    var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

    RecurringJob.AddOrUpdate("ProcesarFijosDiarios", 
        () => service.ProcesarMovimientosFijos(), 
        "1 0 * * *");

    RecurringJob.AddOrUpdate("ResumenMensual", 
        () => service.EnviarResumenMensual(), 
        "0 8 1 * *");

    RecurringJob.AddOrUpdate("GenerarAlquileres", 
        () => service.GenerarDeudasMensuales(), 
        "0 1 1 * *");

    RecurringJob.AddOrUpdate("NotificarVencimientos", 
        () => pushService.NotificarVencimientosProximos(), 
        "0 9 * * *");

    RecurringJob.AddOrUpdate("NotificarPresupuestos", 
        () => pushService.NotificarPresupuestosExcedidos(), 
        "0 20 * * *");
}

// Esta línea debe ser la ÚLTIMA
app.Run();
```

---

## ?? **Código Completo Correcto**

```csharp
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar Entity Framework Core con SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PresupuestoContext>(options =>
    options.UseSqlServer(connectionString));

// Añadir esto para poder leer la Cookie en el HTML (Navbar)
builder.Services.AddHttpContextAccessor();

// Configurar la Autenticación
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Configurar Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Registrar servicios
builder.Services.AddScoped<AutomatizacionService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PushNotificationService>();

var app = builder.Build();

// Activar el panel de control de Hangfire
app.UseHangfireDashboard("/hangfire");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ? IMPORTANTE: Registrar los jobs recurrentes ANTES de app.Run()
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<AutomatizacionService>();
    var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

    RecurringJob.AddOrUpdate("ProcesarFijosDiarios", 
        () => service.ProcesarMovimientosFijos(), 
        "1 0 * * *");

    RecurringJob.AddOrUpdate("ResumenMensual", 
        () => service.EnviarResumenMensual(), 
        "0 8 1 * *");

    RecurringJob.AddOrUpdate("GenerarAlquileres", 
        () => service.GenerarDeudasMensuales(), 
        "0 1 1 * *");

    RecurringJob.AddOrUpdate("NotificarVencimientos", 
        () => pushService.NotificarVencimientosProximos(), 
        "0 9 * * *");

    RecurringJob.AddOrUpdate("NotificarPresupuestos", 
        () => pushService.NotificarPresupuestosExcedidos(), 
        "0 20 * * *");
}

// Esta línea debe ser la ÚLTIMA
app.Run();
```

---

## ?? **Verificar la Solución**

### **1. Recompilar y ejecutar:**

```bash
dotnet build
dotnet run
```

### **2. Acceder a Hangfire:**

Ve a: `https://localhost:7036/hangfire/recurring`

### **3. Verificar que aparezcan los jobs:**

Deberías ver los siguientes 5 jobs recurrentes:

| Job ID | Método | Cron Expression | Descripción |
|--------|--------|----------------|-------------|
| `ProcesarFijosDiarios` | `ProcesarMovimientosFijos()` | `1 0 * * *` | Diario a la 00:01 AM |
| `ResumenMensual` | `EnviarResumenMensual()` | `0 8 1 * *` | Día 1 de cada mes a las 08:00 AM |
| `GenerarAlquileres` | `GenerarDeudasMensuales()` | `0 1 1 * *` | Día 1 de cada mes a las 01:00 AM |
| `NotificarVencimientos` | `NotificarVencimientosProximos()` | `0 9 * * *` | Diario a las 09:00 AM |
| `NotificarPresupuestos` | `NotificarPresupuestosExcedidos()` | `0 20 * * *` | Diario a las 20:00 (8:00 PM) |

---

## ?? **Estructura del Pipeline de ASP.NET Core**

Es importante entender el orden de ejecución:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1?? CONFIGURACIÓN DE SERVICIOS
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<...>();
builder.Services.AddHangfire(...);
builder.Services.AddScoped<...>();

// 2?? CONSTRUCCIÓN DE LA APLICACIÓN
var app = builder.Build();

// 3?? CONFIGURACIÓN DEL PIPELINE
app.UseHangfireDashboard(...);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(...);

// 4?? INICIALIZACIÓN (antes de iniciar el servidor)
using (var scope = app.Services.CreateScope())
{
    // Aquí puedes obtener servicios y ejecutar código de inicialización
    RecurringJob.AddOrUpdate(...);
}

// 5?? INICIAR EL SERVIDOR (BLOQUEANTE)
app.Run(); // ? Todo después de esto NO se ejecuta
```

---

## ?? **Mejores Prácticas**

### ? **Hacer:**

1. Registrar jobs recurrentes ANTES de `app.Run()`
2. Usar un scope para obtener servicios con DI
3. Usar nombres descriptivos para los jobs
4. Documentar las expresiones cron

### ? **No Hacer:**

1. Poner código DESPUÉS de `app.Run()`
2. Registrar jobs dentro de endpoints o controllers
3. Hardcodear credenciales en los jobs
4. Olvidar agregar los servicios al DI container

---

## ?? **Troubleshooting Adicional**

### **Problema: Jobs aparecen pero no se ejecutan**

**Solución:**
1. Verifica que `AddHangfireServer()` esté configurado
2. Verifica las expresiones cron con https://crontab.guru
3. Revisa los logs en Hangfire ? Failed Jobs

### **Problema: Error "Service not found"**

**Solución:**
1. Verifica que el servicio esté registrado en DI:
   ```csharp
   builder.Services.AddScoped<TuServicio>();
   ```
2. Verifica el namespace del servicio

### **Problema: Jobs se duplican**

**Solución:**
- `AddOrUpdate` reemplaza jobs existentes con el mismo ID
- Si usas solo `Add`, se crearán duplicados

---

## ?? **Explicación de Expresiones Cron**

| Expresión | Significado |
|-----------|-------------|
| `* * * * *` | Cada minuto |
| `0 * * * *` | Cada hora |
| `0 0 * * *` | Cada día a medianoche |
| `1 0 * * *` | Cada día a las 00:01 AM |
| `0 9 * * *` | Cada día a las 9:00 AM |
| `0 20 * * *` | Cada día a las 20:00 (8:00 PM) |
| `0 8 1 * *` | Día 1 de cada mes a las 8:00 AM |
| `0 0 * * 1` | Cada lunes a medianoche |
| `*/5 * * * *` | Cada 5 minutos |

**Formato:** `minuto hora día mes díaDeLaSemana`

**Herramienta:** https://crontab.guru

---

## ? **Resultado**

Después de aplicar la corrección:

1. ? Los 5 jobs recurrentes ahora aparecen en `/hangfire/recurring`
2. ? Se pueden ejecutar manualmente haciendo clic en "Trigger now"
3. ? Se ejecutarán automáticamente según su schedule
4. ? El histórico de ejecuciones se puede ver en "Succeeded jobs"

---

## ?? **¡Problema Resuelto!**

Ahora tu sistema de notificaciones push y automatizaciones funciona correctamente con Hangfire.

**Próximos pasos:**
1. ? Verifica que los jobs aparezcan en Hangfire
2. ? Prueba ejecutarlos manualmente con "Trigger now"
3. ? Verifica los logs de ejecución
4. ? Espera a que se ejecuten automáticamente según su schedule
