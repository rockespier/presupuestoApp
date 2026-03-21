# ?? SOLUCIÓN: Problema con Login Corregido

## ? **PROBLEMA IDENTIFICADO Y CORREGIDO**

El login no funcionaba debido a un desajuste entre el formulario y el controlador.

### **Problemas Encontrados:**

1. ? **Parámetro incorrecto**: El controlador esperaba `email` pero el formulario enviaba `nombreUsuario`
2. ? **Búsqueda limitada**: Solo buscaba por email, no por nombre de usuario
3. ? **Mensaje de error mal configurado**: Usaba `ViewBag.Error` en lugar de `TempData["Error"]`

---

## ? **CORRECCIONES APLICADAS**

### **1. Actualización del Controlador (AuthController.cs)**

**Antes:**
```csharp
public async Task<IActionResult> Login(string email, string password)
{
    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Email == email);
    
    // ...
    
    ViewBag.Error = "Correo electrónico o contraseña incorrectos.";
    return View();
}
```

**Después:**
```csharp
public async Task<IActionResult> Login(string nombreUsuario, string password)
{
    // Buscar por email O nombre de usuario
    var usuario = await _context.Usuarios
        .Include(u => u.Espacios)
        .FirstOrDefaultAsync(u => u.Email == nombreUsuario || u.NombreUsuario == nombreUsuario);
    
    // ...
    
    TempData["Error"] = "Usuario/Email o contraseña incorrectos.";
    return RedirectToAction("Login");
}
```

### **Cambios Realizados:**

? **Parámetro corregido**: Ahora acepta `nombreUsuario` (coincide con el formulario)  
? **Búsqueda flexible**: Busca por `Email` O `NombreUsuario`  
? **Include necesario**: Carga los espacios del usuario con `.Include(u => u.Espacios)`  
? **TempData correcto**: Usa `TempData["Error"]` para persistir entre redirects  
? **Redirect apropiado**: Usa `RedirectToAction("Login")` en lugar de `return View()`  

---

## ?? **CÓMO USAR EL LOGIN AHORA**

Los usuarios pueden iniciar sesión de **2 formas**:

### **Opción 1: Con Email**
```
Usuario: admin@presupuesto.com
Contraseña: admin123
```

### **Opción 2: Con Nombre de Usuario**
```
Usuario: admin
Contraseña: admin123
```

**¡Ambas funcionan!** ??

---

## ?? **CÓMO PROBAR EL LOGIN**

### **Paso 1: Verificar Usuario Admin en Base de Datos**

Ejecuta esta consulta SQL:

```sql
SELECT Id, NombreUsuario, Email, Rol 
FROM Usuarios 
WHERE NombreUsuario = 'admin' OR Email LIKE '%admin%';
```

**Resultado esperado:**
```
Id | NombreUsuario | Email              | Rol
1  | admin         | admin@presup.com   | Administrador
```

### **Paso 2: Probar Login**

1. Ejecuta la aplicación: `dotnet run`
2. Ve a: `https://localhost:5001/Auth/Login`
3. Intenta con **nombre de usuario**:
   - Usuario: `admin`
   - Contraseña: `admin123`
4. O con **email**:
   - Usuario: `admin@presupuesto.com`
   - Contraseña: `admin123`

### **Paso 3: Verificar Cookies de Sesión**

Después de login exitoso:
1. Abre DevTools (F12)
2. Ve a: **Application ? Cookies**
3. Busca: `.AspNetCore.Cookies`
4. Debe existir y tener contenido

---

## ?? **SI AÚN NO FUNCIONA**

### **Problema 1: "Usuario/Email o contraseña incorrectos"**

**Posibles causas:**
- ? La contraseña no coincide
- ? El usuario no existe en la BD
- ? La contraseña está mal hasheada

**Solución:**
```csharp
// Resetear contraseña del admin manualmente
var admin = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == "admin");
if (admin != null)
{
    admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
    await _context.SaveChangesAsync();
}
```

