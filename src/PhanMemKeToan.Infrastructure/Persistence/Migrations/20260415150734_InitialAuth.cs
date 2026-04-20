using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    module_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    database_schema_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_sys_role_permissions_sys_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "sys_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sys_role_permissions_sys_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "sys_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_family = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_sys_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "sys_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sys_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_sys_user_roles_sys_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "sys_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sys_user_roles_sys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "sys_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sys_permissions_code",
                table: "sys_permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sys_permissions_module_code",
                table: "sys_permissions",
                column: "module_code");

            migrationBuilder.CreateIndex(
                name: "ix_sys_refresh_tokens_token_family_is_revoked",
                table: "sys_refresh_tokens",
                columns: new[] { "token_family", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_sys_refresh_tokens_token_hash",
                table: "sys_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sys_refresh_tokens_user_id",
                table: "sys_refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sys_role_permissions_permission_id",
                table: "sys_role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_sys_roles_tenant_id",
                table: "sys_roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sys_roles_tenant_id_name",
                table: "sys_roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sys_tenants_code",
                table: "sys_tenants",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sys_user_roles_role_id",
                table: "sys_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_sys_users_tenant_id",
                table: "sys_users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sys_users_tenant_id_email",
                table: "sys_users",
                columns: new[] { "tenant_id", "email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_refresh_tokens");

            migrationBuilder.DropTable(
                name: "sys_role_permissions");

            migrationBuilder.DropTable(
                name: "sys_tenants");

            migrationBuilder.DropTable(
                name: "sys_user_roles");

            migrationBuilder.DropTable(
                name: "sys_permissions");

            migrationBuilder.DropTable(
                name: "sys_roles");

            migrationBuilder.DropTable(
                name: "sys_users");
        }
    }
}
