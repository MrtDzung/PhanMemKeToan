# Feature Specification: Import Danh mục Tài khoản (COA) Chuẩn

**Feature Branch**: `di-import-coa`  
**Created**: 2026-04-18  
**Status**: Draft  
**Module**: DI (Danh mục — Master Data), sub-module: account-tree  

---

## User Scenarios & Testing *(mandatory)*

### Overview

Accountants and system administrators need a way to quickly populate the Chart of Accounts (COA) when setting up a new tenant or when their existing account list is incomplete. Rather than manually entering hundreds of accounts one by one, users can import a Vietnamese standard COA (TT99 for enterprises, TT133 for small businesses) in a single guided operation.

This feature covers **standard import only** — importing the Vietnamese Ministry of Finance official account templates directly from the system. File-based Excel import is out of scope for this iteration.

---

### User Story 1 - Onboarding: Thiết lập COA cho tenant mới (Priority: P1)

**As a** kế toán trưởng setting up a new company account,  
**I want to** import a complete standard COA (TT99 or TT133) in one click,  
**So that** I don't have to manually create 200+ accounts before starting data entry.

**Why this priority**: New tenant onboarding is the primary use case. Without accounts, no vouchers can be recorded. This is a day-0 blocker.

**Acceptance Criteria**:

- Given a new tenant with zero accounts
- When the user opens the Import COA dialog, selects TT99 or TT133, and confirms import
- Then all standard accounts for the chosen template are created in the system
- And the account tree view reloads automatically showing all imported accounts
- And a result summary is displayed: "Đã nhập X tài khoản, bỏ qua 0, ghi đè 0"

**Test Scenario**:

1. Log in as admin of a new tenant (no accounts)
2. Navigate to DI → Danh mục Tài khoản
3. Click "Nhập COA chuẩn" in toolbar
4. Select standard TT99, verify description shows "Thông tư 99/2016 — Dành cho doanh nghiệp (~200 tài khoản)"
5. Select conflict resolution "Bỏ qua (Skip)" — default
6. Preview shows account count for TT99
7. Click "Nhập"
8. Loading indicator is visible while import runs
9. Success summary shows: imported > 0, skipped = 0, overwritten = 0, no errors
10. Account tree reloads with all standard accounts visible

---

### User Story 2 - Bổ sung tài khoản còn thiếu (Priority: P2)

**As a** kế toán đang sử dụng hệ thống với COA tùy chỉnh một phần,  
**I want to** import missing standard accounts while keeping my existing customized accounts intact,  
**So that** I can extend my COA without losing data I've already entered.

**Why this priority**: Many tenants migrate from another system with a partial COA. They need to fill gaps without disrupting existing entries.

**Acceptance Criteria**:

- Given a tenant with some existing accounts
- When the user imports with conflict resolution "Bỏ qua (Skip)"
- Then only accounts that do NOT already exist in the system are created
- And all pre-existing accounts remain unchanged
- And the result summary clearly shows how many were skipped

**Test Scenario**:

1. Tenant already has 50 accounts (some overlap with TT99)
2. Open Import COA dialog, select TT99, select "Bỏ qua"
3. After import: imported count + skipped count = total TT99 accounts
4. Existing accounts with matching account numbers are unmodified
5. No data loss or side-effects on existing vouchers

---

### User Story 3 - Đặt lại COA về chuẩn (Overwrite) (Priority: P3)

**As a** quản trị viên hệ thống muốn reset lại danh mục tài khoản,  
**I want to** overwrite my current accounts with the standard template,  
**So that** I can start fresh with the official COA after incorrect manual entries.

**Why this priority**: Less frequent than P1/P2, but needed for cases where initial setup was wrong. Must include a safety warning since it is a destructive action.

**Acceptance Criteria**:

- Given a tenant with existing accounts
- When the user selects conflict resolution "Ghi đè (Overwrite)" on Step 2
- Then an inline `p-message` warning (orange/warn severity) appears immediately inside the form
- And the import begins directly when user clicks "Nhập" (no separate confirmation dialog)
- And after import, existing accounts with matching numbers are overwritten with template data
- And the result summary panel (replacing the stepper) shows how many accounts were overwritten in orange

**Test Scenario**:

1. Tenant has 100 existing accounts
2. Open Import COA dialog, advance to Step 2, select "Ghi đè"
3. An inline `p-message` (orange/warn) appears: "Hành động này sẽ ghi đè các tài khoản hiện có có cùng số hiệu. Kiểm tra kỹ trước khi nhập."
4. Switch back to "Bỏ qua" — inline warning disappears
5. Switch again to "Ghi đè" — inline warning reappears
6. Click "Nhập" — import starts immediately (no separate confirmation dialog)
7. Import runs → result summary panel replaces stepper, shows overwritten count > 0 in orange
8. Tree reloads in background; result summary remains visible
9. User clicks "Đóng" to dismiss dialog

