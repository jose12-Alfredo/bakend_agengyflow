using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentDirectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDirector",
                table: "DepartmentUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"),
                column: "IsDirector",
                value: true);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"),
                column: "IsDirector",
                value: true);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03"),
                column: "IsDirector",
                value: true);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04"),
                column: "IsDirector",
                value: false);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05"),
                column: "IsDirector",
                value: false);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06"),
                column: "IsDirector",
                value: false);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa07"),
                column: "IsDirector",
                value: false);

            migrationBuilder.UpdateData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa08"),
                column: "IsDirector",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDirector",
                table: "DepartmentUsers");
        }
    }
}
