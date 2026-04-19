# Tasks: DI Master Data — Account Objects & Items (Part 2)

**Feature**: `di-master-data`  
**Generated**: 2026-04-18  
**Input**: spec.md (US1–US11), plan.md (v4), data-model.md (19 entities), contracts/ (4 files)  
**Depends On**: `feature/di-account-tree` (ApplicationDbContext, AuditableEntity, ITenantEntity)  
**Tech Stack**: .NET 10 / C# 12, EF Core 10, PostgreSQL 16, Angular 20, PrimeNG 20, NgRx Signals

---

## Format Legend

- **[P]**: Can run in parallel (different files, no overlapping dependencies)
- **[USn]**: User Story number from spec.md (US1–US11)
- **Complexity**: S=half-day, M=1 day, L=2 days, XL=3+ days
- **Dependencies**: listed as `deps:` inline per task

---

## Phase T0: Shared Toolbar Components (FR-TB-001…024)

**Purpose**: Three reusable Angular standalone components used by ALL list screens. Must be built before T10/T11/T12 frontend phases.

**Path**: `src/webapp/src/app/shared/components/toolbar/`

**⚠️ NOTE**: T10, T11, T12 frontend phases depend on T0 being complete.

### T0.1 GridActionBar Component
- [x] T0.1 Create `GridActionBarComponent` standalone component `src/webapp/src/app/shared/components/toolbar/grid-action-bar/`
  - **Description**: Standalone Angular component. Inputs (all Signal-based): `selectedCount: InputSignal<number>`, `showDuplicate: InputSignal<boolean> = false`, `showImport: InputSignal<boolean> = false`, `showExport: InputSignal<boolean> = false`, `showPrint: InputSignal<boolean> = false`. Outputs: `addClick`, `editClick`, `deleteClick`, `duplicateClick`, `importClick`, `exportClick`, `printClick` (all `OutputEmitterRef<void>`). Button layout: Thêm mới (primary) · Sửa · Xóa (danger) · divider · Nhập Excel · Xuất Excel · divider · In. `ng-content` slot after In button. Sửa/Xóa disabled when `selectedCount() === 0`. Responsive: <960px hide labels (icon-only + `pTooltip`); <768px optional buttons collapse to overflow `p-menu`.
  - **Acceptance**: Renders 3 standard buttons. Sửa+Xóa disabled at selectedCount=0, enabled at 1+. Optional buttons hidden by default, shown when flag true. `ng-content` renders host-injected controls. Emits correct event on each click. Overflow menu appears below 768px.
  - **Mockup ref**: `.specify/mockups/di-master-data/toolbar-components.html#section-1`
  - **Complexity**: M

### T0.2 DateRangeBar Component
- [x] T0.2 Create `DateRangeBarComponent` standalone component `src/webapp/src/app/shared/components/toolbar/date-range-bar/`
  - **Description**: Inputs: `defaultPreset: InputSignal<string> = 'Đầu tháng đến hiện tại'`. Output: `rangeChange: OutputEmitterRef<{ fromDate: Date; toDate: Date; preset: string }>`. Preset dropdown options grouped: Ngày (Hôm nay / Hôm qua) · Tuần (Tuần này / Tuần trước) · Tháng (Tháng này / Tháng trước / Đầu tháng đến hiện tại) · Quý (Đầu quý đến hiện tại / Quý này / Quý trước) · Năm (Đầu năm đến hiện tại / Năm nay / Năm trước / 6 tháng đầu năm / 6 tháng cuối năm) · Tháng cụ thể (Tháng 1…Tháng 12) · Tùy chỉnh. Selecting a preset auto-calculates and fills fromDate/toDate using current year. Manually editing either date switches preset to "Tùy chỉnh". Data emitted ONLY on "Lấy dữ liệu" button click — no auto-emit on date change. Use `p-calendar` (PrimeNG) for date pickers, `p-dropdown` for preset. Default on mount: apply defaultPreset and pre-fill dates.
  - **Acceptance**: Preset dropdown shows all groups and 12 individual months. Selecting "Đầu tháng đến hiện tại" fills from=first-of-month, to=today. Editing date field sets preset to "Tùy chỉnh". "Lấy dữ liệu" emits `rangeChange` with correct values. No emission on date edit alone.
  - **Mockup ref**: `.specify/mockups/di-master-data/toolbar-components.html#section-2`
  - **Complexity**: M

### T0.3 SearchBar Component
- [x] T0.3 Create `SearchBarComponent` standalone component `src/webapp/src/app/shared/components/toolbar/search-bar/`
  - **Description**: Input: `placeholder: InputSignal<string> = 'Nhập từ khóa tìm kiếm...'`. Output: `search: OutputEmitterRef<{ keyword: string }>`. Layout: text input + "Tìm kiếm" button + `ng-content slot="filters"`. Search triggers on Enter keydown OR button click. When input non-empty show × icon inside field; clicking × clears value and emits `{ keyword: '' }`. No debounce/auto-search.
  - **Acceptance**: Enter triggers emit. Button triggers emit. × appears when text present, clears and emits empty. Host-injected `ng-content` renders after button. No emission on every keystroke.
  - **Mockup ref**: `.specify/mockups/di-master-data/toolbar-components.html#section-3`
  - **Complexity**: S

### T0.4 Barrel export
- [x] T0.4 [P] Create `src/webapp/src/app/shared/components/toolbar/index.ts` barrel exporting all 3 components. Add components to `src/webapp/src/app/shared/shared.module.ts` (or equivalent shared barrel) so all feature modules can import with single path.
  - **Acceptance**: `import { GridActionBarComponent, DateRangeBarComponent, SearchBarComponent } from '@shared/toolbar'` works from any feature module.
  - **deps**: T0.1, T0.2, T0.3
  - **Complexity**: S

---

## Phase T1: Domain Entities (19 Entities + Enums)

**Purpose**: All domain entities and enums must exist before Application or Infrastructure layers.

**⚠️ CRITICAL**: All subsequent phases depend on this phase being complete.

### T1 Enums & Value Objects (Parallel batch)

- [X] T1.1 [P] Create `ObjectType` bitmask constants (1=Customer, 2=Vendor, 4=Employee) as static class `src/PhanMemKeToan.Domain/Enums/ObjectType.cs`
  - **Description**: Static class (not enum) with `const int Customer = 1; Vendor = 2; Employee = 4;` and helper `IsValid(int flags)` returning `flags >= 1 && flags <= 7`. This is a bitmask — not a standard enum.
  - **Acceptance**: `ObjectType.IsValid(3)` → true. `ObjectType.IsValid(0)` → false. Class in namespace `PhanMemKeToan.Domain.Enums`.
  - **Complexity**: S

- [X] T1.2 [P] Create `InventoryItemType` enum `src/PhanMemKeToan.Domain/Enums/InventoryItemType.cs`
  - **Description**: `RawMaterial=0, FinishedProduct=1, Goods=2, Service=3`. Add XML doc comment per value explaining GL account + stock movement behavior (BR-A).
  - **Acceptance**: Enum compiles; values match data-model.md §2.
  - **Complexity**: S

- [X] T1.3 [P] Create `CostingMethod` enum `src/PhanMemKeToan.Domain/Enums/CostingMethod.cs`
  - **Description**: `FIFO=1, LIFO=2, WeightedAverage=3, SpecificIdentification=4`. WeightedAverage is the system default (BR-IN01).
  - **Acceptance**: Enum compiles; WeightedAverage=3.
  - **Complexity**: S

- [X] T1.4 [P] Create `BarcodeType` enum `src/PhanMemKeToan.Domain/Enums/BarcodeType.cs`
  - **Description**: `Code128=0, EAN13=1, EAN8=2, QRCode=3, DataMatrix=4, UPC_A=5`.
  - **Acceptance**: Enum compiles.
  - **Complexity**: S

- [X] T1.5 [P] Create `Gender` enum `src/PhanMemKeToan.Domain/Enums/Gender.cs`
  - **Description**: `Male=1, Female=2, Other=3`. Used by AccountObjectEmployeeProfile (DD-007).
  - **Acceptance**: Enum compiles.
  - **Complexity**: S

### T1 Lookup Entities (Parallel batch — no inter-dependencies)

- [X] T1.6 [P] Create `Currency` entity `src/PhanMemKeToan.Domain/Entities/Currency.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `CurrencyCode string(10)`, `CurrencyName string(100)`, `CurrencyNameEnglish string?(100)`, `Symbol string(10)`, `ExchangeRate decimal(18,2)`, `IsActive bool=true`. Navigation: `ICollection<AccountObjectOpeningBalance> OpeningBalances`.
  - **Acceptance**: Compiles. No hardcoded values. Matches data-model.md §5.
  - **Complexity**: S

- [X] T1.7 [P] Create `Unit` entity `src/PhanMemKeToan.Domain/Entities/Unit.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `UnitCode string(25)`, `UnitName string(100)`, `IsActive bool=true`. Navigation: `ICollection<InventoryItem> Items`, `ICollection<InventoryItemUnitConvert> UnitConverts`, `ICollection<InventoryItemOpeningBalance> OpeningBalances`.
  - **Acceptance**: Compiles. Matches contracts/lookups-api.md §2.
  - **Complexity**: S

- [X] T1.8 [P] Create `Warehouse` entity `src/PhanMemKeToan.Domain/Entities/Warehouse.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `WarehouseCode string(25)`, `WarehouseName string(255)`, `Address string?(500)`, `IsActive bool=true`. Navigation: `ICollection<InventoryItemOpeningBalance> OpeningBalances`.
  - **Acceptance**: Compiles. Matches lookups-api.md §3.
  - **Complexity**: S

- [X] T1.9 [P] Create `Department` entity `src/PhanMemKeToan.Domain/Entities/Department.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `DepartmentCode string(25)`, `DepartmentName string(255)`, `ParentId Guid?` (self-referential FK), `Level int=1` (1–5, computed on write). `IsActive bool=true`. Navigation: `Parent`, `ICollection<Department> Children`, `ICollection<AccountObjectEmployeeProfile> EmployeeProfiles`.
  - **Acceptance**: Compiles. Self-referential nav properties present.
  - **Complexity**: S

- [X] T1.10 [P] Create `ExpenseItem` entity `src/PhanMemKeToan.Domain/Entities/ExpenseItem.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `ExpenseCode string(25)`, `ExpenseName string(255)`, `IsActive bool=true`.
  - **Acceptance**: Compiles.
  - **Complexity**: S

### T1 AccountObject Cluster (Sequential within cluster, parallel to other clusters)

- [X] T1.11 Create `AccountObjectGroup` entity `src/PhanMemKeToan.Domain/Entities/AccountObjectGroup.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `GroupCode string(25)`, `GroupName string(255)`, `ObjectType int` (bitmask same as AccountObject), `IsActive bool=true`. Navigation: `ICollection<AccountObject> AccountObjects`. XML doc: "CRUD deferred to later sprint (BR-E). FK already present on AccountObject."
  - **Acceptance**: Compiles.
  - **deps**: T1.1
  - **Complexity**: S

