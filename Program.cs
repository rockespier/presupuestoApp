using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PresupuestoFamiliarApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

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

var app = builder.Build();


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


app.Run();
