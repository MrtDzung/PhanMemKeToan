# Quality Checklist: Import COA Chuẩn

**Purpose**: Validate the completeness, clarity, consistency, and measurability of requirements in spec.md and plan.md for the Import COA Chuẩn feature (DI module — account-tree sub-module).  
**Created**: 2026-04-18  
**Depth**: Standard  
**Audience**: Author + Reviewer (pre-implementation gate)  
**Focus areas**: Angular standalone, PrimeNG p-steps/p-dialog, signal state, store integration, error handling, Vietnamese UI labels, unit test coverage, design system compliance  
**Source docs**: `.specify/features/di-import-coa/spec.md`, `.specify/features/di-import-coa/plan.md`

> **Usage**: Each item tests whether the *requirements themselves* are complete, clear, and ready for implementation. Check each item against spec.md and plan.md before starting T01 in tasks.md.

---

## 1. Angular Standalone Component Requirements

- [ ] CHK001 - Is the full `imports` array for `ImportCoaDialogComponent` exhaustively listed, ensuring all PrimeNG modules (`DialogModule`, `StepsModule`, `MessageModule`, `RadioButtonModule`, `ButtonModule`, `ProgressSpinnerModule`, `CommonModule`) are specified? [Completeness, Plan §2.1]
- [ ] CHK002 - Is `changeDetection: ChangeDetectionStrategy.OnPush` explicitly required in the component decorator specification, not just mentioned as a constraint? [Clarity, Plan §Technical Context]
- [ ] CHK003 - Is the rule that the component must not call any API endpoint directly stated as a hard requirement with enforcement mechanism (lint rule, store-only injection)? [Clarity, Spec §FR-10]
- [ ] CHK004 - Is the parent component file path (which needs wiring) definitively identified, or does it rely on the ambiguous instruction "search for `AccountTreeToolbarComponent` usage to find the exact file"? [Ambiguity, Plan §2.3]
- [ ] CHK005 - Are lifecycle hook requirements specified beyond `(onShow)` — e.g., is `ngOnDestroy` needed to prevent signal memory leaks or subscription cleanup? [Coverage, Gap]
- [ ] CHK006 - Is the `selector` name for `ImportCoaDialogComponent` (e.g., `app-import-coa-dialog`) explicitly specified, or left to implementer discretion? [Clarity, Plan §2.1]

---

## 2. PrimeNG p-steps + p-dialog Usage Requirements

- [ ] CHK007 - Is the `stepItems` array structure (labels, icons if any) fully specified for the `p-steps [model]` binding, or is content left unspecified? [Completeness, Plan §2.1 Template]
- [ ] CHK008 - Is the reason for `[readonly]="true"` on `<p-steps>` documented — i.e., that users must NOT be able to jump steps by clicking step indicators directly? [Clarity, Plan §2.1 Template]
- [ ] CHK009 - Is the `[style]="{width: '520px'}"` dialog width requirement justified by UX/design rationale, and is a responsive/mobile fallback specified? [Completeness, Plan §2.1 Template]
- [ ] CHK010 - Is the `[draggable]="false"` requirement for the dialog explicitly stated in the spec, or is it an undocumented implementation detail in plan.md only? [Consistency, Plan §2.1 vs Spec]
- [ ] CHK011 - Are all `p-dialog` property bindings (`[modal]`, `[closable]`, `(onShow)`, `header`) each traceable to a specific spec requirement (FR-02, FR-05, FR-09)? [Traceability, Spec §FR-02/FR-09]
- [ ] CHK012 - Is the behavior when a user presses Escape key during Step 1 or Step 2 (before import starts) fully specified — should it close the dialog or be blocked? [Clarity, Spec §FR-09]
- [ ] CHK013 - Is the `p-message` `[closable]` property requirement specified — can the user dismiss the overwrite warning inline, or is it permanently visible while "Ghi đè" is selected? [Clarity, Spec §FR-03]
- [ ] CHK014 - Is the visual layout of the two "radio cards" for TT99/TT133 on Step 1 specified (card dimensions, spacing, which PrimeNG component is used for the card frame)? [Completeness, Plan §2.1 Template, Gap]