- [X] T1.12 Create `AccountObject` entity `src/PhanMemKeToan.Domain/Entities/AccountObject.cs`
  - **Description**: Extends `AuditableEntity`. Full properties per data-model.md §1: `ObjectCode(25)`, `ObjectName(255)`, `ObjectNameEnglish?(255)`, `Address?(500)`, `TaxCode?(50)`, `Email?(255)`, `Phone?(50)`, `Fax?(50)`, `Website?(255)`, `ContactPerson?(255)`, `ContactPhone?(50)`, `Description?`, `ObjectType int`, `CreditLimit decimal(18,2)`, `PaymentTermDays int`, `IsActive bool=true`, `RowVersion int`, `AccountObjectGroupId Guid?`. Navigation: `AccountObjectGroup?`, `ICollection<AccountObjectBankAccount> BankAccounts`, `ICollection<AccountObjectOpeningBalance> OpeningBalances`, `AccountObjectEmployeeProfile? EmployeeProfile`.
  - **Acceptance**: Compiles. All nav properties declared. RowVersion field present (optimistic concurrency BR-DI02).
  - **deps**: T1.11
  - **Complexity**: M

- [X] T1.13 Create `AccountObjectBankAccount` entity `src/PhanMemKeToan.Domain/Entities/AccountObjectBankAccount.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `AccountObjectId Guid`, `BankName string(255)`, `BankBranch string?(255)`, `AccountNumber string(50)`, `SwiftCode string?(20)`. Navigation: `AccountObject AccountObject`.
  - **Acceptance**: Compiles. FK to AccountObject declared.
  - **deps**: T1.12
  - **Complexity**: S

- [X] T1.14 Create `AccountObjectOpeningBalance` entity `src/PhanMemKeToan.Domain/Entities/AccountObjectOpeningBalance.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `AccountObjectId Guid`, `CurrencyId Guid`, `DebitAmount decimal(18,0)`, `CreditAmount decimal(18,0)`, `DebitAmountOC decimal(18,3)`, `CreditAmountOC decimal(18,3)`, `ExchangeRate decimal(18,2)=1`. Navigation: `AccountObject`, `Currency`.
  - **Acceptance**: Compiles. FK to both AccountObject and Currency.
  - **deps**: T1.12, T1.6
  - **Complexity**: S

- [X] T1.15 Create `AccountObjectEmployeeProfile` entity `src/PhanMemKeToan.Domain/Entities/AccountObjectEmployeeProfile.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `AccountObjectId Guid` (PK+FK, unique — 1:1), `CitizenId string?(20)`, `DateOfBirth DateTime?`, `Gender Gender?`, `SocialInsuranceNumber string?(10)`, `HireDate DateTime?`, `DepartmentId Guid?`, `DependentCount int=0`. Navigation: `AccountObject`, `Department?`. XML doc per data-model.md DD-007.
  - **Acceptance**: Compiles. 1:1 relationship pattern (AccountObjectId as unique FK). Matches DD-007 specification.
  - **deps**: T1.12, T1.5, T1.9
  - **Complexity**: M

### T1 InventoryItem Cluster

- [X] T1.16 Create `InventoryItemCategory` entity `src/PhanMemKeToan.Domain/Entities/InventoryItemCategory.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `CategoryCode string(25)`, `CategoryName string(255)`, `ParentId Guid?`, `Level int=1` (1–5), `IsActive bool=true`, `SortOrder int`. Navigation: `Parent?`, `ICollection<InventoryItemCategory> Children`, `ICollection<InventoryItem> Items`.
  - **Acceptance**: Compiles. Self-referential tree structure.
  - **Complexity**: S

- [X] T1.17 Create `ItemAttributeType` entity `src/PhanMemKeToan.Domain/Entities/ItemAttributeType.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `AttributeCode string(50)`, `AttributeName string(255)`, `IsActive bool=true`. Navigation: `ICollection<InventoryItemAttribute> ItemAttributes`.
  - **Acceptance**: Compiles.
  - **Complexity**: S

- [X] T1.18 Create `InventoryQuantityFormulaTemplate` entity `src/PhanMemKeToan.Domain/Entities/InventoryQuantityFormulaTemplate.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `TemplateCode string(50)`, `TemplateName string(255)`, `Description string?`, `IsActive bool=true`. Navigation: `ICollection<InventoryQuantityFormulaDetail> Details`, `ICollection<InventoryItem> Items` (back-nav for SET NULL on delete).
  - **Acceptance**: Compiles.
  - **Complexity**: S

- [X] T1.19 Create `InventoryItem` entity `src/PhanMemKeToan.Domain/Entities/InventoryItem.cs`
  - **Description**: Extends `AuditableEntity`. Full properties per data-model.md §2: `ItemCode(25)`, `ItemName(255)`, `ItemNameEnglish?(255)`, `Description?`, `Barcode?(255)`, `UnitId Guid`, `CategoryId Guid?`, `CostingMethod` (default WeightedAverage), `ItemType` (default Goods), `DefaultTaxRate decimal?(5,2)`, `UnitPrice decimal?(18,2)`, `MinStockLevel decimal(18,2)`, `MaxStockLevel decimal(18,2)`, `LeadTimeDays int`, tracking flags (`IsFollowSerial`, `IsFollowLot`, `IsFollowExpiry`, `IsPanelItem`), `PanelUnitId Guid?`, `FormulaTemplateId Guid?`, `SalePrice1/2/3 decimal?(18,2)`, `IsActive bool=true`, `RowVersion int`. Navigation: `Unit`, `PanelUnit?` (DUAL FK — explicit config required), `Category?`, `FormulaTemplate?`, `ICollection<InventoryItemUnitConvert> UnitConverts`, `ICollection<InventoryItemBarcode> Barcodes`, `ICollection<InventoryItemAttribute> ItemAttributes`, `ICollection<InventoryItemOpeningBalance> OpeningBalances`.
  - **Acceptance**: Compiles. Both `UnitId`→`Unit` and `PanelUnitId`→`PanelUnit` navigations declared. RowVersion present.
  - **deps**: T1.2, T1.3, T1.7, T1.16, T1.17, T1.18
  - **Complexity**: L

- [X] T1.20 Create `InventoryItemUnitConvert` entity `src/PhanMemKeToan.Domain/Entities/InventoryItemUnitConvert.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `InventoryItemId Guid`, `UnitId Guid` (alternate/child unit), `ConvertRate decimal(18,6)` (how many main units = 1 child unit). Navigation: `InventoryItem`, `Unit`.
  - **Acceptance**: Compiles. FK to both InventoryItem and Unit.
  - **deps**: T1.19
  - **Complexity**: S

- [X] T1.21 Create `InventoryQuantityFormulaDetail` entity `src/PhanMemKeToan.Domain/Entities/InventoryQuantityFormulaDetail.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `FormulaTemplateId Guid`, `MaterialItemId Guid` (FK → InventoryItem), `UnitId Guid`, `Quantity decimal(18,6)`, `SortOrder int`. Navigation: `FormulaTemplate`, `MaterialItem (InventoryItem)`, `Unit`.
  - **Acceptance**: Compiles. FKs declared.
  - **deps**: T1.18, T1.19
  - **Complexity**: S

- [X] T1.22 Create `InventoryItemBarcode` entity `src/PhanMemKeToan.Domain/Entities/InventoryItemBarcode.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `InventoryItemId Guid`, `BarcodeValue string(255)`, `BarcodeType BarcodeType`. Navigation: `InventoryItem`.
  - **Acceptance**: Compiles.
  - **deps**: T1.4, T1.19
  - **Complexity**: S

- [X] T1.23 Create `InventoryItemAttribute` entity `src/PhanMemKeToan.Domain/Entities/InventoryItemAttribute.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `InventoryItemId Guid`, `AttributeTypeId Guid` (FK → ItemAttributeType), `AttributeValue string(500)`. Navigation: `InventoryItem`, `AttributeType`.
  - **Acceptance**: Compiles.
  - **deps**: T1.17, T1.19
  - **Complexity**: S

- [X] T1.24 Create `InventoryItemOpeningBalance` entity `src/PhanMemKeToan.Domain/Entities/InventoryItemOpeningBalance.cs`
  - **Description**: Extends `AuditableEntity`. Properties: `InventoryItemId Guid`, `WarehouseId Guid`, `UnitId Guid`, `Quantity decimal(18,6)`, `UnitCost decimal(18,2)`, `Amount decimal(18,0)` (computed = Qty×UnitCost, rounded to VND), `CurrencyId Guid?`, `ForeignAmount decimal?(18,3)`, `ExchangeRate decimal(18,2)=1`. Navigation: `InventoryItem`, `Warehouse`, `Unit`, `Currency?`. XML doc: "DD-006 — opening stock per warehouse. Service items (ItemType=3) must never have rows (BR-IN05)."
  - **Acceptance**: Compiles. All FKs declared.
  - **deps**: T1.19, T1.8, T1.7, T1.6
  - **Complexity**: M

---

## Phase T2: EF Core Configuration + Migration

**Purpose**: Wire up all 19 entities to ApplicationDbContext, configure constraints/indexes, run single migration.

**deps**: All of Phase T1

- [x] T2.1 Create EF Core configs for Lookup entities (parallel batch) `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/`
  - **Files** (create all 5):
    - `CurrencyConfiguration.cs` — table `currencies`, UQ `(TenantId, CurrencyCode)`, decimal precision `(18,2)` on ExchangeRate
    - `UnitConfiguration.cs` — table `units`, UQ `(TenantId, UnitCode)`
    - `WarehouseConfiguration.cs` — table `warehouses`, UQ `(TenantId, WarehouseCode)`
    - `DepartmentConfiguration.cs` — table `departments`, UQ `(TenantId, DepartmentCode)`, self-referential HasOne(Parent).WithMany(Children)
    - `ExpenseItemConfiguration.cs` — table `expense_items`, UQ `(TenantId, ExpenseCode)`
  - **Acceptance**: Each file implements `IEntityTypeConfiguration<T>`. Table names snake_case. Unique indexes defined.
  - **Complexity**: M

- [x] T2.2 [P] Create `AccountObjectGroupConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountObjectGroupConfiguration.cs`
  - **Description**: Table `account_object_groups`. UQ `(TenantId, GroupCode)`. TenantId query filter. IsDeleted filter.
  - **Complexity**: S
  - **deps**: T1.11

- [x] T2.3 Create `AccountObjectConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountObjectConfiguration.cs`
  - **Description**: Table `account_objects`. UQ `(TenantId, ObjectCode)`. Property precision: `CreditLimit decimal(18,2)`. `RowVersion` → `IsConcurrencyToken()`. HasOne(Group).WithMany(AccountObjects).HasForeignKey(AccountObjectGroupId).OnDelete(SetNull). HasMany(BankAccounts).WithOne().HasForeignKey(AccountObjectId).OnDelete(Cascade). HasMany(OpeningBalances).WithOne().HasForeignKey(AccountObjectId).OnDelete(Cascade). HasOne(EmployeeProfile).WithOne(a=>a.AccountObject).HasForeignKey<AccountObjectEmployeeProfile>(e=>e.AccountObjectId).OnDelete(Cascade). Global query filter: `e => !e.IsDeleted && e.TenantId == tenantId`.
  - **Acceptance**: Compiles. Concurrency token on RowVersion. Cascade deletes correct.
  - **deps**: T1.12, T2.2
  - **Complexity**: M

