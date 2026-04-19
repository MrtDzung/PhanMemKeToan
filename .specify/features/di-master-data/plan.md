# Implementation Plan: DI Module — Step 3: Master Data

**Branch**: `feature/di-master-data` | **Date**: 2026-04-18 | **Spec**: `spec.md`  
**Updated**: 2026-04-18 (Review v4 — synced with spec.md + data-model.md: 19 entities, EmployeeProfile, FormulaTemplate, ItemAttributeType, InventoryItemOpeningBalance)  
**Depends On**: `feature/di-account-tree` (Accounts table + ApplicationDbContext must exist)

---

## Summary

Implement master data management for the DI module covering **19 entities** across 9 sub-features (spec.md §1):

- **AccountObject** (customers/vendors/employees) with child entities: BankAccount, OpeningBalance, EmployeeProfile (DD-007), Group
- **InventoryItem** (products/materials/services) with child entities: Category tree, UnitConvert, FormulaTemplate/Detail (BOM), Barcode, Attribute/AttributeType, OpeningBalance (DD-006)
- **Lookup Data**: Currency, Unit, Warehouse, Department, ExpenseItem
- **Excel Import** for AccountObject and InventoryItem (best-effort, DD-001)

Technical approach mirrors `di-account-tree`: Clean Architecture (Domain → Application → Infrastructure → API → Angular). Backend uses CQRS/MediatR + EF Core 10. Frontend uses Angular 20 standalone + NgRx Signals + PrimeNG. **EPPlus is not used** (commercial license); **ClosedXML** (MIT) handles Excel read/write.

---

## Technical Context

| Item | Detail |
|------|--------|
| Language | C# 12 (.NET 10) + TypeScript 5.x (Angular 20) |
| Backend | ASP.NET Core 10, Clean Architecture + CQRS, MediatR 14, EF Core 10, FluentValidation |
| Excel Library | **ClosedXML** (MIT) — add via `dotnet add package ClosedXML` to Infrastructure |
| Frontend | Angular 20 + PrimeNG 20 LTS + NgRx Signals + TailwindCSS v4 |
| Database | PostgreSQL 16 via ApplicationDbContext (per-tenant, Dual-DB strategy) |
| Cache | Redis 7 (list caches for lookup dropdowns) |
| Testing | xUnit (backend), Jasmine/Karma (frontend) |
| Performance | Account Objects list 10K rows < 1s; search < 500ms; Import 1K rows < 30s |

---

## Constitution Check

- ✅ Design-before-code: spec.md completed + clarifications confirmed 2026-04-18
- ✅ Domain-driven: all entities in Domain layer; business rules enforced in handlers
- ✅ Metadata-driven: ObjectType bitmask, CostingMethod as data; no hardcoded business-type logic
- ✅ Posting Engine untouched: this feature is pre-transaction master data only
- ✅ Tenant isolation: all entities implement `ITenantEntity`; ApplicationDbContext global query filters
- ✅ Incremental: DI is MVP scope; Warehouse CRUD deliberately included (DD-005)
- ✅ No hardcoded hex (CSS custom properties; `--primary`, `--debit`, `--credit`, etc.)
- ✅ Soft delete (IsDeleted on AuditableEntity); hard-delete only when no references
- ✅ Optimistic concurrency: `RowVersion` on AccountObject and InventoryItem (BR-DI02, edge case)
- ✅ Import transaction model: best-effort / row-by-row (DD-001, FR-IMP-006)
- ✅ InventoryItemOpeningBalance: flat table per warehouse (DD-006, FR-IN-040–044)
- ✅ AccountObjectEmployeeProfile: 1:1 extension entity for Employee-type objects (DD-007, FR-AO-030–034)

---

## Project Structure

### Documentation (this feature)
```
.specify/features/di-master-data/
├── spec.md                          ✅ Done
├── plan.md                          ✅ This file
├── data-model.md                    ✅ Done
├── contracts/
│   ├── account-objects-api.md       ✅ Done
│   ├── inventory-items-api.md       ✅ Done
│   ├── lookups-api.md               ✅ Done
│   └── import-api.md                ✅ Done
└── tasks.md                         ⏳ Phase 2 — /speckit.tasks
```

### Backend Source Files

