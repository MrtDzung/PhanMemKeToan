# Implementation Plan: Auth & Tenant Core (SYS Module)

> **Version**: 2.0.0 | **Updated**: 2026-04-16 — Dual-DB Architecture (Phương án B Kết hợp)
> **Previous**: v1.0 single-DB (see git history)

## Technical Approach

**Security-first JWT design with 2-step login.** The system uses RS256 asymmetric JWT rather than HS256 because the private signing key never leaves the API tier — microservices or future resource servers only need the public key (exposed via JWKS). Access tokens are short-lived (15 min, configurable) and stored in Angular memory (not localStorage, preventing XSS theft). Login follows a 2-step flow: Step 1 verifies email/password against MasterUser in the Master DB and returns a **tempToken** (HMAC-SHA256, TTL 60s, minimal claims: `sub` + `rmb`) along with a list of accessible companies. Step 2 (`/api/auth/select-company`) validates the tempToken, resolves the selected company's Tenant DB, loads user roles/permissions, and issues the final JWT + refresh token cookie. Refresh tokens are transmitted exclusively as HttpOnly + Secure + SameSite=Strict cookies and persisted only as a SHA-256 hash in the **Master DB** (with `tenant_id` for Tenant DB resolution during refresh), so a DB breach cannot replay them. Token rotation with family-lineage tracking means a replayed refresh token causes full session revocation for that user — a fail-closed posture. The Redis-backed JTI blacklist ensures logout is immediately effective even within the remaining access token TTL window. BCrypt cost 12 (~300 ms per hash) is intentional: it makes offline dictionary attacks economically infeasible without noticeably affecting UX.

**Dual-DB tenant isolation.** The architecture uses two separate EF Core DbContexts: **MasterDbContext** (fixed connection to Master DB, no tenant filters) manages central auth data (MasterUser, MasterUserTenant, Tenant registry, RefreshToken). **ApplicationDbContext** (per-request connection via `TenantDbContextFactory` + `ITenantConnectionResolver`) manages tenant-specific data (User, Role, Permission, UserRole, RolePermission, and future accounting entities) with EF Core global query filters on User and Role as defense-in-depth. For CloudManaged tenants, the connection string is built from `CloudDatabaseHost` + `cloud_database_name`. For OnPremise tenants, the encrypted connection string is decrypted via `IConnectionStringEncryptor` (ASP.NET DataProtection API) and connected via Cloudflare Tunnel. The `TenantMiddleware` resolves `ITenantContext.TenantId` from the JWT `tid` claim and triggers connection resolution before the MediatR pipeline executes. Cross-DB identity is maintained by convention: `MasterUser.Id = User.Id` (same GUID, no FK constraint). `UserRole` cross-tenant consistency (User.TenantId == Role.TenantId) is enforced at the Application layer in command handlers.

**Angular auth state with NgRx Signals + 2-step login flow.** Auth state (token, user profile, permissions, tempToken, companies, selectedCompany) lives in a lightweight NgRx Signal Store (`auth.store.ts`) — kept in memory (not persisted to sessionStorage) to prevent XSS exfiltration. Login Step 1 stores tempToken + companies and navigates to `/select-company` (or auto-selects if only 1 company). Step 2 stores the JWT + user and navigates to the dashboard. A company switcher in the header allows switching companies via `/api/auth/switch-company`. The `AuthInterceptor` intercepts 401 responses, serializes concurrent refresh attempts via a single shared `BehaviorSubject<boolean>` mutex: the first interceptor instance triggers the refresh call and sets `isRefreshing = true`; all other concurrent 401 handlers subscribe to the same observable and wait for the token update before retrying. The `AuthGuard` reads `isAuthenticated` signal directly; `TempTokenGuard` protects `/select-company` route; `TenantGuard` verifies a valid TenantId is present in the user signal before activating accounting module routes.

---

## NuGet Packages Required

| Package | Version | Purpose |
|---------|---------|---------|
| BCrypt.Net-Next | 4.0.3 | Password hashing (BCrypt, cost 12) |
| System.IdentityModel.Tokens.Jwt | 8.x | JWT creation, parsing, validation |
| Microsoft.IdentityModel.Tokens | 8.x | SecurityKey, SigningCredentials |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.x | JWT Bearer middleware || Microsoft.AspNetCore.DataProtection | 10.x | Connection string encryption |
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 10.x | DataProtection key storage in DB || StackExchange.Redis | 2.8.x | Token blacklist, rate limit counters |
| AspNetCore.HealthChecks.Redis | 10.x | Redis health check endpoint |
| EFCore.NamingConventions | 10.x | snake_case column naming |
| MediatR | 14.x | CQRS command/query dispatching |
| FluentValidation.AspNetCore | 12.x | Input validation pipeline |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.x | EF Core PostgreSQL provider |
| Serilog.AspNetCore | 8.x | Structured logging |
| Serilog.Sinks.File | 5.x | Log file output |

