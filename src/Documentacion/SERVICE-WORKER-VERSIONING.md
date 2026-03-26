# ?? Guía de Versiones del Service Worker

## ?? **¿Por Qué Es Importante Actualizar la Versión?**

El Service Worker cachea archivos estáticos (CSS, JS, imágenes) para que la PWA funcione offline. Si NO actualizas la versión:

? Los usuarios seguirán viendo archivos antiguos en caché
? Tus cambios no se reflejarán en sus dispositivos  
? Pueden experimentar bugs o comportamientos inesperados

? **Actualizar la versión** fuerza al navegador a descargar los archivos nuevos

---

## ?? **¿Cuándo Debo Actualizar la Versión?**

### **? SIEMPRE actualizar si modificas:**

| Tipo de Cambio | Ejemplo | ¿Actualizar? |
|----------------|---------|-------------|
| **CSS** | Cambios en estilos, colores, layouts | ? SÍ |
| **JavaScript** | Nuevo código JS, funciones | ? SÍ |
| **HTML** | Cambios en las vistas Razor Pages | ? SÍ |
| **Imágenes/Iconos** | Nuevos logos, iconos | ? SÍ |
| **manifest.json** | Cambios en la configuración PWA | ? SÍ |

### **?? Opcional (pero recomendado):**

| Tipo de Cambio | Ejemplo | ¿Actualizar? |
|----------------|---------|-------------|
| **Código C#** | Cambios en controladores, models | ?? Recomendado |
| **Base de Datos** | Nuevas migraciones | ?? Recomendado |
| **Configuración** | appsettings.json | ?? Recomendado |

### **? NO es necesario si solo:**

| Tipo de Cambio | Ejemplo | ¿Actualizar? |
|----------------|---------|-------------|
| **Documentación** | README.md, guías | ? NO |
| **Scripts de deployment** | .ps1, .sh | ? NO |
| **Archivos de configuración** | .gitignore, editorconfig | ? NO |

---

## ?? **Cómo Actualizar la Versión**

### **Paso 1: Abrir el Service Worker**

```bash
# Ubicación del archivo
wwwroot/service-worker.js
```

### **Paso 2: Incrementar la Versión**

```javascript
// ANTES (versión antigua)
const CACHE_NAME = 'presupuesto-app-v2';
const RUNTIME_CACHE = 'presupuesto-runtime-v2';

// DESPUÉS (nueva versión)
const CACHE_NAME = 'presupuesto-app-v3';  // ? Incrementar número
const RUNTIME_CACHE = 'presupuesto-runtime-v3';  // ? Incrementar número
```

### **Paso 3: Commit y Push**

```bash
git add wwwroot/service-worker.js
git commit -m "Update Service Worker to v3"
git push origin main
```

---

## ?? **Validación Automática con GitHub Actions**

El workflow de GitHub Actions ahora **verifica automáticamente** si actualizaste la versión:

### **? Si actualizaste la versión:**

```
============================================
   VERIFICACIÓN DE VERSIÓN SERVICE WORKER
============================================

?? CACHE_NAME actual: presupuesto-app-v3
?? RUNTIME_CACHE actual: presupuesto-runtime-v3

?? CACHE_NAME anterior: presupuesto-app-v2
?? RUNTIME_CACHE anterior: presupuesto-runtime-v2

? VERSIÓN ACTUALIZADA CORRECTAMENTE

   ? CACHE_NAME cambió: presupuesto-app-v2 ? presupuesto-app-v3
   ? RUNTIME_CACHE cambió: presupuesto-runtime-v2 ? presupuesto-runtime-v3

?? El Service Worker será actualizado en los clientes

============================================
```

### **?? Si NO actualizaste la versión:**

```
?? ADVERTENCIA: Las versiones del Service Worker NO han cambiado

Si has modificado archivos estáticos (CSS, JS, imágenes),
debes actualizar la versión en service-worker.js:

  const CACHE_NAME = 'presupuesto-app-v3';         // Incrementar versión
  const RUNTIME_CACHE = 'presupuesto-runtime-v3';  // Incrementar versión

Esto asegura que los usuarios reciban las últimas actualizaciones.

? Continuando con el deployment...
```

**Nota:** El deployment **NO se detiene**, pero te avisa para que lo corrijas en el próximo commit.

---

## ?? **Esquema de Versiones Recomendado**

### **Opción 1: Números Incrementales (Simple)**

```javascript
presupuesto-app-v1
presupuesto-app-v2
presupuesto-app-v3
presupuesto-app-v4
// ... y así sucesivamente
```

? **Ventajas:** Simple, fácil de entender  
? **Desventajas:** No indica qué cambió

---

### **Opción 2: Semantic Versioning (Avanzado)**

```javascript
presupuesto-app-v1.0.0  // Major.Minor.Patch
presupuesto-app-v1.1.0  // Nuevas funcionalidades
presupuesto-app-v1.1.1  // Bug fixes
presupuesto-app-v2.0.0  // Cambios importantes (breaking changes)
```

**Reglas:**
- **MAJOR (1.x.x)**: Cambios grandes incompatibles
- **MINOR (x.1.x)**: Nuevas funcionalidades (compatible)
- **PATCH (x.x.1)**: Corrección de bugs

? **Ventajas:** Indica el tipo de cambio  
? **Desventajas:** Más complejo de mantener

---

### **Opción 3: Por Fecha (Híbrido)**

```javascript
presupuesto-app-2026-01-15
presupuesto-app-2026-01-20
presupuesto-app-2026-02-01
```

? **Ventajas:** Sabes cuándo se desplegó  
? **Desventajas:** Nombres largos

---

