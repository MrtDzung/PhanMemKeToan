# DI Module — Account Tree (Hệ thống Tài khoản Kế toán)
## Feature Specification

| Field | Value |
|---|---|
| **Status** | Draft |
| **Date** | 2026-04-17 |
| **Feature Branch** | `feature/di-account-tree` |
| **Module** | DI (Dictionary / Master Data) |

---

## 1. Overview

The **Account Tree** feature manages the Chart of Accounts (CoA) for Vietnamese enterprise accounting. It provides:

- A hierarchical tree structure following Vietnamese accounting standards **TT99/2025** (Enterprise) and **TT133** (SME/Micro)
- Full CRUD lifecycle for accounting accounts
- Sub-ledger tracking flags (`DetailBy*`) controlling what dimensions each account must reconcile against
- Import of standard CoA templates (seed data) from TT99 or TT133
- Fast typeahead lookup used across all voucher forms

This is a **foundational master-data feature** that must be implemented before any journal-entry or reporting feature can function. All voucher modules reference `Account.AccountNumber` via FK.

### Scope
- In scope: Chart of Accounts CRUD, tree navigation, import standard CoA, search/typeahead
- Out of scope: Period-end closing transfers (`AccountTransfer`), default account mappings (`AccountDefault`), GL posting engine

---

## 2. User Stories

### P1 — Must Have

| ID | Story | Acceptance Criteria |
|---|---|---|
| US-DI-001 | As an accountant, I want to view the full chart of accounts as an expandable/collapsible tree so I can navigate the account hierarchy quickly. | Tree renders all accounts in hierarchy; expand/collapse nodes; root nodes visible by default |
| US-DI-002 | As an accountant, I want to add a new account under an existing parent so I can extend the CoA. | Code must start with parent's code; Grade auto-calculated; IsParent auto-set on parent; duplicate code blocked |
| US-DI-003 | As an accountant, I want to edit an existing account's name, flags, and settings so I can correct or update details. | Code and ParentID locked if account has journal entries; all other fields editable; dirty-form guard on navigation |
| US-DI-004 | As an accountant, I want to deactivate (soft-delete) an account so it no longer appears in account lookups without losing historical data. | Sets `Inactive = true`; hidden in lookups; cannot deactivate if account has unposted vouchers referencing it; reversible |
| US-DI-005 | As an accountant, I want to search accounts by code or name so I can quickly find the account I need. | Filters tree in real-time; matching nodes highlighted; ancestors auto-expanded; response <200ms |

### P2 — Should Have

| ID | Story | Acceptance Criteria |
|---|---|---|
| US-DI-006 | As a system administrator, I want to import the standard TT99 or TT133 chart of accounts so I don't have to enter 300+ accounts manually when setting up a new company. | Select standard; preview; confirm; duplicate detection (skip or overwrite); full transaction rollback on error |
| US-DI-007 | As an accountant, I want to filter the account list by status (active/inactive) so I can review disabled accounts separately. | Status filter: All / Active / Inactive; filter persists in localStorage |
| US-DI-008 | As an accountant, I want to see account usage indicators so I know which accounts are actively used. | "Has transactions" badge for accounts in GeneralLedger (read-only, computed) |

### P3 — Nice to Have

| ID | Story | Acceptance Criteria |
|---|---|---|
| US-DI-009 | As an administrator, I want to reorder sibling accounts by sequence number so the printed CoA follows Vietnamese standard ordering. | Drag-and-drop reorder within same parent; default order by AccountNumber ASC |
| US-DI-010 | As an accountant, I want to export the chart of accounts to Excel for auditors. | Export flat list or tree; columns: AccountNumber, AccountName, AccountNameEnglish, Grade, IsParent, AccountCategoryKind, Inactive |

---

## 3. Functional Requirements

### 3.1 Tree Display

| ID | Requirement |
|---|---|
| FR-001 | The system SHALL display all accounts as a hierarchical tree determined by `ParentID`. |
| FR-002 | Root accounts (ParentID IS NULL) SHALL be displayed as top-level nodes. |
| FR-003 | The tree SHALL support expand/collapse per node; state persisted in localStorage per user. |
| FR-004 | Each tree node SHALL display: `AccountNumber`, `AccountName`, `AccountCategoryKind` icon (Dr/Cr), and `Inactive` badge. |
| FR-005 | Accounts with `IsParent = 1` SHALL render as non-selectable summary nodes with a folder icon. |
| FR-006 | Accounts with `IsParent = 0` (leaf/postable) SHALL render with a document icon and be selectable. |
| FR-007 | The tree SHALL support virtual scrolling when total account count exceeds 200 nodes. |