---

## npm Packages Required

| Package | Version | Purpose |
|---------|---------|---------|
| @ngrx/signals | 20.x | Lightweight signal-based state store |
| @ngx-translate/core | 15.x | i18n translation service |
| @ngx-translate/http-loader | 8.x | Load translation JSON from HTTP |
| primeng | 20.x | UI component library (LTS) |
| primeicons | 7.x | Icon set for PrimeNG |
| tailwindcss | 4.x | Layout utility classes |

---

## Implementation Phases

### Phase 1: Domain Layer (Day 1)

**New Entities** (`src/PhanMemKeToan.Domain/Entities/`):

- **`MasterUser.cs`** — Inherits `BaseEntity`. Fields: `Email` (string, unique), `PasswordHash`, `FullName`, `IsActive`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil`, `CreatedAt`. Navigation: `MasterUserTenants` (collection), `RefreshTokens` (collection).
- **`MasterUserTenant.cs`** — Join entity (no base class). Fields: `UserId` (Guid, FK → MasterUser), `TenantId` (Guid, FK → Tenant), `DisplayRoleName` (string?), `IsDefault` (bool), `GrantedAt` (DateTimeOffset), `GrantedBy` (Guid?). Navigation: `MasterUser`, `Tenant`.

**New Enums** (`src/PhanMemKeToan.Domain/Enums/`):

- **`DatabaseMode.cs`** — `public enum DatabaseMode { CloudManaged, OnPremise }`
- **`TenantDbStatus.cs`** — `public enum TenantDbStatus { Online, Offline, Provisioning, Migrating }`

**Modified Entities**:

- **`Tenant.cs`** — Add 5 new fields: `DatabaseMode DatabaseMode` (default CloudManaged), `string? EncryptedConnectionString`, `string? TunnelHostname`, `string? CloudDatabaseName`, `TenantDbStatus DbStatus` (default Online). Remove `DatabaseSchemaName`. Add navigation: `MasterUserTenants` (collection).
- **`User.cs`** — Remove: `PasswordHash`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil` (all moved to MasterUser). Keep: `Id`, `TenantId`, `Email`, `FullName`, `IsActive`, `IsDeleted`, audit fields. Navigation: `UserRoles` (keep), `RefreshTokens` (remove).
- **`RefreshToken.cs`** — Add: `Guid TenantId`. Change FK: `UserId` now references MasterUser.Id conceptually (no cross-DB FK). Add navigation: removed `User`, no navigation to MasterUser (cross-DB).

**Domain Exceptions** (`src/PhanMemKeToan.Domain/Common/Exceptions/`):
- Keep existing: `InvalidCredentialsException`, `AccountDeactivatedException`, `AccountLockedException`, `TenantNotFoundException`, `TenantDeactivatedException`, `TokenExpiredException`, `TokenRevokedException`, `DuplicateEmailException`
- Add: `TempTokenExpiredException` (401), `TempTokenInvalidException` (401), `CompanyNotAccessibleException` (403), `TenantDbOfflineException` (503)

---

### Phase 2: Infrastructure — Persistence (Day 2)

**MasterDbContext** (`src/PhanMemKeToan.Infrastructure/Persistence/MasterDbContext.cs`) — **NEW**:
- Implements `IMasterDbContext`
- Registers DbSets: `Tenants`, `MasterUsers`, `MasterUserTenants`, `RefreshTokens`
- Constructor accepts `DbContextOptions<MasterDbContext>` (no tenant context)
- Calls `modelBuilder.UseSnakeCaseNamingConvention()`
- **NO global query filters** — all data is cross-tenant
- Connection: fixed from `"MasterConnection"` in appsettings

**ApplicationDbContext** (`src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs`) — **MODIFY**:
- Implements `IApplicationDbContext`
- Registers DbSets: `Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`
- **REMOVE**: `Tenants`, `RefreshTokens` DbSets (moved to MasterDbContext)
- Constructor accepts `DbContextOptions<ApplicationDbContext>` + `ITenantContext`
- Connection: per-request via `TenantDbContextFactory` + `ITenantConnectionResolver`
- Applies global filter: `modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == _tenantContext.TenantId)` — same for `Role`