---

### Edge Cases

| Scenario | Expected Behavior |
|----------|------------------|
| All accounts already exist (skip mode) | Result: imported = 0, skipped = N; success message shown; no error |
| All accounts already exist (overwrite mode) | Result: overwritten = N, imported = 0; tree reloads |
| Backend returns partial errors | Success summary shown with error list below; tree reloads for successfully imported accounts |
| User closes dialog mid-wizard | No import is triggered; tree remains unchanged |
| User switches overwrite warning away | Inline `p-message` disappears when user selects "Bỏ qua"; reappears on selecting "Ghi đè" |
| Network error during import | Error toast/message shown; dialog remains open for retry |
| Tenant has no permission to import | "Nhập COA chuẩn" button is hidden or disabled; unauthorized message if forced |

---

## Requirements *(mandatory)*

### Functional Requirements

**FR-01**: The account-tree toolbar must include a button labeled "Nhập COA chuẩn" that opens the Import COA dialog. The button must be visible only to users with COA management permission.

**FR-02**: The Import COA dialog must present a two-step guided flow implemented as a PrimeNG `p-steps` stepper inside a `p-dialog`:
- **Step 1 — Chọn chuẩn**: User selects between TT99 and TT133. Each option shows its name, legal reference (e.g., "Thông tư 99/2016/TT-BTC"), target business type, and static account count. Navigation button: "Tiếp theo" (Next) to advance to Step 2.
- **Step 2 — Tùy chọn nhập**: User selects conflict resolution strategy ("Bỏ qua" or "Ghi đè"). A preview summary (static) of expected account count is shown. Navigation buttons: "Quay lại" (Back) to return to Step 1, and "Nhập" (Import) to execute the import.
- The **"Nhập" button must appear only on Step 2**. It must not be visible or accessible on Step 1.
- Step progress indicators are rendered by the `p-steps` component above the step content.

**FR-03**: When conflict resolution "Ghi đè" is selected on Step 2, an inline `p-message` alert (severity="warn", orange) must appear immediately inside the form, stating: "Hành động này sẽ ghi đè các tài khoản hiện có có cùng số hiệu. Kiểm tra kỹ trước khi nhập." The alert must disappear reactively when the user switches back to "Bỏ qua".

**FR-04**: No separate confirmation dialog is required for "Ghi đè". The inline `p-message` warning (FR-03) is the sole safety mechanism. Clicking "Nhập" starts the import directly without any additional acknowledgment step.

**FR-05**: During the import operation, the dialog must display a loading indicator. All interactive controls (Back, Nhập, Close) must be disabled while import is in progress.

**FR-06**: On successful import, the `p-steps` stepper view must be replaced entirely with a result summary panel (within the same dialog) showing:
- Number of accounts imported (Đã nhập) — displayed in green (`--positive` / `--success` token)
- Number of accounts skipped (Bỏ qua) — displayed in gray (`--text-secondary` token)
- Number of accounts overwritten (Ghi đè) — displayed in orange (`--warning` token)
- List of error messages, if any (displayed individually, collapsible if > 5 items)
The dialog footer must show only a single "Đóng" button. The Back and Nhập buttons must be hidden once the result panel is displayed.

**FR-07**: After a successful import (regardless of skip/overwrite counts), the account tree must automatically reload in the background while the result summary panel is displayed. The reload must complete without requiring the user to close the dialog first.

**FR-08**: On failure (network error or all-error backend response), the dialog must display an error message without reloading the tree.

**FR-09**: The dialog must be dismissible at any time before import starts (via Cancel button or pressing Escape). During import (loading state), the dialog must not be dismissible.

**FR-10**: The feature must use the existing `AccountTreeStore.importCoa(standard, conflictResolution)` method exclusively. The dialog component must not call any API endpoint directly.

**FR-11**: File-based Excel upload is explicitly out of scope for this iteration. The dialog must not include any file upload UI.

---

### Key Entities *(include if feature involves data)*

**COA Standard** (frontend display model):

| Field | Type | Value |
|-------|------|-------|
| id | string | `'TT99'` or `'TT133'` |
| name | string | Display name (e.g., "TT99 — Doanh nghiệp") |
| legalReference | string | e.g., "Thông tư 99/2016/TT-BTC" |
| description | string | Short description of intended business type |
| accountCount | number | Static count: TT99 ≈ 200, TT133 ≈ 60 |