### 3.2 Account CRUD

| ID | Requirement |
|---|---|
| FR-010 | The system SHALL allow creating a new account via a side panel form. |
| FR-011 | On create, the system SHALL validate that `AccountNumber` is unique within the tenant. |
| FR-012 | On create, if a parent is selected, the system SHALL validate that `AccountNumber` starts with the parent's `AccountNumber` (BR-DI01). |
| FR-013 | `Grade` SHALL be auto-calculated: root accounts get Grade=1; child gets `parent.Grade + 1`. |
| FR-014 | When a new child account is added, the system SHALL auto-set `IsParent = true` on the parent. |
| FR-015 | The system SHALL allow editing all fields EXCEPT `AccountNumber` and `ParentID` if the account has associated `GeneralLedger` entries. |
| FR-016 | If no GL entries exist, `AccountNumber` MAY be edited; the API SHALL reject if existing children would violate the prefix rule after the change. |
| FR-017 | The system SHALL allow soft-deleting (deactivating) an account by setting `Inactive = true`. |
| FR-018 | The system SHALL PREVENT deactivation if the account has unposted/open vouchers referencing it. |
| FR-019 | The system SHALL PREVENT hard-deletion of any account that has appeared in `GeneralLedger`. |
| FR-020 | The system SHALL support re-activating an inactive account. |
| FR-021 | `RowVersion` (optimistic concurrency) SHALL be required in all update/delete requests. |

### 3.3 Account Code Validation (BR-DI01)

| ID | Requirement |
|---|---|
| FR-030 | `AccountNumber` is limited to 20 characters, alphanumeric only. |
| FR-031 | Child `AccountNumber` MUST start with the exact parent's `AccountNumber`. Example: parent `111` → valid: `1111`, `1112`; invalid: `211`, `11`. |
| FR-032 | `AccountNumber` MUST be unique across all tenant accounts (case-insensitive). |
| FR-033 | On validation failure, display inline error via i18n key `account.error.code_must_start_with_parent`. |

### 3.4 Sub-Ledger Tracking Flags (DetailBy*)

| ID | Requirement |
|---|---|
| FR-040 | Each account exposes: `DetailByAccountObject`, `DetailByBankAccount`, `DetailByJob`, `DetailByProjectWork`, `DetailByOrder`, `DetailByContract`, `DetailByExpenseItem`, `DetailByDepartment`, `DetailByListItem`, `DetailByPUContract`. |
| FR-041 | Flags are displayed as a checklist group under "Hạch toán chi tiết theo". |
| FR-042 | When `DetailByAccountObject = true`, `AccountObjectType` becomes required (0=None, 1=Supplier, 2=Customer, 3=Employee). |
| FR-043 | These flags drive sub-ledger dimension requirements in downstream voucher modules. |

### 3.5 Account Category Kind

| ID | Requirement |
|---|---|
| FR-050 | `AccountCategoryKind` SHALL be required: `0 = Debit nature`, `1 = Credit nature`. |
| FR-051 | Debit-nature: use color token `--debit`. Credit-nature: use color token `--credit`. NEVER hardcode hex. |
| FR-052 | `AccountCategoryKind` SHALL default to the parent's value when creating a child account. |

### 3.6 Multi-Currency Support

| ID | Requirement |
|---|---|
| FR-060 | `IsPostableInForeignCurrency` flag controls whether the account tracks foreign currency amounts. |
| FR-061 | When enabled, voucher lines for this account may carry `ForeignAmount` and `CurrencyCode` (enforced downstream). |

### 3.7 Search and Filter

| ID | Requirement |
|---|---|
| FR-070 | Search bar filters tree by `AccountNumber` (prefix match) or `AccountName` (contains, case/accent-insensitive). |
| FR-071 | Search results highlight matched text and auto-expand ancestor nodes of matches. |
| FR-072 | Status filter (All / Active / Inactive) available above the tree. |
| FR-073 | `GET /api/accounts/search` SHALL return only active, postable (`IsParent=0`) accounts in <200ms. |
| FR-074 | Typeahead results display `AccountNumber` + `AccountName`; support keyboard navigation (arrow keys, Enter). |

