# ?? Corrección: Diferenciación de Ingresos y Egresos en Calendario

## ? **PROBLEMA CORREGIDO**

**Antes:** Todos los movimientos fijos (suscripciones/pagos recurrentes) aparecían como "Pagar..." en el calendario, sin importar si eran ingresos o egresos.

**Después:** Ahora el calendario diferencia correctamente:
- ?? **"Cobrar..."** para movimientos fijos de tipo **INGRESO**
- ?? **"Pagar..."** para movimientos fijos de tipo **EGRESO**

---

## ?? **CAMBIOS VISUALES**

### **Colores en el Calendario:**

| Tipo de Evento | Color | Código | Significado |
|----------------|-------|--------|-------------|
| **Cobrar (Ingreso Fijo)** | ?? Verde | `#198754` | Dinero que vas a recibir |
| **Pagar (Egreso Fijo)** | ?? Azul Claro | `#0dcaf0` | Dinero que vas a pagar |
| **Cobrar a Deudor (Vencido)** | ?? Rojo | `#dc3545` | Cobro vencido |
| **Cobrar a Deudor (Pendiente)** | ?? Verde | `#198754` | Cobro pendiente |

---

## ?? **CÓDIGO MODIFICADO**

### **Archivo:** `Controllers/HomeController.cs`

**Antes (líneas 214-227):**
```csharp
// --- 2. EVENTOS DE GASTOS (Suscripciones / Pagos Fijos) ---
var suscripciones = await _context.MovimientosFijos
    .Where(s => s.EspacioId == espacioActualId)
    .ToListAsync();

var eventosPagos = new List<object>();

foreach (var sub in suscripciones)
{
    int diaValido = Math.Min(sub.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
    DateTime fechaDelPago = new DateTime(anioConsulta, mesConsulta, diaValido);

    eventosPagos.Add(new
    {
        title = $"Pagar {sub.Descripcion}: {sub.Monto.ToString("C")}",  // ? Siempre "Pagar"
        start = fechaDelPago.ToString("yyyy-MM-dd"),
        color = "#0dcaf0",  // ? Siempre azul
        allDay = true,
    });
}
```

**Después (corregido):**
```csharp
// --- 2. EVENTOS DE MOVIMIENTOS FIJOS (Ingresos y Egresos Recurrentes) ---
var suscripciones = await _context.MovimientosFijos
    .Where(s => s.EspacioId == espacioActualId)
    .ToListAsync();

var eventosPagos = new List<object>();

foreach (var sub in suscripciones)
{
    int diaValido = Math.Min(sub.DiaDelMes, DateTime.DaysInMonth(anioConsulta, mesConsulta));
    DateTime fechaDelPago = new DateTime(anioConsulta, mesConsulta, diaValido);

    // ? Diferenciar entre Ingreso y Egreso
    string accion = sub.Tipo == TipoTransaccion.Ingreso ? "Cobrar" : "Pagar";
    string color = sub.Tipo == TipoTransaccion.Ingreso ? "#198754" : "#0dcaf0";

    eventosPagos.Add(new
    {
        title = $"{accion} {sub.Descripcion}: {sub.Monto.ToString("C")}",  // ? Dinámico
        start = fechaDelPago.ToString("yyyy-MM-dd"),
        color = color,  // ? Verde o Azul según el tipo
        allDay = true,
    });
}
```

---

## ?? **EJEMPLOS DE RESULTADOS**

### **Movimiento Fijo de INGRESO:**
```
Tipo: TipoTransaccion.Ingreso
Descripción: "Sueldo Mensual"
Monto: 3000.00

Evento en calendario:
?? "Cobrar Sueldo Mensual: S/ 3,000.00"
```

### **Movimiento Fijo de EGRESO:**
```
Tipo: TipoTransaccion.Egreso
Descripción: "Alquiler Oficina"
Monto: 1200.00

Evento en calendario:
?? "Pagar Alquiler Oficina: S/ 1,200.00"
```

---

## ?? **CÓMO VERIFICAR**

### **1. Crear Movimiento Fijo de Ingreso:**
1. Ve a **Suscripciones** ? **Nuevo Registro Fijo**
2. Tipo: **Ingreso**
3. Descripción: "Sueldo Mensual"
4. Monto: 3000
5. Día del mes: 1
6. Guardar

