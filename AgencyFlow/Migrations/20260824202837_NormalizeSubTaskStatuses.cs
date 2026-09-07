using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSubTaskStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "SubTasks"
                SET "Status" = CASE
                    WHEN "Status" = 'Completado' THEN 'Completada'
                    WHEN "Status" IN ('Pendiente', 'En curso') THEN 'Activo'
                    ELSE "Status"
                END
                WHERE "Status" IN ('Completado', 'Pendiente', 'En curso');
                """);

            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999901"),
                column: "Status",
                value: "Completada");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999901"),
                column: "Status",
                value: "Completado");

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
    }
}