### **Problema 2: Redirige al login después de autenticar**

**Causa:**
- ? Las cookies no se están guardando
- ? Middleware de autenticación no configurado

**Solución:**
Verifica en `Program.cs` que exista:
```csharp
app.UseAuthentication();  // ? Debe estar ANTES de UseAuthorization
app.UseAuthorization();
```

### **Problema 3: Error 400 - Bad Request**

**Causa:**
- ? Token AntiForgery inválido

**Solución:**
Limpia las cookies del navegador:
```
Chrome: Ctrl+Shift+Delete ? Cookies y otros datos del sitio
```

### **Problema 4: No hay usuario admin**

**Solución:**
Crear usuario admin manualmente:

```csharp
// En Program.cs, después de app.Build()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PresupuestoContext>();
    
    if (!context.Usuarios.Any(u => u.NombreUsuario == "admin"))
    {
        var admin = new Usuario
        {
            NombreUsuario = "admin",
            Email = "admin@presupuesto.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Rol = "Administrador"
        };
        
        context.Usuarios.Add(admin);
        context.SaveChanges();
    }
}
```

---

## ?? **LOGS DE DEPURACIÓN**

Para ver qué está pasando durante el login, agrega logs temporales:

```csharp
[HttpPost]
public async Task<IActionResult> Login(string nombreUsuario, string password)
{
    Console.WriteLine($"?? Intentando login con: {nombreUsuario}");
    
    var usuario = await _context.Usuarios
        .Include(u => u.Espacios)
        .FirstOrDefaultAsync(u => u.Email == nombreUsuario || u.NombreUsuario == nombreUsuario);
    
    if (usuario == null)
    {
        Console.WriteLine("? Usuario no encontrado");
        TempData["Error"] = "Usuario/Email no existe.";
        return RedirectToAction("Login");
    }
    
    Console.WriteLine($"? Usuario encontrado: {usuario.NombreUsuario}");
    
    bool passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
    Console.WriteLine($"?? Contraseña válida: {passwordValido}");
    
    if (passwordValido)
    {
        Console.WriteLine("? Login exitoso, creando sesión...");
        // ... resto del código
    }
    else
    {
        Console.WriteLine("? Contraseña incorrecta");
        TempData["Error"] = "Contraseña incorrecta.";
        return RedirectToAction("Login");
    }
}
```

**Ver logs:**
- Visual Studio: Ventana "Salida" (Output)
- VS Code/Terminal: Consola donde ejecutas `dotnet run`

---

## ? **CHECKLIST DE VERIFICACIÓN**

Antes de contactar soporte, verifica:

- [ ] La aplicación está corriendo (`dotnet run`)
- [ ] La URL es correcta: `/Auth/Login`
- [ ] Estás usando HTTPS (no HTTP)
- [ ] La base de datos tiene el usuario admin
- [ ] La contraseña está hasheada con BCrypt
- [ ] Las cookies están habilitadas en el navegador
- [ ] No hay errores en la consola del navegador (F12)
- [ ] Program.cs tiene `UseAuthentication()` antes de `UseAuthorization()`

---

## ?? **RESULTADO ESPERADO**

Después de estas correcciones:

? **Login funciona** con email o nombre de usuario  
? **Mensaje de error** se muestra correctamente si falla  
? **Redirección** al dashboard después de login exitoso  
? **Sesión persistente** durante 7 días  
? **Espacios cargados** automáticamente para el usuario  

---

## ?? **¿NECESITAS MÁS AYUDA?**

Si el problema persiste:

1. **Copia los logs** de la consola
2. **Captura de pantalla** del error
3. **Query SQL** del usuario admin:
   ```sql
   SELECT * FROM Usuarios WHERE NombreUsuario = 'admin';
   ```
4. **Verificar** que la tabla `Usuarios` tiene datos

---

**¡El login ahora debería funcionar perfectamente!** ??