**TenantDbContextFactory** (`src/PhanMemKeToan.Infrastructure/Persistence/TenantDbContextFactory.cs`) — **NEW**:
- Scoped service: injects `ITenantConnectionResolver` + `ITenantContext`
- Resolves connection string per-request and creates `ApplicationDbContext` with that connection

**Entity Type Configurations (Master DB)**:
- `MasterUserConfiguration.cs` — **NEW**: table `sys_master_users`, unique email index
- `MasterUserTenantConfiguration.cs` — **NEW**: table `sys_master_user_tenants`, composite PK `(user_id, tenant_id)`, FK configs
- `TenantConfiguration.cs` — **MODIFY**: add `database_mode`, `encrypted_connection_string`, `tunnel_hostname`, `cloud_database_name`, `db_status` columns. Remove `database_schema_name`.
- `RefreshTokenConfiguration.cs` — **MODIFY**: add `tenant_id` column + index. FK `user_id` → `sys_master_users.id`. Remove User navigation.

**Entity Type Configurations (Tenant DB)** — unchanged:
- `UserConfiguration` — table `sys_users`, composite unique index `(tenant_id, email)`. **REMOVE**: `password_hash`, `last_login_at`, `failed_login_count`, `locked_until` columns.
- `RoleConfiguration` — table `sys_roles`, composite unique index `(tenant_id, name)`
- `PermissionConfiguration` — table `sys_permissions`, unique index `code`
- `UserRoleConfiguration` — table `sys_user_roles`, composite PK
- `RolePermissionConfiguration` — table `sys_role_permissions`, composite PK

**Migration Folders**:
- `Persistence/Migrations/Master/` — for MasterDbContext
- `Persistence/Migrations/Tenant/` — for ApplicationDbContext

**Migrations**:
- `InitMasterDb` (Master) — creates `sys_master_users`, `sys_master_user_tenants`. Extends `sys_tenants` (+5 cols). Adds `tenant_id` to `sys_refresh_tokens`.
- `RemoveMasterEntities` (Tenant) — removes `sys_tenants`, `sys_refresh_tokens` from Tenant DB. Drops auth columns from `sys_users`.
- `SeedMasterData` (Master) — migrates existing superadmin to MasterUser + MasterUserTenant.
- *(Existing Phase 1 migrations `SeedPermissions`, `SeedSuperAdmin` remain in Tenant folder)*

---

### Phase 3: Infrastructure — Services (Day 2–3)

**JWT Service** (`Services/JwtService.cs`):
- `IJwtService` interface: `GenerateAccessToken(UserClaimsDto)`, `GenerateRefreshToken()`, `ValidateToken(string)`, `GetJwks()`, **`GenerateTempToken(Guid userId, bool rememberMe, TimeSpan? ttl)`**, **`ValidateTempToken(string tempToken)`**
- RS256 for access tokens: loads `RsaSecurityKey` from `appsettings` (Base64-encoded private key in dev; `.pfx` or Azure Key Vault reference in production)
- **HMAC-SHA256 for tempTokens**: separate secret key, minimal claims (`sub`, `rmb`), TTL 60s
- `GenerateAccessToken` embeds claims: `sub` (UserId), `tid` (TenantId), `email`, `roles` (JSON array), `permissions` (JSON array), `jti` (Guid), `iat`, `exp`
- `ValidateTempToken` returns `(Guid userId, bool rememberMe)` or throws `TempTokenExpiredException` / `TempTokenInvalidException`
- `GetJwks()` returns `JsonWebKeySet` from the public key only

**Password Hasher** (`Services/BcryptPasswordHasher.cs`) — unchanged:
- `IPasswordHasher` interface: `HashPassword(string)`, `VerifyPassword(string, string)`
- BCrypt.Net-Next, cost 12

**Token Blacklist Service** (`Services/RedisTokenBlacklistService.cs`) — unchanged:
- `ITokenBlacklistService`: `BlacklistAsync(string jti, TimeSpan ttl)`, `IsBlacklistedAsync(string jti)`
- Redis key: `blacklist:jti:{jti}`, value = `"1"`, TTL = remaining token validity
- If Redis is unavailable: throw `ServiceUnavailableException` → GlobalExceptionHandler maps to 503; token rejected (fail-closed)

