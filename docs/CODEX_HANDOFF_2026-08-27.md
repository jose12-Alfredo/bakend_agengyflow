# Handoff de Codex — 27 de agosto de 2026

Este documento deja el contexto de las dos intervenciones más recientes en el
backend de AgencyFlow. Léelo antes de modificar el dashboard o los indicadores
de productividad.

## Estado actual

- El backend es ASP.NET Core 10 y usa PostgreSQL, EF Core y JWT.
- La solución es `AgencyFlow.sln`; el proyecto web está en `AgencyFlow/`.
- El backend debe ejecutarse con el perfil `http`, en
  `http://localhost:5155`.
- Swagger, en desarrollo, está disponible en
  `http://localhost:5155/swagger`.
- Todas las rutas salvo `POST /api/users/login` requieren un JWT Bearer.

Los secretos de conexión y JWT se obtienen desde User Secrets, no desde el
repositorio. Consultar el `README.md` para configurarlos.

## Incidencia resuelta: contrato antiguo del dashboard

El frontend mostró el aviso:

> El backend activo usa el contrato anterior. Reinícialo para habilitar los
> KPI filtrados.

La causa no fue `ProjectTypesController`. Había un proceso viejo de
`AgencyFlow.exe` que mantenía bloqueado
`AgencyFlow/bin/Debug/net10.0/AgencyFlow.exe`; por ello `dotnet build` no
podía copiar el binario actualizado. Se detuvo esa instancia, se compiló de
nuevo y se inició el backend actualizado.

El contrato de dashboard actual está en:

- `GET /api/dashboard`
- Implementación: `Controllers/DashboardController.cs` y
  `Services/DashboardService.cs`.
- Filtros query soportados: `clientCompanyId`, `projectId`, `subProjectId`,
  `departmentId` y `responsibleUserId`.

Si el aviso vuelve a aparecer, verificar primero que el proceso activo sea el
binario recién compilado. Una compilación con el ejecutable bloqueado termina
con errores `MSB3027`/`MSB3021`.

### Reinicio local recomendado

Desde la raíz del repositorio:

```powershell
dotnet build .\AgencyFlow.sln --no-restore
dotnet run --project .\AgencyFlow\AgencyFlow.csproj --launch-profile http
```

No asumir que un PID indicado por un chat anterior sigue vigente. Confirmar la
API consultando Swagger antes de continuar:

```powershell
Invoke-RestMethod http://localhost:5155/swagger/v1/swagger.json
```

## Funcionalidad agregada: reporte de retrasos y productividad

Se agregó un reporte de retrasos para obtener visibilidad por proyecto,
subproyecto, tarea y subtarea.

### Endpoint

```text
GET /api/productivity/delays
Authorization: Bearer <token>
```

Controlador: `Controllers/ProductivityController.cs`.

Admite estos filtros opcionales por query string:

```text
clientCompanyId
projectId
subProjectId
departmentId
responsibleUserId
```

Ejemplo:

```text
GET /api/productivity/delays?projectId=<guid>&responsibleUserId=<guid>
```

### Respuesta

La respuesta tiene esta estructura de alto nivel:

```json
{
  "generatedAt": "2026-08-27T00:00:00Z",
  "summary": {
    "totalItemCount": 0,
    "completedItemCount": 0,
    "delayedItemCount": 0,
    "averageDelayDays": 0,
    "maximumDelayDays": 0,
    "onTimePercentage": 0
  },
  "items": []
}
```

Cada elemento de `items` expone:

| Campo | Descripción |
| --- | --- |
| `level` | `Project`, `SubProject`, `Task` o `SubTask`. |
| `id`, `parentId` | Identificador y padre dentro de la jerarquía. |
| `projectId`, `subProjectId`, `taskId` | Contexto jerárquico. |
| `title`, `status` | Nombre y estado actual. |
| `responsibleUserId`, `responsibleUserName` | Responsable si existe. |
| `plannedStartDate`, `plannedEndDate` | Fechas planificadas. |
| `completedAt` | Fecha/hora efectiva de finalización, cuando está disponible. |
| `delayDays` | Días calendario por encima de la fecha final planificada. `null` sin fecha límite. |
| `plannedDurationDays`, `actualDurationDays` | Duración prevista y transcurrida/real. |
| `isCompleted`, `isDelayed` | Estado derivado. |
| `completionSource` | Fuente usada para calcular la finalización. |

### Regla de cálculo

1. Con fecha límite, `delayDays = max(0, fecha de referencia - fecha límite)`.
2. Para elementos abiertos, la fecha de referencia es el día actual UTC; el
   retraso continúa creciendo mientras sigan abiertos.
3. Para tareas y subtareas completadas se toma el último evento de
   `task_status_history` o `subtask_status_history` cuyo destino es
   `Completado`/`Completada`.
4. Si falta ese historial pero el elemento está marcado completado, se usa
   `UpdatedAt` como respaldo.
5. Proyectos y subproyectos no tienen historial propio. Su fecha de
   finalización se deriva de sus hijos terminados cuando es posible y se usa
   `UpdatedAt` como respaldo. El campo `completionSource` permite al frontend
   indicar el origen (`StatusHistory`, `ChildTasks`, `ChildSubProjects`,
   `UpdatedAt` o `CurrentDate`).

Importante: el reporte mide puntualidad/retraso. Es un indicador de
productividad, pero no equivale por sí solo a productividad individual; para
una medición más completa se puede combinar después con volumen entregado,
tiempo de ciclo y carga asignada.

### Implementación

- DTOs: `DTOs/Productivity/DelayReportDto.cs`.
- Lógica: `Services/ProductivityService.cs`.
- Registro DI: `Program.cs` incluye
  `builder.Services.AddScoped<ProductivityService>();`.
- No se creó migración: no cambió el esquema de la base de datos.

La implementación carga los elementos activos, aplica los filtros, consulta
los historiales de estados para tareas/subtareas y construye una lista plana
ordenada por mayor retraso. La jerarquía se conserva mediante los IDs de
contexto y `parentId`.

## Verificación realizada

Después de compilar e iniciar el backend se validó:

```text
dotnet build AgencyFlow.sln --no-restore
```

Resultado: compilación correcta, sin errores ni advertencias.

También se autenticó contra la base local y se consultó
`GET /api/productivity/delays`. La respuesta contenía los cuatro niveles
(`Project`, `SubProject`, `Task`, `SubTask`) y el resumen; en ese momento se
obtuvieron 32 elementos y 15 con retraso. Son datos de la base de desarrollo,
por lo que pueden cambiar.

## Archivos que no son parte de esta funcionalidad

`Controllers/ProjectTypesController.cs` está correcto y no se modificó en
estas intervenciones. Su responsabilidad es el CRUD de tipos de proyecto,
no el dashboard ni los indicadores de productividad.

## Siguiente trabajo sugerido

- Integrar `GET /api/productivity/delays` en el frontend como tabla/indicador
  filtrable.
- Mostrar `completionSource` o una nota visual cuando la fecha se infiere de
  hijos o de `UpdatedAt`.
- Si se necesita auditoría exacta también para proyectos y subproyectos,
  agregar estados e historial propios a esas entidades; requerirá modelos,
  servicios y una migración.