### 3.8 Import Standard Chart of Accounts

| ID | Requirement |
|---|---|
| FR-080 | "Import standard CoA" action seeds the tenant account table from a built-in template. |
| FR-081 | Two templates: **TT99/2025** (330+ accounts, enterprise) and **TT133** (200+ accounts, SME). |
| FR-082 | Preview dialog shows: accounts to add, skip, overwrite before confirming. |
| FR-083 | Import is atomic: all-or-nothing within a single database transaction. |
| FR-084 | On duplicate `AccountNumber`: user chooses **Skip** (keep existing) or **Overwrite** (update from template). |
| FR-085 | After successful import, tree reloads and displays newly imported accounts. |

---

## 4. Non-Functional Requirements

| ID | Category | Requirement |
|---|---|---|
| NFR-001 | Performance | Tree with 500 accounts MUST render in <500ms after data load. |
| NFR-002 | Performance | Account typeahead MUST return results in <200ms (indexed query). |
| NFR-003 | Performance | Virtual scrolling MUST support 1,000+ accounts at 60fps. |
| NFR-004 | Scalability | Import of 335 accounts (TT99) MUST complete within 5 seconds. |
| NFR-005 | Security | All endpoints require valid JWT (`[Authorize]`); tenant isolation via `ApplicationDbContext`. |
| NFR-006 | Concurrency | Optimistic concurrency via `RowVersion` prevents lost-update conflicts. |
| NFR-007 | Accessibility | Tree MUST implement `role="tree"`, `role="treeitem"`, `aria-expanded`, `aria-level`, `aria-selected`. |
| NFR-008 | Accessibility | All controls keyboard-navigable; focus indicators meet 3:1 contrast ratio. |
| NFR-009 | Bundle Size | DI module lazy chunk MUST stay under 150KB gzipped. |
| NFR-010 | UX | Form MUST support Ctrl+S (Save) and Escape (Cancel with dirty-form guard). |
| NFR-011 | Internationalization | All UI labels via `ngx-translate`; i18n keys prefixed with `account.`. |
| NFR-012 | Data Integrity | `AccountNumber` uniqueness enforced at DB level (unique index) and application level. |

---

## 5. Key Entities / Data Model

### 5.1 Account Entity

```csharp
public class Account
{
    public Guid Id { get; set; }                      // PK (inherited from AuditableEntity; exposed as `accountId` in JSON responses via EF Core naming convention)
    public string AccountNumber { get; set; }        // e.g. "111", "1111"
    public string AccountName { get; set; }          // Vietnamese name
    public string? AccountNameEnglish { get; set; }  // English name
    public Guid? ParentID { get; set; }              // FK -> Account.AccountID (null = root)
    public int Grade { get; set; }                   // 1=root, 2=child, 3=grandchild...
    public bool IsParent { get; set; }               // true = summary/non-postable
    public AccountCategoryKind AccountCategoryKind { get; set; }
    public bool Inactive { get; set; }
    public bool IsPostableInForeignCurrency { get; set; }

    // Sub-ledger tracking flags
    public bool DetailByAccountObject { get; set; }
    public AccountObjectType AccountObjectType { get; set; }
    public bool DetailByBankAccount { get; set; }
    public bool DetailByJob { get; set; }
    public bool DetailByProjectWork { get; set; }
    public bool DetailByOrder { get; set; }
    public bool DetailByContract { get; set; }
    public bool DetailByExpenseItem { get; set; }
    public bool DetailByDepartment { get; set; }
    public bool DetailByListItem { get; set; }
    public bool DetailByPUContract { get; set; }

    public bool IsDeleted { get; set; }              // BR-GL02 soft delete
    public int RowVersion { get; set; }              // BR-GL04 optimistic concurrency
    public string? MISACodeID { get; set; }

    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = [];
}
```

### 5.2 Enumerations

```csharp
public enum AccountCategoryKind
{
    Debit = 0,   // Tài khoản có tính chất Nợ (Assets, Expenses)
    Credit = 1   // Tài khoản có tính chất Có (Liabilities, Equity, Revenue)
}

public enum AccountObjectType
{
    None = 0,
    Supplier = 1,
    Customer = 2,
    Employee = 3
}
```

### 5.3 Database Indexes (PostgreSQL)

