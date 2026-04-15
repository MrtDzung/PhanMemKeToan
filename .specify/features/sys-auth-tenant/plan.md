# Implementation Plan: Auth & Tenant Core (SYS Module)

## Technical Approach

**Security-first JWT design.** The system uses RS256 asymmetric JWT rather than HS256 because the private signing key never leaves the API tier — microservices or future resource servers only need the public key (exposed via JWKS). Access tokens are short-lived (15 min, configurable) and stored in Angular memory (not localStorage, preventing XSS theft). Refresh tokens are transmitted exclusively as HttpOnly + Secure + SameSite=Strict cookies and persisted only as a SHA-256 hash in the database, so a DB breach cannot replay them. Token rotation with family-lineage tracking means a replayed refresh token causes full session revocation for that user — a fail-closed posture. The Redis-backed JTI blacklist ensures logout is immediately effective even within the remaining access token TTL window. BCrypt cost 12 (~300 ms per hash) is intentional: it makes offline dictionary attacks economically infeasible without noticeably affecting UX.

**Multi-tenant isolation via EF Core global query filter.** All tenant-scoped entities implement `ITenantEntity` (TenantId property). `AppDbContext` applies a global query filter `e => e.TenantId == _currentTenantId` for every such entity, so no individual query handler can accidentally return cross-tenant data — it is structurally impossible unless the filter is explicitly ignored. The `TenantMiddleware` resolves `ITenantContext.TenantId` from the JWT claim (primary) or `X-Tenant-Code` header (fallback for public/provisioning flows), then injects it into the scoped `ITenantContext` before the MediatR pipeline executes. UserRole cross-tenant consistency (User.TenantId == Role.TenantId) is enforced at the Application layer in command handlers rather than by a DB foreign key, because both entities share the same schema and the FK would be redundant with the global filter enforcement.

**Angular auth state with NgRx Signals + refresh storm protection.** Auth state (token, user profile, permissions) lives in a lightweight NgRx Signal Store (`auth.store.ts`) — kept in memory (not persisted to sessionStorage) to prevent XSS exfiltration. The `AuthInterceptor` intercepts 401 responses, serializes concurrent refresh attempts via a single shared `BehaviorSubject<boolean>` mutex: the first interceptor instance triggers the refresh call and sets `isRefreshing = true`; all other concurrent 401 handlers subscribe to the same observable and wait for the token update before retrying. The `AuthGuard` reads `isAuthenticated` signal directly; `TenantGuard` verifies a valid TenantId is present in the user signal before activating accounting module routes. Post-login redirect is preserved in the router state so users land on their intended page after re-authentication.

---

## NuGet Packages Required

| Package | Version | Purpose |
|---------|---------|---------|
| BCrypt.Net-Next | 4.0.3 | Password hashing (BCrypt, cost 12) |
| System.IdentityModel.Tokens.Jwt | 8.x | JWT creation, parsing, validation |
| Microsoft.IdentityModel.Tokens | 8.x | SecurityKey, SigningCredentials |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.x | JWT Bearer middleware |
| StackExchange.Redis | 2.8.x | Token blacklist, rate limit counters |
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

**Entities** (`src/PhanMemKeToan.Domain/Entities/`):

