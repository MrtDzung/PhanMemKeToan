---
description: Review implemented code against constitution, architecture spec, design system, and feature spec before commit. Covers both backend (C#) and frontend (Angular/TypeScript).
model: Claude Opus 4.6 (copilot)
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Goal

After `speckit.implement` completes, review ALL changed/created files against project standards. Produce a structured review report with CRITICAL/WARNING/PASS verdicts. This is the quality gate before `speckit.git.commit`.

## Operating Constraints

- **READ-ONLY**: Do NOT modify any files. Output a review report only.
- **Scope**: Only review files changed in the current feature (from tasks.md file list or `git diff --name-only`).
- **Constitution is law**: `.specify/memory/constitution.md` rules are NON-NEGOTIABLE. Violations are automatically CRITICAL.
- **No false positives**: Only flag issues you are confident about. When uncertain, mark as WARNING not CRITICAL.

## Execution Steps

### 1. Initialize Review Context

Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS. All paths must be absolute.

Load:
- **SPEC** = FEATURE_DIR/spec.md (requirements to verify)
- **PLAN** = FEATURE_DIR/plan.md (architecture decisions)
- **TASKS** = FEATURE_DIR/tasks.md (file list to scope review)

If check-prerequisites script is not available, fall back to reading tasks.md directly from the most recent feature directory under `.specify/features/`.

### 2. Identify Changed Files

Determine files to review using this priority:
1. Parse `tasks.md` for all file paths mentioned in task descriptions
2. If git is available: `git diff --name-only HEAD` to catch additional changes
3. Combine both lists, deduplicate

Classify each file:
- **Backend**: `*.cs` files
- **Frontend**: `*.ts`, `*.html`, `*.scss`, `*.css` files
- **Config**: `*.json`, `*.yaml`, `*.yml` files
- **Other**: Skip from detailed review (README, docs, etc.)

### 3. Load Review Standards

Read these files for review criteria (use progressive disclosure — only load sections relevant to changed file types):

- **Always**: `.specify/memory/constitution.md` — Core principles, architecture decisions, business rules
- **If Frontend files exist**: `.specify/memory/frontend-design-system.md` — Full design system spec
- **If Backend files exist**: `.specify/memory/architecture-technology-report.md` — Sections: §4 Architecture, §9 Permissions, §15 Security, §20 Testing
- **Feature spec**: FEATURE_DIR/spec.md — Requirements coverage

### 4. Execute Review (7 Categories)

For each changed file, run applicable checks:

#### CAT-1: Architecture Compliance

**Backend (C#):**
- [ ] 1.1 — Clean Architecture: Domain layer has ZERO references to Infrastructure/Application/Presentation
- [ ] 1.2 — CQRS: Commands use EF Core (write), Queries can use Dapper or EF Core (read)
- [ ] 1.3 — MediatR: Commands/Queries go through MediatR pipeline, not direct service calls from controllers
- [ ] 1.4 — Domain events: Business state changes emit domain events via MediatR
- [ ] 1.5 — Outbox pattern: Domain events persisted to OutboxMessage in same transaction

**Frontend (TypeScript/Angular):**
- [ ] 1.6 — Standalone components: No `NgModule` declarations. All components use `standalone: true`
- [ ] 1.7 — Lazy loading: Feature modules loaded via `loadChildren` / `loadComponent` in routes
- [ ] 1.8 — State management: Uses Angular signals or NgRx SignalStore. No raw BehaviorSubject for shared state
- [ ] 1.9 — HttpClient: API calls use HttpClient with interceptors, not raw fetch()

#### CAT-2: Design System Compliance (Frontend only)

- [ ] 2.1 — **CRITICAL**: No hardcoded hex colors in `.scss`/`.css`/inline styles. Must use CSS variables (`var(--primary)`, `var(--error)`, etc.)
- [ ] 2.2 — Font stack: Uses `var(--font-family)` or `var(--font-family-mono)`. No hardcoded font names in components
- [ ] 2.3 — **CRITICAL**: Financial amounts use `tabular-nums` + monospace + `text-align: right`
- [ ] 2.4 — Spacing: Uses spacing scale (4/8/16/24/32px or Tailwind equivalents). No arbitrary px values for margins/paddings
- [ ] 2.5 — PrimeNG: Uses PrimeNG components directly. No custom wrapper components for existing PrimeNG functionality
- [ ] 2.6 — TailwindCSS: Used for layout utilities only (flex, grid, gap, padding). NOT for colors (`text-red-500`) or typography (`text-lg`)

#### CAT-3: Number & Date Formatting

**Backend:**
- [ ] 3.1 — **CRITICAL**: All decimal rounding uses centralized precision config (DecimalPrecisionConfig). No hardcoded `Math.Round(x, 2)`
- [ ] 3.2 — Amount calculations: Uses `decimal` type (not `double`/`float`) for financial amounts

**Frontend:**
- [ ] 3.3 — **CRITICAL**: Number display uses `NumberFormatService` (reads tenant config). No hardcoded `toLocaleString('vi-VN')` or similar
- [ ] 3.4 — Date display: `dd/MM/yyyy` format via centralized pipe/service. No hardcoded format strings in templates
- [ ] 3.5 — Input fields: Amount inputs accept both `.` and `,` as decimal separators

#### CAT-4: Multi-Tenant & Security

**Backend:**
- [ ] 4.1 — **CRITICAL**: All repository queries filtered by TenantId (EF Core Global Query Filter or explicit WHERE)
- [ ] 4.2 — **CRITICAL**: All API endpoints have `[Authorize]` attribute (except auth endpoints)
- [ ] 4.3 — **CRITICAL**: No string concatenation in SQL queries (parameterized only — EF Core LINQ or Dapper `@param`)
- [ ] 4.4 — FluentValidation: Every Command has a corresponding Validator class
- [ ] 4.5 — Sensitive data: Bank account numbers, tax IDs encrypted at rest (AES-256)
- [ ] 4.6 — Soft delete: Financial entities use `IsDeleted` flag, never `DELETE FROM`
- [ ] 4.7 — Audit columns: `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` on all entities

**Frontend:**
- [ ] 4.8 — No sensitive data in localStorage (tokens in memory or httpOnly cookies only)
- [ ] 4.9 — No `innerHTML` binding with user-provided data (XSS risk)

#### CAT-5: Accounting Business Rules

Only check if the feature involves vouchers/posting:
- [ ] 5.1 — **CRITICAL**: Each detail line → exactly 2 GeneralLedger entries (Debit + Credit) that balance
- [ ] 5.2 — **CRITICAL**: Only leaf accounts (IsParent=0) allowed in journal entries
- [ ] 5.3 — Account.DetailBy* flags validated: mandatory dimensions checked before posting
- [ ] 5.4 — Period closed → posting rejected with clear error message
- [ ] 5.5 — Optimistic concurrency: RowVersion/ConcurrencyToken on voucher entities
- [ ] 5.6 — Dual-book support: DisplayOnBook filter applied correctly

#### CAT-6: UX & Accessibility (Frontend only)

Only check if the feature has UI components:
- [ ] 6.1 — Keyboard shortcuts: Voucher forms register required shortcuts (Ctrl+S, F9, Insert, etc.)
- [ ] 6.2 — Dirty form guard: `CanDeactivate` guard on voucher form routes
- [ ] 6.3 — Required fields: Red asterisk `*` on labels, red border on validation error
- [ ] 6.4 — Data grid: Frozen columns (RefNo+RefDate), amount columns right-aligned, sum footer
- [ ] 6.5 — Status badges: Correct color mapping (Draft=gray, Posted=green, Cancelled=red+strikethrough)
- [ ] 6.6 — ARIA: Icon-only buttons have `aria-label` attribute
- [ ] 6.7 — Focus: Interactive elements have visible focus indicator

#### CAT-7: Spec Coverage & Code Quality

- [ ] 7.1 — **CRITICAL**: All functional requirements in spec.md have corresponding implementation
- [ ] 7.2 — Code language: All code, comments, variable names in English. Vietnamese only in i18n key values
- [ ] 7.3 — No `console.log` / `console.debug` / `debugger` statements in production code
- [ ] 7.4 — No unused imports or dead code blocks
- [ ] 7.5 — Domain terminology: Uses project conventions (Voucher, PostingEntry, AccountObject, RefType, etc.)

### 5. Generate Review Report

Format the output as:

```
═══════════════════════════════════════════════
  REVIEW REPORT — [Feature Name from spec.md]
  Date: [today] | Files reviewed: [count]
  Backend: [count] files | Frontend: [count] files
═══════════════════════════════════════════════

Overall: ✅ PASS / ⚠️ WARN / ❌ FAIL

┌────────────────────────┬────────┬──────┬──────┐
│ Category               │ Status │ Pass │ Fail │
├────────────────────────┼────────┼──────┼──────┤
│ CAT-1: Architecture    │ ✅/❌  │ X/Y  │ Z    │
│ CAT-2: Design System   │ ✅/❌  │ X/Y  │ Z    │
│ CAT-3: Number/Date     │ ✅/❌  │ X/Y  │ Z    │
│ CAT-4: Security        │ ✅/❌  │ X/Y  │ Z    │
│ CAT-5: Business Rules  │ ✅/❌  │ X/Y  │ Z    │
│ CAT-6: UX/A11y         │ ✅/❌  │ X/Y  │ Z    │
│ CAT-7: Spec/Quality    │ ✅/❌  │ X/Y  │ Z    │
└────────────────────────┴────────┴──────┴──────┘

## ❌ CRITICAL Issues (must fix before commit)
[number]. [CAT-X.Y] [file:line] — [description]. Fix: [suggestion]

## ⚠️ Warnings (recommend fix)
[number]. [CAT-X.Y] [file:line] — [description]. Fix: [suggestion]

## ✅ Notable Good Practices
- [positive observations about the implementation]

👉 [verdict action — see below]
```

### 6. Verdict & Action

| Condition | Verdict | Output |
|-----------|---------|--------|
| 0 CRITICAL + 0 WARNING | **✅ PASS** | "All checks passed. Ready to commit." |
| 0 CRITICAL + 1+ WARNING | **⚠️ WARN** | "No blocking issues. Fix warnings recommended. Proceed to commit? (yes / fix first)" |
| 1+ CRITICAL | **❌ FAIL** | "CRITICAL issues found. Must fix before commit. Run `/speckit.review` again after fixing." |

## Rules

1. **READ-ONLY** — Never modify files. Report only.
2. **File-scoped** — Only review files in the current feature, not the entire codebase.
3. **Evidence-based** — Every issue must cite a specific file and line number.
4. **No nitpicking** — Focus on constitution/architecture violations, not style preferences.
5. **Category skip** — If no backend files changed, skip CAT-1 backend checks, CAT-4 backend checks, CAT-5. If no frontend files, skip CAT-2, CAT-6.
6. **Proportional** — Small features (1-3 files) get a quick review. Large features (20+ files) get thorough review with sampling.
7. **Positive feedback** — Include 1-3 "Notable Good Practices" to reinforce correct patterns.
8. **Vietnamese OK** — Report can be in Vietnamese if user communicates in Vietnamese.