```
src/PhanMemKeToan.Domain/
├── Entities/
│   ├── AccountObject.cs                              NEW
│   ├── AccountObjectBankAccount.cs                   NEW
│   ├── AccountObjectOpeningBalance.cs                NEW
│   ├── AccountObjectGroup.cs                         NEW (BR-E, deferred CRUD)
│   ├── AccountObjectEmployeeProfile.cs               NEW (DD-007, 1:1 with AccountObject)
│   ├── InventoryItem.cs                              NEW (ItemType, UnitPrice, SalePrice, tracking flags)
│   ├── InventoryItemCategory.cs                      NEW (5-level tree)
│   ├── InventoryItemUnitConvert.cs                   NEW (Gap I — multi-unit conversion)
│   ├── InventoryQuantityFormulaTemplate.cs           NEW (Gap J — BOM header)
│   ├── InventoryQuantityFormulaDetail.cs             NEW (Gap J — BOM material lines)
│   ├── InventoryItemBarcode.cs                       NEW (Gap P — multi-barcode)
│   ├── ItemAttributeType.cs                          NEW (Gap N — tenant-level attribute definitions)
│   ├── InventoryItemAttribute.cs                     NEW (Gap N — item×attribute values)
│   ├── InventoryItemOpeningBalance.cs                NEW (DD-006 — opening stock per warehouse)
│   ├── Currency.cs                                   NEW
│   ├── Unit.cs                                       NEW
│   ├── Warehouse.cs                                  NEW
│   ├── Department.cs                                 NEW
│   └── ExpenseItem.cs                                NEW
├── Enums/
│   ├── CostingMethod.cs                              NEW (FIFO, LIFO, WeightedAverage, SpecificIdentification)
│   ├── ItemType.cs                                   NEW (RawMaterial, FinishedProduct, Goods, Service)
│   └── BarcodeType.cs                                NEW (Code128, EAN13, EAN8, QRCode, DataMatrix, UPC_A)

src/PhanMemKeToan.Application/
├── Common/Interfaces/
│   └── IApplicationDbContext.cs                      MODIFY (+19 DbSet<T>)
├── Features/
│   ├── AccountObjects/
│   │   ├── Commands/
│   │   │   ├── CreateAccountObject/
│   │   │   │   ├── CreateAccountObjectCommand.cs     NEW
│   │   │   │   └── CreateAccountObjectCommandHandler.cs NEW
│   │   │   ├── UpdateAccountObject/
│   │   │   │   ├── UpdateAccountObjectCommand.cs     NEW
│   │   │   │   └── UpdateAccountObjectCommandHandler.cs NEW
│   │   │   └── DeleteAccountObject/
│   │   │       ├── DeleteAccountObjectCommand.cs     NEW
│   │   │       └── DeleteAccountObjectCommandHandler.cs NEW
│   │   ├── Queries/
│   │   │   ├── GetAccountObjects/
│   │   │   │   ├── GetAccountObjectsQuery.cs         NEW
│   │   │   │   └── GetAccountObjectsQueryHandler.cs  NEW
│   │   │   └── GetAccountObjectById/
│   │   │       ├── GetAccountObjectByIdQuery.cs      NEW
│   │   │       └── GetAccountObjectByIdQueryHandler.cs NEW
│   │   └── DTOs/
│   │       ├── AccountObjectListItemDto.cs           NEW
│   │       ├── AccountObjectDetailDto.cs             NEW (includes EmployeeProfile, BankAccounts, OpeningBalances)
│   │       ├── BankAccountDto.cs                     NEW
│   │       ├── OpeningBalanceDto.cs                  NEW
│   │       └── EmployeeProfileDto.cs                 NEW (DD-007)
│   ├── InventoryItems/
│   │   ├── Commands/
│   │   │   ├── CreateInventoryItem/
│   │   │   │   ├── CreateInventoryItemCommand.cs     NEW
│   │   │   │   └── CreateInventoryItemCommandHandler.cs NEW
│   │   │   ├── UpdateInventoryItem/
│   │   │   │   ├── UpdateInventoryItemCommand.cs     NEW
│   │   │   │   └── UpdateInventoryItemCommandHandler.cs NEW
│   │   │   └── DeleteInventoryItem/
│   │   │       ├── DeleteInventoryItemCommand.cs     NEW
│   │   │       └── DeleteInventoryItemCommandHandler.cs NEW
│   │   ├── Queries/
│   │   │   ├── GetInventoryItems/
│   │   │   │   ├── GetInventoryItemsQuery.cs         NEW (includes itemType filter)
│   │   │   │   └── GetInventoryItemsQueryHandler.cs  NEW
│   │   │   ├── GetInventoryItemById/
│   │   │   │   ├── GetInventoryItemByIdQuery.cs      NEW
│   │   │   │   └── GetInventoryItemByIdQueryHandler.cs NEW (includes child collections)
│   │   └── DTOs/
│   │       ├── InventoryItemListItemDto.cs           NEW (includes itemType, unitPrice)
│   │       ├── InventoryItemDetailDto.cs             NEW (includes child collections: unitConverts, barcodes, attributes, openingBalances)
│   │       ├── UnitConvertDto.cs                     NEW (Gap I)
│   │       ├── BarcodeDto.cs                         NEW (Gap P)
│   │       ├── ItemAttributeDto.cs                   NEW (Gap N)
│   │       └── InventoryItemOpeningBalanceDto.cs     NEW (DD-006)
│   ├── FormulaTemplates/                             NEW feature folder (FR-IN-015)
│   │   ├── Commands/
│   │   │   ├── CreateFormulaTemplate/                NEW (header + details in single command)
│   │   │   ├── UpdateFormulaTemplate/                NEW (full-replace details)
│   │   │   └── DeleteFormulaTemplate/                NEW (soft-delete, SET NULL on items)
│   │   ├── Queries/
│   │   │   ├── GetFormulaTemplates/                  NEW (list with detailCount)
│   │   │   └── GetFormulaTemplateById/               NEW (includes details)
│   │   └── DTOs/
│   │       ├── FormulaTemplateListDto.cs             NEW
│   │       ├── FormulaTemplateDetailDto.cs           NEW
│   │       └── FormulaDetailDto.cs                   NEW
│   ├── ItemAttributeTypes/                           NEW feature folder (FR-IN-016)
│   │   ├── Commands/
│   │   │   ├── CreateItemAttributeType/              NEW
│   │   │   ├── UpdateItemAttributeType/              NEW
│   │   │   └── DeleteItemAttributeType/              NEW (block if has_references)
│   │   ├── Queries/
│   │   │   └── GetItemAttributeTypes/                NEW
│   │   └── DTOs/
│   │       └── ItemAttributeTypeDto.cs               NEW
│   ├── InventoryItemCategories/                      NEW feature folder
│   │   ├── Commands/
│   │   │   ├── CreateCategory/                       NEW
│   │   │   ├── UpdateCategory/                       NEW
│   │   │   └── DeleteCategory/                       NEW (block if has_children or has_items)
│   │   ├── Queries/
│   │   │   └── GetCategoryTree/                      NEW (tree query lives here, not in InventoryItems)
│   │   └── DTOs/
│   │       └── CategoryTreeNodeDto.cs                NEW (includes isActive, sortOrder)
│   ├── Lookups/
│   │   ├── Commands/
│   │   │   ├── UpsertCurrency/
│   │   │   │   └── UpsertCurrencyCommand.cs + Handler NEW
│   │   │   ├── UpsertUnit/ ...                       NEW (same pattern)
│   │   │   ├── UpsertWarehouse/ ...                  NEW
│   │   │   ├── UpsertDepartment/ ...                 NEW
│   │   │   └── UpsertExpenseItem/ ...                NEW
│   │   ├── Queries/
│   │   │   └── GetLookups/ (one per entity)          NEW
│   │   └── DTOs/ (one per entity)                    NEW (all include isActive field)
│   └── Import/
│       ├── Commands/
│       │   ├── ImportAccountObjects/
│       │   │   ├── ImportAccountObjectsCommand.cs    NEW
│       │   │   └── ImportAccountObjectsCommandHandler.cs NEW
│       │   └── ImportInventoryItems/
│       │       ├── ImportInventoryItemsCommand.cs    NEW
│       │       └── ImportInventoryItemsCommandHandler.cs NEW
│       └── DTOs/
│           └── ImportResultDto.cs                    NEW

src/PhanMemKeToan.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs                       MODIFY (+19 DbSet + query filters)
│   ├── Configurations/
│   │   ├── AccountObjectConfiguration.cs             NEW
│   │   ├── AccountObjectBankAccountConfiguration.cs  NEW
│   │   ├── AccountObjectOpeningBalanceConfiguration.cs NEW
│   │   ├── AccountObjectGroupConfiguration.cs        NEW
│   │   ├── AccountObjectEmployeeProfileConfiguration.cs NEW (DD-007)
│   │   ├── InventoryItemConfiguration.cs             NEW
│   │   ├── InventoryItemCategoryConfiguration.cs     NEW
│   │   ├── InventoryItemUnitConvertConfiguration.cs  NEW (Gap I)
│   │   ├── InventoryQuantityFormulaTemplateConfiguration.cs NEW (Gap J)
│   │   ├── InventoryQuantityFormulaDetailConfiguration.cs NEW (Gap J)
│   │   ├── InventoryItemBarcodeConfiguration.cs      NEW (Gap P)
│   │   ├── ItemAttributeTypeConfiguration.cs         NEW (Gap N)
│   │   ├── InventoryItemAttributeConfiguration.cs    NEW (Gap N)
│   │   ├── InventoryItemOpeningBalanceConfiguration.cs NEW (DD-006)
│   │   ├── CurrencyConfiguration.cs                  NEW
│   │   ├── UnitConfiguration.cs                      NEW
│   │   ├── WarehouseConfiguration.cs                 NEW
│   │   ├── DepartmentConfiguration.cs                NEW
│   │   └── ExpenseItemConfiguration.cs               NEW
│   ├── Migrations/
│   │   └── [timestamp]_AddMasterData.cs              NEW migration
│   └── SeedData/
│       └── MasterDataSeedData.cs                     NEW (VND + 10 default units)
├── Services/
│   └── ExcelImportService.cs                         NEW (ClosedXML, implements IExcelImportService)
└── PhanMemKeToan.Infrastructure.csproj               MODIFY (+ClosedXML NuGet)

src/PhanMemKeToan.Api/
├── Controllers/
│   ├── AccountObjectsController.cs                   NEW
│   ├── InventoryItemsController.cs                   NEW
│   ├── InventoryItemCategoriesController.cs          NEW (tree + CRUD)
│   ├── FormulaTemplatesController.cs                 NEW (FR-IN-015)
│   ├── ItemAttributeTypesController.cs               NEW (FR-IN-016)
│   ├── CurrenciesController.cs                       NEW
│   ├── UnitsController.cs                            NEW
│   ├── WarehousesController.cs                       NEW
│   ├── DepartmentsController.cs                      NEW
│   ├── ExpenseItemsController.cs                     NEW
│   └── ImportController.cs                           NEW
```

