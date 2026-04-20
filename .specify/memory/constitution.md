# PhanMemKeToan Constitution
> Vietnamese Enterprise Accounting Webapp — Spec-Driven Development

## Core Principles

### I. Design-Before-Code (NON-NEGOTIABLE)
No code is written until the design is reviewed and approved by the project owner. Every feature must pass the **6-Layer MISA Research Process** (Tables → FKs → VoucherTemplate → Posting → SPs → Views) before specification begins. This ensures hidden business rules are discovered, not invented.

### II. Domain-Driven Architecture
The accounting domain is the center of the system. All design decisions are guided by Vietnamese accounting standards (Thông tư 99/2025/TT-BTC, thay thế Thông tư 200), double-entry bookkeeping rules, and the 15-module structure surveyed from MISA AMIS. Domain terminology is Vietnamese in UI, English in code.

### III. Metadata-Driven Configuration
Voucher types, form layouts, posting rules, and permission mappings are stored as configuration data (JSON), not hardcoded logic. This mirrors MISA's SYSRefType + SYSPostMapping + SYSVoucherTemplate architecture but replaces XML/SP with JSON/API. New voucher types should be addable without code changes.


### IV. Posting Engine Correctness (NON-NEGOTIABLE)
The posting engine is the heart of the system. Every detail line produces exactly 2 GeneralLedger entries (Debit + Credit) that must balance. Posting targets 8 ledgers simultaneously (General, Tax, Inventory, Purchase, Sale, FixedAsset, Supply, CustomField). The posting engine must have 100% unit test coverage before any other feature depends on it.

### V. All Business Types Supported
The app serves manufacturing, trading, service, and construction enterprises. All 15 modules (DI, GL, CA, BA, PU, SA, IN, FA, SU, JC, PA, TA, CT, IP/EI, SYS) must be designed for, even if a specific business doesn't use all of them. Empty data ≠ unused feature.

### VI. Incremental Delivery
Build in phases:
- **MVP**: DI + GL + CA + BA + PU + SA + IN + Posting Engine + 5 standard reports (B01-DN through B09-DN) + Workflow Engine + 2 workflow templates (Sales Order, Purchase Order)
- **Phase 2**: FA, JC (Production Order + BOM + Costing), PA, TA, CT, E-Invoice, Import/Export
- **Phase 3**: E-commerce integration, advanced workflow templates (Period Close, Payment Approval, etc.)
- **Phase 4**: MRP (auto production planning), mobile native app, advanced analytics

### VII. Cross-Department Workflow (NON-NEGOTIABLE)
This is NOT just an accounting app — it is a lightweight ERP where multiple departments (Sale, Accounting, Warehouse, Logistics, Procurement, Production, Management) collaborate on shared business processes. The Workflow Engine is the coordination layer that connects all 15 accounting modules across departments. Every business process that spans multiple departments MUST go through the Workflow Engine.

## Domain Constraints

### Accounting Standards
- **Thông tư 99/2025/TT-BTC** (enterprise accounting regime, replaces TT200) is the primary target
- **Thông tư 133** (SMEs) remains in effect — simpler chart of accounts
- Legacy reference: TT200 (large enterprises, AccountingSystem=15) is now superseded by TT99
- Account codes are hierarchical: 1xx=Assets, 2xx=Liabilities, 3xx=Equity, 5xx=Revenue, 6xx=Expenses
- Only leaf accounts (IsParent=0) may appear in journal entries
- Dual-book system: Financial book (DisplayOnBook=0) for tax/auditors, Management book (1) for internal, Both (2)

### Tax System
- VAT supports 2 methods: Khấu trừ (deduction) and Trực tiếp (direct) — different UI, different posting
- 7 tax types possible in a single purchase voucher: VAT, Import, Export, Special Consumption, Environmental, Resource, Others
- E-Invoice (HĐĐT) is mandatory (ND123/ND51) — track PublishStatus 0–4
- Tax law changes annually — design for flexibility

