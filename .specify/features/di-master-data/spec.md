# Feature Specification: DI Module — Step 3: Master Data (Account Objects & Items)

**Feature Branch**: `di-master-data`  
**Created**: 2026-04-18  
**Updated**: 2026-04-18 (Review v4 — EmployeeProfile DD-007, InventoryOpeningBalance DD-006, UnitPrice, Review v3 entities)  
**Status**: Draft  
**Module**: DI (Data Initialization) — Step 3 of N  
**Depends On**: `di-account-tree` (Chart of Accounts must exist)

---

## 1. Overview

This feature implements master data management for the DI (Data Initialization) module of a Vietnamese enterprise accounting webapp. It covers the core reference entities that all transaction modules depend on: **AccountObjects** (customers, vendors, employees), **InventoryItems** (products, materials, services), and essential **Lookup Data** (currencies, units, warehouses, departments, expense items).

Additionally, it provides **Excel/CSV import** capability so accounting staff can bulk-load data from existing spreadsheets when migrating to the system.

This is a **Step 3** foundational feature — it must be completed before any voucher entry modules (SA, PU, IN, GL) can function, since every transaction references account objects, inventory items, and lookup values.

### Scope Summary

| Sub-feature | Description |
|-------------|-------------|
| 3.1 | AccountObject entity (backend domain + persistence) |
| 3.2 | AccountObject CRUD + List UI (full-stack) |
| 3.3 | AccountObjectEmployeeProfile extension entity (DD-007) |
| 3.4 | InventoryItem entity (backend domain + persistence) — includes UnitPrice, SalePrice, ItemType, Serial/Lot/Panel flags |
| 3.5 | InventoryItem CRUD + List UI (full-stack) |
| 3.6 | InventoryItem child entities: UnitConvert (Gap I), FormulaTemplate/BOM (Gap J), Barcode (Gap P), Attributes (Gap N) |
| 3.7 | InventoryItemOpeningBalance entity (DD-006) — opening stock per warehouse |
| 3.8 | Lookup Data (Currency, Unit, Warehouse, Department, ExpenseItem, ItemAttributeType) |
| 3.9 | Excel/CSV Import for AccountObject and InventoryItem |

---

## 2. User Stories

### User Story 1 — Browse and Search Account Objects (Priority: P1)

As an accountant, I want to view all customers, vendors, and employees in a searchable, filterable list so I can quickly find the entity I need to reference in a voucher.

**Why this priority**: AccountObjects are referenced in nearly every transaction. Lookup performance during voucher entry is critical to daily workflow.

**Independent Test**: Navigate to DI → Account Objects. The list loads within 1 second, shows paginated rows with code, name, type badges, and status. Filtering by type (Customer / Vendor / Employee) and searching by name or code works correctly.

**Acceptance Scenarios**:

1. **Given** the system has 500 account objects, **When** the user opens the Account Objects screen, **Then** the list displays paginated rows (25 per page by default), with columns: Code, Name, Type, Tax Code, Phone, Status.
2. **Given** the list is open, **When** the user selects "Customer" in the type filter, **Then** only records with ObjectType including the Customer bit are shown.
3. **Given** the list is open, **When** the user types "Nguyen" in the search box, **Then** results filter in real-time to show all records where code or name contains "Nguyen" (case-insensitive, accent-insensitive).
4. **Given** the list is open, **When** the user clicks the column header "Code", **Then** the list sorts ascending by code; clicking again sorts descending.

---

### User Story 2 — Create and Edit an Account Object (Priority: P1)

As an accountant, I want to create a new account object (customer, vendor, or employee) with all its details — including bank accounts and opening balances — so the entity is ready to be used in vouchers.

**Why this priority**: Without the ability to create account objects, no transaction module can operate. This is the core CRUD flow for the most heavily used master data entity.

**Independent Test**: Create a new customer named "Công ty TNHH ABC" with code "KH001", attach one bank account, set an opening balance in VND, and save. The record appears in the list and can be reopened to confirm all fields persist.

**Acceptance Scenarios**:

1. **Given** the user is on the Account Objects list, **When** they click "Thêm mới" (New), **Then** a form opens with four tabs: Thông tin chung (General), Thông tin nhân viên (Employee — visible only when Employee bit is set), Tài khoản ngân hàng (Bank Accounts), Số dư đầu kỳ (Opening Balance).
2. **Given** the form is open, **When** the user fills in required fields (Code, Name, ObjectType) and presses Ctrl+S, **Then** the record is saved and the user is returned to the list with a success message.
3. **Given** the user selects "Employee" type, **When** they save, **Then** the ObjectType bitmask stores the Employee flag (4); the same object can additionally be flagged as Vendor (2) via checkbox.
4. **Given** the form has unsaved changes, **When** the user navigates away, **Then** a confirmation dialog warns about unsaved changes (dirty-form guard).
5. **Given** an existing account object, **When** the user opens it and modifies the address, **Then** Ctrl+S saves the change; the updated record reflects immediately in the list.
6. **Given** an account object has vouchers referencing it, **When** the user attempts to change the code, **Then** the system prevents the change and shows an explanatory message.

---

### User Story 3 — Manage Bank Accounts for an Account Object (Priority: P2)

As an accountant, I want to record one or more bank accounts for a customer or vendor so that payment vouchers can auto-populate the correct bank details.

**Why this priority**: Bank account details are needed for payment processing but are not blocking for basic voucher entry.

**Independent Test**: Open an existing vendor, go to the Bank Accounts tab, add a bank account with bank name "Vietcombank", branch "Hà Nội", account number "1234567890", save. Re-open the vendor and confirm the bank account is present.

**Acceptance Scenarios**:

1. **Given** the Bank Accounts tab is active, **When** the user clicks "Thêm" (Add), **Then** a new inline row appears with fields: BankName, BankBranch, AccountNumber, SwiftCode.
2. **Given** a bank account row exists, **When** the user clicks the delete icon on that row, **Then** the row is removed and the change is saved with the parent record on Ctrl+S.
3. **Given** the user leaves AccountNumber blank, **When** they save, **Then** validation blocks save and highlights the required field.

---

### User Story 4 — Deactivate an Account Object (Priority: P2)

As an accountant, I want to mark an account object as inactive so it no longer appears in lookup dropdowns while preserving its historical transaction data.

**Why this priority**: Keeping the master data clean is important, but it is not blocking for initial data entry.

**Independent Test**: Mark a customer as Inactive. Open any voucher's account object lookup — the deactivated customer is no longer selectable. Re-activate and confirm it reappears.

**Acceptance Scenarios**:

1. **Given** an active account object, **When** the user toggles Status to "Inactive" and saves, **Then** the record is hidden from all voucher lookup dropdowns but remains visible in the Account Objects list with an "Inactive" badge.
2. **Given** an inactive account object is selected in the list, **When** the user filters by "Active", **Then** the inactive record is excluded.
3. **Given** an account object has open (unposted) vouchers referencing it, **When** the user attempts to deactivate it, **Then** the system warns but still allows deactivation (non-blocking warning, not a hard block).

---

### User Story 5 — Browse and Search Inventory Items (Priority: P1)

