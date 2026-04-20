using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhanMemKeToan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDIMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountObjectGroups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    group_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    object_type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_object_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    currency_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency_name_english = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    department_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                    table.ForeignKey(
                        name: "fk_departments_departments_parent_id",
                        column: x => x.parent_id,
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    expense_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemCategories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    category_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_categories_inventory_item_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "InventoryItemCategories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryQuantityFormulaTemplates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_quantity_formula_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ItemAttributeTypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    attribute_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_attribute_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    warehouse_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AccountObjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    object_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    object_name_english = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fax = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_person = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    object_type = table.Column<int>(type: "integer", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_term_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    account_object_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_objects", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_objects_account_object_groups_account_object_group_id",
                        column: x => x.account_object_group_id,
                        principalTable: "AccountObjectGroups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_code = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    item_name_english = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    barcode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    costing_method = table.Column<int>(type: "integer", nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    default_tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_stock_level = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_stock_level = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    is_follow_serial = table.Column<bool>(type: "boolean", nullable: false),
                    is_follow_lot = table.Column<bool>(type: "boolean", nullable: false),
                    is_follow_expiry = table.Column<bool>(type: "boolean", nullable: false),
                    is_panel_item = table.Column<bool>(type: "boolean", nullable: false),
                    panel_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    formula_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sale_price1 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sale_price2 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sale_price3 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_items_inventory_item_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "InventoryItemCategories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_inventory_items_inventory_quantity_formula_templates_formula",
                        column: x => x.formula_template_id,
                        principalTable: "InventoryQuantityFormulaTemplates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_inventory_items_units_panel_unit_id",
                        column: x => x.panel_unit_id,
                        principalTable: "Units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_inventory_items_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "Units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountObjectBankAccounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bank_branch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    swift_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_object_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_object_bank_accounts_account_objects_account_object_id",
                        column: x => x.account_object_id,
                        principalTable: "AccountObjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountObjectEmployeeProfiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    citizen_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: true),
                    social_insurance_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dependent_count = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_object_employee_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_object_employee_profiles_account_objects_account_object",
                        column: x => x.account_object_id,
                        principalTable: "AccountObjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_account_object_employee_profiles_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AccountObjectOpeningBalances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    credit_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    debit_amount_oc = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    credit_amount_oc = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_object_opening_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_account_object_opening_balances_account_objects_account_object_",
                        column: x => x.account_object_id,
                        principalTable: "AccountObjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_account_object_opening_balances_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "Currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemAttributes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item_attributes", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_attributes_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "InventoryItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_item_attributes_item_attribute_types_attribute_type",
                        column: x => x.attribute_type_id,
                        principalTable: "ItemAttributeTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemBarcodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode_value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    barcode_type = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_barcodes_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "InventoryItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemOpeningBalances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    foreign_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item_opening_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_opening_balances_currencies_currency_id",
                        column: x => x.currency_id,
                        principalTable: "Currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_item_opening_balances_inventory_items_inventory_item_",
                        column: x => x.inventory_item_id,
                        principalTable: "InventoryItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_item_opening_balances_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "Units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_item_opening_balances_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "Warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItemUnitConverts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    convert_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_item_unit_converts", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_unit_converts_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalTable: "InventoryItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_item_unit_converts_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "Units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryQuantityFormulaDetails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    formula_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_quantity_formula_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_quantity_formula_details_inventory_items_material_ite",
                        column: x => x.material_item_id,
                        principalTable: "InventoryItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_quantity_formula_details_inventory_quantity_formula_",
                        column: x => x.formula_template_id,
                        principalTable: "InventoryQuantityFormulaTemplates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_quantity_formula_details_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "Units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountObjectBankAccounts_AccountObjectId",
                table: "AccountObjectBankAccounts",
                column: "account_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_object_employee_profiles_department_id",
                table: "AccountObjectEmployeeProfiles",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjectEmployeeProfiles_AccountObjectId",
                table: "AccountObjectEmployeeProfiles",
                column: "account_object_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjectEmployeeProfiles_TenantId_CitizenId",
                table: "AccountObjectEmployeeProfiles",
                columns: new[] { "tenant_id", "citizen_id" },
                unique: true,
                filter: "citizen_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjectEmployeeProfiles_TenantId_SIN",
                table: "AccountObjectEmployeeProfiles",
                columns: new[] { "tenant_id", "social_insurance_number" },
                unique: true,
                filter: "social_insurance_number IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjectGroups_TenantId_GroupCode",
                table: "AccountObjectGroups",
                columns: new[] { "tenant_id", "group_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_account_object_opening_balances_account_object_id",
                table: "AccountObjectOpeningBalances",
                column: "account_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_object_opening_balances_currency_id",
                table: "AccountObjectOpeningBalances",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjectOpeningBalances_TenantId_ObjectId_CurrencyId",
                table: "AccountObjectOpeningBalances",
                columns: new[] { "tenant_id", "account_object_id", "currency_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_account_objects_account_object_group_id",
                table: "AccountObjects",
                column: "account_object_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountObjects_ObjectType",
                table: "AccountObjects",
                column: "object_type");

            migrationBuilder.CreateIndex(
                name: "IX_AccountObjects_TenantId",
                table: "AccountObjects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UIX_AccountObjects_TenantId_ObjectCode",
                table: "AccountObjects",
                columns: new[] { "tenant_id", "object_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_Currencies_TenantId_CurrencyCode",
                table: "Currencies",
                columns: new[] { "tenant_id", "currency_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_departments_parent_id",
                table: "Departments",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "UIX_Departments_TenantId_DepartmentCode",
                table: "Departments",
                columns: new[] { "tenant_id", "department_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_ExpenseItems_TenantId_ExpenseCode",
                table: "ExpenseItems",
                columns: new[] { "tenant_id", "expense_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_attributes_attribute_type_id",
                table: "InventoryItemAttributes",
                column: "attribute_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_attributes_inventory_item_id",
                table: "InventoryItemAttributes",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItemAttributes_TenantId_ItemId_AttributeTypeId",
                table: "InventoryItemAttributes",
                columns: new[] { "tenant_id", "inventory_item_id", "attribute_type_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_barcodes_inventory_item_id",
                table: "InventoryItemBarcodes",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItemBarcodes_TenantId_BarcodeValue",
                table: "InventoryItemBarcodes",
                columns: new[] { "tenant_id", "barcode_value" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_categories_parent_id",
                table: "InventoryItemCategories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItemCategories_IsActive_SortOrder",
                table: "InventoryItemCategories",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItemCategories_TenantId_CategoryCode",
                table: "InventoryItemCategories",
                columns: new[] { "tenant_id", "category_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_opening_balances_currency_id",
                table: "InventoryItemOpeningBalances",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_opening_balances_inventory_item_id",
                table: "InventoryItemOpeningBalances",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_opening_balances_unit_id",
                table: "InventoryItemOpeningBalances",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_opening_balances_warehouse_id",
                table: "InventoryItemOpeningBalances",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItemOpeningBalances_TenantId_ItemId_WarehouseId_UnitId",
                table: "InventoryItemOpeningBalances",
                columns: new[] { "tenant_id", "inventory_item_id", "warehouse_id", "unit_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_category_id",
                table: "InventoryItems",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_formula_template_id",
                table: "InventoryItems",
                column: "formula_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_panel_unit_id",
                table: "InventoryItems",
                column: "panel_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_unit_id",
                table: "InventoryItems",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ItemType",
                table: "InventoryItems",
                column: "item_type");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_TenantId",
                table: "InventoryItems",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItems_TenantId_ItemCode",
                table: "InventoryItems",
                columns: new[] { "tenant_id", "item_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_unit_converts_inventory_item_id",
                table: "InventoryItemUnitConverts",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_item_unit_converts_unit_id",
                table: "InventoryItemUnitConverts",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryItemUnitConverts_TenantId_ItemId_UnitId",
                table: "InventoryItemUnitConverts",
                columns: new[] { "tenant_id", "inventory_item_id", "unit_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_quantity_formula_details_formula_template_id",
                table: "InventoryQuantityFormulaDetails",
                column: "formula_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_quantity_formula_details_material_item_id",
                table: "InventoryQuantityFormulaDetails",
                column: "material_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_quantity_formula_details_unit_id",
                table: "InventoryQuantityFormulaDetails",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "UIX_InventoryQuantityFormulaTemplates_TenantId_TemplateCode",
                table: "InventoryQuantityFormulaTemplates",
                columns: new[] { "tenant_id", "template_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_ItemAttributeTypes_TenantId_AttributeCode",
                table: "ItemAttributeTypes",
                columns: new[] { "tenant_id", "attribute_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_Units_TenantId_UnitCode",
                table: "Units",
                columns: new[] { "tenant_id", "unit_code" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "UIX_Warehouses_TenantId_WarehouseCode",
                table: "Warehouses",
                columns: new[] { "tenant_id", "warehouse_code" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountObjectBankAccounts");

            migrationBuilder.DropTable(
                name: "AccountObjectEmployeeProfiles");

            migrationBuilder.DropTable(
                name: "AccountObjectOpeningBalances");

            migrationBuilder.DropTable(
                name: "ExpenseItems");

            migrationBuilder.DropTable(
                name: "InventoryItemAttributes");

            migrationBuilder.DropTable(
                name: "InventoryItemBarcodes");

            migrationBuilder.DropTable(
                name: "InventoryItemOpeningBalances");

            migrationBuilder.DropTable(
                name: "InventoryItemUnitConverts");

            migrationBuilder.DropTable(
                name: "InventoryQuantityFormulaDetails");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "AccountObjects");

            migrationBuilder.DropTable(
                name: "ItemAttributeTypes");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "AccountObjectGroups");

            migrationBuilder.DropTable(
                name: "InventoryItemCategories");

            migrationBuilder.DropTable(
                name: "InventoryQuantityFormulaTemplates");

            migrationBuilder.DropTable(
                name: "Units");
        }
    }
}