### Decimal Precision
- VND amounts: 0 decimal places
- Foreign currency amounts: 3 decimal places
- Unit prices: 2 decimal places
- Quantities: 2 decimal places
- Exchange rates: 2 decimal places
- Cost allocation: 10 decimal places
- All calculations must use centralized ROUND() based on tenant-level precision config

### Analysis Dimensions
Every journal entry supports 10+ analysis dimensions: AccountObject (Customer/Vendor/Employee), OrganizationUnit, ExpenseItem, BudgetItem, Job, ProjectWork, Order, Contract, DebtAgreement, ListItem + 10 CustomFields. Account.DetailBy* flags control which dimensions are mandatory per account.

## Architecture Decisions

### Pattern: Unified Voucher Model
MISA uses separate tables per RefType (780 tables). The new app consolidates into unified `Voucher` + `VoucherDetail` + `VoucherTax` tables with a `RefType` discriminator column. Trade-off: simpler schema, must watch query performance at scale.

### Pattern: Master-Detail
All vouchers follow Header → Detail lines. Header holds shared info (date, partner, currency). Detail holds line items (account, amount, dimensions). Additional child tables for tax lines, freight, serial numbers as needed.

### Business Logic: Application Layer
MISA puts business logic in 5,410 stored procedures. The new app puts all business logic in the application layer (API services), making it testable, versionable, and database-agnostic.

### Form Layout: JSON Schema
MISA stores UI layout in XML (ntext columns). The new app uses JSON schema for dynamic form rendering. The VoucherTemplate concept (605 templates, 1993 details, 141 column keys, 36 tab types) is preserved as JSON config.

### Search: Materialized Views / Elasticsearch
MISA uses a mega view (308K chars, 86 UNION ALL). The new app uses materialized views for list screens, with optional Elasticsearch for full-text search at scale.

### IDs: Snowflake + GUID
Internal IDs use Snowflake/auto-increment for performance. External-facing IDs use GUIDs for API safety.

### E-commerce Integration: API Pattern (NON-NEGOTIABLE)
The accounting app and any e-commerce storefront **must** use separate databases connected via REST API + Message Queue. Accounting DB is the Single Source of Truth for inventory, pricing, and master data. E-com DB owns cart, sessions, media, SEO. Key rules:
- Stock hold/reserve/release via Accounting API — no direct DB access from e-com
- Idempotent APIs with `X-Idempotency-Key` on all write operations
- Price snapshot locked at order confirmation — anti-tampering validation ±5%
- Daily reconciliation (stock, orders, revenue) between the two systems
- **Full specification**: See `ecom-integration-architecture.md` in this memory folder

### Multi-Tenant Strategy: Dual-DB Dedicated Per-Tenant (Phương án B Kết hợp)
Master DB (always cloud) stores auth, tenant registry, refresh tokens. Tenant DB (one per company — cloud or on-premise via Cloudflare Tunnel) stores all accounting data. Each company = 1 separate database. EF Core global query filters (`ITenantEntity`) still applied within Tenant DB for `User` and `Role` entities as defense-in-depth.

### Connection Management
Tenant DB connections resolved per-request via `ITenantConnectionResolver`. Connection strings for on-premise tenants encrypted with ASP.NET DataProtection API (Phase 1) → KMS (Phase 2). On-premise connections via Cloudflare Tunnel (outbound-only, port 443). Connection cached in-memory with 5-minute TTL.

### Cross-DB Identity
`MasterUser.Id = User.Id` (same GUID in both databases, no FK constraint). Password stored in Master DB only (`MasterUser.PasswordHash`). Creating a user in Tenant DB must create/link corresponding `MasterUser` + `MasterUserTenant` in Master DB.