As a warehouse manager or accountant, I want to view all inventory items in a filterable list with a category tree panel so I can quickly locate products, materials, or services.

**Why this priority**: Inventory items are referenced in every purchase and sales voucher line. The lookup must be fast and intuitive.

**Independent Test**: Navigate to DI → Inventory Items. The list loads with a left-side category tree. Selecting a category filters items to that category and its children. Search by name works across all categories.

**Acceptance Scenarios**:

1. **Given** the Inventory Items screen is open, **When** the page loads, **Then** a category tree panel is shown on the left and a DataTable of items on the right.
2. **Given** the user clicks a leaf category node in the tree, **When** the table updates, **Then** only items belonging to that category are shown.
3. **Given** the user clicks a parent category node, **When** the table updates, **Then** items from that category AND all its descendant categories are shown.
4. **Given** the search box is used while a category is selected, **When** the user types a keyword, **Then** results are filtered by both the selected category subtree and the keyword.

---

### User Story 6 — Create and Edit an Inventory Item (Priority: P1)

As a warehouse manager, I want to create a new inventory item with its unit, category, costing method, and stock settings so the item is ready to be used in purchase/sales vouchers.

**Why this priority**: The item catalog must be populated before any inventory transactions can be processed.

**Independent Test**: Create an item "Laptop Dell XPS 13" with code "IT001", unit "Cái", category "Máy tính", costing method FIFO, min stock 2, save. The item appears in the list and can be found via the category tree.

**Acceptance Scenarios**:

1. **Given** the user clicks "Thêm mới" on the Inventory Items screen, **Then** a form opens with tabs: Thông tin chung (General), Cài đặt kho (Stock Settings).
2. **Given** the form is open, **When** the user selects a costing method from the dropdown (FIFO / LIFO / Weighted Average / Specific Identification), **Then** the selection is saved and the correct method code (1/2/3/4) is stored.
3. **Given** required fields are filled (Code, Name, Unit, Category), **When** the user presses Ctrl+S, **Then** the item is saved successfully.
4. **Given** an item code already exists, **When** another item is saved with the same code, **Then** the system blocks save with a "Duplicate code" error.

---

### User Story 7 — Manage Lookup Data (Priority: P2)

As a system administrator, I want to manage reference data (currencies, units of measure, warehouses, departments, expense items) so accountants can select correct values during data entry.

**Why this priority**: Lookup data must be seeded before inventory items and vouchers can be created, but the UI for managing it is lower priority than the main entities.

**Independent Test**: Navigate to DI → Setup → Units. The seeded Vietnamese units (Cái, Chiếc, Hộp, etc.) are present. Add a new unit "Tấn", save. It appears in the list and in the Unit dropdown on the Inventory Item form.

**Acceptance Scenarios**:

1. **Given** the system is newly initialized, **When** an admin opens DI → Setup → Currencies, **Then** VND is present with symbol "₫" and exchange rate 1.
2. **Given** the Units management screen, **When** the user adds unit "Kg" and saves, **Then** it becomes available in the Unit lookup on the Inventory Item form.
3. **Given** the Departments screen, **When** the user creates "Phòng Kế Toán" under "Văn Phòng Công Ty" parent, **Then** the department tree displays the correct parent-child relationship.
4. **Given** a unit is referenced by at least one inventory item, **When** the user tries to delete it, **Then** the system blocks deletion and shows which items reference it.

---

### User Story 8 — Import Account Objects from Excel/CSV (Priority: P3)

As an accountant migrating from a legacy system, I want to import account objects from an Excel file so I don't have to enter hundreds of customers/vendors manually.

**Why this priority**: Useful for initial setup but not blocking for system operation. Manual entry is always available.

**Independent Test**: Download the AccountObject template, fill in 10 rows including one row with a duplicate code and one with a missing required field. Upload the file. The system imports the valid 8 rows, reports 2 errors with row numbers and messages, and the 8 valid records appear in the list.

**Acceptance Scenarios**:

1. **Given** the Account Objects screen, **When** the user clicks "Nhập từ Excel" (Import from Excel), **Then** a dialog opens with options: Download Template, Select File, Import.
2. **Given** a valid Excel file is uploaded, **When** import runs, **Then** a progress indicator is shown; upon completion a result summary displays: X records imported, Y records failed.
3. **Given** the imported file has rows with duplicate codes (matching existing records), **When** import runs, **Then** duplicates are skipped (not overwritten) and listed in the error report with message "Mã đã tồn tại".
4. **Given** a row is missing a required field (Code or Name), **When** import runs, **Then** that row is skipped and listed in the error report with the specific missing field identified.
5. **Given** import completes with errors, **When** the user downloads the error report, **Then** the report is an Excel file containing only the failed rows with an additional "Lỗi" (Error) column.

---

### User Story 9 — Import Inventory Items from Excel/CSV (Priority: P3)

As a warehouse manager migrating from a legacy system, I want to import the item catalog from Excel so the entire product list is available quickly.

**Why this priority**: Same rationale as AccountObject import — useful for migration but not blocking.

**Independent Test**: Download the InventoryItem template, fill in 20 rows. Upload and import. All 20 items appear in the Inventory Items list assigned to their respective categories.

**Acceptance Scenarios**:

1. **Given** the Inventory Items screen, **When** the user clicks "Nhập từ Excel", **Then** a dialog opens with template download and file upload options.
2. **Given** the uploaded file references a category code that does not exist, **When** import runs, **Then** that row is rejected with message "Nhóm hàng không tồn tại: [code]".
3. **Given** the uploaded file references a unit code that does not exist, **When** import runs, **Then** that row is rejected with message "Đơn vị tính không tồn tại: [code]".
4. **Given** a valid batch of items is imported, **When** the user opens the category tree, **Then** items are correctly placed under their respective categories.

---

### User Story 10 — Manage Employee Profile for an Account Object (Priority: P2)

As an accountant, I want to record employee-specific details (citizen ID, date of birth, social insurance number, hire date, department, dependent count) for an AccountObject flagged as Employee, so that PIT declarations and social insurance reports can be generated correctly.

**Why this priority**: Employee HR-lite data is needed for PIT (TT111/2013) and BHXH (TT59/2015-BHXH) compliance. Not blocking voucher entry but required before PA module (payroll).

**Independent Test**: Create an AccountObject with Employee type checked. Go to "Thông tin nhân viên" tab. Fill in CCCD "012345678901", DOB "1990-05-15", Gender "Nam", BHXH "0123456789", Hire Date "2020-01-15", Department "Kế Toán", Dependents 2. Save. Re-open and confirm all fields persist.

**Acceptance Scenarios**:

