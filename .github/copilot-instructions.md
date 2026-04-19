# Copilot Instructions — PhanMemKeToan

> These rules are auto-loaded by GitHub Copilot for every conversation in this workspace.
> Full reference: `.specify/memory/frontend-design-system.md`

## Project Context
Vietnamese enterprise accounting webapp. Angular 18+ / PrimeNG 17+ / TailwindCSS 3.4+.
15 accounting modules: DI, GL, CA, BA, PU, SA, IN, FA, SU, JC, PA, TA, CT, IP/EI, SYS.
Design-before-code: no implementation without approved spec.
Backend uses dual DbContext: MasterDbContext (central auth, tenant registry) + ApplicationDbContext (per-tenant accounting data via TenantDbContextFactory).

## Code Language
- **Code, API, variables, comments**: English only
- **UI labels, messages**: Vietnamese (via ngx-translate i18n keys)
- **Domain terms in code**: English (e.g., `Voucher`, `PostingEngine`, `AccountObject`)

## Frontend Architecture Rules
- Use Angular standalone components + signals (no NgModules)
- Lazy-load each accounting module separately
- State management: NgRx Signals (lightweight signal store)
- All colors via CSS custom properties (`--primary`, `--error`, etc.) — NEVER hardcode hex in components
- Use PrimeNG components — do NOT create custom replacements for existing PrimeNG components
- TailwindCSS for layout utilities ONLY (flex, grid, spacing) — NOT for colors or typography tokens

## Design Tokens (must use)
```
--primary: #1B5E9E          --primary-dark: #0D3F6E       --primary-light: #E8F0FE
--debit: #1565C0 (blue)     --credit: #C62828 (red)
--positive: #2E7D32         --negative: #C62828
--posted: #2E7D32           --unposted: #F57C00           --draft: #757575
--success: #2E7D32          --warning: #ED6C02            --error: #D32F2F            --info: #0288D1
--surface-ground: #F5F5F5   --surface-card: #FFFFFF       --surface-border: #E0E0E0
--text-primary: #212121     --text-secondary: #616161     --text-disabled: #9E9E9E
```

## Typography
- Font: `'Inter', 'Roboto', 'Segoe UI', sans-serif`
- Monospace (for numbers): `'JetBrains Mono', 'Fira Code', 'Consolas', monospace`
- Base size: 13px. Table cells: 12px. Page titles: 16px.
- All financial amounts: `font-variant-numeric: tabular-nums; text-align: right; font-family: monospace`

## Number Formatting (CRITICAL)
- Number format (thousand separator, decimal separator) is **USER-CONFIGURABLE per tenant** — do NOT hardcode locale
- Use centralized `NumberFormatService` that reads tenant `NumberFormatConfig` (thousandSeparator, decimalSeparator)
- Decimal precision per tenant via `DecimalPrecisionConfig`: amount(0), foreignAmount(3), unitPrice(2), quantity(2), exchangeRate(2), allocation(10)
- Negative: minus prefix + red color (NO parentheses)
- Zero: display `0` (not blank). Null: display blank
- Input fields: accept both `.` and `,`, auto-format on blur

## Date Format
- Display: `dd/MM/yyyy` (Vietnamese standard)
- Input: calendar picker + keyboard accepted

## Data Grid Rules
- Frozen columns: RefNo + RefDate pinned left
- Column resize/reorder: saved to localStorage per user per screen
- Row height: 32px compact (default)
- Amount columns: RIGHT-aligned, monospace, tabular-nums
- Footer: auto-sum for amount/quantity columns, BOLD
- Virtual scroll required for 10K+ rows

## Voucher Form Rules
- Keyboard: Ctrl+S=Save, Ctrl+Shift+S=Save&New, F9=Post, Ctrl+P=Print, Ctrl+D=Duplicate, Insert=AddRow, Ctrl+Delete=DeleteRow, F3=Search
- Required fields: red asterisk `*`, red border on error
- Tab order: left→right, top→bottom, skip auto-filled
- Dirty form guard: warn on unsaved changes before navigation
- Account lookup: type-ahead with hierarchical tree display

## Status Badges
- Draft(Nháp): gray outline. Pending(Chờ duyệt): orange filled. Approved(Đã duyệt): blue filled.
- Posted(Đã ghi sổ): green filled. Cancelled(Đã hủy): red outline + strikethrough.

## Spacing (8px base)
- xs=4px, sm=8px, md=16px, lg=24px, xl=32px

## Performance
- Initial bundle < 300KB gzipped
- Data grid 1000 rows < 500ms render
- Virtual scroll 10K+ rows at 60fps
- FCP < 1.5s, LCP < 2.5s, TTI < 3.5s

## Accessibility (WCAG 2.1 AA)
- Contrast ≥ 4.5:1 text, ≥ 3:1 UI components
- All interactive elements keyboard-focusable with visible focus indicator
- ARIA labels on icon-only buttons
- Data grids: role=grid, aria-sort, aria-selected

## What NOT to do
- Do NOT hardcode hex colors in component styles — use CSS variables
- Do NOT hardcode number format locale — use tenant config
- Do NOT use NgModules — use standalone components
- Do NOT create wrapper components for PrimeNG — use PrimeNG directly with our theme
- Do NOT use `px` units in spacing — use the spacing scale variables
- Do NOT skip keyboard shortcut registration in voucher forms
