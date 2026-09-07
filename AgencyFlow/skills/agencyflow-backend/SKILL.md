---
name: agencyflow-backend
description: Maintain the AgencyFlow ASP.NET backend, including API contracts, dashboard filters, productivity-delay reports, and local verification. Use when changing or diagnosing this repository's backend; do not use for unrelated frontend-only work.
---

# AgencyFlow Backend

Use this skill for changes and diagnosis inside the AgencyFlow backend project.

## Project orientation

- Solution: `AgencyFlow.sln`.
- Web project: `AgencyFlow/AgencyFlow.csproj`.
- Development API: `http://localhost:5155`; Swagger:
  `http://localhost:5155/swagger`.
- The API uses JWT. Only `POST /api/users/login` is public.
- Service registration is centralized in `AgencyFlow/Program.cs`.

Read [references/current-context.md](references/current-context.md) before
working on the dashboard, productivity reporting, or a backend process that
appears out of date. For broader domain and endpoint context, consult
`AgencyFlow/docs/BACKEND_HANDOFF.md` only when it is relevant to the task.

## Working conventions

- Keep controllers thin; put data access and business calculations in services.
- Preserve logical-delete filtering (`DeletedAt == null`) when adding queries.
- Reuse query filters for client, project, subproject, department and
  responsible user when a report should follow dashboard context.
- Add a migration only when changing the database schema, not for DTO,
  controller, or service-only work.
- Build before handoff with `dotnet build AgencyFlow.sln --no-restore`.

## Verification and local process state

If the active backend is stale or a build cannot copy `AgencyFlow.exe`, first
confirm whether an older backend process is locking the Debug binary. Stop or
restart only when the user request authorizes it. Do not rely on a previous
chat's PID.

After a restart, validate the running API through its Swagger document before
claiming that a route or its query contract is active. Run authenticated report
requests only with credentials or tokens already available in the task context;
never invent, expose, or persist credentials.

## Reporting semantics

For delay/productivity work, distinguish planned end date from actual
completion. Task and subtask completion can use status history. Project and
subproject completion is inferred from child elements because those entities do
not currently keep their own status history; preserve an explicit source field
when presenting inferred completion dates.
