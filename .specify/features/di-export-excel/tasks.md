# Tasks: Xuất Excel — Danh mục tài khoản

**Feature ID**: `di-export-excel`  
**Spec**: `.specify/features/di-export-excel/spec.md`  
**Plan**: `.specify/features/di-export-excel/plan.md`  
**Branch**: `feature/di-import-coa` (current)  
**Date**: 2026-04-18

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Có thể chạy song song (file khác nhau, không phụ thuộc nhau)
- **[Story]**: User story tương ứng (US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: Cài đặt dependency Excel trước khi viết service

- [X] T001 Install `xlsx-js-style` package — run `npm install xlsx-js-style` in `src/webapp/`, update `package.json`

**Checkpoint**: `xlsx-js-style` available in `node_modules`; import `import * as XLSX from 'xlsx-js-style'` works

---

## Phase 2: Foundational — ExcelExportService

**Purpose**: Tạo service core trước khi wire vào component

**⚠️ CRITICAL**: T001 phải hoàn thành trước khi bắt đầu phase này

- [X] T002 Create `ExcelExportService` at `src/webapp/src/app/core/services/excel-export.service.ts`

  **Service signature**:
  ```typescript
  @Injectable({ providedIn: 'root' })
  export class ExcelExportService {
    exportAccountsToExcel(
      accounts: AccountTreeNodeDto[],
      allAccounts: AccountTreeNodeDto[]
    ): void
  }
  ```

  **Implementation requirements** (follow plan.md §1.2):
  - `accounts` = filtered list to export; `allAccounts` = full roster for parent lookup
  - Build `Map<accountId, accountNumber>` from `allAccounts` to resolve `parentId → accountNumber`
  - **Empty check**: If `accounts.length === 0`, do NOT create file — just return (caller shows toast)
  - Map each account to row: `[accountNumber, accountName, categoryLabel, grade, parentNumber|'', currencyLabel, statusLabel]`
  - `CATEGORY_KIND_LABELS: Record<number, string> = { 0: 'Dư Nợ', 1: 'Dư Có', 2: 'Lưỡng tính' }`
  - `isPostableInForeignCurrency=true` → `'Ngoại tệ'`; `false` → `''`
  - `inactive=false` → `'Đang dùng'`; `inactive=true` → `'Ngừng dùng'`
  - Header row: `bold: true`, `fill: { fgColor: { rgb: 'FF1B5E9E' } }`, `font: { color: { rgb: 'FFFFFFFF' }, bold: true }`
  - Column widths `wch`: `[15, 40, 12, 6, 15, 10, 12]`
  - Column D (Cấp, index 3): cell style `{ alignment: { horizontal: 'right' } }` for all data rows
  - Sheet name: `'Danh mục tài khoản'`; filename: `` `danh-muc-tai-khoan_${yyyymmdd}.xlsx` ``
  - All constants defined in service (`EXCEL_PRIMARY_COLOR`, `SHEET_NAME`, `FILE_PREFIX`, `COLUMN_HEADERS`) — NONE in component
  - Wrap SheetJS work in `setTimeout(resolve, 0)` pattern (see plan.md §1.2) so spinner renders before CPU block
  - **Import**: `import * as XLSX from 'xlsx-js-style'` (NOT `xlsx`)

  **Key field names** (from `AccountTreeNodeDto`):
  - `accountNumber` (NOT `accountCode`)
  - `accountName`
  - `accountCategoryKind` (NOT `accountType`)
  - `grade` (NOT `accountLevel`)
  - `parentId` (resolved via Map to `accountNumber`)
  - `isPostableInForeignCurrency` (NOT `currencyCode`)
  - `inactive` (boolean)

**Checkpoint**: `ExcelExportService` compiles, no TS errors; `get_errors` passes

---

## Phase 3: User Story 1 — Xuất toàn bộ danh mục tài khoản (Priority: P1) 🎯 MVP

**Goal**: Người dùng nhấn nút "Xuất Excel" → file `danh-muc-tai-khoan_YYYYMMDD.xlsx` tải về trong vòng 3 giây.

**Independent Test**: Mở `/di/accounts` không bộ lọc → nhấn "Xuất Excel" → file `.xlsx` tải về, có 1 sheet "Danh mục tài khoản", header in đậm nền xanh, đủ 7 cột đúng thứ tự.

### Tasks

- [X] T003 [US1] Modify toolbar component at `src/webapp/src/app/features/di/account-tree/components/account-tree-toolbar/account-tree-toolbar.component.ts`:

  **Changes**:
  1. Add import: `input` from `@angular/core`
  2. Add class field: `isExporting = input<boolean>(false)`
  3. Add class field: `exportExcel = output<void>()`
  4. Replace the `<p-button>` "Xuất Excel" block:
     ```html
     <!-- REMOVE: [disabled]="true" and old aria-label -->
     <!-- ADD: -->
     <p-button
       icon="pi pi-download"
       label="Xuất Excel"
       severity="secondary"
       size="small"
       [disabled]="isExporting()"
       [loading]="isExporting()"
       (onClick)="exportExcel.emit()"
       aria-label="Xuất danh sách tài khoản ra Excel"
     />
     ```

  **Do NOT** modify any other toolbar logic (search, filter, expand/collapse, TT toggle).

- [X] T004 [US1] Modify page component at `src/webapp/src/app/features/di/account-tree/account-tree-page.component.ts`:

  **Changes**:
  1. Import `ExcelExportService` from `../../../core/services/excel-export.service`
  2. Import `signal` from `@angular/core` (if not already imported)
  3. Add injection: `private readonly excelExportService = inject(ExcelExportService)`
  4. Add field: `readonly isExporting = signal(false)`
  5. Add handler method:
     ```typescript
     async onExportExcel(): Promise<void> {
       const accounts = this.store.filteredAccounts();
       if (accounts.length === 0) {
         this.messageService.add({
           severity: 'warn',
           summary: 'Không có dữ liệu',
           detail: 'Không có tài khoản nào để xuất. Vui lòng kiểm tra lại bộ lọc.'
         });
         return;
       }
       this.isExporting.set(true);
       await new Promise<void>(resolve => setTimeout(resolve, 0));
       try {
         this.excelExportService.exportAccountsToExcel(
           accounts,
           this.store.accounts()
         );
       } catch {
         this.messageService.add({
           severity: 'error',
           summary: 'Lỗi',
           detail: 'Không thể xuất file Excel. Vui lòng thử lại.'
         });
       } finally {
         this.isExporting.set(false);
       }
     }
     ```
  6. Add bindings to `<app-account-tree-toolbar>` in template:
     ```html
     (exportExcel)="onExportExcel()"
     [isExporting]="isExporting()"
     ```

  **Note**: `messageService` and `store` are already injected in this component — do NOT add duplicate injections.

**Checkpoint (US1)**: `get_errors` on T003 + T004 files passes; toolbar button no longer disabled; page handles `(exportExcel)` event.

---

## Phase 4: User Story 2 — Xuất khi có bộ lọc (Priority: P2)

**Goal**: Export tôn trọng bộ lọc hiện tại (search + status filter).

**Independent Test**: Áp bộ lọc "Ngừng dùng" → nhấn "Xuất Excel" → file chỉ chứa tài khoản trạng thái "Ngừng dùng".

> **Note**: US2 được đảm bảo hoàn toàn bởi T004 — `this.store.filteredAccounts()` trả về đúng danh sách sau bộ lọc. **Không cần task bổ sung.**

**Checkpoint (US2)**: Covered by T004. Verify bằng manual test.

---

## Phase 5: User Story 3 — Loading state & Error feedback (Priority: P3)

**Goal**: Nút disabled + spinner trong khi export; toast lỗi tiếng Việt nếu thất bại.

**Independent Test**: Click "Xuất Excel", quan sát spinner trên nút trong lúc xử lý; sau đó nút trở về bình thường.

> **Note**: US3 được đảm bảo hoàn toàn bởi T003 (`[loading]`, `[disabled]`) + T004 (`isExporting` signal, try/catch/finally, messageService). **Không cần task bổ sung.**

**Checkpoint (US3)**: Covered by T003 + T004. Verify bằng manual test.

---

## Phase 6: Unit Tests

**Purpose**: Kiểm tra ExcelExportService và toolbar mới

- [X] T005 [P] [US1] Create unit tests at `src/webapp/src/app/core/services/excel-export.service.spec.ts`

  **Test cases** (follow plan.md §1.5):
  | Test | Verification |
  |------|-------------|
  | `exportAccountsToExcel()` calls `XLSX.writeFile` | Service triggers download |
  | Header row has 7 correct column labels | FR-002: đúng thứ tự + đúng tên |
  | `inactive=false` → `'Đang dùng'` | FR-009 status label |
  | `inactive=true` → `'Ngừng dùng'` | FR-009 status label |
  | `accountCategoryKind=0` → `'Dư Nợ'`, `1` → `'Dư Có'`, `2` → `'Lưỡng tính'` | Plan decision: CATEGORY_KIND_LABELS |
  | `isPostableInForeignCurrency=true` → `'Ngoại tệ'`; `false` → `''` | Plan decision: currencyCode gap |
  | `parentId=null` → empty string in parent column | FR-011 root account |
  | `parentId=<guid>` → resolved `accountNumber` from allAccounts | Parent lookup Map |
  | Empty `accounts=[]` → `XLSX.writeFile` NOT called | Plan: empty check, do not generate file |
  | Filename contains today's date `YYYYMMDD` | FR-004 filename format |
  | Vietnamese characters in accountName preserved (e.g., `'Tiền mặt'`) | Encoding correctness |

  **Mock strategy**: Spy on `XLSX.writeFile` using `spyOn(XLSX, 'writeFile').and.callFake(() => {})`

- [X] T006 [P] [US1] Create/update unit tests for toolbar at `src/webapp/src/app/features/di/account-tree/components/account-tree-toolbar/account-tree-toolbar.component.spec.ts`

  **Test cases**:
  | Test | Verification |
  |------|-------------|
  | `exportExcel` output emits when button clicked | Output wiring |
  | Button has `[loading]="true"` when `isExporting=true` | Loading state input |
  | Button is disabled when `isExporting=true` | Disabled state input |
  | Button is enabled when `isExporting=false` (default) | Default state |

**Checkpoint (Tests)**: All tests in T005 + T006 pass with 0 failures

---

## Phase 7: Polish

- [X] T007 [P] Run `get_errors` on all 4 changed/created files and fix any TypeScript errors:
  - `src/webapp/src/app/core/services/excel-export.service.ts`
  - `src/webapp/src/app/core/services/excel-export.service.spec.ts`
  - `src/webapp/src/app/features/di/account-tree/components/account-tree-toolbar/account-tree-toolbar.component.ts`
  - `src/webapp/src/app/features/di/account-tree/account-tree-page.component.ts`

---

## Dependency Graph

```
T001 (install xlsx-js-style)
  └── T002 (ExcelExportService)
        ├── T003 (toolbar: add output/input) ──┐
        ├── T005 (service unit tests)           ├── T007 (get_errors verify)
        └── T004 (page: wire export)  ──────────┤
              └── T006 (toolbar unit tests) ────┘
```

**T003 and T005 are parallel** (different files, both depend on T002).  
**T006 depends on T003** (needs new input/output to test).  
**T007 runs last** after all files are written.

---

## Parallel Execution Examples

**After T002 completes**, these can run in parallel:
- `T003` — toolbar modification
- `T005` — service unit tests

**After T003 completes**:
- `T004` — page component wiring
- `T006` — toolbar unit tests

---

## Implementation Strategy

**MVP Scope**: T001 → T002 → T003 → T004 (US1 fully working, no tests)

**Full delivery**: T001 → T002 → T003+T005 (parallel) → T004+T006 (parallel) → T007

---

## Summary

| Phase | Tasks | Files |
|-------|-------|-------|
| Setup | T001 | `package.json` |
| Foundational | T002 | `excel-export.service.ts` (new) |
| US1 | T003, T004 | `account-tree-toolbar.component.ts`, `account-tree-page.component.ts` |
| US2 | — | Covered by T004 |
| US3 | — | Covered by T003 + T004 |
| Tests | T005, T006 | `excel-export.service.spec.ts` (new), `account-tree-toolbar.component.spec.ts` |
| Polish | T007 | — |

**Total**: 7 tasks | 4 files changed/created (2 new, 2 modified) | 2 parallel opportunities  
**MVP scope**: T001–T004 (4 tasks) — US1 fully functional, export works end-to-end