- **`Tenant.cs`** — `id` (Guid PK), `Code` (string, unique), `Name`, `DatabaseSchemaName`, `IsActive`, `CreatedAt`. Does NOT inherit `AuditableEntity` (system-level entity, no tenant context for audit).
- **`User.cs`** — Inherits `AuditableEntity` (already includes `TenantId` via `ITenantEntity`). Fields: `Email`, `PasswordHash`, `FullName`, `IsActive`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil`. Navigation: `UserRoles`, `RefreshTokens`.
- **`Role.cs`** — Inherits `AuditableEntity` (already includes `TenantId` via `ITenantEntity`). Fields: `Name`, `Description`. Navigation: `UserRoles`, `RolePermissions`.
- **`Permission.cs`** — Inherits `BaseEntity`. Fields: `Code` (unique, e.g. `GL.Journal.View`), `Name`, `ModuleCode`. NOT scoped to tenant — global/shared.
- **`UserRole.cs`** — Join entity. `UserId` (FK → User), `RoleId` (FK → Role). NO inheritance (no audit needed on join tables).
- **`RolePermission.cs`** — Join entity. `RoleId` (FK → Role), `PermissionId` (FK → Permission).
- **`RefreshToken.cs`** — NO `AuditableEntity` (high-write; audit fields add unnecessary noise). Fields: `UserId (FK)`, `TokenHash` (SHA-256 hex, unique), `TokenFamily` (Guid — lineage tracking), `ExpiresAt`, `IssuedAt`, `IsRevoked`.

**Domain Exceptions** (`src/PhanMemKeToan.Domain/Common/Exceptions/`):
- `InvalidCredentialsException` — 401, generic message (no email/password differentiation)
- `AccountDeactivatedException` — 401
- `AccountLockedException` — 429 (rate limited)
- `TenantNotFoundException` — 403
- `TenantDeactivatedException` — 403
- `TokenExpiredException` — 401
- `TokenRevokedException` — 401
- `DuplicateEmailException` — 409

---

### Phase 2: Infrastructure — Persistence (Day 2)

**`AppDbContext`** (`src/PhanMemKeToan.Infrastructure/Persistence/`):
- Registers all entity sets: `Tenants`, `Users`, `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `RefreshTokens`
- Constructor accepts `IOptions<DbContextOptions>` + `ITenantContext` (scoped DI)
- Calls `modelBuilder.UseSnakeCaseNamingConvention()` (EFCore.NamingConventions)
- Applies global filter: `modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == _tenantContext.TenantId)` — same for `Role`

**Entity Type Configurations** (Fluent API, one file per entity):
- `TenantConfiguration` — table `sys_tenants`, unique index `code`
- `UserConfiguration` — table `sys_users`, composite unique index `(tenant_id, email)`, index `tenant_id`
- `RoleConfiguration` — table `sys_roles`, composite unique index `(tenant_id, name)`, index `tenant_id`
- `PermissionConfiguration` — table `sys_permissions`, unique index `code`, index `module_code`
- `UserRoleConfiguration` — table `sys_user_roles`, composite PK `(user_id, role_id)`, cascade delete
- `RolePermissionConfiguration` — table `sys_role_permissions`, composite PK `(role_id, permission_id)`, cascade delete
- `RefreshTokenConfiguration` — table `sys_refresh_tokens`, unique index `token_hash`, composite index `(token_family, is_revoked)`, cascade delete on User

**Migrations**:
- `InitialAuth` — creates all 7 tables with constraints and indexes
- `SeedPermissions` — inserts all permission codes for 15 modules (View/Create/Edit/Delete/Post actions per module resource)
- `SeedSuperAdmin` — inserts default Tenant (code: `SYSTEM`), User (`superadmin@system.local`, hashed), Role (`SuperAdmin`), assigns all permissions to that role, assigns role to superadmin user

---

### Phase 3: Infrastructure — Services (Day 2–3)

**JWT Service** (`Services/JwtService.cs`):
- `IJwtService` interface: `GenerateAccessToken(UserClaimsDto)`, `GenerateRefreshToken()`, `ValidateToken(string)`, `GetJwks()`
- RS256: loads `RsaSecurityKey` from `appsettings` (Base64-encoded private key in dev; `.pfx` or Azure Key Vault reference in production)
- `GenerateAccessToken` embeds claims: `sub` (UserId), `tid` (TenantId), `email`, `roles` (JSON array), `permissions` (JSON array), `jti` (Guid), `iat`, `exp`
- `GetJwks()` returns `JsonWebKeySet` from the public key only

**Password Hasher** (`Services/BcryptPasswordHasher.cs`):
- `IPasswordHasher` interface: `HashPassword(string)`, `VerifyPassword(string, string)`
- BCrypt.Net-Next, cost 12