---

## 3. Signal-Based State Management Requirements

- [ ] CHK015 - Are the initial values for ALL six signals (`activeStep`, `selectedStd`, `resolution`, `importing`, `result`, `importError`) explicitly specified and consistent between plan.md and spec user stories? [Completeness, Plan §2.1]
- [ ] CHK016 - Is the `showResultPanel` computed signal definition unambiguous: does it trigger when EITHER `result !== null` OR `importError !== null`, and is this dual-condition behavior tested in the test plan? [Clarity, Plan §2.1 + Test T09/T12]
- [ ] CHK017 - Is there a requirement specifying whether `result` and `importError` signals are mutually exclusive, or can both be non-null simultaneously? [Clarity, Gap]
- [ ] CHK018 - Is the `selectedStandard` computed signal (derived from `selectedStd` + `COA_STANDARDS`) specified as necessary for the preview text on Step 2, and is the null fallback behavior documented? [Completeness, Plan §2.1]
- [ ] CHK019 - Is the state reset behavior on dialog *close* (not just open) specified — i.e., if user closes the dialog after seeing the result panel and reopens it, is `onDialogShow()` sufficient to guarantee clean state? [Edge Case, Plan §2.1]
- [ ] CHK020 - Is it specified which signals (if any) are exposed as `public` vs. `protected` to the template, given Angular's template compilation rules for `OnPush`? [Clarity, Gap]

---

## 4. AccountTreeStore Integration Requirements

- [ ] CHK021 - Is the `AccountTreeStore.importCoa()` method signature (parameter order: `standard`, then `conflictResolution`) documented to match the call site in `executeImport()`? [Consistency, Plan §2.1 + §2.4]
- [ ] CHK022 - Is the return type fix for `importCoa` (adding `errors: string[]`) specified with a concrete before/after diff, and is it clear which file and which exact line to modify? [Clarity, Plan §2.4]
- [ ] CHK023 - Is the `(err as any)?.error?.detail` error extraction pattern documented as a convention used elsewhere in the codebase, or is this a new arbitrary pattern that needs justification? [Assumption, Plan §2.1]
- [ ] CHK024 - Is the tree auto-reload (FR-07) confirmed to happen *inside* `store.importCoa()` itself, so the component requires NO additional reload call — and is this store behavior documented in the store's spec/plan? [Clarity, Plan §2.1 + Spec §FR-07]
- [ ] CHK025 - Is the `cast: res as ImportCoaResultDto` in `executeImport()` specified as a temporary measure with a clear removal condition (i.e., "remove cast after CHK022 store fix is applied")? [Clarity, Plan §2.1]
- [ ] CHK026 - Is there a requirement to handle the case where `store.importCoa()` resolves successfully but returns `imported=0`, `skipped=0`, `overwritten=0` (all-empty result)? [Edge Case, Spec Edge Cases table]

---

## 5. Error Handling & Loading State Requirements

- [ ] CHK027 - Is "all interactive controls disabled while importing" (FR-05) exhaustively enumerated — does it cover Cancel button in footer, Close (X) icon, radio buttons on Step 2, Back button, and Escape keypress? [Completeness, Spec §FR-05]
- [ ] CHK028 - Is the fallback error message string ("Nhập danh mục thất bại. Vui lòng thử lại.") specified in the spec's Vietnamese UI strings list, or only embedded as a code comment in plan.md? [Consistency, Plan §2.1 vs Spec]
- [ ] CHK029 - Is the "backend returns partial errors" scenario (spec Edge Cases table) fully reflected in FR-06 requirements — specifically, the `errors[]` list rendering and the "collapsible if > 5 items" requirement? [Consistency, Spec §FR-06 + Edge Cases]
- [ ] CHK030 - Is the "collapsible if > 5 error items" requirement specific enough — which PrimeNG component provides the collapse, and is the collapsed/expanded default state defined? [Clarity, Spec §FR-06]
- [ ] CHK031 - Is the visual distinction between "import succeeded with some errors" (partial success) and "import completely failed" (network error) specified clearly for the result panel display? [Clarity, Gap]
- [ ] CHK032 - Is retry behavior after a network error specified — can the user click "Nhập" again, or must they close and reopen the dialog? [Coverage, Gap]

