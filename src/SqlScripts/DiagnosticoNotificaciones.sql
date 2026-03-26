-- ???????????????????????????????????????????????????????????????
-- ?? SCRIPT DE DIAGNÓSTICO: NOTIFICACIONES PUSH
-- ???????????????????????????????????????????????????????????????

PRINT '========================================='
PRINT '?? DIAGNÓSTICO DE NOTIFICACIONES PUSH'
PRINT '========================================='
PRINT ''

-- ??????????????????????????????????????????????????????????????
-- 1. VERIFICAR SI EXISTE LA TABLA PushSubscriptions
-- ??????????????????????????????????????????????????????????????
PRINT '1?? Verificando tabla PushSubscriptions...'
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PushSubscriptions')
BEGIN
    PRINT '   ? La tabla PushSubscriptions EXISTE'
    
    -- Ver estadísticas
    SELECT 
        COUNT(*) as TotalSuscripciones,
        SUM(CASE WHEN Activa = 1 THEN 1 ELSE 0 END) as SuscripcionesActivas,
        SUM(CASE WHEN Activa = 0 THEN 1 ELSE 0 END) as SuscripcionesInactivas
    FROM PushSubscriptions
    
    PRINT ''
    PRINT '   ?? Suscripciones por Usuario:'
    SELECT 
        u.Id,
        u.NombreUsuario,
        COUNT(ps.Id) as Suscripciones,
        SUM(CASE WHEN ps.Activa = 1 THEN 1 ELSE 0 END) as Activas
    FROM Usuarios u
    LEFT JOIN PushSubscriptions ps ON ps.UsuarioId = u.Id
    GROUP BY u.Id, u.NombreUsuario
    ORDER BY Activas DESC
END
ELSE
BEGIN
    PRINT '   ? La tabla PushSubscriptions NO EXISTE'
    PRINT '   ??  ACCIÓN REQUERIDA: Ejecuta las migraciones'
    PRINT '      dotnet ef migrations add AddPushSubscriptions'
    PRINT '      dotnet ef database update'
END
PRINT ''

-- ??????????????????????????????????????????????????????????????
-- 2. VERIFICAR DATOS PARA NOTIFICAR (Cuentas por Cobrar)
-- ??????????????????????????????????????????????????????????????
PRINT '2?? Verificando Cuentas por Cobrar próximas a vencer...'
PRINT ''

DECLARE @hoy DATE = CAST(GETDATE() AS DATE)
DECLARE @limite DATE = DATEADD(DAY, 3, @hoy)

PRINT '   ?? Fecha actual: ' + CAST(@hoy AS VARCHAR(20))
PRINT '   ?? Límite de búsqueda (3 días): ' + CAST(@limite AS VARCHAR(20))
PRINT ''

-- Cuentas que vencen HOY
PRINT '   ?? Cuentas que vencen HOY:'
SELECT 
    d.Nombre as Deudor,
    cpc.Concepto,
    cpc.MontoTotal,
    cpc.SaldoPendiente,
    cpc.FechaVencimiento,
    e.Nombre as Espacio
FROM CuentasPorCobrar cpc
INNER JOIN Deudores d ON cpc.DeudorId = d.Id
INNER JOIN Espacios e ON d.EspacioId = e.Id
WHERE cpc.EstaPagado = 0
AND CAST(cpc.FechaVencimiento AS DATE) = @hoy
ORDER BY cpc.FechaVencimiento

PRINT ''
PRINT '   ?? Cuentas que vencen en 1-3 días:'
SELECT 
    d.Nombre as Deudor,
    cpc.Concepto,
    cpc.MontoTotal,
    cpc.SaldoPendiente,
    cpc.FechaVencimiento,
    DATEDIFF(DAY, @hoy, cpc.FechaVencimiento) as DiasRestantes,
    e.Nombre as Espacio
FROM CuentasPorCobrar cpc
INNER JOIN Deudores d ON cpc.DeudorId = d.Id
INNER JOIN Espacios e ON d.EspacioId = e.Id
WHERE cpc.EstaPagado = 0
AND cpc.FechaVencimiento > @hoy
AND cpc.FechaVencimiento <= @limite
ORDER BY cpc.FechaVencimiento

PRINT ''
PRINT '   ?? Resumen:'
SELECT 
    'Vencen HOY' as Categoria,
    COUNT(*) as Cantidad
FROM CuentasPorCobrar cpc
WHERE cpc.EstaPagado = 0
AND CAST(cpc.FechaVencimiento AS DATE) = @hoy

UNION ALL

SELECT 
    'Vencen en 1-3 días',
    COUNT(*)
FROM CuentasPorCobrar cpc
WHERE cpc.EstaPagado = 0
AND cpc.FechaVencimiento > @hoy
AND cpc.FechaVencimiento <= @limite

UNION ALL

SELECT 
    'Total Pendientes',
    COUNT(*)
FROM CuentasPorCobrar cpc
WHERE cpc.EstaPagado = 0

