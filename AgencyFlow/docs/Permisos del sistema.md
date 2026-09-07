# Permisos del sistema

## Propósito

Este documento registra el catálogo funcional de permisos de AgencyFlow. Describe
el comportamiento comprobado en el backend actual y sirve como base para un futuro
panel de permisos por usuario.

Estado del documento: catálogo inicial de solo lectura, levantado desde los
controladores, servicios y reglas de autorización existentes.

## Situación actual

`SuperUsuario` y `Gerente` pertenecen actualmente al grupo interno
`Administrators`. No existe una autorización exclusiva para uno de esos dos roles;
por tanto, ambos tienen los mismos permisos efectivos.

Los permisos se conceden hoy principalmente por rol. Todavía no existen tablas ni
endpoints para conceder permisos individuales a un usuario.

## Catálogo actual de SuperUsuario y Gerente

La columna Estado utiliza `Actual` para indicar que la capacidad ya existe. Los
códigos son nombres propuestos para representar de manera granular capacidades
que hoy pueden estar agrupadas dentro de un mismo endpoint.

| Dominio | Código | Capacidad | Alcance actual | Estado |
|---|---|---|---|---|
| Dashboard | `dashboard.view` | Ver indicadores generales | Global | Actual |
| Dashboard | `dashboard.filter` | Filtrar indicadores por cliente, proyecto, subproyecto, área, responsable y fechas | Global | Actual |
| Dashboard | `dashboard.client_hierarchy.view` | Ver la jerarquía y el progreso de un cliente | Global | Actual |
| Productividad | `productivity.delays.view` | Consultar el reporte de retrasos | Global | Actual |
| Roles | `roles.view` | Listar y consultar roles | Global | Actual |
| Roles | `roles.create` | Crear roles | Global | Actual |
| Roles | `roles.update` | Modificar roles | Global | Actual |
| Roles | `roles.delete` | Desactivar roles | Global | Actual |
| Usuarios | `users.view` | Listar y consultar usuarios | Global | Actual |
| Usuarios | `users.clients.view` | Listar usuarios con rol Cliente | Global | Actual |
| Usuarios | `users.create` | Crear usuarios | Global | Actual |
| Usuarios | `users.update` | Modificar datos de usuarios | Global | Actual |
| Usuarios | `users.password.update` | Cambiar la contraseña de un usuario desde su edición | Global; agrupado en `users.update` | Actual |
| Usuarios | `users.role.assign` | Asignar o cambiar el rol de un usuario | Global; agrupado en `users.update` | Actual |
| Usuarios | `users.client_company.assign` | Vincular un usuario con una empresa cliente | Global; agrupado en `users.update` | Actual |
| Usuarios | `users.delete` | Desactivar usuarios mediante borrado lógico | Global | Actual |
| Departamentos | `departments.view` | Listar y consultar departamentos | Global | Actual |
| Departamentos | `departments.create` | Crear departamentos | Global | Actual |
| Departamentos | `departments.update` | Modificar departamentos | Global | Actual |
| Departamentos | `departments.delete` | Desactivar departamentos | Global | Actual |
| Miembros de áreas | `department_memberships.view` | Ver integrantes y directores de áreas | Global | Actual |
| Miembros de áreas | `department_memberships.create` | Incorporar usuarios a departamentos | Global | Actual |
| Miembros de áreas | `department_memberships.update` | Cambiar la pertenencia de un usuario | Global | Actual |
| Miembros de áreas | `department_memberships.delete` | Retirar usuarios de departamentos | Global | Actual |
| Directores de área | `department_directors.assign` | Designar como director de área a un usuario con rol Director | Global; agrupado en membresías | Actual |
| Directores de área | `department_directors.remove` | Retirar la dirección de un área | Global; agrupado en membresías | Actual |
| Empresas cliente | `client_companies.view` | Listar y consultar empresas cliente | Global | Actual |
| Empresas cliente | `client_companies.create` | Crear empresas cliente | Global | Actual |
| Empresas cliente | `client_companies.update` | Modificar empresas cliente | Global | Actual |
| Empresas cliente | `client_companies.delete` | Desactivar empresas cliente | Global | Actual |
| Empresas cliente | `client_companies.representative.assign` | Asignar o retirar el representante de una empresa | Global; agrupado en edición | Actual |
| Tipos de proyecto | `project_types.view` | Listar y consultar tipos de proyecto | Global | Actual |
| Tipos de proyecto | `project_types.create` | Crear tipos de proyecto | Global | Actual |
| Tipos de proyecto | `project_types.update` | Modificar tipos de proyecto | Global | Actual |
| Tipos de proyecto | `project_types.delete` | Desactivar tipos de proyecto | Global | Actual |
| Proyectos | `projects.view` | Listar y consultar proyectos y su detalle completo | Global | Actual |
| Proyectos | `projects.create` | Crear proyectos | Global | Actual |
| Proyectos | `projects.update` | Modificar proyectos | Global | Actual |
| Proyectos | `projects.delete` | Desactivar proyectos | Global | Actual |
| Accesos de directores | `project_director_access.view` | Consultar accesos otorgados | Global | Actual |
| Accesos de directores | `project_director_access.grant` | Conceder a un Director acceso a un proyecto | Global; destinatario Director activo | Actual |
| Accesos de directores | `project_director_access.revoke` | Revocar el acceso de un Director | Global | Actual |
| Subproyectos | `subprojects.view` | Listar y consultar subproyectos | Global | Actual |
| Subproyectos | `subprojects.create` | Crear subproyectos | Global | Actual |
| Subproyectos | `subprojects.update` | Modificar contenido, fechas, estado, proyecto o área | Global | Actual |
| Subproyectos | `subprojects.assign` | Asignar o retirar responsables | Cualquier usuario interno activo; no Cliente | Actual |
| Subproyectos | `subprojects.delete` | Desactivar subproyectos | Global | Actual |
| Tareas | `tasks.view` | Listar y consultar tareas | Global | Actual |
| Tareas | `tasks.create` | Crear tareas, inicialmente Pendientes | Global | Actual |
| Tareas | `tasks.update` | Modificar contenido, fechas, subproyecto y progreso propio | Global | Actual |
| Tareas | `tasks.assign` | Asignar o retirar responsables | Cualquier usuario interno activo; no Cliente | Actual |
| Tareas | `tasks.delete` | Desactivar tareas | Global | Actual |
| Subtareas | `subtasks.view` | Listar y consultar subtareas | Global | Actual |
| Subtareas | `subtasks.create` | Crear subtareas | Global | Actual |
| Subtareas | `subtasks.update` | Modificar contenido, fechas, estado, tarea padre y responsable | Global | Actual |
| Subtareas | `subtasks.status.update` | Cambiar el estado de una subtarea | Global; sujeto a estados válidos | Actual |
| Subtareas | `subtasks.assign` | Asignar o retirar responsables | Cualquier usuario interno activo; no Cliente | Actual |
| Subtareas | `subtasks.delete` | Desactivar subtareas | Global | Actual |
| Comentarios | `subtask_comments.view` | Ver comentarios de subtareas | Global | Actual |
| Comentarios | `subtask_comments.create` | Crear comentarios con la identidad autenticada | Global | Actual |
| Comentarios | `subtask_comments.delete_own` | Eliminar comentarios propios | Solamente comentario propio | Actual |