### Frontend Source Files

```
src/webapp/src/app/
├── features/di/
│   ├── di.routes.ts                                  MODIFY (+account-objects, inventory-items, setup routes)
│   ├── models/
│   │   └── master-data.models.ts                     NEW (AccountObject, InventoryItem, Lookup, FormulaTemplate, ItemAttributeType interfaces)
│   ├── account-objects/                              NEW sub-feature
│   │   ├── account-objects-page.component.ts/html/scss NEW
│   │   ├── components/
│   │   │   ├── account-objects-list/                 NEW (DataTable with toolbar)
│   │   │   ├── account-object-form/                  NEW (4 tabs: General, EmployeeProfile, BankAccounts, OpeningBalance)
│   │   │   └── import-account-objects-dialog/        NEW (3-step wizard)
│   │   ├── store/
│   │   │   └── account-objects.store.ts              NEW (NgRx Signals)
│   │   └── services/
│   │       └── account-objects-api.service.ts        NEW
│   ├── inventory-items/                              NEW sub-feature
│   │   ├── inventory-items-page.component.ts/html/scss NEW
│   │   ├── components/
│   │   │   ├── inventory-items-list/                 NEW (split: category tree left, DataTable right)
│   │   │   ├── inventory-item-form/                  NEW (7 tabs: General, UnitConvert, BOM, StockSettings, Barcodes, Attributes, OpeningBalance)
│   │   │   ├── formula-templates-page/               NEW (FR-IN-015 — list + detail form)
│   │   │   ├── item-attribute-types-page/            NEW (FR-IN-016 — list + inline edit)
│   │   │   └── import-inventory-items-dialog/        NEW (3-step wizard)
│   │   ├── store/
│   │   │   └── inventory-items.store.ts              NEW (NgRx Signals — includes category tree + computed descendant filter)
│   │   └── services/
│   │       ├── inventory-items-api.service.ts        NEW
│   │       ├── formula-templates-api.service.ts      NEW (FR-IN-015)
│   │       └── item-attribute-types-api.service.ts   NEW (FR-IN-016)
│   └── setup/                                        NEW sub-feature (lookup data)
│       ├── currencies/                               NEW (inline DataTable, includes isActive toggle)
│       ├── units/                                    NEW (inline DataTable, includes isActive toggle)
│       ├── warehouses/                               NEW (inline DataTable, includes isActive toggle)
│       ├── departments/                              NEW (tree + dialog)
│       ├── expense-items/                            NEW (list + dialog)
│       └── services/
│           └── lookups-api.service.ts                NEW (single service for all 5 lookup entities)
├── core/i18n/
│   └── vi.json                                       MODIFY (+account-objects.*, inventory-items.*, lookups.*, formula-templates.*, attribute-types.*)
```

