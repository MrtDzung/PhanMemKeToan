using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var permissions = new (string Code, string Name, string ModuleCode)[]
            {
                ("SYS.Users.View",        "Xem người dùng",             "SYS"),
                ("SYS.Users.Manage",      "Quản lý người dùng",         "SYS"),
                ("SYS.Roles.View",        "Xem vai trò",                "SYS"),
                ("SYS.Roles.Manage",      "Quản lý vai trò",            "SYS"),
                ("SYS.Permissions.Manage","Quản lý quyền",              "SYS"),
                ("GL.Vouchers.View",      "Xem chứng từ GL",            "GL"),
                ("GL.Vouchers.Manage",    "Quản lý chứng từ GL",        "GL"),
                ("GL.Vouchers.Post",      "Ghi sổ chứng từ GL",         "GL"),
                ("GL.Reports.View",       "Xem báo cáo GL",             "GL"),
                ("CA.Receipts.View",      "Xem phiếu thu",              "CA"),
                ("CA.Receipts.Manage",    "Quản lý phiếu thu",          "CA"),
                ("CA.Receipts.Post",      "Ghi sổ phiếu thu",           "CA"),
                ("CA.Payments.View",      "Xem phiếu chi",              "CA"),
                ("CA.Payments.Manage",    "Quản lý phiếu chi",          "CA"),
                ("CA.Payments.Post",      "Ghi sổ phiếu chi",           "CA"),
                ("BA.Receipts.View",      "Xem báo nợ ngân hàng",       "BA"),
                ("BA.Receipts.Manage",    "Quản lý báo nợ ngân hàng",   "BA"),
                ("BA.Payments.View",      "Xem báo có ngân hàng",       "BA"),
                ("BA.Payments.Manage",    "Quản lý báo có ngân hàng",   "BA"),
                ("BA.Payments.Post",      "Ghi sổ ngân hàng",           "BA"),
                ("PU.Orders.View",        "Xem đơn mua hàng",           "PU"),
                ("PU.Orders.Manage",      "Quản lý đơn mua hàng",       "PU"),
                ("PU.Invoices.View",      "Xem hóa đơn mua",            "PU"),
                ("PU.Invoices.Manage",    "Quản lý hóa đơn mua",        "PU"),
                ("PU.Invoices.Post",      "Ghi sổ hóa đơn mua",         "PU"),
                ("SA.Orders.View",        "Xem đơn bán hàng",           "SA"),
                ("SA.Orders.Manage",      "Quản lý đơn bán hàng",       "SA"),
                ("SA.Invoices.View",      "Xem hóa đơn bán",            "SA"),
                ("SA.Invoices.Manage",    "Quản lý hóa đơn bán",        "SA"),
                ("SA.Invoices.Post",      "Ghi sổ hóa đơn bán",         "SA"),
                ("IN.Products.View",      "Xem hàng tồn kho",           "IN"),
                ("IN.Products.Manage",    "Quản lý hàng tồn kho",       "IN"),
                ("IN.Movements.View",     "Xem xuất nhập kho",          "IN"),
                ("IN.Movements.Manage",   "Quản lý xuất nhập kho",      "IN"),
                ("IN.Reports.View",       "Xem báo cáo kho",            "IN"),
                ("FA.Assets.View",        "Xem tài sản cố định",         "FA"),
                ("FA.Assets.Manage",      "Quản lý tài sản cố định",     "FA"),
                ("FA.Depreciation.Run",   "Chạy khấu hao",               "FA"),
                ("FA.Reports.View",       "Xem báo cáo TSCĐ",            "FA"),
                ("SU.Suppliers.View",     "Xem nhà cung cấp",            "SU"),
                ("SU.Suppliers.Manage",   "Quản lý nhà cung cấp",        "SU"),
                ("SU.Reports.View",       "Xem báo cáo NCC",             "SU"),
                ("JC.Jobs.View",          "Xem công trình",              "JC"),
                ("JC.Jobs.Manage",        "Quản lý công trình",          "JC"),
                ("JC.Reports.View",       "Xem báo cáo công trình",      "JC"),
                ("PA.Employees.View",     "Xem nhân viên",               "PA"),
                ("PA.Employees.Manage",   "Quản lý nhân viên",           "PA"),
                ("PA.Payslips.View",      "Xem phiếu lương",             "PA"),
                ("PA.Payslips.Manage",    "Quản lý phiếu lương",         "PA"),
                ("PA.Payroll.Run",        "Chạy bảng lương",             "PA"),
                ("TA.Declarations.View",  "Xem khai thuế",               "TA"),
                ("TA.Declarations.Manage","Quản lý khai thuế",           "TA"),
                ("TA.Reports.View",       "Xem báo cáo thuế",            "TA"),
                ("CT.Contracts.View",     "Xem hợp đồng",                "CT"),
                ("CT.Contracts.Manage",   "Quản lý hợp đồng",            "CT"),
                ("CT.Reports.View",       "Xem báo cáo hợp đồng",        "CT"),
                ("IP.Documents.View",     "Xem chứng từ XNK",            "IP"),
                ("IP.Documents.Manage",   "Quản lý chứng từ XNK",        "IP"),
                ("IP.Reports.View",       "Xem báo cáo XNK",             "IP"),
                ("DI.Distribution.View",  "Xem phân phối",               "DI"),
                ("DI.Distribution.Manage","Quản lý phân phối",           "DI"),
                ("DI.Reports.View",       "Xem báo cáo phân phối",       "DI"),
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
            migrationBuilder.Sql("DELETE FROM sys_permissions");
        }
    }
}
