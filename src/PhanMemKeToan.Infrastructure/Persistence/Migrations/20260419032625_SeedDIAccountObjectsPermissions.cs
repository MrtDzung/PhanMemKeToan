using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDIAccountObjectsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed DI.AccountObjects permissions
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.AccountObjects.View', 'Xem đối tượng kế toán', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.AccountObjects.Manage', 'Quản lý đối tượng kế toán', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Assign to SuperAdmin role
            migrationBuilder.Sql(@"
                INSERT INTO sys_role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM sys_roles r
                CROSS JOIN sys_permissions p
                WHERE r.name = 'SuperAdmin'
                  AND p.code IN ('DI.AccountObjects.View', 'DI.AccountObjects.Manage')
                  AND NOT EXISTS (
                      SELECT 1 FROM sys_role_permissions rp2
                      WHERE rp2.role_id = r.id AND rp2.permission_id = p.id
                  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM sys_role_permissions
                WHERE permission_id IN (SELECT id FROM sys_permissions WHERE code IN ('DI.AccountObjects.View', 'DI.AccountObjects.Manage'));");
            migrationBuilder.Sql("DELETE FROM sys_permissions WHERE code IN ('DI.AccountObjects.View', 'DI.AccountObjects.Manage');");
        }
    }
}