---

## Technical Design

### Domain Architecture

All entities extend `AuditableEntity` (which provides `Id`, `TenantId`, `CreatedAt/By`, `ModifiedAt/By`, `IsDeleted`) and implicitly implement `ITenantEntity` through the base class.

**ObjectType Bitmask** (BR-DI04): Stored as `int` on `AccountObject`. Values: 1=Customer, 2=Vendor, 4=Employee. Query filter: `ObjectType & requestedBits != 0`. The frontend maps this to three `p-checkbox` controls. Minimum valid value = 1 (BR-DI01).

**Tree Entities** (InventoryItemCategory, Department): Self-referential via `ParentId → Id`. Level computed by traversing ancestors on write. Maximum 5 levels enforced by FluentValidation in command handler (BR-DI05).

**Opening Balances** (DD-004): Separate table `account_object_opening_balances` with `(account_object_id, currency_id)` composite unique per tenant. Supports multi-currency AR/AP sub-ledger initialization.

**EmployeeProfile** (DD-007): 1:1 extension table `account_object_employee_profiles`. Created/deleted automatically when Employee bit (4) is set/unset on AccountObject. Fields: CitizenId (unique), DateOfBirth, Gender, SocialInsuranceNumber (unique), HireDate, DepartmentId, DependentCount.

