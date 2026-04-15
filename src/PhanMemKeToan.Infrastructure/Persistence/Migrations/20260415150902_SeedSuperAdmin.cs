using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var tenantId = new Guid("00000000-0000-0000-0000-000000000001");
            var userId   = new Guid("00000000-0000-0000-0000-000000000002");
            var roleId   = new Guid("00000000-0000-0000-0000-000000000003");
            var now      = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            // Pre-computed BCrypt hash of "Admin@123456" cost=12 to avoid runtime BCrypt dependency in migration
            var passwordHash = "$2a$12$LQHpuXWfGbZwFPDrMjGnJ.YC2kkmTlfF5V4GcmfF7m1YTB6VJ5Xmm";

            migrationBuilder.InsertData(
                table: "sys_tenants",
                columns: ["id", "code", "name", "is_active", "created_at"],
                values: new object[] { tenantId, "SYSTEM", "System Administration", true, now });

            migrationBuilder.InsertData(
                table: "sys_users",
                columns: ["id", "tenant_id", "email", "password_hash", "full_name",
                          "is_active", "failed_login_count", "is_deleted", "created_at", "created_by"],
                values: new object[] { userId, tenantId, "superadmin@system.local",
                                       passwordHash, "Super Administrator",
                                       true, 0, false, now, "SYSTEM" });

            migrationBuilder.InsertData(
                table: "sys_roles",
                columns: ["id", "tenant_id", "name", "description", "is_deleted", "created_at", "created_by"],
                values: new object[] { roleId, tenantId, "SuperAdmin", "Full system access", false, now, "SYSTEM" });

            migrationBuilder.Sql($"""
                INSERT INTO sys_role_permissions (role_id, permission_id)
                SELECT '{roleId}', id FROM sys_permissions;
                """);

            migrationBuilder.InsertData(
                table: "sys_user_roles",
                columns: ["user_id", "role_id"],
                values: new object[] { userId, roleId });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tenantId = new Guid("00000000-0000-0000-0000-000000000001");
            var userId   = new Guid("00000000-0000-0000-0000-000000000002");
            var roleId   = new Guid("00000000-0000-0000-0000-000000000003");

            migrationBuilder.Sql($"DELETE FROM sys_user_roles WHERE user_id = '{userId}'");
            migrationBuilder.Sql($"DELETE FROM sys_role_permissions WHERE role_id = '{roleId}'");
            migrationBuilder.Sql($"DELETE FROM sys_roles WHERE id = '{roleId}'");
            migrationBuilder.Sql($"DELETE FROM sys_users WHERE id = '{userId}'");
            migrationBuilder.Sql($"DELETE FROM sys_tenants WHERE id = '{tenantId}'");
        }
    }
}