1. **Given** the user checks "Nhân viên" (Employee) in the ObjectType checkboxes, **When** the form saves, **Then** the "Thông tin nhân viên" tab becomes visible and editable.
2. **Given** the Employee tab is active, **When** the user fills in CitizenId and saves, **Then** the system validates uniqueness of CitizenId per tenant. If a duplicate exists, save is blocked with message "Số CMND/CCCD đã tồn tại".
3. **Given** the Employee tab is active, **When** the user fills SocialInsuranceNumber, **Then** the system validates uniqueness per tenant. Duplicate → error "Mã số BHXH đã tồn tại".
4. **Given** the user unchecks "Nhân viên" on an object that has EmployeeProfile data, **When** they save, **Then** the system warns "Bỏ chọn Nhân viên sẽ xóa thông tin nhân viên đã nhập. Tiếp tục?" — if confirmed, the profile record is soft-deleted.
5. **Given** the user selects a Department dropdown on the Employee tab, **Then** the dropdown shows the Department tree in hierarchical format, filtered to active departments only.

---

### User Story 11 — Manage Inventory Opening Stock Balances (Priority: P2)

As an accountant setting up the system, I want to enter opening stock quantities and values per item per warehouse so that inventory reports are accurate from the first day of use.

**Why this priority**: Opening stock balance is required for correct inventory valuation and costing from day one. Must be entered before any IN module transactions.

**Independent Test**: Open Inventory Items → select item "Laptop Dell XPS 13" → go to "Số dư tồn kho đầu kỳ" tab. Add row: Warehouse "Kho Chính", Quantity 50, Unit Cost 25,000,000. Amount auto-calculates to 1,250,000,000. Save. Re-open and confirm.

**Acceptance Scenarios**:

1. **Given** the user opens an InventoryItem detail form, **When** the item has `ItemType != Service`, **Then** a tab "Số dư tồn kho đầu kỳ" (Opening Stock Balance) is visible.
2. **Given** the Opening Stock tab is active, **When** the user adds a row, **Then** they select Warehouse (required), Unit (defaults to item main unit), enter Quantity and UnitCost. Amount is auto-calculated as `Quantity × UnitCost`.
3. **Given** a row already exists for the same Item + Warehouse + Unit combination, **When** the user tries to add a duplicate, **Then** the system blocks with message "Đã có số dư đầu kỳ cho kho và đơn vị tính này".
4. **Given** the opening balance uses a foreign currency, **When** the user selects a currency other than VND, **Then** ForeignAmount and ExchangeRate fields become visible; Amount is recalculated.
5. **Given** the item is a Service (ItemType=3), **When** the form loads, **Then** the Opening Stock tab is hidden (services have no stock).

---

### Edge Cases

- What happens when an AccountObject has ObjectType bitmask 0 (no type selected)? → System requires at least one type bit to be set; saves are blocked.
- What happens when a user imports an Excel file with 10,000+ rows? → System processes in batches; UI shows progress; partial success is possible (valid rows imported even if some fail).
- What happens when two users simultaneously edit the same AccountObject? → Optimistic concurrency (via RowVersion) detects the conflict; the second save fails with a "Record was modified by another user" message.
- What happens when a currency's exchange rate is updated? → Existing vouchers are unaffected; only new vouchers use the new rate.
- What happens when a category is deleted that has child categories? → System blocks deletion and instructs user to reassign or delete children first.
- What happens when the Excel import file contains formulas or merged cells? → System reads cell values only (not formulas); merged cells are treated as blank — the row is rejected if the merged cell held a required field.
- What happens when a Department tree exceeds 5 levels? → Level 6+ creation is blocked with "Tối đa 5 cấp phòng ban" (Maximum 5 department levels).
- What happens when two employees have the same CCCD number? → System blocks save with "Số CMND/CCCD đã tồn tại" (unique per tenant, partial index — NULLs allowed).
- What happens when opening stock is entered for a Service item? → The Opening Stock tab is hidden; no opening balance row can be created for ItemType=Service.
- What happens when a barcode value is used by another item? → System blocks save with "Mã vạch đã được sử dụng bởi hàng hóa khác" (unique per tenant).
- What happens when an item's main unit is changed after unit conversions exist? → System warns that existing conversion rates may be invalidated and requires confirmation.
- What happens when a BOM formula template is deleted while items reference it? → FormulaTemplateId on referencing items is set to NULL (ON DELETE SET NULL); items lose BOM association but are not affected otherwise.

---

## 3. Functional Requirements

### 3.1 AccountObject Entity

| ID | Requirement |
|----|------------|
| FR-AO-001 | The system SHALL maintain an AccountObject entity with fields: ObjectCode (unique, max 25 chars), ObjectName (max 255 chars), ObjectNameEnglish, Address, TaxCode, Email, Phone, Fax, Website, ContactPerson, ContactPhone, Description, CreditLimit (decimal), PaymentTermDays (int), Status (Active/Inactive). |
| FR-AO-002 | The system SHALL implement ObjectType as an integer bitmask: bit 1 = Customer (Khách hàng), bit 2 = Vendor (Nhà cung cấp), bit 4 = Employee (Nhân viên). An AccountObject MAY have multiple bits set simultaneously (BR-DI04). |
| FR-AO-003 | The system SHALL associate zero or more BankAccount records with each AccountObject, each containing: BankName, BankBranch, AccountNumber (required), SwiftCode. |
| FR-AO-004 | The system SHALL associate zero or more OpeningBalance records with each AccountObject per currency, persisted in table **account_object_opening_balance** (columns: `account_object_id`, `currency_id`, `debit_amount`, `credit_amount`). This enables multi-currency opening balance initialization for AR/AP sub-ledger. |
| FR-AO-005 | The system SHALL enforce uniqueness of ObjectCode per tenant. |
| FR-AO-006 | The system SHALL apply TenantId as a global filter on all AccountObject queries. |
| FR-AO-007 | The system SHALL support soft-deactivation: Status = Inactive hides the record from lookup dropdowns but preserves all data. |

### 3.1.1 AccountObjectEmployeeProfile (DD-007)

| ID | Requirement |
|----|------------|
| FR-AO-030 | The system SHALL maintain an AccountObjectEmployeeProfile entity as a 1:1 optional extension of AccountObject, containing: CitizenId (max 20), DateOfBirth, Gender (Male/Female/Other), SocialInsuranceNumber (max 10), HireDate, DepartmentId (FK → Department), DependentCount (int, default 0). |
| FR-AO-031 | The EmployeeProfile record SHALL only exist when the AccountObject has the Employee bit (4) set in ObjectType. |
| FR-AO-032 | The system SHALL enforce uniqueness of CitizenId per tenant (partial unique index — NULLs allowed). |
| FR-AO-033 | The system SHALL enforce uniqueness of SocialInsuranceNumber per tenant (partial unique index — NULLs allowed). |
| FR-AO-034 | When the Employee bit is removed from ObjectType, the system SHALL prompt for confirmation and soft-delete the associated EmployeeProfile if confirmed. |

### 3.2 AccountObject API

| ID | Requirement |
|----|------------|
| FR-AO-010 | The system SHALL expose `GET /api/account-objects` returning a paginated list (page, pageSize, totalCount) supporting query parameters: `type` (bitmask filter — returns objects where `ObjectType & type != 0`, i.e., objects that include at least the selected type bit), `status` (Active/Inactive/All), `search` (code or name contains). |
| FR-AO-011 | The system SHALL expose `GET /api/account-objects/{id}` returning full detail including bank accounts, opening balances, and employee profile (when Employee bit is set). |
| FR-AO-012 | The system SHALL expose `POST /api/account-objects` to create a new account object. |
| FR-AO-013 | The system SHALL expose `PUT /api/account-objects/{id}` to update an existing account object. |
| FR-AO-014 | The system SHALL expose `DELETE /api/account-objects/{id}` performing a soft delete (Status = Inactive) if the object has referenced vouchers; hard delete only if no references exist. |
| FR-AO-015 | All API endpoints SHALL return standard problem-details error responses on validation failure. |