**InventoryItemOpeningBalance** (DD-006): Flat table `inventory_item_opening_balances` with `(inventory_item_id, warehouse_id)` composite unique per tenant. Stores opening stock quantity + value per warehouse. Not allowed for Service items (BR-IN05).

**FormulaTemplate/BOM** (FR-IN-015): Header-detail pattern. `InventoryQuantityFormulaTemplate` → `InventoryQuantityFormulaDetail[]`. Items reference templates via nullable FK (ON DELETE SET NULL). Full CRUD in `/api/formula-templates`.

**ItemAttributeType** (FR-IN-016): Tenant-level custom attribute definitions. Items reference via `InventoryItemAttribute` junction entity. Delete blocked if has_references.

### CQRS Handler Patterns

| Handler | Key Logic |
|---------|-----------|
| `GetAccountObjectsQueryHandler` | Paginated EF Core query with `ObjectType & filter != 0` bitwise, search by Code/Name ILIKE, status filter |
| `CreateAccountObjectCommandHandler` | Validate ObjectType ≥ 1, unique code per tenant (409 on conflict), cascade save BankAccounts + OpeningBalances; auto-create EmployeeProfile if Employee bit set (DD-007) |
| `UpdateAccountObjectCommandHandler` | RowVersion optimistic concurrency; block ObjectCode change if vouchers reference it; auto-create/soft-delete EmployeeProfile when Employee bit toggled |
| `DeleteAccountObjectCommandHandler` | Block hard-delete if voucher references; soft-delete always allowed |
| `GetInventoryItemsQueryHandler` | Category subtree filter (recursive CTE or in-memory category set), search, pagination, itemType filter |
| `CreateInventoryItemCommandHandler` | Full-replace child collections (unitConverts, barcodes, attributes, openingBalances); validate Service items have no openingBalances (BR-IN05) |
| `UpdateInventoryItemCommandHandler` | RowVersion concurrency; full-replace child collections on PUT |
| `CreateFormulaTemplateCommandHandler` | Header + details in single transaction; validate materialItemId references existing InventoryItem (FR-IN-015) |
| `DeleteFormulaTemplateCommandHandler` | Soft-delete; SET NULL on InventoryItem.FormulaTemplateId |
| `DeleteItemAttributeTypeCommandHandler` | Block delete if InventoryItemAttribute references exist (has_references) |
| `ImportAccountObjectsCommandHandler` | ClosedXML read → row-by-row validate → individual SaveChanges (best-effort, DD-001) |
| `ImportInventoryItemsCommandHandler` | Same pattern; resolves UnitCode/CategoryCode FKs by lookup before insert; resolves ItemType string to enum |

### Excel Import Architecture (FR-IMP-003 – FR-IMP-008)

```
POST /api/import/account-objects
  → ImportController → IMediator.Send(ImportAccountObjectsCommand { Stream fileStream })
  → ImportAccountObjectsCommandHandler
      1. Validate file size ≤ 5MB
      2. ClosedXML: workbook.Worksheet(1).RowsUsed()
      3. Validate row count ≤ 5000
      4. For each data row:
         a. Parse + validate fields (required, format)
         b. Check code uniqueness against DB
         c. If valid: context.AccountObjects.Add(entity); await context.SaveChangesAsync()
         d. If invalid: errors.Add({ rowNumber, field, message })
      5. Return ImportResultDto { successCount, errorCount, errors[] }
```

No batch-level transaction. Each valid row committed independently (DD-001).

