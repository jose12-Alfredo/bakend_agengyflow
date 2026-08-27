using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class SeparateTaskAndSubTaskStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_status_history_SubTasks_task_id",
                table: "task_status_history");

            migrationBuilder.CreateTable(
                name: "subtask_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtask_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "text", nullable: true),
                    to_status = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subtask_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_subtask_status_history_SubTasks_subtask_id",
                        column: x => x.subtask_id,
                        principalTable: "SubTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subtask_status_history_subtask_id_timestamp",
                table: "subtask_status_history",
                columns: new[] { "subtask_id", "timestamp" });

            migrationBuilder.Sql(
                """
                INSERT INTO subtask_status_history
                    (id, subtask_id, from_status, to_status, timestamp)
                SELECT id, task_id, from_status, to_status, timestamp
                FROM task_status_history;

                DELETE FROM task_status_history;

                INSERT INTO task_status_history
                    (id, task_id, from_status, to_status, timestamp)
                SELECT
                    md5("Id"::text || '-initial-task-status-history')::uuid,
                    "Id",
                    NULL,
                    "Status",
                    COALESCE("UpdatedAt", "CreatedAt", NOW())
                FROM "TaskItems"
                WHERE "DeletedAt" IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_task_status_history_TaskItems_task_id",
                table: "task_status_history",
                column: "task_id",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_status_history_TaskItems_task_id",
                table: "task_status_history");

            migrationBuilder.Sql(
                """
                DELETE FROM task_status_history;

                INSERT INTO task_status_history
                    (id, task_id, from_status, to_status, timestamp)
                SELECT id, subtask_id, from_status, to_status, timestamp
                FROM subtask_status_history;
                """);

            migrationBuilder.DropTable(
                name: "subtask_status_history");

            migrationBuilder.AddForeignKey(
                name: "FK_task_status_history_SubTasks_task_id",
                table: "task_status_history",
                column: "task_id",
                principalTable: "SubTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
