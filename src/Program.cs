using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using PresupuestoFamiliarApp.Servicios;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar Entity Framework Core con SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PresupuestoContext>(options =>
    options.UseSqlServer(connectionString));

// ✅ Configurar Health Checks con clase personalizada
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        tags: new[] { "database", "ready" }
    );

// Añadir esto para poder leer la Cookie en el HTML (Navbar)
builder.Services.AddHttpContextAccessor();

// Configurar Rate Limiting para proteger el endpoint de Login contra fuerza bruta
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(15);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

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
builder.Services.AddScoped<AzureOcrService>(); // ✨ Nuevo: Azure OCR para mayor precisión
builder.Services.AddHttpClient(); // Para Azure Computer Vision API

var app = builder.Build();

// Endpoint de Health Check
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            }),
            duration = report.TotalDuration
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Activar el panel de control de Hangfire
app.UseHangfireDashboard("/hangfire");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Registrar los jobs recurrentes ANTES de app.Run()
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<AutomatizacionService>();
    var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

    RecurringJob.AddOrUpdate("ProcesarFijosDiarios", () => service.ProcesarMovimientosFijos(), "1 0 * * *");
    RecurringJob.AddOrUpdate("ResumenMensual", () => service.EnviarResumenMensual(), "0 8 1 * *");
    RecurringJob.AddOrUpdate("GenerarAlquileres", () => service.GenerarDeudasMensuales(), "0 1 1 * *");
    RecurringJob.AddOrUpdate("NotificarVencimientos", () => pushService.NotificarVencimientosProximos(), "0 9 * * *");
    RecurringJob.AddOrUpdate("NotificarPresupuestos", () => pushService.NotificarPresupuestosExcedidos(), "0 20 * * *");
}
//Solo debe existir un app.Run() en toda la aplicación. 30.03.2026
app.Run();
