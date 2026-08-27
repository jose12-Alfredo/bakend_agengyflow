using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixTaskItemInReviewEncoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "TaskItems"
                SET "Status" = 'En revisión'
                WHERE "Status" LIKE 'En revisi%'
                  AND "Status" <> 'En revisión';

                UPDATE task_status_history
                SET from_status = 'En revisión'
                WHERE from_status LIKE 'En revisi%'
                  AND from_status <> 'En revisión';

                UPDATE task_status_history
                SET to_status = 'En revisión'
                WHERE to_status LIKE 'En revisi%'
                  AND to_status <> 'En revisión';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Normalized text is intentionally preserved when rolling back.
        }
    }
}