**Tenant Connection Resolver** (`Services/TenantConnectionResolver.cs`) — **NEW**:
- `ITenantConnectionResolver`: `Task<string> ResolveConnectionStringAsync(Guid tenantId, CancellationToken ct)`
- Logic: query MasterDbContext for Tenant record. If `CloudManaged`: build from `CloudDatabaseHost` (appsettings) + `cloud_database_name`. If `OnPremise`: decrypt `encrypted_connection_string` via `IConnectionStringEncryptor`.
- Cache: `IMemoryCache` with 5-minute TTL per tenantId
- Throws: `TenantNotFoundException`, `TenantDeactivatedException`, `TenantDbOfflineException`

**Connection String Encryptor** (`Services/DataProtectionEncryptor.cs`) — **NEW**:
- `IConnectionStringEncryptor`: `string Encrypt(string plaintext)`, `string Decrypt(string ciphertext)`
- Implements via `IDataProtector` (ASP.NET DataProtection API)
- Purpose: application/purpose string = `"TenantConnectionStrings"`

**Tenant Middleware** (`Middleware/TenantMiddleware.cs`) — **MODIFY**:
- Resolves tenant from JWT `tid` claim (primary). Fallback to `X-Tenant-Code` header for dev/provisioning.
- After resolving tenant: calls `ITenantConnectionResolver.ResolveConnectionStringAsync()` and stores resolved connection string in scoped `ITenantContext` for `TenantDbContextFactory`.
- Validates tenant is active via cached lookup.
- On unknown/inactive/offline tenant: returns 403 or 503 JSON response immediately (short-circuits)

**Tenant Repository** (`Services/TenantRepository.cs`) — **MODIFY**:
- Inject `IMasterDbContext` instead of `IApplicationDbContext`
- Query `_masterDb.Tenants` instead of `_dbContext.Set<Tenant>()`

**Expired Token Cleanup** (`Services/ExpiredTokenCleanupService.cs`) — **MODIFY**:
- Inject `IServiceScopeFactory` → resolve `IMasterDbContext`
- Query `_masterDb.RefreshTokens` (tokens are in Master DB now)

**DependencyInjection.cs** — **MODIFY**:
- Register `MasterDbContext` with `AddDbContext<MasterDbContext>` using `"MasterConnection"`
- Register `ApplicationDbContext` via `AddDbContextFactory` or scoped `TenantDbContextFactory`
- Register: `ITenantConnectionResolver` → `TenantConnectionResolver`
- Register: `IConnectionStringEncryptor` → `DataProtectionEncryptor`
- Add DataProtection services
- Update health checks for Master DB

---

### Phase 4: Application Layer — CQRS Commands/Queries (Day 3–4)

All handlers live under `src/PhanMemKeToan.Application/Features/Auth/` and `Features/Users/` etc.

**Auth Commands/Queries**:
| Name | Type | Description |
|------|------|-------------|
| `LoginCommand` | Command | Validate rate limit → verify credentials in **Master DB** → generate tempToken → return `LoginResult { TempToken, TempTokenExpiresIn, Companies[] }` |
| `LoginCommandHandler` | Handler | Uses `IMasterDbContext`, `IPasswordHasher`, `IJwtService`. Queries MasterUser + MasterUserTenants + Tenants. |
| `SelectCompanyCommand` | Command | **NEW**. Validate tempToken → verify company access → resolve Tenant DB → load roles/permissions → generate JWT + RefreshToken → return `SelectCompanyResult` |
| `SelectCompanyCommandHandler` | Handler | **NEW**. Uses `IJwtService`, `IMasterDbContext`, `IApplicationDbContext` (via factory), `ITenantConnectionResolver`. Dual-context. |
| `SwitchCompanyCommand` | Command | **NEW**. Verify current user access to new tenant → resolve Tenant DB → load roles/permissions → generate new JWT + RefreshToken → revoke old RefreshToken |
| `SwitchCompanyCommandHandler` | Handler | **NEW**. Uses `IMasterDbContext`, `IApplicationDbContext` (via factory). Dual-context. |
| `RefreshTokenCommand` | Command | Validate refresh token hash in **Master DB** → check revoked → from token.TenantId resolve **Tenant DB** → load user claims → rotate → return new `LoginResult` |
| `RefreshTokenCommandHandler` | Handler | **MODIFY**: `IMasterDbContext` for token ops, `IApplicationDbContext` for claims loading. Dual-context. |
| `LogoutCommand` | Command | Blacklist JTI → revoke refresh token in **Master DB** → void |
| `LogoutCommandHandler` | Handler | **MODIFY**: `IMasterDbContext` for RefreshToken. |
| `GetCurrentUserQuery` | Query | Returns `CurrentUserDto` from token claims + DB lookup. Add `companies[]` from MasterUserTenants. |
| `GetCurrentUserQueryHandler` | Handler | **MODIFY**: Loads User+Roles+Permissions from Tenant DB. Optionally loads companies from MasterDbContext. |