## ?? **Flujo de Trabajo Recomendado**

### **Cada vez que hagas cambios:**

```bash
# 1. Hacer cambios en código
code Pages/Dashboard.cshtml

# 2. Probar localmente
dotnet run

# 3. Actualizar Service Worker
code wwwroot/service-worker.js
# Cambiar v2 ? v3

# 4. Commit TODO junto
git add .
git commit -m "feat: Mejoras en el dashboard (SW v3)"

# 5. Push
git push origin main

# 6. GitHub Actions automáticamente:
#    ? Valida la versión del SW
#    ? Compila y despliega
#    ? Los usuarios reciben la actualización
```

---

## ?? **Cómo Verificar Qué Versión Está Activa**

### **Opción 1: En el Navegador (Consola)**

```javascript
// Abrir DevTools (F12) ? Console
navigator.serviceWorker.getRegistration().then(reg => {
  if (reg && reg.active) {
    console.log('Service Worker activo:', reg.active.scriptURL);
  }
});

// O revisar el cache
caches.keys().then(keys => console.log('Caches:', keys));
```

**Output esperado:**
```
Caches: ['presupuesto-app-v3', 'presupuesto-runtime-v3']
```

---

### **Opción 2: En DevTools (Application)**

1. **F12** ? **Application**
2. **Service Workers** ? Ver estado
3. **Cache Storage** ? Ver versiones

---

### **Opción 3: En el Código Fuente**

```javascript
// Ver directamente en el archivo desplegado
http://presupuesto.gestionaminegocio.com/service-worker.js

// Buscar las líneas:
const CACHE_NAME = 'presupuesto-app-v?';
const RUNTIME_CACHE = 'presupuesto-runtime-v?';
```

---

## ?? **Troubleshooting**

### **? Problema: Los usuarios no ven los cambios**

**Causa:** No actualizaste la versión del Service Worker

**Solución:**
```javascript
// 1. Incrementar versión
const CACHE_NAME = 'presupuesto-app-v4';  // v3 ? v4
const RUNTIME_CACHE = 'presupuesto-runtime-v4';

// 2. Commit y push
git add wwwroot/service-worker.js
git commit -m "fix: Update SW version to force cache refresh"
git push origin main
```

---

### **? Problema: Quiero forzar actualización en todos los clientes**

**Solución 1: Incrementar versión (Recomendado)**
```javascript
const CACHE_NAME = 'presupuesto-app-v5';
```

**Solución 2: Pedir a los usuarios que limpien caché**
1. **Chrome:** Ctrl+Shift+Del ? Borrar datos de navegación
2. **PWA instalada:** Desinstalar y reinstalar la app

---

### **? Problema: Olvidé actualizar la versión**

**No hay problema:** El workflow te avisará, pero el deployment continúa.

**Para corregir:**
```bash
# 1. Hacer un commit de corrección
git add wwwroot/service-worker.js
git commit -m "chore: Update SW version (missed in previous commit)"
git push origin main
```

---

## ?? **Resumen Rápido**

| Acción | Comando |
|--------|---------|
| **Ver versión actual** | Revisar `wwwroot/service-worker.js` líneas 4-5 |
| **Incrementar versión** | Cambiar `v2` ? `v3` en ambas constantes |
| **Verificar en navegador** | F12 ? Application ? Cache Storage |
| **Forzar actualización** | Incrementar versión y hacer push |
| **Ver validación** | GitHub ? Actions ? Ver logs del workflow |

---

## ? **Checklist de Pre-Deployment**

Antes de hacer push, verifica:

- [ ] ? ¿Modifiqué archivos CSS?
- [ ] ? ¿Modifiqué archivos JS?
- [ ] ? ¿Modifiqué HTML/Razor Pages?
- [ ] ? ¿Modifiqué imágenes o iconos?
- [ ] ? Si respondiste SÍ a alguno: **Incrementar versión SW**

---

## ?? **Buenas Prácticas**

### ? **DO (Hacer):**

1. **Actualizar versión con cada cambio visual o de funcionalidad**
2. **Usar números incrementales simples (v1, v2, v3...)**
3. **Incluir la versión en el mensaje de commit**
   ```bash
   git commit -m "feat: Nueva función de reportes (SW v5)"
   ```
4. **Probar localmente antes de hacer push**
5. **Revisar los logs de GitHub Actions**

### ? **DON'T (No hacer):**

1. **No saltarse números** (v2 ? v5 ?)
2. **No usar la misma versión para cambios diferentes**
3. **No olvidar actualizar AMBAS constantes** (CACHE_NAME y RUNTIME_CACHE)
4. **No usar caracteres especiales** (v2.1-beta ?, mejor: v3)

---

## ?? **Resultado Final**

Con este sistema:

? **Validación automática** en cada deployment  
? **Advertencias claras** si olvidas actualizar  
? **Usuarios siempre con la última versión**  
? **PWA funcional** con cache actualizado  
? **Logs detallados** de qué cambió  

---

## ?? **Comandos de Referencia Rápida**

```bash
# Ver versión actual del SW
grep "CACHE_NAME\|RUNTIME_CACHE" wwwroot/service-worker.js

# Ver cambios en el SW desde el último commit
git diff HEAD~1 wwwroot/service-worker.js

# Ver historial de versiones del SW
git log --oneline --all --graph -- wwwroot/service-worker.js

# Buscar todos los commits que modificaron el SW
git log --follow --all -- wwwroot/service-worker.js
```

---

**?? ¡Ahora tienes control total sobre las versiones del Service Worker!**

Cada vez que hagas cambios importantes, simplemente incrementa el número y GitHub Actions se encargará de validarlo y notificarte.
