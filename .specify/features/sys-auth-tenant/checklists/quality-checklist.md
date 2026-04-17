# Quality Checklist: Auth & Tenant Core (SYS Module)

**Purpose**: Requirements quality validation
**Created**: 2026-04-15
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [contracts/](../contracts/)
**Scope**: Architecture · Security · Data Model · API Contracts · Frontend · Testing · Business Rules · Performance

---

## 1. Architecture (CHK001–CHK010)

- [ ] CHK001 — Clean Architecture 4-layer boundaries documented with one-way dependency direction
- [ ] CHK002 — Domain entities must not reference Application or Infrastructure types
- [ ] CHK003 — All mutations=Command, all reads=Query per CQRS (MediatR)
- [ ] CHK004 — DI wiring for `IJwtService`, `IPasswordHasher`, `ITokenBlacklistService`, `ITenantContext` specified
- [ ] CHK005 — Application handlers must not reference EF Core types directly
- [ ] CHK006 — `[Gap]` FluentValidation pipeline behavior registration order relative to logging/transaction behaviors
- [ ] CHK007 — Per-entity base class (AuditableEntity vs BaseEntity vs none) justified for all 7 entities
- [ ] CHK008 — `ICurrentUserService` contract and DI lifetime (scoped) specified
- [ ] CHK009 — `GlobalExceptionHandler` domain-exception-to-HTTP-status mappings enumerated for all 8 exceptions
- [ ] CHK010 — `[Gap]` OutboxMessage/domain event scope explicitly in or out of this feature

---

## 2. Security (CHK011–CHK025)

- [ ] CHK011 — RS256 private key storage per environment (dev: user-secrets; prod: Key Vault), no hardcoded fallback
- [ ] CHK012 — HS256 and symmetric algorithms explicitly prohibited; RS256 is the only valid algorithm
- [ ] CHK013 — All JWT claims (`sub`, `tid`, `email`, `roles`, `permissions`, `jti`, `iat`, `exp`) documented with source and type
- [ ] CHK014 — BCrypt cost 12 justified with ~300ms latency note (intentional, not a defect)
- [ ] CHK015 — Redis fail-closed semantics: Redis unavailable → token rejected (not accepted)
- [ ] CHK016 — Two rate-limiting layers distinguished: ASP.NET Core middleware (all requests) vs Application Redis counter (failed attempts only)
- [ ] CHK017 — Per-IP lockout (FR-007) and per-user lockout (FR-010b) are independent with separate Redis keys
- [ ] CHK018 — CORS: named policy per environment, wildcard `*` prohibited in production, allowlist from appsettings
- [ ] CHK019 — Refresh token cookie attributes (HttpOnly, Secure, SameSite=Strict, Max-Age for Remember Me) documented
- [ ] CHK020 — TokenFamily algorithm (shared UUID across rotated tokens → replay → revoke entire family) specified precisely
- [ ] CHK021 — Concurrent logout idempotency (blacklist write idempotent, no duplication errors) in edge cases
- [ ] CHK022 — Production error sanitization: ProblemDetails fields present vs omitted, no stack traces
- [ ] CHK023 — OWASP A01→FR-011/FR-015, A02→FR-001/FR-004/FR-008, A07→FR-007/FR-010b mapped for SC-010
- [ ] CHK024 — `[Gap]` PasswordHash and TokenHash must never appear in API responses, logs, or error messages
- [ ] CHK025 — `[Gap]` Auth audit log schema: structured Serilog fields per event type documented

---

## 3. Data Model (CHK026–CHK037)

- [ ] CHK026 — User entity has all 10 fields: Id, TenantId, Email, PasswordHash, FullName, IsActive, LastLoginAt, FailedLoginCount, LockedUntil, IsDeleted
- [ ] CHK027 — IsDeleted on User and Role consistent with Constitution hard-delete prohibition
- [ ] CHK028 — No AuditableEntity on UserRole, RolePermission, RefreshToken: absence justified
- [ ] CHK029 — Tenant does NOT inherit AuditableEntity; reason documented (pre-exists tenant context)
- [ ] CHK030 — All indexes documented: (`tenant_id`,`email`), (`tenant_id`,`name`), unique `token_hash`, (`token_family`,`is_revoked`), `module_code`
- [ ] CHK031 — Permission.Code: global uniqueness, MODULE.Resource.Action format, DB unique index
- [ ] CHK032 — EF Core global filter scope: User + Role get filter; Permission, Tenant, UserRole, RolePermission, RefreshToken do NOT
- [ ] CHK033 — Cross-tenant UserRole constraint (User.TenantId == Role.TenantId) is application-layer (not DB FK); rationale documented
- [ ] CHK034 — RefreshToken.TokenFamily semantics (single UUID shared across rotated tokens in one session lineage) unambiguous
- [ ] CHK035 — snake_case column naming via EFCore.NamingConventions project-wide
- [ ] CHK036 — Cascade deletes: UserRole on User/Role delete, RolePermission on Role delete, RefreshToken on User delete
- [ ] CHK037 — Seed data complete: Tenant(SYSTEM), superadmin user, SuperAdmin role, ~75 permission codes (15 modules × actions), role-user assignment

