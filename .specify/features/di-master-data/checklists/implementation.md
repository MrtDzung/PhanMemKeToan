# Implementation Quality Checklist: DI Master Data (Account Objects & Items — Part 2)

**Purpose**: Unit tests for requirements — validate completeness, clarity, consistency, and coverage of all implementation artifacts before coding begins  
**Created**: 2026-04-18  
**Feature**: `di-master-data` (spec.md v4, data-model.md v4, plan.md v4, 4 API contracts v4)  
**Scope**: 19 entities, 9 sub-features, 4 API contracts, Angular 20 frontend, ClosedXML import  
**Depth**: Comprehensive (release-gate quality)

---

## Data Model Completeness

- [ ] CHK001 — Are all 19 entities listed in plan.md's project structure accounted for in data-model.md with complete C# class definitions and PostgreSQL DDL? [Completeness, plan.md §Backend Source Files]
- [ ] CHK002 — Do all 19 entities specify their EF Core `IEntityTypeConfiguration` including table name, column types, FK relationships, and index definitions? [Completeness, data-model.md]
- [ ] CHK003 — Are the AuditableEntity base class fields (TenantId, IsDeleted, CreatedAt/By, UpdatedAt/By) listed in every PostgreSQL DDL block, not just the C# class definition? [Completeness, Gap]
- [ ] CHK004 — Is the `InventoryItem` dual-FK to `Unit` (UnitId + PanelUnitId pointing to the same table) explicitly resolved in EF Core config to prevent the "two navigations from same FK" error? [Clarity, data-model.md §InventoryItemConfiguration note]
- [ ] CHK005 — Are all 3 enums (`CostingMethod`, `InventoryItemType`, `BarcodeType`) defined with integer backing values, descriptions, and traceability to spec business rules? [Completeness, Spec §FR-IN-003/007/032]
- [ ] CHK006 — Is the `RowVersion` field on `AccountObject` and `InventoryItem` specified as a proper EF Core concurrency token (`IsConcurrencyToken()`)? [Completeness, Edge Cases §optimistic concurrency]

---

## Constraint & Index Specifications

- [ ] CHK007 — Are the partial unique indexes for `CitizenId` and `SocialInsuranceNumber` (filtering `WHERE value IS NOT NULL AND IsDeleted = false`) specified in both the PostgreSQL DDL and the EF Core config? [Completeness, BR-DI07/08, Spec §FR-AO-032/033]
- [ ] CHK008 — Is the composite unique constraint `(TenantId, InventoryItemId, WarehouseId, UnitId)` on `InventoryItemOpeningBalance` specified in DDL? [Completeness, BR-IN03]
- [ ] CHK009 — Is the `BarcodeValue` unique-per-tenant index specified in DDL with `WHERE IsDeleted = false`? [Completeness, BR-IN05]
- [ ] CHK010 — Is the unique constraint preventing duplicate `UnitId` per item in `InventoryItemUnitConvert` specified in DDL? [Completeness, BR-IN06]
- [ ] CHK011 — Are text-search performance indexes (e.g., on `ObjectCode`, `ObjectName`, `ItemCode`, `ItemName`) specified in data-model.md to support SC-002 (< 1s for 10K rows) and SC-003 (search < 500ms)? [Coverage, SC-002/003/004, Gap]
- [ ] CHK012 — Is the `ON DELETE` behavior explicitly specified for all foreign keys (Cascade, SetNull, Restrict)? Specifically: EmployeeProfile → AccountObject (Cascade), EmployeeProfile → Department (SetNull), FormulaTemplateId on InventoryItem → FormulaTemplate (SetNull), and Category → parent (Restrict)? [Completeness, data-model.md, Edge Cases]

---

## API Contract Completeness