### 3.3 AccountObject UI

| ID | Requirement |
|----|------------|
| FR-AO-020 | The list screen SHALL display a PrimeNG DataTable with columns: **checkbox** (first column, for row selection), Code (frozen), Name (frozen), Type badges (color-coded: Customer=blue, Vendor=orange, Employee=green), TaxCode, Phone, Status badge. There is NO inline action column — all row actions (Edit, Delete) are driven through the `GridActionBar` toolbar. |
| FR-AO-021 | The list screen SHALL provide filter controls: Type multi-select checkboxes (Customer / Vendor / Employee) — each checkbox submits the corresponding bit value; the query returns objects that include ANY of the selected bits; Status dropdown (All / Active / Inactive); search input. |
| FR-AO-022 | The list SHALL support server-side pagination with configurable page size (10, 25, 50, 100). Column resize and reorder SHALL be persisted in localStorage per user. |
| FR-AO-023 | The detail form SHALL use a tabbed layout: Tab 1 "Thông tin chung" (General Info), Tab 2 "Thông tin nhân viên" (Employee — conditionally visible when Employee bit is set), Tab 3 "Tài khoản ngân hàng" (Bank Accounts), Tab 4 "Số dư đầu kỳ" (Opening Balances). |
| FR-AO-024 | The detail form SHALL register keyboard shortcuts: Ctrl+S = Save, Ctrl+Shift+S = Save & New, F3 = Search/Lookup, Ctrl+D = Duplicate, Insert = Add row (on bank accounts tab), Ctrl+Delete = Delete row. |
| FR-AO-025 | Required fields (Code, Name, ObjectType) SHALL be marked with a red asterisk (*) and show red border validation on blur. |
| FR-AO-026 | The form SHALL implement dirty-form guard: navigating away from unsaved changes triggers a confirmation dialog. |
| FR-AO-027 | The list screen SHALL use `GridActionBar` (FR-TB-001) as the sole source of row actions. The **Sửa** and **Xóa** buttons on `GridActionBar` SHALL be disabled when `selectedCount === 0`. Clicking a row (or its checkbox) SHALL select it and increment `selectedCount`; `GridActionBar` SHALL immediately re-enable the relevant buttons. |

### 3.4 InventoryItem Entity

| ID | Requirement |
|----|------------|
| FR-IN-001 | The system SHALL maintain an InventoryItem entity with fields: ItemCode (unique, max 25 chars), ItemName (max 255 chars), ItemNameEnglish, Description, Barcode (max 255, legacy single-barcode field), ItemType (enum: RawMaterial/FinishedProduct/Goods/Service, default Goods), UnitPrice (decimal nullable — default purchase/cost price), SalePrice1/2/3 (decimal nullable — 3-tier sale prices), DefaultTaxRate (decimal nullable — default VAT rate), IsActive. |
| FR-IN-002 | Each InventoryItem SHALL reference a Unit (FK, required) and a Category (FK, nullable). Additionally: PanelUnitId (FK → Unit, nullable — for panel/dimension items), FormulaTemplateId (FK → FormulaTemplate, nullable — BOM for finished products). |
| FR-IN-003 | The system SHALL store CostingMethod per item: 1=FIFO, 2=LIFO, 3=Weighted Average, 4=Specific Identification. Default is 3 (Weighted Average) per BR-IN01/BR-IN02. |
| FR-IN-004 | The system SHALL store stock settings: MinStockLevel (decimal), MaxStockLevel (decimal), LeadTimeDays (int). |
| FR-IN-005 | The system SHALL enforce uniqueness of ItemCode per tenant. |
| FR-IN-006 | The system SHALL apply TenantId as a global filter on all InventoryItem queries. |
| FR-IN-007 | The system SHALL store tracking flags per item: IsFollowSerial (serial number tracking), IsFollowLot (lot/batch tracking), IsFollowExpiry (expiry date tracking). All default to false. |
| FR-IN-008 | The system SHALL store panel/dimension flags: IsPanelItem (boolean), PanelUnitId (FK → Unit). When IsPanelItem=true, voucher lines enable dimension input (Height×Width×Length). |

### 3.4.1 InventoryItem Child Entities (Review v3)

| ID | Requirement |, IsActive (default true), SortOrder (int, for display ordering)
|----|------------|
| FR-IN-030 | The system SHALL maintain InventoryItemUnitConvert (Gap I): per-item secondary units with ConvertRate (numeric 18,6), IsDefaultSaleUnit, IsDefaultPurchaseUnit. Unique constraint: one item cannot have two rows with the same UnitId. |
| FR-IN-031 | The system SHALL maintain InventoryQuantityFormulaTemplate (Gap J — BOM): FormulaCode (unique), FormulaName, IsActive. Each template has child InventoryQuantityFormulaDetail rows: MaterialItemId (FK → InventoryItem), UnitId, Quantity (numeric 18,6), Description. |
| FR-IN-032 | The system SHALL maintain InventoryItemBarcode (Gap P): per-item barcodes with BarcodeType (Code128/EAN13/EAN8/QRCode/DataMatrix/UPC_A), BarcodeValue (max 255, unique per tenant), UnitId (nullable — null = main unit), IsPrimary flag. |
| FR-IN-033 | The system SHALL maintain ItemAttributeType (Gap N): tenant-level attribute type definitions (AttributeCode, AttributeName, SortOrder, IsActive). And InventoryItemAttribute: per-item attribute values (AttributeTypeId, AttributeValue max 255, SortOrder). Unique constraint: one item + one attribute type = one row. |

### 3.4.2 InventoryItemOpeningBalance (DD-006)

| ID | Requirement |
|----|------------|
| FR-IN-040 | The system SHALL maintain an InventoryItemOpeningBalance entity per item per warehouse: InventoryItemId (FK), WarehouseId (FK), UnitId (FK), Quantity (numeric 18,4), UnitCost (numeric 18,6), Amount (numeric 18,2, auto-calculated), CurrencyId (FK nullable), ForeignAmount (nullable), ExchangeRate (nullable), OpeningDate (required). |
| FR-IN-041 | The system SHALL enforce uniqueness of (TenantId, InventoryItemId, WarehouseId, UnitId) — one opening balance per item per warehouse per unit. |
| FR-IN-042 | The Amount field SHALL be auto-calculated as Quantity × UnitCost on save. |
| FR-IN-043 | The Opening Stock tab SHALL be hidden for items with ItemType = Service (no stock for services). |
| FR-IN-044 | When the IN module is built (future sprint), a batch job SHALL convert InventoryItemOpeningBalance rows into proper INInward vouchers (RefType=OpeningInventoryEntry) that post to InventoryLedger. |

### 3.5 InventoryItemCategory Entity

