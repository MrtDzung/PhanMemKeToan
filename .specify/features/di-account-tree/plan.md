# Implementation Plan: DI Module — Account Tree

**Branch**: `feature/di-account-tree` | **Date**: 2026-04-17 | **Spec**: `spec.md`

## Summary

Implement the Chart of Accounts (CoA) master data feature for the DI module. The feature provides a hierarchical tree view of accounting accounts following Vietnamese standards (TT99/2025, TT133), with full CRUD, search/typeahead, and bulk import capabilities. This is a **foundational feature** — all voucher modules depend on it.

Technical approach: Clean Architecture (Domain → Application → Infrastructure → API → Frontend) following the existing `sys-auth-tenant` pattern. Backend uses CQRS/MediatR with EF Core + Dapper. Frontend uses Angular 20 standalone components + NgRx Signals + PrimeNG p-tree with virtual scrolling.

## Technical Context

| Item | Detail |
|------|--------|
| Language | C# 12 (.NET 10) + TypeScript 5.x (Angular 20) |
| Backend | ASP.NET Core 10, Clean Architecture + CQRS, MediatR 14, EF Core 10, FluentValidation 12 |
| Frontend | Angular 20 + PrimeNG 20 LTS + NgRx Signals + TailwindCSS v4 |
| Database | PostgreSQL 16 (Docker port 5433) via ApplicationDbContext (per-tenant) |
| Cache | Redis 7 (typeahead cache, tree cache) |
| Testing | xUnit (backend), Jasmine/Karma (frontend) |
| Performance | Tree 500 accounts <500ms; typeahead <200ms; import 335 accounts <5s |

## Constitution Check

- ✅ Design-before-code: spec.md completed before plan
- ✅ Domain-driven: Account entity in Domain layer
- ✅ Metadata-driven: AccountCategoryKind and DetailBy* flags stored as data
- ✅ Incremental: DI is part of MVP scope
- ✅ No hardcoded colors (CSS custom properties)
- ✅ Tenant isolation via ApplicationDbContext (TenantId global filter)
- ✅ Soft delete (IsDeleted on AuditableEntity base class)

## Project Structure

### Documentation (this feature)
```
.specify/features/di-account-tree/
├── spec.md              ✅ Done
├── plan.md              ✅ This file
├── data-model.md        ✅ Done
├── contracts/
│   └── accounts-api.md  (generated with plan)
└── tasks.md             (Phase 2 — speckit.tasks)
```

### Backend Source Files
```
src/PhanMemKeToan.Domain/
├── Entities/
│   └── Account.cs                                   NEW
├── Enums/
│   ├── AccountCategoryKind.cs                        NEW
│   └── AccountObjectType.cs                          NEW (if not exists)

src/PhanMemKeToan.Application/
├── Common/Interfaces/
│   └── IApplicationDbContext.cs                      MODIFY (+DbSet<Account>)
├── Features/Accounts/
│   ├── Commands/
│   │   ├── CreateAccount/
│   │   │   ├── CreateAccountCommand.cs               NEW
│   │   │   └── CreateAccountCommandHandler.cs        NEW
│   │   ├── UpdateAccount/
│   │   │   ├── UpdateAccountCommand.cs               NEW
│   │   │   └── UpdateAccountCommandHandler.cs        NEW
│   │   ├── DeleteAccount/
│   │   │   ├── DeleteAccountCommand.cs               NEW
│   │   │   └── DeleteAccountCommandHandler.cs        NEW
│   │   └── ImportStandardCoa/
│   │       ├── ImportStandardCoaCommand.cs           NEW
│   │       └── ImportStandardCoaCommandHandler.cs    NEW
│   ├── Queries/
│   │   ├── GetAccountTree/
│   │   │   ├── GetAccountTreeQuery.cs                NEW
│   │   │   └── GetAccountTreeQueryHandler.cs         NEW
│   │   ├── GetAccountById/
│   │   │   ├── GetAccountByIdQuery.cs                NEW
│   │   │   └── GetAccountByIdQueryHandler.cs         NEW
│   │   └── SearchAccounts/
│   │       ├── SearchAccountsQuery.cs                NEW
│   │       └── SearchAccountsQueryHandler.cs         NEW
│   └── DTOs/
│       ├── AccountTreeNodeDto.cs                     NEW
│       ├── AccountDetailDto.cs                       NEW
│       ├── AccountListItemDto.cs                     NEW
│       └── ImportCoaResultDto.cs                     NEW

src/PhanMemKeToan.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs                       MODIFY (+DbSet<Account>)
│   ├── Configurations/
│   │   └── AccountConfiguration.cs                  NEW
│   ├── Migrations/                                    NEW migration
│   └── SeedData/
│       ├── TT99AccountSeedData.cs                    NEW (embedded JSON loader)
│       └── TT133AccountSeedData.cs                   NEW
├── Resources/
│   ├── coa_tt99.json                                 NEW (330+ accounts)
│   └── coa_tt133.json                               NEW (210+ accounts)

src/PhanMemKeToan.Api/
├── Controllers/
│   └── AccountsController.cs                         NEW
```