**Token Blacklist Service** (`Services/RedisTokenBlacklistService.cs`):
- `ITokenBlacklistService`: `BlacklistAsync(string jti, TimeSpan ttl)`, `IsBlacklistedAsync(string jti)`
- Redis key: `blacklist:jti:{jti}`, value = `"1"`, TTL = remaining token validity
- If Redis is unavailable: throw `ServiceUnavailableException` → GlobalExceptionHandler maps to 503; token rejected (fail-closed)

**Tenant Middleware** (`Middleware/TenantMiddleware.cs`):
- Resolves tenant from JWT `tid` claim first; falls back to `X-Tenant-Code` header
- Validates tenant is active via `ITenantRepository` (cached lookup, 5-minute Redis TTL)
- Sets `ITenantContext.TenantId` on the scoped service
- On unknown/inactive tenant: returns 403 JSON response immediately (short-circuits)

---

### Phase 4: Application Layer — CQRS Commands/Queries (Day 3–4)

All handlers live under `src/PhanMemKeToan.Application/Features/Auth/` and `Features/Users/` etc.

**Auth Commands/Queries**:
| Name | Type | Description |
|------|------|-------------|
| `LoginCommand` | Command | Validate rate limit → verify credentials → generate tokens → return `LoginResult` |
| `LoginCommandHandler` | Handler | Uses `IPasswordHasher`, `IJwtService`, `ITokenBlacklistService` |
| `RefreshTokenCommand` | Command | Validate refresh token hash → check revoked → rotate → return new `LoginResult` |
| `RefreshTokenCommandHandler` | Handler | Implements token family revocation on replay detection |
| `LogoutCommand` | Command | Blacklist JTI → revoke refresh token → void |
| `LogoutCommandHandler` | Handler | Calls `ITokenBlacklistService` + marks `RefreshToken.IsRevoked = true` |
| `GetCurrentUserQuery` | Query | Returns `CurrentUserDto` from token claims + DB lookup |
| `GetCurrentUserQueryHandler` | Handler | Loads User with Roles + Permissions from DB |

**User Management Commands/Queries**:
| Name | Type | Description |
|------|------|-------------|
| `CreateUserCommand` | Command | Create user with initial password hash + role assignments |
| `CreateUserCommandHandler` | Handler | Validates email uniqueness within tenant |
| `UpdateUserCommand` | Command | Update FullName, email, role assignments |
| `UpdateUserCommandHandler` | Handler | Re-validates uniqueness if email changes |
| `ToggleUserActivationCommand` | Command | Set `IsActive`; if deactivating, revoke all refresh tokens |
| `UnlockUserCommand` | Command | Reset `FailedLoginCount` and `LockedUntil` |
| `GetUsersQuery` | Query | Paginated + filtered list for tenant |
| `GetUserByIdQuery` | Query | Single user with roles |
| `UpdateProfileCommand` | Command | Update own FullName (from `ICurrentUserService`) |
| `ChangePasswordCommand` | Command | Verify current password → update hash |

**Role & Permission Commands/Queries**:
| Name | Type | Description |
|------|------|-------------|
| `CreateRoleCommand` | Command | Create role with optional initial permissions |
| `UpdateRoleCommand` | Command | Update name/description |
| `DeleteRoleCommand` | Command | Validate no active users → soft-delete Role (IsDeleted=true) + hard-delete RolePermission join rows |
| `GetRolesQuery` | Query | List roles with user count per role |
| `GetRoleByIdQuery` | Query | Single role with permissions |
| `AssignPermissionsToRoleCommand` | Command | Bulk replace permissions for a role |
| `GetPermissionsQuery` | Query | All permissions grouped by module |
| `GetPermissionMatrixQuery` | Query | All roles × all permissions cross-tab |

**Validators** (FluentValidation, one per Command):
- `LoginCommandValidator`: email format, password not empty
- `CreateUserCommandValidator`: email format, password complexity (8–128 chars, upper+lower+digit+special)
- `ChangePasswordCommandValidator`: `currentPassword` not empty, `newPassword` complexity
- `CreateRoleCommandValidator`: name not empty, max 100 chars
- `AssignPermissionsToRoleCommandValidator`: permission codes exist in DB

---

### Phase 5: API Layer (Day 4–5)

