using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCycleTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartedAt",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "task_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartedAt",
                table: "SubTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "SubTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "subtask_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartedAt",
                table: "SubProjects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "SubProjects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "Pendiente");

            migrationBuilder.CreateTable(
                name: "project_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: true),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_status_history_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subproject_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "text", nullable: true),
                    ToStatus = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subproject_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subproject_status_history_SubProjects_SubProjectId",
                        column: x => x.SubProjectId,
                        principalTable: "SubProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666601"),
                columns: new[] { "ActualStartedAt", "CompletedAt", "Status" },
                values: new object[] { null, null, "Pendiente" });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666602"),
                columns: new[] { "ActualStartedAt", "CompletedAt", "Status" },
                values: new object[] { null, null, "Pendiente" });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666603"),
                columns: new[] { "ActualStartedAt", "CompletedAt", "Status" },
                values: new object[] { null, null, "Pendiente" });

            migrationBuilder.UpdateData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777701"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777702"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777703"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999901"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999902"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999903"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888801"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888802"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888803"),
                columns: new[] { "ActualStartedAt", "CompletedAt" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_project_status_history_ProjectId_Timestamp",
                table: "project_status_history",
                columns: new[] { "ProjectId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_subproject_status_history_SubProjectId_Timestamp",
                table: "subproject_status_history",
                columns: new[] { "SubProjectId", "Timestamp" });

            // Reconstrucción conservadora para registros anteriores. Las marcas
            // provenientes de historiales son exactas; los niveles superiores se
            // infieren desde sus descendientes y el reporte los identifica así.
            migrationBuilder.Sql("""
                UPDATE "SubTasks" s
                SET "ActualStartedAt" = h.started_at
                FROM (
                    SELECT subtask_id, MIN(timestamp) AS started_at
                    FROM subtask_status_history
                    WHERE to_status = 'En proceso'
                    GROUP BY subtask_id
                ) h WHERE h.subtask_id = s."Id";

                UPDATE "SubTasks" s
                SET "CompletedAt" = h.completed_at
                FROM (
                    SELECT subtask_id, MAX(timestamp) AS completed_at
                    FROM subtask_status_history
                    WHERE to_status IN ('Completada', 'Completado')
                    GROUP BY subtask_id
                ) h WHERE h.subtask_id = s."Id"
                    AND s."Status" IN ('Completada', 'Completado');

                UPDATE "TaskItems" t
                SET "ActualStartedAt" = h.started_at
                FROM (
                    SELECT task_id, MIN(timestamp) AS started_at
                    FROM task_status_history
                    WHERE to_status IN ('En curso', 'En proceso')
                    GROUP BY task_id
                ) h WHERE h.task_id = t."Id";

                UPDATE "TaskItems" t
                SET "CompletedAt" = h.completed_at
                FROM (
                    SELECT task_id, MAX(timestamp) AS completed_at
                    FROM task_status_history
                    WHERE to_status IN ('Completado', 'Completada')
                    GROUP BY task_id
                ) h WHERE h.task_id = t."Id"
                    AND t."Status" IN ('Completado', 'Completada');

                UPDATE "SubProjects" sp SET
                    "ActualStartedAt" = x.started_at,
                    "CompletedAt" = CASE WHEN sp."Status" IN ('Completado', 'Completada')
                        THEN x.completed_at ELSE NULL END
                FROM (
                    SELECT "SubProjectId", MIN("ActualStartedAt") AS started_at,
                           MAX("CompletedAt") AS completed_at
                    FROM "TaskItems" GROUP BY "SubProjectId"
                ) x WHERE x."SubProjectId" = sp."Id";

                UPDATE "Projects" p SET
                    "Status" = CASE
                        WHEN EXISTS (SELECT 1 FROM "SubProjects" sp WHERE sp."ProjectId" = p."Id" AND sp."DeletedAt" IS NULL)
                         AND NOT EXISTS (SELECT 1 FROM "SubProjects" sp WHERE sp."ProjectId" = p."Id" AND sp."DeletedAt" IS NULL AND sp."Status" NOT IN ('Completado','Completada'))
                        THEN 'Completado'
                        WHEN EXISTS (SELECT 1 FROM "SubProjects" sp WHERE sp."ProjectId" = p."Id" AND sp."DeletedAt" IS NULL AND sp."Status" <> 'Pendiente')
                        THEN 'En curso' ELSE 'Pendiente' END,
                    "ActualStartedAt" = x.started_at,
                    "CompletedAt" = CASE WHEN x.all_completed THEN x.completed_at ELSE NULL END
                FROM (
                    SELECT "ProjectId", MIN("ActualStartedAt") AS started_at,
                           MAX("CompletedAt") AS completed_at,
                           BOOL_AND("Status" IN ('Completado','Completada')) AS all_completed
                    FROM "SubProjects" WHERE "DeletedAt" IS NULL GROUP BY "ProjectId"
                ) x WHERE x."ProjectId" = p."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_status_history");

            migrationBuilder.DropTable(
                name: "subproject_status_history");

            migrationBuilder.DropColumn(
                name: "ActualStartedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "task_status_history");

            migrationBuilder.DropColumn(
                name: "ActualStartedAt",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "subtask_status_history");

            migrationBuilder.DropColumn(
                name: "ActualStartedAt",
                table: "SubProjects");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "SubProjects");

            migrationBuilder.DropColumn(
                name: "ActualStartedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");
        }
    }
}
