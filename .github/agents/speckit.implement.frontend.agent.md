---
description: Execute frontend implementation tasks (Angular/TypeScript/PrimeNG). Handles all [FE] tasks from tasks.md. Runs AFTER backend implementation to ensure API contracts are available.
model: ['GPT-5.4 (copilot)', 'Claude Sonnet 4.6 (copilot)']
tools: [read, edit, search, execute, web, 'context7/*', todo, vscode/askQuestions, vscode/memory]
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before frontend implementation)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_implement` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Scope

This agent handles **FRONTEND tasks only**:
- Files in `src/webapp/` directory
- File types: `*.ts`, `*.html`, `*.scss`, `*.css`, `*.json` (Angular/TypeScript)
- Task markers: `[FE]` in tasks.md
- **SKIP** all `[BE]` tasks — those are handled by `speckit.implement`

## Outline

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute.

2. **Check checklists status** (if FEATURE_DIR/checklists/ exists):
   - Scan all checklist files in the checklists/ directory
   - For each checklist, count total/completed/incomplete items
   - If any checklist is incomplete: display table and ask user to proceed or stop
   - If all checklists are complete: automatically proceed

3. Load and analyze the implementation context:
   - **REQUIRED**: Read tasks.md — extract only `[FE]` tasks
   - **REQUIRED**: Read plan.md for tech stack, architecture, and file structure
   - **IF EXISTS**: Read data-model.md for entities and relationships (to build TypeScript models)
   - **IF EXISTS**: Read contracts/ for API specifications (to build Angular services)
   - **IF EXISTS**: Check `src/webapp/src/app/mockups/<module>/` for approved Angular mockup components — **copy and adapt** these instead of building from scratch
   - **REQUIRED**: Read `.specify/memory/frontend-design-system.md` for design tokens and rules
   - **REQUIRED**: Read `.github/copilot-instructions.md` for frontend architecture rules

3b. **Mockup-to-Feature Migration** (if mockup components exist):
   - Copy component files from `mockups/<module>/<screen>/` to `features/<module>/<screen>/`
   - Replace mock data signals with real service injections (`inject(HttpClient)`, `inject(XxxService)`)
   - Replace hardcoded Vietnamese strings with i18n keys (`{{ 'key' | translate }}`)
   - Wire routes in feature module routes (remove `/mockup` prefix)
   - Delete mockup folder and route after migration
   - This saves significant time — mockup code is production-quality Angular

4. **Filter tasks**: From tasks.md, extract ONLY tasks marked `[FE]`. Ignore all `[BE]` tasks. For tasks without a marker that clearly belong to frontend (e.g., Angular components, routes, i18n), include them.

5. Execute frontend implementation following the task plan:
   - **Phase-by-phase execution**: Complete each phase before moving to the next
   - **Respect dependencies**: Sequential tasks in order, parallel `[P]` tasks together
   - **File-based coordination**: Tasks affecting the same files must run sequentially
   - **Validation checkpoints**: After each phase, run `npx ng build` to verify no compile errors

6. **Frontend-Specific Rules (MUST FOLLOW)**:

   ### Angular Architecture
   - **Standalone components** — No NgModules. All components use `standalone: true`
   - **Signals** — Use Angular signals for reactive state, not BehaviorSubject
   - **OnPush** — All components use `ChangeDetectionStrategy.OnPush`
   - **Lazy loading** — Feature modules loaded via `loadChildren`/`loadComponent` in routes
   - **NgRx SignalStore** — Per feature, loading/error state tracked per operation

   ### Design System Compliance
   - **NO hardcoded hex colors** — Use CSS custom properties (`var(--primary)`, `var(--error)`, etc.)
   - **Typography**: Base 13px, tables 12px, titles 16px. Monospace for numbers
   - **Financial amounts**: `font-variant-numeric: tabular-nums; text-align: right; font-family: var(--font-mono)`
   - **Spacing**: Use 8px base scale (xs=4, sm=8, md=16, lg=24, xl=32)
   - **PrimeNG**: Use directly, do NOT create wrapper components
   - **TailwindCSS**: Layout utilities ONLY (flex, grid, spacing). NOT for colors or typography

   ### Number & Date Formatting
   - Numbers via centralized `NumberFormatService` (tenant config) — NEVER hardcode locale
   - Dates: `dd/MM/yyyy` via centralized pipe/service
   - Input fields: Accept both `.` and `,` as decimal separators, auto-format on blur

   ### Keyboard & UX
   - Voucher forms: Register shortcuts (Ctrl+S=Save, F9=Post, Insert=AddRow, etc.)
   - Required fields: Red asterisk `*`, red border on error
   - Dirty form guard: `CanDeactivate` on voucher routes
   - Data grids: Frozen columns (RefNo+RefDate), right-aligned amounts, sum footers

   ### Accessibility (WCAG 2.1 AA)
   - ARIA labels on icon-only buttons
   - Keyboard-focusable interactive elements with visible focus indicator
   - Contrast ≥ 4.5:1 text, ≥ 3:1 UI components

   ### i18n
   - All UI text via ngx-translate i18n keys (Vietnamese)
   - Code comments in English

7. Progress tracking and error handling:
   - Report progress after each completed task
   - Mark completed tasks as `[X]` in tasks.md
   - Halt execution if compile errors — fix before proceeding
   - Provide clear error messages with context

8. Completion validation:
   - Run `npx ng build` to verify zero compile errors
   - Verify all `[FE]` tasks are marked `[X]` in tasks.md
   - Confirm Angular components follow standalone + OnPush pattern
   - Check no hardcoded hex colors in any `.scss`/`.css`/inline styles
   - Report final status with summary of completed frontend work

9. **Check for extension hooks**: After completion, check `.specify/extensions.yml` for `hooks.after_implement` entries and handle as described in Pre-Execution Checks.
