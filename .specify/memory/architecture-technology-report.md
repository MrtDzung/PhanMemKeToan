# Architecture & Technology Report
> PhanMemKeToan — Vietnamese Enterprise Accounting Webapp (Lightweight ERP)

**Version**: 1.2.0  
**Date**: 2026-04-15  
**Status**: APPROVED (v1.2.0 — all HIGH + MEDIUM review corrections applied)  
**Referenced by**: constitution.md v2.0.0

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Overview](#2-system-overview)
3. [Technology Stack](#3-technology-stack)
4. [Architecture Design](#4-architecture-design)
5. [Database Design](#5-database-design)
6. [Domain Model — Core Patterns](#6-domain-model--core-patterns)
7. [Posting Engine](#7-posting-engine)
8. [Workflow Engine](#8-workflow-engine)
9. [Permission & Authorization](#9-permission--authorization)
10. [Search & Query Performance](#10-search--query-performance)
11. [Concurrency & Scalability](#11-concurrency--scalability)
12. [Caching Strategy](#12-caching-strategy)
13. [E-Commerce Integration Readiness](#13-e-commerce-integration-readiness)
14. [Infrastructure & Deployment](#14-infrastructure--deployment)
15. [Security Architecture](#15-security-architecture)
16. [Performance Targets](#16-performance-targets)
17. [Fiscal Year, Period Close & Voucher Numbering](#17-fiscal-year-period-close--voucher-numbering)
18. [Localization & Formatting](#18-localization--formatting)
19. [Error Handling & Observability](#19-error-handling--observability)
20. [Testing Strategy](#20-testing-strategy)
21. [Data Migration & Opening Balance](#21-data-migration--opening-balance)
22. [Print Templates & Report Engine](#22-print-templates--report-engine)
23. [MVP Scope & Phased Delivery](#23-mvp-scope--phased-delivery)
24. [Appendix: Key Business Rules](#24-appendix-key-business-rules)

---

## 1. Executive Summary

### 1.1 Vision

Build a Vietnamese enterprise accounting webapp that serves as a **lightweight ERP** — not just an accounting tool. Multiple departments (Sales, Accounting, Warehouse, Logistics, Procurement, Production, Management) collaborate on shared business processes through a **Cross-Department Workflow Engine**.

### 1.2 Key Differentiators from MISA AMIS

| Aspect | MISA AMIS (surveyed) | New Webapp |
|--------|----------------------|------------|
| Business logic | 5,410 stored procedures in DB | Application layer (testable, versionable) |
| Posting engine | Dynamic SQL + Func_POST_* | Domain service + 100% unit tests |
| Form layout | XML in ntext columns | JSON schema + Angular dynamic forms |
| Reports | 4,443 SPs + 86 cache tables | Query builder + Redis cache + materialized views |
| Search | Mega view 308K chars (86 UNION ALL) | Per-module query + PostgreSQL FTS + Elasticsearch (Phase 3) |
| Auth | 19 roles × 50 perms × 414 subsystems | 4-layer RBAC + Data Scope + Field Visibility + Workflow Permission |
| IDs | GUID everywhere | Snowflake/auto-increment + GUID external |
| Audit | Action log only | Full data-level audit trail (before/after diff) |
| Workflow | None (accounting-only) | Config-driven cross-department Workflow Engine |
| Database | SQL Server Express (10GB limit) | PostgreSQL 16 (no limit, native jsonb, FTS) |
| Schema | 780 separate tables | Unified Voucher model with RefType discriminator |

### 1.3 Source of Research

- **MISA AMIS database**: INHONGHA (780 tables, 5,410 SPs, 107 views, 304 functions, 15 modules, ~6GB)
- **Accounting standards**: Thông tư 99/2025/TT-BTC (replaces TT200), Thông tư 133 (SMEs)
- **Business types**: Manufacturing, trading, service, construction

---

## 2. System Overview

### 2.1 High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER (Browser)                        │
│    Angular 18+ │ PrimeNG │ AG Grid Community │ CDK Drag-Drop        │
│    Dynamic Forms (JSON) │ SignalR (Real-time) │ Responsive Web       │
└─────────────────────────────┬────────────────────────────────────────┘
                              │ HTTPS / REST API
┌─────────────────────────────▼────────────────────────────────────────┐
│                        API LAYER (ASP.NET Core 8)                    │
│    Controllers │ Auth (JWT) │ Rate Limiting │ API Versioning         │
│    /api/v1/ (internal) │ /api/v1/integration/ (reserved for e-com)  │
└─────────────────────────────┬────────────────────────────────────────┘
                              │
┌─────────────────────────────▼────────────────────────────────────────┐
│                     APPLICATION LAYER (MediatR CQRS)                 │
│    Commands (write) │ Queries (read) │ Validators (FluentValidation) │
│    Domain Events │ Outbox Pattern │ Workflow Orchestration            │
└────┬────────────────────────┬───────────────────────────┬────────────┘
     │                        │                           │
┌────▼─────┐          ┌──────▼───────┐           ┌───────▼──────┐
│  DOMAIN  │          │INFRASTRUCTURE│           │  BACKGROUND  │
│  LAYER   │          │    LAYER     │           │    JOBS      │
│          │          │              │           │              │
│ Entities │          │ EF Core 8    │           │ Quartz.NET   │
│ Value    │          │ (CRUD)       │           │              │
│ Objects  │          │ Dapper       │           │ - Outbox     │
│ Domain   │          │ (Reports)    │           │   Publisher  │
│ Events   │          │ Redis        │           │ - SLA        │
│ Services │          │ MinIO        │           │   Escalation │
│          │          │ Serilog      │           │ - Hold       │
│          │          │              │           │   Cleanup    │
└──────────┘          └──────┬───────┘           │ - Report     │
                             │                   │   Cache      │
              ┌──────────────▼───────────────┐   └──────────────┘
              │      DATA LAYER              │
              │                              │
              │  PostgreSQL 16 (Primary)     │
              │  Redis (Cache/Session)       │
              │  MinIO (Files/Attachments)   │
              └──────────────────────────────┘
```

### 2.2 Module Structure (15 Modules)

| Code | Module | Vietnamese Name | MVP | Description |
|------|--------|----------------|:---:|-------------|
| DI | Master Data | Danh mục | ✅ | Accounts, AccountObjects (KH/NCC/NV), Inventory Items, Warehouses |
| GL | General Ledger | Sổ cái | ✅ | Journal entries, trial balance, financial statements |
| CA | Cash | Tiền mặt | ✅ | Cash receipts, cash payments |
| BA | Bank | Ngân hàng | ✅ | Bank deposits, bank payments, bank reconciliation |
| PU | Purchase | Mua hàng | ✅ | Purchase orders, purchase invoices (domestic) |
| SA | Sales | Bán hàng | ✅ | Sales orders, sales invoices, returns (domestic) |
| IN | Inventory | Kho | ✅ | Inward, outward, transfer, stock take, costing |
| FA | Fixed Assets | Tài sản cố định | ❌ | Asset management, depreciation |
| JC | Job Costing | Sản xuất | ❌ | Production orders, BOM, costing (Level 2) |
| PA | Payroll | Tiền lương | ❌ | Salary, insurance, PIT |
| TA | Tax | Thuế | ❌ | VAT reports, PIT, CIT |
| CT | Contract | Hợp đồng | ❌ | Contract management |
| IP | E-Invoice | Hóa đơn ĐT | ❌ | E-invoice publishing (ND123/ND51) |
| EI | Export/Import | XNK | ❌ | Import/export invoices, customs |
| SYS | System | Hệ thống | ✅ | Users, roles, permissions, config, audit log |

---

## 3. Technology Stack

### 3.1 Backend

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Runtime | ASP.NET Core | 8.x LTS | Web API framework |
| Architecture | Clean Architecture + CQRS | — | Layer separation, command/query split |
| Mediator | MediatR | latest | In-process message bus, domain events |
| ORM (CRUD) | EF Core | 8.x | Entity mapping, migrations, LINQ queries |
| ORM (Reports) | Dapper | latest | Raw SQL for complex reports, high performance |
| Validation | FluentValidation | latest | Business rule validation |
| Background Jobs | Quartz.NET | latest | Scheduled tasks (outbox, SLA, cleanup, cache) |
| Logging | Serilog | latest | Structured JSON logging |
| PDF Export | QuestPDF | latest | Vietnamese accounting report templates |
| Excel Export | ClosedXML | latest | Excel export for reports |
| API Docs | Swagger / NSwag | latest | OpenAPI spec generation |
| Real-time | SignalR | built-in | Workflow notifications, stage updates |

### 3.2 Frontend

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| Framework | Angular | 18+ | SPA framework, lazy-loaded modules |
| UI Library | PrimeNG | latest | DataTable, TreeTable, TabView, Dialog, InputNumber |
| Grid (Voucher Edit) | AG Grid Community | latest | Inline editing, LookupEdit, TreeLookupEdit columns |
| Kanban UI | @angular/cdk/drag-drop | built-in | Cross-department workflow board |
| Forms | Angular Reactive Forms | built-in | Dynamic form rendering from JSON schema |
| State | NgRx or Signals | latest | State management for complex forms |
| HTTP | HttpClient + Interceptors | built-in | JWT attach, error handling, retry |
| Real-time | @microsoft/signalr | latest | Workflow stage updates, notifications |

**Grid Technology Split:**

| Use Case | Technology | Reason |
|----------|-----------|--------|
| Voucher detail entry (inline edit) | AG Grid Community | Cell editing, tab navigation, LookupEdit popup, formula columns |
| Master data list / Report grid | PrimeNG DataTable | Sorting, filtering, pagination, export, lazy loading |
| Account tree / Category tree | PrimeNG TreeTable | Hierarchical data display |
| Kanban board / Workflow stages | CDK Drag-Drop | Custom cross-department workflow UI |

### 3.3 Database

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Primary DB** | **PostgreSQL** | **16** | All application data |
| Cache | Redis | latest | L2 cache, sessions, rate limiting, pub/sub |
| File Storage | MinIO | latest | S3-compatible, attachments, scanned documents |
| Reference Only | SQL Server Express | — | MISA survey data comparison (not used in production) |

**Why PostgreSQL 16:**

| Feature | Benefit for Accounting Webapp |
|---------|-------------------------------|
| No storage limit | MISA SQL Server Express limited to 10GB |
| Native `jsonb` | Store VoucherTemplate, WorkflowTemplate, SearchConfig as queryable JSON |
| Materialized Views | Replace MISA's 86-UNION-ALL mega view with per-module materialized views |
| Built-in FTS (`tsvector`) | Full-text search on Vietnamese text without external service |
| Row-Level Security (RLS) | Potential additional layer for multi-tenant isolation |
| `LISTEN/NOTIFY` | Real-time cache invalidation triggers |
| Table Partitioning | Partition GeneralLedger by year for long-term performance |
| `pg_trgm` extension | Fuzzy search for account names, customer names |
| Free & open source | No licensing cost at any scale |

### 3.4 Infrastructure

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Container | Docker Compose | Local dev: PostgreSQL + Redis + MinIO |
| CI/CD | GitHub Actions | Build, test, deploy pipeline |
| Dev OS | Windows | Primary development environment |
| Production (MVP) | VPS (DigitalOcean/Vultr Singapore) | Low latency for Vietnam users |
| Production (Enterprise) | Azure / On-premise | For large enterprise clients |

---

## 4. Architecture Design

### 4.1 Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│           Presentation (API)            │  ← Controllers, DTOs, Filters
├─────────────────────────────────────────┤
│         Application (Use Cases)         │  ← Commands, Queries, Validators, DTOs
├─────────────────────────────────────────┤
│              Domain (Core)              │  ← Entities, Value Objects, Events, Interfaces
├─────────────────────────────────────────┤
│        Infrastructure (I/O)             │  ← EF Core, Dapper, Redis, MinIO, Email
└─────────────────────────────────────────┘
```

**Dependency Rule**: Inner layers NEVER reference outer layers. Domain has ZERO external dependencies.

### 4.2 CQRS Pattern

```
Commands (Write)                          Queries (Read)
────────────────                          ───────────────
CreateVoucherCommand                      GetVoucherByIdQuery
  → Validator                               → Dapper (simple)
  → Handler                                 → or EF Core (with includes)
  → Domain logic                             → DTO mapping
  → EF Core save                             → Redis cache check
  → Domain events                            → Return DTO
  → Outbox persist
```

| Aspect | Command Side | Query Side |
|--------|-------------|------------|
| ORM | EF Core (change tracking) | Dapper (raw SQL, performance) |
| Validation | FluentValidation (full) | Parameter validation only |
| Cache | Invalidate on write | Check cache first |
| Return | ID / success | DTO / list |

### 4.3 Outbox Pattern (Domain Events)

**Domain Event Capture:**
```csharp
// Base entity class
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Usage in entity:
public void Post(Guid postedBy)
{
    Status = VoucherStatus.Posted;
    PostedDate = DateTimeOffset.UtcNow;
    AddDomainEvent(new VoucherPostedEvent(Id, RefType, TotalAmount));
}

// In DbContext.SaveChangesAsync() override:
//   1. Collect all DomainEvents from tracked entities
//   2. For each event → create OutboxMessage { EventType, Payload = JsonSerializer.Serialize(event) }
//   3. Add OutboxMessage to same transaction
//   4. ClearDomainEvents() on each entity
//   5. Commit transaction (entities + outbox messages atomically)
```

```
┌─────────────────────────────────────────┐
│          Application Service            │
│                                         │
│  1. Validate business rules             │
│  2. Modify domain entities              │
│  3. entity.AddDomainEvent(new ...)      │
│  4. _dbContext.SaveChanges()            │
│     ↓ (same transaction)               │
│     4a. Save entity changes             │
│     4b. Save OutboxMessage records      │
│  5. Return success                      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│      Outbox Publisher (Quartz.NET)       │
│                                         │
│  Poll OutboxMessage WHERE Processed=0   │
│  → Publish via MediatR (in-memory)      │
│  → Mark Processed = true                │
│  Phase 3: → Publish to RabbitMQ         │
└─────────────────────────────────────────┘
```

**Outbox Retry Policy:**
- Quartz.NET job runs every **5 seconds** (configurable)
- On failure: exponential backoff — retry after 5s, 15s, 45s, 135s, 405s (5 retries)
- After 5 failed retries: move to **dead-letter** state (`RetryCount >= 5 AND Error IS NOT NULL`)
- Dead-letter messages: logged to Serilog as `Error`, alert via email to Admin
- Manual retry: Admin can reset `RetryCount = 0, ProcessedAt = NULL` to re-process
- **Idempotency:** Event handlers MUST be idempotent (use `EventId` for dedup)

**OutboxMessage Table:**

| Column | Type | Purpose |
|--------|------|---------|
| Id | bigint PK | Auto-increment |
| EventType | varchar(200) | Full type name of domain event |
| Payload | jsonb | Serialized event data |
| OccurredAt | timestamptz | When event was raised |
| ProcessedAt | timestamptz | When published (null = pending) |
| RetryCount | int | Retry attempts |
| Error | text | Last error message |

### 4.4 Project Structure

```
src/
├── PhanMemKeToan.Domain/              # Entities, Value Objects, Events, Interfaces
│   ├── Entities/
│   │   ├── Voucher.cs
│   │   ├── VoucherDetail.cs
│   │   ├── GeneralLedger.cs
│   │   ├── Account.cs
│   │   ├── AccountObject.cs
│   │   ├── InventoryItem.cs
│   │   └── ...
│   ├── Events/
│   │   ├── VoucherPostedEvent.cs
│   │   ├── StockChangedEvent.cs
│   │   └── ...
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   ├── AccountCode.cs
│   │   └── ...
│   └── Interfaces/
│       ├── IInventoryService.cs
│       ├── IPostingEngine.cs
│       └── ...
│
├── PhanMemKeToan.Application/         # Use Cases (Commands/Queries)
│   ├── Vouchers/
│   │   ├── Commands/
│   │   │   ├── CreateVoucherCommand.cs
│   │   │   ├── PostVoucherCommand.cs
│   │   │   └── ...
│   │   └── Queries/
│   │       ├── GetVoucherByIdQuery.cs
│   │       ├── GetVoucherListQuery.cs
│   │       └── ...
│   ├── Workflow/
│   ├── Reports/
│   └── ...
│
├── PhanMemKeToan.Infrastructure/      # Data Access, External Services
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/           # EF Core Fluent API configs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Services/
│   │   ├── PostingEngine.cs
│   │   ├── InventoryService.cs
│   │   └── ...
│   ├── Redis/
│   └── MinIO/
│
├── PhanMemKeToan.API/                 # Web API Host
│   ├── Controllers/
│   ├── Middleware/
│   ├── Filters/
│   └── Program.cs
│
└── PhanMemKeToan.Web/                 # Angular Frontend
    ├── src/app/
    │   ├── core/                      # Auth, guards, interceptors
    │   ├── shared/                    # Shared components, pipes
    │   ├── features/
    │   │   ├── master-data/           # DI module
    │   │   ├── general-ledger/        # GL module
    │   │   ├── sales/                 # SA module
    │   │   ├── purchase/              # PU module
    │   │   ├── inventory/             # IN module
    │   │   ├── cash/                  # CA module
    │   │   ├── bank/                  # BA module
    │   │   ├── workflow/              # Workflow engine UI
    │   │   └── system/                # SYS module
    │   └── app.routes.ts
    └── ...
```

---

## 5. Database Design

### 5.1 Multi-Tenant Strategy

**Pattern**: Shared Database + TenantId Column

```sql
-- Every table has TenantId
CREATE TABLE voucher (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    ref_type        smallint NOT NULL,
    ...
    CONSTRAINT fk_voucher_tenant FOREIGN KEY (tenant_id) REFERENCES tenant(id)
);

-- EF Core Global Query Filter (auto-applied)
-- modelBuilder.Entity<Voucher>().HasQueryFilter(v => v.TenantId == _currentTenant.Id);
```

**Isolation**: EF Core Global Query Filters auto-apply `WHERE tenant_id = @tenantId` on every query. All write operations set `TenantId` from the authenticated context.

**Migration path**: If a large enterprise client requires physical isolation → migrate to schema-per-tenant (PostgreSQL natively supports this).

### 5.2 ID Strategy

| Scope | ID Type | Format | Use Case |
|-------|---------|--------|----------|
| Internal PK | bigint IDENTITY | Auto-increment | All table primary keys (fast, compact, sorted) |
| External API | UUID v7 | `uuid_generate_v7()` | API response IDs, webhook payloads, external refs |
| Voucher Number | varchar(20) | `{prefix}-{YYYYMMDD}-{seq}` | Human-readable: BH-20260415-0001 |

**UUID v7** is time-ordered (unlike v4), so it maintains index locality while being globally unique.

### 5.3 Core Tables — Unified Voucher Model

```
┌──────────────┐     ┌──────────────────┐     ┌────────────────┐
│   voucher    │────▶│  voucher_detail  │     │ voucher_tax    │
│              │     │                  │     │                │
│ id (PK)      │     │ id (PK)          │     │ id (PK)        │
│ tenant_id    │     │ voucher_id (FK)  │     │ voucher_id (FK)│
│ ref_type     │     │ sort_order       │     │ tax_type       │
│ ref_no       │     │ description      │     │ tax_rate       │
│ ref_date     │     │ debit_account    │     │ tax_amount     │
│ posted_date  │     │ credit_account   │     └────────────────┘
│ is_posted    │     │ amount           │
│ account_obj  │     │ amount_oc        │     ┌────────────────┐
│ currency     │     │ quantity         │     │ general_ledger │
│ exchange_rate│     │ unit_price       │     │                │
│ total_amount │     │ account_object   │     │ Posting output │
│ description  │     │ org_unit         │     │ 2 entries per  │
│ source       │     │ expense_item     │     │ detail line    │
│ external_ref │     │ job              │     │ 8 ledger types │
│ workflow_id  │     │ ...10+ dims      │     └────────────────┘
│ row_version  │     └──────────────────┘
│ ...          │
└──────────────┘
```

**RefType enum** (262 types from MISA survey — key examples):

| RefType | Description | Module |
|---------|-------------|--------|
| 1xx | Cash receipts / payments | CA |
| 2xx | Bank deposits / payments | BA |
| 3xx | Purchase invoices / orders | PU |
| 4xx | Sales invoices / orders | SA |
| 5xx | Inventory in / out / transfer | IN |
| 6xx | Fixed asset operations | FA |
| 7xx | Journal entries | GL |
| 8xx | Production orders | JC |
| 9xx | Payroll vouchers | PA |

**MVP RefTypes (Phase 1 — must support):**

| RefType | Code | Vietnamese Name | Module |
|---------|------|----------------|--------|
| 101 | PT | Phiếu thu tiền mặt | CA |
| 102 | PC | Phiếu chi tiền mặt | CA |
| 201 | BC | Giấy báo Có (Nạp tiền ngân hàng) | BA |
| 202 | BN | Giấy báo Nợ (Thanh toán ngân hàng) | BA |
| 301 | PO | Đơn mua hàng | PU |
| 302 | PI | Hóa đơn mua hàng | PU |
| 303 | PR | Trả lại hàng mua | PU |
| 401 | SO | Đơn bán hàng | SA |
| 402 | SI | Hóa đơn bán hàng | SA |
| 403 | SR | Trả lại hàng bán | SA |
| 501 | NK | Phiếu nhập kho | IN |
| 502 | XK | Phiếu xuất kho | IN |
| 503 | CK | Phiếu chuyển kho | IN |
| 701 | PC_GL | Bút toán tổng hợp (General JE) | GL |
| 702 | KC | Bút toán kết chuyển cuối kỳ | GL |
| 703 | FX_REVAL | Bút toán đánh giá lại tỷ giá | GL |
| 704 | OB | Bút toán số dư đầu kỳ | GL |

> **Total MVP RefTypes: 17.** Phase 2 adds FA (6xx), Phase 3 adds JC (8xx) + PA (9xx). Full 262 types loaded from MISA survey data for reference.

### 5.4 Audit Fields (Every Table)

```sql
-- Standard audit columns on EVERY table
created_date    timestamptz NOT NULL DEFAULT now(),
created_by      uuid NOT NULL,
modified_date   timestamptz,
modified_by     uuid,
is_deleted      boolean NOT NULL DEFAULT false   -- Soft delete
```

**Soft Delete Policy:**
| Category | Delete Type | Reason |
|----------|------------|--------|
| Financial data (voucher, voucher_detail, general_ledger) | Soft delete ONLY | Audit trail, legal requirement, TT99 Article 8 |
| Master data (account, inventory_item, account_object) | Soft delete | May be referenced by historical vouchers |
| Lookup data (unit, warehouse, department) | Soft delete (default) + Admin hard-delete if no FK references | Prevent orphan references |
| System config (sys_ref_type, sys_post_mapping, voucher_template) | Soft delete | Rollback capability |
| Transient data (outbox_message processed > 30 days, audit_log > retention period) | Hard delete via scheduled job | Storage management |
| User sessions, rate limit counters | Hard delete | No audit value, TTL-based cleanup |

> **Rule:** Any table with `is_deleted` column uses soft delete. EF Core Global Query Filter `WHERE is_deleted = false` applied automatically. Dapper queries MUST include this filter explicitly via `IDapperContext`.

### 5.5 Table Partitioning Strategy

```sql
-- GeneralLedger partitioned by year (high-volume table)
CREATE TABLE general_ledger (
    id              bigint GENERATED ALWAYS AS IDENTITY,
    tenant_id       uuid NOT NULL,
    posted_date     date NOT NULL,
    ...
) PARTITION BY RANGE (posted_date);

CREATE TABLE general_ledger_2025 PARTITION OF general_ledger
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');
CREATE TABLE general_ledger_2026 PARTITION OF general_ledger
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
-- Auto-create next year's partition via Quartz.NET job
```

Partitioning candidates: `general_ledger`, `audit_log`, `outbox_message`, `workflow_stage_instance`

---

## 6. Domain Model — Core Patterns

### 6.1 Unified Voucher Pattern

MISA: 780 separate tables (one per RefType). New app: unified model with RefType discriminator.

```
Voucher (header)
  └── VoucherDetail[] (line items — 10+ dimension FKs)
  └── VoucherTax[] (tax lines)
  └── VoucherReference[] (cross-voucher links — traceability chain)
  └── VoucherAttachment[] (scanned docs, files)
```

**VoucherReference (Traceability Chain):**
MISA has 369,131 rows in `VoucherReference` — links PO → GRN → Purchase Invoice → Payment.
```sql
CREATE TABLE voucher_reference (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    src_voucher_id  bigint NOT NULL,   -- FK → voucher (source)
    src_ref_type    smallint NOT NULL,
    dest_voucher_id bigint NOT NULL,   -- FK → voucher (destination)
    dest_ref_type   smallint NOT NULL,
    relation_type   varchar(30) NOT NULL,  -- 'ORDER_TO_RECEIPT', 'RECEIPT_TO_INVOICE', 'INVOICE_TO_PAYMENT'
    created_at      timestamptz DEFAULT now()
);
CREATE INDEX ix_vref_src ON voucher_reference (tenant_id, src_voucher_id);
CREATE INDEX ix_vref_dest ON voucher_reference (tenant_id, dest_voucher_id);
```
**Use cases:** audit trail, navigate related documents, prevent orphan payments, track order fulfillment status.

**Trade-offs:**

| Aspect | Pros | Cons | Mitigation |
|--------|------|------|------------|
| Schema simplicity | 1 table vs 780 | Wide table | Use jsonb for rare fields |
| Query performance | Single table scan | Large table with mixed types | (tenant_id, ref_type, ref_date) composite index + partitioning |
| Code DRY | Shared CRUD logic | Need per-type validation | FluentValidation per RefType |
| Migration | Single migration | All types affected | Feature flags per RefType |

### 6.2 Master Data (DI Module)

| Entity | MISA Rows | Key Fields |
|--------|-----------|------------|
| Account | 269 | Code (hierarchical 111→1111), IsParent, DetailBy* flags |
| AccountObject | 23,708 | Unified Customer + Vendor + Employee (Type discriminator) |
| InventoryItem | 78,491 | Code, SKU, Barcode, UnitConvert[], CostingMethod |
| Stock (Warehouse) | 52 | Code, Name, Address, IsEcomFulfillment |
| OrganizationUnit | 50 | Department tree structure |
| ExpenseItem | 100+ | Expense categories |
| BudgetItem | 50+ | Budget line items |
| Currency | 10+ | ExchangeRate per date |

### 6.3 Metadata-Driven Configuration

```
SYSRefType (262 types)
  └── What tables, validations, posting rules per voucher type

SYSPostMapping (80+ rules)
  └── Source → Target ledger mapping, posting function selection

VoucherTemplate (605 templates, 1993 details)
  └── JSON schema: which columns visible, order, width, tab layout
  └── 141 column keys, 36 tab types

SearchConfig
  └── Per-module search field definitions
```

All stored as **jsonb** in PostgreSQL, loaded at startup, cached in IMemoryCache.

**VoucherTemplate JSON Schema Example:**
```jsonc
{
  "refType": 301,                    // SA Invoice
  "description": "Hóa đơn bán hàng",
  "tabs": [
    {
      "code": "DETAIL",
      "label": "Hàng tiền",
      "sortOrder": 1,
      "columns": [
        { "fieldCode": "InventoryItemCode", "label": "Mã hàng", "width": 120, "visible": true, "editable": true, "required": true },
        { "fieldCode": "InventoryItemName", "label": "Tên hàng", "width": 250, "visible": true, "editable": false },
        { "fieldCode": "UnitName", "label": "ĐVT", "width": 80, "visible": true, "editable": true },
        { "fieldCode": "Quantity", "label": "Số lượng", "width": 100, "visible": true, "editable": true, "required": true, "dataType": "decimal" },
        { "fieldCode": "UnitPrice", "label": "Đơn giá", "width": 120, "visible": true, "editable": true, "dataType": "decimal" },
        { "fieldCode": "Amount", "label": "Thành tiền", "width": 130, "visible": true, "editable": false, "formula": "Quantity * UnitPrice" },
        { "fieldCode": "VATRate", "label": "% thuế GTGT", "width": 80, "visible": true, "editable": true },
        { "fieldCode": "VATAmount", "label": "Tiền thuế", "width": 120, "visible": true, "editable": false, "formula": "Amount * VATRate / 100" },
        { "fieldCode": "WarehouseCode", "label": "Mã kho", "width": 100, "visible": true, "editable": true }
      ]
    },
    {
      "code": "TAX",
      "label": "Thuế",
      "sortOrder": 2,
      "columns": [
        { "fieldCode": "VATRate", "label": "% thuế", "width": 80, "visible": true },
        { "fieldCode": "VATAccount", "label": "TK thuế", "width": 100, "visible": true },
        { "fieldCode": "InvoiceNo", "label": "Số hóa đơn", "width": 120, "visible": true },
        { "fieldCode": "InvoiceDate", "label": "Ngày HĐ", "width": 100, "visible": true }
      ]
    }
  ],
  "rules": {
    "autoCalcTotal": true,
    "totalFields": ["Amount", "VATAmount", "TotalAmount"],
    "debitAccountDefault": "131",     // Receivable
    "creditAccountDefault": "511"     // Revenue
  }
}
```

> **Note:** `fieldCode` maps to VoucherDetail column names. Angular reads this JSON to dynamically render AG Grid columns.

### 6.4 Analysis Dimensions (10+ per Journal Entry)

Every `GeneralLedger` entry supports:

| Dimension | FK Target | Controlled By |
|-----------|-----------|---------------|
| AccountObject | AccountObject (KH/NCC/NV) | Account.DetailByAccountObject |
| OrganizationUnit | OrganizationUnit (Phòng ban) | Account.DetailByOrganizationUnit |
| ExpenseItem | ExpenseItem (Khoản mục CP) | Account.DetailByExpenseItem |
| BudgetItem | BudgetItem (Mục ngân sách) | Account.DetailByBudgetItem |
| Job | Job (Công trình) | Account.DetailByJob |
| ProjectWork | ProjectWork (Dự án) | Account.DetailByProjectWork |
| Order | Order (Đơn hàng) | Account.DetailByOrder |
| Contract | Contract (Hợp đồng) | Account.DetailByContract |
| DebtAgreement | DebtAgreement (Khế ước) | Custom config |
| ListItem | ListItem (DM tùy chọn) | Account.DetailByListItem |
| CustomField1..10 | Custom | Tenant-level config |

---

## 7. Posting Engine

### 7.1 Architecture

```
VoucherDetail (input)
       │
       ▼
┌──────────────────────────┐
│    IPostingEngine        │
│                          │
│  1. Load PostMapping     │   ← Config-driven (SYSPostMapping JSON)
│     for this RefType     │
│  2. For each detail:     │
│     a. Resolve accounts  │
│     b. Calculate amounts │   ← ROUND() per tenant precision config
│     c. Generate 2 GL     │   ← Debit + Credit (ALWAYS balanced)
│        entries           │
│  3. Post to 8 ledgers    │   ← Simultaneous
│  4. Update balances      │   ← InventoryBalance, AccountBalance
│  5. Emit domain events   │   ← VoucherPostedEvent, StockChangedEvent
└──────────────────────────┘
       │
       ▼
GeneralLedger (2 rows per detail line)
  + TaxLedger
  + InventoryLedger
  + PurchaseLedger
  + SaleLedger
  + FixedAssetLedger
  + SupplyLedger
  + CustomFieldLedger
```

### 7.2 Target Ledger Registry (8 Ledgers)

| # | Ledger | Table | Purpose | Source Modules |
|---|--------|-------|---------|----------------|
| 1 | **GeneralLedger** | `general_ledger` | Main double-entry journal (ALL entries) | ALL modules |
| 2 | **TaxLedger** | `tax_ledger` | VAT input/output tracking | CA, BA, GL, PU, SA |
| 3 | **InventoryLedger** | `inventory_ledger` | Stock movement & valuation | IN, PU |
| 4 | **PurchaseLedger** | `purchase_ledger` | Purchase tracking per supplier | PU |
| 5 | **SaleLedger** | `sale_ledger` | Sales tracking per customer | SA |
| 6 | **FixedAssetLedger** | `fixed_asset_ledger` | Asset value & depreciation | FA |
| 7 | **SupplyLedger** | `supply_ledger` | Tool/supply tracking (CCDC) | SU |
| 8 | **CustomFieldLedger** | `custom_field_ledger` | User-defined fields preservation | ALL modules |

### 7.3 Posting Rules (NON-NEGOTIABLE)

1. **Double-entry**: Every detail line → exactly 2 GeneralLedger entries (Debit + Credit)
2. **Balance check**: SUM(Debit) = SUM(Credit) per voucher (zero-tolerance)
3. **Config-driven**: Posting targets determined by SYSPostMapping JSON, not hardcoded
4. **Dual-book**: Financial book (DisplayOnBook=0) for auditors/tax + Management book (1) for internal + Both (2)
5. **Cascade**: After posting, recalculate InventoryBalance weighted average if costing method = weighted avg
6. **Idempotent**: Posting same voucher twice = no effect (check IsPosted flag)
7. **Unpost**: Reverse posting = delete GL entries + recalculate balances
8. **100% unit test coverage** before any feature depends on it

### 7.4 Decimal Precision

| Measurement | Column Type | Decimal Places | Example |
|-------------|-------------|:--------------:|---------|
| VND amounts | `NUMERIC(19,0)` | 0 | 1,500,000 đ |
| Foreign currency amounts | `NUMERIC(19,3)` | 3 | 150.500 USD |
| Unit prices | `NUMERIC(19,2)` | 2 | 25,000.50 |
| Foreign currency prices | `NUMERIC(19,4)` | 4 | 0.0394 |
| Quantities | `NUMERIC(19,2)` | 2 | 10.50 |
| Exchange rates | `NUMERIC(19,2)` | 2 | 25,450.00 |
| Cost allocation ratios | `NUMERIC(19,10)` | 10 | 0.0000012345 |
| Coefficient | `NUMERIC(19,2)` | 2 | 1.50 |

All calculations use centralized `ROUND()` based on tenant-level precision config stored in `sys_db_option`.

**Rounding Timing (NON-NEGOTIABLE):**
1. Input: Raw values from UI (no rounding)
2. Intermediate calculations: Use **full precision** (NUMERIC(19,10) for allocation)
3. Final GL entry write: `ROUND(amount, tenant.AmountDecimalDigits)` — applied **at the moment of writing each GL line**
4. Balance check: `SUM(DebitAmount) = SUM(CreditAmount)` — checked **after rounding all lines**
5. If rounding causes imbalance (<= 1 unit of smallest decimal): adjust last line to force balance
6. Never use FLOAT/DOUBLE — always NUMERIC for monetary values

### 7.5 SYSPostMapping — Config Schema (CRITICAL)

```json
// sys_post_mapping — stored as jsonb, loaded into IMemoryCache at startup
{
  "refType": 1020,                          // CA Payment
  "sourceMasterTable": "voucher",
  "sourceDetailTable": "voucher_detail",
  "targets": [
    {
      "ledger": "general_ledger",
      "postingService": "GeneralLedgerPostingService",
      "enabled": true,
      "condition": null                     // Always post to GL
    },
    {
      "ledger": "tax_ledger",
      "postingService": "TaxLedgerPostingService",
      "enabled": true,
      "condition": "detail.VATAmount != 0"  // Only if has tax entry
    },
    {
      "ledger": "custom_field_ledger",
      "postingService": "CustomFieldLedgerPostingService",
      "enabled": true,
      "condition": "detail.HasCustomFields"
    }
  ]
}
```

**MVP mapping per module (from MISA SYSPostMapping 80+ rows):**

| RefType Range | Module | GL | Tax | Inventory | Purchase | Sale | FA | Supply | Custom |
|---------------|--------|:--:|:---:|:---------:|:--------:|:----:|:--:|:------:|:------:|
| 1010-1026 | CA (Cash) | ✅ | ✅ | — | — | — | — | — | ✅ |
| 1500-1560 | BA (Bank) | ✅ | ✅ | — | — | — | — | — | ✅ |
| 301-378 | PU (Purchase) | ✅ | ✅ | ✅ | ✅ | — | — | — | ✅ |
| 3400-3571 | SA (Sales) | ✅ | ✅ | — | — | ✅ | — | — | ✅ |
| 2010-2094 | IN (Inventory) | ✅ | — | ✅ | — | — | — | — | ✅ |
| 4010-4092 | GL (General) | ✅ | ✅ | — | — | — | — | — | ✅ |
| 251-256 | FA (Fixed Assets) | ✅ | — | — | — | — | ✅ | — | ✅ |
| 412-454 | SU (Supplies) | ✅ | — | — | — | — | — | ✅ | ✅ |

### 7.6 Posting Idempotency

```
Post(voucherId):
  1. BEGIN TRANSACTION
  2. SELECT is_posted FROM voucher WHERE id = @id FOR UPDATE NOWAIT
  3. IF is_posted = true → RETURN {already_posted: true} (idempotent)
  4. Execute posting pipeline (validate → generate GL → write ledgers)
  5. UPDATE voucher SET is_posted = true, posted_date = now()
  6. COMMIT
  
On failure at step 4:
  → ROLLBACK (transaction-level atomicity)
  → No partial GL entries persisted
  → Client retries safely (step 3 will fail until lock released)
```

### 7.7 Dapper Multi-Tenant Safety (NON-NEGOTIABLE)

All Dapper queries MUST include `tenant_id` filter. EF Core uses Global Query Filter automatically, but Dapper does **NOT**.

```csharp
// ❌ FORBIDDEN — data leak across tenants
var sql = "SELECT * FROM general_ledger WHERE posted_date > @date";

// ✅ REQUIRED — always filter by tenant
var sql = "SELECT * FROM general_ledger WHERE tenant_id = @tenantId AND posted_date > @date";

// Enforcement: DapperQueryValidator (code review + static analysis rule)
// All Dapper .Query() calls pass through IDapperContext which auto-injects tenant_id
```

---

## 8. Workflow Engine

### 8.1 Design Principles

| Principle | Description |
|-----------|-------------|
| Config-Driven | JSON templates, no code changes for new workflows |
| Cross-Department | Sales → Accounting → Warehouse → Logistics → Procurement → Production |
| Semi-Auto Integration | Auto-check + auto-generate tasks; human confirms voucher posting |
| Advanced Change Request | Full impact analysis + cascade when orders change mid-workflow |
| 24/7 SLA | Calculated continuously, not business-hours only |

### 8.2 Data Model

```
WorkflowTemplate (JSON definition)
  └── StageTemplate[] (ordered stages)
       └── Allowed roles
       └── Auto-check rules (domain service calls)
       └── SLA hours
       └── Outcomes (Approve/Reject/RequestChange)
       └── Fork/Join logic

WorkflowInstance (runtime per order)
  └── StageInstance[] (one per stage)
       └── AssignedTo (user)
       └── Status (Pending/InProgress/Completed/Skipped)
       └── StartedAt / CompletedAt
       └── SLADeadline
       └── Comments / Feedback thread

ChangeRequest (mid-workflow changes)
  └── ImpactAnalysis[] (auto-generated)
       └── Affected stages
       └── Auto-updatable vs needs human action
       └── Blocked items (e.g., already in production)
```

### 8.3 MVP Workflow Templates

**Template 1: Sales Order (Đơn bán hàng)**

```
Sale creates order → Accountant checks debt/price → Warehouse checks stock →
Logistics assigns shipping → [Optional: Procurement if out of stock] →
[Optional: Production if manufacturing] → Warehouse ships → Accountant posts
```

**Template 2: Purchase Order (Đơn mua hàng)**

```
Procurement creates PO → Manager approves (if > threshold) →
Accountant checks budget → Supplier delivers → Warehouse receives →
Quality check → Accountant posts purchase invoice
```

### 8.4 Notification Chain

```
Stage assigned → In-app notification (SignalR, real-time)
SLA warning (80% elapsed) → In-app + Email
SLA overdue → Escalate to manager + Email
Escalation ignored (24h) → Escalate to next level
```

---

## 9. Permission & Authorization

### 9.1 MISA Current State (Survey Findings)

| Table | Rows | Purpose |
|-------|-----:|---------|
| MSC_User | 25 | User accounts |
| MSC_Role | 19 | Role definitions |
| MSC_Permission | 50 | Permission types (View, Add, Edit, Delete, Print, Import, Export...) |
| MSC_SubSystem | 414 | Screens/entities (target resources) |
| MSC_RolePermissionMaping | 10,210 | Role × Permission × SubSystem matrix |
| MSC_UserJoinRole | 63 | User → Role assignments |
| MSC_AudittingLog | 117,151 | Action audit trail |

**MISA gaps**: No data-level scoping (All/Own/Dept), no field visibility control, no workflow-stage permissions, no API-level auth, no data-diff audit.

### 9.2 New App: 4-Layer Permission Model

```
Layer 1: RBAC (Role-Based Access Control)
  └── WHO can do WHAT on WHICH resource
  └── Role × Permission × Resource matrix

Layer 2: Data Scope
  └── HOW MUCH data can they see
  └── All / OwnOnly / Department / Branch / Custom

Layer 3: Field Visibility
  └── WHICH columns can they see/edit
  └── Hide / Show / ReadOnly per field per role

Layer 4: Workflow Permission
  └── WHICH workflow actions can they perform
  └── Per-stage action authorization
```

### 9.3 Layer 1: RBAC

```sql
-- Core permission tables
CREATE TABLE role (
    id          bigint PRIMARY KEY,
    tenant_id   uuid NOT NULL,
    name        varchar(100) NOT NULL,
    description text
);

CREATE TABLE permission (
    id      smallint PRIMARY KEY,
    code    varchar(50) NOT NULL UNIQUE,  -- 'View', 'Add', 'Edit', 'Delete', 'Post', 'Unpost', 'Print', 'Export', 'Import'
    name    varchar(100)
);

CREATE TABLE resource (
    id      smallint PRIMARY KEY,
    code    varchar(100) NOT NULL UNIQUE, -- 'SA.Order', 'PU.Invoice', 'GL.JournalEntry', 'DI.Account'
    module  varchar(10)                   -- 'SA', 'PU', 'GL', 'DI', 'IN', etc.
);

CREATE TABLE role_permission (
    role_id       bigint REFERENCES role(id),
    permission_id smallint REFERENCES permission(id),
    resource_id   smallint REFERENCES resource(id),
    data_scope    varchar(20) DEFAULT 'All',  -- Layer 2
    PRIMARY KEY (role_id, permission_id, resource_id)
);
```

### 9.4 Layer 2: Data Scope

| Scope | Description | SQL Filter |
|-------|-------------|------------|
| All | See/edit all tenant data | `WHERE tenant_id = @tid` |
| OwnOnly | Only records created by self | `WHERE created_by = @uid` |
| Department | Only records from same department | `WHERE org_unit_id = @deptId` |
| Branch | Only records from same branch | `WHERE branch_id = @branchId` |
| Custom | Custom filter expression (jsonb) | Dynamic WHERE clause |

**Data Scope Injection (NON-NEGOTIABLE):**
```csharp
// EF Core: IQueryable interceptor applies data scope automatically
public class DataScopeInterceptor : IQueryExpressionInterceptor
{
    // On every IQueryable<T> where T : ITenantEntity
    // 1. Read user's DataScope for current resource from permission matrix
    // 2. Append WHERE clause: OwnOnly → created_by = userId, Department → org_unit_id IN (userDepts), etc.
    // 3. Applied BEFORE any .Where()/.Select() — cannot be bypassed by application code
}

// Dapper: Must use IDapperContext.QueryAsync<T>() which auto-appends:
//   AND tenant_id = @tenantId
//   AND {data_scope_filter} based on current user's scope
// Direct IDbConnection usage is BANNED.
```
> **Security:** Data scope is enforced at repository/query level, not at controller level. Even internal service calls go through the same filter.

### 9.5 Layer 3: Field Visibility

```sql
CREATE TABLE role_field_visibility (
    role_id     bigint REFERENCES role(id),
    resource_id smallint REFERENCES resource(id),
    field_code  varchar(100),              -- 'UnitPrice', 'CostPrice', 'Margin'
    visibility  varchar(10) DEFAULT 'Show', -- 'Show', 'Hide', 'ReadOnly'
    PRIMARY KEY (role_id, resource_id, field_code)
);
```

Use case: Warehouse staff can see order details but CostPrice column is hidden.

**Enforcement (NON-NEGOTIABLE — both layers):**
1. **API level:** DTO projection in Application layer reads `role_field_visibility` and EXCLUDES hidden fields from response JSON. `ReadOnly` fields are included but rejected if present in update payload.
2. **UI level:** Angular reads the same visibility config (cached at login) to hide/show/disable AG Grid columns.
3. **Validation:** If a `Hide` field appears in a create/update request body → silently strip it (do NOT return error, to avoid field-name enumeration).

### 9.6 Layer 4: Workflow Permission

```sql
CREATE TABLE workflow_stage_permission (
    role_id             bigint REFERENCES role(id),
    workflow_template_id bigint,
    stage_code          varchar(50),       -- 'ACCOUNTING_CHECK', 'WAREHOUSE_PICK'
    can_view            boolean DEFAULT false,
    can_action          boolean DEFAULT false,  -- Approve/Reject/RequestChange
    can_reassign        boolean DEFAULT false,
    can_comment         boolean DEFAULT true,
    PRIMARY KEY (role_id, workflow_template_id, stage_code)
);
```

### 9.7 Default Roles (MVP)

| Role | Module Access | Data Scope | Workflow Stages |
|------|--------------|------------|-----------------|
| Admin | All | All | All |
| ChiefAccountant | GL, CA, BA, SA, PU, IN, TA | All | All accounting stages |
| Accountant_Sales | SA, CA, GL (view) | Department | SA posting, CA receipt |
| Accountant_Purchase | PU, BA, GL (view) | Department | PU posting, BA payment |
| Accountant_General | GL, DI | All | GL entry, period close |
| SalesRep | SA (add/edit) | OwnOnly | Create order |
| SalesManager | SA (all) | Department | Approve order |
| WarehouseStaff | IN (add/edit) | Branch | Pick/pack/ship |
| WarehouseManager | IN (all) | Branch | Stock take, transfer approve |
| Procurement | PU (add/edit) | OwnOnly | Create PO |
| Logistics | IN (view), SA (view) | Department | Assign shipping |
| Production | JC (all), IN (view) | Department | Production operations |
| Manager | All (view) | All | Approve, dashboard |
| IntegrationAPI | Integration endpoints only | All | None |

### 9.8 Performance: Permission Check < 1ms

```
On login:
  1. Load user's roles
  2. Load ALL role_permission entries (small dataset, ~200 rows per role)
  3. Build in-memory permission matrix: Dictionary<(Permission, Resource), DataScope>
  4. Cache in IMemoryCache per user (TTL = session lifetime)
  5. Invalidate on role change via Redis pub/sub

On each request:
  1. Read from IMemoryCache → O(1) dictionary lookup
  2. Apply DataScope filter in EF Core query pipeline
```

---

## 10. Search & Query Performance

### 10.1 MISA Problem

MISA uses a single mega view (`View_Search_Voucher`: 308K chars, 86 UNION ALL from ALL voucher tables) for search. This is unmaintainable and unscalable.

### 10.2 New App: 3-Tier Search Strategy

```
Tier 1: Direct Module Query (99% of use cases)
  └── User is in SA module → query voucher WHERE ref_type IN (SA types)
  └── Composite index: (tenant_id, ref_type, ref_date)
  └── Target: < 100ms

Tier 2: PostgreSQL Full-Text Search (cross-module search)
  └── tsvector column on voucher + voucher_detail
  └── GIN index for fast text matching
  └── Vietnamese text normalization (see §10.5)
  └── Target: < 500ms

Tier 3: Elasticsearch (Phase 3+, e-commerce scale)
  └── Separate search service
  └── Sync via outbox events
  └── Advanced faceting, fuzzy search
  └── Target: < 200ms for 1M+ documents
```

### 10.3 Index Strategy

```sql
-- Primary composite indexes (most queries filter by these)
CREATE INDEX ix_voucher_tenant_type_date
    ON voucher (tenant_id, ref_type, ref_date DESC);

CREATE INDEX ix_voucher_tenant_ref_no
    ON voucher (tenant_id, ref_no);

CREATE INDEX ix_voucher_tenant_account_object
    ON voucher (tenant_id, account_object_id, ref_date DESC);

-- Data scope index (for permission filtering)
CREATE INDEX ix_voucher_tenant_created_by
    ON voucher (tenant_id, created_by);

CREATE INDEX ix_voucher_tenant_org_unit
    ON voucher (tenant_id, org_unit_id);

-- Full-text search
ALTER TABLE voucher ADD COLUMN search_vector tsvector;
CREATE INDEX ix_voucher_search ON voucher USING GIN (search_vector);

-- GeneralLedger (high volume, partitioned)
CREATE INDEX ix_gl_tenant_account_date
    ON general_ledger (tenant_id, account_code, posted_date);

CREATE INDEX ix_gl_tenant_account_object
    ON general_ledger (tenant_id, account_object_id, posted_date);
```

### 10.4 Materialized Views (Replace MISA Mega View)

```sql
-- Per-module materialized view (instead of 86-UNION ALL mega view)
CREATE MATERIALIZED VIEW mv_voucher_list_sa AS
SELECT v.id, v.ref_no, v.ref_date, v.total_amount,
       v.description, v.is_posted, v.source,
       ao.name AS customer_name, ao.code AS customer_code
FROM voucher v
LEFT JOIN account_object ao ON v.account_object_id = ao.id
WHERE v.ref_type BETWEEN 400 AND 499  -- SA module types
  AND v.is_deleted = false;

CREATE UNIQUE INDEX ON mv_voucher_list_sa (id);

-- Refresh strategy: CONCURRENTLY after voucher save (via domain event handler)
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_voucher_list_sa;
```

**MV Refresh Safety:** Domain event handler refreshes MV after voucher save. If handler fails, Quartz.NET scheduled job refreshes all MVs every 5 minutes as fallback.

### 10.5 Vietnamese Full-Text Search Configuration (CRITICAL)

PostgreSQL default tsvector is English-centric. Vietnamese requires explicit setup:

```sql
-- Required extensions
CREATE EXTENSION IF NOT EXISTS unaccent;    -- Remove diacritics (à→a, ô→o)
CREATE EXTENSION IF NOT EXISTS pg_trgm;     -- Trigram fuzzy matching

-- Custom text search configuration for Vietnamese
CREATE TEXT SEARCH CONFIGURATION vietnamese (COPY = simple);
ALTER TEXT SEARCH CONFIGURATION vietnamese
    ALTER MAPPING FOR word WITH unaccent, simple;

-- Apply to search vector column
-- Trigger to auto-update search_vector on INSERT/UPDATE:
CREATE OR REPLACE FUNCTION voucher_search_vector_update() RETURNS trigger AS $$
BEGIN
  NEW.search_vector :=
    to_tsvector('vietnamese', coalesce(NEW.ref_no, '')) ||
    to_tsvector('vietnamese', coalesce(NEW.description, '')) ||
    to_tsvector('vietnamese', coalesce(NEW.contact_name, ''));
  RETURN NEW;
END $$ LANGUAGE plpgsql;

-- Search query (handles both accented and unaccented input):
SELECT * FROM voucher
WHERE search_vector @@ to_tsquery('vietnamese', unaccent(@query))
   OR description ILIKE '%' || unaccent(@query) || '%'  -- fallback for partial match
ORDER BY ts_rank(search_vector, to_tsquery('vietnamese', unaccent(@query))) DESC
LIMIT 50;
```

**Limitations:**
- No Vietnamese stemmer available in PostgreSQL (unlike English Porter stemmer)
- `unaccent` handles diacritics but NOT word segmentation (Vietnamese has compound words)
- For MVP: `unaccent + pg_trgm + ILIKE fallback` covers 90% of use cases
- Phase 3: Migrate to Elasticsearch with `icu_tokenizer` + Vietnamese analyzer plugin

---

## 11. Concurrency & Scalability

### 11.1 Target: 300 Concurrent Users

| Metric | Target | Strategy |
|--------|--------|----------|
| Voucher list load | < 100ms | Composite indexes + materialized views + Redis cache |
| Voucher save | < 500ms | Optimistic concurrency + EF Core |
| Voucher post | < 1s single, < 30s batch (1000) | Pessimistic lock per voucher, parallel batch |
| Lookup (Account, Customer) | < 10ms | IMemoryCache (loaded at startup) |
| Report generation | < 3s first, < 100ms cached | Dapper + Redis cache (TTL by report type) |
| Permission check | < 1ms | In-memory dictionary per user |

### 11.2 Optimistic Concurrency (Edit Conflicts)

```csharp
// EF Core optimistic concurrency via RowVersion — BOTH header AND detail rows
public class Voucher
{
    [Timestamp]
    public uint RowVersion { get; set; }  // PostgreSQL xmin
}

public class VoucherDetail
{
    [Timestamp]
    public uint RowVersion { get; set; }  // Prevents silent detail-line overwrites
}

// On save:
// If another user modified the same voucher OR any detail line → DbUpdateConcurrencyException
// → Return HTTP 409 Conflict with message "Chứng từ đã bị thay đổi bởi người dùng khác"
// → Client must refresh and re-apply changes
```

**CRITICAL**: RowVersion on VoucherDetail prevents last-write-wins when two users edit the same detail line simultaneously.

### 11.3 Pessimistic Lock (Posting Protection)

```sql
-- When posting a voucher, lock the row to prevent concurrent posting
BEGIN;
SELECT id FROM voucher
WHERE id = @voucherId AND is_posted = false
FOR UPDATE NOWAIT;  -- Fail immediately if locked

-- Execute posting logic...
UPDATE voucher SET is_posted = true, posted_date = now();
COMMIT;
```

### 11.4 Entity-Level Lock (Workflow Stages)

```sql
-- Lock workflow stage while processing (prevent double-processing)
SELECT id FROM workflow_stage_instance
WHERE id = @stageId AND status = 'InProgress'
FOR UPDATE SKIP LOCKED;  -- Skip if already being processed
```

### 11.5 Connection Pool Configuration

```
PostgreSQL:
  - Min pool size: 10
  - Max pool size: 100 (for 300 users, most queries are fast)
  - Connection lifetime: 300s
  - Idle timeout: 60s

Redis:
  - Min connections: 5
  - Max connections: 50
  - Connect timeout: 5s
```

### 11.6 Scalability Architecture (300 → 1000+ Users)

```
Current (MVP — 300 users):
┌──────────┐     ┌────────────┐     ┌─────────┐
│ API x2   │────▶│ PostgreSQL │     │  Redis  │
│(load bal)│     │  Primary   │     │ (cache) │
└──────────┘     └────────────┘     └─────────┘

Future (1000+ users):
┌──────────┐     ┌────────────┐     ┌─────────────┐     ┌──────────────┐
│ API x4   │────▶│ PostgreSQL │     │ Redis       │     │Elasticsearch │
│(load bal)│     │  Primary   │     │ Cluster     │     │ (search)     │
│          │──┐  │  + Replica │     └─────────────┘     └──────────────┘
└──────────┘  │  └────────────┘
              │  ┌────────────┐
              └─▶│ PostgreSQL │  ← Read replica for reports
                 │  Replica   │
                 └────────────┘
```

---

## 12. Caching Strategy

### 12.1 Two-Level Cache

```
L1: IMemoryCache (per API server instance)
  - Master data: Accounts, AccountObjects, InventoryItems, Warehouses
  - Permission matrix per user
  - Config: VoucherTemplate, PostMapping, SYSDBOption
  - TTL: 5 minutes
  - Invalidation: Redis pub/sub on data change
  - **Fallback:** If Redis pub/sub is DOWN, L1 still expires after TTL (max 5 min stale)
  - **Max stale guarantee:** Even with Redis failure, no data older than 5 minutes served

L2: Redis (shared across all API instances)
  - Report results (TTL varies by report type)
  - Session data (JWT refresh tokens)
  - Rate limit counters
  - Workflow stage status (real-time)
  - Stock availability cache (for future e-com)
```

### 12.2 Cache Invalidation

```
Data change in any API instance
  → Domain event (MediatR)
  → Handler invalidates L1 local cache
  → Handler publishes to Redis pub/sub channel
  → All other API instances receive message
  → Each invalidates their L1 local cache

Channel examples:
  - cache:account:invalidate
  - cache:inventory:invalidate
  - cache:permission:invalidate:{userId}
```

### 12.3 Report Cache Strategy

| Report Type | TTL | Key Pattern |
|-------------|-----|-------------|
| Trial Balance — F01 (Bảng cân đối phát sinh) | 30 min | `report:F01:{tenantId}:{fromDate}:{toDate}` |
| Balance Sheet — B01-DN (Bảng cân đối kế toán) | 30 min | `report:B01:{tenantId}:{date}` |
| Income Statement — B02-DN (Báo cáo kết quả HĐKD) | 30 min | `report:B02:{tenantId}:{period}` |
| Cash Flow — B03-DN (Báo cáo lưu chuyển tiền tệ) | 1 hour | `report:B03:{tenantId}:{period}` |
| Notes — B09-DN (Thuyết minh BCTC) | 1 hour | `report:B09:{tenantId}:{period}` |
| Voucher list | 5 min | `list:{module}:{tenantId}:{filterHash}` |
| Dashboard KPIs | 2 min | `dashboard:{tenantId}` |

**Invalidation**: When a voucher is posted/unposted → invalidate all report caches for that tenant + affected period.

---

## 13. E-Commerce Integration Readiness

### 13.1 Design-Now Items (20 DO-NOW)

Built into the accounting webapp from day 1 — no breaking changes when e-com integrates.

| # | Category | Item | Impact |
|---|----------|------|--------|
| 1 | Cross-cutting | Source tracking on all vouchers (Manual/EcomAPI/Import/System) | Schema |
| 2 | Cross-cutting | ExternalRefId + ExternalRefType columns | Schema |
| 3 | Cross-cutting | IdempotencyKey table (TTL 72h) | Schema |
| 4 | Cross-cutting | Domain events bus (MediatR in-memory → RabbitMQ later) | Architecture |
| 5 | Cross-cutting | Data-level audit trail (before/after JSON diff) | Schema |
| 6 | Cross-cutting | API route namespace (/api/v1/integration/ reserved) | Architecture |
| 7 | DI | SKU, Barcode, IsActive, IsPublishable on InventoryItem | Schema |
| 8 | DI | AccountObject source tracking + customer matching interface | Schema + Interface |
| 9 | DI | AccountObjectAddress (multiple addresses per customer) | Schema |
| 10 | SA | ChannelType, OrderStatus lifecycle, cancellation fields | Schema |
| 11 | SA | Shipping information fields on voucher | Schema |
| 12 | SA | Payment method + status + reference fields | Schema |
| 13 | SA | Return/exchange fields (RMA, refund method, exchange link) | Schema |
| 14 | SA | Price snapshot rule (NON-NEGOTIABLE) | Business rule |
| 15 | IN | 4-quantity model (OnHand, Reserved, OnHold, Damaged) | Schema + Logic |
| 16 | IN | InventoryHold table (cart hold with TTL) | Schema |
| 17 | IN | IInventoryService interface (hold/reserve/release) | Interface |
| 18 | IN | Anti-oversell validation using QuantityAvailable | Business rule |
| 19 | GL | Source + ChannelType on GeneralLedger entries | Schema |
| 20 | GL | Report filters by Source/Channel | Query design |

### 13.2 Design-Later Items (13 DO-LATER)

| # | Category | Item | Phase |
|---|----------|------|-------|
| 1 | SA | SAPolicyPrice (multi-price-list engine) | Phase 2 |
| 2 | SA | Promotion/discount tracking | Phase 2 |
| 3 | SA | Partial fulfillment (1 order → N shipments) | Phase 2 |
| 4 | SA | Backorder management | Phase 3 |
| 5 | IN | Auto-create INOutward from SAOrder | Phase 3 |
| 6 | IN | Multi-warehouse optimal selection | Phase 3 |
| 7 | IN | Serial number / lot tracking | Phase 2 |
| 8 | IN | Auto-transfer between warehouses | Phase 3 |
| 9 | CA | Auto-receipt from COD collection | Phase 3 |
| 10 | BA | Payment gateway reconciliation | Phase 3 |
| 11 | PU | Auto-PO from low stock (reorder point) | Phase 4 |
| 12 | TA | E-Invoice auto-publish queue | Phase 2 |
| 13 | IP | Batch e-invoice publishing | Phase 2 |

### 13.3 Architecture Pattern

```
Accounting App          Message Queue           E-commerce App
(Source of Truth)       (RabbitMQ)              (Storefront)
                                                
┌───────────┐           ┌───────┐               ┌───────────┐
│ REST API  │◄────────►│  MQ   │◄─────────────►│ REST API  │
│           │           │       │               │           │
│ PostgreSQL│           │ Redis │               │ Separate  │
│ (all data)│           │(cache)│               │    DB     │
└───────────┘           └───────┘               └───────────┘
```

**Key rules:**
- Separate databases (NEVER share DB)
- Accounting = Source of Truth for inventory, pricing, master data
- Idempotent APIs with `X-Idempotency-Key`
- Price snapshot locked at order confirmation
- Daily reconciliation (stock, orders, revenue)
- Full specification: `ecom-integration-architecture.md`

---

## 14. Infrastructure & Deployment

### 14.1 Development Environment

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    volumes: ["pgdata:/var/lib/postgresql/data"]
    environment:
      POSTGRES_DB: phanmemketoan
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  minio:
    image: minio/minio
    ports: ["9000:9000", "9001:9001"]
    command: server /data --console-address ":9001"
    volumes: ["miniodata:/data"]
```

### 14.2 Production Architecture (MVP)

```
┌──────────────────────────────────────────────┐
│              VPS (Singapore region)           │
│                                              │
│  ┌─────────┐  ┌─────────┐  ┌─────────────┐  │
│  │ Nginx   │  │ API     │  │ PostgreSQL  │  │
│  │ (proxy) │─▶│ x2      │─▶│   16        │  │
│  │ + SSL   │  │ (dotnet)│  │ (primary)   │  │
│  └─────────┘  └─────────┘  └─────────────┘  │
│                    │                         │
│               ┌────▼────┐  ┌──────────────┐  │
│               │  Redis  │  │    MinIO     │  │
│               │   7     │  │  (files)     │  │
│               └─────────┘  └──────────────┘  │
│                                              │
│  ┌──────────────────────────────────────┐    │
│  │ Angular SPA (served by Nginx)        │    │
│  └──────────────────────────────────────┘    │
└──────────────────────────────────────────────┘
```

### 14.3 CI/CD Pipeline (GitHub Actions)

```
Push to main branch
  → Build .NET solution
  → Run unit tests (posting engine 100% coverage required)
  → Run integration tests
  → Build Angular app (ng build --prod)
  → Build Docker images
  → Push to container registry
  → Deploy to staging
  → Smoke tests
  → Manual approval → Deploy to production
```

### 14.4 Backup Strategy

| Component | Frequency | Retention | Method |
|-----------|-----------|-----------|--------|
| PostgreSQL | Daily full + hourly WAL | 30 days | pg_basebackup + WAL archiving |
| PostgreSQL | Point-in-time | 7 days | Continuous WAL archiving |
| Redis | Hourly RDB | 7 days | redis-cli bgsave |
| MinIO | Daily sync | 30 days | mc mirror to backup storage |
| Application config | Per deploy | Infinite | Git repository |

---

## 15. Security Architecture

### 15.1 Authentication

| Aspect | Implementation |
|--------|---------------|
| Protocol | JWT (access token + refresh token) |
| Access token TTL | 15 minutes |
| Refresh token TTL | 7 days |
| Storage | Access token in memory, refresh token in httpOnly cookie |
| Multi-device | Support multiple active sessions per user |
| Password | bcrypt hash, minimum 8 chars |
| 2FA | TOTP (Google Authenticator) — optional in MVP |
| **Session revocation** | **Per-device token tracking + revocation endpoint** |

**Session Management:**
```
Login → issue access + refresh token → store refresh_token_id in DB table:
  user_session (id, user_id, device_info, refresh_token_hash, created_at, revoked_at)

Logout (single device) → SET revoked_at = now() WHERE id = @sessionId
Logout (all devices) → SET revoked_at = now() WHERE user_id = @userId
Token refresh → check user_session.revoked_at IS NULL before issuing new tokens

API endpoint: GET /api/auth/sessions → list active sessions
API endpoint: DELETE /api/auth/sessions/{id} → revoke specific session
```

### 15.2 API Security

| Concern | Solution |
|---------|----------|
| Transport | HTTPS / TLS 1.3 only |
| CORS | Whitelist allowed origins |
| Rate limiting | 100 req/s read, 20 req/s write per user |
| Input validation | FluentValidation on every command |
| SQL injection | Parameterized queries (EF Core + Dapper) |
| XSS | Angular auto-escapes, Content-Security-Policy headers |
| CSRF | SameSite cookies + anti-forgery tokens |
| File upload | Type validation, size limits (10MB), virus scan queue |

### 15.3 Data Security

| Concern | Solution |
|---------|----------|
| Multi-tenant isolation | EF Core Global Query Filters (TenantId on every query) |
| Sensitive fields | Encrypt at rest (AES-256): bank account numbers, tax IDs |
| Audit trail | All create/update/delete logged with before/after values |
| Soft delete | `is_deleted = true` (never hard delete financial data) |
| Data export | GDPR-style: export user's data on request |
| Backup encryption | AES-256 encrypted backups |

### 15.4 Integration Security (Future E-com)

| Concern | Solution |
|---------|----------|
| Service-to-service auth | API Key + JWT (service account) |
| IP whitelist | Only registered e-com server IPs |
| Webhook verification | HMAC-SHA256 signature |
| Rate limiting | Separate limits for integration endpoints |
| Price tampering | Server-side validation: order price within ±5% of current |

---

## 16. Performance Targets

### 16.1 Response Time SLA

| Operation | Target | P99 | Strategy |
|-----------|--------|-----|----------|
| Voucher list (paginated) | < 100ms | < 200ms | Composite index + materialized view |
| Voucher create/save | < 500ms | < 1s | Direct EF Core insert |
| Voucher post (single) | < 1s | < 2s | Pessimistic lock + batch GL insert |
| Voucher post (batch 1000) | < 30s | < 60s | Parallel processing, bulk insert |
| Lookup/autocomplete | < 10ms | < 50ms | IMemoryCache (L1) |
| Report (first load) | < 3s | < 10s | Dapper + optimized SQL |
| Report (cached) | < 100ms | < 200ms | Redis (L2) |
| Permission check | < 1ms | < 5ms | In-memory dictionary |
| Full-text search | < 500ms | < 1s | PostgreSQL tsvector + GIN index |
| Workflow action | < 500ms | < 1s | Optimistic lock + SignalR notify |
| Login | < 500ms | < 1s | bcrypt verify + JWT issue |
| File upload (10MB) | < 3s | < 5s | Stream to MinIO |

### 16.2 Throughput

| Metric | Target (300 users) | Notes |
|--------|-------------------|-------|
| Concurrent API requests | 300 | 2 API instances × 150 concurrent |
| DB connections | 100 pool | 50 per API instance — justification: 300 users ÷ avg 3:1 multiplexing ÷ 2 instances = ~50/instance. PostgreSQL max_connections = 150 (headroom for admin + Quartz.NET jobs). Monitor with `pg_stat_activity`. Scale up if avg active > 80%. |
| Voucher saves/min | 600 | ~2 saves/user/min average |
| Posting operations/min | 100 | Batch posting during peak |
| WebSocket connections | 300 | 1 per active user |

### 16.3 Storage Estimates (Per Tenant Per Year)

| Data Type | Estimated Size | Notes |
|-----------|---------------|-------|
| Vouchers | 500MB | ~50K vouchers × 10KB avg |
| GeneralLedger | 1GB | ~100K entries × 10KB avg |
| AuditLog | 500MB | ~200K entries × 2.5KB avg |
| Attachments (MinIO) | 5GB | Scanned docs, receipts |
| Redis cache | 100MB | Reports, sessions, config |
| **Total per tenant/year** | **~7GB** | **Well within PostgreSQL capacity** |

---

## 17. Fiscal Year, Period Close & Voucher Numbering

### 17.1 Fiscal Year Configuration

MISA uses `SYSDBOption.StartDate = 01/01/2019` and `AccountingSystem = 15` (TT200). The new app must support:

**SYSDBOption equivalent (tenant-scoped config):**
```sql
CREATE TABLE sys_db_option (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    option_key      varchar(100) NOT NULL,
    option_value    jsonb NOT NULL,
    description     text,
    UNIQUE (tenant_id, option_key)
);

-- MVP option keys:
-- 'AccountingSystem'    → {"value": "TT99"}          -- or TT133
-- 'FiscalYearStart'     → {"month": 1, "day": 1}     -- default Jan 1
-- 'BaseCurrency'        → {"code": "VND"}
-- 'DecimalPlaces'       → {"amount": 0, "quantity": 3, "unit_price": 2, "exchange_rate": 4}
-- 'AllowOverCashPay'    → {"value": false}            -- ref:BR-CA01
-- 'AllowOverStock'      → {"value": false}            -- ref:BR-IN02
-- 'VoucherDateRule'     → {"mode": "period_only"}     -- or "any_open_period"
-- 'AutoNumberPrefix'    → {"PT": "PT{YY}{MM}-", "PC": "PC{YY}{MM}-", ...}
```

```sql
CREATE TABLE fiscal_year (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    year_code       varchar(10) NOT NULL,         -- '2026', 'FY2026-27'
    start_date      date NOT NULL,                -- Usually Jan 1, but some orgs start Apr 1
    end_date        date NOT NULL,
    status          varchar(20) DEFAULT 'Open',   -- Open, SoftClosed, HardClosed
    is_current      boolean DEFAULT false,
    UNIQUE (tenant_id, year_code)
);

CREATE TABLE fiscal_period (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    fiscal_year_id  bigint REFERENCES fiscal_year(id),
    period_number   smallint NOT NULL,            -- 1-12 (monthly) or 1-4 (quarterly)
    period_name     varchar(50),                  -- 'Tháng 01/2026'
    start_date      date NOT NULL,
    end_date        date NOT NULL,
    status          varchar(20) DEFAULT 'Open',   -- Open, SoftClosed, HardClosed
    closed_by       uuid,
    closed_at       timestamptz,
    UNIQUE (tenant_id, fiscal_year_id, period_number)
);
```

**Period status rules:**
- **Open**: Normal CRUD + posting
- **SoftClosed**: Only ChiefAccountant/Admin can post (prevent accidental edits after close)
- **HardClosed**: No changes allowed (readonly). Used after tax filing / audit

**Validation on every voucher save/post:**
```
IF period.status = 'HardClosed' → reject with "Kỳ kế toán đã khóa sổ"
IF period.status = 'SoftClosed' AND user.role NOT IN ('Admin', 'ChiefAccountant') → reject
```

### 17.2 Period Close Workflow (Kết chuyển cuối kỳ)

MISA uses `AccountTransfer` table (20 rules) for period-end closing entries. The new app:

```
Period Close Process:
  1. Pre-check: All vouchers posted? Any draft vouchers in this period?
  2. ChiefAccountant clicks "Kết chuyển cuối kỳ" → system auto-generates closing entries (AccountTransfer rules):
     - Revenue accounts (5xx) → 911 (P&L Summary)
     - Expense accounts (6xx) → 911 (P&L Summary)
     - 911 → 421 (Retained Earnings) if profit, or 421 → 911 if loss
  3. Closing entries are created as DRAFT vouchers → ChiefAccountant reviews amounts
  4. ChiefAccountant approves → closing entries auto-posted in single transaction
  5. Calculate cost of goods (JC module — Phase 2, skipped in MVP)
  6. Generate financial reports (F01, B01-DN, B02-DN, B03-DN, B09-DN)
  7. Set period status = SoftClosed (only Admin/ChiefAccountant can still post)
  8. After tax filing / audit → ChiefAccountant sets HardClosed (no changes allowed)

Reopen (rare):
  - Only Admin can revert HardClosed → SoftClosed (audit trail required)
  - Closing entries must be unposted + deleted before reverting SoftClosed → Open
```

**AccountTransfer config (JSON):**
```json
[
  { "fromAccount": "511*", "toAccount": "911", "description": "Kết chuyển doanh thu bán hàng" },
  { "fromAccount": "515*", "toAccount": "911", "description": "Kết chuyển doanh thu tài chính" },
  { "fromAccount": "632*", "toAccount": "911", "description": "Kết chuyển giá vốn hàng bán" },
  { "fromAccount": "641*", "toAccount": "911", "description": "Kết chuyển chi phí bán hàng" },
  { "fromAccount": "642*", "toAccount": "911", "description": "Kết chuyển chi phí quản lý" },
  { "fromAccount": "911",  "toAccount": "4212", "description": "Kết chuyển lãi/lỗ" }
]
```

### 17.3 Opening Balance (Số dư đầu kỳ)

```
New fiscal year:
  1. Carry forward balance sheet accounts (1xx, 2xx, 3xx, 4xx) → opening balance
  2. Income/expense accounts (5xx-9xx) reset to zero
  3. Inventory balance carry forward per item per warehouse
  4. AccountObject debt balance carry forward
  5. Store as special voucher: RefType = 'OPENING_BALANCE'
```

### 17.4 Voucher Numbering (Đánh số chứng từ)

MISA uses `SYSNewRefNo` table for tracking. New app needs concurrent-safe numbering:

```sql
CREATE TABLE voucher_sequence (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    ref_type        smallint NOT NULL,
    fiscal_year_id  bigint REFERENCES fiscal_year(id),
    prefix          varchar(10) NOT NULL,         -- 'PT', 'PC', 'BH', 'MH'
    current_value   bigint NOT NULL DEFAULT 0,
    format_pattern  varchar(50) DEFAULT '{prefix}{YYYYMMDD}-{seq:4}',
    UNIQUE (tenant_id, ref_type, fiscal_year_id)
);
```

**Concurrent-safe generation:**
```sql
-- Atomic increment + return (no gaps under concurrency)
UPDATE voucher_sequence
SET current_value = current_value + 1
WHERE tenant_id = @tenantId AND ref_type = @refType AND fiscal_year_id = @yearId
RETURNING current_value;

-- Format: PT-20260415-0001, BH-20260415-0042
```

**Rules:**
- Each RefType has its own sequence per fiscal year
- Prefix is configurable per tenant (some use 'PT' for Phiếu thu, others use 'RC' for Receipt)
- Sequence resets on new fiscal year or new month (configurable)
- Gaps are acceptable (no fill-gap-on-delete — accounting can't pretend voucher never existed)
- Manual override allowed for ChiefAccountant (e.g., imported vouchers with legacy numbers)

### 17.5 Dual Reference Numbers (Finance + Management)

MISA has `RefNoFinance` and `RefNoManagement` — separate numbering for each book.

```
Voucher header:
  - ref_no_finance:    varchar(20)  -- Số chứng từ sổ tài chính (cho thuế/kiểm toán)
  - ref_no_management: varchar(20)  -- Số chứng từ sổ quản trị (nội bộ)
```

Both numbers may differ. Financial book is for tax authorities; management book is for internal tracking.

### 17.6 Exchange Rate Difference Handling (Chênh lệch tỷ giá)

MISA has `GLVoucherDetailForeignExchange` table + `ExchangeRateOperator` field. TT99/TT200 requires:

**Two types of FX difference:**

| Type | When | Accounts | Description |
|------|------|----------|-------------|
| **Realized** (Thanh toán) | Pay/receive foreign currency debt | TK 515 (gain) / TK 635 (loss) | Difference between booked rate and settlement rate |
| **Unrealized** (Đánh giá lại cuối kỳ) | Period-end revaluation | TK 413 (FX revaluation reserve) | Mark-to-market all foreign currency accounts |

**Revaluation targets (period-end):**
- TK 1112: Cash in hand (foreign currency)
- TK 1122: Cash in bank (foreign currency)
- TK 131: Accounts receivable (foreign currency debts)
- TK 331: Accounts payable (foreign currency debts)
- TK 136, 138, 141, 244, 341, 343: Other foreign currency balances

**ExchangeRateOperator:**
- `multiply` (default): VND amount = OC amount × ExchangeRate
  - Applies to: USD, EUR, GBP, JPY, CNY, KRW, THB, SGD, AUD, CAD, CHF, TWD, HKD, MYR — virtually all currencies
- `divide` (rare, legacy): VND amount = OC amount ÷ ExchangeRate
  - Currently no standard currency uses this. Kept for backward compatibility with MISA data migration.
  - If encountered during import, convert to multiply equivalent: `new_rate = 1 / old_rate`
- Stored per voucher detail line as `exchange_rate_operator` enum (`Multiply = 0`, `Divide = 1`)
- **Default: Multiply for all new vouchers. Divide only imported from legacy.**

**Implementation:**
```sql
-- Realized FX difference (on payment of foreign currency debt)
-- Example: Booked at 25,000 VND/USD, paid at 25,450 VND/USD → Loss of 450 VND/USD
fx_diff = (settlement_rate - booked_rate) × oc_amount × operator_direction
IF fx_diff > 0 → Debit 635 (Financial expense) / Credit debt account
IF fx_diff < 0 → Debit debt account / Credit 515 (Financial income)

-- Period-end revaluation
-- For each foreign currency account balance:
revaluation_diff = (closing_rate × oc_balance) - vnd_balance
-- Auto-generate GL voucher: Debit/Credit 413 ↔ target account
```

**Business rules:**
- ref:BR-GL13 — Foreign currency vouchers MUST store both Amount (VND) and AmountOC (original currency)
- ExchangeRate per voucher line (not global per voucher) — some vouchers mix currencies
- Period-end revaluation auto-generates a GL voucher (RefType = FX_REVALUATION)
- Revaluation reverses on next period open (provisional entries)

### 17.7 Debt Aging & Debt Agreement Tracking (Công nợ theo tuổi nợ)

MISA has `DebtPeriod` (15 rows), `PUDebtPeriod` (40 rows), `DebtAgreement` tables.

**Debt aging periods (configurable per tenant):**

```sql
CREATE TABLE debt_aging_period (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    period_name     varchar(50) NOT NULL,         -- 'Chưa đến hạn', '1-30 ngày', '31-60 ngày', 'Quá hạn > 90 ngày'
    from_days       int NOT NULL,                 -- 0, 1, 31, 61, 91
    to_days         int,                          -- 0, 30, 60, 90, NULL (unlimited)
    sort_order      smallint NOT NULL,
    UNIQUE (tenant_id, sort_order)
);
```

**Debt agreement (Khế ước nợ — per-invoice tracking):**

```sql
CREATE TABLE debt_agreement (
    id              bigint PRIMARY KEY,
    tenant_id       uuid NOT NULL,
    account_object_id bigint NOT NULL,            -- FK → account_object
    account_code    varchar(20) NOT NULL,         -- '131' (receivable) or '331' (payable)
    ref_voucher_id  bigint NOT NULL,              -- FK → voucher (original invoice)
    original_amount decimal(18,3) NOT NULL,
    remaining_amount decimal(18,3) NOT NULL,
    currency_id     varchar(3) NOT NULL DEFAULT 'VND',
    due_date        date,                         -- For aging calculation
    is_settled      boolean DEFAULT false,
    created_at      timestamptz DEFAULT now()
);
```

**Reports:**
- Bảng kê công nợ phải thu theo tuổi nợ (Receivable aging — TK 131)
- Bảng kê công nợ phải trả theo tuổi nợ (Payable aging — TK 331)
- Chi tiết công nợ theo khế ước (Debt detail per agreement)
- Đối chiếu công nợ (Reconciliation statement — send to customer/vendor)

**Payment matching:**
- When cash receipt pays an invoice → link CAReceiptDetail → DebtAgreement
- FIFO matching by default (oldest debt first), manual override allowed
- Partial payment → reduce DebtAgreement.remaining_amount

---

## 18. Localization & Formatting

### 18.1 Timezone Handling

| Rule | Implementation |
|------|---------------|
| Storage | All timestamps stored as `timestamptz` (UTC) in PostgreSQL |
| Display | Convert to tenant's timezone on API response |
| Default timezone | `Asia/Ho_Chi_Minh` (UTC+7) |
| Configuration | Per-tenant timezone setting in SYSDBOption |
| Date input | Frontend sends ISO 8601 with timezone; API normalizes to UTC |

**Critical for accounting**: `ref_date` (ngày chứng từ) and `posted_date` (ngày hạch toán) are **date-only** (no time component) — stored as `date` type, not `timestamptz`. This avoids timezone conversion issues for financial dates.

### 18.2 Number & Currency Formatting

| Format | Vietnam Standard | Example |
|--------|-----------------|---------|
| Thousands separator | `.` (dot) | 1.500.000 |
| Decimal separator | `,` (comma) | 25.000,50 |
| Currency symbol | `đ` (suffix) | 1.500.000 đ |
| Foreign currency | ISO code prefix | USD 150.500 |
| Negative amounts | Parentheses or minus | (1.500.000) or -1.500.000 |
| Percentage | `,` decimal | 10,5% |

**Implementation:**
- Backend returns raw numbers; frontend formats using Angular locale `vi-VN`
- API accepts raw numbers (no formatting)
- Excel/PDF export uses `vi-VN` locale formatting
- Configurable per tenant (some enterprises prefer international format)

### 18.3 Date Formatting

| Context | Format | Example |
|---------|--------|---------|
| Display (default) | dd/MM/yyyy | 15/04/2026 |
| API (JSON) | yyyy-MM-dd | 2026-04-15 |
| Date-Time display | dd/MM/yyyy HH:mm | 15/04/2026 14:30 |
| Report headers | "Tháng 04 năm 2026" | Full Vietnamese month |
| Fiscal period | "Quý I/2026" or "T01/2026" | Quarterly or monthly |

### 18.4 Multi-Language (Future-Ready)

- MVP: Vietnamese only (UI labels, messages, report titles)
- All UI strings externalized in Angular i18n files (`vi.json`)
- API error messages: return `code` + `message_vi` + `message_en` (English fallback)
- Report templates: language parameter for bilingual reports (Vietnamese + English for foreign investors)
- Database content (account names, item names): stored as-is, no translation needed

---

## 19. Error Handling & Observability

### 19.1 Global Error Handling

```
API Exception Pipeline:
  Request → Auth → Validation → Handler → Response
                      ↓              ↓
              ValidationException  DomainException
              → 400 Bad Request    → 422 Unprocessable
                                        ↓
                              InfrastructureException
                              → 500 Internal Server Error
```

**Standard error response format:**
```json
{
  "status": 422,
  "code": "VOUCHER_PERIOD_CLOSED",
  "message": "Kỳ kế toán tháng 03/2026 đã khóa sổ. Không thể hạch toán.",
  "details": [
    { "field": "postedDate", "message": "Ngày hạch toán thuộc kỳ đã khóa" }
  ],
  "traceId": "abc123-def456"
}
```

**Exception types:**

| Exception | HTTP Status | When |
|-----------|-------------|------|
| ValidationException | 400 | Input validation fails (FluentValidation) |
| NotFoundException | 404 | Entity not found |
| ForbiddenException | 403 | Permission denied (4-layer check) |
| ConflictException | 409 | Optimistic concurrency conflict |
| DomainException | 422 | Business rule violation (period closed, insufficient stock, imbalanced posting) |
| InfrastructureException | 500 | DB error, Redis error, MinIO error |

### 19.2 Correlation & Tracing

```
Every request:
  1. Middleware generates CorrelationId (or reads from X-Correlation-Id header)
  2. CorrelationId attached to:
     - All log entries (Serilog enricher)
     - Database commands (comment tag)
     - Redis operations
     - Domain events (Outbox payload)
     - Error responses (traceId field)
  3. Frontend includes CorrelationId in error reports → Support can trace full request lifecycle
```

### 19.3 Structured Logging (Serilog)

```json
{
  "timestamp": "2026-04-15T14:30:00Z",
  "level": "Information",
  "message": "Voucher posted successfully",
  "correlationId": "abc123",
  "tenantId": "tenant-001",
  "userId": "user-042",
  "voucherId": 12345,
  "refType": "SAInvoice",
  "refNo": "BH-20260415-0001",
  "duration_ms": 234
}
```

**Log levels:**
- `Debug`: SQL queries, cache hits/misses (dev only)
- `Information`: Voucher CRUD, posting, workflow actions, login
- `Warning`: Permission denied, concurrency conflict, SLA approaching
- `Error`: Unhandled exceptions, DB connection failures, posting imbalance
- `Fatal`: Application startup failure, migration failure

### 19.4 Health Checks & Monitoring

```csharp
// ASP.NET Core health check endpoints
/health          → Overall status (for load balancer)
/health/ready    → Readiness (DB connected, Redis connected, MinIO reachable)
/health/live     → Liveness (process alive)
```

**Health check dependencies:**

| Dependency | Check | Interval |
|------------|-------|----------|
| PostgreSQL | SELECT 1 | 30s |
| Redis | PING | 30s |
| MinIO | HeadBucket | 60s |
| Quartz.NET | Scheduler running | 60s |

**Key metrics to collect (via Prometheus / Application Insights):**

| Metric | Type | Purpose |
|--------|------|---------|
| `http_request_duration_seconds` | Histogram | API latency per endpoint |
| `posting_operations_total` | Counter | Posting volume |
| `posting_errors_total` | Counter | Posting failures |
| `active_websocket_connections` | Gauge | SignalR connections |
| `db_connection_pool_size` | Gauge | PostgreSQL pool utilization |
| `cache_hit_ratio` | Gauge | L1/L2 cache effectiveness |
| `workflow_sla_breaches_total` | Counter | SLA violations |
| `outbox_pending_messages` | Gauge | Outbox queue depth |

### 19.5 Email Service (Workflow Notifications)

```
Workflow → Notification Service → Email Queue (background job)
                                → In-App (SignalR, real-time)
```

| Component | Technology | Purpose |
|-----------|-----------|---------|
| SMTP Transport | MailKit | Send emails via SMTP |
| Template Engine | Razor Light or Scriban | HTML email templates (Vietnamese) |
| Queue | Quartz.NET job | Batch send, retry on failure |
| Provider (MVP) | Gmail SMTP / SendGrid free tier | Cost-effective |
| Provider (Production) | Amazon SES / SendGrid | Deliverability + scale |

**Email types (MVP):**
- Workflow stage assigned
- SLA warning (80% elapsed)
- SLA overdue escalation
- Password reset
- Daily digest: pending tasks summary

---

## 20. Testing Strategy

### 20.1 Test Pyramid

```
                    ┌─────────┐
                    │   E2E   │  5% — Critical happy paths
                    │ (Playwright) │
                ┌───┴─────────┴───┐
                │   Integration   │  25% — API + DB + Redis
                │   Tests         │
            ┌───┴─────────────────┴───┐
            │       Unit Tests        │  70% — Domain + Application layer
            │                         │
            └─────────────────────────┘
```

### 20.2 Coverage Requirements

| Component | Minimum Coverage | Priority |
|-----------|:----------------:|----------|
| Posting Engine | **100%** | NON-NEGOTIABLE |
| Domain Services (Inventory, Period Close) | 90% | CRITICAL |
| Application Handlers (Commands) | 80% | HIGH |
| FluentValidation Rules | 80% | HIGH |
| Query Handlers | 50% | MEDIUM |
| Controllers | 30% (integration tests cover most) | LOW |

### 20.3 Test Types

| Type | Framework | What to Test |
|------|-----------|-------------|
| Unit | xUnit + Moq + FluentAssertions | Domain logic, validators, calculations, posting rules |
| Integration | xUnit + TestContainers (PostgreSQL) | EF Core queries, Dapper queries, API endpoints with real DB |
| E2E | Playwright | Login → Create voucher → Post → Check report |
| Performance | k6 / NBomber | 300 concurrent users, posting throughput |
| Snapshot | Verify | Financial report output comparison (prevent regression) |

### 20.4 Key Test Scenarios for Posting Engine

```
✅ Single detail → 2 GL entries (balanced)
✅ Multi-detail → 2N GL entries (all balanced)
✅ Foreign currency → correct exchange rate conversion + rounding
✅ VAT deduction method → TaxLedger entries correct
✅ VAT direct method → no TaxLedger entries
✅ Dual-book: Financial vs Management vs Both
✅ Weighted average recalculation after posting
✅ Unpost → GL entries deleted, balances reverted
✅ Idempotent: post same voucher twice → no duplicate GL
✅ Decimal precision: VND (0), OC (3), UnitPrice (2)
✅ Period closed → posting rejected
✅ Insufficient stock → posting rejected (if inventory voucher)
✅ Account is parent → posting rejected
✅ Mandatory dimensions missing → posting rejected
✅ Rounding edge case: 3+ GL lines with fractional amounts → balanced after rounding adjustment
✅ FX realized difference: pay foreign debt at different rate → correct 515/635 entries
✅ FX revaluation: period-end revalue → correct 413 entries + auto-reverse next period
✅ AllowOverCashPayment=false: cash payment exceeds balance → rejected
✅ AllowOverOutwardStock=false: outward exceeds stock → rejected
✅ Concurrent edit: two users save same voucher → second gets 409 Conflict
```

### 20.5 Test Data

- **Seed data**: Chart of Accounts (TT99 + TT133), sample AccountObjects, sample InventoryItems
- **Golden dataset**: ~100 vouchers with known correct GL output (exported from MISA for comparison)
- **Property-based tests**: Random voucher generation → assert SUM(Debit) = SUM(Credit) always

---

## 21. Data Migration & Opening Balance

### 21.1 Migration Sources

| Source | Method | Priority |
|--------|--------|----------|
| MISA AMIS | Direct DB read (SQL Server) → transform → import via API | HIGH |
| Excel/CSV | User uploads standardized templates | HIGH (MVP) |
| Fast Accounting | CSV export → import | MEDIUM |
| Other systems | API or CSV → mapping template | LOW |

### 21.2 Import Templates (MVP)

| Data | Template | Key Fields |
|------|----------|------------|
| Chart of Accounts | `import_accounts.xlsx` | Code, Name, ParentCode, IsParent, Kind, DetailBy* |
| Account Objects (KH/NCC) | `import_customers.xlsx` | Code, Name, TaxCode, Phone, Email, Address |
| Inventory Items | `import_items.xlsx` | Code, Name, Unit, Category, CostingMethod, OpeningQty, OpeningAmount |
| Warehouses | `import_warehouses.xlsx` | Code, Name, Address, IsDefault |
| Units of Measure | `import_units.xlsx` | Code, Name, BaseUnit, ConvertRate |
| Departments | `import_departments.xlsx` | Code, Name, ParentCode, ManagerCode |
| Employees | `import_employees.xlsx` | Code, Name, DepartmentCode, Phone, Email, BankAccount |
| Opening Balance (Account) | `import_opening_balance.xlsx` | AccountCode, DebitAmount, CreditAmount, AccountObjectCode |
| Opening Balance (Inventory) | `import_inventory_balance.xlsx` | ItemCode, WarehouseCode, Quantity, UnitPrice, Amount |
| Opening Balance (Debt) | `import_debt_balance.xlsx` | AccountCode, AccountObjectCode, Amount, DueDate |

### 21.3 Migration Validation

```
After import:
  1. Trial balance check: SUM(Debit opening) = SUM(Credit opening)
  2. Inventory check: SUM(item value) = SUM(inventory account balance)
  3. Debt check: SUM(receivable items) = SUM(131 account) per customer
  4. Duplicate check: No duplicate codes, no orphan references
  5. Generate validation report → user confirms before finalizing
```

### 21.4 Migration Safety

- All imports are **transactional**: success all or rollback all
- Preview mode: run import in dry-run → show what WOULD be created → user confirms
- Rollback: opening balance vouchers can be deleted before first real voucher is posted
- Idempotency: re-import with same data = no duplicates (checks ExternalRefId)

---

## 22. Print Templates & Report Engine

### 22.1 Voucher Print Templates

Every voucher type needs a printable form (A4/A5). Vietnamese accounting requires specific paper formats.

| Voucher Type | Print Name | Paper | Key Fields |
|--------------|-----------|-------|------------|
| Cash Receipt | Phiếu thu | A5 | Số, ngày, người nộp, lý do, số tiền (chữ + số) |
| Cash Payment | Phiếu chi | A5 | Số, ngày, người nhận, lý do, số tiền |
| Bank Deposit | Giấy báo Có | A5 | Tài khoản, số tiền, ngày |
| Bank Payment | Ủy nhiệm chi | A5 | Ngân hàng, TK, người nhận, số tiền |
| Sales Invoice | Phiếu xuất kho bán hàng | A4 | Khách hàng, tên hàng, SL, đơn giá, thành tiền |
| Purchase Invoice | Phiếu nhập mua | A4 | NCC, tên hàng, SL, đơn giá |
| Inventory Inward | Phiếu nhập kho | A4 | Kho, tên hàng, SL |
| Inventory Outward | Phiếu xuất kho | A4 | Kho, tên hàng, SL |
| Journal Entry | Phiếu kế toán | A4 | Nợ/Có, TK, số tiền |

### 22.2 Print Engine Architecture

```
Print Request
  → Load VoucherTemplate (JSON schema for print layout)
  → Load Voucher data (with details, tax, references)
  → QuestPDF renders to PDF
  → Return PDF stream (download or preview in browser)
```

**Template customization:**
- Each tenant can customize: company logo, header text, footer, signature blocks
- Print templates stored as JSON config (not code): add new layout without redeployment
- Support: PDF (primary), HTML preview (in-browser), Excel export

### 22.3 Financial Report Engine

| Report | Code | Standard | Output |
|--------|------|----------|--------|
| Bảng cân đối phát sinh (Trial Balance) | F01 / B01-DN/PS | Custom | PDF + Excel |
| Bảng cân đối kế toán (Balance Sheet) | B01-DN | TT99 Appendix | PDF + Excel |
| Báo cáo kết quả HĐKD (Income Statement) | B02-DN | TT99 Appendix | PDF + Excel |
| Báo cáo lưu chuyển tiền tệ — Trực tiếp (Cash Flow — Direct) | B03-DN | TT99 Appendix | PDF + Excel |
| Báo cáo lưu chuyển tiền tệ — Gián tiếp (Cash Flow — Indirect) | B03-DN/GT | TT99 Appendix | PDF + Excel |
| Thuyết minh BCTC (Notes) | B09-DN | TT99 Appendix | PDF + Excel |

**Report generation flow:**
```
Report Request (parameters: period, filters)
  → Check Redis cache → if hit, return cached
  → Dapper raw SQL with optimized query
  → FRReportTemplate (JSON schema: which accounts, formulas, groupings)
  → QuestPDF renders to PDF / ClosedXML renders to Excel
  → Cache result in Redis (TTL per report type)
  → Return
```

**Report template config (FRReportTemplate — JSON):**
```json
{
  "reportCode": "B02-DN",
  "title": "BÁO CÁO KẾT QUẢ HOẠT ĐỘNG KINH DOANH",
  "rows": [
    { "code": "01", "label": "Doanh thu bán hàng và cung cấp dịch vụ", "formula": "SUM(511*)", "bold": true },
    { "code": "02", "label": "Các khoản giảm trừ doanh thu", "formula": "SUM(521*)", "indent": 1 },
    { "code": "10", "label": "Doanh thu thuần", "formula": "R01 - R02", "bold": true }
  ]
}
```

### 22.4 Amount in Words (Số tiền bằng chữ)

Vietnamese accounting forms require amount written in words:
- `1.500.000 đ` → `Một triệu năm trăm nghìn đồng chẵn`
- `2.345.678 đ` → `Hai triệu ba trăm bốn mươi lăm nghìn sáu trăm bảy mươi tám đồng chẵn`
- Support Vietnamese + English for foreign currency vouchers
- Utility service: `IAmountInWordsService.Convert(decimal amount, string currencyCode, string language)`

**Edge cases (MUST handle):**
- Zero: `Không đồng` (not empty string)
- Negative amounts: `Âm [amount]` (e.g., `Âm một triệu đồng`)
- Decimal (foreign currency): `One hundred twenty-three dollars and forty-five cents` / `Một trăm hai mươi ba đô la Mỹ và bốn mươi lăm xu`
- "lăm" vs "năm": Vietnamese uses `lăm` (not `năm`) when the digit 5 is NOT in the ones place of thousands (e.g., 15 = `mười lăm`, 50 = `năm mươi`)
- "mười" vs "mười một" vs "mười mốt": `đồng mốt` for single digit after tens
- "linh" / "lẻ": Use `lẻ` for zero in middle (e.g., 101 = `một trăm lẻ một`)
- Maximum: support up to 999,999,999,999,999 (15 digits) — covers national budget scale
- Currency suffix: VND → `đồng`, USD → `đô la Mỹ`, EUR → `ơ-rô`, JPY → `yên Nhật`

---

## 23. MVP Scope & Phased Delivery

### 23.1 Phase 1: MVP

**Modules**: DI + GL + CA + BA + PU + SA + IN + SYS

| Deliverable | Description |
|-------------|-------------|
| Master Data | Account tree (TT99/TT133), AccountObjects, Inventory Items, Warehouses, Units, Currency |
| Voucher CRUD | Create/Edit/Delete/Print for all MVP module voucher types |
| Posting Engine | Config-driven, 8 ledgers, 100% unit test coverage |
| 6 Standard Reports | F01 (Trial Balance), B01-DN (Balance Sheet), B02-DN (Income Statement), B03-DN (Cash Flow — direct + indirect), B09-DN (Notes), B01-DN/PS (Bảng cân đối phát sinh) |
| Workflow Engine | Config-driven, 2 templates (Sales Order, Purchase Order) |
| Permission System | 4-layer RBAC, 13+ default roles |
| Dynamic Forms | JSON schema-driven voucher forms (from VoucherTemplate config) |
| Import/Export | CSV/Excel import for opening balances, Excel export for all lists |
| Dashboard | Revenue, expenses, cash flow KPIs |

**Phase 1 Assumptions:**
- **Team**: 1 full-stack developer (+ AI-assisted code generation)
- **Timeline**: Scope will be determined per feature spec — no fixed deadline. Quality over speed.
- **MVP criteria**: A single tenant can complete a full accounting cycle: opening balance → daily vouchers → posting → period close → 6 standard financial reports
- **NOT in Phase 1**: E-commerce integration, multi-branch, advanced analytics, mobile app, e-invoice, payroll

### 23.2 Phase 2

| Deliverable | Description |
|-------------|-------------|
| FA Module | Fixed asset management + depreciation |
| JC Module | Production Order + BOM + Job Costing (Level 2) |
| PA Module | Payroll, PIT, insurance |
| TA Module | VAT reports (deduction + direct), PIT annual reconciliation |
| E-Invoice | Integration with HĐĐT providers (ND123/ND51) |
| Advanced Reports | Debt aging, inventory aging, GL detail |
| Multi-price-list | SAPolicyPrice engine |
| Serial/Lot tracking | INSerialNumber + batch management |

### 23.3 Phase 3

| Deliverable | Description |
|-------------|-------------|
| E-commerce Integration | Full API bridge, RabbitMQ events, stock sync, order lifecycle |
| Elasticsearch | Advanced full-text search, faceting |
| Advanced Workflows | Period Close, Payment Approval, Budget Approval |
| CT Module | Contract management |
| EI Module | Import/export purchase/sales |
| Bank Integration | VCB/BIDV/Techcombank API auto-statement import |

### 23.4 Phase 4

| Deliverable | Description |
|-------------|-------------|
| MRP | Auto production planning from demand forecast |
| Mobile Native | iOS/Android app for approval workflows, dashboard |
| Advanced Analytics | BI dashboards, trend analysis, AI-powered insights |
| Multi-currency Advanced | Revaluation, multi-currency reconciliation |

---

## 24. Appendix: Key Business Rules

### 24.1 Registry

| Rule Code | Module | Description | Priority |
|-----------|--------|-------------|----------|
| BR-GL07 | GL | Posting engine is config-driven (SYSPostMapping), not hardcoded | NON-NEGOTIABLE |
| BR-GL08 | GL | Each detail line → 2 GeneralLedger rows (Debit + Credit) | NON-NEGOTIABLE |
| BR-GL09 | GL | Dual-book: Financial (0) + Management (1) + Both (2) | NON-NEGOTIABLE |
| BR-GL10 | GL | Decimal precision per tenant config | NON-NEGOTIABLE |
| BR-DI01 | DI | Account tree: child code starts with parent | CRITICAL |
| BR-DI02 | DI | Only leaf accounts (IsParent=0) in journal entries | CRITICAL |
| BR-DI03 | DI | DetailBy* flags control dimension tracking per account | CRITICAL |
| BR-DI04 | DI | AccountObject = unified Customer + Vendor + Employee | CRITICAL |
| BR-IN01 | IN | 4 costing methods: Weighted Avg, FIFO, Specific, Moving Avg | CRITICAL |
| BR-IN02 | IN | Anti-oversell: check QuantityAvailable, not QuantityOnHand | CRITICAL |
| BR-SA01 | SA | Price snapshot locked at order confirmation | NON-NEGOTIABLE |
| BR-SA02 | SA | Return creates reverse posting entries | CRITICAL |
| BR-SYS01 | SYS | SYSRefType = 262 voucher types (central registry) | CRITICAL |
| BR-SYS02 | SYS | Audit trail: every action logged | CRITICAL |
| BR-SYS03 | SYS | RBAC: 4-layer permission model | CRITICAL |
| BR-SYS04 | SYS | VoucherTemplate = metadata-driven form builder | CRITICAL |
| BR-SYS05 | SYS | Period close: HardClosed = absolutely no changes — no posting, no unposting, no creation, no edit, no delete of any voucher with ref_date in that period. Only Admin can revert to SoftClosed (audit-logged). | CRITICAL |
| BR-SYS06 | SYS | Voucher numbering: atomic, sequential per RefType per fiscal year | CRITICAL |
| BR-GL11 | GL | Opening balance must balance: SUM(Debit) = SUM(Credit) | CRITICAL |
| BR-GL12 | GL | Period close: auto-generate closing entries (5xx,6xx→911→421) | CRITICAL |
| BR-GL13 | GL | Foreign currency: store Amount (VND) + AmountOC + ExchangeRate + Operator per line | NON-NEGOTIABLE |
| BR-GL14 | GL | Period-end FX revaluation: auto-generate revaluation entries for TK 1112,1122,131,331→413 | CRITICAL |
| BR-CA01 | CA | AllowOverCashPayment: cannot pay more than cash balance (configurable per tenant) | CRITICAL |
| BR-CA02 | CA | CashBook posts separately alongside GeneralLedger (sổ quỹ riêng) | CRITICAL |
| BR-GL15 | GL | VoucherReference: every downstream voucher must link back to source (PO→GRN→Invoice→Payment) | CRITICAL |
| BR-GL16 | GL | Posting auto-validates: DebitAccount and CreditAccount must exist in Account table, status = Active, is_parent = false (leaf only) | NON-NEGOTIABLE |
| BR-GL17 | GL | Cannot post voucher with ref_date before fiscal year start or after fiscal year end | NON-NEGOTIABLE |
| BR-GL18 | GL | Cannot post voucher into future date (ref_date <= today) | CRITICAL |
| BR-DI05 | DI | Cannot delete Account with posted GL entries | NON-NEGOTIABLE |
| BR-DI06 | DI | Cannot delete InventoryItem with posted transactions | NON-NEGOTIABLE |
| BR-DI07 | DI | Cannot delete AccountObject with outstanding debt balance | CRITICAL |
| BR-GL19 | GL | Opening balance cannot be modified after first real voucher is posted in that fiscal year | CRITICAL |
| BR-SYS07 | SYS | Optimistic locking (RowVersion) on Voucher + VoucherDetail — reject stale writes | NON-NEGOTIABLE |

### 24.2 Accounting Standards Reference

| Standard | Scope | Status |
|----------|-------|--------|
| Thông tư 99/2025/TT-BTC | Enterprise accounting regime | PRIMARY (replaces TT200) |
| Thông tư 133/2016/TT-BTC | SME accounting regime | ACTIVE (simpler chart of accounts) |
| Nghị định 123/2020/NĐ-CP | E-Invoice regulations | ACTIVE |
| Nghị định 51/2010/NĐ-CP | E-Invoice (legacy) | ACTIVE (transition) |

### 24.3 Technology Constraints

| Constraint | Detail |
|------------|--------|
| Development OS | Windows (primary) |
| UI Language | Vietnamese (UTF-8) |
| Code Language | English (variables, comments, API names, DB columns) |
| Domain terms | Vietnamese accounting terms translated to English (Voucher, PostingEntry, AccountObject) |
| Error messages | Both `code` and `message` fields — Vietnamese for user-facing |

---

**Document End**  
**Version**: 1.2.0 | **Date**: 2026-04-15
**Source**: Constitution v2.0.0 + all design discussions + final review + deep review corrections
**Next**: Create feature specifications via `/speckit.specify`

---

## Changelog

### v1.2.0 (Current)
**Deep Review Corrections — 25+ fixes applied:**
- §4.3: Added Domain Event capture pattern (BaseEntity._domainEvents) + Outbox retry policy (exponential backoff, dead-letter)
- §5.4: Added Soft Delete Policy table (financial = soft only, transient = hard delete via job)
- §6.2: Added 17 MVP RefTypes detail table (PT, PC, BC, BN, PO, PI, PR, SO, SI, SR, NK, XK, CK, PC_GL, KC, FX_REVAL, OB)
- §6.3: Added VoucherTemplate JSON schema example (full tabs/columns/rules structure)
- §9.4: Added Data Scope Injection mechanism (IQueryable interceptor + IDapperContext)
- §9.5: Added Field Visibility Enforcement (API-level DTO projection + UI-level AG Grid + strip hidden fields)
- §12.1: Added L1 cache fallback guarantee (max 5 min stale even without Redis)
- §16.2: Added DB connection pool justification (300 users ÷ 3:1 multiplexing)
- §17.2: Clarified Period Close authorization (ChiefAccountant initiates → DRAFT entries → review → approve → post)
- §17.6: Expanded ExchangeRateOperator with full currency list (multiply = all standard; divide = legacy import only)
- §22.4: Added Amount-in-Words edge cases (zero, negative, decimal, lăm/năm/lẻ Vietnamese rules)
- §23.1: Added Phase 1 assumptions (1 dev + AI, no fixed deadline, quality over speed)
- §24.1: Fixed duplicate BR-GL16, clarified BR-SYS05
- sys_db_option: Added full schema with tenant_id + 8 MVP option keys

### v1.1.0
- §7.2-7.7: Ledger registry, decimal types, SYSPostMapping schema, idempotency, Dapper safety
- §10.4-10.5: MV refresh safety, Vietnamese FTS config
- §11.2: RowVersion on VoucherDetail
- §12.3/22.3/23.1: Fixed report codes (F01, B01-DN, B03-DN)
- §15.1: Session revocation mechanism
- §17.6-17.7: FX handling, Debt aging
- §20.4/21.2/24.1: Test scenarios, import templates, 8 new business rules
- §6.1: VoucherReference table

### v1.0.0
- Initial consolidated report (24 sections)