```sql
-- Unique AccountNumber per tenant (scoped to tenant, excludes soft-deleted rows)
CREATE UNIQUE INDEX uix_account_number ON "Accounts" ("TenantId", "AccountNumber") WHERE "IsDeleted" = false
    WHERE "IsDeleted" = false;

-- Fast prefix search for typeahead
CREATE INDEX ix_account_number_text ON "Accounts" ("AccountNumber" text_pattern_ops);
CREATE INDEX ix_account_name_gin ON "Accounts" USING gin(to_tsvector('simple', "AccountName"));

-- Tree traversal
CREATE INDEX ix_account_parent ON "Accounts" ("ParentID");
```

### 5.4 Related Tables (Read-Only from this feature)

| Table | Relationship | Used For |
|---|---|---|
| `GeneralLedger` | References `Account.AccountNumber` | Determine if account has transactions (blocks delete/code-change) |
| `AccountDefault` | References `Account.AccountNumber` | Default account mappings (managed by SYS module) |
| `AccountTransfer` | References `Account.AccountNumber` | Period-end transfer rules (out of scope) |

---

## 6. API Endpoints

### Base URL: `/api/accounts`

All endpoints require `Authorization: Bearer {jwt}`. Response envelope: `{ "data": ..., "errors": [...] }`.

#### `GET /api/accounts`

| Param | Type | Default | Description |
|---|---|---|---|
| `format` | `tree`/`flat` | `tree` | Nested children array or flat ordered list |
| `includeInactive` | bool | `false` | Include inactive accounts |
| `q` | string | — | Filter by code/name (flat format only) |

**Response 200 (tree):**
```json
{
  "data": [
    {
      "accountId": "guid",
      "accountNumber": "1",
      "accountName": "TÀI SẢN",
      "grade": 1,
      "isParent": true,
      "accountCategoryKind": 0,
      "inactive": false,
      "children": [...]
    }
  ]
}
```

#### `GET /api/accounts/{id}`
Full `AccountDetailDto` with all `DetailBy*` flags. **404** if not found.

#### `POST /api/accounts`

```json
{
  "accountNumber": "1111",
  "accountName": "Tiền mặt VND",
  "accountNameEnglish": "Cash in VND",
  "parentId": "guid-of-111",
  "accountCategoryKind": 0,
  "isPostableInForeignCurrency": false,
  "detailByAccountObject": false,
  "accountObjectType": 0,
  "detailByBankAccount": false,
  "detailByJob": false,
  "detailByProjectWork": false,
  "detailByOrder": false,
  "detailByContract": false,
  "detailByExpenseItem": false,
  "detailByDepartment": false,
  "detailByListItem": false,
  "detailByPUContract": false
}
```

Errors: **400** duplicate_code | code_must_start_with_parent; **404** parent_not_found.

#### `PUT /api/accounts/{id}`
Same as POST body + `"rowVersion": int`.
Errors: **409** row_version_conflict; **422** locked_has_transactions.

#### `DELETE /api/accounts/{id}`
Soft-delete. Query param: `?rowVersion={int}`.
Errors: **409** row_version_conflict; **422** has_transactions | has_children.

#### `POST /api/accounts/import`

```json
{ "standard": "TT99", "conflictResolution": "skip" }
```
`standard`: `"TT99"` or `"TT133"`. `conflictResolution`: `"skip"` or `"overwrite"`.

**Response 200:** `{ "imported": 310, "skipped": 25, "overwritten": 0, "errors": [] }`
Entire import is atomic (rolled back on any error).

#### `GET /api/accounts/search`

| Param | Type | Default | Description |
|---|---|---|---|
| `q` | string | required | AccountNumber prefix OR AccountName contains |
| `postableOnly` | bool | `true` | IsParent=false accounts only |
| `limit` | int | `20` | Max 50 |

SLA: <200ms via `text_pattern_ops` + GIN indexes.

---

## 7. UI Screens

### 7.1 Account List Screen — Master-Detail Layout

**Route:** `/di/accounts`
**Layout:** Resizable two-panel split (default 60/40)

**Left panel toolbar:**
```
[ Search by code or name... ]  [ Status: All ▾ ]  [ Import ▾ ]  [ + Thêm tài khoản ]
```