**User Management Commands/Queries**:
| Name | Type | Description |
|------|------|-------------|
| `CreateUserCommand` | Command | Create User in **Tenant DB** + create/link MasterUser in **Master DB** + create MasterUserTenant. Dual-context. |
| `CreateUserCommandHandler` | Handler | **MODIFY**: inject `IMasterDbContext`. Check if MasterUser exists by email → if yes: link; if no: create. Dual-SaveChanges. |
| `UpdateUserCommand` | Command | Update FullName, email, role assignments in Tenant DB |
| `UpdateUserCommandHandler` | Handler | Re-validates uniqueness if email changes. Note: email change may need MasterUser sync. |
| `ToggleUserActivationCommand` | Command | Set `IsActive` in Tenant DB; if deactivating, revoke RefreshTokens for this user+tenant in **Master DB** |
| `ToggleUserActivationCommandHandler` | Handler | **MODIFY**: inject `IMasterDbContext` for token revocation. |
| `UnlockUserCommand` | Command | Reset `MasterUser.FailedLoginCount` and `MasterUser.LockedUntil` in **Master DB** |
| `UnlockUserCommandHandler` | Handler | **MODIFY**: inject `IMasterDbContext`. |
| `ChangePasswordCommand` | Command | Verify current password → update `MasterUser.PasswordHash` in **Master DB** → revoke ALL RefreshTokens for this user |
| `ChangePasswordCommandHandler` | Handler | **MODIFY**: inject `IMasterDbContext`. Master DB only operation. |
| `GetUsersQuery` | Query | Paginated + filtered list for tenant (Tenant DB) |
| `GetUserByIdQuery` | Query | Single user with roles (Tenant DB) |
| `UpdateProfileCommand` | Command | Update own FullName in Tenant DB |

**Role & Permission Commands/Queries** — unchanged (all operate within Tenant DB):
| Name | Type | Description |
|------|------|-------------|
| `CreateRoleCommand` | Command | Create role with optional initial permissions |
| `UpdateRoleCommand` | Command | Update name/description |
| `DeleteRoleCommand` | Command | Validate no active users → soft-delete Role + hard-delete RolePermission join rows |
| `GetRolesQuery` | Query | List roles with user count per role |
| `GetRoleByIdQuery` | Query | Single role with permissions |
| `AssignPermissionsToRoleCommand` | Command | Bulk replace permissions for a role |
| `GetPermissionsQuery` | Query | All permissions grouped by module |
| `GetPermissionMatrixQuery` | Query | All roles × all permissions cross-tab |

**Validators** (FluentValidation, one per Command):
- Unchanged: `LoginCommandValidator`, `CreateUserCommandValidator`, `ChangePasswordCommandValidator`, `CreateRoleCommandValidator`, `AssignPermissionsToRoleCommandValidator`
- **NEW**: `SelectCompanyCommandValidator` (tempToken not empty, tenantId not empty)
- **NEW**: `SwitchCompanyCommandValidator` (tenantId not empty, differs from current)

---

### Phase 5: API Layer (Day 4–5)

**Controllers** (`src/PhanMemKeToan.Api/`):

| Controller | Base Path | Auth | Key Actions |
|------------|----------|------|-------------|
| `AuthController` | `/api/auth` | Public (login/select-company/refresh) / Bearer (logout/switch-company) | `POST /login`, `POST /select-company` (**NEW**), `POST /switch-company` (**NEW**), `POST /refresh`, `POST /logout` |
| `JwksController` | `/.well-known` | Public | `GET /jwks.json` |
| `MeController` | `/api/me` | Bearer | `GET /` (includes `companies[]`), `PUT /profile`, `PUT /password` |
| `UsersController` | `/api/users` | Bearer + `SYS.Users.*` | CRUD + activate/deactivate/unlock |
| `RolesController` | `/api/roles` | Bearer + `SYS.Roles.*` | CRUD |
| `PermissionsController` | `/api/permissions` | Bearer + `SYS.Roles.Manage` | Matrix GET/PUT + flat list GET |
| `TenantsController` | `/api/tenants` | Bearer + SuperAdmin | **NEW**: CRUD for tenant registry. `GET /`, `POST /`, `PUT /:id`, `PATCH /:id/status` |

