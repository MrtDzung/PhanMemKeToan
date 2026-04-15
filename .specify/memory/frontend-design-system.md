# Frontend Design System — PhanMemKeToan

> Angular 18+ / PrimeNG 17+ / TailwindCSS 3.4+
> Vietnamese Enterprise Accounting Webapp
> Approved: 2026-04-15

---

## 1. COLOR SYSTEM

### 1.1 Primary Palette
| Token | Hex | Usage |
|-------|-----|-------|
| `--primary` | `#1B5E9E` | Header, primary buttons, active tabs, links |
| `--primary-dark` | `#0D3F6E` | Hover state, active sidebar item |
| `--primary-light` | `#E8F0FE` | Selected row, active filter badge |

### 1.2 Semantic Colors (Accounting-specific)
| Token | Hex | Usage |
|-------|-----|-------|
| `--debit` | `#1565C0` (blue) | Nợ (Debit) amounts, debit columns |
| `--credit` | `#C62828` (red) | Có (Credit) amounts, credit columns |
| `--positive` | `#2E7D32` (green) | Lãi, surplus, positive variance |
| `--negative` | `#C62828` (red) | Lỗ, deficit, negative variance |
| `--posted` | `#2E7D32` | Đã ghi sổ (Posted status) |
| `--unposted` | `#F57C00` (orange) | Chưa ghi sổ (Unposted status) |
| `--draft` | `#757575` (gray) | Nháp (Draft) |

### 1.3 Neutral & Surface
| Token | Hex | Usage |
|-------|-----|-------|
| `--surface-ground` | `#F5F5F5` | Page background |
| `--surface-card` | `#FFFFFF` | Cards, panels, modals |
| `--surface-border` | `#E0E0E0` | Dividers, table borders |
| `--text-primary` | `#212121` | Main text |
| `--text-secondary` | `#616161` | Labels, captions |
| `--text-disabled` | `#9E9E9E` | Disabled elements |

### 1.4 Alert Colors
| Token | Hex | Usage |
|-------|-----|-------|
| `--success` | `#2E7D32` | Thành công |
| `--warning` | `#ED6C02` | Cảnh báo (SLA gần hết, thiếu thông tin) |
| `--error` | `#D32F2F` | Lỗi validation, lệch cân đối |
| `--info` | `#0288D1` | Thông báo, hướng dẫn |

> Blue primary = trustworthy, professional. Debit=blue / Credit=red is standard Vietnamese accounting convention.

---

## 2. TYPOGRAPHY

### 2.1 Font Stack
```scss
--font-family: 'Inter', 'Roboto', 'Segoe UI', -apple-system, sans-serif;
--font-family-mono: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
```
- **Inter**: Excellent Vietnamese diacritical marks support, free, great hinting on Windows
- **Monospace**: Used for financial amounts in data grids — numbers align vertically

### 2.2 Scale
| Token | Size | Weight | Usage |
|-------|------|--------|-------|
| `--text-xs` | 11px | 400 | Footnotes, helper text |
| `--text-sm` | 12px | 400 | Table cells, form labels |
| `--text-base` | 13px | 400 | Default body text |
| `--text-md` | 14px | 500 | Section headers, card titles |
| `--text-lg` | 16px | 600 | Page titles, voucher headers |
| `--text-xl` | 20px | 700 | Module names, report titles |

> Base 13px — accounting software needs dense data display. MISA uses 12-13px.

### 2.3 Number Typography (CRITICAL)
```scss
.amount, .quantity, .unit-price, .exchange-rate {
  font-feature-settings: 'tnum' 1;
  font-variant-numeric: tabular-nums;
  font-family: var(--font-family-mono);
  text-align: right;
}
```
All financial numbers MUST use tabular figures + monospace + right-align.

---

## 3. NUMBER & CURRENCY FORMATTING

### 3.1 User-Configurable Format (CRITICAL)
Number formatting (thousand separator, decimal separator) is **user-configurable per tenant**, NOT hardcoded to any locale. The system stores the user's chosen format in tenant settings and applies it globally.

#### Tenant Number Format Config
```typescript
interface NumberFormatConfig {
  thousandSeparator: string;   // e.g. '.' or ',' or ' '
  decimalSeparator: string;    // e.g. ',' or '.'
  // Validation: thousandSeparator !== decimalSeparator
}
```

