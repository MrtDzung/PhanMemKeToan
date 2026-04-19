using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDILookupPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed DI Lookup permissions: Currencies
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Currencies.View', 'Xem danh mục tiền tệ', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Currencies.Manage', 'Quản lý danh mục tiền tệ', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Seed DI Lookup permissions: Units
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Units.View', 'Xem danh mục đơn vị tính', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Units.Manage', 'Quản lý danh mục đơn vị tính', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Seed DI Lookup permissions: Warehouses
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Warehouses.View', 'Xem danh mục kho', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Warehouses.Manage', 'Quản lý danh mục kho', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Seed DI Lookup permissions: Departments
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Departments.View', 'Xem danh mục bộ phận', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.Departments.Manage', 'Quản lý danh mục bộ phận', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Seed DI Lookup permissions: ExpenseItems
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.ExpenseItems.View', 'Xem danh mục khoản mục chi phí', 'DI')
                ON CONFLICT (code) DO NOTHING;");
            migrationBuilder.Sql(@"
                INSERT INTO sys_permissions (id, code, name, module_code)
                VALUES (gen_random_uuid(), 'DI.ExpenseItems.Manage', 'Quản lý danh mục khoản mục chi phí', 'DI')
                ON CONFLICT (code) DO NOTHING;");

            // Assign all 10 permissions to SuperAdmin role
            migrationBuilder.Sql(@"
                INSERT INTO sys_role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM sys_roles r
                CROSS JOIN sys_permissions p
                WHERE r.name = 'SuperAdmin'
                  AND p.code IN (
                      'DI.Currencies.View', 'DI.Currencies.Manage',
                      'DI.Units.View', 'DI.Units.Manage',
                      'DI.Warehouses.View', 'DI.Warehouses.Manage',
                      'DI.Departments.View', 'DI.Departments.Manage',
                      'DI.ExpenseItems.View', 'DI.ExpenseItems.Manage'
                  )
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
                    WHERE code IN (
                        'DI.Currencies.View', 'DI.Currencies.Manage',
                        'DI.Units.View', 'DI.Units.Manage',
                        'DI.Warehouses.View', 'DI.Warehouses.Manage',
                        'DI.Departments.View', 'DI.Departments.Manage',
                        'DI.ExpenseItems.View', 'DI.ExpenseItems.Manage'
                    )
                );");
            migrationBuilder.Sql(@"
                DELETE FROM sys_permissions
                WHERE code IN (
                    'DI.Currencies.View', 'DI.Currencies.Manage',
                    'DI.Units.View', 'DI.Units.Manage',
                    'DI.Warehouses.View', 'DI.Warehouses.Manage',
                    'DI.Departments.View', 'DI.Departments.Manage',
                    'DI.ExpenseItems.View', 'DI.ExpenseItems.Manage'
                );");
        }
    }
}