### Frontend Signal Store Shape

```typescript
// account-objects.store.ts
interface AccountObjectsState {
  items: AccountObjectListItem[];
  selectedId: string | null;
  formMode: 'view' | 'edit' | 'create' | null;
  loading: boolean;
  saving: boolean;
  filters: {
    typeFlags: number;    // bitmask: 1=Customer, 2=Vendor, 4=Employee, 0=All
    status: 'all' | 'active' | 'inactive';
    search: string;
  };
  pagination: { page: number; pageSize: number; totalCount: number };
  error: string | null;
}

// inventory-items.store.ts  
interface InventoryItemsState {
  items: InventoryItemListItem[];
  categories: CategoryTreeNode[];         // full tree, built client-side
  selectedCategoryId: string | null;      // filters items to subtree
  selectedItemId: string | null;
  formMode: 'view' | 'edit' | 'create' | null;
  loading: boolean;
  saving: boolean;
  filters: {
    itemType: number | null;              // 0=RawMaterial, 1=FinishedProduct, 2=Goods, 3=Service, null=All
    search: string;
  };
  pagination: { page: number; pageSize: number; totalCount: number };
  error: string | null;
}
```

### API Performance Strategy

1. **AccountObjects list**: Server-side pagination (25/page default), ILIKE search on indexed columns
2. **InventoryItem category filter**: Pass `categoryId` to API; handler computes descendant IDs via recursive CTE in PostgreSQL then filters items
3. **Lookup dropdowns** (Unit, Currency, etc.): Full list cached in Redis (TTL=10min); invalidated on any write. Returns max 500 records per entity (sufficient for lookup purposes)
4. **Import file upload**: Multipart form-data, max 5MB enforced at both ASP.NET Core request body level and handler level

### EF Core Migration Strategy

Single new migration: `AddMasterData`
- 19 new tables (see data-model.md for full DDL)
- 19 EF Core configurations (see data-model.md §6)
- Seed data: Currency VND + 10 default units (FR-LK-007) in `MasterDataSeedData` class called from migration's `Up()` via `migrationBuilder.Sql()`
- Indexes: unique partial indexes + GIN full-text on Name columns

---

## Implementation Phases

### Phase A: Domain + Infrastructure (Backend Core)
1. Add ClosedXML NuGet to Infrastructure project
2. Create 19 Domain entities:
   - AccountObject, AccountObjectBankAccount, AccountObjectOpeningBalance, AccountObjectGroup, AccountObjectEmployeeProfile (DD-007)
   - InventoryItem, InventoryItemCategory, InventoryItemUnitConvert, InventoryQuantityFormulaTemplate, InventoryQuantityFormulaDetail, InventoryItemBarcode, ItemAttributeType, InventoryItemAttribute, InventoryItemOpeningBalance (DD-006)
   - Currency, Unit, Warehouse, Department, ExpenseItem
3. Create 3 enums: CostingMethod, ItemType, BarcodeType
4. Create 19 EF Core Configurations (fluent API mappings, indexes, unique constraints)
5. Modify `ApplicationDbContext`: add 19 DbSets + query filters
6. Modify `IApplicationDbContext`: add 19 DbSet properties
7. Run `dotnet ef migrations add AddMasterData`
8. Create `MasterDataSeedData.cs` and apply seed via migration
9. Create `ExcelImportService.cs` (ClosedXML wrapper)

### Phase B: Application Layer (CQRS)
1. AccountObject: DTOs (list, detail, bankAccount, openingBalance, employeeProfile) + 5 handlers (GetList, GetById, Create, Update, Delete)
   - Create/Update handlers must auto-manage EmployeeProfile (DD-007)
2. InventoryItem: DTOs (list with itemType/unitPrice, detail with child collections) + 5 handlers + category tree handler
   - Create/Update handlers must full-replace child collections (unitConverts, barcodes, attributes, openingBalances)
   - Validate Service items have no openingBalances (BR-IN05)
3. InventoryItemCategory: DTOs + 4 handlers (GetTree, Create, Update, Delete with has_children/has_items check)
4. FormulaTemplate: DTOs (list with detailCount, detail with material lines) + 5 handlers (GetList, GetById, Create with details, Update full-replace, Delete soft-delete with SET NULL)
5. ItemAttributeType: DTOs + 4 handlers (GetList, Create, Update, Delete with has_references check)
6. Lookup entities: DTOs (all with isActive) × 5 entities:
   - Currency, Unit, Warehouse, ExpenseItem: 4 handlers each (GetList, Create, Update, Delete)
   - **Department: 5 handlers** (GetTree, GetList, Create, Update, Delete) — tree entity with level validation (BR-DI05, max 5 levels)
   - Currency: include currencyNameEnglish field
   - Unit DELETE: check references in InventoryItem, InventoryItemUnitConvert, InventoryItemOpeningBalance, FormulaDetail
   - Warehouse DELETE: check references in InventoryItemOpeningBalance
   - Department Create/Update: validate level ≤ 5 via parent chain walk (same pattern as InventoryItemCategory)
