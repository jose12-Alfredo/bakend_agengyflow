# AgencyFlow Backend - Guia de continuidad para agentes

Documento verificado contra el codigo del backend el 26 de agosto de 2026.
Su objetivo es permitir que otro agente entienda el sistema antes de modificarlo.

## 1. Ubicacion y alcance

- Solucion: `C:\Users\jose1\RiderProjects\AgencyFlow\AgencyFlow.sln`
- Proyecto backend: `C:\Users\jose1\RiderProjects\AgencyFlow\AgencyFlow`
- Proyecto frontend relacionado: `C:\Users\jose1\RiderProjects\agencyflow-frontend`
- Namespace principal: `AgencyFlow`
- API local HTTP: `http://localhost:5155`
- Swagger en Development: `http://localhost:5155/swagger`

Este documento describe el backend AgencyFlow. No confundirlo con el proyecto
de referencia `TaskSystem-Backend` que tambien existe en la maquina.

## 2. Stack y paquetes

- ASP.NET Core Web API sobre `.NET 10`.
- Entity Framework Core `10.0.9`.
- PostgreSQL mediante `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2`.
- Base remota alojada en Neon.
- JWT Bearer para autenticacion.
- BCrypt para hash y verificacion de contrasenas.
- Swagger/OpenAPI mediante Swashbuckle.
- Nullable reference types e implicit usings habilitados.

Paquetes declarados en `AgencyFlow.csproj`:

- `BCrypt.Net-Next 4.2.0`
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.9`
- `Microsoft.AspNetCore.OpenApi 10.0.9`
- `Microsoft.EntityFrameworkCore.Design 10.0.9`
- `Microsoft.OpenApi 2.11.0`
- `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2`
- `Swashbuckle.AspNetCore 10.2.3`

No existe actualmente un proyecto de pruebas automatizadas.

## 3. Arquitectura real

El proyecto usa una arquitectura por capas dentro de un solo proyecto:

```text
Cliente HTTP / frontend
        |
        v
Controllers  ->  Services  ->  AppDbContext  ->  PostgreSQL
    |               |               |
    v               v               v