- [x] T2.4 [P] Create `AccountObjectBankAccountConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountObjectBankAccountConfiguration.cs`
  - **Description**: Table `account_object_bank_accounts`. HasOne(AccountObject).WithMany(BankAccounts).HasForeignKey(AccountObjectId).OnDelete(Cascade). IsDeleted query filter.
  - **Complexity**: S
  - **deps**: T1.13

- [x] T2.5 [P] Create `AccountObjectOpeningBalanceConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountObjectOpeningBalanceConfiguration.cs`
  - **Description**: Table `account_object_opening_balances`. UQ composite `(TenantId, AccountObjectId, CurrencyId)`. Decimal precision: `DebitAmount/CreditAmount decimal(18,0)`, `DebitAmountOC/CreditAmountOC decimal(18,3)`, `ExchangeRate decimal(18,2)`. HasOne(Currency).WithMany(OpeningBalances).OnDelete(Restrict).
  - **Complexity**: S
  - **deps**: T1.14

- [x] T2.6 [P] Create `AccountObjectEmployeeProfileConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountObjectEmployeeProfileConfiguration.cs`
  - **Description**: Table `account_object_employee_profiles`. HasIndex(e=>e.AccountObjectId).IsUnique() (1:1). Partial unique indexes: `(TenantId, CitizenId)` with filter `"CitizenId" IS NOT NULL AND "IsDeleted" = false`. `(TenantId, SocialInsuranceNumber)` with same filter. HasOne(Department).WithMany().HasForeignKey(DepartmentId).OnDelete(SetNull).
  - **Acceptance**: Partial indexes use `HasFilter(...)`. Matches data-model.md DD-007 EF config.
  - **Complexity**: M
  - **deps**: T1.15

- [x] T2.7 Create `InventoryItemCategoryConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/InventoryItemCategoryConfiguration.cs`
  - **Description**: Table `inventory_item_categories`. UQ `(TenantId, CategoryCode)`. Self-referential HasOne(Parent).WithMany(Children).HasForeignKey(ParentId).OnDelete(Restrict). IsActive, SortOrder indexed.
  - **Complexity**: S
  - **deps**: T1.16

- [x] T2.8 [P] Create `ItemAttributeTypeConfiguration.cs` and `InventoryQuantityFormulaTemplateConfiguration.cs`
  - **Files**:
    - `ItemAttributeTypeConfiguration.cs` — table `item_attribute_types`, UQ `(TenantId, AttributeCode)`
    - `InventoryQuantityFormulaTemplateConfiguration.cs` — table `inventory_quantity_formula_templates`, UQ `(TenantId, TemplateCode)`
  - **Complexity**: S
  - **deps**: T1.17, T1.18

- [x] T2.9 Create `InventoryItemConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs`
  - **Description**: Table `inventory_items`. UQ `(TenantId, ItemCode)`. **DUAL FK** for Unit: `HasOne(e=>e.Unit).WithMany(u=>u.Items).HasForeignKey(e=>e.UnitId).OnDelete(Restrict)` AND `HasOne(e=>e.PanelUnit).WithMany().HasForeignKey(e=>e.PanelUnitId).OnDelete(SetNull)` — both must use explicit `HasForeignKey`. Decimal precision: `UnitPrice/SalePrice1/2/3 decimal(18,2)`, `DefaultTaxRate decimal(5,2)`, `MinStockLevel/MaxStockLevel decimal(18,2)`. FormulaTemplate FK: OnDelete(SetNull). RowVersion → IsConcurrencyToken(). Global query filter: `!IsDeleted && TenantId==tenantId`.
  - **Acceptance**: Compiles. Dual-FK Unit relationship does NOT cause EF ambiguity error. RowVersion concurrency token set.
  - **deps**: T1.19, T2.7, T2.8
  - **Complexity**: L

- [x] T2.10 [P] Create remaining child entity configurations `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/`
  - **Files** (create all 4):
    - `InventoryItemUnitConvertConfiguration.cs` — table `inventory_item_unit_converts`, UQ `(TenantId, InventoryItemId, UnitId)`, `ConvertRate decimal(18,6)`
    - `InventoryQuantityFormulaDetailConfiguration.cs` — table `inventory_quantity_formula_details`, FK to FormulaTemplate OnDelete(Cascade), FK to MaterialItem(InventoryItem) OnDelete(Restrict), `Quantity decimal(18,6)`
    - `InventoryItemBarcodeConfiguration.cs` — table `inventory_item_barcodes`, UQ `(TenantId, BarcodeValue)` with filter `IsDeleted = false`
    - `InventoryItemAttributeConfiguration.cs` — table `inventory_item_attributes`, UQ `(TenantId, InventoryItemId, AttributeTypeId)`
  - **Complexity**: M
  - **deps**: T1.20, T1.21, T1.22, T1.23

- [x] T2.11 Create `InventoryItemOpeningBalanceConfiguration.cs` `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/InventoryItemOpeningBalanceConfiguration.cs`
  - **Description**: Table `inventory_item_opening_balances`. Composite UQ `(TenantId, InventoryItemId, WarehouseId, UnitId)`. Decimal precision: `Quantity decimal(18,6)`, `UnitCost decimal(18,2)`, `Amount decimal(18,0)`, `ForeignAmount decimal(18,3)`, `ExchangeRate decimal(18,2)`.
  - **Complexity**: S
  - **deps**: T1.24

- [x] T2.12 Modify `IApplicationDbContext` interface to add 19 new DbSet declarations `src/PhanMemKeToan.Application/Common/Interfaces/IApplicationDbContext.cs`
  - **Description**: Add `DbSet<AccountObject>`, `DbSet<AccountObjectGroup>`, `DbSet<AccountObjectBankAccount>`, `DbSet<AccountObjectOpeningBalance>`, `DbSet<AccountObjectEmployeeProfile>`, `DbSet<InventoryItem>`, `DbSet<InventoryItemCategory>`, `DbSet<InventoryItemUnitConvert>`, `DbSet<InventoryQuantityFormulaTemplate>`, `DbSet<InventoryQuantityFormulaDetail>`, `DbSet<InventoryItemBarcode>`, `DbSet<ItemAttributeType>`, `DbSet<InventoryItemAttribute>`, `DbSet<InventoryItemOpeningBalance>`, `DbSet<Currency>`, `DbSet<Unit>`, `DbSet<Warehouse>`, `DbSet<Department>`, `DbSet<ExpenseItem>`.
  - **Acceptance**: Interface compiles. All 19 DbSets present.
  - **deps**: All T1, T2.1–T2.11
  - **Complexity**: S

- [x] T2.13 Modify `ApplicationDbContext.cs` to add DbSets + apply configurations + global query filters `src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs`
  - **Description**: Add 19 `DbSet<T>` properties. In `OnModelCreating`: call `modelBuilder.ApplyConfiguration(new XConfiguration())` for all 19 configurations. Add global query filters per-entity using `HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantId)`. Do NOT break existing Account/AccountGroup filters from di-account-tree feature.
  - **Acceptance**: ApplicationDbContext compiles. `dotnet build` passes.
  - **deps**: T2.12, T2.1–T2.11
  - **Complexity**: M

- [x] T2.14 Add ClosedXML NuGet package to Infrastructure project `src/PhanMemKeToan.Infrastructure/PhanMemKeToan.Infrastructure.csproj`
  - **Description**: Run `dotnet add src/PhanMemKeToan.Infrastructure package ClosedXML`. Verify MIT license. Update csproj.
  - **Acceptance**: `dotnet restore` succeeds. ClosedXML appears in csproj PackageReference.
  - **Complexity**: S

- [x] T2.15 Create EF Core migration `AddMasterData` `src/PhanMemKeToan.Infrastructure/Persistence/Migrations/`
  - **Description**: Run `dotnet ef migrations add AddMasterData --project src/PhanMemKeToan.Infrastructure --startup-project src/PhanMemKeToan.Api`. Review generated migration: confirm 19 new tables, all indexes, FKs match data-model.md DDL. Apply migration to dev DB with `dotnet ef database update`.
  - **Acceptance**: Migration applies without error. `dotnet ef database update` succeeds on local PostgreSQL. 19 new tables present.
  - **deps**: T2.13, T2.14
  - **Complexity**: M

- [x] T2.16 Create seed data for VND currency and default units `src/PhanMemKeToan.Infrastructure/Persistence/SeedData/MasterDataSeedData.cs`
  - **Description**: Implement `IMasterDataSeedData`. Seed: VND currency (symbol=₫, rate=1, isActive=true). 10 default Vietnamese units per FR-LK-007: Cái, Chiếc, Hộp, Kg, Lít, M2, M3, Thùng, Bộ, Đôi. Seed runs only if table is empty (idempotent). Called from ApplicationDbContext startup in DI or from `Program.cs` seed block.
  - **Acceptance**: Running seed twice does not create duplicate records. VND always has exchangeRate=1.
  - **deps**: T2.15
  - **Complexity**: M

---

## Phase T3: Application Layer — AccountObject CQRS (US1 + US2 + US3 + US4 + US10)

**Purpose**: CQRS handlers for AccountObject list/detail/create/update/delete + DTOs.

**deps**: T2.12, T2.15

### T3 DTOs

- [x] T3.1 [P] Create AccountObject DTOs `src/PhanMemKeToan.Application/Features/AccountObjects/DTOs/`
  - **Files**:
    - `AccountObjectListItemDto.cs` — fields: Id, ObjectCode, ObjectName, ObjectType(int), TaxCode?, Phone?, IsActive, CreatedAt
    - `AccountObjectDetailDto.cs` — all fields + `List<BankAccountDto> BankAccounts`, `List<OpeningBalanceDto> OpeningBalances`, `EmployeeProfileDto? EmployeeProfile`, AccountObjectGroupId?, RowVersion
    - `BankAccountDto.cs` — Id, BankName, BankBranch?, AccountNumber, SwiftCode?
    - `OpeningBalanceDto.cs` — Id, CurrencyId, CurrencyCode, DebitAmount, CreditAmount, DebitAmountOC, CreditAmountOC, ExchangeRate
    - `EmployeeProfileDto.cs` — Id, CitizenId?, DateOfBirth?, Gender(int?), SocialInsuranceNumber?, HireDate?, DepartmentId?, DepartmentName?, DependentCount
  - **Acceptance**: All DTOs compile. Shapes match contracts/account-objects-api.md responses exactly.
  - **Complexity**: M

### T3 Query Handlers (US1)

- [x] T3.2 Create `GetAccountObjectsQuery` + Handler `src/PhanMemKeToan.Application/Features/AccountObjects/Queries/GetAccountObjects/`
  - **Description**: Query with params: `Page int=1`, `PageSize int=25`, `TypeFilter int=0` (bitmask), `Status string="all"`, `Search string?`, `SortBy string="objectCode"`, `SortDir string="asc"`. Handler: EF Core paginated query. Bitwise filter: `e.ObjectType & TypeFilter != 0` when TypeFilter > 0. Search: `EF.Functions.ILike(e.ObjectCode, pattern) || EF.Functions.ILike(e.ObjectName, pattern)`. Returns `PaginatedResult<AccountObjectListItemDto>`. Select only list-item fields (NO child collection loading).
  - **Acceptance**: Bitwise filter works for TypeFilter=1,2,4,3,5,6,7. TypeFilter=0 returns all. Pagination correct. Sort applies.
  - **deps**: T3.1
  - **Complexity**: M

