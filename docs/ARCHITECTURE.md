# Arquitectura de AgencyFlow

## Recorrido de una peticion

```text
Frontend o cliente HTTP
        |
        v
Controller -> Service -> AppDbContext -> PostgreSQL
        |          |
        v          v
       DTO       Models
```

Ejemplo: al enviar `POST /api/projects`, `ProjectsController` recibe un `CreateProjectDto`, llama a `ProjectService`, el servicio valida las relaciones y crea un `Project`, y `AppDbContext` lo guarda en PostgreSQL.

## Capas

### Models

Representan las entidades almacenadas en la base de datos. Sus relaciones producen llaves foraneas. Todas heredan de `BaseEntity`, que aporta `Id` y datos de auditoria y borrado logico.

### DTOs

Son los contratos JSON de entrada y salida. Un DTO evita exponer directamente una entidad de la base de datos. Los DTO de creacion y actualizacion contienen reglas de validacion.

### Data

`AppDbContext` conecta Entity Framework con PostgreSQL y declara las tablas mediante `DbSet`. `SeedData` define datos iniciales reproducibles. `Migrations` contiene el historial de cambios de la base.

### Services

Contienen la logica del negocio: filtros, validacion de relaciones, conversion entre Models y DTOs, guardado y borrado logico. Cada servicio recibe `AppDbContext` mediante inyeccion de dependencias.

### Controllers

Exponen las rutas HTTP, reciben parametros y DTOs, llaman al servicio correspondiente y devuelven codigos HTTP como `200`, `201`, `400`, `404` o `401`.

### Program.cs

Es el punto de inicio y composicion. Registra DbContext, Services, Controllers, JWT, CORS y Swagger. Luego configura el orden del middleware y mapea los Controllers.

## Direccion de las dependencias

```text
Controllers dependen de Services y DTOs
Services dependen de Data, Models y DTOs
Data depende de Models y Entity Framework
Models no dependen de Controllers ni Services
```

Esta direccion evita mezclar responsabilidades. Un Controller no escribe SQL y un Model no conoce las rutas HTTP.

## Entidades y relaciones

- `User` pertenece a un `Role` y opcionalmente a una `ClientCompany`.
- `DepartmentUser` relaciona usuarios con departamentos.
- `Project` pertenece a un `ProjectType` y a un usuario cliente.
- `SubProject` pertenece a un proyecto y un departamento; puede tener un usuario asignado.
- `TaskItem` pertenece a un subproyecto; puede tener un usuario asignado.
- `SubTask` pertenece a una tarea; puede tener un usuario asignado.

## Conceptos importantes

- **Inyeccion de dependencias:** ASP.NET crea los Services y les entrega su `AppDbContext`.
- **Borrado logico:** eliminar asigna `DeletedAt`; no borra fisicamente la fila.
- **AsNoTracking:** mejora consultas que solo leen y no modifican entidades.
- **Include:** carga datos relacionados, por ejemplo el nombre del rol de un usuario.
- **JWT:** identifica al usuario autenticado en cada peticion protegida.
- **Migracion:** versiona cambios de estructura o datos iniciales de la base.