### Workflow Engine: Config-Driven (NON-NEGOTIABLE)
Workflow definitions stored as JSON config (not hardcode). Each business process = 1 JSON template defining stages, roles, SLA, auto-checks, outcomes, and fork/join logic. Adding a new workflow type requires NO code changes — only a new JSON template + domain event wiring.
- **Stage Model**: WorkflowTemplate → StageTemplate[] → runtime StageInstance per order
- **SLA**: Calculated 24/7 (not business hours)
- **Auto-checks**: Workflow stages can call domain services (IInventoryService, IAccountObjectService, etc.) to auto-validate before human action
- **Change Request Engine (Advanced)**: Full impact analysis + cascade. When an order changes mid-workflow, the system auto-analyzes which stages are affected, what can be auto-updated vs needs human action, and whether change is blocked (e.g., item already on production line). Auto-generates rollback/adjustment tasks for each affected department.
- **Integration Level: Semi-Auto**: Workflow auto-checks (debt, stock, price validation) and auto-creates tasks with pre-filled data. But creating/posting accounting vouchers requires human confirmation — accountants bear legal responsibility for every journal entry.
- **Feedback System**: Bi-directional comments between stages/departments with thread replies, @mentions, read tracking, and response deadlines.
- **MVP Templates**: Sales Order workflow + Purchase Order workflow only. Other workflows (Period Close, Payment Approval, etc.) added in Phase 2+.
- **Notification Chain**: In-app (SignalR real-time) → Email → Escalation if overdue SLA.
- **Full specification**: See `workflow-engine-design.md` in this memory folder

### Production: Level 2 (Production Order + BOM + Costing)
Production module scope = MISA JC module equivalent. Manages Production Orders, Bill of Materials (BOM), material requisition from warehouse, finished goods receipt, and job costing. Does NOT include MRP (auto-planning) in MVP — that is Phase 4. In workflow context, "Production" stage tracks: Planned → In-Production → Completed. Items on production line (In-Production) are LOCKED from change requests.

## Technology Stack

### Backend: ASP.NET Core 8 (C#) + Clean Architecture + CQRS
- **Architecture**: API Layer → Application Layer (MediatR Commands/Queries) → Domain Layer → Infrastructure Layer
- **ORM**: EF Core 8 for CRUD, Dapper for complex reports
- **Domain Events**: MediatR in-memory bus (swap to RabbitMQ/MassTransit when e-com integrates)
- **Outbox Pattern**: Domain events saved to OutboxMessage table in same transaction as entity changes, background job publishes
- **Validation**: FluentValidation for business rules
- **Background Jobs**: Quartz.NET (expired hold cleanup, SLA escalation, recurring tasks, report cache)
- **Logging**: Serilog (structured JSON)
- **PDF/Excel**: QuestPDF + ClosedXML

### Frontend: Angular 18+ (TypeScript) + PrimeNG
- **Dynamic Forms**: Angular Reactive Forms + JSON schema → auto-render voucher forms (605 templates, 141 column keys)
- **Component Library**: PrimeNG (DataTable, TreeTable, TabView, InputNumber, Dialog)
- **Kanban/Workflow UI**: @angular/cdk/drag-drop (custom component — PrimeNG has no Kanban)
- **Real-time**: SignalR for workflow stage updates, notifications
- **Module Structure**: Lazy-loaded feature modules per accounting module (DI, GL, SA, PU, IN, etc.)
- **Mobile**: Responsive web first. Native mobile app in Phase 4.
- **Design System**: Full spec at `.specify/memory/frontend-design-system.md`, enforcement rules at `.github/copilot-instructions.md`

### Frontend Design System (NON-NEGOTIABLE)
> Full reference: `.specify/memory/frontend-design-system.md`

#### Colors — CSS Custom Properties Only
All colors MUST use CSS variables. NEVER hardcode hex in components.
- Primary: `--primary: #1B5E9E`, `--primary-dark: #0D3F6E`, `--primary-light: #E8F0FE`
- Accounting: `--debit: #1565C0` (blue/Nợ), `--credit: #C62828` (red/Có)
- Status: `--posted: #2E7D32`, `--unposted: #F57C00`, `--draft: #757575`
- Alerts: `--success: #2E7D32`, `--warning: #ED6C02`, `--error: #D32F2F`, `--info: #0288D1`
- Surface: `--surface-ground: #F5F5F5`, `--surface-card: #FFFFFF`, `--surface-border: #E0E0E0`
- Text: `--text-primary: #212121`, `--text-secondary: #616161`, `--text-disabled: #9E9E9E`