## Catálogo actual de Director para gestionar su equipo

| Dominio | Código | Capacidad | Alcance actual | Estado |
|---|---|---|---|---|
| Mi equipo | `director_team.view` | Ver integrantes del área que dirige | Solo áreas con membresía activa `isDirector=true` | Actual |
| Mi equipo | `director_team.roles.view` | Ver roles permitidos para nuevos integrantes | Operativo 1, Operativo 2 y Pasante | Actual |
| Mi equipo | `director_team.users.create` | Crear una cuenta y agregarla al equipo | Solo su área dirigida y roles permitidos | Actual |
| Mi equipo | `director_team.candidates.view` | Buscar cuentas existentes elegibles | Solo usuarios activos de roles permitidos que aún no estén en el área | Actual |
| Mi equipo | `director_team.existing.add` | Agregar una cuenta existente al equipo | Solo su área dirigida; conserva rol, contraseña, datos y otras membresías | Actual |

El Director no recibe mediante estas capacidades permiso para modificar la cuenta,
la contraseña o el rol global del usuario, ni para agregar Gerentes,
SuperUsuarios, Directores o Clientes.

## Catálogo actual de Director para el dashboard

| Dominio | Código | Capacidad | Alcance actual | Estado |
|---|---|---|---|---|
| Dashboard | `dashboard.view` | Ver indicadores, carga, riesgos y progreso | Proyectos autorizados y, dentro de ellos, solo subproyectos, tareas y subtareas de áreas con membresía activa `isDirector=true` | Actual |
| Dashboard | `dashboard.filter` | Filtrar los indicadores visibles | Solo sobre los datos comprendidos en su alcance de proyecto y área | Actual |
| Dashboard | `dashboard.client_hierarchy.view` | Ver la jerarquía de un cliente | Solo proyectos autorizados y trabajo de áreas dirigidas; no expone otras áreas | Actual |

