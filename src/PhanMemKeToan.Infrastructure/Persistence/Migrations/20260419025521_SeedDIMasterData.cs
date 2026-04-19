using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDIMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed VND currency for each tenant (idempotent)
            migrationBuilder.Sql(@"
                INSERT INTO ""Currencies"" (id, currency_code, currency_name, currency_name_english, symbol, exchange_rate, is_active, tenant_id, created_at, created_by, is_deleted)
                SELECT gen_random_uuid(), 'VND', N'Đồng Việt Nam', 'Vietnamese Dong', '₫', 1.00, true, t.id, NOW(), 'SYSTEM', false
                FROM sys_tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Currencies"" c
                    WHERE c.currency_code = 'VND' AND c.tenant_id = t.id AND c.is_deleted = false
                );
            ");

            // Seed 10 standard Vietnamese units for each tenant (idempotent)
            migrationBuilder.Sql(@"
                INSERT INTO ""Units"" (id, unit_code, unit_name, is_active, tenant_id, created_at, created_by, is_deleted)
                SELECT gen_random_uuid(), u.code, u.name, true, t.id, NOW(), 'SYSTEM', false
                FROM sys_tenants t
                CROSS JOIN (VALUES
                    ('CAI',    N'Cái'),
                    ('CHIEC',  N'Chiếc'),
                    ('HOP',    N'Hộp'),
                    ('KG',     N'Kg'),
                    ('LIT',    N'Lít'),
                    ('M2',     N'M²'),
                    ('M3',     N'M³'),
                    ('THUNG',  N'Thùng'),
                    ('BO',     N'Bộ'),
                    ('DOI',    N'Đôi')
                ) AS u(code, name)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Units"" un
                    WHERE un.unit_code = u.code AND un.tenant_id = t.id AND un.is_deleted = false
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Units""
                WHERE unit_code IN ('CAI','CHIEC','HOP','KG','LIT','M2','M3','THUNG','BO','DOI')
                  AND created_by = 'SYSTEM';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""Currencies""
                WHERE currency_code = 'VND' AND created_by = 'SYSTEM';
            ");
        }
    }
}