---

## 4. API Contracts (CHK038–CHK047)

- [ ] CHK038 — All 19 endpoints across 5 controllers documented with HTTP method, path, auth requirement, permission code
- [ ] CHK039 — All success response shapes (200/201) documented including side-effects (cookie set on login/refresh)
- [ ] CHK040 — Error shapes consistent: RFC 7807 ProblemDetails with uniform fields across all endpoints
- [ ] CHK041 — Pagination params (page, pageSize, search, isActive), defaults, upper bounds documented for list endpoints
- [ ] CHK042 — Refresh token delivered ONLY via HttpOnly cookie (never in response body) for login and refresh endpoints
- [ ] CHK043 — All error conditions with HTTP status codes and errorCode strings: 400/401/403/404/409/429
- [ ] CHK044 — `[Gap]` GET /.well-known/jwks.json response shape (kty, use, n, e, kid, alg fields) documented
- [ ] CHK045 — AllowAnonymous vs Authorize vs Authorize+permission policy specified per endpoint
- [ ] CHK046 — `[Gap]` Deactivate endpoint side effect (all active refresh tokens revoked) documented in contract
- [ ] CHK047 — `[Gap]` POST /api/users/{id}/unlock request shape, required permission, side effects documented

---

## 5. Frontend (CHK048–CHK057)

- [ ] CHK048 — NgRx Signal Store 4 signals with exact TypeScript types: `token: string|null`, `user: UserProfile|null`, `isAuthenticated: computed<boolean>`, `permissions: string[]`
- [ ] CHK049 — In-memory-only token storage required (no localStorage, sessionStorage, IndexedDB)
- [ ] CHK050 — AuthInterceptor refresh storm mutex (single shared `BehaviorSubject<boolean>`) specified precisely
- [ ] CHK051 — Post-login redirect (returnUrl preserved in router state, restored after re-auth) specified
- [ ] CHK052 — /sys/users table columns (Full Name, Email, Active Roles, Status, Last Login), display order, row actions specified
- [ ] CHK053 — PrimeNG used directly; no custom wrapper replacements; specific components per screen specified
- [ ] CHK054 — All SYS.* i18n keys (SYS.AUTH.*, SYS.USERS.*, SYS.ROLES.*, SYS.PERMISSIONS.*, SYS.ME.*) enumerated in spec
- [ ] CHK055 — TenantGuard behavior for pass (TenantId present) and fail (redirect destination) specified
- [ ] CHK056 — Loading/empty/error states for 4 screens use CSS design token variables, no hardcoded hex
- [ ] CHK057 — Component stylesheets use CSS custom properties only; hardcoded hex prohibited per design system

---

## 6. Testing (CHK058–CHK067)

- [ ] CHK058 — LoginCommandHandlerTests: 5 scenarios with exact exception types per scenario
- [ ] CHK059 — RefreshTokenCommandHandlerTests: 4 scenarios including replay with DB assertion on family revocation
- [ ] CHK060 — JwtServiceTests: 5 scenarios (claims, RS256 signature, expired, tampered, JWKS output)
- [ ] CHK061 — Tenant isolation integration test: 0% cross-tenant leakage on read + write + existence-probe endpoints
- [ ] CHK062 — E2E transparent refresh: 10s TTL config → wait 11s → API call → assert 200 + new token in memory
- [ ] CHK063 — xUnit + Moq specified as project-wide test framework and mock library
- [ ] CHK064 — EF InMemory vs TestContainers PostgreSQL selection rule specified per test type
- [ ] CHK065 — `[Gap]` ToggleUserActivation (refresh tokens revoked) and UnlockUser (FailedLoginCount reset) unit test scenarios
- [ ] CHK066 — E2E permission matrix test: create role → toggle 3 perms → save → re-open → assert persisted
- [ ] CHK067 — `[Gap]` Test data isolation strategy between integration test runs (rollback vs reset vs shared fixture)