El filtro de alcance se aplica en el backend antes de calcular métricas. En el caso
de un Director, el dashboard exige simultáneamente acceso activo al proyecto y una
relación `DepartmentUser` activa con `isDirector=true` para el área del subproyecto.
El avance promedio de una persona combina sus tareas principales activas con las
subtareas que tenga asignadas bajo otras tareas, incluso si pertenecen a distintos
subproyectos. Una subtarea no se contabiliza por separado cuando su tarea padre
activa también está asignada a la misma persona, para evitar el doble conteo. Si no
tiene trabajo asignado, el valor es cero.

## Catálogo actual de Operativos para comentarios

Aplica a los roles `Operativo 1`, `Operativo 2` y `Pasante`.

| Dominio | Código | Capacidad | Alcance actual | Estado |
|---|---|---|---|---|
| Comentarios | `subtask_comments.view` | Ver los comentarios de una subtarea | Solo subtareas activas asignadas al usuario autenticado | Actual |
| Comentarios | `subtask_comments.create` | Comentar con la identidad autenticada | Solo subtareas activas asignadas al usuario autenticado | Actual |
| Comentarios | `subtask_comments.delete_own` | Eliminar un comentario propio | Comentario propio dentro de una subtarea asignada al usuario | Actual |

La interfaz de **Mi trabajo** muestra la acción **Comentarios** al abrir una
subtarea. El backend vuelve a comprobar la asignación; conocer o modificar el
identificador de otra subtarea no concede acceso a sus comentarios.

## Catálogo actual de notificaciones

Aplica a `SuperUsuario`, `Gerente`, `Director`, `Operativo 1`, `Operativo 2` y
`Pasante`. La identidad se obtiene exclusivamente del JWT.

| Dominio | Código | Capacidad | Alcance actual | Estado |
|---|---|---|---|---|
| Notificaciones | `notifications.view_own` | Consultar la bandeja y el contador | Solamente notificaciones cuyo destinatario es el usuario autenticado | Actual |
| Notificaciones | `notifications.read_own` | Marcar una o todas como leídas | Solamente la bandeja propia | Actual |
| Notificaciones | `notifications.assignment.receive` | Recibir avisos de asignación | Responsable nuevo de la tarea o subtarea | Actual |
| Notificaciones | `notifications.comment.receive` | Recibir avisos de comentarios | Responsable y participantes previos de la conversación, excepto el actor | Actual |
| Notificaciones | `notifications.status.receive` | Recibir cambios de estado y revisión | Responsable, administradores y directores con acceso al proyecto y dirección activa del área; excepto el actor | Actual |
| Notificaciones | `notifications.deadline.receive` | Recibir próximos vencimientos y atrasos | Tareas y subtareas asignadas directamente al usuario | Actual |