### **2. Crear Movimiento Fijo de Egreso:**
1. Ve a **Suscripciones** ? **Nuevo Registro Fijo**
2. Tipo: **Egreso**
3. Descripción: "Netflix"
4. Monto: 45
5. Día del mes: 15
6. Guardar

### **3. Ver en el Dashboard:**
1. Ve al **Dashboard**
2. Busca el **Calendario** en la parte inferior
3. Verifica que:
   - El día 1 aparezca: ?? **"Cobrar Sueldo Mensual: S/ 3,000.00"** (verde)
   - El día 15 aparezca: ?? **"Pagar Netflix: S/ 45.00"** (azul claro)

---

## ?? **IMPACTO EN EL DASHBOARD**

### **Sección de Pagos Pendientes:**
Los cálculos de pagos/cobros pendientes **ya estaban correctos** porque usaban filtros por `sub.Tipo`:

**Cobros pendientes (verde):**
```csharp
// Ya incluye correctamente MovimientosFijos de tipo INGRESO
var ingresosFijosPendientes = suscripciones
    .Where(s => s.Activo && s.Tipo == TipoTransaccion.Ingreso)
    .Select(...)
    .Sum(p => p.Monto);
```

**Pagos pendientes (rojo):**
```csharp
// Ya incluye correctamente MovimientosFijos de tipo EGRESO
var pagosFijosPendientes = suscripciones
    .Where(s => s.Activo && s.Tipo == TipoTransaccion.Egreso)
    .Select(...)
    .Sum(p => p.Monto);
```

---

## ?? **RESUMEN DE LA CORRECCIÓN**

### **Cambios realizados:**
1. ? Agregada condición para detectar `sub.Tipo`
2. ? Variable `accion` que determina "Cobrar" o "Pagar"
3. ? Variable `color` que determina verde o azul
4. ? Actualizado comentario de la sección
5. ? Título del evento ahora es dinámico

### **Archivos modificados:**
- ? `Controllers/HomeController.cs` (líneas 214-233)

### **Sin cambios en:**
- ? Base de datos (sin migraciones)
- ? Modelos
- ? Vistas
- ? JavaScript del calendario

---

## ?? **RESULTADO FINAL**

El calendario ahora muestra correctamente:

### **Eventos de Ingresos:**
- ?? **Cobrar a [Deudor]**: Cuentas por cobrar vencidas/pendientes
- ?? **Cobrar [Descripción]**: Ingresos fijos recurrentes

### **Eventos de Egresos:**
- ?? **Pagar [Descripción]**: Gastos fijos recurrentes

**Esto hace que el calendario sea mucho más intuitivo y fácil de entender de un vistazo.**

---

## ?? **NOTAS ADICIONALES**

### **Si quieres personalizar más:**

**Cambiar emojis en los títulos:**
```csharp
string accion = sub.Tipo == TipoTransaccion.Ingreso 
    ? "?? Cobrar" 
    : "?? Pagar";
```

**Agregar enlaces directos:**
```csharp
eventosPagos.Add(new
{
    title = $"{accion} {sub.Descripcion}: {sub.Monto.ToString("C")}",
    start = fechaDelPago.ToString("yyyy-MM-dd"),
    color = color,
    allDay = true,
    url = $"/MovimientosFijos/Edit/{sub.Id}"  // ? Agregar link
});
```

**Agregar descripción emergente:**
```csharp
eventosPagos.Add(new
{
    title = $"{accion} {sub.Descripcion}",
    start = fechaDelPago.ToString("yyyy-MM-dd"),
    color = color,
    allDay = true,
    extendedProps = new  // ? Datos adicionales
    {
        monto = sub.Monto.ToString("C"),
        tipo = sub.Tipo.ToString(),
        cuenta = sub.Cuenta?.Nombre
    }
});
```

---

## ? **COMPILACIÓN EXITOSA**

```
? Build succeeded
   0 Warning(s)
   0 Error(s)
```

---

**¡Corrección completada con éxito!** ??

Ahora el calendario refleja correctamente la naturaleza de cada movimiento fijo (ingreso vs egreso).