**AuthController endpoint changes**:
- `POST /login`: Returns `{ tempToken, tempTokenExpiresIn: 60, companies: CompanyInfo[] }` — **NO JWT**. Sets no cookies.
- `POST /select-company` (**NEW**): Body `{ tempToken, tenantId }`. Returns `{ accessToken, expiresIn, user: CurrentUserDto }`. Sets `refresh_token` HttpOnly cookie.
- `POST /switch-company` (**NEW**): Requires `[Authorize]`. Body `{ tenantId }`. Returns same as select-company. Revokes old RefreshToken, sets new cookie.
- `POST /refresh`: Reads `refresh_token` cookie. Returns `{ accessToken, expiresIn, user }`. Rotates cookie.
- `POST /logout`: Requires `[Authorize]`. Clears `refresh_token` cookie. Blacklists JTI. Revokes RefreshToken in Master DB.

**CurrentUserService** (`Api/Services/CurrentUserService.cs`) — **FIX**:
- Change claim `"tenant_id"` → `"tid"` to match JWT claim name

**Program.cs registrations** — **MODIFY**:
- `AddDbContext<MasterDbContext>("MasterConnection")` — fixed connection
- `AddScoped<TenantDbContextFactory>()` — per-request tenant connection
- `AddAuthentication().AddJwtBearer(...)` with JWKS validation from `IJwtService.GetJwks()`
- `AddDataProtection()` with `PersistKeysToDbContext<MasterDbContext>()`
- `AddRateLimiter()` — fixed window policy on `/api/auth/login` for general throughput protection. **Note**: counting *failed* login attempts (FR-007, FR-010b) is handled at the Application layer in `LoginCommandHandler` using a Redis counter keyed on IP address.
- `AddResponseCaching()`, `AddHealthChecks().AddRedis(...)`, `AddHealthChecks().AddNpgSql("MasterConnection")`, `AddHealthChecks().AddNpgSql("DefaultTenantConnection")`
- `app.UseMiddleware<TenantMiddleware>()` — after `UseAuthentication`, before `UseAuthorization`
- CORS: `AddCors()` with named policies per environment
- `app.MapHealthChecks("/health")`

---

### Phase 6: Angular Frontend (Day 5–7)

**State & Services** (`src/webapp/src/app/core/auth/`):

| File | Purpose |
|------|---------|
| `auth.store.ts` | NgRx Signal Store: `token`, `user`, `isAuthenticated`, `permissions`, **`tempToken`**, **`companies`**, **`selectedCompany`** signals |
| `auth.service.ts` | `login()` → returns tempToken+companies, **`selectCompany()`** → returns JWT, **`switchCompany()`** → returns JWT, `logout()`, `refresh()`, `getMe()` HTTP calls |
| `auth.interceptor.ts` | Injects Bearer token; handles 401 → refresh with mutex; retries original request |
| `auth.guard.ts` | `CanActivateFn` — reads `isAuthenticated`, redirects to `/login` with `returnUrl` |
| **`temp-token.guard.ts`** | **NEW** `CanActivateFn` — reads `tempToken` signal, redirects to `/login` if missing/expired |
| `tenant.guard.ts` | `CanActivateFn` — verifies `user.tenantId` is set |
| `auth.models.ts` | **MODIFY**: Add `LoginStep1Response`, `CompanyInfo`, `SelectCompanyRequest`, `SwitchCompanyRequest` interfaces |

**Pages** (`src/webapp/src/app/features/sys/`):

| File | Route | Purpose |
|------|-------|---------|
| `login/login.component.ts` | `/login` | PrimeNG form → on success: store tempToken+companies → navigate to `/select-company` (or auto-select if 1 company) |
| **`select-company/select-company.component.ts`** | `/select-company` | **NEW**: Display company cards/list. Click → `selectCompany(tenantId)` → store JWT → navigate to dashboard. Shows default badge, offline badge. Timer for tempToken countdown (60s). |
| `users/sys-users.component.ts` | `/sys/users` | p-table, search, pagination, activate/deactivate |
| `users/sys-users-form.component.ts` | Modal | Create/edit user dialog, role multi-select |
| `roles/sys-roles.component.ts` | `/sys/roles` | Role list, CRUD actions |
| `roles/sys-roles-form.component.ts` | Modal | Role create/edit with permission assignment |
| `permissions/sys-permissions.component.ts` | `/sys/permissions` | Permission matrix grid with module grouping |
| `me/me.component.ts` | `/me` | Profile view + edit FullName + change password |

