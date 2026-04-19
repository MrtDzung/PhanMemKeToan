---
description: Review implemented code against constitution, architecture spec, design system, and feature spec before commit. Covers both backend (C#) and frontend (Angular/TypeScript).
model: Claude Sonnet 4.6 (copilot)
tools: [vscode/getProjectSetupInfo, vscode/installExtension, vscode/memory, vscode/newWorkspace, vscode/resolveMemoryFileUri, vscode/runCommand, vscode/vscodeAPI, vscode/extensions, vscode/askQuestions, execute/runNotebookCell, execute/testFailure, execute/getTerminalOutput, execute/killTerminal, execute/sendToTerminal, execute/createAndRunTask, execute/runInTerminal, execute/runTests, read/getNotebookSummary, read/problems, read/readFile, read/viewImage, read/terminalSelection, read/terminalLastCommand, agent/runSubagent, edit/createDirectory, edit/createFile, edit/createJupyterNotebook, edit/editFiles, edit/editNotebook, edit/rename, search/changes, search/codebase, search/fileSearch, search/listDirectory, search/textSearch, search/usages, web/fetch, web/githubRepo, awesome-copilot/load_instruction, awesome-copilot/search_instructions, browser-tools/getConsoleErrors, browser-tools/getConsoleLogs, browser-tools/getNetworkErrors, browser-tools/getNetworkLogs, browser-tools/getSelectedElement, browser-tools/runAccessibilityAudit, browser-tools/runAuditMode, browser-tools/runBestPracticesAudit, browser-tools/runDebuggerMode, browser-tools/runNextJSAudit, browser-tools/runPerformanceAudit, browser-tools/runSEOAudit, browser-tools/takeScreenshot, browser-tools/wipeLogs, context7/query-docs, context7/resolve-library-id, github/add_comment_to_pending_review, github/add_issue_comment, github/assign_copilot_to_issue, github/create_branch, github/create_or_update_file, github/create_pull_request, github/create_repository, github/delete_file, github/fork_repository, github/get_commit, github/get_file_contents, github/get_label, github/get_latest_release, github/get_me, github/get_release_by_tag, github/get_tag, github/get_team_members, github/get_teams, github/issue_read, github/issue_write, github/list_branches, github/list_commits, github/list_issue_types, github/list_issues, github/list_pull_requests, github/list_releases, github/list_tags, github/merge_pull_request, github/pull_request_read, github/pull_request_review_write, github/push_files, github/request_copilot_review, github/search_code, github/search_issues, github/search_pull_requests, github/search_repositories, github/search_users, github/sub_issue_write, github/update_pull_request, github/update_pull_request_branch, playwright/browser_click, playwright/browser_close, playwright/browser_console_messages, playwright/browser_drag, playwright/browser_evaluate, playwright/browser_file_upload, playwright/browser_fill_form, playwright/browser_handle_dialog, playwright/browser_hover, playwright/browser_navigate, playwright/browser_navigate_back, playwright/browser_network_requests, playwright/browser_press_key, playwright/browser_resize, playwright/browser_run_code, playwright/browser_select_option, playwright/browser_snapshot, playwright/browser_tabs, playwright/browser_take_screenshot, playwright/browser_type, playwright/browser_wait_for, screenshot/capture, browser/openBrowserPage, ms-azuretools.vscode-containers/containerToolsConfig, ms-mssql.mssql/mssql_schema_designer, ms-mssql.mssql/mssql_dab, ms-mssql.mssql/mssql_connect, ms-mssql.mssql/mssql_disconnect, ms-mssql.mssql/mssql_list_servers, ms-mssql.mssql/mssql_list_databases, ms-mssql.mssql/mssql_get_connection_details, ms-mssql.mssql/mssql_change_database, ms-mssql.mssql/mssql_list_tables, ms-mssql.mssql/mssql_list_schemas, ms-mssql.mssql/mssql_list_views, ms-mssql.mssql/mssql_list_functions, ms-mssql.mssql/mssql_run_query, todo]
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

### 4. Execute Review (9 Categories)

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
- [ ] 1.10 — API controllers are thin: call MediatR.Send() + return ActionResult — no business logic in action methods
- [ ] 1.11 — CancellationToken passed to all async MediatR.Send() calls
- [ ] 1.12 — Frontend: OnPush change detection strategy on all components (not Default)
- [ ] 1.13 — Frontend: NgRx SignalStore per feature — loading/error state tracked per operation, not globally

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
- [ ] 4.10 — **CRITICAL**: No secrets (JWT signing key, connection strings, API keys) in appsettings.json — use User Secrets or environment variables
- [ ] 4.11 — **CRITICAL**: No mass assignment — DTO-to-entity mapping must NOT allow setting TenantId, PostedDate, PostedBy, IsPosted, CreatedDate, CreatedBy. Verify explicit `Ignore()` or allowlisting in AutoMapper/manual mapping.
- [ ] 4.12 — Export endpoints (Excel/PDF) enforce the same TenantId + module permission checks as data grid endpoints — export is a common permission bypass vector
- [ ] 4.13 — All lazy-loaded Angular module routes have AuthGuard + PermissionGuard applied (`canActivate`/`canMatch`)
- [ ] 4.14 — NgRx Signal stores holding financial data cleared on logout AND on tenant switch (prevent data leak on shared devices)

#### CAT-5: Accounting Business Rules

Only check if the feature involves vouchers/posting:
- [ ] 5.1 — **CRITICAL**: Each detail line → exactly 2 GeneralLedger entries (Debit + Credit) that balance
- [ ] 5.2 — **CRITICAL**: Only leaf accounts (IsParent=0) allowed in journal entries
- [ ] 5.3 — Account.DetailBy* flags validated: mandatory dimensions checked before posting
- [ ] 5.4 — Period closed → posting rejected with clear error message
- [ ] 5.5 — Optimistic concurrency: RowVersion/ConcurrencyToken on voucher entities
- [ ] 5.6 — Dual-book support: DisplayOnBook filter applied correctly
- [ ] 5.7 — **CRITICAL**: Posted voucher is immutable — IsPosted=true, PostedDate=UTC now, PostedBy=UserId set; no further edits allowed
- [ ] 5.8 — Unposting requires explicit "Unpost" command with audit trail, not a simple flag flip
- [ ] 5.9 — Cannot post if referenced accounts are inactive (Account.IsActive check)
- [ ] 5.10 — Multi-currency: functional currency amount calculated and stored alongside foreign amount
- [ ] 5.11 — Currency exchange rate taken from system rate table, not from voucher input (for revaluation)

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

#### CAT-8: Database Migration Quality (Backend only)

Only check if the feature includes new/altered EF Core migrations:
- [ ] 8.1 — Migration file present for every new table/column/index
- [ ] 8.2 — Migration has both `Up()` and `Down()` implemented
- [ ] 8.3 — No data migrations mixed with schema migrations (separate migration for data seeding)
- [ ] 8.4 — **CRITICAL**: Money/amount columns use `decimal(18,6)` — NEVER `float` or `double` (floating-point precision errors in accounting)
- [ ] 8.5 — All FK constraints are explicit (not just EF Core shadow properties)
- [ ] 8.6 — New tables have: TenantId, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, IsDeleted (soft delete)
- [ ] 8.7 — IsDeleted global query filter applied in `ApplicationDbContext.OnModelCreating`

#### CAT-9: Performance

**Backend:**
- [ ] 9.1 — No N+1 queries: navigation properties loaded via `.Include()` or projection, not lazy loading
- [ ] 9.2 — List queries use `.Select(x => new Dto {...})` projections — never load full entity for read-only operations
- [ ] 9.3 — Async all the way: no `.Result`, `.Wait()`, or sync-over-async patterns
- [ ] 9.4 — No unbounded queries: all list endpoints have pagination (page + pageSize with upper bound)

**Frontend:**
- [ ] 9.5 — No heavy computations in template expressions (move to `computed()` signals or pipes)
- [ ] 9.6 — No memory leaks: `effect()` cleaned up, subscriptions unsubscribed, timers cleared on destroy
- [ ] 9.7 — New lazy-loaded module does NOT increase initial bundle size
- [ ] 9.8 — Virtual scroll active for lists expected to exceed 200 items

### 5. Generate Review Report

Format the output as:

```
═══════════════════════════════════════════════
  REVIEW REPORT — [Feature Name from spec.md]
  Date: [today] | Files reviewed: [count]
  Backend: [count] files | Frontend: [count] files
═══════════════════════════════════════════════

Overall: ✅ PASS / ⚠️ WARN / ❌ FAIL
Score: [XX]% ([earned]/[possible] points)

┌─────────────────────────────┬────────┬───────┬──────┬──────┐
│ Category                    │ Status │ Score │ Pass │ Fail │
├─────────────────────────────┼────────┼───────┼──────┼──────┤
│ CAT-1: Architecture         │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-2: Design System        │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-3: Number/Date          │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-4: Security             │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-5: Business Rules       │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-6: UX/A11y              │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-7: Spec/Quality         │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-8: Migration Quality    │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
│ CAT-9: Performance          │ ✅/❌  │ 10/10 │ X/Y  │ Z    │
└─────────────────────────────┴────────┴───────┴──────┴──────┘
N/A categories excluded from score denominator.

## ❌ CRITICAL Issues (must fix before commit)
[number]. [CAT-X.Y] [file:line] — [description]. Fix: [suggestion]

## ⚠️ Warnings (recommend fix)
[number]. [CAT-X.Y] [file:line] — [description]. Fix: [suggestion]

## ✅ Notable Good Practices
- [positive observations about the implementation]

👉 [verdict action — see below]
```

### 6. Scoring & Verdict

#### Scoring per Category

| Check result | Points |
|-------------|--------|
| All checks PASS | **10** |
| Has WARNING (0 CRITICAL) | **6** |
| Has CRITICAL | **0** |
| N/A (category skipped) | Excluded from denominator |

Calculate: `score% = (earned / possible) × 100`

#### Automatic Block Rules (override score)

These violations trigger **immediate ❌ FAIL** regardless of overall score:
- **CAT-4** (4.1): Any TenantId filter missing → data isolation breach
- **CAT-5** (5.1): Double-entry balance check missing → financial correctness
- **CAT-3** (3.3): Hardcoded number format → tenant config ignored
- **CAT-4** (4.10): Secrets in appsettings.json → security breach
- **CAT-8** (8.4): `float`/`double` for money columns → precision errors

#### Verdict Decision

| Condition | Verdict | Output |
|-----------|---------|--------|
| Score ≥ 85% AND 0 CRITICAL AND no auto-block | **✅ PASS** | "All checks passed. Score: X%. Ready to commit." |
| Score 70-84% OR has WARNING on auto-block categories | **⚠️ WARN** | "Score: X%. Fix warnings recommended. Proceed to commit? (yes / fix first)" |
| Score < 70% OR 1+ CRITICAL OR auto-block triggered | **❌ FAIL** | "Score: X%. CRITICAL issues found. Must fix before commit." |

## Rules

1. **READ-ONLY** — Never modify files. Report only.
2. **File-scoped** — Only review files in the current feature, not the entire codebase.
3. **Evidence-based** — Every issue must cite a specific file and line number.
4. **No nitpicking** — Focus on constitution/architecture violations, not style preferences.
5. **Category skip** — If no backend files changed, skip CAT-1 backend checks, CAT-4 backend checks, CAT-5. If no frontend files, skip CAT-2, CAT-6.
6. **Proportional** — Small features (1-3 files) get a quick review. Large features (20+ files) get thorough review with sampling.
7. **Positive feedback** — Include 1-3 "Notable Good Practices" to reinforce correct patterns.
8. **Vietnamese OK** — Report can be in Vietnamese if user communicates in Vietnamese.
