using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "text", nullable: true),
                    to_status = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_task_status_history_SubTasks_task_id",
                        column: x => x.task_id,
                        principalTable: "SubTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_status_history_task_id_timestamp",
                table: "task_status_history",
                columns: new[] { "task_id", "timestamp" });

            migrationBuilder.Sql(
                """
                INSERT INTO task_status_history
                    (id, task_id, from_status, to_status, timestamp)
                SELECT
                    md5("Id"::text || '-initial-status-history')::uuid,
                    "Id",
                    NULL,
                    "Status",
                    COALESCE("UpdatedAt", "CreatedAt", NOW())
                FROM "SubTasks"
                WHERE "DeletedAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_status_history");
        }
    }
}