Los vencimientos se generan de forma idempotente al consultar la bandeja para el
día actual y abarcan los siguientes tres días o trabajo ya vencido. El índice de
deduplicación evita repetir el mismo aviso diario.

## Reglas transversales actuales

- La identidad y el rol del actor se obtienen del JWT.
- SuperUsuario y Gerente omiten las restricciones de acceso de Director por
  proyecto y área.
- Los responsables deben ser usuarios activos, con rol activo y distintos de
  `Cliente`.
- Una tarea o subtarea sin responsable solamente puede permanecer en estado
  `Pendiente`.
- Las eliminaciones administrativas observadas utilizan borrado lógico mediante
  `DeletedAt`.
- La designación de director de área exige que el usuario tenga rol `Director`.
- Un acceso de proyecto solamente puede concederse a un usuario activo con rol
  `Director` y no puede duplicarse mientras continúe activo.
- El endpoint especial `/api/director/team-members` es exclusivo del rol Director.
  SuperUsuario y Gerente administran usuarios y membresías mediante los endpoints
  generales.

## Diferencias actuales entre SuperUsuario y Gerente

No hay diferencias efectivas. En particular, el Gerente puede actualmente:

- crear un usuario con rol SuperUsuario;
- cambiar el rol de otro usuario a SuperUsuario;
- modificar o desactivar un SuperUsuario;
- crear, modificar o desactivar roles del sistema.

Antes de habilitar permisos personalizados debe decidirse si estas operaciones
seguirán compartidas o quedarán reservadas al SuperUsuario.

## Limitaciones detectadas

- No existe todavía `permissions.manage` ni un panel de permisos por usuario.
- No existe una tabla de concesiones, denegaciones o alcances individuales.
- No existe un permiso de moderación para eliminar comentarios ajenos.
- El DTO de edición de tareas recibe `Status`, pero el servicio actual no copia
  directamente ese valor a la tarea; el estado también se sincroniza a partir de
  las subtareas.
- Varias capacidades sensibles están agrupadas en un solo endpoint de edición,
  especialmente cambio de contraseña, asignación de rol y vínculo con empresa.

## Catálogo propuesto para administración de permisos

Estas capacidades aún no existen y deberán reservarse hasta que se defina quién
puede administrarlas:

| Código | Descripción | Estado |
|---|---|---|
| `permissions.view` | Ver los permisos efectivos de roles y usuarios | Propuesto |
| `permissions.manage` | Conceder, denegar y revocar permisos individuales | Propuesto |
| `permissions.scope.manage` | Limitar permisos por área o proyecto | Propuesto |
| `permissions.audit.view` | Consultar el historial de cambios de permisos | Propuesto |
| `superusers.create` | Crear cuentas SuperUsuario | Propuesto |
| `superusers.update` | Modificar cuentas SuperUsuario | Propuesto |
| `superusers.delete` | Desactivar cuentas SuperUsuario | Propuesto |
| `system_roles.manage` | Administrar los roles protegidos del sistema | Propuesto |

Recomendación inicial: reservar estas ocho capacidades para `SuperUsuario` y no
concederlas por defecto a `Gerente`.

## Modelo recomendado para la siguiente etapa

```text
Permission
  Code
  Name
  Description
  IsSensitive

RolePermission
  RoleId
  PermissionId

UserPermission
  UserId
  PermissionId
  Effect: Allow | Deny

UserPermissionScope
  UserPermissionId
  ScopeType: Global | Department | Project | OwnWork
  ScopeId

PermissionAudit
  ActorUserId
  TargetUserId
  PermissionId
  Action
  PreviousValue
  NewValue
  Timestamp
```

Este modelo conserva los roles como base y permite excepciones individuales sin
convertir cada combinación de permisos en un rol nuevo.
