# Tasks: Import COA Chuẩn

**Feature**: Import Danh mục Tài khoản (COA) Chuẩn  
**Branch**: `di-import-coa`  
**Spec**: `.specify/features/di-import-coa/spec.md`  
**Plan**: `.specify/features/di-import-coa/plan.md`  
**Generated**: 2026-04-18  

---

## Pre-conditions (verify before starting)

- `AccountApiService.importCoa()` — EXISTS in `src/webapp/src/app/features/di/account-tree/services/account-api.service.ts`
- `AccountTreeStore.importCoa()` — EXISTS in `src/webapp/src/app/features/di/account-tree/store/account-tree.store.ts` (return type needs fix)
- `ImportCoaResultDto` — EXISTS in `src/webapp/src/app/features/di/models/account.models.ts`
- `account-tree-page.component.ts` — already imports `ImportCoaDialogComponent` and calls `importDialog().open()` via `viewChild.required(ImportCoaDialogComponent)` → dialog component file is the missing piece
- **No backend changes needed**

---

## Phase 1: Setup

> Fix the one existing type mismatch before building the dialog.

- [X] T001 [S] Fix `importCoa` return type annotation in `src/webapp/src/app/features/di/account-tree/store/account-tree.store.ts`:
  - Change `Promise<{ imported: number; skipped: number; overwritten: number }>` → `Promise<ImportCoaResultDto>`
  - Add `ImportCoaResultDto` to the import from `../../models/account.models` if not already imported
  - **AC**: TypeScript resolves `res.errors` without type error; `get_errors` passes on this file

---

## Phase 2: Component — ImportCoaDialogComponent

> Core work. Creates the dialog component that the page already references.

### User Story 1 (P1): Onboarding — import full COA for new tenant
### User Story 2 (P2): Supplement — skip existing accounts
### User Story 3 (P3): Reset — overwrite existing accounts with standard

All three user stories are served by the single dialog component (US differentiated by step-2 selection).

---

- [X] T002 [M] [US1][US2][US3] Create component class `src/webapp/src/app/features/di/account-tree/components/import-coa-dialog/import-coa-dialog.component.ts`:

  **Internal signal state** (all signals, no BehaviorSubject):
  ```
  activeStep   = signal<number>(0)           // 0 = Step 1, 1 = Step 2
  selectedStd  = signal<'TT99'|'TT133'|null>(null)
  resolution   = signal<'skip'|'overwrite'>('skip')
  importing    = signal<boolean>(false)
  result       = signal<ImportCoaResultDto|null>(null)
  importError  = signal<string|null>(null)
  ```

  **Computed signals**:
  ```
  showOverwriteWarn = computed(() => resolution() === 'overwrite')
  canNext           = computed(() => selectedStd() !== null)
  canImport         = computed(() => !importing() && selectedStd() !== null)
  isClosable        = computed(() => !importing())
  showResultPanel   = computed(() => result() !== null || importError() !== null)
  selectedStandard  = computed(() => COA_STANDARDS.find(s => s.id === selectedStd()) ?? null)
  ```

  **Public method** `open(): void` — called by parent via `viewChild` (matches `account-tree-page.component.ts` usage pattern):
  ```
  open(): void {
    this.activeStep.set(0);
    this.selectedStd.set(null);
    this.resolution.set('skip');
    this.importing.set(false);
    this.result.set(null);
    this.importError.set(null);
    this.visible.set(true);
  }
  ```
  Note: dialog visibility is managed by an internal `visible = signal(false)` (NOT a parent input), since page calls `open()` imperatively. `onShow` / `visibleChange` should keep this signal in sync.

  **`executeImport()` method**: calls `this.store.importCoa(selectedStd()!, resolution())`, sets `importing=true` before, `importing=false` in finally, sets `result` or `importError`, handles thrown error with fallback message.

  **COA_STANDARDS const** (define at top of file, above class):
  ```ts
  interface CoaStandard { id: 'TT99'|'TT133'; name: string; legalReference: string; description: string; accountCount: number; }
  const COA_STANDARDS: CoaStandard[] = [
    { id: 'TT99',  name: 'TT99 — Doanh nghiệp', legalReference: 'Thông tư 99/2016/TT-BTC',  description: 'Dành cho doanh nghiệp vừa và lớn (~200 tài khoản)', accountCount: 200 },
    { id: 'TT133', name: 'TT133 — SME',          legalReference: 'Thông tư 133/2016/TT-BTC', description: 'Dành cho doanh nghiệp vừa và nhỏ (~60 tài khoản)',   accountCount: 60  },
  ];
  ```

  **Decorator requirements**:
  - `standalone: true`, `changeDetection: ChangeDetectionStrategy.OnPush`
  - `imports`: `[DialogModule, StepsModule, ButtonModule, MessageModule, RadioButtonModule, ProgressSpinnerModule, CommonModule]`
  - `inject(AccountTreeStore)` — do NOT call any API directly

  **AC**: Class compiles with no TypeScript errors; all signals and computed signals defined; `open()` and `executeImport()` methods present

---