**Shell Component** (`src/webapp/src/app/core/layout/`):
- **Company Switcher** (`company-switcher.component.ts`) — **NEW**: dropdown in header showing current company name + switcher. On switch: confirm dialog (warn unsaved changes) → `switchCompany(tenantId)` → reload auth state → navigate to dashboard.

**Routing** (`app.routes.ts`) — **MODIFY**:
- Add `/select-company` route guarded by `TempTokenGuard`
- Accounting module routes guarded by `AuthGuard` + `TenantGuard`

**i18n** (`src/webapp/src/assets/i18n/vi.json`):
- All keys under namespace `SYS.*`: `SYS.AUTH.*`, `SYS.USERS.*`, `SYS.ROLES.*`, `SYS.PERMISSIONS.*`, `SYS.ME.*`
- Error keys: `SYS.AUTH.INVALID_CREDENTIALS`, `SYS.AUTH.ACCOUNT_DEACTIVATED`, `SYS.AUTH.RATE_LIMITED`, etc.
- **NEW keys**: `SYS.AUTH.ERROR.NO_COMPANIES`, `SYS.AUTH.ERROR.TEMP_TOKEN_EXPIRED`, `SYS.AUTH.ERROR.COMPANY_NOT_ACCESSIBLE`, `SYS.AUTH.ERROR.TENANT_DB_OFFLINE`, `SYS.AUTH.COMPANY_SELECT.*`, `SYS.AUTH.COMPANY_SWITCH.*`

---

## Testing Strategy

### Unit Tests (xUnit + Moq)

| Test Class | Scenarios |
|-----------|-----------|
| `LoginCommandHandlerTests` | Valid credentials → tempToken+companies returned; invalid password → `InvalidCredentialsException`; deactivated MasterUser → 401 before password check; rate limit hit → `AccountLockedException`; email not found → same generic error; user with 0 companies → `NoCompaniesException` |
| `SelectCompanyCommandHandlerTests` | **NEW**: Valid tempToken+tenantId → JWT+RefreshToken; expired tempToken → `TempTokenExpiredException`; invalid tempToken → `TempTokenInvalidException`; user not in tenant → `CompanyNotAccessibleException`; tenant offline → `TenantDbOfflineException`; tenant deactivated → `TenantDeactivatedException` |
| `SwitchCompanyCommandHandlerTests` | **NEW**: Valid JWT+tenantId → new JWT+RefreshToken; same tenantId → validation error; user not in target tenant → `CompanyNotAccessibleException`; old RefreshToken revoked after switch |
| `RefreshTokenCommandHandlerTests` | Valid refresh → new tokens issued + old revoked; expired token → 401; revoked token → entire family revoked; replay attack detected → all family tokens revoked; token.TenantId resolves correct Tenant DB for claims loading |
| `JwtServiceTests` | Access token generation has correct claims; RS256 signature validates with public key; expired token rejected; tampered token rejected; `GetJwks()` returns correct key material; **tempToken** generation with HMAC-SHA256; tempToken validation success/expiry/tampering |
| `BcryptPasswordHasherTests` | Hash + verify roundtrip; wrong password returns false; hash is non-deterministic (salted) |
| `CreateUserCommandHandlerTests` | Duplicate email → `DuplicateEmailException`; invalid role id → validation error; cross-tenant role assignment blocked; **NEW**: MasterUser created if not exists; MasterUser linked if exists; MasterUserTenant created |
| `DeleteRoleCommandHandlerTests` | Role with active users → rejection with user list; role with no users → success |
| `TenantMiddlewareTests` | JWT tid claim → correct tenant resolved; header fallback; deactivated tenant → 403; unknown code → 403; **NEW**: tenant DB offline → 503 |
| `TenantConnectionResolverTests` | **NEW**: CloudManaged → builds connection from host+dbname; OnPremise → decrypts connection string; tenant not found → `TenantNotFoundException`; result cached for 5 min; tenant offline → `TenantDbOfflineException` |
| `ChangePasswordCommandHandlerTests` | **NEW focus**: Updates MasterUser.PasswordHash in Master DB; revokes ALL RefreshTokens for user across all tenants |

### Integration Tests (EF Core InMemory / TestContainers PostgreSQL)

