using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixDepartmentUserUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DepartmentUsers_UserId_DepartmentId",
                table: "DepartmentUsers");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentUsers_UserId_DepartmentId",
                table: "DepartmentUsers",
                columns: new[] { "UserId", "DepartmentId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DepartmentUsers_UserId_DepartmentId",
                table: "DepartmentUsers");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentUsers_UserId_DepartmentId",
                table: "DepartmentUsers",
                columns: new[] { "UserId", "DepartmentId" },
                unique: true);
        }
    }
}