DTOs        Models + DTOs          Models
```

### Controllers

- Definen rutas HTTP y parametros.
- Validan Data Annotations automaticamente por `[ApiController]`.
- Llaman a un servicio.
- Traducen `null` a `404`, creaciones a `201` y eliminaciones a `204`.
- Todos tienen `[Authorize]`, excepto `POST /api/users/login`.

### Services

- Contienen filtros, reglas de negocio, consultas EF y mapeo a DTOs.
- Validan existencia de relaciones antes de guardar.
- Implementan borrado logico.
- Lanzan `BusinessValidationException` para errores esperados.

### DTOs

- Separan los contratos JSON de las entidades persistidas.
- Hay DTOs de crear, actualizar y respuesta por modulo.
- Los DTOs con fechas implementan `IValidatableObject` para impedir que
  `EndDate` sea anterior a `StartDate`.
- Los listados usan `PagedResultDto<T>` con `TotalItems`, `Page`, `PageSize`
  e `Items`.

### Models

- Representan tablas y relaciones.
- Casi todos heredan de `BaseEntity`.
- Los historiales de estado no heredan de `BaseEntity`.

### Data

- `AppDbContext` declara los `DbSet`, indices y tablas especiales.
- `SeedData` agrega datos iniciales con GUID fijos.
- `Migrations` versiona esquema y normalizaciones de datos.

### Program.cs

Es la raiz de composicion. Configura:

1. Controllers y Swagger.
2. JWT y autorizacion.
3. CORS.
4. PostgreSQL.
5. Servicios `Scoped`.
6. Manejador global de excepciones.
7. Orden del middleware.

## 4. Configuracion y secretos

Claves de configuracion utilizadas:

```text
ConnectionStrings:CoreConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience
```

`Jwt:Key` y la cadena de Neon son secretos. Nunca deben copiarse a este
documento, commits, logs ni mensajes para otros agentes.

Configuracion local recomendada desde la carpeta de `AgencyFlow.csproj`:

```powershell
dotnet user-secrets set "ConnectionStrings:CoreConnection" "CADENA_PRIVADA"
dotnet user-secrets set "Jwt:Key" "CLAVE_PRIVADA_LARGA"
dotnet user-secrets set "Jwt:Issuer" "AgencyFlow"
dotnet user-secrets set "Jwt:Audience" "AgencyFlowFrontend"
```

Comandos habituales:

```powershell
dotnet restore
dotnet build
dotnet ef database update
dotnet run --launch-profile http
```

Si `dotnet build` indica que `AgencyFlow.exe` esta bloqueado, existe otra
instancia del backend ejecutandose. Detenerla antes de compilar.

## 5. Middleware y respuestas de error

Orden actual:

```text
UseExceptionHandler
UseSwagger / UseSwaggerUI (solo Development)
UseHttpsRedirection
UseCors("AllowAll")
UseAuthentication
UseAuthorization
MapControllers
```

El manejador global transforma:

- `BusinessValidationException` -> HTTP `400` con `{ "message": "..." }`.
- Cualquier otra excepcion -> HTTP `500` con mensaje generico.

ASP.NET maneja automaticamente:

- DTO invalido -> HTTP `400` con ProblemDetails.
- Falta o token invalido -> HTTP `401`.
- Recurso no encontrado segun Controller -> HTTP `404`.
- Creacion correcta -> HTTP `201`.
- Borrado logico correcto -> HTTP `204`.

El CORS actual se llama `AllowAll` y permite cualquier origen, cabecera y
metodo. Es practico en desarrollo, pero debe restringirse antes de produccion.

## 6. Autenticacion y autorizacion

### Login

Ruta publica:

```http
POST /api/users/login
```

Flujo de `UserService.LoginAsync`:

1. Normaliza el email con `Trim()` y `ToLowerInvariant()`.
2. Busca usuario activo con rol, empresa y departamentos activos.
3. Verifica el hash BCrypt.
4. Crea JWT firmado con HMAC SHA-256 y duracion de 8 horas.
5. Devuelve token y perfil basico.

Claims incluidos:

- `ClaimTypes.NameIdentifier`: GUID del usuario.
- `ClaimTypes.Email`: correo.
- `ClaimTypes.Role`: nombre del rol.
- `departments`: nombres separados por coma.

### Limitacion actual

Existe autenticacion, pero no autorizacion granular por rol. Cualquier usuario
con token puede acceder tecnicamente a todos los Controllers protegidos. No hay
`[Authorize(Roles = "...")]`, policies ni comprobacion de pertenencia al cliente.

## 7. BaseEntity, auditoria y borrado logico

`BaseEntity` aporta:

```text
Id          Guid, generado en la aplicacion
CreatedAt   UTC, requerido
CreatedBy   opcional
UpdatedAt   opcional
UpdatedBy   opcional
DeletedAt   opcional
DeletedBy   opcional
```

Eliminar no borra fisicamente. Los servicios asignan `DeletedAt`.

No existen query filters globales en EF. Cada consulta debe escribir sus
condiciones `DeletedAt == null` manualmente.

Para recursos jerarquicos se valida toda la cadena padre:

```text
SubProject:  subproject activo + project activo
TaskItem:    task activo + subproject activo + project activo
SubTask:     subtask activo + task activo + subproject activo + project activo
Comment:     comment activo + toda la cadena anterior activa
```

Esto significa que borrar logicamente un padre oculta a sus descendientes sin
borrar sus filas. Restaurar un padre puede volver a mostrar descendientes que
no tengan su propio `DeletedAt`.

## 8. Entidades y relaciones

```text
Role 1 -------- N User
ClientCompany 1 - N User (opcional desde User)
User N -------- N Department mediante DepartmentUser

ProjectType 1 -- N Project
User(cliente) 1 - N Project (opcional desde Project)
Project 1 ------ N SubProject
Department 1 --- N SubProject
User 1 --------- N SubProject (responsable opcional)

SubProject 1 --- N TaskItem
User 1 --------- N TaskItem (responsable opcional)
TaskItem 1 ----- N SubTask
User 1 --------- N SubTask (responsable opcional)

