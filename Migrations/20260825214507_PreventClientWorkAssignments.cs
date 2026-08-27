using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class PreventClientWorkAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "TaskItems" AS task
                SET "AssignedUserId" = NULL,
                    "UpdatedAt" = NOW()
                FROM "Users" AS app_user
                INNER JOIN "Roles" AS role ON role."Id" = app_user."RoleId"
                WHERE task."AssignedUserId" = app_user."Id"
                  AND role."Name" = 'Cliente';

                UPDATE "SubTasks" AS subtask
                SET "AssignedUserId" = NULL,
                    "UpdatedAt" = NOW()
                FROM "Users" AS app_user
                INNER JOIN "Roles" AS role ON role."Id" = app_user."RoleId"
                WHERE subtask."AssignedUserId" = app_user."Id"
                  AND role."Name" = 'Cliente';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Previous assignees cannot be restored without risking incorrect data.
        }
    }
}
