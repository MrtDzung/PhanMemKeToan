---
description: Review implemented code against constitution, architecture spec, design system, and feature spec before commit. Covers both backend (C#) and frontend (Angular/TypeScript).
model: ['Claude Opus 4.6 (copilot)']
tools: [read, search, execute, todo, vscode/memory]
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

Read these files for review criteria (use progressive disclosure â€” only load sections relevant to changed file types):

- **Always**: `.specify/memory/constitution.md` â€” Core principles, architecture decisions, business rules
- **If Frontend files exist**: `.specify/memory/frontend-design-system.md` â€” Full design system spec
- **If Backend files exist**: `.specify/memory/architecture-technology-report.md` â€” Sections: Â§4 Architecture, Â§9 Permissions, Â§15 Security, Â§20 Testing
- **Feature spec**: FEATURE_DIR/spec.md â€” Requirements coverage

### 4. Execute Review (9 Categories)

For each changed file, run applicable checks:

#### CAT-1: Architecture Compliance

**Backend (C#):**
- [ ] 1.1 â€” Clean Architecture: Domain layer has ZERO references to Infrastructure/Application/Presentation
- [ ] 1.2 â€” CQRS: Commands use EF Core (write), Queries can use Dapper or EF Core (read)
- [ ] 1.3 â€” MediatR: Commands/Queries go through MediatR pipeline, not direct service calls from controllers
- [ ] 1.4 â€” Domain events: Business state changes emit domain events via MediatR
- [ ] 1.5 â€” Outbox pattern: Domain events persisted to OutboxMessage in same transaction

**Frontend (TypeScript/Angular):**
- [ ] 1.6 â€” Standalone components: No `NgModule` declarations. All components use `standalone: true`
- [ ] 1.7 â€” Lazy loading: Feature modules loaded via `loadChildren` / `loadComponent` in routes
- [ ] 1.8 â€” State management: Uses Angular signals or NgRx SignalStore. No raw BehaviorSubject for shared state
- [ ] 1.9 â€” HttpClient: API calls use HttpClient with interceptors, not raw fetch()
- [ ] 1.10 â€” API controllers are thin: call MediatR.Send() + return ActionResult â€” no business logic in action methods
- [ ] 1.11 â€” CancellationToken passed to all async MediatR.Send() calls
- [ ] 1.12 â€” Frontend: OnPush change detection strategy on all components (not Default)
- [ ] 1.13 â€” Frontend: NgRx SignalStore per feature â€” loading/error state tracked per operation, not globally

#### CAT-2: Design System Compliance (Frontend only)

- [ ] 2.1 â€” **CRITICAL**: No hardcoded hex colors in `.scss`/`.css`/inline styles. Must use CSS variables (`var(--primary)`, `var(--error)`, etc.)
- [ ] 2.2 â€” Font stack: Uses `var(--font-family)` or `var(--font-family-mono)`. No hardcoded font names in components
- [ ] 2.3 â€” **CRITICAL**: Financial amounts use `tabular-nums` + monospace + `text-align: right`
- [ ] 2.4 â€” Spacing: Uses spacing scale (4/8/16/24/32px or Tailwind equivalents). No arbitrary px values for margins/paddings
- [ ] 2.5 â€” PrimeNG: Uses PrimeNG components directly. No custom wrapper components for existing PrimeNG functionality
- [ ] 2.6 â€” TailwindCSS: Used for layout utilities only (flex, grid, gap, padding). NOT for colors (`text-red-500`) or typography (`text-lg`)

#### CAT-3: Number & Date Formatting

**Backend:**
- [ ] 3.1 â€” **CRITICAL**: All decimal rounding uses centralized precision config (DecimalPrecisionConfig). No hardcoded `Math.Round(x, 2)`
- [ ] 3.2 â€” Amount calculations: Uses `decimal` type (not `double`/`float`) for financial amounts

**Frontend:**
- [ ] 3.3 â€” **CRITICAL**: Number display uses `NumberFormatService` (reads tenant config). No hardcoded `toLocaleString('vi-VN')` or similar
- [ ] 3.4 â€” Date display: `dd/MM/yyyy` format via centralized pipe/service. No hardcoded format strings in templates
- [ ] 3.5 â€” Input fields: Amount inputs accept both `.` and `,` as decimal separators

#### CAT-4: Multi-Tenant & Security

**Backend:**
- [ ] 4.1 â€” **CRITICAL**: All repository queries filtered by TenantId (EF Core Global Query Filter or explicit WHERE)
- [ ] 4.2 â€” **CRITICAL**: All API endpoints have `[Authorize]` attribute (except auth endpoints)
- [ ] 4.3 â€” **CRITICAL**: No string concatenation in SQL queries (parameterized only â€” EF Core LINQ or Dapper `@param`)
- [ ] 4.4 â€” FluentValidation: Every Command has a corresponding Validator class
- [ ] 4.5 â€” Sensitive data: Bank account numbers, tax IDs encrypted at rest (AES-256)
- [ ] 4.6 â€” Soft delete: Financial entities use `IsDeleted` flag, never `DELETE FROM`
- [ ] 4.7 â€” Audit columns: `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` on all entities

**Frontend:**
- [ ] 4.8 â€” No sensitive data in localStorage (tokens in memory or httpOnly cookies only)
- [ ] 4.9 â€” No `innerHTML` binding with user-provided data (XSS risk)
- [ ] 4.10 â€” **CRITICAL**: No secrets (JWT signing key, connection strings, API keys) in appsettings.json â€” use User Secrets or environment variables
- [ ] 4.11 â€” **CRITICAL**: No mass assignment â€” DTO-to-entity mapping must NOT allow setting TenantId, PostedDate, PostedBy, IsPosted, CreatedDate, CreatedBy. Verify explicit `Ignore()` or allowlisting in AutoMapper/manual mapping.
- [ ] 4.12 â€” Export endpoints (Excel/PDF) enforce the same TenantId + module permission checks as data grid endpoints â€” export is a common permission bypass vector
- [ ] 4.13 â€” All lazy-loaded Angular module routes have AuthGuard + PermissionGuard applied (`canActivate`/`canMatch`)
- [ ] 4.14 â€” NgRx Signal stores holding financial data cleared on logout AND on tenant switch (prevent data leak on shared devices)

#### CAT-5: Accounting Business Rules

Only check if the feature involves vouchers/posting:
- [ ] 5.1 â€” **CRITICAL**: Each detail line â†’ exactly 2 GeneralLedger entries (Debit + Credit) that balance
- [ ] 5.2 â€” **CRITICAL**: Only leaf accounts (IsParent=0) allowed in journal entries
- [ ] 5.3 â€” Account.DetailBy* flags validated: mandatory dimensions checked before posting
- [ ] 5.4 â€” Period closed â†’ posting rejected with clear error message
- [ ] 5.5 â€” Optimistic concurrency: RowVersion/ConcurrencyToken on voucher entities
- [ ] 5.6 â€” Dual-book support: DisplayOnBook filter applied correctly
- [ ] 5.7 â€” **CRITICAL**: Posted voucher is immutable â€” IsPosted=true, PostedDate=UTC now, PostedBy=UserId set; no further edits allowed
- [ ] 5.8 â€” Unposting requires explicit "Unpost" command with audit trail, not a simple flag flip
- [ ] 5.9 â€” Cannot post if referenced accounts are inactive (Account.IsActive check)
- [ ] 5.10 â€” Multi-currency: functional currency amount calculated and stored alongside foreign amount
- [ ] 5.11 â€” Currency exchange rate taken from system rate table, not from voucher input (for revaluation)

#### CAT-6: UX & Accessibility (Frontend only)

Only check if the feature has UI components:
- [ ] 6.1 â€” Keyboard shortcuts: Voucher forms register required shortcuts (Ctrl+S, F9, Insert, etc.)
- [ ] 6.2 â€” Dirty form guard: `CanDeactivate` guard on voucher form routes
- [ ] 6.3 â€” Required fields: Red asterisk `*` on labels, red border on validation error
- [ ] 6.4 â€” Data grid: Frozen columns (RefNo+RefDate), amount columns right-aligned, sum footer
- [ ] 6.5 â€” Status badges: Correct color mapping (Draft=gray, Posted=green, Cancelled=red+strikethrough)
- [ ] 6.6 â€” ARIA: Icon-only buttons have `aria-label` attribute
- [ ] 6.7 â€” Focus: Interactive elements have visible focus indicator

#### CAT-7: Spec Coverage & Code Quality

- [ ] 7.1 â€” **CRITICAL**: All functional requirements in spec.md have corresponding implementation
- [ ] 7.2 â€” Code language: All code, comments, variable names in English. Vietnamese only in i18n key values
- [ ] 7.3 â€” No `console.log` / `console.debug` / `debugger` statements in production code
- [ ] 7.4 â€” No unused imports or dead code blocks
- [ ] 7.5 â€” Domain terminology: Uses project conventions (Voucher, PostingEntry, AccountObject, RefType, etc.)

#### CAT-8: Database Migration Quality (Backend only)

Only check if the feature includes new/altered EF Core migrations:
- [ ] 8.1 â€” Migration file present for every new table/column/index
- [ ] 8.2 â€” Migration has both `Up()` and `Down()` implemented
- [ ] 8.3 â€” No data migrations mixed with schema migrations (separate migration for data seeding)
- [ ] 8.4 â€” **CRITICAL**: Money/amount columns use `decimal(18,6)` â€” NEVER `float` or `double` (floating-point precision errors in accounting)
- [ ] 8.5 â€” All FK constraints are explicit (not just EF Core shadow properties)
- [ ] 8.6 â€” New tables have: TenantId, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, IsDeleted (soft delete)
- [ ] 8.7 â€” IsDeleted global query filter applied in `ApplicationDbContext.OnModelCreating`

#### CAT-9: Performance

**Backend:**
- [ ] 9.1 â€” No N+1 queries: navigation properties loaded via `.Include()` or projection, not lazy loading
- [ ] 9.2 â€” List queries use `.Select(x => new Dto {...})` projections â€” never load full entity for read-only operations
- [ ] 9.3 â€” Async all the way: no `.Result`, `.Wait()`, or sync-over-async patterns
- [ ] 9.4 â€” No unbounded queries: all list endpoints have pagination (page + pageSize with upper bound)

**Frontend:**
- [ ] 9.5 â€” No heavy computations in template expressions (move to `computed()` signals or pipes)
- [ ] 9.6 â€” No memory leaks: `effect()` cleaned up, subscriptions unsubscribed, timers cleared on destroy
- [ ] 9.7 â€” New lazy-loaded module does NOT increase initial bundle size
- [ ] 9.8 â€” Virtual scroll active for lists expected to exceed 200 items

### 5. Generate Review Report

Format the output as:

```
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  REVIEW REPORT â€” [Feature Name from spec.md]
  Date: [today] | Files reviewed: [count]
  Backend: [count] files | Frontend: [count] files
â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

Overall: âœ… PASS / âš ï¸ WARN / âŒ FAIL
Score: [XX]% ([earned]/[possible] points)

â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”
â”‚ Category                    â”‚ Status â”‚ Score â”‚ Pass â”‚ Fail â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”¤
â”‚ CAT-1: Architecture         â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-2: Design System        â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-3: Number/Date          â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-4: Security             â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-5: Business Rules       â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-6: UX/A11y              â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-7: Spec/Quality         â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-8: Migration Quality    â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â”‚ CAT-9: Performance          â”‚ âœ…/âŒ  â”‚ 10/10 â”‚ X/Y  â”‚ Z    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”˜
N/A categories excluded from score denominator.

## âŒ CRITICAL Issues (must fix before commit)
[number]. [CAT-X.Y] [file:line] â€” [description]. Fix: [suggestion]

## âš ï¸ Warnings (recommend fix)
[number]. [CAT-X.Y] [file:line] â€” [description]. Fix: [suggestion]

## âœ… Notable Good Practices
- [positive observations about the implementation]

ðŸ‘‰ [verdict action â€” see below]
```

### 6. Scoring & Verdict

#### Scoring per Category

| Check result | Points |
|-------------|--------|
| All checks PASS | **10** |
| Has WARNING (0 CRITICAL) | **6** |
| Has CRITICAL | **0** |
| N/A (category skipped) | Excluded from denominator |

Calculate: `score% = (earned / possible) Ã— 100`

#### Automatic Block Rules (override score)

These violations trigger **immediate âŒ FAIL** regardless of overall score:
- **CAT-4** (4.1): Any TenantId filter missing â†’ data isolation breach
- **CAT-5** (5.1): Double-entry balance check missing â†’ financial correctness
- **CAT-3** (3.3): Hardcoded number format â†’ tenant config ignored
- **CAT-4** (4.10): Secrets in appsettings.json â†’ security breach
- **CAT-8** (8.4): `float`/`double` for money columns â†’ precision errors

#### Verdict Decision

| Condition | Verdict | Output |
|-----------|---------|--------|
| Score â‰¥ 85% AND 0 CRITICAL AND no auto-block | **âœ… PASS** | "All checks passed. Score: X%. Ready to commit." |
| Score 70-84% OR has WARNING on auto-block categories | **âš ï¸ WARN** | "Score: X%. Fix warnings recommended. Proceed to commit? (yes / fix first)" |
| Score < 70% OR 1+ CRITICAL OR auto-block triggered | **âŒ FAIL** | "Score: X%. CRITICAL issues found. Must fix before commit." |

## Rules

1. **READ-ONLY** â€” Never modify files. Report only.
2. **File-scoped** â€” Only review files in the current feature, not the entire codebase.
3. **Evidence-based** â€” Every issue must cite a specific file and line number.
4. **No nitpicking** â€” Focus on constitution/architecture violations, not style preferences.
5. **Category skip** â€” If no backend files changed, skip CAT-1 backend checks, CAT-4 backend checks, CAT-5. If no frontend files, skip CAT-2, CAT-6.
6. **Proportional** â€” Small features (1-3 files) get a quick review. Large features (20+ files) get thorough review with sampling.
7. **Positive feedback** â€” Include 1-3 "Notable Good Practices" to reinforce correct patterns.
8. **Vietnamese OK** â€” Report can be in Vietnamese if user communicates in Vietnamese.