#### Typography
- Font: `'Inter', 'Roboto', 'Segoe UI', sans-serif` — good Vietnamese diacritical support
- Monospace: `'JetBrains Mono', 'Fira Code', 'Consolas', monospace` — for financial amounts
- Base: 13px. Table cells: 12px. Page titles: 16px.
- All amounts: `font-variant-numeric: tabular-nums; text-align: right; font-family: monospace`

#### Number Formatting — User-Configurable per Tenant (CRITICAL)
- Thousand/decimal separators defined in tenant `NumberFormatConfig` — NEVER hardcode locale
- Centralized `NumberFormatService` reads config and formats all numbers
- Decimal precision per `DecimalPrecisionConfig`: amount(0), foreignAmount(3), unitPrice(2), quantity(2), exchangeRate(2), allocation(10)
- Negative: minus prefix + red. Zero: display `0`. Null: blank.
- Input: accept both `.` and `,`, auto-format on blur

#### Data Grid
- Frozen: RefNo + RefDate pinned left. Virtual scroll for 10K+ rows.
- Column resize/reorder saved to localStorage per user per screen.
- Amount columns: RIGHT-aligned, monospace, tabular-nums. Footer auto-sum BOLD.

#### Voucher Form Keyboard Shortcuts
Ctrl+S=Save, Ctrl+Shift+S=Save&New, F9=Post, Ctrl+P=Print, Ctrl+D=Duplicate, Insert=AddRow, Ctrl+Delete=DeleteRow, F3=Search. Dirty form guard on navigation.

#### Spacing (8px base)
xs=4px, sm=8px, md=16px, lg=24px, xl=32px

#### Accessibility (WCAG 2.1 AA)
Contrast ≥ 4.5:1 text, ≥ 3:1 UI. ARIA labels on icon-only buttons. Data grids: role=grid.

### Database: PostgreSQL 16 (Primary)
- **Why**: Free (no 10GB limit), native `jsonb` for VoucherTemplate/WorkflowTemplate config, native materialized views, built-in full-text search (tsvector)
- **SQL Server Express**: Kept as reference-only for MISA survey data comparison
- **Multi-tenant**: Dual-DB: Master DB (central auth) + Tenant DB (per-company, cloud or on-premise). Dual DbContext: `MasterDbContext` + `ApplicationDbContext` (per-request via `TenantDbContextFactory`)

### Infrastructure
- **Dev**: Docker Compose (PostgreSQL + Redis)
- **Cache**: L1 IMemoryCache (config data) + L2 Redis (report cache, sessions, rate limit)
- **File Storage**: MinIO (self-hosted S3-compatible) for attachments/scans
- **CI/CD**: GitHub Actions
- **Production**: VPS (DigitalOcean/Vultr Singapore) for MVP. Azure/on-premise for enterprise clients.

### Technology Constraints
- Windows development environment (primary dev OS)
- Vietnamese language in UI (UTF-8)
- English in all code, comments, API names, and database columns

### Performance Targets
- Voucher save: < 500ms
- Posting a voucher: < 1s for single, < 30s for batch (1000 vouchers)
- List screen load: < 2s for 10,000 rows with pagination
- Report generation: < 10s for monthly reports

## Development Workflow