---

## 6. Vietnamese UI Label Requirements

- [ ] CHK033 - Are ALL Vietnamese label strings used in the dialog (step labels, button labels, section headings, result summary labels, error message templates) consolidated in one place in the spec or plan, rather than scattered across sections? [Completeness, Gap]
- [ ] CHK034 - Is it specified whether Vietnamese UI strings should be implemented via ngx-translate i18n keys (project convention per copilot-instructions.md) or as hardcoded template strings? [Consistency, Gap]
- [ ] CHK035 - Is the result summary label format precisely specified: e.g., "Đã nhập: 5 tài khoản" vs. "5 tài khoản đã được nhập" vs. "Đã nhập 5"? [Clarity, Spec §FR-06]
- [ ] CHK036 - Are the two TT99/TT133 card `description` strings ("Dành cho doanh nghiệp vừa và lớn (~200 tài khoản)") final and confirmed, or are they draft copy subject to change before release? [Assumption, Plan §1.1]
- [ ] CHK037 - Is the `aria-label` for the "Nhập COA chuẩn" toolbar button ("Nhập danh mục tài khoản chuẩn") specified as the WCAG accessibility requirement per the design system, and is this the only icon-only-adjacent button requiring an ARIA label? [Completeness, Plan §2.2]

---

## 7. Unit Test Coverage Requirements

- [ ] CHK038 - Are the 14 planned tests (T01–T14) collectively sufficient to cover all 11 functional requirements (FR-01 through FR-11), or are there FRs with no corresponding test case? [Coverage, Plan §3 vs Spec §FR-*]
- [ ] CHK039 - Is the mock `AccountTreeStore` setup in tests specified to return `Promise.resolve(...)` for success AND `Promise.reject(...)` for failure scenarios — are both stubs documented in plan.md? [Completeness, Plan §3 DI section]
- [ ] CHK040 - Is T10 ("Result summary shows correct counts with correct CSS") measurable as a unit test — specifically, how is CSS variable application (e.g., `color: var(--positive)`) verified in Karma/Jasmine without a real browser rendering engine? [Measurability, Plan §3 T10]
- [ ] CHK041 - Is T08 ("Loading state disables all buttons") defined with sufficient specificity — which DOM query selectors or fixture queries confirm that *all* targeted buttons have `[disabled]` attribute? [Clarity, Plan §3 T08]
- [ ] CHK042 - Is there a test for FR-11 (no file upload UI present) — i.e., a test asserting absence of `<input type="file">` or file-upload-related elements in the template? [Coverage, Spec §FR-11, Gap]
- [ ] CHK043 - Is there a test for FR-02's "Nhập button not shown on Step 1" (spec requirement) — T05 tests the footer content on Step 1 but is it specific enough to assert that no "Nhập" button exists in the DOM? [Clarity, Plan §3 T05 vs Spec §FR-02]
- [ ] CHK044 - Is the test for T01 (state reset on `onDialogShow`) verifying ALL six signals reset to their exact initial values, or only a subset? [Completeness, Plan §3 T01]
- [ ] CHK045 - Are there tests for the `canNext` and `canImport` computed signals' boundary conditions (e.g., `canImport = false` when `importing = true` AND `selectedStd` is set)? [Coverage, Plan §3, Gap]

---

## 8. Design System Compliance Requirements

