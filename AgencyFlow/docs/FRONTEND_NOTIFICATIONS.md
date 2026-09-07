# Integración frontend de notificaciones

Todos los endpoints requieren `Authorization: Bearer <token>` y admiten los roles
internos `SuperUsuario`, `Gerente`, `Director`, `Operativo 1`, `Operativo 2` y
`Pasante`. La API nunca acepta un `userId`: usa la identidad del JWT.

## Endpoints

- `GET /api/notifications?page=1&pageSize=20&unreadOnly=false`: bandeja propia.
- `GET /api/notifications/summary`: devuelve `{ "unreadCount": 3 }`.
- `PATCH /api/notifications/{id}/read`: marca un aviso propio; `204`, `404` si no pertenece al usuario.
- `PATCH /api/notifications/read-all`: marca toda la bandeja propia; `204`.

Cada elemento contiene `id`, `type`, `title`, `message`, `resourceType`,
`resourceId`, `url`, `createdAt`, `readAt` e `isRead`. Los tipos iniciales son
`assignment`, `comment`, `status`, `review`, `deadline` y `overdue`.

La campana consulta el resumen cada 30 segundos y carga los últimos 20 elementos
al abrirse. Los operativos navegan a **Mi trabajo**; los gestores navegan a las
listas administrativas. Los estados esperados son `200`/`204`, `401` sin sesión y
`404` al intentar modificar una notificación inexistente o ajena.
