using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDIAccountsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var permissions = new (string Code, string Name, string ModuleCode)[]
            {
                ("DI.Accounts.View",   "Xem danh mục tài khoản",    "DI"),
                ("DI.Accounts.Manage", "Quản lý danh mục tài khoản", "DI"),
                ("DI.Accounts.Import", "Nhập khẩu hệ thống tài khoản", "DI"),
            };

            foreach (var (code, name, moduleCode) in permissions)
            {
                migrationBuilder.InsertData(
                    table: "sys_permissions",
                    columns: ["id", "code", "name", "module_code"],
                    values: new object[] { Guid.NewGuid(), code, name, moduleCode });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sys_permissions WHERE code IN ('DI.Accounts.View', 'DI.Accounts.Manage', 'DI.Accounts.Import')");
        }
    }
}