**Controllers** (`src/PhanMemKeToan.Api/`):

| Controller | Base Path | Auth | Key Actions |
|------------|----------|------|-------------|
| `AuthController` | `/api/auth` | Public (login/refresh) / Bearer (logout) | `POST /login`, `POST /refresh`, `POST /logout` |
| `JwksController` | `/.well-known` | Public | `GET /jwks.json` |
| `MeController` | `/api/me` | Bearer | `GET /`, `PUT /profile`, `PUT /password` |
| `UsersController` | `/api/users` | Bearer + `SYS.Users.*` | CRUD + activate/deactivate/unlock |
| `RolesController` | `/api/roles` | Bearer + `SYS.Roles.*` | CRUD |
| `PermissionsController` | `/api/permissions` | Bearer + `SYS.Roles.Manage` | Matrix GET/PUT + flat list GET |

**Program.cs registrations**:
- `AddAuthentication().AddJwtBearer(...)` with JWKS validation from `IJwtService.GetJwks()`
- `AddRateLimiter()` — fixed window policy on `/api/auth/login` for general throughput protection (request-level). **Note**: counting *failed* login attempts (FR-007, FR-010b) is handled at the Application layer in `LoginCommandHandler` using a Redis counter keyed on IP address — not by the ASP.NET Core built-in rate limiter, which counts all requests (not just failures).
- `AddResponseCaching()`, `AddHealthChecks().AddRedis(...)`, `AddHealthChecks().AddNpgSql(...)`
- `app.UseMiddleware<TenantMiddleware>()` — after `UseAuthentication`, before `UseAuthorization`
- CORS: `AddCors()` with named policies per environment (dev allows localhost; prod uses allowlist from config)
- `app.MapHealthChecks("/health")`

---

### Phase 6: Angular Frontend (Day 5–7)

**State & Services** (`src/webapp/src/app/core/auth/`):

| File | Purpose |
|------|---------|
| `auth.store.ts` | NgRx Signal Store: `token`, `user`, `isAuthenticated` (computed), `permissions` signals |
| `auth.service.ts` | `login()`, `logout()`, `refresh()`, `getMe()` HTTP calls |
| `auth.interceptor.ts` | Injects Bearer token; handles 401 → refresh with mutex; retries original request |
| `auth.guard.ts` | `CanActivateFn` — reads `isAuthenticated`, redirects to `/login` with `returnUrl` |
| `tenant.guard.ts` | `CanActivateFn` — verifies `user.tenantId` is set |

**Pages** (`src/webapp/src/app/features/sys/`):

| File | Route | Purpose |
|------|-------|---------|
| `login/login.component.ts` | `/login` | PrimeNG form, error messages, redirect post-login |
| `users/sys-users.component.ts` | `/sys/users` | p-table, search, pagination, activate/deactivate |
| `users/sys-users-form.component.ts` | Modal | Create/edit user dialog, role multi-select |
| `roles/sys-roles.component.ts` | `/sys/roles` | Role list, CRUD actions |
| `roles/sys-roles-form.component.ts` | Modal | Role create/edit with permission assignment |
| `permissions/sys-permissions.component.ts` | `/sys/permissions` | Permission matrix grid with module grouping |
| `me/me.component.ts` | `/me` | Profile view + edit FullName + change password |

**i18n** (`src/webapp/src/assets/i18n/vi.json`):
- All keys under namespace `SYS.*`: `SYS.AUTH.*`, `SYS.USERS.*`, `SYS.ROLES.*`, `SYS.PERMISSIONS.*`, `SYS.ME.*`
- Error key translations: `SYS.AUTH.INVALID_CREDENTIALS`, `SYS.AUTH.ACCOUNT_DEACTIVATED`, `SYS.AUTH.RATE_LIMITED`, etc.

---

## Testing Strategy

### Unit Tests (xUnit + Moq)