**Tree visual conventions:**
- `IsParent=true`: bold, folder icon; not selectable for posting
- `IsParent=false`: regular weight, document icon; selectable
- Debit: AccountNumber badge color `var(--debit)`; Credit: `var(--credit)`
- Inactive: gray strikethrough text + "(Ngừng sử dụng)" chip
- Has GL entries: small indicator dot
- Row hover: `[Sửa]` `[Ngừng sử dụng]` action buttons

**Right panel states:**
1. Empty: "Chọn tài khoản để xem chi tiết" placeholder
2. View/Edit: full account form
3. New: empty form with parent pre-filled (if adding child)

### 7.2 Account Detail / Edit Form

**Section 1: Thông tin cơ bản**

| Field | Control | Notes |
|---|---|---|
| Số tài khoản * | Text input | Lock icon if has GL entries |
| Tên tài khoản * | Text input | Always editable |
| Tên tiếng Anh | Text input | Optional |
| Tài khoản cha | Read-only text | Locked if has GL entries |
| Cấp tài khoản | Badge (auto) | Not user-editable |
| Tính chất * | Radio: Nợ / Có | Defaults to parent value on create |
| Loại đối tượng | Select | Required when DetailByAccountObject=true |

**Section 2: Tùy chọn**
- Toggle: Hạch toán theo ngoại tệ
- Toggle: Đang sử dụng / Ngừng sử dụng

**Section 3: Hạch toán chi tiết theo** — two-column checkbox grid:
```
[x] Đối tượng công nợ    [ ] Ngân hàng
[ ] Công việc            [ ] Dự án / Công trình
[ ] Đơn đặt hàng         [ ] Hợp đồng bán
[ ] Khoản mục chi phí    [ ] Bộ phận
[ ] Danh sách tùy chỉnh  [ ] Hợp đồng mua
```

**Action bar:** `[ Lưu Ctrl+S ]  [ Hủy Esc ]  [ Ngừng sử dụng ]  [ Xóa ]`

### 7.3 Add New Account

- Triggered from toolbar or right-click context menu
- On child add: parent pre-filled + locked; AccountCategoryKind defaults to parent value

### 7.4 Import Standard CoA Dialog (4-step wizard)

**Step 1:** Select standard (TT99/TT133) + conflict resolution (skip/overwrite) → Next
**Step 2:** Preview — show add/skip/overwrite counts → Confirm Import
**Step 3:** Progress bar "Đang nhập... 150/330 tài khoản"
**Step 4:** Success summary → Close (triggers tree reload)

### 7.5 Search Behavior

- Debounce: 300ms
- Match: AccountNumber prefix-first then substring; AccountName contains (accent-insensitive)
- Highlight matched text: `background: var(--primary-light)`
- Auto-expand ancestors of matching nodes
- Empty state: "Không tìm thấy tài khoản nào" + Clear search button

---

## 8. Component Architecture (Angular)

### Module Route

```typescript
// di.routes.ts
{
  path: 'accounts',
  loadComponent: () =>
    import('./account-tree/account-tree-page.component')
      .then(m => m.AccountTreePageComponent),
  title: 'Hệ thống tài khoản'
}
```

### Component Tree

```
AccountTreePageComponent            <- Route component (standalone)
  AccountTreeToolbarComponent       <- Search, filter, Import, Add
  AccountTreeComponent              <- p-tree with virtual scroll
    AccountTreeNodeComponent        <- Custom node (badges, hover actions)
  AccountDetailPanelComponent       <- Right panel
    AccountFormComponent            <- Reactive form (all fields)
    DetailByFlagsComponent          <- DetailBy* checkbox grid
```

### Signal Store

```typescript
interface AccountTreeState {
  accounts: Account[];               // flat list; tree built client-side
  selectedAccountId: string | null;       // `selectedAccount` is a computed signal derived from `selectedAccountId`
  loading: boolean;
  saving: boolean;
  searchQuery: string;
  statusFilter: 'all' | 'active' | 'inactive';
  expandedNodeIds: Set<string>;      // persisted to localStorage
  error: string | null;
}
```

### Key Services

| Service | Responsibility |
|---|---|
| `AccountApiService` | HTTP calls to all `/api/accounts` endpoints |
| `AccountTreeBuilderService` | Flat `Account[]` → nested tree nodes (memoized) |
| `AccountValidationService` | Client-side validation: prefix rule, duplicate check |
| `AccountSearchService` | Debounced search, highlight, ancestor-expand logic |

---

## 9. Constraints & Validation Summary

