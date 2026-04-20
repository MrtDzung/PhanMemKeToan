using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class InitMasterDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create NEW table: sys_master_users
            migrationBuilder.CreateTable(
                name: "sys_master_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_master_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sys_master_users_email",
                table: "sys_master_users",
                column: "email",
                unique: true);

            // 2. Add new columns to EXISTING table: sys_tenants
            migrationBuilder.AddColumn<string>(
                name: "database_mode",
                table: "sys_tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CloudManaged");

            migrationBuilder.AddColumn<string>(
                name: "db_status",
                table: "sys_tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Online");

            migrationBuilder.AddColumn<string>(
                name: "connection_string_encrypted",
                table: "sys_tenants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cloudflare_subdomain",
                table: "sys_tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "db_host",
                table: "sys_tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // 3. Add tenant_id column to EXISTING table: sys_refresh_tokens
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sys_refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "ix_sys_refresh_tokens_user_tenant",
                table: "sys_refresh_tokens",
                columns: new[] { "user_id", "tenant_id", "is_revoked" });

            // 4. Create NEW table: sys_master_user_tenants
            migrationBuilder.CreateTable(
                name: "sys_master_user_tenants",
                columns: table => new
                {
                    master_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    display_role = table.Column<string>(type: "text", nullable: true),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_master_user_tenants", x => new { x.master_user_id, x.tenant_id });
                    table.ForeignKey(
                        name: "fk_sys_master_user_tenants_sys_master_users_master_user_id",
                        column: x => x.master_user_id,
                        principalTable: "sys_master_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sys_master_user_tenants_sys_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "sys_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sys_master_user_tenants_tenant_id",
                table: "sys_master_user_tenants",
                column: "tenant_id");

            // 5. Seed data: Copy superadmin to sys_master_users + create tenant link
            migrationBuilder.Sql(@"
                INSERT INTO sys_master_users (id, email, password_hash, full_name, is_active, failed_login_count, created_at)
                SELECT id, email, password_hash, full_name, is_active, 0, created_at
                FROM sys_users
                WHERE email = 'superadmin@system.local'
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO sys_master_user_tenants (master_user_id, tenant_id, is_default, joined_at)
                SELECT u.id, t.id, true, NOW()
                FROM sys_users u
                CROSS JOIN sys_tenants t
                WHERE u.email = 'superadmin@system.local'
                  AND t.code = 'SYSTEM'
                ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_master_user_tenants");

            migrationBuilder.DropIndex(
                name: "ix_sys_refresh_tokens_user_tenant",
                table: "sys_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sys_refresh_tokens");

            migrationBuilder.DropColumn(name: "database_mode", table: "sys_tenants");
            migrationBuilder.DropColumn(name: "db_status", table: "sys_tenants");
            migrationBuilder.DropColumn(name: "connection_string_encrypted", table: "sys_tenants");
            migrationBuilder.DropColumn(name: "cloudflare_subdomain", table: "sys_tenants");
            migrationBuilder.DropColumn(name: "db_host", table: "sys_tenants");

            migrationBuilder.Sql("DELETE FROM sys_master_users WHERE email = 'superadmin@system.local';");

            migrationBuilder.DropTable(
                name: "sys_master_users");
        }
    }
}
