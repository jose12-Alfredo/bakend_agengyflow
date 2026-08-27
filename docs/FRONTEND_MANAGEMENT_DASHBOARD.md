# Integración frontend: dashboard gerencial

## Endpoint y filtros

Usar `GET /api/dashboard` con JWT. Los filtros son compatibles con los
anteriores y pueden combinarse:

```text
clientCompanyId, projectId, subProjectId, departmentId, responsibleUserId,
year, month
```

`year` y `month` son opcionales; si se omiten se toma el mes UTC actual. El
alcance mensual es único para todos los cálculos nuevos: un elemento pertenece
al mes si su intervalo planificado se solapa con el mes (`startDate <= último
día` y `endDate` nulo o `endDate >= primer día`). Por ello, una tarea abierta
iniciada antes del mes sigue apareciendo en el mes seleccionado.

No enviar combinaciones jerárquicas incompatibles. El backend devuelve `400`
si un proyecto no pertenece al cliente o un subproyecto no pertenece al
proyecto/cliente indicados.

## Campos nuevos aditivos

Los campos existentes de `kpis` se mantienen. Se agregan:

```text
kpis.unassignedTaskCount
kpis.unassignedSubTaskCount
kpis.globalProgressPercentage  // null si no hay elementos calculables
kpis.stalledTaskCount
kpis.stalledSubTaskCount
overdueSubTasks
unassignedTasks
unassignedSubTasks
clientHealth
clientDetail                    // solo al filtrar por clientCompanyId
```

Las listas de riesgo nuevas contienen IDs navegables y contexto: `itemId`,
`parentTaskId`, `subProjectId`, `projectId`, `clientCompanyId`, `departmentId`,
además de títulos, `departmentName`, `assignedUserId`, `assignedUserName`,
estado, fecha límite y actividad. Un responsable ausente llega como `null`.

`clientHealth` entrega representante, avance nullable y conteos separados. Al
usar `clientCompanyId`, `clientDetail.projects` entrega la jerarquía
Proyecto → Subproyecto → Tarea → Subtarea, con área y responsable en cada
nivel operativo.

## Reglas que el frontend debe reflejar

- En los selectores de tarea y subtarea excluir `roleName === "Cliente"`.
- La opción **Sin responsable** envía `assignedUserId: null`.
- Una tarea o subtarea sin responsable solo puede quedar en `Pendiente`.
  Mostrar el error de negocio que devuelve el servidor si se intenta cambiar
  a otro estado.
- Usar `PATCH /api/tasks/{id}/assignee` y
  `PATCH /api/subtasks/{id}/assignee` para la reasignación rápida; ambos
  reciben `{ "assignedUserId": "guid-o-null" }`.
- Ante un `400` con `message`, conservar el formulario y mostrar ese mensaje;
  no reemplazarlo por un error HTTP genérico.

## Actividad y avance

El backend actualiza `lastActivityAt` ante edición relevante, reasignación,
cambio de estado y creación/eliminación de comentarios. La actividad de una
subtarea actualiza también su tarea padre. Un elemento activo entra en
“sin movimiento” después de 3 días calendario; los elementos pausados se
excluyen de esa lista.

El avance se calcula desde abajo: subtareas completadas/activas para una tarea
con subtareas; `ownProgressPercentage` para una tarea sin subtareas; y promedio
sin ponderar hacia subproyecto, proyecto y cliente. Si no hay datos
calculables, el valor es `null`; el frontend debe mostrar “Sin datos”, no 0%.

## Migración requerida

Antes de desplegar este contrato, aplicar la migración
`20260827123000_AddManagementDashboardActivity`. Agrega actividad a tareas y
subtareas, avance propio opcional y `representativeUserId` en empresa cliente.
El representante se configura desde el CRUD de `/api/client-companies` y debe
ser un usuario interno activo; no se infiere desde tareas.