| Rule ID | Constraint | Enforcement |
|---|---|---|
| BR-DI01 | Child code MUST start with parent code | API 400 + client reactive form validator |
| BR-DI02 | Only leaf accounts (IsParent=false) postable in journal entries | `/search` returns postable-only by default |
| BR-DI03 | DetailBy* flags drive sub-ledger requirements | Stored on account; enforced by downstream modules |
| BR-GL02 | Soft delete via IsDeleted; no physical deletion | DELETE endpoint sets IsDeleted=true |
| BR-GL04 | Optimistic concurrency via RowVersion | All PUT/DELETE require rowVersion; 409 on mismatch |
| VAL-001 | AccountNumber unique per tenant | DB unique partial index + API 400 |
| VAL-002 | AccountNumber max 20 chars, alphanumeric | Client + API validation |
| VAL-003 | AccountName required, max 128 chars | Client + API validation |
| VAL-004 | Cannot change AccountNumber/ParentID if GL entries exist | API 422: locked_has_transactions |
| VAL-005 | Cannot delete account with children | API 422: has_children |
| VAL-006 | Cannot delete account in GeneralLedger | API 422: has_transactions |
| VAL-007 | Grade auto-set from parent.Grade + 1 | Server-side computed on create |
| VAL-008 | IsParent auto-set to true on first child creation | Server-side triggered on child create |

---

## 10. i18n Keys (ngx-translate)

```json
{
  "account": {
    "title": "Hệ thống tài khoản kế toán",
    "add": "Thêm tài khoản",
    "edit": "Sửa tài khoản",
    "delete": "Xóa tài khoản",
    "deactivate": "Ngừng sử dụng",
    "activate": "Kích hoạt lại",
    "import_standard": "Nhập tài khoản chuẩn",
    "search_placeholder": "Tìm kiếm theo mã hoặc tên...",
    "no_results": "Không tìm thấy tài khoản nào",
    "select_to_view": "Chọn tài khoản để xem chi tiết",
    "error": {
      "duplicate_code": "Mã tài khoản '{{code}}' đã tồn tại",
      "code_must_start_with_parent": "Mã tài khoản phải bắt đầu bằng '{{parentCode}}'",
      "parent_not_found": "Tài khoản cha không tồn tại",
      "locked_has_transactions": "Không thể thay đổi mã hoặc tài khoản cha khi đã có phát sinh giao dịch",
      "has_transactions": "Không thể xóa tài khoản đã có phát sinh giao dịch",
      "has_children": "Không thể xóa tài khoản có tài khoản con",
      "row_version_conflict": "Dữ liệu đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang."
    }
  }
}
```

---

## 11. Open Questions

| # | Question | Resolution |
|---|---|---|
| OQ-001 | When AccountNumber is changed (no GL entries), cascade-update children or reject? | Recommend: reject and list affected children |
| OQ-002 | Is dual-book support (Financial CoA vs Management CoA) required in v1? | TBD |
| OQ-003 | Are TT99/TT133 seed data files embedded in backend binary or fetched from remote? | Recommend: embedded JSON |
| OQ-004 | Is MISACodeID visible in UI? | Recommend: hidden in v1 |

---

## 12. Acceptance Criteria (Definition of Done)

- [ ] All P1 user stories implemented and verified by manual testing
- [ ] All API endpoints return correct HTTP status codes per spec
- [ ] Account code prefix validation (BR-DI01) enforced both client-side and server-side
- [ ] Optimistic concurrency tested: concurrent edit returns 409 and prompts reload
- [ ] Import of TT99 template seeds 330+ accounts within 5 seconds
- [ ] Tree with 500+ accounts renders in <500ms
- [ ] Typeahead `GET /api/accounts/search` responds in <200ms
- [ ] Inactive accounts hidden in typeahead by default
- [ ] WCAG 2.1 AA: tree keyboard navigation works (Tab, Arrow, Enter, Space, Escape)
- [ ] All i18n keys defined in `vi.json`; no hardcoded Vietnamese strings in TypeScript/templates
- [ ] No hardcoded hex colors in Angular components — CSS custom properties only
- [ ] Unit tests: `AccountValidationService` (prefix rule), `AccountTreeBuilderService` (flat-to-tree)
- [ ] Integration tests: POST/PUT/DELETE GL-entry lock scenarios; import atomic rollback

---

*End of Specification — DI Module: Account Tree — v1.0 Draft (2026-04-17)*