TaskItem 1 ----- N TaskStatusHistory
SubTask 1 ------ N SubTaskStatusHistory
SubTask 1 ------ N SubTaskComment
User 1 --------- N SubTaskComment
```

### Role

Catalogo de roles: nombre y descripcion.

### Department

Catalogo de areas/departamentos: nombre y descripcion.

### ClientCompany

Empresa cliente: nombre, descripcion, email y telefono.

### ProjectType

Catalogo de tipos de proyecto: nombre y descripcion.

### User

- Nombre, apellido, telefono, email y password hash.
- `RoleId` obligatorio.
- `ClientCompanyId` opcional.
- Puede pertenecer a varios departamentos mediante `DepartmentUser`.

El codigo no obliga actualmente a que solo el rol `Cliente` tenga empresa, ni
a que todo cliente tenga empresa. Esa coherencia depende de la capa de uso.

### DepartmentUser

Tabla puente entre usuario y departamento. Tiene indice unico filtrado para
`(UserId, DepartmentId)` mientras `DeletedAt IS NULL`. Una asociacion borrada
logicamente puede crearse nuevamente.

### Project

- Titulo, detalle, fechas.
- `ProjectTypeId` obligatorio.
- `ClientUserId` opcional.
- El cliente se obtiene indirectamente mediante
  `Project.ClientUser.ClientCompany`.

El servicio valida que `ClientUserId`, si existe, sea un usuario activo, pero
no comprueba actualmente que su rol sea exactamente `Cliente`.

### SubProject

- Pertenece obligatoriamente a un proyecto y departamento activos.
- Responsable opcional.
- Estado almacenado como texto; no hay catalogo ni enum validado para este
  modulo.
- El servicio permite actualmente cualquier usuario activo como responsable,
  incluido un cliente.

### TaskItem

Tarea principal dentro de un subproyecto:

- Responsable opcional.
- Puede tener muchas subtareas.
- Guarda historial de cambios de estado.
- Expone cantidad total, cantidad completada y porcentaje de progreso.

Regla de responsable vigente:

- Puede ser `null`.
- Si existe, debe ser usuario activo, con rol activo y rol distinto de
  `Cliente`.
- La misma regla se aplica en crear, editar y `PATCH /assignee`.

### SubTask

Unidad operativa dentro de una tarea:

- Responsable opcional.
- Comentarios e historial de estados.
- Regla de responsable igual a `TaskItem`: nunca rol `Cliente`.
- `IsOverdue` se calcula, no se persiste.

### SubTaskComment

- Contenido, subtarea y autor.
- El autor se obtiene del `NameIdentifier` del JWT, no del body.
- Solo el propio autor puede borrar logicamente su comentario.

### Historiales de estado

Tablas fisicas:

- `task_status_history`
- `subtask_status_history`

Columnas en snake_case: id, identificador del elemento, estado anterior,
estado nuevo y timestamp. Tienen indice por identificador + timestamp y
cascade fisico si se elimina fisicamente el padre.

## 9. Indices y restricciones de base

Configurados en `AppDbContext`:

- Email de usuario unico para filas activas:
  `Email IS NOT NULL AND DeletedAt IS NULL`.
- Asociacion usuario/departamento unica para filas activas.
- Indices cronologicos para ambos historiales de estado.

La aplicacion tambien valida el email antes de guardar, pero el indice es la
proteccion final ante concurrencia.

## 10. Estados y flujo operativo

Usar siempre las constantes de Models. No repetir textos a mano.

### Estados de SubTask

Definidos en `SubTaskStatuses`:

```text
Pending     -> Pendiente
InProgress  -> En proceso
InReview    -> En revision
Completed   -> Completada
Paused      -> Pausada
```

Los DTOs de crear y actualizar subtareas validan que el estado pertenezca a
`SubTaskStatuses.All`.

`Atrasada` no es un estado. Se calcula cuando:

```text
EndDate tiene valor
AND EndDate < fecha UTC actual
AND Status != SubTaskStatuses.Completed
```

### Estados de TaskItem

Definidos en `TaskItemStatuses`:

```text
Pending     -> Pendiente
InProgress  -> En curso
InReview    -> En revision
Completed   -> Completado
```

El estado de una tarea principal se sincroniza automaticamente desde sus
subtareas en `SubTaskService.SyncParentTaskStatusAsync`:

```text
0 subtareas                         -> Pending
todas Pending                       -> Pending
todas Completed                     -> Completed
todas las no completadas InReview   -> InReview
cualquier otra combinacion          -> InProgress
```

La sincronizacion ocurre al crear, actualizar o borrar logicamente una
subtarea. Si cambia el estado de la tarea, se registra `TaskStatusHistory`.

`TaskItemService.CreateAsync` siempre crea la tarea en `Pending`, aunque el
DTO contenga Status. `TaskItemService.UpdateAsync` tampoco asigna `dto.Status`.
Esto refleja que el estado debe derivarse de las subtareas, no editarse
directamente.

### Progreso de tarea

```text
si totalSubTasks == 0: progreso = 0
si no: round(completedSubTasks * 100.0 / totalSubTasks)
```

Solo cuenta `SubTaskStatuses.Completed`. Pendiente, proceso, revision, pausada
y atrasada no cuentan como completadas.

## 11. Modulos, rutas y reglas

Todos los listados usan `page >= 1` y `1 <= pageSize <= 100`.

### Roles - `/api/roles`

```text
GET    /api/roles?nombre=
GET    /api/roles/{id}
POST   /api/roles
PUT    /api/roles/{id}
DELETE /api/roles/{id}
```

CRUD simple con busqueda parcial por nombre y borrado logico.

### Departamentos - `/api/departments`

CRUD simple, filtro por nombre y borrado logico.

### Empresas cliente - `/api/client-companies`

CRUD simple; listado filtra por nombre y email.

### Tipos de proyecto - `/api/project-types`

CRUD simple y filtro por nombre.

### Usuarios - `/api/users`

```text
GET    /api/users?nombre=&apellido=&email=&roleId=&clientCompanyId=
GET    /api/users/clients?nombre=&apellido=&clientCompanyId=
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
POST   /api/users/login                 publico
```

Reglas:

- Email normalizado a minusculas y sin espacios externos.
- Email activo no puede repetirse.
- Password se guarda con BCrypt si fue proporcionado.
- En update, password vacio conserva el hash anterior.
- Rol debe existir y estar activo.
- Empresa opcional debe existir y estar activa.
- `GET /clients` localiza primero el rol activo llamado exactamente `Cliente`.

### Usuarios por departamento - `/api/department-users`

```text
GET    /api/department-users?userId=&departmentId=
GET    /api/department-users/{id}
POST   /api/department-users
PUT    /api/department-users/{id}
DELETE /api/department-users/{id}
```

Crear una relacion duplicada activa devuelve `409 Conflict`. Usuario y
departamento deben existir y estar activos.

### Proyectos - `/api/projects`

```text
GET    /api/projects?titulo=&projectTypeId=&clientUserId=&startDateFrom=&startDateTo=
GET    /api/projects/{id}
GET    /api/projects/{id}/full
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}
```

`/full` devuelve proyecto con subproyectos activos, departamento y responsable.
Tipo de proyecto obligatorio; usuario cliente opcional.

### Subproyectos - `/api/subprojects`

```text
GET    /api/subprojects?titulo=&projectId=&departmentId=&status=&assignedUserId=
GET    /api/subprojects/{id}
POST   /api/subprojects
PUT    /api/subprojects/{id}
DELETE /api/subprojects/{id}
```

Proyecto y departamento obligatorios y activos. Responsable opcional.

### Tareas - `/api/tasks`

```text
GET    /api/tasks?titulo=&clientCompanyId=&projectId=&subProjectId=&status=&assignedUserId=
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}
PATCH  /api/tasks/{id}/assignee
```

El cliente se filtra por la ruta relacional:
`Task -> SubProject -> Project -> ClientUser -> ClientCompany`.

`PATCH /assignee` acepta:

```json
{ "assignedUserId": "guid-o-null" }
```

### Subtareas - `/api/subtasks`

```text
GET    /api/subtasks?titulo=&taskItemId=&status=&assignedUserId=
GET    /api/subtasks/{id}
POST   /api/subtasks
PUT    /api/subtasks/{id}
DELETE /api/subtasks/{id}
```

Crear y actualizar registran historial si corresponde. Cada respuesta incluye
conteo de comentarios, responsable e indicador calculado de atraso.

No existe aun un `PATCH /api/subtasks/{id}/assignee`; para reasignar se usa el
`PUT` completo.

### Comentarios - `/api/subtasks/{subTaskId}/comments`

```text
GET    /api/subtasks/{subTaskId}/comments
POST   /api/subtasks/{subTaskId}/comments
DELETE /api/subtasks/{subTaskId}/comments/{commentId}
```

Contenido vacio o solo espacios se rechaza. El listado es cronologico.

### Dashboard - `/api/dashboard`

```text
GET /api/dashboard
GET /api/dashboard?clientCompanyId={id}&projectId={id}&subProjectId={id}
                  &departmentId={id}&responsibleUserId={id}