- [x] T3.3 Create `GetAccountObjectByIdQuery` + Handler `src/PhanMemKeToan.Application/Features/AccountObjects/Queries/GetAccountObjectById/`
  - **Description**: Query param: `Id Guid`. Handler: Include BankAccounts, OpeningBalances (Include CurrencyCode via Currency nav), EmployeeProfile (Include Department for DepartmentName). Map to `AccountObjectDetailDto`. Return 404 if not found.
  - **Acceptance**: All child collections populated. EmployeeProfile null when Employee bit not set.
  - **deps**: T3.1
  - **Complexity**: M

### T3 Command Handlers (US2 + US3 + US4 + US10)

- [x] T3.4 Create `CreateAccountObjectCommand` + Handler `src/PhanMemKeToan.Application/Features/AccountObjects/Commands/CreateAccountObject/`
  - **Description**: Command contains all fields from POST request body (data-model + bank accounts + opening balances + optional employeeProfile). Handler logic:
    1. FluentValidation: ObjectCode required/max25, ObjectName required/max255, ObjectType >= 1 (BR-DI01)
    2. Unique check: `context.AccountObjects.AnyAsync(e=>e.TenantId==tenantId && e.ObjectCode==cmd.ObjectCode)` → 409 if exists
    3. Create `AccountObject` entity, cascade-add child collections
    4. If Employee bit set AND employeeProfile provided: create `AccountObjectEmployeeProfile` (DD-007); validate CitizenId/BHXH uniqueness
    5. `await context.SaveChangesAsync()`
    6. Return created Id + RowVersion
  - **Acceptance**: ObjectType=0 blocked. Duplicate code returns 409. EmployeeProfile created only when Employee bit set. Unique CitizenId/BHXH validated.
  - **deps**: T3.1
  - **Complexity**: L

- [x] T3.5 Create `UpdateAccountObjectCommand` + Handler `src/PhanMemKeToan.Application/Features/AccountObjects/Commands/UpdateAccountObject/`
  - **Description**: Command adds `Id Guid`, `RowVersion int` for concurrency. Handler logic:
    1. Load entity with all child nav properties
    2. Check `RowVersion` matches (BR-DI02 optimistic concurrency) → 409 if stale
    3. Block ObjectCode change if voucher references exist (check trans tables — placeholder count query for now, returns 422 with error code `code_has_references`)
    4. Full-replace BankAccounts: remove existing, add new from command
    5. Full-replace OpeningBalances: remove existing, add new
    6. EmployeeProfile: toggle Employee bit → auto-create profile (if newly set) or soft-delete (if newly unset, requires confirmation flag `confirmRemoveProfile=true` in command)
    7. Unique CitizenId/BHXH validation if updating EmployeeProfile
    8. SaveChangesAsync
  - **Acceptance**: Stale RowVersion returns 409. Employee bit toggle correctly creates/deletes profile. Bank accounts fully replaced.
  - **deps**: T3.4
  - **Complexity**: XL

- [x] T3.6 Create `DeleteAccountObjectCommand` + Handler `src/PhanMemKeToan.Application/Features/AccountObjects/Commands/DeleteAccountObject/`
  - **Description**: Command: `Id Guid`. Handler: Load entity. Check for hard references in transaction tables (placeholder — always allows soft-delete for now). Soft-delete: `entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow`. SaveChangesAsync.
  - **Acceptance**: Soft-delete sets IsDeleted=true. Record no longer returned in list query. Hard-delete not implemented.
  - **deps**: T3.4
  - **Complexity**: S

- [x] T3.7 [P] Create FluentValidation validators for AccountObject commands `src/PhanMemKeToan.Application/Features/AccountObjects/`
  - **Description**: `CreateAccountObjectCommandValidator.cs` and `UpdateAccountObjectCommandValidator.cs`. Rules per contracts/account-objects-api.md Validation table: ObjectCode required+max25, ObjectName required+max255, ObjectType must-be-geq-1, BankAccounts.AccountNumber required when row present, OpeningBalances.CurrencyId required, EmployeeProfile.DependentCount >=0, CitizenId max20, BHXH max10.
  - **Acceptance**: Invalid commands return ValidationException with field-level errors. Valid commands pass.
  - **deps**: T3.4, T3.5
  - **Complexity**: M

---

## Phase T4: Application Layer — InventoryItem CQRS (US5 + US6 + US11)

**Purpose**: CQRS handlers for InventoryItem + InventoryItemCategory.

**deps**: T2.12, T2.15

### T4 DTOs

- [ ] T4.1 [P] Create InventoryItem DTOs `src/PhanMemKeToan.Application/Features/InventoryItems/DTOs/`
  - **Files**:
    - `InventoryItemListItemDto.cs` — Id, ItemCode, ItemName, ItemType(int), UnitId, UnitCode, CategoryId?, CategoryName?, IsActive, UnitPrice?, SalePrice1?
    - `InventoryItemDetailDto.cs` — all fields + `List<UnitConvertDto>`, `List<BarcodeDto>`, `List<ItemAttributeDto>`, `List<InventoryItemOpeningBalanceDto>`, RowVersion
    - `UnitConvertDto.cs` — Id, UnitId, UnitCode, UnitName, ConvertRate
    - `BarcodeDto.cs` — Id, BarcodeValue, BarcodeType(int)
    - `ItemAttributeDto.cs` — Id, AttributeTypeId, AttributeCode, AttributeName, AttributeValue
    - `InventoryItemOpeningBalanceDto.cs` — Id, WarehouseId, WarehouseName, UnitId, UnitCode, Quantity, UnitCost, Amount, CurrencyId?, CurrencyCode?, ForeignAmount?, ExchangeRate
    - `CategoryTreeNodeDto.cs` — Id, CategoryCode, CategoryName, ParentId?, Level, IsActive, SortOrder, `List<CategoryTreeNodeDto> Children`
  - **Acceptance**: All DTOs compile. Shapes match contracts/inventory-items-api.md.
  - **Complexity**: M

- [ ] T4.2 Create `GetCategoryTreeQuery` + Handler `src/PhanMemKeToan.Application/Features/InventoryItemCategories/Queries/GetCategoryTree/`
  - **Description**: No params (returns full tree for tenant). Handler: Load all non-deleted categories ordered by Level, SortOrder. Build tree in-memory: first pass = dict by Id; second pass = assign Children. Return root nodes (ParentId=null). Map to `List<CategoryTreeNodeDto>`.
  - **Acceptance**: Nested tree structure correct. Root nodes returned. Performance acceptable for up to 1000 categories.
  - **deps**: T4.1
  - **Complexity**: M

- [ ] T4.3 [P] Create Category CRUD Commands + Handlers `src/PhanMemKeToan.Application/Features/InventoryItemCategories/Commands/`
  - **Files**:
    - `CreateCategoryCommand.cs + Handler` — validate: CategoryCode unique, Level ≤ 5 (BR-DI05), ParentId exists if provided; compute Level = parent.Level + 1; save
    - `UpdateCategoryCommand.cs + Handler` — validate same; cannot change ParentId to create circular reference
    - `DeleteCategoryCommand.cs + Handler` — block if has children OR has items (return 422 with counts)
  - **Acceptance**: Level validation enforced. Delete blocked when has_children or has_items. Circular reference prevented.
  - **deps**: T4.1
  - **Complexity**: M

- [ ] T4.4 Create `GetInventoryItemsQuery` + Handler `src/PhanMemKeToan.Application/Features/InventoryItems/Queries/GetInventoryItems/`
  - **Description**: Query params: `Page`, `PageSize`, `CategoryId Guid?`, `ItemType int?`, `Search string?`, `SortBy`, `SortDir`, `IsActive bool?`. Handler: If CategoryId provided, compute descendant category set via recursive CTE (PostgreSQL) or in-memory traversal (load full category list). Filter items. ILIKE search on ItemCode/ItemName. Returns `PaginatedResult<InventoryItemListItemDto>` (no child collections, include UnitCode + CategoryName via join).
  - **Acceptance**: CategoryId filter includes all descendants. ItemType filter works. Pagination correct.
  - **deps**: T4.1, T4.2
  - **Complexity**: L

- [ ] T4.5 Create `GetInventoryItemByIdQuery` + Handler `src/PhanMemKeToan.Application/Features/InventoryItems/Queries/GetInventoryItemById/`
  - **Description**: Load item with all child collections: UnitConverts (include UnitCode/Name), Barcodes, ItemAttributes (include AttributeCode/Name from AttributeType), OpeningBalances (include WarehouseName, UnitCode, CurrencyCode). Map to `InventoryItemDetailDto`.
  - **Acceptance**: All child collections populated correctly. Service items have empty OpeningBalances.
  - **deps**: T4.1
  - **Complexity**: M

- [ ] T4.6 Create `CreateInventoryItemCommand` + Handler `src/PhanMemKeToan.Application/Features/InventoryItems/Commands/CreateInventoryItem/`
  - **Description**: Command includes all fields + child collections (unitConverts, barcodes, attributes, openingBalances). Handler logic:
    1. Validate ItemCode unique per tenant
    2. Validate UnitId exists (required)
    3. Validate CategoryId exists if provided
    4. For Service items (ItemType=3): block if openingBalances provided (BR-IN04)
    5. Validate unique barcode values (UQ per tenant, check DB)
    6. Full-save all child collections atomically
    7. Return created Id
  - **Acceptance**: Service item blocks opening balance. Duplicate ItemCode returns 409. Duplicate barcode returns 422.
  - **deps**: T4.1
  - **Complexity**: L

- [ ] T4.7 Create `UpdateInventoryItemCommand` + Handler `src/PhanMemKeToan.Application/Features/InventoryItems/Commands/UpdateInventoryItem/`
  - **Description**: PUT semantics — full replace of child collections. RowVersion concurrency check. Same validation as Create. Warn (but allow) if main UnitId changes while UnitConverts exist — include `confirmUnitChange` flag in command. Service item still cannot have opening balances.
  - **Acceptance**: Stale RowVersion → 409. Child collections fully replaced. Unit change warning implemented.
  - **deps**: T4.6
  - **Complexity**: L

- [ ] T4.8 Create `DeleteInventoryItemCommand` + Handler `src/PhanMemKeToan.Application/Features/InventoryItems/Commands/DeleteInventoryItem/`
  - **Description**: Soft-delete. Block if referenced by voucher lines (placeholder check for now).
  - **Complexity**: S
  - **deps**: T4.6

- [ ] T4.9 [P] Create FluentValidation validators for InventoryItem commands `src/PhanMemKeToan.Application/Features/InventoryItems/`
  - **Description**: `CreateInventoryItemCommandValidator.cs`, `UpdateInventoryItemCommandValidator.cs`. Rules: ItemCode required+max25, ItemName required+max255, UnitId not-empty, ItemType in 0–3, CostingMethod in 1–4, UnitPrice >= 0 if provided, DefaultTaxRate in 0–100 if provided, ConvertRate > 0 for each UnitConvert.
  - **Complexity**: M
  - **deps**: T4.6, T4.7