| ID | Requirement |
|----|------------|
| FR-CAT-001 | The system SHALL maintain an InventoryItemCategory entity with: CategoryCode (unique), CategoryName, ParentId (self-referential FK, nullable for root nodes). |
| FR-CAT-002 | The system SHALL enforce a maximum nesting depth of 5 levels for category hierarchy. |
| FR-CAT-003 | The system SHALL expose `GET /api/inventory-item-categories/tree` returning the full category tree as a nested structure. |

### 3.6 InventoryItem API

| ID | Requirement |
|----|------------|
| FR-IN-010 | The system SHALL expose `GET /api/inventory-items` returning a paginated list supporting query parameters: `categoryId` (includes descendants), `status`, `search` (code or name contains), `itemType`. |
| FR-IN-011 | The system SHALL expose `GET /api/inventory-items/{id}` returning full detail including unit converts, barcodes, attributes, opening balances. |
| FR-IN-012 | The system SHALL expose `POST /api/inventory-items` to create a new item (with child entities in same request body). |
| FR-IN-013 | The system SHALL expose `PUT /api/inventory-items/{id}` to update an existing item (with child entity diff — add/update/remove). |
| FR-IN-014 | The system SHALL expose `DELETE /api/inventory-items/{id}` performing a soft delete if the item has stock transactions; hard delete otherwise. |
| FR-IN-015 | The system SHALL expose CRUD endpoints for BOM templates: `GET/POST /api/formula-templates`, `GET/PUT/DELETE /api/formula-templates/{id}` (with detail lines). |
| FR-IN-016 | The system SHALL expose CRUD endpoints for ItemAttributeType: `GET/POST /api/item-attribute-types`, `GET/PUT/DELETE /api/item-attribute-types/{id}`. |

### 3.7 InventoryItem UI

| ID | Requirement |
|----|------------|
| FR-IN-020 | The list screen SHALL display a split layout: left panel = category tree (PrimeNG Tree), right panel = PrimeNG DataTable. |
| FR-IN-021 | The DataTable columns SHALL include: **checkbox** (first column, for row selection), ItemCode (frozen), ItemName, Unit, Category, UnitPrice (right-aligned monospace), ItemType badge, Status (Active/Inactive). There is NO inline action column — all row actions (Edit, Delete) are driven through the `GridActionBar` toolbar. |
| FR-IN-022 | Selecting a category node in the tree SHALL filter the DataTable to items in that category and all descendants. |
| FR-IN-023 | The detail form SHALL use 7 tabs: Tab 1 "Thông tin chung" (General — ItemCode, ItemName, UnitId, PanelUnitId, CategoryId, UnitPrice, SalePrice1/2/3, DefaultTaxRate, ItemType, CostingMethod, FormulaTemplateId), Tab 2 "Đơn vị tính phụ" (Unit Conversions), Tab 3 "Mã vạch" (Barcodes), Tab 4 "Thuộc tính" (Attributes), Tab 5 "Định mức NVL" (BOM — visible when FormulaTemplateId is set or ItemType=1 FinishedProduct), Tab 6 "Cài đặt kho" (Stock Settings — MinStockLevel, MaxStockLevel, LeadTimeDays), Tab 7 "Số dư tồn kho đầu kỳ" (Opening Stock — hidden for Service items per BR-IN04). |
| FR-IN-024 | The CostingMethod field SHALL be a dropdown: FIFO / LIFO / Bình quân gia quyền / Đích danh. |
| FR-IN-025 | The detail form SHALL register the same keyboard shortcuts as the AccountObject form (FR-AO-024). |
| FR-IN-026 | MinStockLevel and MaxStockLevel fields SHALL accept decimal input; MaxStockLevel must be greater than MinStockLevel when both are non-zero. |
| FR-IN-027 | The InventoryItem list screen SHALL use `GridActionBar` (FR-TB-001) as the sole source of row actions. The **Sửa** and **Xóa** buttons SHALL be disabled when `selectedCount === 0`. Selecting a row (or its checkbox) SHALL immediately enable the buttons. |

### 3.8 Lookup Data — Entities and APIs

| ID | Requirement |
|----|------------|
| FR-LK-001 | The system SHALL manage a **Currency** entity: CurrencyCode (ISO 4217, e.g., "VND"), CurrencyName, Symbol, ExchangeRate (decimal, tenant-specific). |
| FR-LK-002 | The system SHALL manage a **Unit** entity: UnitCode (unique), UnitName. |
| FR-LK-003 | The system SHALL manage a **Warehouse (Stock)** entity: WarehouseCode (unique), WarehouseName, Address, ManagerName. Full CRUD (Create, Read, Update, Delete) for Warehouse is implemented in **step 3.5** of this feature and is **not deferred to the IN (Inventory) module**. |
| FR-LK-004 | The system SHALL manage a **Department (OrganizationUnit)** entity: DeptCode (unique), DeptName, ParentId (self-referential, max 5 levels). |
| FR-LK-005 | The system SHALL manage an **ExpenseItem** entity: ExpenseCode (unique), ExpenseName, AccountCode (references Account.AccountNumber). |
| FR-LK-006 | Each lookup entity SHALL expose standard REST endpoints: `GET /api/{resource}` (list), `POST`, `PUT /{id}`, `DELETE /{id}`. |
| FR-LK-009 | The system SHALL manage an **ItemAttributeType** entity: AttributeCode (unique per tenant, max 25), AttributeName (max 100), SortOrder, IsActive. Used to define custom attribute types (e.g., "Thương hiệu", "Màu sắc"). |
| FR-LK-010 | The system SHALL manage **InventoryQuantityFormulaTemplate** (BOM master) and child **InventoryQuantityFormulaDetail** entities. Template: FormulaCode (unique), FormulaName, IsActive. Detail: MaterialItemId (FK → InventoryItem), UnitId, Quantity, Description, SortOrder. |
| FR-LK-007 | The system SHALL seed the following data on new tenant creation: Currency VND (symbol "₫", rate 1.0); Units: Cái, Chiếc, Hộp, Kg, Lít, M2, M3, Thùng, Bộ, Đôi. |
| FR-LK-008 | Deletion of a lookup record referenced by any active entity (InventoryItem.UnitId, etc.) SHALL be blocked with an error listing the referencing records. |

### 3.9 Lookup Management UI

| ID | Requirement |
|----|------------|
| FR-LK-020 | Each lookup entity SHALL have a management screen accessible from DI → Danh mục (Setup) sub-menu. |
| FR-LK-021 | Currency, Unit, and Warehouse screens SHALL use inline edit within a PrimeNG DataTable (editable rows). |
| FR-LK-022 | Department screen SHALL display a tree view with add/edit/delete actions per node. |
| FR-LK-023 | ExpenseItem screen SHALL use a simple list with a detail dialog. |
| FR-LK-024 | ItemAttributeType screen SHALL use inline edit within a PrimeNG DataTable. |
| FR-LK-025 | FormulaTemplate screen SHALL show a master-detail layout: template list on the left, detail lines (material items grid) on the right. |

### 3.11 Shared Toolbar Components