- [ ] CHK013 — Does the `GET /api/account-objects/{id}` response schema include all child collections: `bankAccounts`, `openingBalances` (with OC fields), and `employeeProfile`? [Completeness, FR-AO-011, contracts/account-objects-api.md]
- [ ] CHK014 — Does the `GET /api/inventory-items/{id}` response schema include all 4 child collections: `unitConverts`, `barcodes`, `itemAttributes`, and `openingBalances`? [Completeness, FR-IN-011, contracts/inventory-items-api.md]
- [ ] CHK015 — Are all 5 Lookup entity APIs (Currency, Unit, Warehouse, Department, ExpenseItem) with full CRUD documented in `contracts/lookups-api.md`? [Completeness, FR-LK-001…006]
- [ ] CHK016 — Is there a documented endpoint for downloading the import error report Excel file? The import result UI (FR-IMP-009 step 3) references a downloadable error report but `import-api.md` does not define a `GET /api/import/error-report/{id}` endpoint. [Coverage, FR-IMP-009, **Gap**]
- [ ] CHK017 — Does the `DELETE /api/account-objects/{id}` contract specify the precise decision logic: "soft-deactivate if has voucher references, hard delete if no references"? Is this distinguishable from a client-perspective (204 vs 200 with body)? [Clarity, FR-AO-014, BR-DI03]
- [ ] CHK018 — Are all 8 AccountObject error codes (`duplicate_code`, `duplicate_citizen_id`, `duplicate_social_insurance_number`, `row_version_conflict`, `code_change_locked`, `has_voucher_references`, `account_object_not_found`, `validation_error`) present in the Error Codes Reference table? [Completeness, contracts/account-objects-api.md]

---

## API Contract Consistency

- [ ] CHK019 — Does the `AccountObjectOpeningBalance` response in `account-objects-api.md` include the foreign-currency fields (`debitAmountOC`, `creditAmountOC`, `exchangeRate`) that are defined in `data-model.md` and required by DD-004? [Consistency, **Gap** — current response only shows `debitAmount`/`creditAmount`]
- [ ] CHK020 — Is the `type` bitmask filter semantics (`ObjectType & type != 0`) consistently described in both `spec.md` FR-AO-010 and the `account-objects-api.md` query parameter table and its Type Bitmask Reference? [Consistency]
- [ ] CHK021 — Does the `PUT /api/account-objects/{id}` contract describe the EmployeeProfile soft-delete behavior when the Employee bit is removed — specifically that the server automatically soft-deletes the profile (FR-AO-034/DD-007)? [Completeness, DD-007, contracts/account-objects-api.md]
- [ ] CHK022 — Are the InventoryItem child collection field names (`unitConverts`, `barcodes`, `itemAttributes`, `openingBalances`) consistent between `inventory-items-api.md`, `plan.md` DTO definitions, and `spec.md` FR-IN-011? [Consistency]
- [ ] CHK023 — Are the `itemType` enum integer values (0=RawMaterial, 1=FinishedProduct, 2=Goods, 3=Service) consistent between `data-model.md`, `inventory-items-api.md` response, and `import-api.md` template values ("NVL", "TP", "HH", "DV")? [Consistency, Spec §FR-IN-001 vs import-api.md §Template columns]

---

## Business Rules Coverage (BR-DI01–08, BR-IN01–06)

- [ ] CHK024 — Is BR-DI01 (ObjectType ≥ 1) enforced in both the API contract (POST/PUT validation error response) AND as a domain-layer check? Is the exact error message ("Phải chọn ít nhất một loại đối tượng") specified in the API contract? [Completeness, BR-DI01]
- [ ] CHK025 — Is BR-DI03 (hard-delete blocked if voucher references) implementable in this sprint without voucher tables existing yet? Is the reference-check strategy (query against which tables?) documented in plan.md? [Clarity, BR-DI03, **Gap**]
- [ ] CHK026 — Is BR-DI05 (max 5 tree levels) enforced consistently for BOTH `InventoryItemCategory` AND `Department`? Does each API return a 422 `max_depth_exceeded` error for a 6th-level node attempt? [Consistency, BR-DI05, contracts/inventory-items-api.md]
- [ ] CHK027 — Is BR-IN04 (Service items have no opening stock) enforced at the backend API level (POST to opening balance for Service item → 422) — not only at the UI level (tab hidden)? [Completeness, BR-IN04, **Gap**]
- [ ] CHK028 — Is BR-IN06 (secondary UnitId ≠ item's main UnitId, unique per item) enforced in the `POST /api/inventory-items` validation rules in the API contract? [Completeness, BR-IN06, **Gap**]
- [ ] CHK029 — Are BR-DI07 and BR-DI08 (CitizenId/BHXH unique per tenant) distinguishable as SEPARATE error codes (`duplicate_citizen_id` vs `duplicate_social_insurance_number`) to enable UI-specific field error highlighting? [Clarity, BR-DI07/08, contracts/account-objects-api.md §Error Codes]
- [ ] CHK030 — Is the import duplicate-skip behavior (BR-DI06: skip and report, do not overwrite) consistent with the best-effort model (DD-001, FR-IMP-006)? Is the error message for duplicate-during-import ("Mã đã tồn tại") distinct from duplicate-on-direct-create ("Mã đối tượng X đã tồn tại")? [Consistency, BR-DI06 vs FR-IMP-007]

---

## Design Decisions Implementation Clarity (DD-001–007)

- [ ] CHK031 — Is DD-001 (best-effort import: no batch rollback) clearly reflected in the `import-api.md` processing logic section? Specifically, does the spec define the behavior when the same file is re-uploaded after a partial failure (duplicate rows would be skipped, not re-inserted)? [Clarity, DD-001]
- [ ] CHK032 — Is DD-002 (ObjectType bitmask, 3 checkboxes in UI) fully specified from both directions — does FR-AO-021 define how unchecking ALL boxes is handled before save (BR-DI01 validation fires on save, not on uncheck)? [Completeness, DD-002, FR-AO-021]
- [ ] CHK033 — Does DD-003 (5-level max) apply to the `Department` entity even though `Department` is managed as a lookup (FR-LK-004) rather than as a primary entity? Is the 5-level enforcement explicitly stated in `lookups-api.md`? [Consistency, DD-003, FR-LK-004]
- [ ] CHK034 — Is DD-004 (multi-currency opening balances) fully reflected in data-model.md DDL for `account_object_opening_balances`? Does the DDL include `DebitAmountOC`, `CreditAmountOC`, `ExchangeRate` columns as defined in the C# entity? [Consistency, DD-004, data-model.md §OpeningBalance]
- [ ] CHK035 — Is DD-005 (Warehouse CRUD in DI, not deferred) reflected in `lookups-api.md` with complete Warehouse CRUD endpoints (not just GET for dropdown)? [Completeness, DD-005, FR-LK-003]
- [ ] CHK036 — Is DD-006 (InventoryItemOpeningBalance flat table, future INInward conversion) in FR-IN-044 clearly marked as OUT-OF-SCOPE for this sprint, with the future batch-job strategy documented without creating false implementation obligations? [Clarity, DD-006, FR-IN-044]
- [ ] CHK037 — Is DD-007 (EmployeeProfile 1:1 extension) accompanied by a spec for the "Employee tab becomes visible" UI behavior — specifically does the tab appear immediately when the Employee checkbox is checked (reactive) or only after save? [Clarity, DD-007, US10-1, **Gap**]

---

## Frontend Requirements — AccountObject Form

- [ ] CHK038 — Is the conditional visibility of the "Thông tin nhân viên" tab in the AccountObject form (FR-AO-023) specified for ALL states: (a) new form with Employee checked, (b) edit form with Employee bit set, (c) edit form after Employee bit is unchecked but before save? [Completeness, FR-AO-023, US10-1]
- [ ] CHK039 — Is the keyboard shortcut context behavior fully specified — specifically that `Insert` adds a bank account row ONLY when the Bank Accounts tab is active, not when other tabs are focused? [Clarity, FR-AO-024, Gap]
- [ ] CHK040 — Is the `DepartmentId` field on the Employee tab specified to use a hierarchical tree-select component (PrimeNG TreeSelect or equivalent), and is the "active departments only" filter requirement traceable to a query param in `lookups-api.md`? [Completeness, US10-5, FR-AO-030]
- [ ] CHK041 — Are the column persistence requirements (FR-AO-022, localStorage) specified with a key naming convention (e.g., `account-objects-list-columns-{userId}`) to prevent collisions across modules or users? [Clarity, FR-AO-022, Gap]
- [ ] CHK042 — Is the dirty-form guard (FR-AO-026) specified to cover BOTH the parent form fields AND changes to inline rows (bank accounts, opening balances) on child tabs? [Completeness, FR-AO-026]

---

## Frontend Requirements — InventoryItem Form

- [ ] CHK043 — Are all 7 InventoryItem form tabs listed with their EXACT conditional visibility rules? FR-IN-021 appears truncated in spec.md — specifically Tab 5 (BOM), Tab 6 (Stock Settings), Tab 7 (Opening Stock) need explicit visibility conditions. [Completeness, FR-IN-021, **Gap**]
- [ ] CHK044 — Is there a consistency issue between FR-IN-021 (implies 7 tabs: General, UnitConversions, Barcodes, Attributes, BOM, Stock Settings, Opening Stock) and FR-IN-023 (states only "Tab 1 General, Tab 2 Stock Settings")? Which is the authoritative tab specification? [Consistency, FR-IN-021 vs FR-IN-023, **Conflict**]
- [ ] CHK045 — Is the split-panel category tree + DataTable layout (FR-IN-020) specified with responsive behavior — what happens on narrow screens or when the tree is collapsed? [Clarity, FR-IN-020, Gap]
- [ ] CHK046 — Is category-subtree filtering behavior (FR-IN-022) precisely defined — does "category and all descendants" use a recursive CTE query, or a pre-computed path column? Is the spec technology-neutral enough to allow either? [Clarity, FR-IN-022]
- [ ] CHK047 — Is the `MaxStockLevel > MinStockLevel` validation (FR-IN-026) specified as: (a) server-side only, (b) client-side only, or (c) both? Is the exact condition ("when both are non-zero") consistent with the API contract validation rules? [Consistency, FR-IN-026]

---

## Frontend Requirements — Lookup Screens

- [ ] CHK048 — Are the lookup UI requirements free of duplicate/conflicting IDs? Spec §3.9 contains both `FR-LK-010..013` and `FR-LK-020..025` blocks that appear to cover the same screens. Which set is authoritative? [Consistency, Spec §3.9, **Conflict**]
- [ ] CHK049 — Is the inline-edit DataTable requirement (FR-LK-021) specified with detail on save behavior — are edits auto-saved on row blur, or is there a per-row save action? [Clarity, FR-LK-021]
- [ ] CHK050 — Is the FormulaTemplate master-detail layout (FR-LK-025) specified with enough detail to implement — specifically how detail lines (material items) are added, edited, and deleted within the UI? [Completeness, FR-LK-025, Gap]
- [ ] CHK051 — Are tenant seed data requirements (FR-LK-007: VND + 10 units) tied to a specific application event (tenant creation hook, EF migration seed, API endpoint)? Is the trigger mechanism documented? [Completeness, FR-LK-007, Gap]

---

## Import/Export Requirements

- [ ] CHK052 — Is the 3-step import dialog (FR-IMP-009) complete — specifically: what is the "file preview" (step 2) behavior when the file has fewer than 10 data rows (show all), zero data rows (empty state), or parsing errors in the first row? [Completeness, FR-IMP-009, Edge Cases]
- [ ] CHK053 — Is the error report download file format specified: does it reuse the original import template columns with an added "Lỗi" column, or is it a custom format? Is the exact column structure documented? [Clarity, US8-5, Gap]
- [ ] CHK054 — Does the import API contract specify behavior for files containing Excel formulas, merged cells, or password-protected sheets? The spec's edge cases section defines this but `import-api.md` processing logic does not mention it. [Consistency, Edge Cases vs import-api.md §Processing Logic]
- [ ] CHK055 — Is the `ObjectType` text parsing for AccountObject import (e.g., `"Khách hàng,Nhà cung cấp"` → bitmask 3) specified as case-insensitive, and are comma/semicolon separators both accepted? [Clarity, import-api.md §Template columns]
- [ ] CHK056 — Is ClosedXML (MIT license, not EPPlus) explicitly documented in plan.md as the mandatory Excel library, with the reason (commercial license avoidance) noted? [Completeness, plan.md §Technical Context]

---

## Multi-Tenant Security Requirements

- [ ] CHK057 — Is the TenantId global query filter (`HasQueryFilter`) requirement specified for ALL 19 entities — not just the two explicitly mentioned (FR-AO-006, FR-IN-006)? [Completeness, Security]
- [ ] CHK058 — Are cross-tenant injection risks in the import API addressed — specifically, is TenantId taken from the JWT claim (not from the Excel file content or URL parameters)? [Completeness, Security, Gap]
- [ ] CHK059 — Is the partial unique index on CitizenId and SocialInsuranceNumber scoped to `TenantId` (per-tenant unique, not globally unique), and is this explicitly stated in the DDL WHERE clause? [Clarity, BR-DI07/08, data-model.md]
- [ ] CHK060 — Are JWT Bearer auth requirements applied to ALL API endpoints, including the template download endpoints (`GET /api/import/template/*`)? Is any endpoint intentionally public? [Coverage, Security, Gap]
- [ ] CHK061 — Is file upload security specified for the import API — specifically: MIME type validation (not just file extension), maximum file size enforcement before parsing (5 MB, FR-IMP-008), and protection against zip-bomb attacks (ClosedXML with a large XLSX)? [Completeness, FR-IMP-008, Security]

---

## Performance Requirements

- [ ] CHK062 — Are the database index requirements for text search on `ObjectCode`/`ObjectName` and `ItemCode`/`ItemName` specified (e.g., pg_trgm GIN indexes or `ILIKE` index) to achieve SC-003 (search < 500ms)? [Completeness, SC-003, Gap]
- [ ] CHK063 — Is the category subtree query strategy (recursive CTE, materialized path, or adjacency list) specified in data-model.md or plan.md to support SC-004 (50K items, category filter < 1s)? [Completeness, SC-004, Gap]
- [ ] CHK064 — Is the Redis cache strategy for lookup dropdowns (Currency, Unit, Warehouse, Department) documented — specifically cache key format, TTL, and invalidation on CRUD operations? [Completeness, plan.md §Technical Context, Gap]
- [ ] CHK065 — Is the batch-import performance strategy documented — does the spec address whether row-by-row `SaveChanges` (current spec) meets the 30-second SLA for 1,000 rows (SC-005/006), or should bulk insert be used? [Clarity, SC-005/006, Gap]
- [ ] CHK066 — Are virtual scroll requirements specified with a threshold (e.g., activate virtual scroll when row count > N) for both the AccountObject and InventoryItem list screens? [Completeness, design-system rules]

---

## Test Coverage Requirements

- [ ] CHK067 — Are the key business rule test scenarios (ObjectType bitmask filter, CitizenId uniqueness, opening balance auto-calc, optimistic concurrency conflict) listed in plan.md's test strategy, or are tests left entirely to the implementation phase? [Coverage, plan.md, Gap]
- [ ] CHK068 — Is tenant isolation testing specified — specifically a test that AccountObject from TenantA is not accessible via `GET /api/account-objects/{id}` from a JWT belonging to TenantB? [Coverage, Security]
- [ ] CHK069 — Are import error scenarios (missing required field, duplicate code, invalid UnitCode reference, invalid CategoryCode reference) each represented as a distinct test scenario in the spec? [Coverage, FR-IMP-005/007]
- [ ] CHK070 — Is the FormulaTemplate soft-delete cascade effect (FormulaTemplateId set to NULL on referencing InventoryItems) covered by a specified test case? [Coverage, Edge Cases §BOM deletion]

---

## Cross-Artifact Consistency

- [ ] CHK071 — Does the `AccountObjectOpeningBalance` C# entity in data-model.md (which includes `DebitAmountOC`, `CreditAmountOC`, `ExchangeRate`) match the columns in its PostgreSQL DDL block? If the DDL is missing those columns, is that a gap? [Consistency, data-model.md §OpeningBalance]
- [ ] CHK072 — Is `OpeningDate` (required field per FR-IN-040) present in the `InventoryItemOpeningBalance` DDL in data-model.md? [Consistency, FR-IN-040 vs data-model.md, Gap]
- [ ] CHK073 — Is there a conflict between FR-IN-021 ("Tab 2 'Đơn vị tính phụ'") and FR-IN-023 ("Tab 2 'Cài đặt kho'") for the InventoryItem detail form? One of these is incorrect — which is authoritative? [Consistency, **Conflict**]
- [ ] CHK074 — Are the `AccountObjectGroup` entity requirements (GroupCode, GroupName, GroupType bitmask) complete enough for the FK reference from `AccountObject.AccountObjectGroupId` to be implemented, even though Group CRUD is deferred (BR-E)? [Completeness, data-model.md, Gap]
- [ ] CHK075 — Is the spec for what constitutes "posted voucher references" for BR-DI03 (hard-delete prevention) documented — specifically which tables/columns will be checked, given no voucher tables exist in this sprint? [Clarity, BR-DI03, **Gap**]

---

## Ambiguities & Deferred Items

- [ ] CHK076 — Is `AccountObjectGroup` CRUD deferral (BR-E) explicitly scoped out with a note like "CRUD managed in future sprint X" rather than just a comment in the entity class? [Clarity, Gap]
- [ ] CHK077 — Is the "warning but non-blocking deactivation" behavior for AccountObjects with open unposted vouchers (US4-3) specified at the API level — what HTTP response code does the successful deactivation return when there are open vouchers? [Clarity, US4-3, Gap]
- [ ] CHK078 — Is the behavior for changing an InventoryItem's main unit after unit conversions exist (edge case: "system warns, requires confirmation") specified at the API level — what error code or response triggers the confirmation dialog? [Clarity, Edge Cases, Gap]

---

**Summary**

| Category | Items | Key Gaps Found |
|---|---|---|
| Data Model Completeness | CHK001–CHK006 | AuditableEntity in DDL, dual-FK resolution, RowVersion concurrency token |
| Constraints & Indexes | CHK007–CHK012 | Text-search indexes missing, ON DELETE behaviors incomplete |
| API Contract Completeness | CHK013–CHK018 | Error report download endpoint missing (CHK016) |
| API Contract Consistency | CHK019–CHK023 | OC fields missing from AO opening balance response (CHK019) |
| Business Rules Coverage | CHK024–CHK030 | BR-DI03 cross-module check (CHK025), BR-IN04/06 backend enforcement (CHK027/028) |
| Design Decisions | CHK031–CHK037 | DD-007 Employee tab reactive visibility (CHK037) |
| Frontend — AccountObject | CHK038–CHK042 | Key shortcut tab-context (CHK039), localStorage key naming (CHK041) |
| Frontend — InventoryItem | CHK043–CHK047 | **7-tab vs 2-tab conflict** (CHK044 — HIGH PRIORITY) |
| Frontend — Lookup Screens | CHK048–CHK051 | Duplicate FR-LK IDs (CHK048 — HIGH PRIORITY), seed data trigger (CHK051) |
| Import/Export | CHK052–CHK056 | Error report format (CHK053), file security (CHK054) |
| Security | CHK057–CHK061 | TenantId on all 19 entities (CHK057), template download auth (CHK060) |
| Performance | CHK062–CHK066 | pg_trgm indexes (CHK062), Redis strategy (CHK064), batch import perf (CHK065) |
| Test Coverage | CHK067–CHK070 | Test scenarios largely undocumented |
| Cross-Artifact Consistency | CHK071–CHK075 | OpeningDate missing from DDL (CHK072), **FR-IN-021 vs FR-IN-023 tab conflict** (CHK073) |
| Ambiguities & Deferred | CHK076–CHK078 | Hard-delete reference check (CHK075/CHK076), deactivation response code (CHK077) |

**HIGH PRIORITY items (must resolve before tasks.md)**: CHK016, CHK019, CHK025, CHK027, CHK028, CHK044, CHK048, CHK072, CHK073
