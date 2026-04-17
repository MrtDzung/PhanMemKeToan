using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    account_name_english = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    is_parent = table.Column<bool>(type: "boolean", nullable: false),
                    account_category_kind = table.Column<int>(type: "integer", nullable: false),
                    inactive = table.Column<bool>(type: "boolean", nullable: false),
                    is_postable_in_foreign_currency = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_account_object = table.Column<bool>(type: "boolean", nullable: false),
                    account_object_type = table.Column<int>(type: "integer", nullable: false),
                    detail_by_bank_account = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_job = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_project_work = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_order = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_contract = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_expense_item = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_department = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_list_item = table.Column<bool>(type: "boolean", nullable: false),
                    detail_by_pu_contract = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    misa_code_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_accounts_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountNumber_Pattern",
                table: "Accounts",
                column: "account_number");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentID",
                table: "Accounts",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId",
                table: "Accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UIX_Accounts_TenantId_AccountNumber",
                table: "Accounts",
                columns: new[] { "tenant_id", "account_number" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
