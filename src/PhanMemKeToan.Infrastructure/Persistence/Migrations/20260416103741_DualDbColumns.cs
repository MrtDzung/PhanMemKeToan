using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Snapshot-only migration. Actual schema changes are applied by MasterDbContext InitMasterDb migration.
    /// This keeps ApplicationDbContext's model snapshot in sync with the shared database schema.
    /// </summary>
    public partial class DualDbColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: columns added by MasterDbContext InitMasterDb migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: columns removed by MasterDbContext InitMasterDb migration
        }
    }
}