#### Default Presets
| Preset | Thousand Sep | Decimal Sep | Example |
|--------|-------------|-------------|---------|
| Vietnamese (default) | `.` | `,` | `1.234.567,89` |
| International | `,` | `.` | `1,234,567.89` |
| Space | ` ` (space) | `,` | `1 234 567,89` |

### 3.2 Decimal Precision Config (per tenant)
```typescript
interface DecimalPrecisionConfig {
  amountDecimalDigits: number;       // VND default=0
  amountOCDecimalDigits: number;     // Foreign currency default=3
  unitPriceDecimalDigits: number;    // default=2
  quantityDecimalDigits: number;     // default=2
  exchangeRateDecimalDigits: number; // default=2
  allocationDecimalDigits: number;   // default=10
}
```

### 3.3 Display Rules
- **Negative amounts**: Minus sign prefix, red color: `-1.234.567` (NO parentheses)
- **Zero amounts**: Display `0` (not blank, not `0,00` for VND)
- **Null/empty**: Display blank (empty string)
- **Input fields**: Accept both `.` and `,` as decimal input, auto-format on blur using tenant config
- **All calculations**: Use centralized `ROUND()` based on tenant DecimalPrecisionConfig

---

## 4. LAYOUT & SPACING

### 4.1 Spacing Scale (8px base unit)
```scss
--spacing-xs: 4px;   // Between inline elements
--spacing-sm: 8px;   // Default gap between form fields
--spacing-md: 16px;  // Padding inside cards/panels
--spacing-lg: 24px;  // Gap between sections
--spacing-xl: 32px;  // Page margins
```

### 4.2 Page Layout
```
┌──────────────────────────────────────────────────┐
│  Top Bar (48px)  — Logo, tenant, user, notif     │
├────────┬─────────────────────────────────────────┤
│Sidebar │  Breadcrumb (32px)                       │
│(220px) │─────────────────────────────────────────│
│collap- │  Toolbar (40px) — Actions, filters       │
│sible   │─────────────────────────────────────────│
│to 56px │  Content Area (flexible)                 │
│        │  - List view (data grid)                 │
│        │  - Detail view (voucher form)            │
│        │  - Dashboard (cards + charts)            │
│        │─────────────────────────────────────────│
│        │  Status bar (24px) — record count, sum   │
└────────┴─────────────────────────────────────────┘
```

### 4.3 Responsive Breakpoints
| Name | Min-width | Sidebar | Grid cols |
|------|-----------|---------|-----------|
| `desktop-lg` | 1440px | 220px expanded | 12 |
| `desktop` | 1280px | 220px / 56px toggle | 12 |
| `tablet` | 768px | 56px collapsed | 8 |
| `mobile` | 0px | Hidden (hamburger) | 4 |

> Primary target: Desktop 1280-1920px. Accountants primarily use desktop. Tablet/mobile is secondary.

---

## 5. DATA GRID STANDARDS (Core UX)

### 5.1 PrimeNG Table Features
- **Frozen columns**: RefNo + RefDate always pinned left
- **Column resize**: User-adjustable, saved to localStorage per user per screen
- **Column reorder**: Drag & drop, saved preference
- **Sorting**: Multi-column sort (Shift+Click)
- **Inline editing**: Double-click cell for voucher detail grid
- **Row height**: 32px compact (default), 40px comfortable (user toggle)
- **Alternating rows**: `--surface-ground` on even rows
- **Selection**: Checkbox column for batch operations
- **Sum footer**: Auto-sum for amount, quantity columns
- **Virtual scroll**: Required for 10K+ rows (smooth 60fps)

### 5.2 Amount Column Rules
- Numbers: RIGHT-aligned, monospace, tabular-nums
- Footer sum: BOLD, top border separator
- Debit column: `--debit` color
- Credit column: `--credit` color
- Negative: red with minus prefix

---

## 6. FORM STANDARDS (Voucher Forms)