PRINT ''

-- ??????????????????????????????????????????????????????????????
-- 3. VERIFICAR USUARIOS CON SUSCRIPCIÓN Y DEUDAS
-- ??????????????????????????????????????????????????????????????
PRINT '3?? Verificando usuarios que deberían recibir notificaciones...'
PRINT ''

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PushSubscriptions')
BEGIN
    SELECT 
        u.Id as UsuarioId,
        u.NombreUsuario,
        u.Email,
        COUNT(DISTINCT ps.Id) as SuscripcionesActivas,
        COUNT(DISTINCT CASE 
            WHEN cpc.EstaPagado = 0 
            AND cpc.FechaVencimiento BETWEEN @hoy AND @limite 
            THEN cpc.Id 
        END) as DeudasProximas
    FROM Usuarios u
    LEFT JOIN PushSubscriptions ps ON ps.UsuarioId = u.Id AND ps.Activa = 1 AND ps.NotificarVencimientos = 1
    LEFT JOIN Espacios e ON e.Id IN (
        SELECT Id FROM Espacios WHERE Id IN (
            SELECT EspacioId FROM Deudores WHERE Id IN (
                SELECT DISTINCT DeudorId FROM CuentasPorCobrar
            )
        )
    )
    LEFT JOIN Deudores d ON d.EspacioId = e.Id
    LEFT JOIN CuentasPorCobrar cpc ON cpc.DeudorId = d.Id
    GROUP BY u.Id, u.NombreUsuario, u.Email
    HAVING COUNT(DISTINCT ps.Id) > 0 OR COUNT(DISTINCT CASE 
            WHEN cpc.EstaPagado = 0 
            AND cpc.FechaVencimiento BETWEEN @hoy AND @limite 
            THEN cpc.Id 
        END) > 0
    ORDER BY SuscripcionesActivas DESC, DeudasProximas DESC

    PRINT ''
    PRINT '   ??  Usuarios que DEBERÍAN recibir notificaciones:'
    PRINT '      - Tienen SuscripcionesActivas > 0'
    PRINT '      - Y tienen DeudasProximas > 0'
END
PRINT ''

-- ??????????????????????????????????????????????????????????????
-- 4. DASHBOARD GENERAL
-- ??????????????????????????????????????????????????????????????
PRINT '4?? Dashboard General:'
PRINT ''

SELECT 
    'Usuarios Totales' as Metrica,
    CAST(COUNT(*) AS VARCHAR(10)) as Valor,
    '' as Detalles
FROM Usuarios

UNION ALL

SELECT 
    'Suscripciones Activas',
    CAST(ISNULL((SELECT COUNT(*) FROM PushSubscriptions WHERE Activa = 1), 0) AS VARCHAR(10)),
    CASE 
        WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PushSubscriptions')
        THEN ''
        ELSE '?? Tabla no existe'
    END
    
UNION ALL

SELECT 
    'Deudores Totales',
    CAST(COUNT(*) AS VARCHAR(10)),
    ''
FROM Deudores

UNION ALL

SELECT 
    'Cuentas por Cobrar Pendientes',
    CAST(COUNT(*) AS VARCHAR(10)),
    ''
FROM CuentasPorCobrar
WHERE EstaPagado = 0

UNION ALL

SELECT 
    'Deudas que Vencen HOY',
    CAST(COUNT(*) AS VARCHAR(10)),
    CASE WHEN COUNT(*) = 0 THEN '?? No hay' ELSE '? Hay datos' END
FROM CuentasPorCobrar
WHERE EstaPagado = 0
AND CAST(FechaVencimiento AS DATE) = @hoy

UNION ALL

SELECT 
    'Deudas Próximas (1-3 días)',
    CAST(COUNT(*) AS VARCHAR(10)),
    CASE WHEN COUNT(*) = 0 THEN '?? No hay' ELSE '? Hay datos' END
FROM CuentasPorCobrar
WHERE EstaPagado = 0
AND FechaVencimiento > @hoy
AND FechaVencimiento <= @limite

PRINT ''
PRINT '========================================='
PRINT '? DIAGNÓSTICO COMPLETADO'
PRINT '========================================='
PRINT ''
PRINT '?? PRÓXIMOS PASOS:'
PRINT ''
PRINT '   Si SuscripcionesActivas = 0:'
PRINT '      ? Ve a /Configuracion y activa las notificaciones'
PRINT ''
PRINT '   Si Deudas Próximas = 0:'
PRINT '      ? Ve a /Deudores y crea una cuenta por cobrar de prueba'
PRINT '      ? Fecha de vencimiento: HOY o MAÑANA'
PRINT ''
PRINT '   Si ambos > 0 pero no recibes notificaciones:'
PRINT '      ? Revisa los logs de Hangfire en /hangfire/jobs'
PRINT '      ? Revisa la consola del servidor'
PRINT '      ? Verifica permisos del navegador'
PRINT ''
