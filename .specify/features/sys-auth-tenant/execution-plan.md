# Execution Plan: Migration Single-DB → Dual-DB (Phương án B Kết hợp)

> Created: 2026-04-16
> Status: **CHỜ PHÊ DUYỆT** — Không thực hiện bất kỳ thay đổi nào cho đến khi user phê duyệt
> Reference: `/memories/repo/architecture-plan-b-combined.md`, `/memories/repo/migration-audit-report.md`

---

## MỤC LỤC

- [PHẦN A: SỬA TÀI LIỆU](#phần-a-sửa-tài-liệu-15-files--trước-khi-code)
- [PHẦN B: SỬA CODE](#phần-b-sửa-code--sau-khi-tài-liệu-approved)
- [PHỤ LỤC: Ma trận Cross-Reference](#phụ-lục-ma-trận-cross-reference)

---

## PHẦN A: SỬA TÀI LIỆU (15 files — Trước khi code)

### Thứ tự thực hiện & dependency

```
Group 1 (nền tảng — làm đầu, các file khác depend vào):
  ① data-model.md          → ER diagram mới, bảng schema mới
  ② auth-api.md             → API contracts cho 2-step login

Group 2 (spec + plan — cần Group 1 xong):
  ③ spec.md                 → User stories + FRs cập nhật
  ④ plan.md                 → Implementation plan mới

Group 3 (architecture docs — parallel với Group 2):
  ⑤ architecture-technology-report.md  → Sections cần rewrite
  ⑥ constitution.md                    → Nguyên tắc multi-tenant

Group 4 (dependent docs — sau Group 1-3):
  ⑦ me-api.md               → Response + behavior changes
  ⑧ users-api.md            → Side-effect notes
  ⑨ quality-checklist.md    → Thêm checklist items

Group 5 (low-priority — cuối cùng):
  ⑩ roles-permissions-api.md       → Scope note
  ⑪ ecom-integration-architecture.md  → Tenant context note
  ⑫ ecom-readiness-checklist.md    → Scope note
  ⑬ copilot-instructions.md        → Project context note
  ⑭ misa-architecture-lessons.md   → Multi-tenant row update
  ⑮ tasks.md                       → Phase 2 tasks section
```

---

### ① `data-model.md` — REWRITE (Ưu tiên: RẤT CAO)

**File**: `.specify/features/sys-auth-tenant/data-model.md`
**Effort**: Lớn (~300 dòng rewrite)

| # | Section hiện tại | Hành động | Chi tiết thay đổi |
|---|-----------------|-----------|-------------------|
| 1 | ER Diagram (Mermaid) | **REWRITE** | Tách thành **2 diagram riêng**: Master DB ER + Tenant DB ER. Master DB: MasterUser, MasterUserTenant, Tenant (extended), RefreshToken (with TenantId). Tenant DB: User (no PasswordHash), Role, Permission, UserRole, RolePermission. Thêm cross-DB convention arrow (dashed): MasterUser.Id ↔ User.Id |
| 2 | PostgreSQL Tables — `sys_tenants` | **SỬA** | Thêm 5 columns: `database_mode` (varchar(20), NOT NULL, default 'CloudManaged'), `encrypted_connection_string` (text, NULL), `tunnel_hostname` (varchar(200), NULL), `cloud_database_name` (varchar(100), NULL), `db_status` (varchar(20), NOT NULL, default 'Online'). Thêm indexes: `idx_sys_tenants_db_status` |
| 3 | PostgreSQL Tables — `sys_users` | **SỬA** | `password_hash` → nullable (deprecated, note: "Password stored in Master DB sys_master_users"). Remove from required columns note. |
| 4 | (NEW) PostgreSQL Tables — `sys_master_users` | **THÊM** | Table mới trong Master DB section: id (uuid PK), email (varchar(256), UNIQUE), password_hash (varchar(100), NOT NULL), full_name (varchar(200), NOT NULL), is_active (bool, default true), last_login_at (timestamptz, NULL), failed_login_count (int, default 0), locked_until (timestamptz, NULL), created_at (timestamptz, NOT NULL). Index: `idx_sys_master_users_email` UNIQUE |
| 5 | (NEW) PostgreSQL Tables — `sys_master_user_tenants` | **THÊM** | Table mới: user_id (uuid, FK→sys_master_users.id), tenant_id (uuid, FK→sys_tenants.id), display_role_name (varchar(100), NULL), is_default (bool, default false), granted_at (timestamptz, NOT NULL), granted_by (uuid, NULL). PK: (user_id, tenant_id). Index: `idx_sys_mut_tenant_id` on tenant_id |
| 6 | PostgreSQL Tables — `sys_refresh_tokens` | **SỬA** | Thêm `tenant_id` (uuid, NOT NULL). Di chuyển từ Tenant DB section → Master DB section. Note: "FK user_id references sys_master_users.id (NOT sys_users.id)". Thêm index: `idx_sys_refresh_tokens_tenant_id` |
| 7 | Index Strategy Summary | **CẬP NHẬT** | Thêm Master DB indexes. Tách bảng thành 2: Master DB indexes + Tenant DB indexes |
| 8 | EF Core Configuration Notes | **REWRITE** | (a) Naming: vẫn snake_case, (b) Global Query Filters: chỉ áp dụng trong ApplicationDbContext (User, Role). MasterDbContext KHÔNG có filters, (c) Thêm section: "Dual DbContext" — MasterDbContext (fixed connection), ApplicationDbContext (per-tenant connection via factory), (d) Cross-DB convention: MasterUser.Id = User.Id (same GUID, no FK constraint) |
| 9 | Cascade Delete Rules | **CẬP NHẬT** | Thêm: MasterUser → RefreshToken (CASCADE), MasterUser → MasterUserTenant (CASCADE). SỬA: User → RefreshToken → XÓA (RefreshToken không còn trong Tenant DB) |
| 10 | Cross-Tenant Consistency | **REWRITE** | Cũ: "UserRole.UserId.TenantId must equal UserRole.RoleId.TenantId". Mới: thêm cross-DB consistency rules: "CreateUser must create/link MasterUser in Master DB. ChangePassword updates MasterUser, not User." |

---

### ② `auth-api.md` — REWRITE (Ưu tiên: RẤT CAO)

**File**: `.specify/features/sys-auth-tenant/contracts/auth-api.md`
**Effort**: Lớn (~250 dòng rewrite)

| # | Section hiện tại | Hành động | Chi tiết thay đổi |
|---|-----------------|-----------|-------------------|
| 1 | `POST /api/auth/login` | **REWRITE** | **Request**: giữ nguyên (email, password, rememberMe). **Response 200**: đổi hoàn toàn → `{ tempToken: string, tempTokenExpiresIn: 60, companies: CompanyInfo[] }`. CompanyInfo = `{ tenantId, name, code, databaseMode, dbStatus, displayRole, isDefault }`. **Side effects**: KHÔNG set refresh_token cookie (chưa chọn company). MasterUser.LastLoginAt updated, FailedLoginCount reset. **Error responses**: giữ 400/401/429. XÓA 403 TENANT_INACTIVE và TENANT_NOT_FOUND (không còn X-Tenant-Code header requirement cho login). Thêm 401 `NO_COMPANIES` khi user không có company nào active. |
| 2 | (NEW) `POST /api/auth/select-company` | **THÊM** | Endpoint mới. **Auth**: None (uses tempToken in body, NOT Bearer). **Request**: `{ tempToken: string, tenantId: uuid }`. **Response 200**: `{ accessToken, expiresIn: 900, tokenType: "Bearer", user: { id, email, fullName, tenantId, tenantName, roles, permissions } }`. **Side effects**: Set HttpOnly refresh_token cookie (Max-Age from original rememberMe). Resolve tenant DB → load user roles/permissions. **Errors**: 400 VALIDATION_ERROR, 401 TEMP_TOKEN_EXPIRED (TTL 60s), 401 TEMP_TOKEN_INVALID, 403 COMPANY_NOT_ACCESSIBLE (user not mapped to this tenant), 403 TENANT_INACTIVE, 503 TENANT_DB_OFFLINE (on-premise tunnel down). |
| 3 | (NEW) `POST /api/auth/switch-company` | **THÊM** | Endpoint mới. **Auth**: Bearer JWT (required). **Request**: `{ tenantId: uuid }`. **Response 200**: `{ accessToken, expiresIn: 900, tokenType: "Bearer", user: { id, email, fullName, tenantId, tenantName, roles, permissions } }`. **Side effects**: Previous JWT remains valid until expiry (no blacklist). New refresh_token cookie set (old revoked). **Errors**: 401 UNAUTHORIZED, 403 COMPANY_NOT_ACCESSIBLE, 403 TENANT_INACTIVE, 503 TENANT_DB_OFFLINE. |
| 4 | `POST /api/auth/logout` | **SỬA NHỎ** | Logic giữ nguyên. Note thêm: "RefreshToken revoked in Master DB (not Tenant DB)". |
| 5 | `POST /api/auth/refresh` | **SỬA** | Note thêm: "RefreshToken read from Master DB. User claims (roles, permissions) loaded from Tenant DB resolved via token.TenantId." Response giữ nguyên. Thêm error: 503 TENANT_DB_OFFLINE. |
| 6 | JWT Access Token Claims | **SỬA NHỎ** | Giữ nguyên tất cả claims. Thêm note: "`tid` claim determines which Tenant DB to connect to for subsequent API requests." |
| 7 | JWKS endpoint | **GIỮ** | Không thay đổi |

---

### ③ `spec.md` — REWRITE (Ưu tiên: RẤT CAO)

**File**: `.specify/features/sys-auth-tenant/spec.md`
**Effort**: Lớn (~200 dòng thay đổi)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | US1 (Login) | **REWRITE** | Cũ: "Khi tôi nhập email/mật khẩu → nhận JWT". Mới: "Khi tôi nhập email/mật khẩu → thấy danh sách công ty → chọn công ty → nhận JWT". Thêm acceptance criteria: (a) 2-step flow, (b) TempToken TTL 60s, (c) Auto-select nếu chỉ 1 company, (d) Company list hiển thị name, code, databaseMode, dbStatus, displayRole |
| 2 | (NEW) US1b (Company Selection) | **THÊM** | "Sau khi login thành công, tôi muốn chọn công ty để làm việc". AC: (a) Hiển thị list companies từ master_user_tenants, (b) Default company highlighted, (c) Offline company hiển thị disabled + badge, (d) Click company → JWT issued → redirect /dashboard |
| 3 | (NEW) US1c (Company Switching) | **THÊM** | "Khi đang làm việc, tôi muốn chuyển sang công ty khác mà không cần đăng nhập lại". AC: (a) Header dropdown, (b) Click company → new JWT, (c) Dashboard reload, (d) Unsaved changes warning |
| 4 | US2 (Token Refresh) | **SỬA** | Thêm AC: "RefreshToken stored in Master DB with TenantId. Refresh loads permissions from correct Tenant DB." |
| 5 | US3 (Tenant Isolation) | **REWRITE** | Cũ: "Single-DB + TenantId column isolation". Mới: "Dual-DB isolation: Master DB cho auth, Tenant DB cho accounting data. Mỗi company = 1 DB riêng. Cloud hoặc On-Premise via Cloudflare Tunnel." |
| 6 | US4 (User Management) | **SỬA** | Thêm AC: "CreateUser also creates/links MasterUser in Master DB. Password set in Master DB only." |
| 7 | US5 (Role Management) | **GIỮ** | Roles hoàn toàn trong Tenant DB — không thay đổi |
| 8 | US6 (Profile) | **SỬA** | ChangePassword AC: "Password cập nhật trong Master DB. Áp dụng cho tất cả companies." |
| 9 | FR-001 → FR-010 (Auth) | **SỬA** | FR-001: thêm "Step 1: verify in Master DB". FR-002: thay "trả JWT" → "trả tempToken + companies". Thêm FR mới: FR-002b (select-company), FR-002c (switch-company), FR-002d (auto-select khi 1 company). |
| 10 | FR-011 → FR-020 (Isolation) | **REWRITE** | FR-011: "Each company = separate PostgreSQL database". FR-012: "Master DB stores auth, tokens, tenant registry". FR-013: "Cloud DB for small customers, On-Premise via Cloudflare Tunnel for enterprise". Cập nhật tenant isolation model. |
| 11 | FR-021 → FR-030 (Users) | **SỬA** | FR-021: thêm "also creates MasterUser + MasterUserTenant mapping". FR-025 (ChangePassword): "updates MasterUser.PasswordHash in Master DB". |
| 12 | Entities section | **REWRITE** | Thêm: MasterUser, MasterUserTenant entities. Tách "Which DB": Master DB entities vs Tenant DB entities. |
| 13 | Assumptions | **SỬA** | Cũ: "Shared-database, shared-schema multi-tenant". Mới: "Dual-DB: Master DB (cloud) + Tenant DB (dedicated per-tenant, cloud or on-premise via Cloudflare Tunnel)". XÓA: "EF Core global query filters (ITenantEntity)" cho tenant isolation context (vẫn giữ cho User/Role within Tenant DB). |

---

### ④ `plan.md` — REWRITE (Ưu tiên: RẤT CAO)

**File**: `.specify/features/sys-auth-tenant/plan.md`
**Effort**: Rất lớn (~500 dòng rewrite)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | Technical Approach paragraph 1 (JWT) | **SỬA NHỎ** | Giữ RS256 explanation. Thêm: "TempToken is a short-lived (60s) JWT with minimal claims (sub only) used during company selection step." |
| 2 | Technical Approach paragraph 2 (Multi-tenant) | **REWRITE** | Cũ: "Multi-tenant isolation via EF Core global query filter on single AppDbContext". Mới: "Dual-DB architecture: MasterDbContext (fixed connection, no tenant filters) manages central auth data. ApplicationDbContext (per-request connection via TenantDbContextFactory) manages tenant-specific accounting data with EF Core global query filters on User and Role." |
| 3 | Technical Approach paragraph 3 (Angular) | **SỬA** | Thêm: "Login now follows 2-step flow: email+password → company selection. Auth store manages tempToken + companies state between steps." |
| 4 | NuGet Packages | **SỬA** | Thêm: `Microsoft.AspNetCore.DataProtection` (connection string encryption), `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (key storage). |
| 5 | npm Packages | **GIỮ** | Không thay đổi |
| 6 | Phase 1: Domain Layer | **REWRITE** | Thêm: MasterUser.cs, MasterUserTenant.cs, DatabaseMode.cs, TenantDbStatus.cs. SỬA: Tenant.cs (+5 fields), User.cs (PasswordHash nullable), RefreshToken.cs (+TenantId). |
| 7 | Phase 2: Infrastructure — Persistence | **REWRITE** | Tách thành: (a) MasterDbContext (NEW) + MasterUser/MasterUserTenant configs (NEW), (b) ApplicationDbContext (MODIFY — remove Tenant, RefreshToken; per-request connection), (c) TenantDbContextFactory (NEW). Migration folders tách: Migrations/Master/ + Migrations/Tenant/. |
| 8 | Phase 3: Infrastructure — Services | **REWRITE** | Thêm: TenantConnectionResolver (NEW), DataProtectionEncryptor (NEW). SỬA: TenantRepository (inject MasterDbContext), ExpiredTokenCleanupService (inject MasterDbContext), DependencyInjection.cs (register dual DbContext + new services). |
| 9 | Phase 4: Application Layer — CQRS | **REWRITE** | Auth: LoginCommand output đổi (tempToken + companies). Thêm SelectCompanyCommand, SwitchCompanyCommand. SỬA: RefreshTokenCommand, LogoutCommand (use IMasterDbContext). Users: SỬA CreateUser (dual-context), ChangePassword (MasterUser), ToggleActivation (Master revoke). |
| 10 | Phase 5: API Layer | **SỬA** | AuthController: thêm /select-company, /switch-company. Thêm TenantsController (NEW). Fix CurrentUserService ("tid"). Program.cs: DataProtection services. |
| 11 | Phase 6: Angular Frontend | **REWRITE** | Thêm: CompanySelectComponent (NEW). SỬA: auth.models.ts (new types), auth.service.ts (new methods), auth.store.ts (2-step state), app.routes.ts (/select-company), shell.component.ts (company switcher). |
| 12 | Testing Strategy | **SỬA** | Thêm test scenarios: (a) 2-step login flow, (b) company switching, (c) dual-DB handler tests (mock both contexts), (d) tunnel offline handling, (e) MasterUser creation during CreateUser |

---

### ⑤ `architecture-technology-report.md` — REWRITE SECTIONS (Ưu tiên: RẤT CAO)

**File**: `.specify/memory/architecture-technology-report.md`
**Effort**: Rất lớn (6 sections cần sửa, ~400 dòng)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | §2.1 System Overview Diagram | **SỬA** | Diagram hiện tại chỉ có 1 AppDbContext. Cần cập nhật: 2 DbContext boxes (MasterDbContext → Master DB, ApplicationDbContext → Tenant DB), connection resolution flow via TenantConnectionResolver. |
| 2 | §4.4 Project Structure | **SỬA** | Thêm: `Persistence/MasterDbContext.cs`, `Persistence/TenantDbContextFactory.cs`, `Services/TenantConnectionResolver.cs`, `Services/DataProtectionEncryptor.cs` trong tree. |
| 3 | §5.1 Multi-Tenant Strategy | **REWRITE** | Cũ: "Shared Database + TenantId Column". Mới: "Dual-DB Dedicated Per-Tenant". Content: (a) Master DB schema, (b) Tenant DB schema, (c) Connection resolution (cloud vs tunnel), (d) Cloudflare Tunnel architecture, (e) Encryption. GIỮ: ID strategy (§5.2), core tables (§5.3), audit fields (§5.4), partitioning (§5.5) — tất cả vẫn đúng cho Tenant DB. |
| 4 | §9 Permission & Authorization | **SỬA** | §9.2: Cũ "4-Layer Permission Model". Thêm Layer 0: "Master-level access control (MasterUserTenant: which companies a user can access)". Tầng 1–4 giữ nguyên nhưng note: "Applied within Tenant DB scope". |
| 5 | §15 Security Architecture | **SỬA** | §15.1 JWT Auth: thêm TempToken concept, 2-step flow. §15.4 Integration Security: thêm Cloudflare Tunnel, connection string encryption. |
| 6 | §23 MVP Scope | **SỬA** | Phase 1 MVP: thêm "Dual-DB architecture, 2-step login, company selection". Ghi note: "Current single-DB implementation is Phase 1a; Dual-DB migration is Phase 1b." |
| 7 | Changelog | **THÊM** | v1.3.0: "Dual-DB architecture (Phương án B Kết hợp) — rewrote §5.1, updated §2.1, §9, §15, §23" |

**KHÔNG SỬA** (vẫn đúng cho Tenant DB):
- §3 Tech Stack, §5.2 ID Strategy, §5.3 Core Tables (Unified Voucher), §5.4 Audit Fields, §5.5 Partitioning
- §6 Domain Model (Unified Voucher, Master Data, Metadata-Driven Config, Analysis Dimensions)
- §7 Posting Engine (all 8 sections — posting operates within Tenant DB)
- §8 Workflow Engine
- §10 Search & Query, §11 Concurrency, §12 Caching, §13 E-Commerce
- §14 Infrastructure, §16 Performance, §17 Fiscal Year, §18 Localization
- §19 Error Handling, §20 Testing Strategy, §21 Data Migration, §22 Print Templates
- §24 Business Rules

---

### ⑥ `constitution.md` — SỬA (Ưu tiên: CAO)

**File**: `.specify/memory/constitution.md`
**Effort**: Trung bình (~50 dòng thay đổi)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | Architecture Decisions → "Multi-Tenant Strategy" | **REWRITE** | Cũ: "Shared DB + TenantID. All tenants share single database with TenantId column." Mới: "Dual-DB Dedicated Per-Tenant. Master DB (always cloud) stores auth, tenant registry, tokens. Tenant DB (cloud or on-premise via Cloudflare Tunnel) stores accounting data. Each company = 1 separate database. EF Core global query filters (ITenantEntity) still applied within Tenant DB for User and Role entities." |
| 2 | Architecture Decisions → (NEW) "Connection Management" | **THÊM** | "Tenant DB connections resolved per-request via ITenantConnectionResolver. Connection strings encrypted with ASP.NET Data Protection API (Phase 1) → KMS (Phase 2). On-premise connections via Cloudflare Tunnel (outbound-only, port 443)." |
| 3 | Architecture Decisions → (NEW) "Cross-DB Identity" | **THÊM** | "MasterUser.Id = User.Id (same GUID in both databases, no FK constraint). Password stored in Master DB only. Creating a user in Tenant DB must create/link corresponding MasterUser." |
| 4 | Technology Stack → Backend section | **SỬA NHỎ** | Note: "Dual DbContext: MasterDbContext (central auth) + ApplicationDbContext (per-tenant accounting)" |
| 5 | Domain Constraints | **GIỮ** | Accounting standards, tax, decimals — không liên quan DB architecture |

---

### ⑦ `me-api.md` — SỬA (Ưu tiên: CAO)

**File**: `.specify/features/sys-auth-tenant/contracts/me-api.md`
**Effort**: Nhỏ (~30 dòng thay đổi)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | `GET /api/me` Response | **SỬA** | Thêm fields: `companies: CompanyInfo[]` (list all accessible companies), `currentCompany: { tenantId, name, code, databaseMode }`. CompanyInfo reuse from auth-api. |
| 2 | `PUT /api/me/password` | **SỬA** | Thêm note: "Password updated in Master DB (MasterUser), NOT in Tenant DB (User). Change applies to all companies." Side effect thêm: "All RefreshTokens across ALL tenants for this user are revoked (via Master DB)." |
| 3 | `PUT /api/me/profile` | **SỬA NHỎ** | Thêm note: "fullName updated in current Tenant DB only. Other companies may show the original name. Future: sync fullName to MasterUser for consistency." |

---

### ⑧ `users-api.md` — SỬA (Ưu tiên: TRUNG BÌNH)

**File**: `.specify/features/sys-auth-tenant/contracts/users-api.md`
**Effort**: Nhỏ (~20 dòng thay đổi)

| # | Section | Hành động | Chi tiết thay đổi |
|---|---------|-----------|-------------------|
| 1 | `POST /api/users` (Create) | **SỬA** | Thêm side-effect note: "Also creates MasterUser in Master DB (if email is new) or links existing MasterUser via master_user_tenants (if email already exists in another company). Password is set in Master DB." Thêm error: 503 `MASTER_DB_UNAVAILABLE`. |
| 2 | `POST /api/users/{id}/deactivate` | **SỬA** | Thêm side-effect note: "RefreshTokens revoked in Master DB for this tenant only (tokens with matching user_id + tenant_id)." |
| 3 | `PUT /api/users/{id}` (Update) | **SỬA NHỎ** | Note: "Email change also updates MasterUser.Email in Master DB if this is the user's only company, or flags for manual review if multi-company." |

---

### ⑨ `quality-checklist.md` — THÊM (Ưu tiên: CAO)

**File**: `.specify/features/sys-auth-tenant/checklists/quality-checklist.md`
**Effort**: Trung bình (~40 dòng thêm)

**Thêm section mới: "9. Dual-DB Architecture (CHK082–CHK095)"**

| ID | Check |
|----|-------|
| CHK082 | MasterDbContext registered with fixed "MasterConnection" connection string — no per-tenant variation |
| CHK083 | ApplicationDbContext registered via TenantDbContextFactory with per-request connection resolved from ITenantConnectionResolver |
| CHK084 | Master DB migration folder separate: `Migrations/Master/` |
| CHK085 | Tenant DB migration folder separate: `Migrations/Tenant/` |
| CHK086 | Connection string encryption via IConnectionStringEncryptor (DataProtection API) — never stored in plaintext |
| CHK087 | TempToken: minimal claims (sub only), TTL 60s, separate signing key or nonce-based |
| CHK088 | Cloudflare Tunnel health: `db_status` updated, offline tenants show disabled in company list |
| CHK089 | Cross-DB identity: MasterUser.Id = User.Id (same GUID), enforced in CreateUserCommandHandler |
| CHK090 | ChangePasswordCommandHandler updates MasterUser, NOT tenant User entity |
| CHK091 | CreateUserCommandHandler: dual-context operation — MasterUser in Master DB + User in Tenant DB |
| CHK092 | RefreshToken.TenantId populated on creation, used during refresh to resolve correct Tenant DB |
| CHK093 | 2-step login: no JWT issued at Step 1, only tempToken. JWT issued at Step 2 (select-company) |
| CHK094 | Company switch: verify MasterUserTenant access before issuing new JWT |
| CHK095 | Auto-select: if companies.length === 1, frontend auto-calls select-company (no manual step) |

**Update Summary table**: Total → 95 items, [Gap] → unchanged + 14 new

---

### ⑩ `roles-permissions-api.md` — SỬA NHỎ (Ưu tiên: THẤP)

**File**: `.specify/features/sys-auth-tenant/contracts/roles-permissions-api.md`
**Effort**: Rất nhỏ (1 note)

| # | Thay đổi |
|---|----------|
| 1 | Thêm header note: "All endpoints operate within the current Tenant DB (scoped by JWT `tid` claim). Roles and permissions are fully tenant-specific — not shared across companies." |

---

### ⑪ `ecom-integration-architecture.md` — SỬA NHỎ (Ưu tiên: THẤP)

**File**: `.specify/memory/ecom-integration-architecture.md`
**Effort**: Nhỏ (~10 dòng)

| # | Thay đổi |
|---|----------|
| 1 | Thêm note ở Architecture Pattern section: "E-commerce integration connects to a specific Tenant DB (not Master DB). API calls must include `X-Tenant-Code` header or use JWT with `tid` claim to resolve the correct accounting database." |
| 2 | Note: "Stock hold/reserve/release APIs operate against the Tenant DB for the specific company's inventory." |

---

### ⑫ `ecom-readiness-checklist.md` — SỬA NHỎ (Ưu tiên: THẤP)

**File**: `.specify/memory/ecom-readiness-checklist.md`
**Effort**: Rất nhỏ (1 note)

| # | Thay đổi |
|---|----------|
| 1 | Thêm header note: "All readiness items apply to the Tenant DB (per-company database), not the Master DB." |

---

### ⑬ `copilot-instructions.md` — SỬA NHỎ (Ưu tiên: THẤP)

**File**: `.github/copilot-instructions.md`
**Effort**: Rất nhỏ (2 dòng)

| # | Thay đổi |
|---|----------|
| 1 | Section "Project Context": Thêm dòng: "Backend uses dual DbContext: MasterDbContext (central auth, tenant registry) + ApplicationDbContext (per-tenant accounting data via TenantDbContextFactory)." |

---

### ⑭ `misa-architecture-lessons.md` — SỬA NHỎ (Ưu tiên: THẤP)

**File**: `/memories/repo/misa-architecture-lessons.md`
**Effort**: Rất nhỏ (1 row update)

| # | Thay đổi |
|---|----------|
| 1 | Architecture comparison table: row "Multi-tenant" → Cũ: "Schema-per-tenant or shared + TenantID". Mới: "Dual-DB: Master DB (cloud) + Tenant DB (dedicated per-company, cloud or on-premise via Cloudflare Tunnel)" |

---

### ⑮ `tasks.md` — THÊM (Ưu tiên: TRUNG BÌNH)

**File**: `.specify/features/sys-auth-tenant/tasks.md`
**Effort**: Lớn (~200 dòng thêm)

| # | Thay đổi |
|---|----------|
| 1 | KHÔNG xóa existing 109 tasks (Phase 1 single-DB — all completed) |
| 2 | Thêm section: "## Phase 2: Dual-DB Migration (Phương án B Kết hợp)" |
| 3 | Tasks sẽ follow 10 phases từ `/memories/repo/architecture-plan-b-combined.md` §6 |
| 4 | Mỗi phase → nhóm tasks chi tiết với file path, hành động cụ thể |
| 5 | Tasks numbered: T2-001 through T2-xxx (separate from Phase 1 numbering) |

---

## PHẦN B: SỬA CODE (Sau khi tài liệu approved)

> **CRITICAL**: Không thực hiện cho đến khi user phê duyệt PHẦN A

### Tổng quan theo Phase

```
Phase 0: Bug Fixes                    ← 3 files, fix ngay
Phase 1: Domain Layer                 ← 6 files (2 new + 4 modify)
Phase 2: Application Interfaces       ← 6 files (4 new + 2 modify)
Phase 3: Infrastructure Master DB     ← 10 files (7 new + 3 modify)
Phase 4: Infrastructure Updates       ← 3 files (modify)
Phase 5: Auth Handlers Rewrite        ← 9 files (4 new + 5 modify)
Phase 6: User Handlers Update         ← 4 files (modify)
Phase 7: API Layer                    ← 6 files (1 new + 5 modify)
Phase 8: Database Migrations          ← 4 migration files
Phase 9: Frontend                     ← 8 files (1 new + 7 modify)
Phase 10: Config & Docker             ← 3 files (modify)
Phase 11: E2E Verification            ← testing
────────────────────────────────────────────
Total: ~19 new files + ~27 modified files
```

### Phase 0: Bug Fixes (3 files)

| # | File | Thay đổi | LOC |
|---|------|----------|-----|
| B1 | `Api/Services/CurrentUserService.cs` | `"tenant_id"` → `"tid"` | 1 |
| B2 | `Infrastructure/Persistence/Migrations/*SeedSuperAdmin.cs` | Tạo migration mới UPDATE hash HOẶC fix inline (cần verify hash trước) | ~10 |
| B3 | `Domain/Entities/RefreshToken.cs` | Thêm `public Guid TenantId { get; set; }` | 1 |

**Verify**: `dotnet build`, `get_errors`

---

### Phase 1: Domain Layer (6 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| D1 | `Domain/Enums/DatabaseMode.cs` | **NEW** | `public enum DatabaseMode { CloudManaged, OnPremise }` | ~5 |
| D2 | `Domain/Enums/TenantDbStatus.cs` | **NEW** | `public enum TenantDbStatus { Online, Offline, Provisioning, Migrating }` | ~5 |
| D3 | `Domain/Entities/MasterUser.cs` | **NEW** | Entity: Id (Guid), Email, PasswordHash, FullName, IsActive, LastLoginAt, FailedLoginCount, LockedUntil, CreatedAt. Navigation: MasterUserTenants. Inherits BaseEntity. | ~35 |
| D4 | `Domain/Entities/MasterUserTenant.cs` | **NEW** | Entity: UserId, TenantId, DisplayRoleName, IsDefault, GrantedAt, GrantedBy. Navigation: MasterUser, Tenant. No inheritance (join entity). | ~25 |
| D5 | `Domain/Entities/Tenant.cs` | **MODIFY** | Thêm: `DatabaseMode DatabaseMode`, `string? EncryptedConnectionString`, `string? TunnelHostname`, `string? CloudDatabaseName`, `TenantDbStatus DbStatus` | ~10 |
| D6 | `Domain/Entities/User.cs` | **MODIFY** | `PasswordHash` type: `string` → `string?` (nullable). Thêm comment: "// Deprecated: password stored in MasterUser. Kept for backward compatibility." | ~3 |

**Verify**: `dotnet build`

---

### Phase 2: Application Interfaces (6 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| A1 | `Application/Common/Interfaces/IMasterDbContext.cs` | **NEW** | Interface: `DbSet<Tenant>`, `DbSet<MasterUser>`, `DbSet<MasterUserTenant>`, `DbSet<RefreshToken>`, `SaveChangesAsync()` | ~15 |
| A2 | `Application/Common/Interfaces/IApplicationDbContext.cs` | **MODIFY** | XÓA: `DbSet<Tenant> Tenants`, `DbSet<RefreshToken> RefreshTokens` | -2 |
| A3 | `Application/Common/Interfaces/ITenantConnectionResolver.cs` | **NEW** | `Task<string> ResolveConnectionStringAsync(Guid tenantId, CancellationToken ct)` | ~8 |
| A4 | `Application/Common/Interfaces/IConnectionStringEncryptor.cs` | **NEW** | `string Encrypt(string plaintext)`, `string Decrypt(string ciphertext)` | ~8 |
| A5 | `Application/Common/Interfaces/ITenantRepository.cs` | **MODIFY** | TenantDto thêm: `DatabaseMode`, `TenantDbStatus`, `string? EncryptedConnectionString`, `string? TunnelHostname`, `string? CloudDatabaseName` | ~10 |
| A6 | `Application/Common/Interfaces/IJwtService.cs` | **MODIFY** | Thêm: `string GenerateTempToken(Guid userId, TimeSpan? ttl = null)`, `Guid? ValidateTempToken(string tempToken)` | ~5 |

**Verify**: `dotnet build` (sẽ có compile errors — expected, implementations chưa có)

---

### Phase 3: Infrastructure Master DB (10 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| I1 | `Infrastructure/Persistence/MasterDbContext.cs` | **NEW** | EF Core context: DbSets (Tenant, MasterUser, MasterUserTenant, RefreshToken). `UseSnakeCaseNamingConvention()`. NO tenant query filters. | ~40 |
| I2 | `Infrastructure/Persistence/Configurations/MasterUserConfiguration.cs` | **NEW** | Table `sys_master_users`, unique email index, property configs | ~30 |
| I3 | `Infrastructure/Persistence/Configurations/MasterUserTenantConfiguration.cs` | **NEW** | Table `sys_master_user_tenants`, composite PK, FK configs | ~25 |
| I4 | `Infrastructure/Persistence/Configurations/TenantConfiguration.cs` | **MODIFY** | Thêm: `database_mode`, `encrypted_connection_string`, `tunnel_hostname`, `cloud_database_name`, `db_status` column configs | ~15 |
| I5 | `Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | **MODIFY** | Thêm: `tenant_id` column + index. FK: `user_id` → `sys_master_users.id` (not sys_users). Remove User navigation if present. | ~10 |
| I6 | `Infrastructure/Persistence/ApplicationDbContext.cs` | **MODIFY** | XÓA: `DbSet<Tenant>`, `DbSet<RefreshToken>`. Constructor: accept connection string (not fixed). Remove Tenant/RefreshToken OnModelCreating configs. | ~20 |
| I7 | `Infrastructure/Persistence/TenantDbContextFactory.cs` | **NEW** | Scoped factory: inject ITenantConnectionResolver + ITenantContext → resolve connection per-request → create ApplicationDbContext | ~30 |
| I8 | `Infrastructure/Services/DataProtectionEncryptor.cs` | **NEW** | Implements IConnectionStringEncryptor via `IDataProtector` | ~25 |
| I9 | `Infrastructure/Services/TenantConnectionResolver.cs` | **NEW** | Logic: query MasterDbContext → if CloudManaged: build from CloudDatabaseHost + cloud_database_name. If OnPremise: decrypt encrypted_connection_string. Cache IMemoryCache 5min. | ~50 |
| I10 | `Infrastructure/DependencyInjection.cs` | **MODIFY** | Register: MasterDbContext (AddDbContext, "MasterConnection"), ApplicationDbContext (AddDbContextFactory or scoped), ITenantConnectionResolver, IConnectionStringEncryptor, DataProtection. Update health checks. | ~40 |

**Verify**: `dotnet build`

---

### Phase 4: Infrastructure Updates (3 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| S1 | `Infrastructure/Services/TenantRepository.cs` | **MODIFY** | Inject `IMasterDbContext` thay vì `IApplicationDbContext`. Query `_masterDb.Tenants` thay vì `_dbContext.Set<Tenant>()`. | ~5 |
| S2 | `Infrastructure/Services/ExpiredTokenCleanupService.cs` | **MODIFY** | Inject `IServiceScopeFactory` → resolve `IMasterDbContext`. Query `_masterDb.RefreshTokens`. | ~10 |
| S3 | `Infrastructure/Middleware/TenantMiddleware.cs` | **MODIFY** | After resolving tenant: gọi `ITenantConnectionResolver.ResolveConnectionStringAsync()` → store resolved connection string vào scoped service cho TenantDbContextFactory. | ~15 |

**Verify**: `dotnet build`

---

### Phase 5: Auth Handlers Rewrite (9 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| H1 | `Features/Auth/Commands/Login/LoginCommand.cs` | **MODIFY** | `LoginResult` type: thay `{ AccessToken, ExpiresIn, User }` → `{ TempToken, TempTokenExpiresIn, Companies[] }`. CompanyInfo DTO. | ~30 |
| H2 | `Features/Auth/Commands/Login/LoginCommandHandler.cs` | **REWRITE** | Inject IMasterDbContext. Find MasterUser by email. Verify password. Query MasterUserTenants + Tenants (check IsActive, DbStatus). Generate tempToken (60s). Return companies list. | ~80 |
| H3 | `Features/Auth/Commands/SelectCompany/SelectCompanyCommand.cs` | **NEW** | Input: TempToken, TenantId. Output: SelectCompanyResult (AccessToken, ExpiresIn, User). | ~20 |
| H4 | `Features/Auth/Commands/SelectCompany/SelectCompanyCommandHandler.cs` | **NEW** | Verify tempToken → extract userId. Check MasterUserTenant access. Resolve tenant DB. Load User→Roles→Permissions from Tenant DB. Generate JWT + RefreshToken (store in Master DB). | ~90 |
| H5 | `Features/Auth/Commands/SwitchCompany/SwitchCompanyCommand.cs` | **NEW** | Input: NewTenantId. Output: SwitchCompanyResult (AccessToken, ExpiresIn, User). | ~15 |
| H6 | `Features/Auth/Commands/SwitchCompany/SwitchCompanyCommandHandler.cs` | **NEW** | Verify current user access to newTenantId via MasterUserTenants. Resolve tenant DB. Load permissions. Generate new JWT. Optionally revoke old RefreshToken + create new. | ~70 |
| H7 | `Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs` | **MODIFY** | Replace IApplicationDbContext with IMasterDbContext for token ops. From token.TenantId → resolve Tenant DB → load user claims. Dual-context. | ~40 |
| H8 | `Features/Auth/Commands/Logout/LogoutCommandHandler.cs` | **MODIFY** | Replace dbContext.RefreshTokens → masterDbContext.RefreshTokens. | ~10 |
| H9 | `Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs` | **MODIFY** | Remove `dbContext.Tenants` reference. Get tenant name from JWT claims or cache. Add companies list from MasterUserTenants. | ~20 |

**Verify**: `dotnet build`, unit tests

---

### Phase 6: User Handlers Update (4 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| U1 | `Features/Users/Commands/ChangePassword/ChangePasswordCommandHandler.cs` | **MODIFY** | Inject IMasterDbContext. Find MasterUser by userId. Update MasterUser.PasswordHash (not User.PasswordHash). Revoke all RefreshTokens for this user (all tenants). | ~30 |
| U2 | `Features/Users/Commands/CreateUser/CreateUserCommandHandler.cs` | **MODIFY** | Inject IMasterDbContext. After creating User in Tenant DB → check if MasterUser exists by email → if yes: create MasterUserTenant link; if no: create MasterUser + MasterUserTenant. Dual-SaveChanges. | ~50 |
| U3 | `Features/Users/Commands/ToggleUserActivation/ToggleUserActivationCommandHandler.cs` | **MODIFY** | Inject IMasterDbContext. Revoke RefreshTokens via masterDb.RefreshTokens.Where(rt => rt.UserId == userId && rt.TenantId == currentTenantId). | ~15 |
| U4 | `Features/Users/Commands/UnlockUser/UnlockUserCommandHandler.cs` | **MODIFY** | Inject IMasterDbContext. Reset MasterUser.FailedLoginCount and MasterUser.LockedUntil (lockout data moves to Master DB). | ~15 |

**Verify**: `dotnet build`, unit tests

---

### Phase 7: API Layer (6 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| P1 | `Api/Services/CurrentUserService.cs` | **FIX** | `"tenant_id"` → `"tid"` | ~1 |
| P2 | `Api/Controllers/AuthController.cs` | **MODIFY** | Login response shape changes. Thêm: `[HttpPost("select-company")]` + `[HttpPost("switch-company")]` endpoints. select-company: [AllowAnonymous] (uses tempToken). switch-company: [Authorize]. | ~40 |
| P3 | `Api/Controllers/MeController.cs` | **MODIFY** | ChangePassword: handler changes handle it; controller just passes through. May add companies to GET /me response via updated query. | ~5 |
| P4 | `Api/Controllers/TenantsController.cs` | **NEW** | [Authorize(Policy = "SuperAdmin")]. CRUD tenants, test tunnel connectivity, manage user-tenant access. GET /api/tenants, POST /api/tenants, PUT /api/tenants/{id}, etc. | ~80 |
| P5 | `Api/Program.cs` | **MODIFY** | Add DataProtection services. Update health check to use Master DB connection. Add /api/auth/select-company, /switch-company route if needed. | ~15 |
| P6 | `Api/Infrastructure/GlobalExceptionHandler.cs` | **MODIFY** | Thêm exception mappings: TenantDbOfflineException → 503, CompanyNotAccessibleException → 403, TempTokenExpiredException → 401. | ~10 |

**Verify**: `dotnet build`, `get_errors`

---

### Phase 8: Database Migrations (4 operations)

| # | Thay đổi | Chi tiết |
|---|----------|----------|
| MG1 | Tách migration folders | `Persistence/Migrations/Master/` cho MasterDbContext, `Persistence/Migrations/Tenant/` cho ApplicationDbContext |
| MG2 | Master DB initial migration | `dotnet ef migrations add InitMasterDb --context MasterDbContext --output-dir Persistence/Migrations/Master`. Creates: sys_master_users, sys_master_user_tenants. Extends: sys_tenants (+5 cols). Adds: tenant_id to sys_refresh_tokens. |
| MG3 | Tenant DB cleanup migration | `dotnet ef migrations add RemoveMasterEntities --context ApplicationDbContext --output-dir Persistence/Migrations/Tenant`. Removes: sys_tenants, sys_refresh_tokens from tenant schema. User.PasswordHash → nullable. |
| MG4 | Seed data migration | Migrate existing superadmin: INSERT INTO sys_master_users FROM sys_users WHERE email='superadmin@...'. INSERT INTO sys_master_user_tenants. UPDATE sys_tenants SET database_mode='CloudManaged', db_status='Online'. |

**Verify**: `dotnet ef database update --context MasterDbContext`, `dotnet ef database update --context ApplicationDbContext`, verify seed data

---

### Phase 9: Frontend (8 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| F1 | `webapp/src/app/core/models/auth.models.ts` | **MODIFY** | Thêm: `LoginResponse { tempToken, tempTokenExpiresIn, companies: CompanyInfo[] }`, `CompanyInfo { tenantId, name, code, databaseMode, dbStatus, displayRole, isDefault }`, `SelectCompanyRequest`, `SelectCompanyResponse`, `SwitchCompanyRequest` | ~30 |
| F2 | `webapp/src/app/core/services/auth.service.ts` | **MODIFY** | login() return type → LoginResponse. Thêm: `selectCompany(tempToken, tenantId): Observable<SelectCompanyResponse>`, `switchCompany(tenantId): Observable<SelectCompanyResponse>` | ~20 |
| F3 | `webapp/src/app/core/stores/auth.store.ts` | **MODIFY** | State thêm: tempToken, companies, selectedCompany. login() → store tempToken + companies, navigate to /select-company (or auto-select). Thêm: selectCompany(), switchCompany() actions. | ~60 |
| F4 | `webapp/src/app/features/auth/company-select/company-select.component.ts` | **NEW** | Standalone component. PrimeNG Card + DataView. Each company: name, code, databaseMode badge, dbStatus indicator, displayRole. Default highlighted. Click → selectCompany(). Auto-redirect if 1 company. | ~100 |
| F5 | `webapp/src/app/core/guards/auth.guard.ts` | **MODIFY** | Allow /select-company route when tempToken exists but no accessToken. | ~5 |
| F6 | `webapp/src/app/app.routes.ts` | **MODIFY** | Thêm: `{ path: 'select-company', component: CompanySelectComponent, canActivate: [tempTokenGuard] }` | ~5 |
| F7 | `webapp/src/app/layout/shell/shell.component.ts` | **MODIFY** | Header: thêm company dropdown (PrimeNG Dropdown). Show current company name. Click another → switchCompany(). | ~30 |
| F8 | `webapp/src/app/features/auth/login/login.component.ts` | **MODIFY** | Login success: navigate to /select-company thay vì /dashboard. (Auto-select handled in auth.store). | ~5 |

**Verify**: `ng build`, `ng serve`, manual testing

---

### Phase 10: Config & Docker (3 files)

| # | File | Loại | Chi tiết | LOC |
|---|------|------|----------|-----|
| C1 | `Api/appsettings.json` | **MODIFY** | Thêm: `"ConnectionStrings": { "MasterConnection": "..." }`, `"CloudDatabaseHost": "localhost"`, `"DataProtection": { "KeyDirectory": "./keys" }` | ~10 |
| C2 | `Api/appsettings.Development.json` | **MODIFY** | `"MasterConnection": "Host=localhost;Port=5433;Database=phanmemketoan_master;..."` | ~5 |
| C3 | `docker-compose.yml` | **MODIFY** | Add init script to create 2 databases: `phanmemketoan_master` + `phanmemketoan_tenant_system` in same PostgreSQL instance. | ~15 |

**Verify**: `docker compose up -d`, `dotnet run`, login flow

---

### Phase 11: E2E Verification

| # | Test | Expected Result |
|---|------|----------------|
| 1 | `dotnet build` + `ng build` | No errors |
| 2 | `get_errors` on all changed files | No compile/lint errors |
| 3 | Run Master + Tenant DB migrations | Tables created, seed data correct |
| 4 | Login superadmin (1 company) | Auto-select → dashboard (no company-select page) |
| 5 | Create new tenant + assign user | MasterUser + MasterUserTenant created |
| 6 | Login multi-company user | Company-select page shows 2+ companies |
| 7 | Select company → dashboard | JWT issued with correct tid, permissions loaded |
| 8 | Switch company via header | New JWT, dashboard reloads with new tenant data |
| 9 | Change password | MasterUser.PasswordHash updated, applies to all companies |
| 10 | Token refresh | RefreshToken read from Master DB, claims from Tenant DB |
| 11 | Logout | Token blacklisted, RefreshToken revoked in Master DB |

---

## PHỤ LỤC: Ma trận Cross-Reference

### Tài liệu → Code dependency

| Tài liệu thay đổi | Code Phases bị ảnh hưởng |
|--------------------|--------------------------| 
| data-model.md | Phase 1 (Domain), Phase 3 (EF configs), Phase 8 (Migrations) |
| auth-api.md | Phase 5 (Auth handlers), Phase 7 (AuthController), Phase 9 (Frontend auth) |
| spec.md | Tất cả phases (reference cho acceptance criteria) |
| plan.md | Tất cả phases (implementation guide) |
| architecture-technology-report.md | Phase 3 (Infrastructure design), Phase 5 (Auth design) |
| me-api.md | Phase 6 (User handlers), Phase 7 (MeController) |
| users-api.md | Phase 6 (CreateUser, ToggleActivation) |
| quality-checklist.md | Phase 11 (E2E verification checklist) |

### Handler → DbContext mapping (sau migration)

| Handler | IMasterDbContext | IApplicationDbContext | Cả hai |
|---------|:---:|:---:|:---:|
| LoginCommandHandler | ✅ | | |
| SelectCompanyCommandHandler | | | ✅ |
| SwitchCompanyCommandHandler | | | ✅ |
| RefreshTokenCommandHandler | | | ✅ |
| LogoutCommandHandler | ✅ | | |
| GetCurrentUserQueryHandler | | ✅ | (có thể cả hai nếu thêm companies) |
| ChangePasswordCommandHandler | ✅ | | |
| CreateUserCommandHandler | | | ✅ |
| ToggleUserActivationCommandHandler | | | ✅ |
| UnlockUserCommandHandler | ✅ | | |
| All Role handlers (6) | | ✅ | |
| All Permission handlers (4) | | ✅ | |
| GetUsers, GetUserById | | ✅ | |
| UpdateUser, UpdateProfile | | ✅ | |

---

## Ước lượng tổng thể

| Hạng mục | Số files | Effort |
|----------|----------|--------|
| **PHẦN A: Tài liệu** | 15 | ~1,500 dòng thay đổi |
| **PHẦN B: Code** | ~46 (19 new + 27 modify) | ~1,200 LOC new + ~400 LOC modify |
| **Tổng cộng** | ~61 files | 11 phases |

### Proposed Workflow

```
1. User phê duyệt kế hoạch này
2. Thực hiện PHẦN A Group 1-2 (data-model, auth-api, spec, plan) → User review
3. Thực hiện PHẦN A Group 3-5 (remaining docs) → User review
4. User phê duyệt toàn bộ tài liệu
5. Thực hiện PHẦN B Phase 0-4 (foundation) → verify build
6. Thực hiện PHẦN B Phase 5-7 (handlers + API) → verify build + unit tests
7. Thực hiện PHẦN B Phase 8 (migrations) → verify DB
8. Thực hiện PHẦN B Phase 9-10 (frontend + config) → verify build
9. Phase 11: E2E verification
```