### Feature Development Process
1. **Research**: Run 6-Layer MISA Research for the target module/voucher type
2. **Specify**: Use `/speckit.specify` to create feature spec from research findings
3. **Clarify**: Use `/speckit.clarify` to resolve ambiguities
4. **Plan**: Use `/speckit.plan` to create technical implementation plan
5. **Review Plan**: Owner reviews and approves plan before any code
6. **Tasks**: Use `/speckit.tasks` to break down into actionable tasks
7. **Implement (Backend)**: Use `/speckit.implement` to execute `[BE]` tasks (C#/.NET/EF Core)
7b. **Implement (Frontend)**: Use `/speckit.implement.frontend` to execute `[FE]` tasks (Angular/TypeScript/PrimeNG). Runs AFTER backend to ensure API contracts are available. Skip for backend-only features.
8. **Review Code**: Use `/speckit.review` to auto-review against constitution + architecture + design system (7-category check: Architecture, Design System, Number/Date, Security, Business Rules, UX/A11y, Spec Coverage). CRITICAL issues must be fixed before commit.
9. **Commit**: Use `/speckit.git.commit` only after review PASS or owner override
10. **Verify**: Manual UAT against MISA behavior as reference

### Quality Gates
- **Code Review (NON-NEGOTIABLE)**: `/speckit.review` must run after every implement, before commit. See `.github/agents/speckit.review.agent.md` for full 7-category checklist.
- Posting engine: 100% unit test coverage (NON-NEGOTIABLE)
- Business rule validation: Unit tests for every BR-* rule documented in research
- API endpoints: Integration tests for CRUD + edge cases
- No code merges without passing all tests
- Decimal precision tests: Verify rounding matches MISA output for sample data

### Code Standards
- All code in English
- Domain models use Vietnamese accounting terms as English translations (e.g., `Voucher`, `PostingEntry`, `AccountObject`)
- API responses include both `code` and `message` fields for Vietnamese user-facing errors
- Every table must have `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy`, `IsDeleted` (soft delete)

## Reference Data

### Key Survey Files
- `reseach_dabase.md`: Master DB research (1800+ lines, TAG system: [TBL:*], [FK:*], [BR:*], [MOD:*], [FLOW:*])
- `SURVEY_REPORT.md`: Vietnamese summary report (12 sections covering all 15 modules)

### Architecture Files
- `.specify/memory/architecture-technology-report.md`: **Master report** — Full architecture & technology consolidation (18 sections: tech stack, DB design, posting engine, workflow, permissions, search, concurrency, caching, e-com readiness, infrastructure, security, performance targets, MVP scope)
- `.specify/memory/ecom-integration-architecture.md`: Full E-commerce ↔ Accounting integration spec (inventory sync, order lifecycle, master data sync, returns/exchanges, security, monitoring)
- `.specify/memory/ecom-readiness-checklist.md`: Module-by-module checklist of what to design NOW in accounting webapp for future e-com readiness (20 DO-NOW items, 13 DO-LATER items, 8 new tables, 12 service interfaces)
- `.specify/memory/workflow-engine-design.md`: Cross-department Workflow Engine design (stage model, change request engine, feedback system, permission matrix, integration points)

### Key Business Rules Registry
- BR-GL07: Posting engine is config-driven (SYSPostMapping), not hardcoded
- BR-GL08: Each detail line → 2 GeneralLedger rows (Debit + Credit)
- BR-DI01: Account tree — child code starts with parent code (111 → 1111, 1112)
- BR-DI02: Only leaf accounts (IsParent=0) can be used in journal entries
- BR-DI03: DetailBy* flags on Account control dimension tracking
- BR-DI04: AccountObject = unified Customer + Vendor + Employee table
- BR-IN01: 4 costing methods (Weighted Avg, FIFO, Specific, Moving Avg)
- BR-SYS04-06: VoucherTemplate = metadata-driven form builder (141 column keys, 36 tab types)

## Governance

- This constitution supersedes all other development practices for this project
- Amendments require documentation and owner approval
- Any feature that violates posting engine correctness is rejected regardless of deadline
- Tax compliance changes (new Thông tư / Nghị định) take priority over feature work

**Version**: 2.0.0 | **Ratified**: 2026-04-15 | **Last Amended**: 2026-04-15