---

## Phase T5: Application Layer — Lookups CQRS (US7)

**Purpose**: CRUD handlers for Currency, Unit, Warehouse, Department, ExpenseItem.

**deps**: T2.12, T2.15

- [ ] T5.1 [P] Create Currency CQRS `src/PhanMemKeToan.Application/Features/Lookups/`
  - **Files**:
    - `DTOs/CurrencyDto.cs` — Id, CurrencyCode, CurrencyName, CurrencyNameEnglish?, Symbol, ExchangeRate, IsActive
    - `Queries/GetCurrencies/GetCurrenciesQuery.cs + Handler` — return all for tenant, no pagination (max ~20 currencies)
    - `Commands/UpsertCurrency/UpsertCurrencyCommand.cs + Handler` — Create or Update (upsert by CurrencyCode); validate UQ per tenant; ExchangeRate > 0
    - `Commands/DeleteCurrency/DeleteCurrencyCommand.cs + Handler` — block if referenced by AccountObjectOpeningBalance (return 422 `has_references`)
  - **Acceptance**: GET returns full list. Create/Update validated. Delete blocked when referenced.
  - **Complexity**: M

- [ ] T5.2 [P] Create Unit CQRS `src/PhanMemKeToan.Application/Features/Lookups/`
  - **Files** (same pattern as Currency):
    - `DTOs/UnitDto.cs` — Id, UnitCode, UnitName, IsActive
    - `Queries/GetUnits/GetUnitsQuery.cs + Handler` — optional search param
    - `Commands/UpsertUnit/` + `Commands/DeleteUnit/`
  - **Delete logic**: block if referenced by InventoryItem, UnitConvert, OpeningBalance, or FormulaDetail (return 422 `has_references` with counts per entity type)
  - **Complexity**: M

- [ ] T5.3 [P] Create Warehouse CQRS `src/PhanMemKeToan.Application/Features/Lookups/`
  - **Files**: `DTOs/WarehouseDto.cs`, `Queries/GetWarehouses/`, `Commands/UpsertWarehouse/`, `Commands/DeleteWarehouse/`
  - **Delete logic**: block if referenced by InventoryItemOpeningBalance
  - **Complexity**: M

- [ ] T5.4 [P] Create Department CQRS `src/PhanMemKeToan.Application/Features/Lookups/`
  - **Files**: `DTOs/DepartmentDto.cs` (Id, Code, Name, ParentId?, Level, IsActive), `Queries/GetDepartments/` (returns tree structure), `Commands/UpsertDepartment/` (enforce Level ≤ 5), `Commands/DeleteDepartment/`
  - **Delete logic**: block if has children OR referenced by EmployeeProfile
  - **GetDepartments**: returns flat list with level info (frontend builds tree client-side)
  - **Complexity**: M

- [ ] T5.5 [P] Create ExpenseItem CQRS `src/PhanMemKeToan.Application/Features/Lookups/`
  - **Files**: `DTOs/ExpenseItemDto.cs`, `Queries/GetExpenseItems/`, `Commands/UpsertExpenseItem/`, `Commands/DeleteExpenseItem/`
  - **Complexity**: S

---

## Phase T6: Application Layer — FormulaTemplate + ItemAttributeType CQRS

**Purpose**: BOM template management and item attribute type management.

**deps**: T2.12, T2.15

- [ ] T6.1 [P] Create FormulaTemplate CQRS `src/PhanMemKeToan.Application/Features/FormulaTemplates/`
  - **Files**:
    - `DTOs/FormulaTemplateListDto.cs` — Id, TemplateCode, TemplateName, DetailCount, IsActive
    - `DTOs/FormulaTemplateDetailDto.cs` — full header + `List<FormulaDetailDto> Details`
    - `DTOs/FormulaDetailDto.cs` — Id, MaterialItemId, MaterialItemCode, MaterialItemName, UnitId, UnitCode, Quantity, SortOrder
    - `Queries/GetFormulaTemplates/` — paginated list
    - `Queries/GetFormulaTemplateById/` — header + details
    - `Commands/CreateFormulaTemplate/` — header + details in single transaction; validate MaterialItemId exists; validate UnitId exists
    - `Commands/UpdateFormulaTemplate/` — full-replace details
    - `Commands/DeleteFormulaTemplate/` — soft-delete; SET NULL on InventoryItem.FormulaTemplateId (find all referencing items and set FormulaTemplateId=null before soft-deleting)
  - **Acceptance**: Delete correctly clears FormulaTemplateId on referencing items. Detail creation validates FK references.
  - **Complexity**: L

- [ ] T6.2 [P] Create ItemAttributeType CQRS `src/PhanMemKeToan.Application/Features/ItemAttributeTypes/`
  - **Files**:
    - `DTOs/ItemAttributeTypeDto.cs` — Id, AttributeCode, AttributeName, IsActive, ReferenceCount(int)
    - `Queries/GetItemAttributeTypes/` — return all for tenant with reference count
    - `Commands/CreateItemAttributeType/`, `Commands/UpdateItemAttributeType/`, `Commands/DeleteItemAttributeType/`
  - **Delete logic**: block if `InventoryItemAttribute` references exist (return 422 `has_references`)
  - **Complexity**: M

---

## Phase T7: API Controllers (US1–US11)

**Purpose**: 11 HTTP controllers exposing CQRS handlers via REST API.

**deps**: T3, T4, T5, T6

- [x] T7.1 Create `AccountObjectsController.cs` `src/PhanMemKeToan.Api/Controllers/AccountObjectsController.cs`
  - **Description**: `[Authorize][ApiController][Route("api/account-objects")]`. Endpoints: `GET /` (GetAccountObjectsQuery), `GET /{id}` (GetAccountObjectByIdQuery), `POST /` (CreateAccountObjectCommand → 201+Location), `PUT /{id}` (UpdateAccountObjectCommand → 200), `DELETE /{id}` (DeleteAccountObjectCommand → 204). All endpoints pass TenantId from JWT claims to mediator. Map validation errors to 400 ProblemDetails. Map DomainException to appropriate HTTP codes (409 for duplicate, 422 for business rule violation).
  - **Acceptance**: All 5 endpoints reachable. Auth required. TenantId injected. Error mapping correct.
  - **Complexity**: M

- [ ] T7.2 Create `InventoryItemsController.cs` `src/PhanMemKeToan.Api/Controllers/InventoryItemsController.cs`
  - **Description**: `[Route("api/inventory-items")]`. Endpoints: `GET /` (GetInventoryItemsQuery with all filter params), `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`.
  - **Complexity**: M
  - **deps**: T4

- [ ] T7.3 [P] Create `InventoryItemCategoriesController.cs` `src/PhanMemKeToan.Api/Controllers/InventoryItemCategoriesController.cs`
  - **Description**: `[Route("api/inventory-item-categories")]`. Endpoints: `GET /tree` (GetCategoryTreeQuery), `POST /`, `PUT /{id}`, `DELETE /{id}`.
  - **Complexity**: S
  - **deps**: T4.2, T4.3

- [ ] T7.4 [P] Create `FormulaTemplatesController.cs` `src/PhanMemKeToan.Api/Controllers/FormulaTemplatesController.cs`
  - **Description**: `[Route("api/formula-templates")]`. Standard 5-endpoint CRUD.
  - **Complexity**: S
  - **deps**: T6.1

- [ ] T7.5 [P] Create `ItemAttributeTypesController.cs` `src/PhanMemKeToan.Api/Controllers/ItemAttributeTypesController.cs`
  - **Description**: `[Route("api/item-attribute-types")]`. Standard 4-endpoint CRUD (no GET by ID needed — list only).
  - **Complexity**: S
  - **deps**: T6.2

- [ ] T7.6 [P] Create Lookup Controllers (5 controllers) `src/PhanMemKeToan.Api/Controllers/`
  - **Files**:
    - `CurrenciesController.cs` — `[Route("api/currencies")]`, GET list, POST, PUT/{id}, DELETE/{id}
    - `UnitsController.cs` — same pattern, `[Route("api/units")]`
    - `WarehousesController.cs` — `[Route("api/warehouses")]`
    - `DepartmentsController.cs` — `[Route("api/departments")]`
    - `ExpenseItemsController.cs` — `[Route("api/expense-items")]`
  - **Acceptance**: Each controller routes to correct mediator commands/queries. DELETE returns 422 with body when blocked.
  - **Complexity**: M
  - **deps**: T5

- [ ] T7.7 Create `ImportController.cs` `src/PhanMemKeToan.Api/Controllers/ImportController.cs`
  - **Description**: `[Route("api/import")]`. Endpoints:
    - `GET /template/account-objects` → return Excel template (ClosedXML generated, Content-Type xlsx)
    - `GET /template/inventory-items` → return Excel template
    - `POST /account-objects` → `[RequestSizeLimit(5_242_880)]` multipart file upload → ImportAccountObjectsCommand → 200 with ImportResultDto
    - `POST /inventory-items` → same pattern
  - Template generation: Use ClosedXML to create xlsx with headers in row 1, example row in row 2, data validation notes in row 3.
  - **Acceptance**: Template files downloadable. File size > 5MB rejected with 413. Results include success/error counts.
  - **Complexity**: M
  - **deps**: T7.8 (import handlers)

- [ ] T7.8 Create Excel Import Service + Import Command Handlers `src/PhanMemKeToan.Infrastructure/Services/ExcelImportService.cs`
  - **Description**: Implement `IExcelImportService`. Method: `ParseAccountObjectsAsync(Stream file) → List<AccountObjectImportRow>` and `ParseInventoryItemsAsync(Stream file) → List<InventoryItemImportRow>`. Uses ClosedXML: open workbook, read worksheet(1), skip header rows 1–2, parse each RowsUsed() row. Max 5000 rows. Returns parse result including row number and parse errors.
  - Then create handlers:
    - `src/PhanMemKeToan.Application/Features/Import/Commands/ImportAccountObjects/ImportAccountObjectsCommandHandler.cs` — row-by-row: parse → validate → uniqueness check → individual SaveChanges (DD-001 best-effort). Return `ImportResultDto { SuccessCount, ErrorCount, Errors[{RowNumber, Field, Message}] }`.
    - `src/PhanMemKeToan.Application/Features/Import/Commands/ImportInventoryItems/ImportInventoryItemsCommandHandler.cs` — same pattern; resolve UnitCode→UnitId and CategoryCode→CategoryId via lookup dictionaries loaded once per import; resolve ItemType string (NVL/TP/HH/DV) → InventoryItemType enum.
  - **Acceptance**: Valid rows saved individually. Invalid rows accumulated in error list. 5000-row import completes < 30s. ObjectType string parsing (e.g., "Khách hàng,Nhà cung cấp") → bitmask correct.
  - **deps**: T2.14, T3.4, T4.6
  - **Complexity**: XL

---

## Phase T8: Frontend — Lookup Screens (US7)

**Purpose**: Currency, Unit, Warehouse, Department, ExpenseItem, ItemAttributeType management screens.

