using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignDIPermissionsToSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO sys_role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM sys_roles r
                CROSS JOIN sys_permissions p
                WHERE r.name = 'SuperAdmin'
                  AND p.code LIKE 'DI.%'
                  AND NOT EXISTS (
                      SELECT 1 FROM sys_role_permissions rp2
                      WHERE rp2.role_id = r.id AND rp2.permission_id = p.id
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM sys_role_permissions
                WHERE role_id IN (SELECT id FROM sys_roles WHERE name = 'SuperAdmin')
                  AND permission_id IN (SELECT id FROM sys_permissions WHERE code LIKE 'DI.%');
            ");
        }
    }
}