---

## 7. Business Rules (CHK068–CHK075)

- [ ] CHK068 — User and Role inherit AuditableEntity which implements ITenantEntity (not double-declared)
- [ ] CHK069 — AuditableEntity fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, TenantId) auto-population mechanism via ICurrentUserService documented
- [ ] CHK070 — Constitution no-hard-delete rule: User (IsActive=false + IsDeleted) and Role (IsDeleted=true) both covered; FR-020 and Constitution reconciled
- [ ] CHK071 — Permission codes validated against MODULE.Resource.Action pattern for all 15 module codes in CreateUserCommandValidator
- [ ] CHK072 — Cross-tenant UserRole invariant (User.TenantId == Role.TenantId) pre-condition in CreateUser + UpdateUser handlers
- [ ] CHK073 — Email uniqueness per-tenant (same email across different tenants is valid) documented
- [ ] CHK074 — Permissions seeded via migrations only; no runtime creation via UI/API; enforced by validator or middleware
- [ ] CHK075 — Remember Me token TTL behavior (persistent 7-day Max-Age vs session-scoped, no Max-Age) and security implications documented

---

## 8. Performance (CHK076–CHK081)

- [ ] CHK076 — Login <3s (SC-001) broken into component budgets: BCrypt ~300ms + DB + JWT gen + Redis write + network RTT
- [ ] CHK077 — BCrypt cost 12 ~300ms documented as intentional security parameter, not a performance defect
- [ ] CHK078 — CRUD <2s (SC-006) specified under measurable load: single vs 50 concurrent users
- [ ] CHK079 — `[Gap]` Redis connection pool size (StackExchange.Redis) for 50 concurrent sessions
- [ ] CHK080 — `[Gap]` PostgreSQL connection pool sizing (Min/Max in connection string) for 50 concurrent sessions
- [ ] CHK081 — TenantMiddleware Redis cache: 5-min TTL + invalidation on tenant deactivation mid-session documented

---

## 9. Dual-DB Architecture (CHK082–CHK095)

- [ ] CHK082 — MasterDbContext registered with fixed "MasterConnection" connection string — no per-tenant variation
- [ ] CHK083 — ApplicationDbContext registered via TenantDbContextFactory with per-request connection resolved from ITenantConnectionResolver
- [ ] CHK084 — Master DB migration folder separate: `Migrations/Master/`
- [ ] CHK085 — Tenant DB migration folder separate: `Migrations/Tenant/`
- [ ] CHK086 — Connection string encryption via IConnectionStringEncryptor (DataProtection API) — never stored in plaintext
- [ ] CHK087 — TempToken: minimal claims (sub, rmb only), TTL 60s, separate HMAC-SHA256 signing key
- [ ] CHK088 — Cloudflare Tunnel health: `db_status` updated, offline tenants show disabled in company list
- [ ] CHK089 — Cross-DB identity: MasterUser.Id = User.Id (same GUID), enforced in CreateUserCommandHandler
- [ ] CHK090 — ChangePasswordCommandHandler updates MasterUser.PasswordHash, NOT tenant User entity
- [ ] CHK091 — CreateUserCommandHandler: dual-context operation — MasterUser in Master DB + User in Tenant DB
- [ ] CHK092 — RefreshToken.TenantId populated on creation, used during refresh to resolve correct Tenant DB
- [ ] CHK093 — 2-step login: no JWT issued at Step 1, only tempToken. JWT issued at Step 2 (select-company)
- [ ] CHK094 — Company switch: verify MasterUserTenant access before issuing new JWT
- [ ] CHK095 — Auto-select: if companies.length === 1, frontend auto-calls select-company (no manual step)

---

## Summary

| Domain | Items | [Gap] Items |
|--------|-------|-------------|
| Architecture | 10 | 2 (CHK006, CHK010) |
| Security | 15 | 2 (CHK024, CHK025) |
| Data Model | 12 | 0 |
| API Contracts | 10 | 3 (CHK044, CHK046, CHK047) |
| Frontend | 10 | 0 |
| Testing | 10 | 2 (CHK065, CHK067) |
| Business Rules | 8 | 0 |
| Performance | 6 | 2 (CHK079, CHK080) |
| Dual-DB Architecture | 14 | 0 |
| **Total** | **95** | **11** |