**deps**: T7.6, T7.5 (APIs must exist)

- [ ] T8.1 Create lookups API service `src/webapp/src/app/features/di/setup/services/lookups-api.service.ts`
  - **Description**: Angular injectable service. Methods: `getCurrencies()`, `createCurrency(dto)`, `updateCurrency(id, dto)`, `deleteCurrency(id)`, `getUnits(search?)`, `createUnit(dto)`, `updateUnit(id, dto)`, `deleteUnit(id)`, `getWarehouses()`, `createWarehouse(dto)`, `updateWarehouse(id,dto)`, `deleteWarehouse(id)`, `getDepartments()`, `createDepartment(dto)`, `updateDepartment(id,dto)`, `deleteDepartment(id)`, `getExpenseItems()`, `createExpenseItem(dto)`, `updateExpenseItem(id,dto)`, `deleteExpenseItem(id)`. All return `Observable<T>`.
  - **Acceptance**: Service compiles. URL paths match contracts/lookups-api.md.
  - **Complexity**: M

- [ ] T8.2 Create master-data TypeScript models `src/webapp/src/app/features/di/models/master-data.models.ts`
  - **Description**: Export interfaces: `AccountObject`, `AccountObjectListItem`, `AccountObjectDetail`, `BankAccountDto`, `OpeningBalanceDto`, `EmployeeProfileDto`, `InventoryItem`, `InventoryItemListItem`, `InventoryItemDetail`, `UnitConvertDto`, `BarcodeDto`, `ItemAttributeDto`, `InventoryItemOpeningBalanceDto`, `CategoryTreeNode`, `Currency`, `Unit`, `Warehouse`, `Department`, `ExpenseItem`, `FormulaTemplate`, `FormulaDetail`, `ItemAttributeType`, `ImportResult`, `ImportError`. Match API contract response shapes.
  - **Acceptance**: All interfaces compile. Enums: `ObjectType` (bitmask constants), `InventoryItemType` (0–3), `CostingMethod` (1–4), `BarcodeType` (0–5).
  - **Complexity**: M

- [ ] T8.3 [P] Create inline-editable DataTable lookup screens (Currency, Unit, Warehouse, ExpenseItem)
  - **Files** (4 components, each standalone, Angular 20):
    - `src/webapp/src/app/features/di/setup/currencies/currencies-page.component.ts/html`
    - `src/webapp/src/app/features/di/setup/units/units-page.component.ts/html`
    - `src/webapp/src/app/features/di/setup/warehouses/warehouses-page.component.ts/html`
    - `src/webapp/src/app/features/di/setup/expense-items/expense-items-page.component.ts/html`
  - **Pattern**: PrimeNG `p-table` with inline editing (`[editingRowKeys]`). Toolbar: Add row button, search input. Each row: fields + IsActive toggle + delete button. Delete confirmation dialog (`p-confirmDialog`). Toast on save/error. Load on init via signal store or direct service call.
  - **Acceptance**: Add/edit/delete working. IsActive toggle works inline. Duplicate code error shown as toast. Vietnamese labels via i18n keys.
  - **deps**: T8.1, T8.2
  - **Complexity**: L

- [ ] T8.4 [P] Create Department tree screen `src/webapp/src/app/features/di/setup/departments/departments-page.component.ts/html`
  - **Description**: Left panel: PrimeNG `p-tree` showing department hierarchy. Right panel: form dialog for add/edit with fields: Code, Name, Parent (tree-select dropdown), IsActive. Delete button (blocked if children/references). Max level 5 validation shown on parent selection.
  - **Acceptance**: Tree renders hierarchy. Adding child under parent shows correct level. Delete blocked with message. Max 5 levels enforced client-side.
  - **deps**: T8.1, T8.2
  - **Complexity**: L

- [ ] T8.5 [P] Create ItemAttributeType screen `src/webapp/src/app/features/di/setup/item-attribute-types/item-attribute-types-page.component.ts/html`
  - **Description**: Simple inline-editable PrimeNG DataTable. Columns: AttributeCode, AttributeName, ReferenceCount (readonly), IsActive. Add/edit inline. Delete blocked if ReferenceCount > 0.
  - **Acceptance**: Reference count displayed. Delete blocked with i18n message.
  - **deps**: T8.1, T8.2
  - **Complexity**: M

- [ ] T8.6 Add i18n keys for lookup screens to `src/webapp/src/app/core/i18n/vi.json`
  - **Description**: Add under keys: `lookups.*`, `currencies.*`, `units.*`, `warehouses.*`, `departments.*`, `expense-items.*`, `attribute-types.*`. Vietnamese values for all labels, column headers, messages, error messages. Minimum 40 keys.
  - **Complexity**: M
  - **deps**: T8.3, T8.4, T8.5

- [ ] T8.7 Update DI routes to include setup sub-routes `src/webapp/src/app/features/di/di.routes.ts`
  - **Description**: Add lazy-loaded routes: `/di/setup/currencies`, `/di/setup/units`, `/di/setup/warehouses`, `/di/setup/departments`, `/di/setup/expense-items`, `/di/setup/item-attribute-types`. Each maps to its page component.
  - **Acceptance**: Navigation to each route loads correct component.
  - **deps**: T8.3, T8.4, T8.5
  - **Complexity**: S

---

## Phase T9: Frontend — InventoryItemCategory Tree Screen (US5)

**Purpose**: Category management UI with 5-level tree.

**deps**: T7.3 (category API), T8.2

- [ ] T9.1 Create item-attribute-types and categories API service `src/webapp/src/app/features/di/inventory-items/services/`
  - **Description**: Create `inventory-items-api.service.ts` (initial version — category tree endpoints only for now). Methods: `getCategoryTree()`, `createCategory(dto)`, `updateCategory(id,dto)`, `deleteCategory(id)`. Also `item-attribute-types-api.service.ts`: `getItemAttributeTypes()`, `create/update/delete`.
  - **Complexity**: S

- [ ] T9.2 Create `InventoryItemCategoryTreeComponent` `src/webapp/src/app/features/di/inventory-items/components/category-tree/`
  - **Description**: Standalone component. PrimeNG `p-tree` with context menu (Add child, Rename, Delete). Tree nodes built from `CategoryTreeNode[]`. Emits `categorySelected: EventEmitter<string | null>` for parent filtering. Add/edit opens `p-dialog` form: CategoryCode, CategoryName, IsActive, SortOrder. Delete confirmation. Max level 5 enforced.
  - **Acceptance**: Tree renders. Context menu functional. Emit fires on node click. Add/edit/delete work via API.
  - **deps**: T9.1, T8.2
  - **Complexity**: L

---

## Phase T10: Frontend — AccountObject List + Form (US1 + US2 + US3 + US4 + US10)

**Purpose**: Full-stack AccountObject management UI with 4-tab form.

**deps**: T0 (GridActionBar + SearchBar must exist)

**deps**: T7.1 (API), T8.2 (models), T8.6 (i18n base), Phase T8 (lookup data for dropdowns)

- [ ] T10.1 Create AccountObject API service `src/webapp/src/app/features/di/account-objects/services/account-objects-api.service.ts`
  - **Description**: Methods: `getList(filters, page, pageSize)→Observable<PaginatedResult<AccountObjectListItem>>`, `getById(id)→Observable<AccountObjectDetail>`, `create(dto)→Observable<AccountObjectDetail>`, `update(id,dto)→Observable<AccountObjectDetail>`, `delete(id)→Observable<void>`.
  - **Complexity**: S

- [ ] T10.2 Create AccountObjects Signal Store `src/webapp/src/app/features/di/account-objects/store/account-objects.store.ts`
  - **Description**: NgRx Signals `signalStore(...)`. State shape per plan.md:
    ```
    items: AccountObjectListItem[], selectedId: string|null,
    formMode: 'view'|'edit'|'create'|null, loading: bool, saving: bool,
    filters: { typeFlags: number, status: string, search: string },
    pagination: { page, pageSize, totalCount }, error: string|null
    ```
    Methods: `loadList()`, `setFilters(partial)`, `selectItem(id)`, `createItem(dto)`, `updateItem(id,dto)`, `deleteItem(id)`, `setFormMode(mode)`.
    Computed signals: `filteredTypeLabel()`, `hasSelection()`.
  - **Acceptance**: Store compiles. Signals update reactively. Loading state set during API calls.
  - **deps**: T10.1
  - **Complexity**: M

- [ ] T10.3 Create AccountObjects List Component `src/webapp/src/app/features/di/account-objects/components/account-objects-list/`
  - **Description**: Standalone component. PrimeNG `p-table` with columns: **checkbox** (first, for row selection — drives GridActionBar disabled state), Code (frozen), Name (frozen), ObjectType badges (Customer=blue tag / Vendor=orange / Employee=green), TaxCode, Phone, Status badge. **No inline action column** — Thêm/Sửa/Xóa are on `GridActionBar` only (FR-AO-027). Toolbar: `GridActionBar` (inputs: `[selectedCount]`, `[showImport]=true`, `[showExport]=true`, `[showPrint]=true`) + ObjectType `<select>` slot + `SearchBar`. Search triggers on Enter / button click — **no debounce** (per FR-TB-020). Pagination (server-side). Keyboard: F3=SearchBar focus. Frozen columns: Code + Name. Row height 32px.
  - **ObjectType badge**: Multiple PrimeNG `p-tag` per row if multiple bits set (Customer=info, Vendor=warning, Employee=success).
  - **Acceptance**: List loads. Filters work. Pagination works. Badges render correctly for bitmask combinations.
  - **deps**: T10.2
  - **Complexity**: L

- [ ] T10.4 Create AccountObject Form — 4-tab layout `src/webapp/src/app/features/di/account-objects/components/account-object-form/`
  - **Description**: Standalone component. 4-tab PrimeNG `p-tabView`:
    - **Tab 1 "Thông tin chung"**: ObjectCode*, ObjectName*, ObjectNameEnglish, ObjectType checkboxes (Customer/Vendor/Employee — bitmask), Address, TaxCode, Email, Phone, Fax, Website, ContactPerson, ContactPhone, Description (textarea), AccountObjectGroup (dropdown, optional), CreditLimit (number), PaymentTermDays (number), IsActive toggle.
    - **Tab 2 "Thông tin nhân viên"**: Visible only when Employee bit set. CitizenId, DateOfBirth (p-calendar), Gender (p-dropdown), SocialInsuranceNumber, HireDate, Department (p-dropdown tree), DependentCount.
    - **Tab 3 "Tài khoản ngân hàng"**: Inline DataTable (p-table editable). Columns: BankName*, BankBranch, AccountNumber*, SwiftCode. Add/delete row buttons.
    - **Tab 4 "Số dư đầu kỳ"**: Inline DataTable. Columns: Currency* (p-dropdown), DebitAmountOC, CreditAmountOC, ExchangeRate, DebitAmount (auto), CreditAmount (auto). Amount auto-calculated = OC × ExchangeRate.
    - Keyboard shortcuts: Ctrl+S=Save, Ctrl+Shift+S=Save&New, Ctrl+D=Duplicate, Insert=AddRow (in active tab table), Ctrl+Delete=DeleteRow.
    - Dirty-form guard: canDeactivate check.
    - Required fields: red asterisk + red border on error.
  - **Acceptance**: Tab 2 visibility toggles with Employee checkbox. BankAccount rows add/remove. OpeningBalance amounts auto-calculate. Ctrl+S saves. Dirty guard warns on navigation.
  - **deps**: T10.2, T8.2
  - **Complexity**: XL