Three reusable Angular standalone components used across all list screens. Defined in `src/webapp/src/app/shared/components/toolbar/`.

#### 3.11.1 GridActionBar

| ID | Requirement |
|----|------------|
| FR-TB-001 | `GridActionBar` SHALL render standard action buttons: **Thêm mới** (primary), **Sửa** (default), **Xóa** (danger). |
| FR-TB-002 | Nút **Sửa** và **Xóa** SHALL be disabled when `selectedCount === 0` (no row selected). |
| FR-TB-003 | Optional buttons controlled by boolean inputs: `[showDuplicate]` (Nhân bản), `[showImport]` (Nhập Excel), `[showExport]` (Xuất Excel), `[showPrint]` (In). All default to `false`. |
| FR-TB-004 | SHALL provide `<ng-content>` slot for host screen to inject extra controls (e.g., filter dropdown, type selector). |
| FR-TB-005 | SHALL emit typed events: `(addClick)`, `(editClick)`, `(deleteClick)`, `(duplicateClick)`, `(importClick)`, `(exportClick)`, `(printClick)`. |
| FR-TB-006 | At viewport < 960px: text labels SHALL hide, showing icon-only buttons with `pTooltip`. |
| FR-TB-007 | At viewport < 768px: optional buttons (Nhập/Xuất/In/Nhân bản) SHALL collapse into a single `[···]` overflow dropdown menu. |

#### 3.11.2 DateRangeBar *(form phát sinh only)*

| ID | Requirement |
|----|------------|
| FR-TB-010 | `DateRangeBar` SHALL render: Kỳ preset dropdown · Từ ngày (date picker) · Đến ngày (date picker) · **"Lấy dữ liệu"** button. |
| FR-TB-011 | Default preset on mount: **"Đầu tháng đến hiện tại"** (first day of current month → today). |
| FR-TB-012 | Kỳ preset dropdown SHALL contain the following options in order: Hôm nay / Hôm qua / Tuần này / Tuần trước / Tháng này / Tháng trước / Đầu tháng đến hiện tại / Đầu quý đến hiện tại / Quý này / Quý trước / Đầu năm đến hiện tại / Năm nay / Năm trước / 6 tháng đầu năm / 6 tháng cuối năm / **Tháng 1 … Tháng 12** (12 items) / Tùy chỉnh. |
| FR-TB-013 | Selecting a preset SHALL auto-fill Từ ngày and Đến ngày. Manually editing either date field SHALL switch preset to **"Tùy chỉnh"** automatically. |
| FR-TB-014 | Data SHALL only be fetched when user clicks **"Lấy dữ liệu"** — no auto-apply on date change. |
| FR-TB-015 | SHALL emit `(rangeChange)` with `{ fromDate: Date, toDate: Date, preset: string }` on button click. |

#### 3.11.3 SearchBar

| ID | Requirement |
|----|------------|
| FR-TB-020 | `SearchBar` SHALL render: text input with placeholder "Nhập từ khóa tìm kiếm..." · **"Tìm kiếm"** button. |
| FR-TB-021 | Search SHALL trigger on **Enter** keypress OR clicking "Tìm kiếm" button. No debounce auto-search. |
| FR-TB-022 | When input is non-empty, a **×** clear button SHALL appear inside the field. Clicking it clears the keyword and emits a search event with empty string (resetting the list). |
| FR-TB-023 | SHALL provide `<ng-content slot="filters">` for host screen to inject extra filter controls (e.g., ItemType dropdown for InventoryItem list). |
| FR-TB-024 | SHALL emit `(search)` with `{ keyword: string }` on trigger. |

#### 3.11.4 Composition by screen type

| Screen Type | Components Used |
|-------------|----------------|
| Danh mục (master data list) | `GridActionBar` + `SearchBar` |
| Phát sinh (transaction list) | `GridActionBar` + `DateRangeBar` + `SearchBar` |
| Treelist (Phòng ban, Category) | `GridActionBar` only (no search bar needed at top) |

---

### 3.10 Excel/CSV Import

| ID | Requirement |
|----|------------|
| FR-IMP-001 | The system SHALL provide a downloadable Excel template for AccountObject import containing columns: ObjectCode, ObjectName, ObjectType (Customer/Vendor/Employee as text), Address, TaxCode, Email, Phone, CreditLimit, PaymentTermDays. |
| FR-IMP-002 | The system SHALL provide a downloadable Excel template for InventoryItem import containing columns: ItemCode, ItemName, UnitCode, CategoryCode, Barcode, CostingMethod (FIFO/LIFO/WA/SI as text), ItemType (NVL/TP/HH/DV as text), UnitPrice, MinStock, MaxStock. |
| FR-IMP-003 | The system SHALL expose `GET /api/import/template/account-objects` and `GET /api/import/template/inventory-items` to serve template files as downloadable Excel. |
| FR-IMP-004 | The system SHALL expose `POST /api/import/account-objects` and `POST /api/import/inventory-items` accepting multipart file upload. |
| FR-IMP-005 | Import processing SHALL validate each row for: required fields present, code uniqueness (against existing data), referenced codes exist (UnitCode, CategoryCode). |
| FR-IMP-006 | Import processing SHALL use a **best-effort model**: each row is validated and inserted individually. Valid rows are committed immediately as they are processed; invalid rows are skipped and accumulated in the error list. There is **no** batch-level transaction — a row failure never causes other valid rows to be rolled back. |
| FR-IMP-007 | The import result response SHALL contain: `successCount`, `errorCount`, `errors` array with `{ rowNumber, field, message }` per error. |
| FR-IMP-008 | The system SHALL support files up to 5 MB and up to 5,000 rows per import batch. Files exceeding these limits are rejected before processing. |
| FR-IMP-009 | The import dialog UI SHALL show: step 1 = template download, step 2 = file selection + preview (first 10 rows), step 3 = import result summary with downloadable error report. |

---

## 4. Key Entities
Has an optional one-to-one relationship with AccountObjectEmployeeProfile (when Employee bit is set)
- Referenced by: voucher detail lines (SA, PU, BA, GL), sub-ledger reports

### AccountObjectBankAccount (Tài khoản ngân hàng)
- Child entity of AccountObject; each AccountObject may have 0..N bank accounts
- Used in payment vouchers to auto-populate beneficiary bank details

### AccountObjectEmployeeProfile (Thông tin nhân viên — DD-007)
- 1:1 optional extension of AccountObject (only when Employee bit = 4 is set)
- Contains HR-lite fields: CitizenId (CCCD/CMND, unique per tenant), DateOfBirth, Gender, SocialInsuranceNumber (unique per tenant), HireDate, DepartmentId (FK → Department), DependentCount
- Used for PIT (TT111/2013) and BHXH (TT59/2015-BHXH) compliance in PA module

### InventoryItem (Hàng hóa / Vật tư / Dịch vụ)
- Represents items tracked in inventory
- References Unit (đơn vị tính), InventoryItemCategory, FormulaTemplate (BOM), PanelUnit
- ItemType enum: RawMaterial(0), FinishedProduct(1), Goods(2), Service(3)
- UnitPrice (default purchase price), SalePrice1/2/3 (3-tier sale prices), DefaultTaxRate (VAT)
- Tracking flags: IsFollowSerial, IsFollowLot, IsFollowExpiry, IsPanelItem
- CostingMethod determines how outward price is computed in stock transactions
- Has child collections: UnitConverts, Barcodes, ItemAttributes
- Referenced by: PU invoice lines, SA invoice lines, IN warehouse entries