### Frontend Source Files
```
src/webapp/src/app/
├── app.routes.ts                                     MODIFY (+di module route)
├── features/di/
│   ├── di.routes.ts                                  NEW
│   ├── account-tree/
│   │   ├── account-tree-page.component.ts            NEW (route component)
│   │   ├── account-tree-page.component.html          NEW
│   │   ├── account-tree-page.component.scss          NEW
│   │   ├── components/
│   │   │   ├── account-tree/
│   │   │   │   ├── account-tree.component.ts         NEW (p-tree wrapper)
│   │   │   │   ├── account-tree.component.html       NEW
│   │   │   │   └── account-tree.component.scss       NEW
│   │   │   ├── account-tree-toolbar/
│   │   │   │   ├── account-tree-toolbar.component.ts NEW
│   │   │   │   └── account-tree-toolbar.component.html NEW
│   │   │   ├── account-detail-panel/
│   │   │   │   ├── account-detail-panel.component.ts NEW
│   │   │   │   └── account-detail-panel.component.html NEW
│   │   │   ├── account-form/
│   │   │   │   ├── account-form.component.ts         NEW
│   │   │   │   └── account-form.component.html       NEW
│   │   │   └── import-coa-dialog/
│   │   │       ├── import-coa-dialog.component.ts    NEW
│   │   │       └── import-coa-dialog.component.html  NEW
│   │   ├── store/
│   │   │   └── account-tree.store.ts                 NEW (NgRx Signals)
│   │   └── services/
│   │       ├── account-api.service.ts                NEW
│   │       ├── account-tree-builder.service.ts       NEW
│   │       ├── account-validation.service.ts         NEW
│   │       └── account-search.service.ts             NEW
│   └── models/
│       └── account.models.ts                         NEW
├── core/i18n/
│   └── vi.json                                       MODIFY (+account.* keys)
```

## Technical Design

### Backend Architecture

#### Domain Layer
`Account` extends `AuditableEntity` (has TenantId, IsDeleted, CreatedAt/By, ModifiedAt/By).  
Self-referencing tree via `ParentID → AccountID`.  
Business rules enforced in command handlers + FluentValidation.

#### CQRS Handlers

| Handler | Key Logic |
|---------|-----------|
| `CreateAccountCommandHandler` | Validate prefix rule, auto-calculate Grade, auto-set parent.IsParent=true |
| `UpdateAccountCommandHandler` | Lock AccountNumber/ParentID if GL entries exist; optimistic concurrency |
| `DeleteAccountCommandHandler` | Block if children exist; block if GL entries exist; soft-delete |
| `GetAccountTreeQueryHandler` | Load all tenant accounts, build tree in-memory (Dapper flat query → tree builder) |
| `SearchAccountsQueryHandler` | PostgreSQL text_pattern_ops + GIN index; return only postable accounts by default |
| `ImportStandardCoaCommandHandler` | Load embedded JSON, resolve conflicts, bulk-insert in one transaction |

#### Optimistic Concurrency
`Account.RowVersion` is an `int` incremented on each update.  
EF Core: NOT using native xmin (PostgreSQL) — use application-managed RowVersion for cross-DB compatibility.

#### Import Data Strategy
TT99 and TT133 JSON files embedded as `EmbeddedResource` in Infrastructure project.  
Loaded via `Assembly.GetManifestResourceStream()`. No external network calls.

### Frontend Architecture

#### Signal Store Shape
```typescript
interface AccountTreeState {
  accounts: Account[];           // flat — tree built client-side
  selectedAccountId: string | null;
  formMode: 'view' | 'edit' | 'create' | null;
  loading: boolean;
  saving: boolean;
  searchQuery: string;
  statusFilter: 'all' | 'active' | 'inactive';
  expandedNodeIds: Set<string>;  // localStorage key: 'account-tree-expanded-{tenantId}'
  error: string | null;
}
```

#### Tree Building Strategy
Flat `Account[]` from API → `AccountTreeBuilderService.buildTree()` → `TreeNode[]` for PrimeNG `p-tree`.  
Memoized via Angular `computed()` signal to avoid re-build on every keystroke.  
Search: filter flat list → keep matching nodes + all ancestors → rebuild partial tree.