```

Devuelve un agregado gerencial construido en memoria a partir de snapshots de
tareas, subtareas, usuarios e historiales.

Todos los parametros son opcionales. Sin parametros conserva el resumen general
anterior. Los filtros se aplican a los KPI y a sus listas de detalle relacionadas
sin alterar, por ahora, las demas secciones agregadas del dashboard.

La jerarquia se resuelve siempre por las relaciones reales:
`ClientCompany -> ClientUser -> Project -> SubProject -> TaskItem -> SubTask`.
El area procede de `SubProject.DepartmentId`; el responsable se evalua en la
asignacion propia de cada tarea o subtarea.

## 12. Logica exacta del dashboard

### KPI: atrasos

Cuenta `TaskItem` con fecha limite anterior a hoy y estado no completado.
Cuenta por separado las subtareas con la misma condicion. El contrato devuelve
`OverdueTaskCount` y `OverdueSubTaskCount`; no se usa un join ni un total
indiferenciado que pueda duplicar unidades.

### KPI: tiempo de ciclo promedio

Por cada subtarea busca:

1. Primera entrada a `SubTaskStatuses.InProgress`.
2. Primera entrada posterior a `SubTaskStatuses.Completed`.
3. Calcula la diferencia en dias.
4. Promedia solo casos que tengan ambos eventos.

Si no existen casos completos, devuelve `null`.

### KPI: tasa de finalizacion

Actualmente usa subtareas:

```text
subtareas completadas * 100 / todas las subtareas
```

El contrato tambien devuelve el numerador (`CompletedSubTaskCount`) y el
denominador (`TotalSubTaskCount`). Si el denominador es cero, la interfaz debe
mostrar ausencia de datos y no una tasa real de 0%.

### KPI: proyectos y subproyectos en riesgo

Reutiliza la regla existente de salud de subproyectos: existe riesgo cuando hay
una subtarea vencida o cuando vencio el subproyecto y su progreso es menor a
100%. `AtRiskSubProjectCount` cuenta subproyectos unicos y
`AtRiskProjectCount` cuenta sus proyectos padre distintos.

### KPI: personas disponibles

Una persona esta disponible solo cuando no tiene tareas principales activas ni
subtareas activas en todo el sistema. `ResponsibleUserId` limita la persona
consultada y `DepartmentId` limita la poblacion por membresia de area. Los
filtros de cliente, proyecto y subproyecto no convierten artificialmente en
disponible a alguien que tenga trabajo activo fuera de ese contexto.

### Periodo

No se acepta todavia un rango temporal. Los historiales registran cambios de
estado, pero el modelo no define que trabajo se considera "planificado dentro
del periodo"; sin esa definicion, el denominador de finalizacion seria ambiguo.
La interfaz mantiene `Todo el historial` deshabilitado hasta acordar esa regla.

### KPI: cuellos de botella

Cuenta subtareas en `InReview` o `Paused`.

### Carga por persona

- Incluye usuarios activos con rol activo.
- Excluye clientes, salvo que tengan asignaciones historicamente invalidas.
- Separa tareas principales y subtareas asignadas.
- Cuenta activas, completadas, atrasadas y en revision.
- Promedio de avance usa solo tareas principales activas del responsable.
- Ordena por cantidad activa, atrasos y nombre.

La migracion `PreventClientWorkAssignments` dejo sin responsable las tareas y
subtareas previamente asignadas a clientes. Por tanto, normalmente clientes ya
no deben aparecer en carga por persona.

### Distribucion de flujo

Usa subtareas en Pending, InProgress, InReview y Completed. Paused se informa
por separado y no forma parte del denominador de la distribucion.

### Salud de subproyectos

- Considera hasta 6 subproyectos activos.
- Progreso = subtareas completadas / subtareas del subproyecto.
- Riesgo si hay una subtarea vencida o vencio el subproyecto con progreso < 100.
- Ordena riesgos primero y luego por fecha.

### Proximos vencimientos

Hasta 6 subtareas no completadas con fecha entre hoy y los proximos 5 dias.

### Tareas y subtareas sin movimiento

- Excluye completadas.
- Solo considera elementos cuya fecha de inicio ya llego.
- Punto inicial = max(ultimo cambio de estado, fecha de inicio).
- Si no hay historial, usa `UpdatedAt ?? CreatedAt` y lo compara con inicio.
- Dias sin cambio = piso de la diferencia UTC.
- Solo muestra valores mayores a cero y toma los primeros 6.

`Atrasada` y `Estancada` son conceptos diferentes:

- Atrasada: supero fecha limite.
- Estancada: lleva dias sin cambio de estado.

## 13. Seed inicial

`SeedData` usa GUID fijos y fecha UTC fija para que las migraciones sean
reproducibles. Incluye:

- Roles: Cliente, Gerente, Director, Operativo 1, Operativo 2 y Pasante.
- Departamentos de la agencia.
- Empresas Honor, Adidas y Tigo.
- Usuarios internos y clientes.
- Tipos de proyecto.
- Proyectos, subproyectos, tareas, subtareas y asociaciones de departamento.

Existe un hash BCrypt compartido en datos seed para usuarios de demostracion.
No documentar ni reutilizar su contrasena como credencial productiva.

## 14. Historial de migraciones

Orden funcional:

1. `InitialCreate`: roles.
2. `AddDepartments`.
3. `AddClientCompanies`.
4. `AddProjectTypes`.
5. `AddUsersAndDepartmentUsers`.
6. `AddProjectsAndSubProjects`.
7. `AddTaskItems`.
8. `AddSubTasks`.
9. `FixDepartmentUserUniqueIndex`.
10. `AddUniqueActiveUserEmail`.
11. `SeedInitialData`.
12. `AddSubTaskComments`.
13. `NormalizeSubTaskStatuses`.
14. `ReplaceSubTaskWorkflowStatuses`.
15. `AddTaskStatusHistory`.
16. `SeparateTaskAndSubTaskStatusHistory`.
17. `FixTaskItemInReviewEncoding`.
18. `PreventClientWorkAssignments`: migracion de datos; asignaciones a rol
    Cliente pasan a `NULL` sin borrar trabajo.

Cuando cambia un Model o mapping de `AppDbContext`:

```powershell
dotnet ef migrations add NombreDescriptivo
dotnet ef database update
```

Cambios solo en Controller, Service o DTO normalmente no requieren migracion.

## 15. Reglas de consulta y rendimiento

- Los listados de lectura usan principalmente `AsNoTracking()`.
- Filtros de texto usan `ToLower().Contains(...)`; PostgreSQL los traduce, pero
  no es la mejor opcion para indices o busquedas grandes.
- La paginacion se hace con `Skip((page - 1) * pageSize).Take(pageSize)`.
- No todos los listados tienen `OrderBy` antes de paginar; el orden puede no ser
  estable al crecer la base.
- Dashboard carga snapshots e historiales a memoria y calcula agregados en C#.
  Es adecuado para el volumen actual, no para grandes cantidades de datos.

## 16. Reglas que un agente no debe romper

1. No asignar tareas ni subtareas a usuarios con rol `Cliente`.
2. Permitir `AssignedUserId = null` en tareas y subtareas.
3. No convertir `Atrasada` en estado ni columna del flujo.
4. Calcular progreso solo con subtareas `Completed`.
5. Mantener sincronizacion automatica del estado padre.
6. Registrar historial en cada cambio efectivo de estado.
7. Respetar borrado logico propio y de todos los padres.
8. No borrar fisicamente datos existentes para corregir una relacion.
9. No incluir secretos de Neon o JWT en codigo/documentacion.
10. Reutilizar Services y DTOs existentes; no crear una API paralela.

## 17. Riesgos e inconsistencias conocidas

### Seguridad

- CORS permite cualquier origen.
- No hay permisos por rol ni por empresa/departamento.
- Todas las operaciones autenticadas estan disponibles para cualquier token.

### Integridad de dominio

- Project valida usuario activo, pero no que sea rol Cliente.
- SubProject permite responsable Cliente.
- Cambiar un usuario interno al rol Cliente no limpia automaticamente futuras
  referencias ya existentes; la validacion actua al crear/editar asignaciones.
- Borrar usuario logicamente no desasigna sus trabajos.

### Estados

- SubProject.Status es texto libre.
- TaskItem y SubTask usan vocabulario distinto (`En curso` vs `En proceso`,
  `Completado` vs `Completada`). El frontend normaliza para mostrar.
- Los literales con acentos han tenido una migracion de correccion. Usar las
  constantes y conservar archivos en UTF-8.

### API

- No existe filtro explicito `unassigned=true`; `assignedUserId` solo filtra un
  GUID. Para mostrar trabajo sin responsable se necesita extender la API o
  filtrar una carga completa en frontend.
- SubTask no tiene endpoint PATCH de responsable.
- No hay versionado de API.
- No hay pruebas automatizadas.

### Rendimiento y concurrencia

- Dashboard puede ser costoso al crecer los historiales.
- No hay tokens de concurrencia (`rowversion`, xmin, ETag).
- Algunos listados no ordenan antes de paginar.

## 18. Trabajo pendiente al momento de esta guia

El usuario solicito visibilidad destacada para tareas y subtareas sin
responsable. La idea aprobada fue:

1. KPI clicable `Trabajo sin responsable` en dashboard.
2. Panel `Por asignar`, separado en tareas y subtareas.
3. Asignacion rapida solo a usuarios internos.
4. Badge ambar `Sin responsable` en lista y tableros.
5. Filtro `Sin responsable` en tareas y subtareas.
6. Permitir crear en Pending sin responsable.
7. Impedir avanzar a proceso/revision/completado sin responsable.

Ese trabajo NO estaba terminado cuando se pidio esta documentacion. Solo se
habia implementado y verificado previamente:

- Bloqueo backend para asignar TaskItem/SubTask a rol Cliente.
- Filtro frontend que oculta clientes en formularios de tareas/subtareas.
- Migracion que dejo en NULL asignaciones antiguas a clientes.

No asumir que KPI, panel, filtros o bloqueo por estado ya existen.

## 19. Procedimiento recomendado para continuar

Antes de modificar:

1. Leer esta guia y los archivos del modulo.
2. Ejecutar `dotnet build`.
3. Revisar si el backend esta ejecutandose antes de recompilar.
4. Confirmar contratos que consume el frontend.
5. Hacer cambios pequenos y reutilizar `BusinessValidationException`.
6. Crear migracion solo si cambia modelo/mapping o se necesita normalizar datos.
7. Ejecutar build y probar tanto caso correcto como rechazo.
8. Verificar que recursos borrados logicamente y padres borrados no reaparezcan.

Para la funcionalidad pendiente de trabajo sin responsable, la ruta de menor
riesgo es:

1. Agregar `unassigned` opcional a GET de tareas y subtareas.
2. Agregar DTO agregado al Dashboard con tipo, ubicacion, estado y fecha.
3. Agregar `PATCH /api/subtasks/{id}/assignee` simetrico al de TaskItem.
4. Validar en Services que un elemento sin responsable solo permanezca Pending.
5. Actualizar frontend despues de estabilizar contratos.

## 20. Archivos clave para entrar rapido

```text
Program.cs
Data/AppDbContext.cs
Data/SeedData.cs
Models/Common/BaseEntity.cs
Models/TaskItemStatuses.cs
Models/SubTaskStatuses.cs
Services/UserService.cs
Services/TaskItemService.cs
Services/SubTaskService.cs
Services/SubTaskCommentService.cs
Services/DashboardService.cs
Controllers/TaskItemsController.cs
Controllers/SubTasksController.cs
Controllers/DashboardController.cs
DTOs/Dashboard/DashboardDto.cs
Migrations/AppDbContextModelSnapshot.cs
AgencyFlow.http
```

## 21. Resumen ejecutivo para pegar a otro agente

```text
Trabaja sobre AgencyFlow, ASP.NET Core 10 + EF Core + PostgreSQL Neon.
Arquitectura: Controller -> Service -> AppDbContext -> PostgreSQL, con DTOs.
Todos los recursos usan borrado logico manual y los hijos deben comprobar que
sus padres siguen activos. JWT protege todo salvo POST /api/users/login.
TaskItem pertenece a SubProject; SubTask pertenece a TaskItem. El estado de la
tarea principal se deriva automaticamente de sus subtareas. El progreso es
subtareas completadas / total. Atrasada es una condicion por EndDate, no un
estado. Cada cambio de estado se registra en historiales separados. TaskItem y
SubTask admiten responsable nulo, pero nunca un usuario con rol Cliente.
No hay autorizacion granular por rol, pruebas automatizadas ni filtros globales
de borrado logico. Lee docs/BACKEND_HANDOFF.md antes de editar y no expongas
ConnectionStrings:CoreConnection ni Jwt:Key.
```