### 6.1 Layout Pattern
```
┌──────────────────────────────────────────────┐
│ [Icon] Phiếu chi - PC-00042   [Save][Post][Print][×] │
├──────────────────────────────────────────────┤
│ Ngày HT: [dd/MM/yyyy]  Ngày CT: [__]  Số CT: [Auto]  │
│ Đối tượng: [🔍 Dropdown]  Địa chỉ: [Auto-fill]       │
│ Diễn giải: [______________________________]            │
├──────────────────────────────────────────────┤
│ [Hàng tiền] [Thuế] [Thông tin bổ sung] [Hạch toán]    │
│ ┌──┬────────┬────────┬──────────┬──────────┐           │
│ │# │TK Nợ   │TK Có   │Số tiền   │Diễn giải│           │
│ ├──┼────────┼────────┼──────────┼──────────┤           │
│ │1 │1111    │3311    │1.234.567 │Thanh toán│           │
│ │+ │        │        │          │          │           │
│ └──┴────────┴────────┴──────────┴──────────┘           │
├──────────────────────────────────────────────┤
│ Tổng tiền: 1.234.567       Đã ghi sổ: [✓]  │
└──────────────────────────────────────────────┘
```

### 6.2 Form Rules
- **Date format**: `dd/MM/yyyy` — standard Vietnamese
- **Account lookup**: Type-ahead with hierarchical tree (111 → 1111, 1112)
- **Required fields**: Red asterisk `*`, red border on validation error
- **Tab order**: Logical left→right, top→bottom, skip auto-filled fields
- **Dirty form guard**: Warn on unsaved changes before navigation

### 6.3 Keyboard Shortcuts
| Key | Action |
|-----|--------|
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save & Add new |
| `F9` | Post (Ghi sổ) |
| `Ctrl+P` | Print |
| `Ctrl+D` | Duplicate voucher |
| `Insert` | Add detail row |
| `Ctrl+Delete` | Delete detail row |
| `F3` | Search/Find |
| `Escape` | Close form / Cancel |
| `Ctrl+F5` | Refresh data |

---

## 7. STATUS & BADGE SYSTEM

| Status | Vietnamese | Color | Style |
|--------|-----------|-------|-------|
| Draft | Nháp | `#757575` gray | Chip outline |
| Pending | Chờ duyệt | `#ED6C02` orange | Chip filled |
| Approved | Đã duyệt | `#1565C0` blue | Chip filled |
| Posted | Đã ghi sổ | `#2E7D32` green | Chip filled |
| Cancelled | Đã hủy | `#D32F2F` red | Chip outline + strikethrough |

---

## 8. ICONS & VISUAL LANGUAGE

- **Primary**: PrimeIcons (bundled with PrimeNG)
- **Supplement**: Material Symbols (only when PrimeIcons lacks)
- **Sizes**: 16px menus/buttons, 20px toolbar, 24px empty states
- **Each module**: Consistent icon (e.g., `pi-wallet` CA, `pi-shopping-cart` PU)
- **No decorative illustrations** in main workspace — professional, dense UI

---

## 9. ACCESSIBILITY (WCAG 2.1 AA)

- Contrast ratio ≥ 4.5:1 text, ≥ 3:1 large text/UI components
- All interactive elements keyboard-focusable
- ARIA labels on icon-only buttons
- Data grids: `role=grid`, `aria-sort`, `aria-selected`
- Focus visible indicator on all interactive elements

---

## 10. INTERNATIONALIZATION

- **Default UI**: Vietnamese (`vi-VN`)
- **Code/API**: English only
- **i18n**: ngx-translate (runtime switching)
- **Supported locales MVP**: `vi-VN`, `en-US`
- **Number/date formatting**: Per-tenant config (NOT hardcoded locale)
- **RTL**: Not required

---

## 11. THEME STRATEGY

- **MVP**: Light theme only (accountants prefer light)
- **Phase 2+**: Dark mode via CSS custom properties toggle
- **Base**: PrimeNG Lara Light Blue, customized
- **ALL colors via CSS variables** — zero hardcoded hex in components

---

## 12. PERFORMANCE TARGETS

| Metric | Target |
|--------|--------|
| First Contentful Paint | < 1.5s |
| Largest Contentful Paint | < 2.5s |
| Time to Interactive | < 3.5s |
| Data grid render (1000 rows) | < 500ms |
| Virtual scroll 10K+ rows | Smooth 60fps |
| Bundle size (initial) | < 300KB gzipped |

---

## 13. TECH STACK

```
Angular 18+        — standalone components, signals, defer blocks
PrimeNG 17+        — Lara theme customized to design system
TailwindCSS 3.4+   — utility classes for layout ONLY (not for design tokens)
SCSS               — CSS custom properties for all design tokens
NgRx Signals       — lightweight signal-based state management
Lazy loading        — per accounting module (DI, GL, CA, BA, PU, SA, IN...)
```