| Test | Assertion |
|------|-----------|
| Tenant isolation | User-A token cannot return User-B's data from any endpoint |
| **Dual-DB isolation** | **NEW**: MasterUser data in Master DB; User data in Tenant DB; no cross-DB leakage |
| Permission enforcement | Request with JWT missing `GL.Journal.View` → 403 on GL Journal list endpoint |
| Rate limiting | 6th login attempt within 5 min → 429; counter resets after window |
| Token rotation replay | Replay old refresh token → 401 + all family tokens revoked in DB |
| **2-step login flow** | **NEW**: login → tempToken → select-company → JWT; invalid tempToken → 401; expired tempToken → 401 |
| **Company switching** | **NEW**: switch-company → new JWT with different tid; old RefreshToken revoked |
| Global filter bypass | Direct `IgnoreQueryFilters()` call only available under `[SuperAdminPolicy]` — tested by attempting bypass as regular user |

### E2E Tests (Playwright)

| Scenario | Steps |
|----------|-------|
| **Full 2-step login flow** | Navigate `/login` → submit credentials → assert redirect to `/select-company` → select company → assert redirect to dashboard → assert tenant name visible |
| **Auto-select single company** | **NEW**: User with 1 company → login → auto-redirect to dashboard (skip company select) |
| **Company switching** | **NEW**: Login → dashboard → click company switcher → select different company → confirm dialog → assert dashboard reloads with new company name |
| **TempToken expiry** | **NEW**: Login → wait 65s on select-company page → attempt select → assert redirect to `/login` with error toast |
| Transparent token refresh | Shorten access TTL to 10s → login → select company → wait 12s → make API call → assert success + new token in memory |
| User management CRUD | Login as admin → create user → verify in list → deactivate → verify badge change → attempt login as deactivated user → assert 401 |
| Permission matrix | Create role → open matrix → toggle 3 permissions → save → re-open → assert saved state |

---

## Security Considerations

- **RS256 asymmetric JWT**: Private key stored in `dotnet user-secrets` (dev) / Azure Key Vault reference (prod). Public key exposed only via `/.well-known/jwks.json`. Never logs private key.
- **TempToken security**: HMAC-SHA256 with separate secret key (not RSA). TTL 60 seconds. Minimal claims (`sub`, `rmb`). Not stored server-side — validated by signature+expiry only. Cannot be used as Bearer token (different validation pipeline).
- **Refresh token security**: Stored as `SHA-256(plaintext)` in **Master DB**. Plaintext only in HttpOnly + Secure + SameSite=Strict cookie. Token family lineage: replay of revoked token triggers full user session revocation. Each token includes `tenant_id` for Tenant DB resolution during refresh.
- **Dual-DB isolation**: Authentication data (credentials, tokens) isolated in Master DB. Business data isolated per Tenant DB. No cross-DB foreign keys — consistency enforced at Application layer. Connection strings for OnPremise tenants encrypted at rest via ASP.NET DataProtection API.
- **Rate limiting (two-layer)**: (1) ASP.NET Core built-in `AddRateLimiter()` provides general request throttling on the login endpoint. (2) Application-layer Redis counter (`ratelimit:login:{ip}`) tracks *failed attempts only* — incremented in `LoginCommandHandler` on each authentication failure, with 5-minute sliding window TTL. After 5 failed attempts: `AccountLockedException` → HTTP 429. Counter expires automatically after the window. If Redis is unavailable: fail-open on counter (BCrypt still provides protection); blacklist checks remain fail-closed.
- **BCrypt cost 12**: ~300ms per verification — acceptable UX latency; prevents automated dictionary attacks.
- **CORS allowlist**: Configured per environment in `appsettings.{Environment}.json`. Wildcard `*` is forbidden in production.
- **No stack traces in production**: `GlobalExceptionHandler` already present; maps domain exceptions to RFC 7807 `ProblemDetails`.
- **Default-deny authorization**: All controllers require `[Authorize]`. Only `AuthController` (login/select-company/refresh), `JwksController`, and health endpoints are explicitly public (`[AllowAnonymous]`).
- **Input validation**: All commands have FluentValidation validators registered in the MediatR pipeline behavior. Server-side validation runs before any business logic.
- **Audit trail**: `AuditableEntity` captures `CreatedBy/UpdatedBy` via `ICurrentUserService`. All security-sensitive mutations are auditable.
- **Password complexity** (OWASP ASVS §2.1.1): 8–128 chars, upper + lower + digit + special character. Enforced in `CreateUserCommandValidator` and `ChangePasswordCommandValidator`.