**ConflictResolution** (string union):

| Value | Vietnamese Label | Behavior |
|-------|-----------------|----------|
| `'skip'` | Bỏ qua | Leave existing accounts unchanged |
| `'overwrite'` | Ghi đè | Replace existing accounts with template data |

**ImportCoaResult** (backend response DTO — already defined):

| Field | Type | Description |
|-------|------|-------------|
| imported | number | Count of newly created accounts |
| skipped | number | Count of accounts skipped (already existed, skip mode) |
| overwritten | number | Count of accounts overwritten (already existed, overwrite mode) |
| errors | string[] | List of error messages (empty array if none) |

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

**SC-01 — Time to complete onboarding**:  
A user can go from clicking "Nhập COA chuẩn" to having a fully loaded account tree in under 60 seconds for a new tenant with zero existing accounts.

**SC-02 — Data integrity (skip mode)**:  
After a "skip" import, zero pre-existing accounts are modified. Verified by comparing account snapshots before and after import.

**SC-03 — Safety confirmation (overwrite mode)**:  
100% of overwrite imports require passing through an explicit confirmation step. The import must not execute without user confirmation.

**SC-04 — Result transparency**:  
Users always see the exact outcome: imported count, skipped count, overwritten count, and any error details. No import can complete silently without a visible result summary.

**SC-05 — Tree freshness**:  
After every successful import, the account tree reflects the current state of accounts without requiring a manual page refresh.

**SC-06 — Zero regression**:  
All existing account-tree functionality (view tree, add account, edit account, delete account, search) continues to work correctly after the import feature is integrated.

**SC-07 — Error resilience**:  
If the import operation fails entirely (e.g., network error), no partial state is created in the tree view. The account tree remains in its pre-import state.

---

## Assumptions

- **File import is out of scope**: This iteration covers standard template import only. Excel file upload will be specified separately in a future iteration.
- **Account count is static for display**: The preview counts shown in the dialog (TT99 = 200, TT133 = 60) are hardcoded in the UI as confirmed informational display. The actual import outcome count comes from the backend result.
- **TT133 account count**: Confirmed 60 accounts (Thông tư 133/2016/TT-BTC for small businesses). TT99 confirmed 200 accounts (Thông tư 99/2016/TT-BTC for enterprises). These counts are hardcoded in the UI as static informational display.
- **Permission model**: Import is restricted to users with accounting admin or COA management role. The permission check is enforced by both the UI (button visibility) and the backend API.
- **Import is not undoable**: There is no undo operation for COA import. Users who choose "Ghi đè" accept responsibility for data changes. This is communicated via the inline `p-message` warning shown on Step 2.
- **Existing account-tree store**: The `AccountTreeStore.importCoa()` method and `AccountApiService.importCoa()` are already implemented and functional. This feature only adds the UI layer.
- **Auto-reload mechanism**: After import, the tree reloads via the store's existing `loadTree()` mechanism (no new reload logic needed).
- **PrimeNG Dialog and Stepper**: The dialog uses PrimeNG `<p-dialog>` with `<p-steps>` as the confirmed stepper component. Implementation must not use a custom two-step layout or any alternative PrimeNG stepper variant.
- **No batch/background import**: Import is synchronous from the user's perspective — the dialog blocks until the backend responds. If the backend operation is slow, a loading spinner is shown.
- **Tenant-specific**: COA import is scoped to the current tenant. It does not affect other tenants' account data.

---

## Clarifications

### Session 2026-04-18

- Q: Dialog architecture (stepper component and step breakdown)? → A: Option A — PrimeNG `p-steps` inside `p-dialog`. Step 1: choose standard (TT99/TT133). Step 2: choose conflict resolution (skip/overwrite). Back/Next navigation buttons and step indicators. "Nhập" button only on Step 2.
- Q: Should exact account counts be shown in preview UI? → A: Yes. TT99 = 200 accounts (Thông tư 99/2016/TT-BTC, enterprise). TT133 = 60 accounts (Thông tư 133/2016/TT-BTC, small business). Hardcoded in UI as informational static display.
- Q: Overwrite warning approach (inline vs. separate confirmation dialog)? → A: Inline `p-message` (orange/warn severity) shown reactively when user selects "Ghi đè" on Step 2. No separate confirmation dialog required.
- Q: Post-success display (toast vs. in-dialog summary)? → A: Replace stepper with result summary panel inside the same dialog. Imported count in green, skipped in gray, overwritten in orange, errors if any. Single "Đóng" button. Account tree reloads automatically in background.