- [ ] T10.5 Create AccountObjects Page Component `src/webapp/src/app/features/di/account-objects/account-objects-page.component.ts/html/scss`
  - **Description**: Shell component that hosts list and form side-by-side (or route-based). Manages navigation between list and form modes via store.
  - **Complexity**: S
  - **deps**: T10.3, T10.4

- [ ] T10.6 Add AccountObject i18n keys to `src/webapp/src/app/core/i18n/vi.json`
  - **Description**: Keys under `account-objects.*`: all column headers, tab labels, field labels, validation messages, button labels, confirmation dialogs. Minimum 60 keys.
  - **Complexity**: M
  - **deps**: T10.3, T10.4

- [ ] T10.7 Update DI routes for AccountObjects `src/webapp/src/app/features/di/di.routes.ts`
  - **Description**: Add routes: `/di/account-objects` → AccountObjectsPageComponent (lazy). Guard with CanDeactivate for dirty form.
  - **Complexity**: S
  - **deps**: T10.5

---

## Phase T11: Frontend — InventoryItem List + Form (US5 + US6 + US11)

**deps**: T0 (GridActionBar + SearchBar must exist)

**Purpose**: InventoryItem management with category tree panel and 7-tab form.

**deps**: T7.2, T8.2, T9.2 (category tree component reused)

- [ ] T11.1 Extend AccountObjects API Service with InventoryItem endpoints `src/webapp/src/app/features/di/inventory-items/services/inventory-items-api.service.ts`
  - **Description**: Methods: `getList(filters)→PaginatedResult`, `getById(id)→InventoryItemDetail`, `create(dto)`, `update(id,dto)`, `delete(id)`. Build on top of T9.1 service (extend existing file).
  - **Complexity**: S

- [ ] T11.2 Create InventoryItems Signal Store `src/webapp/src/app/features/di/inventory-items/store/inventory-items.store.ts`
  - **Description**: State shape per plan.md: `items`, `categories[]`, `selectedCategoryId`, `selectedItemId`, `formMode`, `loading`, `saving`, `filters{itemType,search}`, `pagination`, `error`. Computed: `visibleCategoryIds()` — set of all descendant IDs of selectedCategoryId (for highlighting). Methods: `loadCategories()`, `loadList()`, `setCategory(id)`, `setFilters(partial)`, etc.
  - **Complexity**: M
  - **deps**: T11.1, T9.1

- [ ] T11.3 Create InventoryItems List + Category Split Layout `src/webapp/src/app/features/di/inventory-items/components/inventory-items-list/`
  - **Description**: Split panel: left `p-splitter` 280px = `InventoryItemCategoryTreeComponent` (T9.2). Right = DataTable with columns: **checkbox** (first, for row selection), Code (frozen), Name, ItemType badge (NVL/TP/HH/DV), Unit, Category, UnitPrice (right-aligned monospace), IsActive. **No inline action column** — Thêm/Sửa/Xóa are on `GridActionBar` only. Toolbar: `GridActionBar` + ItemType `<select>` slot (Tất cả/HH/NVL/TP/DV) + `SearchBar`. Category tree selection filters table.
  - **Acceptance**: Selecting category filters items to subtree. Deselecting category shows all. ItemType badges render.
  - **deps**: T11.2, T9.2
  - **Complexity**: L

- [ ] T11.4 Create InventoryItem Form — 7-tab layout `src/webapp/src/app/features/di/inventory-items/components/inventory-item-form/`
  - **Description**: Standalone component. 7-tab PrimeNG `p-tabView`:
    - **Tab 1 "Thông tin chung"**: ItemCode*, ItemName*, ItemNameEnglish, Category (p-treeSelect), Unit* (p-dropdown), ItemType (p-dropdown: NVL/TP/HH/DV), CostingMethod (dropdown: FIFO/LIFO/Bình quân/Đích danh), DefaultTaxRate, UnitPrice, SalePrice1/2/3, Description, IsActive.
    - **Tab 2 "Đơn vị tính phụ"**: Inline DataTable. Columns: Unit (p-dropdown)*, ConvertRate* (number). Add/remove rows.
    - **Tab 3 "Mã vạch"**: Inline DataTable. BarcodeValue*, BarcodeType (p-dropdown: Code128/EAN13/etc.), UnitId (optional p-dropdown), IsPrimary flag. Add/delete rows.
    - **Tab 4 "Thuộc tính"**: Inline DataTable. AttributeType (p-dropdown)*,  AttributeValue (text). Add/delete rows.
    - **Tab 5 "Định mức NVL"**: Readonly FormulaTemplate selector (p-dropdown) + link to FormulaTemplates page. Show selected template's details in sub-table (read-only preview). Visible when FormulaTemplateId is set or ItemType=1 FinishedProduct.
    - **Tab 6 "Cài đặt kho"**: MinStockLevel, MaxStockLevel, LeadTimeDays, IsFollowSerial, IsFollowLot, IsFollowExpiry, IsPanelItem toggle (if true → PanelUnit selector appears).
    - **Tab 7 "Số dư tồn kho"**: Hidden for Service items (ItemType=3). Inline DataTable: Warehouse* (p-dropdown), Unit (defaults to main unit), Quantity*, UnitCost*, Amount (auto=Qty×UnitCost), Currency (optional), ForeignAmount (visible if currency≠VND), ExchangeRate.
    - All keyboard shortcuts same as AccountObject form.
  - **Acceptance**: Tab 7 hidden for Service items. Amount auto-calculates. BOM tab shows template preview. Barcode uniqueness validated on save.
  - **deps**: T11.2, T8.2, T9.2
  - **Complexity**: XL

- [ ] T11.5 Create InventoryItems Page + routes `src/webapp/src/app/features/di/inventory-items/`
  - **Description**: `inventory-items-page.component.ts/html`. Add routes to `di.routes.ts`: `/di/inventory-items`, `/di/inventory-items/formula-templates`, `/di/inventory-items/attribute-types`.
  - **Complexity**: S
  - **deps**: T11.3, T11.4

- [ ] T11.6 Add InventoryItem i18n keys `src/webapp/src/app/core/i18n/vi.json`
  - **Description**: Keys under `inventory-items.*`: all 7 tab labels, field labels, ItemType/CostingMethod/BarcodeType Vietnamese labels, validation messages. Minimum 80 keys.
  - **Complexity**: M
  - **deps**: T11.3, T11.4

---

## Phase T12: Frontend — FormulaTemplate Master-Detail Screen

**Purpose**: BOM template management screen (FR-IN-015).

**deps**: T7.4 (API), T11.2 (inventory items store for item lookup)

- [ ] T12.1 Create FormulaTemplate API service `src/webapp/src/app/features/di/inventory-items/services/formula-templates-api.service.ts`
  - **Description**: Methods: `getList(page,size,search)`, `getById(id)`, `create(dto)`, `update(id,dto)`, `delete(id)`.
  - **Complexity**: S

- [ ] T12.2 Create FormulaTemplates Page Component `src/webapp/src/app/features/di/inventory-items/components/formula-templates-page/`
  - **Description**: Split layout: left = list of templates (p-table: TemplateCode, TemplateName, DetailCount, IsActive). Right = detail form when template selected. Detail form: header fields (Code*, Name*, IsActive) + inline DataTable of materials (MaterialItem p-dropdown, Unit p-dropdown, Quantity*, SortOrder). Toolbar: Save (Ctrl+S), Delete. On delete: confirm dialog + warn about referencing items.
  - **Acceptance**: Master-detail works. Detail count accurate. Delete confirmation shown. IsActive toggles correctly.
  - **deps**: T12.1, T11.1 (for item lookup dropdown)
  - **Complexity**: L

---

## Phase T13: Frontend — Import Wizard (US8 + US9)

**Purpose**: Excel import dialog for AccountObjects and InventoryItems.

**deps**: T7.7, T7.8 (import APIs), T10.2, T11.2 (stores to refresh after import)

- [ ] T13.1 Create Import API service `src/webapp/src/app/features/di/services/import-api.service.ts`
  - **Description**: Methods: `downloadAccountObjectTemplate()→Observable<Blob>`, `downloadInventoryItemTemplate()→Observable<Blob>`, `importAccountObjects(file: File)→Observable<ImportResult>`, `importInventoryItems(file: File)→Observable<ImportResult>`. File upload as FormData multipart.
  - **Complexity**: S

- [ ] T13.2 Create AccountObjects Import Dialog `src/webapp/src/app/features/di/account-objects/components/import-account-objects-dialog/`
  - **Description**: Standalone `p-dialog` (3-step stepper):
    - **Step 1 "Tải mẫu"**: Instructions + "Tải file mẫu" button (downloads template). "Tiếp theo" button.
    - **Step 2 "Chọn file"**: `p-fileUpload` (accept=.xlsx, maxFileSize=5MB). File preview: name + size. "Nhập liệu" button triggers import.
    - **Step 3 "Kết quả"**: Shows `ImportResult`: "Thành công: X dòng", "Lỗi: Y dòng". Error table: RowNumber, Field, Message (if errors > 0). "Tải file lỗi" button (if errors). "Đóng" closes dialog and refreshes list.
  - **Acceptance**: Template downloads correctly. File size validation. Results shown with row-level errors. List refreshes after close.
  - **deps**: T13.1
  - **Complexity**: L

- [ ] T13.3 Create InventoryItems Import Dialog `src/webapp/src/app/features/di/inventory-items/components/import-inventory-items-dialog/`
  - **Description**: Same 3-step pattern as T13.2. References `/api/import/template/inventory-items` and `/api/import/inventory-items`. After successful import, triggers `store.loadList()` and `store.loadCategories()`.
  - **Complexity**: M
  - **deps**: T13.1

- [ ] T13.4 Integrate import dialogs into list toolbars
  - **Description**: Add "Nhập từ Excel" toolbar button to `AccountObjectsListComponent` (T10.3) and `InventoryItemsListComponent` (T11.3). Clicking opens respective import dialog with `p-dialog [visible]` binding.
  - **Complexity**: S
  - **deps**: T13.2, T13.3, T10.3, T11.3

---

## Phase T14: Tests

**Purpose**: Unit + integration tests for critical backend logic.

**deps**: All backend phases (T1–T7)

- [ ] T14.1 [P] Domain entity unit tests `tests/PhanMemKeToan.Domain.Tests/`
  - **Description**: Tests in `tests/PhanMemKeToan.Domain.Tests/Entities/MasterDataTests.cs`. Test: AccountObject default state (IsActive=true, RowVersion=0), ObjectType bitmask combinations, InventoryItem default CostingMethod=WeightedAverage, Service item type value=3, BarcodeType enum values.
  - **Acceptance**: All tests pass. No dependencies on DB or EF Core.
  - **Complexity**: S

