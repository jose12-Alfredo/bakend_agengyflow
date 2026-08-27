using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSubTaskWorkflowStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "SubTasks"
                SET "Status" = CASE
                    WHEN "Status" IN ('Activo', 'Atrasada') THEN 'Pendiente'
                    WHEN "Status" = 'En curso' THEN 'En proceso'
                    WHEN "Status" = 'Completado' THEN 'Completada'
                    WHEN "Status" = 'Anulada' THEN 'Pausada'
                    ELSE "Status"
                END
                WHERE "Status" IN (
                    'Activo',
                    'Atrasada',
                    'En curso',
                    'Completado',
                    'Anulada'
                );
                """);

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999902"),
                column: "Status",
                value: "Pendiente");

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999903"),
                column: "Status",
                value: "Pendiente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999902"),
                column: "Status",
                value: "Activo");

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999903"),
                column: "Status",
                value: "Activo");
        }
    }
}