- [ ] CHK046 - Is the use of `--positive` for imported count, `--text-secondary` for skipped count, and `--warning` for overwritten count cross-referenced with the design system token definitions in copilot-instructions.md to confirm exact token names match? [Consistency, Spec §FR-06 + Design System]
- [ ] CHK047 - Is the dialog `header` font size and weight specified using design system typography tokens (base 13px / page title 16px), or is the PrimeNG default header styling acceptable? [Clarity, Gap]
- [ ] CHK048 - Are spacing requirements inside the dialog (between step indicators, between radio cards, between result rows) specified using the 8px scale variables (xs/sm/md/lg/xl), rather than arbitrary pixel values? [Completeness, Gap]
- [ ] CHK049 - Is there a requirement prohibiting hardcoded hex colors in the component's inline styles or class bindings — and is this covered by a spec rule or only by the constitution? [Consistency, copilot-instructions.md]
- [ ] CHK050 - Is the `accountCount` number display (200, 60) in radio cards required to use monospace/tabular-nums styling as per the design system's financial number formatting rule, or is it exempt as a static informational label? [Clarity, copilot-instructions.md + Spec §Key Entities]

---

## 9. Acceptance Criteria Quality

- [ ] CHK051 - Is SC-03 ("100% of overwrite imports require passing through an explicit confirmation step") consistent with FR-04 which explicitly states NO separate confirmation dialog is required? These appear to conflict. [Conflict, Spec §SC-03 vs §FR-04]
- [ ] CHK052 - Is SC-01 ("user can complete onboarding in under 60 seconds") measurable as a frontend unit/integration metric, or does it depend on backend import speed which the frontend cannot control? [Measurability, Spec §SC-01]
- [ ] CHK053 - Is SC-07 ("no partial state in tree view if import fails entirely") verifiable from the frontend — is there a specified assertion that `store.loadTree()` is NOT called when `executeImport()` catches an error? [Measurability, Spec §SC-07 + Plan §2.1]
- [ ] CHK054 - Is SC-02 ("zero pre-existing accounts modified in skip mode") a frontend-verifiable criterion, or does it require a backend integration test? If it requires backend, is that test out of scope for the 14-test unit test plan? [Clarity, Spec §SC-02, Gap]

---

## 10. Edge Case & Scenario Coverage

- [ ] CHK055 - Is the "tenant has no permission to import" edge case (spec Edge Cases table) reflected in FR-01 (button hidden) and is there a specified mechanism — `ngIf` on permission signal, route guard, or disabled state? [Completeness, Spec §FR-01 + Edge Cases]
- [ ] CHK056 - Is the "user closes dialog mid-wizard" edge case specified to cover Step 1, Step 2 (before clicking Nhập), AND the result panel state — are all three close-points tested? [Coverage, Spec Edge Cases]
- [ ] CHK057 - Is the "all accounts already exist in skip mode" edge case (imported=0, skipped=N) explicitly confirmed to show the result summary panel (not an error state)? [Clarity, Spec Edge Cases]
- [ ] CHK058 - Is the behavior when `COA_STANDARDS` array is empty or malformed (defensive coding) specified, or is it assumed to always be valid as a hardcoded constant? [Assumption, Plan §1.1]

---

## Ambiguities & Conflicts to Resolve Before Implementation

- [ ] CHK059 - **[Conflict]** SC-03 states overwrite requires "explicit confirmation step" but FR-04 states no separate confirmation dialog. Clarify: does the inline `p-message` warning (FR-03) count as the "explicit confirmation step" in SC-03? [Conflict, Spec §SC-03 vs §FR-04]
- [ ] CHK060 - **[Ambiguity]** Plan §2.3 says "search for `AccountTreeToolbarComponent` usage to find the exact file" — is the exact parent component file path (`account-tree-page.component.ts`?) determined and should be documented before implementation starts? [Ambiguity, Plan §2.3]
- [ ] CHK061 - **[Assumption]** Plan assumes `store.importCoa()` already triggers `loadTree()` internally (FR-07). Is this confirmed by reading the existing `account-tree.store.ts` source, and is it documented in a store-level spec? [Assumption, Plan §2.1 + Spec §FR-07]
- [ ] CHK062 - **[Gap]** No requirement specifies what happens if the user selects "Ghi đè", starts import, and the network disconnects mid-request — is the dialog stuck in loading state indefinitely, or is there a timeout/cancel mechanism? [Edge Case, Gap]