7. Import: `ImportResultDto` + 2 import handlers (AccountObjects, InventoryItems)
   - InventoryItem import: resolve ItemType string to enum, include UnitPrice column
8. Import template generators (ClosedXML write — one per entity type)
9. FluentValidation validators for all Create/Update commands (including child entity validation)

### Phase C: API Layer
1. `AccountObjectsController.cs` — 5 endpoints (GET list, GET by id with employeeProfile+children, POST, PUT, DELETE)
2. `InventoryItemsController.cs` — 5 endpoints (GET list with itemType filter, GET by id with child collections, POST, PUT, DELETE)
3. `InventoryItemCategoriesController.cs` — GET tree + CRUD (4 endpoints)
4. `FormulaTemplatesController.cs` — 5 endpoints: GET list, GET by id with details, POST with details, PUT full-replace, DELETE (FR-IN-015)
5. `ItemAttributeTypesController.cs` — 4 endpoints: GET list, POST, PUT, DELETE (FR-IN-016)
6. Lookup controllers: `CurrenciesController`, `UnitsController`, `WarehousesController`, `DepartmentsController`, `ExpenseItemsController` — 4 endpoints each (all support isActive)
7. `ImportController.cs` — 4 endpoints (2 templates, 2 uploads)
8. Register all new controllers in DI (auto-detected by convention)

### Phase D: Frontend — Account Objects
1. `master-data.models.ts` — all TypeScript interfaces (including EmployeeProfile, child collection types)
2. `account-objects-api.service.ts` — HTTP methods
3. `account-objects.store.ts` — NgRx Signals store
4. `AccountObjectsListComponent` — PrimeNG DataTable + toolbar + type filter checkboxes + status dropdown
5. `AccountObjectFormComponent` — 4 tabs:
   - Tab 1: Thông tin chung (General) — reactive form with all core fields + accountObjectGroupId dropdown
   - Tab 2: Thông tin nhân viên (Employee Profile) — visible only when Employee bit set (DD-007); CitizenId, DateOfBirth, Gender, SocialInsuranceNumber, HireDate, DepartmentId, DependentCount
   - Tab 3: Tài khoản ngân hàng (Bank Accounts) — inline DataTable
   - Tab 4: Số dư đầu kỳ (Opening Balance) — inline DataTable per currency
6. `ImportAccountObjectsDialogComponent` — 3-step: download template / upload file+preview / result
7. `AccountObjectsPageComponent` — master-detail layout
8. Add keyboard shortcuts (Ctrl+S, Ctrl+Shift+S, Insert, Ctrl+Delete, F3)
9. Dirty-form guard
10. Wire routes in `di.routes.ts`
11. Add i18n keys

### Phase E: Frontend — Inventory Items
1. `inventory-items-api.service.ts` + `formula-templates-api.service.ts` + `item-attribute-types-api.service.ts`
2. `inventory-items.store.ts` — includes category tree loading + computed descendant filter
3. `InventoryItemsListComponent` — split layout (p-splitter): left `p-tree` categories, right `p-table` with itemType filter
4. `InventoryItemFormComponent` — 7 tabs:
   - Tab 1: Thông tin chung (General) — ItemType, ItemCode, ItemName, UnitId, UnitPrice, SalePrice1-3, DefaultTaxRate, CostingMethod, CategoryId, FormulaTemplateId
   - Tab 2: Đơn vị chuyển đổi (Unit Conversions) — inline DataTable for UnitConvert entries (Gap I)
   - Tab 3: Định mức NVL (BOM) — select FormulaTemplate, display material lines read-only (FR-IN-015)
   - Tab 4: Kho & tồn kho (Stock Settings) — MinStock, MaxStock, tracking flags (IsSerial, IsLot, IsPanel + PanelUnitId)
   - Tab 5: Mã vạch (Barcodes) — inline DataTable for multi-barcode (Gap P)
   - Tab 6: Thuộc tính (Attributes) — inline DataTable for custom attributes (Gap N)
   - Tab 7: Số dư đầu kỳ (Opening Balance) — inline DataTable per warehouse (DD-006); hidden for Service items (BR-IN05)
