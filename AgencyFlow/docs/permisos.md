---
name: agencyflow-role-permissions
description: Implementa o revisa la autorización del backend de AgencyFlow para proyectos, áreas, directores, equipos, subproyectos, tareas y subtareas. Usar al cambiar permisos por rol, accesos a proyectos, capacidades de directores o asignaciones limitadas por equipo.
---

# Permisos por rol de AgencyFlow

Implementar la autorización en el backend. La visibilidad del frontend mejora la experiencia, pero nunca constituye una barrera de seguridad.

## Antes de editar

- Inspeccionar el árbol de trabajo y la arquitectura existente.
- Conservar cambios ajenos y evitar archivos no relacionados.
- Identificar los roles, claims, entidades, filtros, paginación y convenciones de respuestas ya existentes antes de diseñar cambios.
- No aplicar migraciones a una base real o compartida sin autorización expresa.

## Reglas de acceso

- `Gerente` y `SuperUsuario` tienen lectura y escritura completas.
- Solo esos roles conceden o revocan accesos a proyectos y administran sin restricciones usuarios, roles, áreas y clientes.
- Un `Director` accede únicamente a proyectos autorizados expresamente.
- El acceso al proyecto y la dirección de un área son requisitos independientes.
- En un proyecto autorizado, el director puede ver los datos generales, pero solo ve y administra subproyectos, tareas y subtareas pertenecientes a áreas donde tenga un `DepartmentUser` activo con `IsDirector == true`.
- El director solo crea subproyectos en áreas que dirige y dentro de proyectos autorizados.
- El director solo asigna tareas o subtareas a usuarios activos, no clientes, con pertenencia activa al área del subproyecto.
- El director solo crea integrantes con roles `Operativo 1`, `Operativo 2` o `Pasante`, dentro de un área que dirige.
- El director nunca crea cuentas `SuperUsuario`, `Gerente`, `Director` o `Cliente`.

Responder `401 Unauthorized` cuando falte autenticación válida y `403 Forbidden` cuando exista autenticación pero falte permiso. No presentar fallos de autorización como validación ni sustituir indiscriminadamente `403` por `404`, salvo una política explícita para ocultar recursos sensibles.

Formato recomendado para `403`: