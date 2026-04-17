# Tasks: DI Module — Account Tree

**Feature**: DI Module — Account Tree (Hệ thống Tài khoản Kế toán)  
**Branch**: `feature/di-account-tree`  
**Generated**: 2026-04-17  
**Spec**: `spec.md` | **Plan**: `plan.md`

---

## Summary

| Phase | Description | Tasks | Complexity |
|-------|-------------|-------|------------|
| A | Domain + Infrastructure | 10 | S–M |
| B | Application Layer (CQRS) | 11 | M–L |
| C | API Layer | 3 | M |
| D | Frontend | 14 | M–L |
| E | Tests | 6 | M–L |
| **Total** | | **43** | |

---

## Dependencies Graph

```
Phase A → Phase B → Phase C → Phase D
Phase A → Phase E (integration tests)
Phase B → Phase E
Phase C → Phase D (API contract)
```

User story completion order (from spec.md priorities):
- **US-DI-001** (view tree) requires: TA01–TA07, TB01–TB05, TC01, TD01–TD04, TD07, TD12–TD13
- **US-DI-002** (create account) adds: TB08, TC01, TD09–TD10
- **US-DI-003** (edit account) adds: TB09, TC01, TD09–TD10
- **US-DI-004** (deactivate account) adds: TB10, TC01
- **US-DI-005** (search) adds: TB07, TC01, TD06, TD08
- **US-DI-006** (import CoA) adds: TA08–TA09, TB11, TC01, TD11
- **US-DI-007** (status filter) adds: TD08 (filter in toolbar)
- **US-DI-008** (usage indicator) adds: TB06 `hasTransactions` field

---

## Phase A: Domain + Infrastructure

> Goal: Entity, EF Core configuration, migration, and seed JSON files.  
> Must complete before any application-layer work.

- [X] TA01 [P] Create `AccountCategoryKind` enum in `src/PhanMemKeToan.Domain/Enums/AccountCategoryKind.cs` — Complexity: **S**
  - Values: `Debit = 0`, `Credit = 1`
  - No dependencies

- [X] TA02 [P] Create `AccountObjectType` enum in `src/PhanMemKeToan.Domain/Enums/AccountObjectType.cs` — Complexity: **S**
  - Values: `None = 0`, `Supplier = 1`, `Customer = 2`, `Employee = 3`
  - No dependencies

- [X] TA03 Create `Account` entity in `src/PhanMemKeToan.Domain/Entities/Account.cs` — Complexity: **M**
  - Extends `AuditableEntity` (inherits `Id`, `TenantId`, `CreatedAt/By`, `ModifiedAt/By`, `IsDeleted`)
  - All fields per data-model.md: `AccountNumber`, `AccountName`, `AccountNameEnglish`, `ParentID`, `Grade`, `IsParent`, `AccountCategoryKind`, `Inactive`, `IsPostableInForeignCurrency`, all `DetailBy*` flags, `AccountObjectType`, `RowVersion`, `MISACodeID`
  - Navigation props: `Parent`, `Children`
  - Dependencies: TA01, TA02

- [X] TA04 Create EF Core configuration in `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/AccountConfiguration.cs` — Complexity: **M**
  - Implement `IEntityTypeConfiguration<Account>`
  - Table: `"Accounts"`, all column mappings per data-model.md DDL
  - Self-referencing FK: `HasOne(a => a.Parent).WithMany(a => a.Children).HasForeignKey(a => a.ParentID).OnDelete(DeleteBehavior.Restrict)`
  - Unique partial index: `UIX_Accounts_TenantId_AccountNumber` WHERE `IsDeleted = false`
  - Indexes: `IX_Accounts_AccountNumber_Pattern` (text_pattern_ops via `HasMethod("GIN")` on `AccountNumber`), `IX_Accounts_AccountName_GIN` (GIN on `to_tsvector`), `IX_Accounts_ParentID`, `IX_Accounts_TenantId`
  - Enum conversions: `AccountCategoryKind` → int, `AccountObjectType` → int
  - Dependencies: TA03

- [X] TA05 Add `DbSet<Account>` to `src/PhanMemKeToan.Application/Common/Interfaces/IApplicationDbContext.cs` — Complexity: **S**
  - Add: `DbSet<Account> Accounts { get; }`
  - Dependencies: TA03

- [X] TA06 Add `DbSet<Account>` and global query filter to `src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs` — Complexity: **S**
  - Add: `public DbSet<Account> Accounts { get; set; }`
  - Add global query filter in `OnModelCreating`: `modelBuilder.Entity<Account>().HasQueryFilter(a => a.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !a.IsDeleted);`
  - Dependencies: TA03, TA04, TA05

- [X] TA07 Generate EF Core migration `AddAccountTree` in `src/PhanMemKeToan.Infrastructure/Migrations/` — Complexity: **S**
  - Run: `dotnet ef migrations add AddAccountTree --project src/PhanMemKeToan.Infrastructure --startup-project src/PhanMemKeToan.Api`
  - Verify generated migration creates `Accounts` table + all 5 indexes per data-model.md
  - Dependencies: TA06

- [X] TA08 Create TT99 seed JSON as embedded resource in `src/PhanMemKeToan.Infrastructure/Resources/coa_tt99.json` — Complexity: **L**
  - Format per data-model.md `Standard CoA JSON Format`
  - Minimum 330 accounts following Vietnamese TT99/2025 enterprise CoA
  - Root accounts: 1 (TÀI SẢN), 2 (NỢ PHẢI TRẢ), 3 (VỐN CHỦ SỞ HỮU), 4 (DOANH THU), 5 (CHI PHÍ), 6 (XÁC ĐỊNH KẾT QUẢ KINH DOANH), 7 (TÀI KHOẢN NGOÀI BẢNG CÂN ĐỐI KẾ TOÁN)
  - Mark file as `<EmbeddedResource>` in `PhanMemKeToan.Infrastructure.csproj`
  - No dependencies

- [X] TA09 Create TT133 seed JSON as embedded resource in `src/PhanMemKeToan.Infrastructure/Resources/coa_tt133.json` — Complexity: **L**
  - Format per data-model.md `Standard CoA JSON Format`
  - Minimum 200 accounts following Vietnamese TT133 SME/Micro CoA
  - Mark file as `<EmbeddedResource>` in `PhanMemKeToan.Infrastructure.csproj`
  - No dependencies

- [X] TA10 [P] Define `IAccountCacheService` interface in `src/PhanMemKeToan.Application/Common/Interfaces/IAccountCacheService.cs` — Complexity: **S**
  - Methods: `Task<List<AccountTreeNodeDto>?> GetTreeAsync(Guid tenantId, bool includeInactive)`, `Task SetTreeAsync(Guid tenantId, bool includeInactive, List<AccountTreeNodeDto> data, TimeSpan ttl)`, `Task InvalidateTreeAsync(Guid tenantId)`
  - Lives in Application layer (no Infrastructure dependency)
  - No dependencies

---

## Phase B: Application Layer (CQRS)

> Goal: DTOs, Query handlers, Command handlers, FluentValidation validators.  
> Complete Phase A before starting Phase B.

### B1 — DTOs

- [X] TB01 [P] Create `AccountTreeNodeDto` in `src/PhanMemKeToan.Application/Features/Accounts/DTOs/AccountTreeNodeDto.cs` — Complexity: **S**
  - Fields per data-model.md: `AccountId`, `AccountNumber`, `AccountName`, `AccountNameEnglish`, `Grade`, `IsParent`, `AccountCategoryKind`, `Inactive`, `IsPostableInForeignCurrency`, `HasTransactions`, `Children: List<AccountTreeNodeDto>`
  - Dependencies: TA01

- [X] TB02 [P] Create `AccountDetailDto` in `src/PhanMemKeToan.Application/Features/Accounts/DTOs/AccountDetailDto.cs` — Complexity: **S**
  - Extends `AccountTreeNodeDto` fields + `ParentId`, `ParentNumber`, `ParentName`, all `DetailBy*` flags, `AccountObjectType`, `RowVersion`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`
  - Dependencies: TB01

- [X] TB03 [P] Create `AccountListItemDto` in `src/PhanMemKeToan.Application/Features/Accounts/DTOs/AccountListItemDto.cs` — Complexity: **S**
  - Fields: `AccountId`, `AccountNumber`, `AccountName`, `AccountCategoryKind`, `Inactive`, `IsParent`
  - Dependencies: TA01

- [X] TB04 [P] Create `ImportCoaResultDto` in `src/PhanMemKeToan.Application/Features/Accounts/DTOs/ImportCoaResultDto.cs` — Complexity: **S**
  - Fields: `Imported: int`, `Skipped: int`, `Overwritten: int`, `Errors: List<string>`
  - No dependencies

### B2 — Queries

- [X] TB05 Create `GetAccountTreeQuery` + `GetAccountTreeQueryHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Queries/GetAccountTree/` — Complexity: **M**
  - Query params: `IncludeInactive: bool`, `Format: string` ("tree" | "flat")
  - Handler: Load all accounts for tenant (EF Core flat list); build in-memory tree via recursive parent-child grouping; `HasTransactions` = check GL table via raw query `SELECT 1 FROM "GeneralLedger" WHERE "AccountNumber" = @code LIMIT 1`
  - Cache response in Redis with key `accounts:tree:{tenantId}:{includeInactive}` TTL=5min
  - Return `List<AccountTreeNodeDto>` (nested) or `List<AccountListItemDto>` (flat)
  - Dependencies: TB01, TB03, TA05, TA10

- [X] TB06 Create `GetAccountByIdQuery` + `GetAccountByIdQueryHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Queries/GetAccountById/` — Complexity: **S**
  - Handler: Load account by `Id` + tenant filter; populate `ParentNumber`/`ParentName` from parent nav; check `HasTransactions` via GL query
  - Throw `NotFoundException` if not found
  - Return `AccountDetailDto`
  - Dependencies: TB02, TA05

- [X] TB07 Create `SearchAccountsQuery` + `SearchAccountsQueryHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Queries/SearchAccounts/` — Complexity: **M**
  - Query params: `Q: string`, `PostableOnly: bool = true`, `Limit: int = 20` (max 50)
  - Handler: PostgreSQL query using EF Core — AccountNumber starts with `q` (LIKE '{q}%') OR `to_tsvector('simple', AccountName) @@ plainto_tsquery('simple', q)` 
  - Throw `ValidationException` if `Q` is null/empty
  - Order: exact AccountNumber match first, then prefix, then name
  - Return `List<AccountListItemDto>`, only active accounts
  - Dependencies: TB03, TA05

### B3 — Commands

- [X] TB08 Create `CreateAccountCommand` + `CreateAccountCommandValidator` + `CreateAccountCommandHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Commands/CreateAccount/` — Complexity: **M**
  - Command fields: `AccountNumber`, `AccountName`, `AccountNameEnglish?`, `ParentId?`, `AccountCategoryKind`, `IsPostableInForeignCurrency`, all `DetailBy*` flags, `AccountObjectType`
  - Validator (FluentValidation): `AccountNumber` required, max 20 chars, alphanumeric; `AccountName` required, max 128; if `DetailByAccountObject=true` then `AccountObjectType != None` required
  - Handler: Check uniqueness; if `ParentId` provided → validate prefix rule (AccountNumber must start with parent.AccountNumber); auto-calculate `Grade` = parent.Grade + 1 (or 1 if root); save; auto-set parent.`IsParent = true` + save; invalidate Redis cache
  - Return created account `Id` + `RowVersion`
  - Dependencies: TB01, TA05, TA10

- [X] TB09 Create `UpdateAccountCommand` + `UpdateAccountCommandValidator` + `UpdateAccountCommandHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Commands/UpdateAccount/` — Complexity: **L**
  - Command fields: `Id`, `RowVersion`, same editable fields as CreateAccount + `Inactive`
  - Validator: same as Create + `RowVersion` required ≥ 0
  - Handler: Load account; check `RowVersion` match (throw `ConflictException` if mismatch); check GL entries exist → if yes, reject if `AccountNumber` or `ParentId` changed (throw `LockedException`); apply changes; increment `RowVersion`; save; invalidate Redis cache
  - Dependencies: TB08, TA05, TA10

- [X] TB10 Create `DeleteAccountCommand` + `DeleteAccountCommandHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Commands/DeleteAccount/` — Complexity: **M**
  - Command fields: `Id: Guid`, `RowVersion: int`
  - Handler: Load account; check RowVersion; check `Children.Any()` → throw `BusinessRuleException("has_children")`; check GL entries → throw `BusinessRuleException("has_transactions")`; soft-delete (`IsDeleted = true`, `Inactive = true`); if parent has no remaining children → set parent.`IsParent = false`; save; invalidate Redis cache
  - Dependencies: TB09, TA05, TA10

- [X] TB11 Create `ImportStandardCoaCommand` + `ImportStandardCoaCommandHandler` in `src/PhanMemKeToan.Application/Features/Accounts/Commands/ImportStandardCoa/` — Complexity: **L**
  - Command fields: `Standard: string` ("TT99" | "TT133"), `ConflictResolution: string` ("skip" | "overwrite")
  - Handler: Validate `Standard` and `ConflictResolution`; load embedded JSON from Assembly manifest resource (`PhanMemKeToan.Infrastructure.Resources.coa_tt99.json` or `coa_tt133.json`); deserialize to `CoaEntryDto[]`; within a single `IDbContextTransaction`: resolve `ParentID` GUIDs from `parentNumber` strings within batch; for each account → check existing by `AccountNumber`; if exists and `skip` → skip; if exists and `overwrite` → update; else insert; return `ImportCoaResultDto`; commit transaction; invalidate Redis cache
  - Dependencies: TA08, TA09, TB04, TA05, TA10

---

## Phase C: API Layer

> Goal: REST controller exposing all 7 endpoints per contracts/accounts-api.md.  
> Complete Phase B before starting Phase C.

- [X] TC01 Create `AccountsController` in `src/PhanMemKeToan.Api/Controllers/AccountsController.cs` — Complexity: **M**
  - `[Route("api/accounts")]`, `[Authorize]`
  - `GET /api/accounts` → send `GetAccountTreeQuery`; query params: `format`, `includeInactive`
  - `GET /api/accounts/search` → send `SearchAccountsQuery`; query params: `q`, `postableOnly`, `limit`
  - `GET /api/accounts/{id}` → send `GetAccountByIdQuery`
  - `POST /api/accounts` → send `CreateAccountCommand`; return 201
  - `PUT /api/accounts/{id}` → send `UpdateAccountCommand`; return 200
  - `DELETE /api/accounts/{id}` → send `DeleteAccountCommand`; query param: `rowVersion`; return 204
  - `POST /api/accounts/import` → send `ImportStandardCoaCommand`; return 200
  - Map exceptions to HTTP codes: `NotFoundException`→404, `ConflictException`→409, `LockedException`→422, `BusinessRuleException`→422, `ValidationException`→400
  - Response envelope: `{ data, errors }` per contracts/accounts-api.md
  - Dependencies: TB05, TB06, TB07, TB08, TB09, TB10, TB11

- [X] TC02 [P] Register `AccountCacheService` in `src/PhanMemKeToan.Infrastructure/DependencyInjection.cs` and create concrete implementation in `src/PhanMemKeToan.Infrastructure/Caching/AccountCacheService.cs` — Complexity: **S**
  - Implements `IAccountCacheService`; uses `IDistributedCache` (Redis)
  - Redis key pattern: `accounts:tree:{tenantId}:{includeInactive}`
  - Register: `services.AddScoped<IAccountCacheService, AccountCacheService>()`
  - Dependencies: TA10

- [X] TC03 Add `AccountManage` permission + policy — Complexity: **S**
  - `src/PhanMemKeToan.Infrastructure/Persistence/Seed/PermissionSeed.cs`: add `AccountManage` permission entry
  - `src/PhanMemKeToan.Api/Controllers/AccountsController.cs`: add `[Authorize(Policy="AccountManage")]` to POST, PUT, and DELETE endpoints
  - Register policy in `src/PhanMemKeToan.Api/Program.cs` or `src/PhanMemKeToan.Infrastructure/DependencyInjection.cs`
  - Dependencies: TC01

---

## Phase D: Frontend

> Goal: Angular 20 standalone components, NgRx Signals store, PrimeNG tree, i18n.  
> Complete Phase C before starting Phase D.

### D1 — Models & Services

- [X] TD01 [P] Create `account.models.ts` in `src/webapp/src/app/features/di/models/account.models.ts` — Complexity: **S**
  - TypeScript interfaces: `AccountTreeNodeDto`, `AccountDetailDto`, `AccountListItemDto`, `ImportCoaResultDto`
  - Enums: `AccountCategoryKind`, `AccountObjectType`
  - No dependencies

- [X] TD02 [P] Create `account-api.service.ts` in `src/webapp/src/app/features/di/account-tree/services/account-api.service.ts` — Complexity: **M**
  - Injectable service using Angular `HttpClient`
  - Methods: `getAccountTree(includeInactive?, format?)`, `getAccountById(id)`, `search(q, postableOnly?, limit?)`, `createAccount(dto)`, `updateAccount(id, dto)`, `deleteAccount(id, rowVersion)`, `importCoa(standard, conflictResolution)`
  - All methods return typed `Observable<ApiResponse<T>>`; error mapped via `catchError`
  - Dependencies: TD01

- [X] TD03 Create `account-tree.store.ts` (NgRx Signals) in `src/webapp/src/app/features/di/account-tree/store/account-tree.store.ts` — Complexity: **L**
  - State shape per plan.md: `accounts`, `selectedAccountId`, `formMode`, `loading`, `saving`, `searchQuery`, `statusFilter`, `expandedNodeIds`, `error`
  - Computed signals: `filteredAccounts` (apply search + status filter on flat list), `treeNodes` (calls AccountTreeBuilderService on filteredAccounts), `selectedAccount` (from accounts by selectedAccountId)
  - Methods: `loadTree()`, `selectAccount(id)`, `setFormMode(mode)`, `createAccount(cmd)`, `updateAccount(id, cmd)`, `deleteAccount(id, rowVersion)`, `importCoa(standard, conflictResolution)`, `setSearchQuery(q)`, `setStatusFilter(f)`, `toggleExpanded(id)`
  - Persist `expandedNodeIds` to localStorage key `account-tree-expanded-{tenantId}` on change
  - Dependencies: TD02

- [X] TD04 [P] Create `account-tree-builder.service.ts` in `src/webapp/src/app/features/di/account-tree/services/account-tree-builder.service.ts` — Complexity: **M**
  - `buildTree(accounts: AccountTreeNodeDto[]): TreeNode[]` — converts flat array to PrimeNG `TreeNode[]` hierarchy using parent–child ID mapping
  - `filterTree(accounts: AccountTreeNodeDto[], query: string): AccountTreeNodeDto[]` — returns matching accounts + all their ancestors
  - `highlightMatch(text: string, query: string): string` — wraps matched text in `<mark>` for display
  - No Angular HttpClient needed; pure computation
  - Dependencies: TD01

- [X] TD05 [P] Create `account-validation.service.ts` in `src/webapp/src/app/features/di/account-tree/services/account-validation.service.ts` — Complexity: **S**
  - `validatePrefix(accountNumber: string, parentNumber: string): boolean` — BR-DI01: accountNumber must start with parentNumber
  - `validateAccountNumber(code: string): boolean` — max 20, alphanumeric only
  - Pure functions, no dependencies

- [X] TD06 [P] Create `account-search.service.ts` in `src/webapp/src/app/features/di/account-tree/services/account-search.service.ts` — Complexity: **S**
  - Wraps `account-api.service.ts` search with RxJS `debounceTime(200)` + `distinctUntilChanged()` + `switchMap`
  - Returns `Observable<AccountListItemDto[]>`
  - Dependencies: TD02

### D2 — Components

- [X] TD07 Create `AccountTreeComponent` in `src/webapp/src/app/features/di/account-tree/components/account-tree/account-tree.component.ts` + `.html` + `.scss` — Complexity: **L**
  - Standalone component using PrimeNG `p-tree` with `virtualScrollItemSize=32`
  - Input signals: `treeNodes: TreeNode[]`, `loading: boolean`, `selectedAccountId: string | null`
  - Output events: `accountSelected`, `nodeExpanded`, `nodeCollapsed`
  - Custom node template: `AccountNumber` (monospace), `AccountName`, `AccountCategoryKind` badge (Debit=`--debit`, Credit=`--credit`), `Inactive` badge (gray outline), folder/document icon based on `IsParent`
  - ARIA: `role="tree"`, `role="treeitem"`, `aria-expanded`, `aria-level`, `aria-selected` per FR-007 + NFR-007
  - Summary parent nodes (`IsParent=true`) rendered non-selectable
  - Dependencies: TD01, TD04

- [X] TD08 Create `AccountTreeToolbarComponent` in `src/webapp/src/app/features/di/account-tree/components/account-tree-toolbar/account-tree-toolbar.component.ts` + `.html` — Complexity: **M**
  - Standalone component
  - Search bar: PrimeNG `p-inputtext` bound to store `searchQuery`; debounced via `TD06`
  - Status filter: PrimeNG `p-dropdown` — options: All/Active/Inactive; bound to store `statusFilter`; persists to localStorage
  - Action buttons: "Thêm mới" (opens create form), "Nhập danh mục" (opens ImportCoaDialog), "Xuất Excel" (US-DI-010 placeholder — disabled for now)
  - i18n: all labels via `translate` pipe with `account.*` keys
  - Dependencies: TD01, TD03, TD06

- [X] TD09 Create `AccountFormComponent` in `src/webapp/src/app/features/di/account-tree/components/account-form/account-form.component.ts` + `.html` — Complexity: **L**
  - Standalone component; Angular Reactive Forms
  - Fields: `accountNumber` (disabled if has transactions), `accountName`, `accountNameEnglish`, `parentId` (type-ahead using TD06), `accountCategoryKind` (dropdown), `isPostableInForeignCurrency` (checkbox), `inactive` (checkbox)
  - DetailBy* flags section: checkbox group with label "Hạch toán chi tiết theo"
  - `accountObjectType` dropdown appears only when `detailByAccountObject = true`
  - Keyboard shortcuts: `Ctrl+S` → save, `Escape` → cancel with dirty-form guard (PrimeNG `p-confirmDialog`)
  - Required fields: red asterisk + red border on error; show inline i18n error messages
  - `RowVersion` hidden field for optimistic concurrency
  - Locks `accountNumber` + `parentId` controls when `hasTransactions = true`
  - Dependencies: TD01, TD03, TD05

- [X] TD10 Create `AccountDetailPanelComponent` in `src/webapp/src/app/features/di/account-tree/components/account-detail-panel/account-detail-panel.component.ts` + `.html` — Complexity: **M**
  - Standalone component; right-side slide panel
  - Renders `AccountFormComponent` in view/edit/create mode based on store `formMode`
  - Header: account number + name; "Chỉnh sửa" / "Hủy" / "Lưu" buttons
  - "Xóa" / "Vô hiệu hóa" actions with confirmation dialogs
  - `HasTransactions` badge displayed if account has GL entries
  - Dependencies: TD03, TD09

- [X] TD11 Create `ImportCoaDialogComponent` in `src/webapp/src/app/features/di/account-tree/components/import-coa-dialog/import-coa-dialog.component.ts` + `.html` — Complexity: **L**
  - Standalone component using PrimeNG `p-dialog` + `p-steps` (4-step wizard)
  - Step 1: Select standard (TT99 / TT133) via radio buttons
  - Step 2: Select conflict resolution (Skip / Overwrite) with explanation text
  - Step 3: Preview — calls `POST /api/accounts/import` with `dryRun: true`; displays predicted counts (`imported`, `skipped`, `overwritten`) without committing DB changes
  - Step 4: Confirmation — calls store `importCoa()`; shows progress spinner; displays result (`imported`, `skipped`, `overwritten`)
  - On success: close dialog → store reloads tree
  - Dependencies: TD03, TD04

- [X] TD12 Create `AccountTreePageComponent` in `src/webapp/src/app/features/di/account-tree/account-tree-page.component.ts` + `.html` + `.scss` — Complexity: **M**
  - Standalone route component; master-detail split layout using PrimeNG `p-panel` (resizable)
  - Left panel: `AccountTreeToolbarComponent` + `AccountTreeComponent`
  - Right panel: `AccountDetailPanelComponent` (conditionally visible when `selectedAccountId != null` or `formMode === 'create'`)
  - Injects `AccountTreeStore`; calls `store.loadTree()` on init
  - `CanDeactivate` guard → dirty-form warning via `p-confirmDialog`
  - Dependencies: TD03, TD07, TD08, TD10

### D3 — Routing & i18n

- [X] TD13 Create `di.routes.ts` in `src/webapp/src/app/features/di/di.routes.ts` and add lazy route to `src/webapp/src/app/app.routes.ts` — Complexity: **S**
  - `di.routes.ts`: `{ path: 'accounts', component: AccountTreePageComponent }`
  - `app.routes.ts`: add `{ path: 'di', loadChildren: () => import('./features/di/di.routes') }`
  - Dependencies: TD12

- [X] TD14 [P] Add `account.*` i18n keys to `src/webapp/src/app/core/i18n/vi.json` — Complexity: **S**
  - Keys to add (minimum set):
    ```
    account.title, account.search.placeholder, account.filter.all, account.filter.active, account.filter.inactive,
    account.form.account_number, account.form.account_name, account.form.account_name_english,
    account.form.parent, account.form.category_kind, account.form.inactive, account.form.foreign_currency,
    account.form.detail_by.title, account.form.detail_by.account_object, account.form.detail_by.bank_account,
    account.form.detail_by.job, account.form.detail_by.project_work, account.form.detail_by.order,
    account.form.detail_by.contract, account.form.detail_by.expense_item, account.form.detail_by.department,
    account.form.detail_by.list_item, account.form.detail_by.pu_contract,
    account.category.debit, account.category.credit,
    account.object_type.none, account.object_type.supplier, account.object_type.customer, account.object_type.employee,
    account.action.add, account.action.edit, account.action.delete, account.action.deactivate, account.action.import,
    account.action.save, account.action.cancel, account.action.export,
    account.import.title, account.import.step_select_standard, account.import.step_conflict,
    account.import.step_preview, account.import.step_confirm,
    account.import.standard.tt99, account.import.standard.tt133,
    account.import.conflict.skip, account.import.conflict.overwrite,
    account.import.result.imported, account.import.result.skipped, account.import.result.overwritten,
    account.error.duplicate_code, account.error.code_must_start_with_parent,
    account.error.has_transactions, account.error.has_children, account.error.not_found,
    account.error.row_version_conflict, account.error.locked_has_transactions,
    account.badge.has_transactions, account.badge.inactive, account.badge.is_parent
    ```
  - No dependencies

---

## Phase E: Tests

> Goal: Unit tests for business logic + integration tests for CRUD with business rules.  
> Requires Phase A + Phase B complete.

- [ ] TE01 [P] Angular Jest unit tests for `AccountValidationService` prefix rule in `src/webapp/src/app/features/di/account-tree/services/account-validation.service.spec.ts` — Complexity: **M**
  - Test cases (min 12):
    - Valid: `"111"` + parent `"11"` → true
    - Valid: `"1111"` + parent `"111"` → true
    - Valid: `"11"` + parent `"1"` → true
    - Invalid: `"211"` + parent `"11"` → false
    - Invalid: `"11"` + parent `"111"` → false (shorter than parent)
    - Invalid: same code as parent → false
    - Root account (no parent) → always valid
    - Empty accountNumber → invalid
    - Alphanumeric check: `"A11!"` → invalid (special chars)
    - Max length: 21 chars → invalid
    - Case-insensitive: `"ABC"` + parent `"abc"` → true
    - Exact prefix match required: `"112"` + parent `"11"` → valid; `"121"` + parent `"11"` → invalid
  - Dependencies: TD05

- [ ] TE02 [P] Angular Jest unit tests for `AccountTreeBuilderService` in `src/webapp/src/app/features/di/account-tree/services/account-tree-builder.service.spec.ts` — Complexity: **M**
  - Test: `buildTree` with 5-node flat list → correct nested structure
  - Test: `buildTree` with root-only accounts → 1-level tree
  - Test: `filterTree` with query matching child → returns child + all ancestors
  - Test: `filterTree` with no match → empty
  - Test: `filterTree` case/accent-insensitive (e.g., `"tien"` matches `"Tiền"`)
  - Test: `buildTree` preserves `children` sort by `accountNumber` ASC
  - Dependencies: TD04

- [ ] TE03 Integration test: `CreateAccount` business rules in `tests/PhanMemKeToan.Application.Tests/Features/Accounts/CreateAccountCommandHandlerTests.cs` — Complexity: **M**
  - Setup: in-memory SQLite or test PostgreSQL via `WebApplicationFactory`
  - Test: happy path → account created, parent.IsParent set to true
  - Test: duplicate AccountNumber → `ValidationException` with code `duplicate_code`
  - Test: AccountNumber doesn't start with parent's → `ValidationException` with code `code_must_start_with_parent`
  - Test: root account (no parent) → Grade = 1
  - Test: child account → Grade = parent.Grade + 1
  - Dependencies: TB08, TA07

- [ ] TE04 Integration test: `UpdateAccount` GL-lock scenario in `tests/PhanMemKeToan.Application.Tests/Features/Accounts/UpdateAccountCommandHandlerTests.cs` — Complexity: **L**
  - Test: change AccountName → succeeds regardless of GL entries
  - Test: change AccountNumber when no GL entries → succeeds
  - Test: change AccountNumber when GL entries exist → throws `LockedException` (422)
  - Test: change ParentId when GL entries exist → throws `LockedException` (422)
  - Test: RowVersion mismatch → throws `ConflictException` (409)
  - Test: RowVersion matches → succeeds, RowVersion incremented
  - Dependencies: TB09, TA07

- [ ] TE05 Integration test: `DeleteAccount` guard checks in `tests/PhanMemKeToan.Application.Tests/Features/Accounts/DeleteAccountCommandHandlerTests.cs` — Complexity: **M**
  - Test: delete leaf account with no GL entries → succeeds (IsDeleted=true, Inactive=true)
  - Test: delete account with children → throws `BusinessRuleException("has_children")`
  - Test: delete account with GL entries → throws `BusinessRuleException("has_transactions")`
  - Test: delete last child → parent.IsParent set to false
  - Test: RowVersion mismatch → throws `ConflictException`
  - Dependencies: TB10, TA07

- [ ] TE06 Integration test: `ImportStandardCoa` atomicity in `tests/PhanMemKeToan.Application.Tests/Features/Accounts/ImportStandardCoaCommandHandlerTests.cs` — Complexity: **L**
  - Test: import TT99 (skip) → 330+ accounts created; no duplicates
  - Test: import TT133 (skip) → 200+ accounts created
  - Test: import with existing accounts + `skip` → existing not modified; skipped count > 0
  - Test: import with existing accounts + `overwrite` → existing updated; overwritten count > 0
  - Test: simulate error mid-import → transaction rolled back; 0 accounts inserted
  - Test: imported accounts have correct `Grade` and `IsParent` values
  - Dependencies: TB11, TA08, TA09, TA07

---

## Parallel Execution Opportunities

Tasks that can be done in parallel (same phase, different files):

**Phase A** (after TA01+TA02 done):
- TA08 and TA09 can be done in parallel (different JSON files)
- TA01 and TA02 can start simultaneously (no dependencies)

**Phase B** (after Phase A):
- TB01, TB02, TB03, TB04 can all be done in parallel
- TB05, TB06, TB07 can start in parallel after TB01–TB04
- TB08, TB09, TB10, TB11 should be sequential (TB10 depends on TB09, TB11 independent)

**Phase D** (after Phase C):
- TD01, TD02, TD04, TD05, TD06, TD14 can start in parallel
- TD07, TD08, TD09 can start in parallel after TD01–TD06
- TD10, TD11 after TD09
- TD12 after TD07, TD08, TD10

**Phase E**:
- TE01 and TE02 can start in parallel
- TE03, TE04, TE05, TE06 can start in parallel after Phase B + Phase A

---

## MVP Scope (Suggested)

Implement US-DI-001 (view tree) + US-DI-002 (create) + US-DI-005 (search) first:

1. TA01 → TA02 → TA03 → TA04 → TA05 → TA06 → TA07
2. TB01 → TB03 → TB05 → TB06 → TB07 → TB08
3. TC01 (partial: GET tree, GET by ID, POST, GET search)
4. TD01 → TD02 → TD03 → TD04 → TD05 → TD06 → TD07 → TD08 → TD09 → TD10 → TD12 → TD13 → TD14

This delivers a working read + create flow for demo/testing.  
Phase E tests and remaining stories (US-DI-003–US-DI-008) follow incrementally.
