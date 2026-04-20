using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAccountModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_Accounts_TenantId_AccountNumber",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "UIX_Accounts_TenantId_AccountNumber",
                table: "Accounts",
                columns: new[] { "tenant_id", "account_number" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_Accounts_TenantId_AccountNumber",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "UIX_Accounts_TenantId_AccountNumber",
                table: "Accounts",
                columns: new[] { "tenant_id", "account_number" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