5. `FormulaTemplatesPageComponent` — standalone CRUD for BOM templates (FR-IN-015); list with detailCount, detail form with material lines inline DataTable
6. `ItemAttributeTypesPageComponent` — standalone list + inline edit for attribute type definitions (FR-IN-016)
7. `ImportInventoryItemsDialogComponent` — same 3-step pattern
8. `InventoryItemsPageComponent`
9. Wire routes (including `/di/inventory-items/formula-templates` and `/di/inventory-items/attribute-types`)

### Phase F: Frontend — Lookup Data (Setup)
1. `lookups-api.service.ts` — single service for all 5 lookup entities (all endpoints support isActive)
2. `CurrenciesComponent` — inline-edit DataTable (includes currencyNameEnglish + isActive toggle)
3. `UnitsComponent` — inline-edit DataTable (includes isActive toggle)
4. `WarehousesComponent` — inline-edit DataTable (includes isActive toggle)
5. `DepartmentsComponent` — p-tree with add/edit/delete per node (same pattern as Account Tree)
6. `ExpenseItemsComponent` — list + dialog form
7. Wire setup sub-routes under `/di/setup/`
8. Add i18n keys

### Phase G: Tests
1. Unit: `CreateAccountObjectCommandHandler` — EmployeeProfile auto-create when Employee bit set, auto-delete when bit removed (DD-007)
2. Unit: `ImportAccountObjectsCommandHandler` — validation edge cases (missing required, duplicate code, row > 5000)
3. Unit: `ImportInventoryItemsCommandHandler` — invalid UnitCode, invalid CategoryCode, ItemType resolution
4. Unit: Category/Department depth validation (level 5 allowed, level 6 blocked)
5. Unit: ObjectType bitmask validation (0 blocked, valid combos allowed)
6. Unit: InventoryItemOpeningBalance — Service items blocked (BR-IN05), composite unique constraint
7. Unit: FormulaTemplate CRUD — detail full-replace, soft-delete with SET NULL on items
8. Unit: ItemAttributeType DELETE — has_references blocking
9. Unit: Unit/Warehouse DELETE — expanded reference checks
10. Integration: AccountObjects CRUD via API (create → get → update → delete, including EmployeeProfile)
11. Integration: InventoryItems CRUD via API (with child collections: unitConverts, barcodes, attributes, openingBalances)
12. Integration: FormulaTemplate CRUD via API
13. Integration: Lookup CRUD (Unit delete blocked when referenced by UnitConvert/FormulaDetail)
14. Frontend: AccountObjectsStore — filter/search signal computation
15. Frontend: InventoryItemFormComponent — 7-tab rendering, conditional OpeningBalance tab hidden for Service items (BR-IN04)
16. Frontend: ImportDialog — step navigation

---

## Dependencies & Pre-conditions

| Dependency | Status | Notes |
|-----------|--------|-------|
| `di-account-tree` merged | Required | `Accounts` table + `Account.AccountNumber` must exist for ExpenseItem FK reference |
| ClosedXML NuGet | Required | Add before Phase A step 1 |
| ApplicationDbContext migration | Required | Run after Phase A step 6 |
| Currency seed data (VND) | Required | Must exist before any AccountObjectOpeningBalance can be created |

---

## Risks & Mitigations

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| ClosedXML memory usage on large files | Low | File size cap = 5MB; row count cap = 5000; streaming read within ClosedXML |
| Category subtree query performance at depth 5 | Low | PostgreSQL recursive CTE on indexed `ParentId`; cache category tree in store |
| ObjectType bitmask filter correctness | Medium | Explicit unit tests for each bitmask combination (1,2,3,4,5,6,7) |
| Optimistic concurrency on concurrent edits | Low | RowVersion conflict returns HTTP 409; frontend shows "modified by another user" toast |
| Import best-effort partial commit (DD-001) | Low | Clearly communicated in UI result summary; error report Excel for failed rows |
| 5-level depth enforcement requires tree traversal | Low | Compute level during Create/Update command via parent chain walk (max 5 queries) |
| EmployeeProfile 1:1 lifecycle complexity (DD-007) | Medium | Thorough unit tests for Employee bit toggle; auto-create on set, soft-delete on unset |
| FormulaTemplate SET NULL on delete | Low | EF Core ON DELETE SET NULL via FK config; verify with integration test |
| 19-entity migration size | Low | Single migration is fine; EF Core handles ordering; seed data via `migrationBuilder.Sql()` |
| Child collection full-replace on PUT | Medium | Clear API contract docs; frontend always sends complete collections; handler deletes old + inserts new in single transaction |