- [X] T003 [M] [US1][US2][US3] Add inline template to `import-coa-dialog.component.ts`:

  **p-dialog binding**:
  ```html
  <p-dialog [visible]="visible()" (visibleChange)="visible.set($event)"
            [closable]="isClosable()" [modal]="true" [draggable]="false"
            header="Nhập danh mục tài khoản chuẩn" [style]="{width:'520px'}">
  ```

  **Stepper area** (`@if (!showResultPanel())`):
  - `<p-steps [model]="stepItems" [activeIndex]="activeStep()" [readonly]="true" />`
  - `stepItems` = `[{ label: 'Chọn chuẩn' }, { label: 'Tùy chọn nhập' }]` (property on class)

  **Step 1** (`@if (activeStep() === 0)`):
  - Two radio cards using `<p-radioButton>` inside clickable `<label>` divs
  - Each card shows: `name`, `legalReference`, `description`, `accountCount` (e.g., "~200 tài khoản")
  - `[(ngModel)]`-style or `[checked]` + `(onClick)` on radioButton to set `selectedStd()`
  - Cards visually distinguish selected state via CSS class binding

  **Step 2** (`@if (activeStep() === 1)`):
  - `<p-radioButton>` for `'skip'` ("Bỏ qua — giữ nguyên tài khoản hiện có")
  - `<p-radioButton>` for `'overwrite'` ("Ghi đè — thay thế tài khoản trùng số hiệu")
  - `@if (showOverwriteWarn())` → `<p-message severity="warn" text="Hành động này sẽ ghi đè các tài khoản hiện có có cùng số hiệu. Kiểm tra kỹ trước khi nhập." />`
  - Preview line: "Sẽ nhập ~{{ selectedStandard()?.accountCount }} tài khoản theo {{ selectedStandard()?.name }}"
  - `@if (importing())` → `<p-progressSpinner styleClass="w-8 h-8" />`

  **Result panel** (`@if (showResultPanel())`):
  - `@if (result(); as r)`:
    - "Đã nhập: **{{ r.imported }}**" — color: `var(--positive)`
    - "Bỏ qua: **{{ r.skipped }}**" — color: `var(--text-secondary)`
    - "Ghi đè: **{{ r.overwritten }}**" — color: `var(--warning)`
    - `@if (r.errors.length > 0)` → error list `<ul>` with each `<li>{{ err }}</li>`
  - `@if (importError(); as err)` → `<p-message severity="error" [text]="err" />`

  **Footer** (`ng-template pTemplate="footer"`):
  - `@if (!showResultPanel())`:
    - Step 0: "Hủy" (secondary, `[disabled]="importing()"`, `(onClick)="visible.set(false)"`) + "Tiếp theo" (icon right, `[disabled]="!canNext()"`, `(onClick)="activeStep.set(1)"`)
    - Step 1: "Quay lại" (secondary, icon left, `[disabled]="importing()"`, `(onClick)="activeStep.set(0)"`) + "Nhập" (`[loading]="importing()"`, `[disabled]="!canImport()"`, `(onClick)="executeImport()"`)
  - `@if (showResultPanel())`:
    - "Đóng" → `(onClick)="visible.set(false)"`

  **AC**: All 3 user stories navigable in browser; overwrite warning appears/disappears reactively; result panel replaces stepper after import; all FR-01 through FR-11 satisfied

---

## Phase 3: Integration

> Wire the new button in toolbar and update page binding. Both tasks are independent of each other after T002 compiles.

- [X] T004 [S] [P] [US1][US2][US3] Add "Nhập COA chuẩn" button + output to `src/webapp/src/app/features/di/account-tree/components/account-tree-toolbar/account-tree-toolbar.component.ts`:
  - Add `importCoaStandard = output<void>()` alongside existing `importCoa` output (line ~223)
  - In template `toolbar-right` section, add button **before** "Nhập từ Excel":
    ```html
    <p-button
      icon="pi pi-list"
      label="Nhập COA chuẩn"
      severity="secondary"
      size="small"
      (onClick)="importCoaStandard.emit()"
      aria-label="Nhập danh mục tài khoản chuẩn"
    />
    ```
  - Existing `importCoa` output and "Nhập từ Excel" button remain unchanged
  - **AC**: Two distinct buttons visible in toolbar; clicking "Nhập COA chuẩn" emits `importCoaStandard` (verify via browser DevTools or spec); clicking "Nhập từ Excel" still emits `importCoa`

---

- [X] T005 [S] [US1][US2][US3] Wire toolbar event to dialog in `src/webapp/src/app/features/di/account-tree/account-tree-page.component.ts`:
  - Change toolbar binding from `(importCoa)="onOpenImportDialog()"` to `(importCoaStandard)="onOpenImportDialog()"` in the template
  - `onOpenImportDialog()` already calls `this.importDialog().open()` — no change needed to the method body
  - Verify `viewChild.required(ImportCoaDialogComponent)` is already present (it is, line 208) — no change needed
  - **AC**: Clicking "Nhập COA chuẩn" in toolbar opens the dialog; clicking "Nhập từ Excel" does NOT open the dialog (correct separation of concerns)

  > **Dependency**: T004 must be completed first (toolbar needs `importCoaStandard` output before page can bind to it)

