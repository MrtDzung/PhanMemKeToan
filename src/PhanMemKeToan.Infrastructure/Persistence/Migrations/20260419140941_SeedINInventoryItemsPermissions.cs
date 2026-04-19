using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedINInventoryItemsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed IN module — InventoryItems permissions
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'IN.InventoryItems.View', 'Xem danh mục hàng tồn kho', 'IN')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'IN.InventoryItems.Manage', 'Quản lý danh mục hàng tồn kho', 'IN')
                ON CONFLICT (code) DO NOTHING;");

            // Assign both permissions to SuperAdmin role
            migrationBuilder.Sql(@"
                INSERT INTO sys_role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM sys_roles r
                CROSS JOIN sys_permissions p
                WHERE r.name = 'SuperAdmin'
                  AND p.code IN ('IN.InventoryItems.View', 'IN.InventoryItems.Manage')
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
                WHERE permission_id IN (
                    SELECT id FROM sys_permissions
                    WHERE code IN ('IN.InventoryItems.View', 'IN.InventoryItems.Manage')
                );");
            migrationBuilder.Sql(@"
                DELETE FROM sys_permissions
                WHERE code IN ('IN.InventoryItems.View', 'IN.InventoryItems.Manage');");
        }
    }
}