### InventoryItemUnitConvert (Đơn vị tính phụ — Gap I)
- Per-item secondary unit definitions with ConvertRate to main unit
- Flags: IsDefaultSaleUnit, IsDefaultPurchaseUnit

### InventoryItemBarcode (Mã vạch — Gap P)
- Multi-barcode/QR per item per unit (BarcodeType: Code128/EAN13/EAN8/QRCode/DataMatrix/UPC_A)
- BarcodeValue unique per tenant (cross-item duplicate = error)

### InventoryQuantityFormulaTemplate + Detail (BOM — Gap J)
- Master template for Bill of Materials (finished products)
- Detail lines: MaterialItemId (FK → InventoryItem), Quantity, UnitId

### ItemAttributeType + InventoryItemAttribute (Thuộc tính — Gap N)
- Tenant-level attribute type definitions (e.g., "Thương hiệu", "Màu sắc")
- Per-item attribute values; one item + one type = one value

### InventoryItemOpeningBalance (Số dư tồn kho đầu kỳ — DD-006)
- Flat master data table for DI setup: per item per warehouse per unit
- Fields: Quantity, UnitCost, Amount (auto-calc), CurrencyId, ForeignAmount, ExchangeRate, OpeningDate
- Later batch-converted to INInward vouchers when IN module is built

### InventoryItemCategory (Nhóm hàng hóa)
- Self-referential tree (ParentId), maximum 5 levels
- IsActive + SortOrder for display control
- Used for filtering in the item list and for reporting by category

### Currency (Loại tiền tệ)
- ISO 4217 codes; VND is the base currency (exchange rate = 1)
- Other currencies store the rate relative to VND
- Referenced by: all vouchers (multi-currency support)

### Unit (Đơn vị tính)
- Referenced by InventoryItem.UnitId, InventoryItemUnitConvert, FormulaDetail
| SC-011 | Employee profile data (CitizenId, BHXH, DOB, etc.) persists correctly on save/reload for AccountObjects with Employee bit set. |
| SC-012 | Opening stock balance Amount auto-calculates as Quantity × UnitCost on blur and on save. |
- Unidirectional FK from consumers → Unit (no navigation collection on Unit)

### Warehouse / Stock (Kho hàng)
- Physical or virtual storage location
- Referenced by IN (inventory) module vouchers and InventoryItemOpeningBalance
### Unit (Đơn vị tính)
- Referenced by InventoryItem.UnitId and voucher detail lines

### Warehouse / Stock (Kho hàng)
- Physical or virtual storage location
- Referenced by IN (inventory) module vouchers

### Department / OrganizationUnit (Bộ phận / Phòng ban)
- Self-referential tree (ParentId), maximum 5 levels
- Referenced by: employee records, cost allocation, voucher dimension tracking

### ExpenseItem (Khoản mục chi phí)
- Maps to a specific accounting account; used in expense vouchers for detailed cost classification

---

## 5. Success Criteria

### Measurable Outcomes

| ID | Criterion |
|----|-----------|
| SC-001 | Accountant can create a complete AccountObject (with 1 bank account) in under 2 minutes without training. |
| SC-002 | The Account Objects list with 10,000 records loads and displays the first page in under 1 second. |
| SC-003 | The Account Objects list search returns filtered results in under 500 milliseconds after the user stops typing. |
| SC-004 | The Inventory Items list with category tree and 50,000 items responds to a category filter in under 1 second. |
| SC-005 | An Excel import batch of 1,000 AccountObject rows completes in under 30 seconds. |
| SC-006 | An Excel import batch of 1,000 InventoryItem rows completes in under 30 seconds. |
| BR-DI07 | An AccountObjectEmployeeProfile CitizenId MUST be unique per tenant (partial unique index, NULLs allowed). Duplicate CitizenId → 409 Conflict "Số CMND/CCCD đã tồn tại". |
| BR-DI08 | An AccountObjectEmployeeProfile SocialInsuranceNumber MUST be unique per tenant (partial unique index, NULLs allowed). Duplicate → 409 Conflict "Mã số BHXH đã tồn tại". |
| BR-IN03 | InventoryItemOpeningBalance (TenantId, InventoryItemId, WarehouseId, UnitId) MUST be unique. Amount is auto-calculated as Quantity × UnitCost on save. |
| BR-IN04 | Service items (ItemType=3) CANNOT have opening stock balances; the Opening Stock tab is hidden for such items. |
| BR-IN05 | InventoryItemBarcode BarcodeValue MUST be unique per tenant (cross-item). Duplicate → 409 Conflict "Mã vạch đã được sử dụng bởi hàng hóa khác". |
| BR-IN06 | InventoryItemUnitConvert: one item cannot have two rows with the same UnitId. The secondary UnitId must differ from the item's main UnitId. |
| SC-007 | 95% of import errors are reported with a row number and a human-readable Vietnamese error message that identifies the specific field and problem. |
| SC-008 | All keyboard shortcuts (Ctrl+S, Ctrl+Shift+S, F3, Insert, Ctrl+Delete) function correctly in both the AccountObject and InventoryItem forms. |
| SC-009 | Seeded lookup data (VND currency, 10 default units) is present on every new tenant after initialization without any manual setup. |
| SC-010 | The dirty-form guard prevents data loss in 100% of navigations away from an unsaved form. |

---

## 6. Assumptions

- **Multi-currency**: Opening balances and credit limits are entered per currency. The base currency for each tenant is VND unless explicitly configured otherwise.
- **ObjectType bitmask**: The frontend will represent this as a set of checkboxes (one per type). The minimum valid value is 1 (at least one type must be selected).
- **Costing method default**: Default costing method for new inventory items is Weighted Average (3), consistent with the observed database default (DefaultCostMethod=0 in source system maps to Weighted Average per BR-IN02). The field is stored per item, allowing different items to use different methods.
- **Import duplicate handling**: On code conflict, the default behavior is Skip (do not overwrite). An "overwrite" option may be added in a future iteration.
- **Excel library**: EPPlus or ClosedXML will be used server-side for reading Excel files. The choice is a backend implementation detail and does not affect this spec.
- **Category depth enforcement**: The 5-level maximum for both InventoryItemCategory and Department matches the observed source system behavior. No technical constraint prevents deeper nesting; the limit is enforced by business validation.
- **Lookup data screens**: The UI for lookup entities (Currency, Unit, Warehouse, Department, ExpenseItem) is intentionally simple compared to AccountObject/InventoryItem. Inline edit within DataTable is sufficient; no complex form tabs are needed.
- **AccountObject group**: The AccountObjectGroup entity (grouping customers/vendors into segments) is out of scope for this step. It will be added in a future spec when CRM/segmentation features are addressed.
- **Opening balance**: Opening balance per currency is stored at the AccountObject level for AR/AP sub-ledger initialization. General Ledger opening balances are managed separately in a different step.
- **Tenant data isolation**: All entities have TenantId. The ApplicationDbContext global query filter ensures strict data isolation without requiring explicit WHERE clauses in queries.
- **Keyboard shortcuts**: The registered shortcuts follow the constitution (`copilot-instructions.md`). Ctrl+P (Print) is noted in the constitution but deferred from this step as print templates are not yet designed.
- **Warehouse scope**: Warehouse CRUD is fully included in step 3.5 of this feature. It is NOT deferred to the IN (Inventory) module. The IN module will reference Warehouse via FK but will not own its management UI.
- **Import transaction model**: The import pipeline uses a best-effort model (no batch-level transaction). Valid rows are committed individually; invalid rows are skipped and reported. This is the confirmed design (see Design Decisions §7).

