using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgencyFlow.Migrations;

public partial class AddManagementDashboardActivity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastActivityAt", table: "TaskItems", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "OwnProgressPercentage", table: "TaskItems", type: "integer", nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "LastActivityAt", table: "SubTasks", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "RepresentativeUserId", table: "ClientCompanies", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_ClientCompanies_RepresentativeUserId", table: "ClientCompanies", column: "RepresentativeUserId");
        migrationBuilder.AddForeignKey(name: "FK_ClientCompanies_Users_RepresentativeUserId", table: "ClientCompanies", column: "RepresentativeUserId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

        migrationBuilder.Sql("""
            UPDATE "TaskItems" SET "LastActivityAt" = COALESCE("UpdatedAt", "CreatedAt") WHERE "LastActivityAt" IS NULL;
            UPDATE "SubTasks" SET "LastActivityAt" = COALESCE("UpdatedAt", "CreatedAt") WHERE "LastActivityAt" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_ClientCompanies_Users_RepresentativeUserId", table: "ClientCompanies");
        migrationBuilder.DropIndex(name: "IX_ClientCompanies_RepresentativeUserId", table: "ClientCompanies");
        migrationBuilder.DropColumn(name: "RepresentativeUserId", table: "ClientCompanies");
        migrationBuilder.DropColumn(name: "LastActivityAt", table: "TaskItems");
        migrationBuilder.DropColumn(name: "OwnProgressPercentage", table: "TaskItems");
        migrationBuilder.DropColumn(name: "LastActivityAt", table: "SubTasks");
    }
}