| Test Class | Scenarios |
|-----------|-----------|
| `LoginCommandHandlerTests` | Valid credentials → token returned; invalid password → `InvalidCredentialsException`; deactivated user → 401 before password check; rate limit hit → `AccountLockedException`; email not found → same generic error |
| `RefreshTokenCommandHandlerTests` | Valid refresh → new tokens issued + old revoked; expired token → 401; revoked token → entire family revoked; replay attack detected → all family tokens revoked |
| `JwtServiceTests` | Token generation has correct claims; RS256 signature validates with public key; expired token rejected; tampered token rejected; `GetJwks()` returns correct key material |
| `BcryptPasswordHasherTests` | Hash + verify roundtrip; wrong password returns false; hash is non-deterministic (salted) |
| `CreateUserCommandHandlerTests` | Duplicate email → `DuplicateEmailException`; invalid role id → validation error; cross-tenant role assignment blocked |
| `DeleteRoleCommandHandlerTests` | Role with active users → rejection with user list; role with no users → success |
| `TenantMiddlewareTests` | JWT tid claim → correct tenant resolved; header fallback; deactivated tenant → 403; unknown code → 403 |

### Integration Tests (EF Core InMemory / TestContainers PostgreSQL)

| Test | Assertion |
|------|-----------|
| Tenant isolation | User-A token cannot return User-B's data from any endpoint |
| Permission enforcement | Request with JWT missing `GL.Journal.View` → 403 on GL Journal list endpoint |
| Rate limiting | 6th login attempt within 5 min → 429; counter resets after window |
| Token rotation replay | Replay old refresh token → 401 + all family tokens revoked in DB |
| Global filter bypass | Direct `IgnoreQueryFilters()` call only available under `[SuperAdminPolicy]` — tested by attempting bypass as regular user |

### E2E Tests (Playwright)

| Scenario | Steps |
|----------|-------|
| Full login flow | Navigate `/login` → submit credentials → assert redirect to dashboard → assert tenant name visible |
| Transparent token refresh | Shorten access TTL to 10s → login → wait 12s → make API call → assert success + new token in memory |
| User management CRUD | Login as admin → create user → verify in list → deactivate → verify badge change → attempt login as deactivated user → assert 401 |
| Permission matrix | Create role → open matrix → toggle 3 permissions → save → re-open → assert saved state |

---

## Security Considerations

- **RS256 asymmetric JWT**: Private key stored in `dotnet user-secrets` (dev) / Azure Key Vault reference (prod). Public key exposed only via `/.well-known/jwks.json`. Never logs private key.
- **Refresh token security**: Stored as `SHA-256(plaintext)`. Plaintext only in HttpOnly + Secure + SameSite=Strict cookie. Token family lineage: replay of revoked token triggers full user session revocation.
- **Rate limiting (two-layer)**: (1) ASP.NET Core built-in `AddRateLimiter()` provides general request throttling on the login endpoint. (2) Application-layer Redis counter (`ratelimit:login:{ip}`) tracks *failed attempts only* — incremented in `LoginCommandHandler` on each authentication failure, with 5-minute sliding window TTL. After 5 failed attempts: `AccountLockedException` → HTTP 429. Counter expires automatically after the window. If Redis is unavailable: fail-open on counter (BCrypt still provides protection); blacklist checks remain fail-closed.
- **BCrypt cost 12**: ~300ms per verification — acceptable UX latency; prevents automated dictionary attacks.
- **CORS allowlist**: Configured per environment in `appsettings.{Environment}.json`. Wildcard `*` is forbidden in production.
- **No stack traces in production**: `GlobalExceptionHandler` already present; maps domain exceptions to RFC 7807 `ProblemDetails`.
- **Default-deny authorization**: All controllers require `[Authorize]`. Only `AuthController`, `JwksController`, and health endpoints are explicitly public (`[AllowAnonymous]`).
- **Input validation**: All commands have FluentValidation validators registered in the MediatR pipeline behavior. Server-side validation runs before any business logic.
- **Audit trail**: `AuditableEntity` captures `CreatedBy/UpdatedBy` via `ICurrentUserService`. All security-sensitive mutations are auditable.
- **Password complexity** (OWASP ASVS §2.1.1): 8–128 chars, upper + lower + digit + special character. Enforced in `CreateUserCommandValidator` and `ChangePasswordCommandValidator`.
