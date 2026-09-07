# Current backend context

## Dashboard contract

`GET /api/dashboard` accepts the optional query parameters:

- `clientCompanyId`
- `projectId`
- `subProjectId`
- `departmentId`
- `responsibleUserId`

The implementation is in `AgencyFlow/Controllers/DashboardController.cs` and
`AgencyFlow/Services/DashboardService.cs`. A frontend warning about an old
contract usually indicates that a previous backend process is still running,
not a `ProjectTypesController` issue.

## Productivity delay report

`GET /api/productivity/delays` is protected by JWT and accepts the same five
filters. Its files are:

- `AgencyFlow/Controllers/ProductivityController.cs`
- `AgencyFlow/Services/ProductivityService.cs`
- `AgencyFlow/DTOs/Productivity/DelayReportDto.cs`

The response returns a `summary` plus flat hierarchical `items` at the levels
`Project`, `SubProject`, `Task`, and `SubTask`.

Delay is `max(0, reference date - planned end date)`. For open items, the
reference date is today in UTC. For completed tasks/subtasks it is the final
matching status-history timestamp, with `UpdatedAt` as a fallback. Project and
subproject dates are inferred from children or fall back to `UpdatedAt` because
there is no dedicated status history for those models. The returned
`completionSource` tells consumers which source was used.

## Local startup

From the repository root:

```powershell
dotnet build .\AgencyFlow.sln --no-restore
dotnet run --project .\AgencyFlow\AgencyFlow.csproj --launch-profile http
```

Then inspect `http://localhost:5155/swagger/v1/swagger.json` to verify the
active contract. Secrets are configured with .NET User Secrets; see
`AgencyFlow/README.md`.