---

## Phase 4: Tests

> Unit tests for the new component. Integration with store is mocked.

- [X] T006 [M] [P] [US1][US2][US3] Create unit test file `src/webapp/src/app/features/di/account-tree/components/import-coa-dialog/import-coa-dialog.component.spec.ts`:

  **Mock store setup**:
  ```ts
  const mockStore = {
    importCoa: jasmine.createSpy('importCoa').and.returnValue(
      Promise.resolve({ imported: 5, skipped: 2, overwritten: 0, errors: [] })
    ),
  };
  TestBed.configureTestingModule({
    imports: [ImportCoaDialogComponent],
    providers: [{ provide: AccountTreeStore, useValue: mockStore }],
  });
  ```

  **Test cases** (must cover all 14 from plan phase 3):

  | ID | Test Name | Signal/Behavior Verified |
  |----|-----------|--------------------------|
  | T01 | `open() resets all state` | `activeStep=0`, `selectedStd=null`, `resolution='skip'`, `result=null`, `visible=true` |
  | T02 | `canNext is false when no standard selected` | `canNext()` false initially |
  | T03 | `canNext is true after selecting TT99; activeStep advances` | set `selectedStd('TT99')` → `canNext()` true; click "Tiếp theo" → `activeStep()=1` |
  | T04 | `showOverwriteWarn reactive to resolution` | `resolution('overwrite')` → `showOverwriteWarn()` true; `resolution('skip')` → false |
  | T05 | `Step 1 footer shows Hủy + Tiếp theo only` | `activeStep=0` → DOM has "Hủy", "Tiếp theo"; no "Nhập" button |
  | T06 | `Quay lại returns to step 1` | `activeStep=1` → click "Quay lại" → `activeStep()=0` |
  | T07 | `store.importCoa called with correct args` | Set `selectedStd('TT99')`, `resolution('skip')`, call `executeImport()` → `mockStore.importCoa` called with `('TT99', 'skip')` |
  | T08 | `loading state disables buttons and isClosable` | `importing=true` → `canImport()=false`, `isClosable()=false` |
  | T09 | `success result replaces stepper` | After `executeImport()` resolves → `showResultPanel()=true`, `result()` not null |
  | T10 | `result counts have correct CSS color tokens` | DOM elements for imported/skipped/overwritten have correct `--positive`/`--text-secondary`/`--warning` styles |
  | T11 | `error list rendered when result.errors.length > 0` | Mock returns `errors: ['err1']` → `<li>` element with "err1" in DOM |
  | T12 | `network error sets importError, not result` | `mockStore.importCoa` rejects → `importError()` set, `result()` null |
  | T13 | `Đóng button sets visible to false` | After result panel shown, click "Đóng" → `visible()=false` |
  | T14 | `isClosable false during import, true otherwise` | `importing=false` → `isClosable()=true`; `importing=true` → `isClosable()=false` |

  **AC**: All 14 tests pass with `ng test --include="**/import-coa-dialog.component.spec.ts" --watch=false`

---

## Dependency Order

```
T001  (store type fix — independent, do first)
  ↓
T002  (component class — depends on store fix for type safety)
  ↓
T003  (template — depends on T002 class)
  ↓
T004  (toolbar button — depends on T002 compiling cleanly)
T006  (tests — depends on T002 + T003)
  ↓
T005  (page wiring — depends on T004 having importCoaStandard output)
```

**Parallel opportunities**:
- T004 and T006 can be worked in parallel after T003 is done
- T001 is independent and can be done any time before T002

---

## Implementation Strategy (MVP-first)

**MVP = User Story 1 only** (new tenant onboarding with skip mode):
1. T001 → T002 → T003 (minimal: skip mode only, no overwrite warning) → T004 → T005
2. Smoke test: open dialog, select TT99, click Tiếp theo, click Nhập, verify result panel

**Full delivery** (all 3 user stories):
3. Add overwrite warning in T003 (FR-03 / US3)
4. T006 all 14 tests

---

## Complexity Legend

- **S** (Small): < 30 min, straightforward mechanical change
- **M** (Medium): 30–90 min, requires design decisions or significant new code
- **L** (Large): > 90 min (none in this feature — scope is well-bounded)

---

## Acceptance Checklist

- [ ] `ng build` produces zero TypeScript errors
- [ ] `get_errors` passes on all 5 changed/created files
- [ ] All 14 unit tests in `import-coa-dialog.component.spec.ts` pass
- [ ] Dialog opens from toolbar "Nhập COA chuẩn" button
- [ ] Step 1 → Step 2 navigation works; "Nhập" not visible on Step 1
- [ ] Overwrite warning appears/disappears reactively (FR-03)
- [ ] Result panel replaces stepper after successful import (FR-06)
- [ ] Account tree reloads while result panel is visible (FR-07)
- [ ] Dialog NOT closable during import (FR-09)
- [ ] "Nhập từ Excel" button still works (no regression)