---

## 7. Business Rules

| ID | Rule |
|----|------|
| BR-DI01 | An AccountObject MUST have at least one ObjectType bit set (ObjectType ≥ 1). Saving with ObjectType = 0 is blocked with validation error. |
| BR-DI02 | ObjectCode uniqueness is enforced per tenant. An attempt to save a

### DD-006 — InventoryItemOpeningBalance (FR-IN-040..044)
**Decision**: Flat master data table in DI module for opening stock setup.
- Per item × per warehouse × per unit → one row with Quantity, UnitCost, Amount (auto-calc).
- Multi-currency support: CurrencyId, ForeignAmount, ExchangeRate (nullable — null = VND).
- Later (when IN module is built): a batch job converts these rows into proper INInward vouchers (RefType=OpeningInventoryEntry) that post to InventoryLedger.
- **Rationale**: During DI setup, the IN module doesn't exist yet. A flat table is simpler for data entry. The conversion to vouchers is a one-time migration when IN module goes live.
- **Research source**: Inhongha uses `INInwardDetail` + `InventoryLedger` for opening stock, tied to a specific voucher (RefType=601, OpeningInventoryEntry).

### DD-007 — AccountObjectEmployeeProfile (FR-AO-030..034)
**Decision**: 1:1 optional extension table for employee HR-lite data.
- 7 fields: CitizenId (CCCD/CMND), DateOfBirth, Gender (Male/Female/Other), SocialInsuranceNumber (BHXH), HireDate, DepartmentId (FK → Dep
- Q: How should inventory opening stock balances be stored during DI setup (before IN module exists)? → A: Flat master data table `InventoryItemOpeningBalance`; later batch-convert to INInward vouchers (DD-006).
- Q: What employee data should be stored on AccountObject for PIT/BHXH compliance? → A: Minimal 7 fields in a 1:1 extension table `AccountObjectEmployeeProfile` (DD-007).artment), DependentCount.
- Partial unique indexes for CitizenId and SocialInsuranceNumber per tenant (NULLs allowed).
- Minimal scope — just enough for PIT (TT111/2013) and BHXH (TT59/2015-BHXH) compliance.
- Full HR module fields (bank info, education, position) are deferred to PA module.
- **Rationale**: Employee data is needed for payroll tax declarations before PA module is built. Extension table avoids polluting the core AccountObject entity. duplicate code within the same tenant returns a 409 Conflict. |
| BR-DI03 | Deleting an AccountObject that is referenced by any posted voucher is blocked (hard-delete prevention). Soft-deactivation (Status = Inactive) is always allowed. |
| BR-DI04 | **ObjectType is a bitmask integer**: bit 1 = Customer (Khách hàng), bit 2 = Vendor (Nhà cung cấp), bit 4 = Employee (Nhân viên). A single AccountObject MAY have multiple bits set simultaneously. Example: value 3 = Customer + Vendor; value 5 = Customer + Employee; value 7 = all three types. The UI represents this as three independent checkboxes. |
| BR-DI05 | InventoryItemCategory and Department trees are limited to a maximum of 5 nesting levels. Attempting to create a 6th-level node is blocked with a validation error. |
| BR-DI06 | Import rows with a code that already exists in the tenant are **skipped** (not overwritten). The error report lists them as "Mã đã tồn tại". An overwrite option is out of scope for this iteration. |
| BR-IN01 | Default CostingMethod for new InventoryItems is Weighted Average (3). |
| BR-IN02 | CostingMethod values: 1=FIFO, 2=LIFO, 3=Weighted Average, 4=Specific Identification. |

---

## 8. Design Decisions

The following decisions were confirmed by the user on **2026-04-18** and supersede any earlier ambiguous or contradicting text in this spec.

### DD-001 — Import Transaction Model (FR-IMP-006)
**Decision**: Option B — Best-effort (no batch transaction).
- Valid rows are committed individually as they are processed.
- Invalid rows are skipped and reported in the error list without affecting other rows.
- There is no batch-level transaction rollback.
- **Rationale**: Simplifies implementation, avoids locking large batches, and gives users partial results immediately. Error reporting is still complete.

### DD-002 — AccountObject Type Bitmask (BR-DI04)
**Decision**: YES — multi-type is allowed.
- `ObjectType` is a bitmask integer: 1=Customer, 2=Vendor, 4=Employee.
- One object may carry multiple types simultaneously (e.g., 3 = Customer+Vendor).
- **UI**: Three independent checkboxes in the form (not a single-select dropdown).
- **Filter**: The list filter applies a bitwise AND — returns objects where `ObjectType & selectedBits != 0`.

### DD-003 — Category Tree Depth
**Decision**: 5 levels maximum (unchanged from current spec).
- Applies to both InventoryItemCategory (BR-DI05) and Department/OrganizationUnit.
- Enforced by server-side validation; the 6th level is blocked.

### DD-004 — Opening Balance Currency
**Decision**: YES — multi-currency opening balances per AccountObject.
- An AccountObject may have one opening balance row per currency.
- Database table: **account_object_opening_balance** (`account_object_id`, `currency_id`, `debit_amount`, `credit_amount`).
- This supports AR/AP sub-ledger initialization for businesses with foreign-currency receivables/payables.

### DD-005 — Warehouse Scope
**Decision**: Full Warehouse CRUD is included in **step 3.5** of this DI feature.
- Warehouse management (list + form: WarehouseCode, WarehouseName, Address, ManagerName) is implemented here.
- It is NOT deferred to the IN (Inventory) module.
- The IN module references Warehouse by FK but does not own its management UI.

---

## 9. Clarifications

### Session 2026-04-18

- Q: What transaction model should the Excel import use? → A: Best-effort — valid rows committed individually, invalid rows skipped and reported (Option B, DD-001).
- Q: Should AccountObject support multiple simultaneous types? → A: Yes — ObjectType is a bitmask; multi-type allowed; UI uses checkboxes (DD-002).
- Q: What is the maximum nesting depth for InventoryItemCategory? → A: 5 levels maximum (confirmed, DD-003).
- Q: Should AccountObject opening balances support multiple currencies? → A: Yes — per-currency table account_object_opening_balance (DD-004).
- Q: Is Warehouse CRUD part of this feature or deferred to IN module? → A: Included in step 3.5 of this DI feature, not deferred (DD-005).
