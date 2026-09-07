---
name: permisos-del-sistema
description: Audita, documenta y diseña el catálogo de permisos de AgencyFlow por usuario, rol y alcance. Usar al inventariar capacidades, comparar permisos efectivos, diseñar el panel de permisos o implementar autorizaciones granulares.
---

# Permisos del sistema de AgencyFlow

Mantener un catálogo verificable de las capacidades del sistema y ayudar a
diseñar permisos granulares sin confundir la interfaz con la seguridad real.

## Fuente de verdad

Leer [el catálogo del sistema](<../../../AgencyFlow/docs/Permisos del sistema.md>)
cuando la tarea requiera consultar o actualizar el inventario funcional.

Antes de afirmar qué está permitido actualmente, contrastar el catálogo con:

- `AgencyFlow/Authorization/RoleNames.cs`;
- `AgencyFlow/Authorization/ResourceAuthorizationService.cs`;
- atributos `Authorize` de `AgencyFlow/Controllers/`;
- validaciones de alcance y responsable dentro de `AgencyFlow/Services/`.

La API y sus servicios son la autoridad. El frontend sirve para presentar las
capacidades, pero ocultar un botón nunca constituye autorización.

## Modos de trabajo

### Auditoría o consulta

Trabajar en modo de solo lectura cuando el usuario pida recopilar, explicar,
comparar o revisar permisos. No modificar código, migraciones, documentación ni
datos a menos que el usuario lo solicite expresamente. Diferenciar:

- permiso declarado por rol en el controlador;
- alcance aplicado por los servicios;
- operación visible en el frontend;
- comportamiento faltante, defectuoso o solamente propuesto.

### Mantenimiento del catálogo

Cuando el usuario solicite actualizar el catálogo, registrar por cada capacidad:

- código estable en minúsculas con notación `recurso.accion`;
- descripción funcional;
- roles que la reciben por defecto;
- alcance permitido: global, área, proyecto, trabajo propio o recurso asignado;
- restricciones sobre destinatarios y dependencias;
- estado: actual, propuesto o pendiente de implementación.

Separar capacidades sensibles aunque hoy compartan un endpoint. Por ejemplo,
`users.update`, `users.password.update` y `users.role.assign` deben permanecer
distintas para que el futuro panel pueda concederlas individualmente.

### Implementación

Modificar el sistema solamente cuando el usuario autorice expresamente la
implementación. Antes de editar, inspeccionar el árbol de trabajo y conservar los
cambios existentes. Aplicar los permisos en el backend y luego reflejarlos en el
frontend.

Para permisos por usuario, preferir este modelo:

```text
Permission                 catálogo estable de capacidades
RolePermission             permisos predeterminados del rol
UserPermission             concesión o denegación individual
UserPermissionScope        alcance por área o proyecto
PermissionAudit            quién cambió qué permiso y cuándo
```

Resolver los permisos efectivos como rol base más excepciones individuales,
respetando siempre el alcance del recurso. No aceptar desde el cliente la
identidad ni el rol de quien ejecuta la acción; obtenerlos del JWT.

## Reglas de seguridad

- Impedir que una persona conceda permisos que ella misma no posee.
- Reservar `permissions.manage` y la administración de SuperUsuarios para la
  autoridad que el usuario defina explícitamente.
- Validar responsables activos y nunca permitir asignaciones a clientes.
- Mantener separados el acceso a un proyecto y la autoridad sobre un área.
- Aplicar filtros de visibilidad antes de paginar.
- Hacer efectivas las revocaciones inmediatamente mediante consulta al backend o
  invalidación de caché; no depender indefinidamente de permisos guardados en un
  JWT antiguo.
- Registrar concesiones, denegaciones, cambios de alcance y revocaciones.
- Responder 401 si falta autenticación y 403 si el usuario autenticado carece del
  permiso o alcance.

## Verificación

Al implementar, cubrir al menos:

- permiso concedido y permiso denegado;
- alcance correcto e intento fuera de alcance;
- prohibición de escalamiento de privilegios;
- asignación únicamente a roles y usuarios elegibles;
- revocación inmediata;
- equivalencia entre los botones visibles y las capacidades devueltas por la API;
- auditoría de cada cambio administrativo.

Informar por separado los permisos actuales, las recomendaciones y los cambios
realmente realizados. No aplicar migraciones a una base compartida sin permiso
expreso.