- [ ] T14.2 [P] AccountObject command handler unit tests `tests/PhanMemKeToan.Application.Tests/Features/AccountObjects/`
  - **Description**: Tests using in-memory DbContext mock (or SQLite). Cover:
    - `CreateAccountObjectCommandHandler`: happy path, duplicate code → exception, ObjectType=0 → validation error, Employee bit set + profile → profile created
    - `UpdateAccountObjectCommandHandler`: stale RowVersion → exception, Employee bit removed → profile soft-deleted
    - `DeleteAccountObjectCommandHandler`: soft-delete sets IsDeleted=true
  - **Acceptance**: All critical paths tested. No real DB required.
  - **Complexity**: M

- [ ] T14.3 [P] InventoryItem command handler unit tests `tests/PhanMemKeToan.Application.Tests/Features/InventoryItems/`
  - **Description**: Tests for `CreateInventoryItemCommandHandler`: happy path, Service item + opening balance → exception (BR-IN05), duplicate barcode → exception, duplicate ItemCode → exception. `GetInventoryItemsQueryHandler`: CategoryId subtree filter.
  - **Acceptance**: BR-IN05 enforced in tests. Category subtree filter tested.
  - **Complexity**: M

- [ ] T14.4 [P] Lookup CRUD handler unit tests `tests/PhanMemKeToan.Application.Tests/Features/Lookups/`
  - **Description**: Tests for DeleteCurrencyCommandHandler (has_references → exception), DeleteUnitCommandHandler (same), DeleteItemAttributeTypeCommandHandler (same). At minimum 1 happy path + 1 blocked path per delete handler.
  - **Complexity**: S

- [ ] T14.5 [P] Import handler unit tests `tests/PhanMemKeToan.Application.Tests/Features/Import/`
  - **Description**: Test `ImportAccountObjectsCommandHandler` with mock Excel stream (small in-memory ClosedXML file): valid rows saved, duplicate code skipped and in error list, missing required field in error list. Test ObjectType string parsing: "Khách hàng,Nhà cung cấp" → bitmask=3.
  - **Complexity**: M

- [ ] T14.6 [P] API controller integration tests `tests/PhanMemKeToan.Api.Tests/Controllers/`
  - **Description**: Using `WebApplicationFactory<Program>`. Test AccountObjectsController: GET returns 200 + pagination shape, POST with invalid body returns 400 ProblemDetails, POST with duplicate code returns 409, DELETE returns 204. Test ImportController: GET template returns xlsx content-type.
  - **Complexity**: M

---

## Phase T15: Polish & Cross-Cutting Concerns

**Purpose**: Final integration wiring, navigation menu, performance optimizations.

**deps**: All previous phases

- [ ] T15.1 Add DI module navigation menu items `src/webapp/src/app/features/di/`
  - **Description**: Update DI sidebar navigation to include: "Đối tượng kế toán" (/di/account-objects), "Hàng hóa/Vật tư" (/di/inventory-items), "Cài đặt" sub-menu → Currency/Units/Warehouses/Departments/ExpenseItems/AttributeTypes. Use existing navigation service pattern from di-account-tree feature.
  - **Complexity**: S

- [ ] T15.2 [P] Implement Redis caching for lookup dropdown data `src/PhanMemKeToan.Infrastructure/`
  - **Description**: In query handlers for `GetCurrencies`, `GetUnits`, `GetWarehouses`, `GetDepartments`, `GetExpenseItems`: wrap EF Core query with `IDistributedCache.GetStringAsync/SetStringAsync` (TTL = 10 minutes). Invalidate cache in respective UpsertX/DeleteX command handlers. Cache key format: `lookup:{tenantId}:{entityType}`.
  - **Acceptance**: Second call to `GetCurrencies` does not hit PostgreSQL. Write commands invalidate cache. Multi-tenant isolation via tenantId in key.
  - **Complexity**: M

- [ ] T15.3 [P] Performance: AccountObjects list index optimization
  - **Description**: In migration or via new migration `AddMasterDataIndexes.cs`: Add composite index `(TenantId, IsDeleted, IsActive, ObjectType)` on `account_objects` for list query. Add GIN index on `ObjectCode || ObjectName` for ILIKE search OR add `pg_trgm` extension + GIN index on ObjectName for fast trigram search.
  - **Acceptance**: `EXPLAIN ANALYZE` on GetAccountObjects query shows Index Scan (not Seq Scan) for 10K+ rows.
  - **Complexity**: M

- [ ] T15.4 [P] Error handling: Map domain exceptions to HTTP responses `src/PhanMemKeToan.Api/Infrastructure/GlobalExceptionHandler.cs`
  - **Description**: In existing `GlobalExceptionHandler`, add cases for new exception types: `DuplicateCodeException` → 409, `BusinessRuleViolationException` → 422, `OptimisticConcurrencyException` → 409. Return RFC 7807 ProblemDetails with `errorCode` extension property matching contract codes (`duplicate_code`, `has_references`, `stale_record`, etc.).
  - **Acceptance**: Each new exception type maps to correct HTTP status. Response body includes `errorCode`.
  - **Complexity**: M

- [ ] T15.5 Validate build: run `dotnet build` + `ng build` `src/`
  - **Description**: Run full backend build (`dotnet build PhanMemKeToan.sln`) and frontend build (`ng build --configuration=development`). Fix any compilation errors. Run `get_errors` on all modified files.
  - **Acceptance**: Zero compilation errors in both backend and frontend.
  - **deps**: All phases
  - **Complexity**: M

---

## Dependency Graph

```
T1 (Domain)
  └──► T2 (EF Config + Migration)
         ├──► T3 (AccountObject CQRS)
         ├──► T4 (InventoryItem CQRS)  [deps: T3 for category subtree]
         ├──► T5 (Lookups CQRS)
         └──► T6 (Formula + AttributeType CQRS)
                └──► T7 (API Controllers)
                       ├──► T8 (Frontend Lookups)
                       ├──► T9 (Category Tree UI)
                       ├──► T10 (AccountObject UI)  [deps: T8 for dropdowns]
                       ├──► T11 (InventoryItem UI)  [deps: T9, T8]
                       ├──► T12 (FormulaTemplate UI) [deps: T11]
                       └──► T13 (Import UI)         [deps: T10, T11]
                              └──► T14 (Tests)
                                     └──► T15 (Polish)
```

### User Story → Phase Mapping

| User Story | Priority | Backend Phases | Frontend Phases |
|------------|----------|---------------|-----------------|
| US1: Browse Account Objects | P1 | T3 (GetList query) | T10.2, T10.3 |
| US2: Create/Edit Account Object | P1 | T3 (Create/Update) | T10.4 |
| US3: Manage Bank Accounts | P2 | T3 (embedded in Create/Update) | T10.4 Tab 3 |
| US4: Deactivate Account Object | P2 | T3 (Update IsActive) | T10.3 (toggle) |
| US5: Browse Inventory Items | P1 | T4 (GetList) | T9, T11.2, T11.3 |
| US6: Create/Edit Inventory Item | P1 | T4 (Create/Update) | T11.4 |
| US7: Manage Lookup Data | P2 | T5 (Lookups CQRS) | T8 |
| US8: Import Account Objects | P3 | T7.8 | T13.2 |
| US9: Import Inventory Items | P3 | T7.8 | T13.3 |
| US10: Employee Profile | P2 | T3.4, T3.5 (DD-007) | T10.4 Tab 2 |
| US11: Opening Stock Balance | P2 | T4.6, T4.7 (DD-006) | T11.4 Tab 7 |

---

## Parallel Execution Opportunities

### Phase T1 (can all run in parallel):
T1.1 (ObjectType) ∥ T1.2 (InventoryItemType) ∥ T1.3 (CostingMethod) ∥ T1.4 (BarcodeType) ∥ T1.5 (Gender) ∥ T1.6 (Currency) ∥ T1.7 (Unit) ∥ T1.8 (Warehouse) ∥ T1.9 (Department) ∥ T1.10 (ExpenseItem) ∥ T1.16 (Category) ∥ T1.17 (AttributeType) ∥ T1.18 (FormulaTemplate)

### Phase T2 (can run in parallel once T1 done):
T2.1 (Lookup configs) ∥ T2.2 (AOGroup config) ∥ T2.4 (BankAccount config) ∥ T2.5 (OpeningBalance config) ∥ T2.7 (Category config) ∥ T2.8 (AttributeType+Formula configs) ∥ T2.10 (child configs) ∥ T2.11 (InvOpenBal config)

### Phase T3–T6 (backend CQRS — can run in parallel):
T3 ∥ T4 ∥ T5 ∥ T6 (independent feature folders, share only IApplicationDbContext)

### Phase T8–T9 (Frontend Lookups + Category — parallel):
T8.3 ∥ T8.4 ∥ T8.5 ∥ T9.2

---

## Implementation Strategy (MVP Increments)

### MVP Increment 1 (2 days) — US1+US2 working end-to-end:
T1.1-T1.5 (enums) → T1.6 (Currency) → T1.11-T1.15 (AO cluster) → T2.1+T2.3+T2.4+T2.5+T2.12+T2.13+T2.15 → T3 → T7.1 → T10.1+T10.2+T10.3+T10.4

### MVP Increment 2 (1.5 days) — Lookup data + US5+US6:
T1.7-T1.10 (lookup entities) → T1.16-T1.24 (inventory cluster) → T2.1(extend)+T2.7-T2.11 → T4+T5 → T7.2+T7.3+T7.6 → T8+T9+T11

### MVP Increment 3 (1 day) — FormulaTemplate + Import:
T6 → T7.4+T7.5+T7.7+T7.8 → T12+T13

### MVP Increment 4 (1 day) — Tests + Polish:
T14 → T15

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 74 |
| Phase T1 (Domain) | 24 tasks (T1.1–T1.24) |
| Phase T2 (EF Config+Migration) | 16 tasks (T2.1–T2.16) |
| Phase T3 (AO CQRS) | 7 tasks (T3.1–T3.7) |
| Phase T4 (Item CQRS) | 9 tasks (T4.1–T4.9) |
| Phase T5 (Lookups CQRS) | 5 tasks (T5.1–T5.5) |
| Phase T6 (Formula+Attr CQRS) | 2 tasks (T6.1–T6.2) |
| Phase T7 (API Controllers) | 8 tasks (T7.1–T7.8) |
| Phase T8 (Frontend Lookups) | 7 tasks (T8.1–T8.7) |
| Phase T9 (Category Tree UI) | 2 tasks (T9.1–T9.2) |
| Phase T10 (AccountObject UI) | 7 tasks (T10.1–T10.7) |
| Phase T11 (InventoryItem UI) | 6 tasks (T11.1–T11.6) |
| Phase T12 (FormulaTemplate UI) | 2 tasks (T12.1–T12.2) |
| Phase T13 (Import UI) | 4 tasks (T13.1–T13.4) |
| Phase T14 (Tests) | 6 tasks (T14.1–T14.6) |
| Phase T15 (Polish) | 5 tasks (T15.1–T15.5) |
| Parallelizable tasks [P] | 32 tasks |
| XL complexity tasks | 4 (T3.5, T4.6, T10.4, T11.4) |
| L complexity tasks | 11 |
| M complexity tasks | 30 |
| S complexity tasks | 29 |


