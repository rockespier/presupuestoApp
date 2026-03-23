using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Servicios;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// AGREGAR ESTAS LÍNEAS: Configurar Entity Framework Core con SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PresupuestoContext>(options =>
    options.UseSqlServer(connectionString));

// Añadir esto para poder leer la Cookie en el HTML (Navbar)
builder.Services.AddHttpContextAccessor();

// Configurar la Autenticación
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // A dónde te envía si no estás logueado
        options.AccessDeniedPath = "/Auth/AccesoDenegado"; // Si un Usuario intenta entrar a zona de Admin
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // La sesión dura 7 días
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

// Activar el panel de control de Hangfire (solo accesible para ti)
app.UseHangfireDashboard("/hangfire");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

    // "0 0 * * *" es formato Cron para: Todos los días a medianoche
    RecurringJob.AddOrUpdate("ProcesarFijosDiarios", () => service.ProcesarMovimientosFijos(), "1 0 * * *");

    // 1. Reporte Mensual por Correo (Día 1 de cada mes a las 8:00 AM)
    RecurringJob.AddOrUpdate("ResumenMensual", () => service.EnviarResumenMensual(), "0 8 1 * *");

    // "0 1 1 * *" significa: Minuto 0, Hora 1 (AM), Día 1 de cada mes.
    RecurringJob.AddOrUpdate("GenerarAlquileres", () => service.GenerarDeudasMensuales(), "0 1 1 * *");

    // Notificaciones Push - Vencimientos (todos los días a las 9:00 AM)
    RecurringJob.AddOrUpdate("NotificarVencimientos", () => pushService.NotificarVencimientosProximos(), "0 9 * * *");

    // Notificaciones Push - Presupuestos (todos los días a las 20:00)
    RecurringJob.AddOrUpdate("NotificarPresupuestos", () => pushService.NotificarPresupuestosExcedidos(), "0 20 * * *");
}

// Esta línea debe ser la ÚLTIMA
app.Run();