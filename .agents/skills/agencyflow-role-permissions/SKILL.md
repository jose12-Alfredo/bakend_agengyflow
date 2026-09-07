---
name: agencyflow-role-permissions
description: Implementa o revisa la autorizacion del backend de AgencyFlow para proyectos, areas, directores, equipos, subproyectos, tareas y subtareas. Usar al cambiar permisos por rol, accesos a proyectos, capacidades de directores o asignaciones limitadas por equipo.
---

# Permisos por rol de AgencyFlow

Implementar la autorización en el backend. La visibilidad de elementos en el
frontend sirve solamente para mejorar la experiencia de usuario y nunca debe
considerarse una barrera de seguridad.

## Modelo de permisos obligatorio

- `Gerente` y `SuperUsuario` tienen acceso completo de lectura y escritura.
- Un `Director` puede acceder solamente a los proyectos que le hayan autorizado
  expresamente.
- El acceso al proyecto y la autoridad sobre un área son requisitos independientes.
  Tener acceso al proyecto no permite operar en áreas que el director no dirige.
- Un director puede ver los datos generales de un proyecto autorizado, pero solo
  puede ver y administrar sus subproyectos, tareas y subtareas cuando pertenecen a
  un área donde tenga una relación `DepartmentUser` activa con
  `IsDirector == true`.
- Un director puede crear subproyectos solamente en las áreas que dirige y dentro
  de proyectos autorizados.
- Un director puede asignar tareas y subtareas solamente a usuarios activos, que
  no sean clientes y que tengan una pertenencia activa al área del subproyecto.
- Un director puede crear integrantes de equipo solamente con los roles
  `Operativo 1`, `Operativo 2` o `Pasante`. La creación debe exigir un área
  dirigida por el director autenticado y crear atómicamente una relación
  `DepartmentUser` activa con `IsDirector == false`.
- Un director no puede crear cuentas con roles `SuperUsuario`, `Gerente`,
  `Director` o `Cliente`.
- Solamente `Gerente` y `SuperUsuario` pueden conceder o revocar accesos a
  proyectos y administrar sin restricciones usuarios, roles, áreas y clientes.
- Responder `401 Unauthorized` cuando falte la autenticación o sea inválida, y
  `403 Forbidden` cuando el usuario esté autenticado pero no tenga permiso. No
  presentar errores de autorización como errores de validación.

## Modelo de datos

Representar los accesos a proyectos con una entidad de borrado lógico equivalente
a:

```text
ProjectDirectorAccess
  ProjectId
  DirectorUserId
  GrantedByUserId
  CreatedAt
  UpdatedAt
  DeletedAt
```

Permitir solamente un acceso activo por `(ProjectId, DirectorUserId)`. Validar que
el destinatario sea un usuario activo con rol `Director`. Un proyecto puede tener
varios directores autorizados y un director puede tener acceso a varios proyectos.

Agregar el rol `SuperUsuario` sin depender de nombres de rol enviados por los
clientes. Conservar el rol `Gerente` y los identificadores existentes de los datos
iniciales, salvo que una migración exija modificarlos.

## Arquitectura de autorización

Usar el claim `NameIdentifier` y el claim de rol del JWT del usuario autenticado.
Nunca aceptar el identificador ni el rol del usuario que realiza la acción desde
el cuerpo o los parámetros de una solicitud.

Centralizar las comprobaciones por recurso en un servicio de autorización con
alcance por solicitud. Esto evita reglas diferentes entre controladores y
servicios de dominio. Usar políticas de roles de ASP.NET para restricciones
generales de endpoints y el servicio central para comprobar proyecto, área y
pertenencia al equipo.

Aplicar autorización tanto a lecturas como a modificaciones:

- Restringir las consultas de colecciones en la base de datos antes de paginar.
- Verificar el acceso al recurso en los endpoints de detalle antes de devolverlo.
- En respuestas anidadas de proyectos, omitir áreas fuera de la autoridad del
  director.
- Aplicar las mismas reglas a crear, actualizar, eliminar, cambiar responsables,
  comentar, consultar dashboards y consultar jerarquías.
- No confiar en filtros opcionales enviados por el frontend, como `departmentId`,
  `projectId` o `assignedUserId`, para controlar la autorización.

Usar una transacción al crear integrantes desde una cuenta de director, de modo
que el usuario y su pertenencia al área se guarden juntos o no se guarde ninguno.

## Capacidades de la API

Proporcionar endpoints autenticados para que `Gerente` y `SuperUsuario` puedan
listar, crear y revocar accesos de directores a proyectos. Proporcionar un endpoint
para que un director cree integrantes de equipo, exigiendo el área de destino y
realizando la creación atómica.

Usar DTOs explícitos. No exponer hashes de contraseñas ni grafos de navegación de
persistencia. Usar cuerpos JSON de error consistentes que contengan un campo
`message`.

## Verificación y documentación

Generar y revisar la migración de EF Core. No aplicarla a una base de datos real o
compartida sin autorización expresa del usuario.

Agregar pruebas de integración o de servicios que cubran como mínimo:

- acceso completo para `Gerente` y `SuperUsuario`;
- rechazo de un director sin acceso al proyecto;
- rechazo dentro de un proyecto autorizado cuando el área no pertenece al
  director;
- creación dentro de un proyecto autorizado y un área dirigida;
- asignación a un integrante del área correcta;
- rechazo de asignaciones fuera del equipo;
- creación atómica de usuario y pertenencia por parte del director;
- rechazo de roles privilegiados creados por un director;
- varios directores en un proyecto y varios directores en un área;
- comportamiento correcto de `401` y `403`;
- filtrado antes de paginar y protección de endpoints de detalle.

Después de implementar, actualizar o crear una guía de frontend en texto plano
dentro de `AgencyFlow/docs/`. Incluir endpoints, JSON de solicitudes y respuestas,
token Bearer requerido, comportamiento por rol, filtros, códigos de estado y flujos
recomendados para la interfaz. Mantener
`AgencyFlow/docs/FRONTEND_DEPARTMENT_DIRECTORS.txt` consistente con la API final.

Antes de editar, inspeccionar el árbol de trabajo y conservar cambios ajenos del
usuario. Compilar y ejecutar las pruebas relevantes al finalizar. Informar las
advertencias preexistentes por separado de los errores causados por la
implementación.