#### PrimeNG Components Used
- `p-tree` with `virtualScrollItemSize=32` for virtual scrolling
- `p-panel` for resizable split layout
- `p-dialog` for import wizard (multi-step via `p-steps`)
- `p-inputtext` for search
- `p-dropdown` for status filter + AccountObjectType
- `p-togglebutton` / `p-checkbox` for DetailBy* flags
- `p-confirmDialog` for delete/deactivate

### API Design

#### Performance Strategy
1. **Tree endpoint** (`GET /api/accounts?format=tree`): Single query loading all accounts flat, tree built in application layer. Cached in Redis (TTL=5min, invalidated on any write).
2. **Search endpoint** (`GET /api/accounts/search`): Direct DB query with `text_pattern_ops` prefix index + GIN full-text index. NOT cached (must be real-time). Target <200ms.
3. **Import**: Bulk insert via `BulkInsertOrUpdate` or batched EF Core SaveChanges in single transaction.

#### Error Codes

| HTTP | Code | Description |
|------|------|-------------|
| 400 | `duplicate_code` | AccountNumber already exists for tenant |
| 400 | `code_must_start_with_parent` | AccountNumber doesn't start with parent's code |
| 404 | `account_not_found` | Account ID not found |
| 404 | `parent_not_found` | ParentID not found |
| 409 | `row_version_conflict` | RowVersion mismatch (concurrent edit) |
| 422 | `locked_has_transactions` | AccountNumber/ParentID locked (has GL entries) |
| 422 | `has_transactions` | Cannot delete — has GL entries |
| 422 | `has_children` | Cannot delete — has children |

### Database Migration Strategy

Single new migration: `AddAccountTree`
- Create `Accounts` table with all columns
- Add unique partial index on `AccountNumber` WHERE `IsDeleted=false`
- Add `text_pattern_ops` index for typeahead
- Add GIN index on `AccountName` for full-text search
- Add `ix_account_parent` on `ParentID`

## Implementation Phases

### Phase A: Domain + Infrastructure (Backend Core)
1. Create `AccountCategoryKind` enum
2. Create `Account` entity (extends AuditableEntity)
3. Create `AccountConfiguration.cs` (EF Core mapping)
4. Add `DbSet<Account>` to `ApplicationDbContext` + `IApplicationDbContext`
5. Generate EF Core migration
6. Create TT99 + TT133 JSON seed files

### Phase B: Application Layer (CQRS)
1. DTOs: `AccountTreeNodeDto`, `AccountDetailDto`, `AccountListItemDto`, `ImportCoaResultDto`
2. `GetAccountTreeQueryHandler` — flat load + tree builder
3. `GetAccountByIdQueryHandler`
4. `SearchAccountsQueryHandler` — typeahead
5. `CreateAccountCommandHandler` — prefix validation, auto-grade, auto-isParent
6. `UpdateAccountCommandHandler` — GL-lock check, optimistic concurrency
7. `DeleteAccountCommandHandler` — children check, GL check, soft delete
8. `ImportStandardCoaCommandHandler` — embedded JSON, atomic transaction

### Phase C: API Layer
1. `AccountsController.cs` — 6 endpoints (GET tree, GET by ID, POST, PUT, DELETE, POST import, GET search)
2. FluentValidation validators for CreateAccountCommand + UpdateAccountCommand

### Phase D: Frontend
1. `account.models.ts` — TypeScript interfaces
2. `account-api.service.ts` — HTTP client
3. `account-tree.store.ts` — NgRx Signals store
4. `account-tree-builder.service.ts` + `account-validation.service.ts` + `account-search.service.ts`
5. `AccountFormComponent` — reactive form with all fields + DetailBy* flags
6. `AccountTreeComponent` — p-tree with virtual scroll + custom node template
7. `AccountTreeToolbarComponent` — search + filter + import + add
8. `AccountDetailPanelComponent` — right panel with form
9. `ImportCoaDialogComponent` — 4-step wizard
10. `AccountTreePageComponent` — route component (master-detail layout)
11. Wire up routes in `di.routes.ts` + `app.routes.ts`
12. Add i18n keys to `vi.json`

### Phase E: Tests
1. Unit: `AccountValidationService` (prefix rule — 10+ test cases)
2. Unit: `AccountTreeBuilderService` (flat-to-tree, search filter)
3. Integration: POST/PUT/DELETE with GL-entry lock scenarios
4. Integration: Import atomic rollback on error

## Risks & Mitigations

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| TT99/TT133 JSON data accuracy | Medium | Derive from MISA INHONGHA DB data (335 accounts confirmed) |
| Tree performance at 500+ nodes | Low | Virtual scroll + Redis cache on tree endpoint |
| Account code change cascade | Low | Reject if children exist; document in spec OQ-001 |
| Dual-book CoA (OQ-002) | Low | Defer to v2; spec is v1 scope only |
