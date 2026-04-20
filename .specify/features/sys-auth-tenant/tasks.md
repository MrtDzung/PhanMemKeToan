# Tasks: Auth & Tenant Core (SYS Module)

**Feature**: `sys-auth-tenant`
**Generated**: 2026-04-15
**Total Tasks**: 109 | **Phases**: 10 | **Backend**: 66 | **Frontend**: 24 | **Tests**: 17 | **Config/Migration**: 12

**Input**: `.specify/features/sys-auth-tenant/`
**Prerequisites**: plan.md ✅ spec.md ✅ data-model.md ✅ contracts/ ✅

---

## Phase 1: Domain Layer (Day 1)

- [x] T001 [BACKEND] Fix AuditableEntity: add `IsDeleted` (bool, default false) — `src/PhanMemKeToan.Domain/Common/AuditableEntity.cs`, `src/PhanMemKeToan.Domain/Common/IAuditableEntity.cs`
- [x] T002 [P] [BACKEND] Create Tenant entity (inherits BaseEntity; Code, Name, DatabaseSchemaName?, IsActive, CreatedAt; does NOT inherit AuditableEntity) — `src/PhanMemKeToan.Domain/Entities/Tenant.cs`
- [x] T003 [P] [BACKEND] Create User entity (inherits AuditableEntity; Email, PasswordHash, FullName, IsActive, LastLoginAt?, FailedLoginCount, LockedUntil?; nav: UserRoles, RefreshTokens) — requires T001 — `src/PhanMemKeToan.Domain/Entities/User.cs`
- [x] T004 [P] [BACKEND] Create Role entity (inherits AuditableEntity; Name, Description?; nav: UserRoles, RolePermissions) — requires T001 — `src/PhanMemKeToan.Domain/Entities/Role.cs`
- [x] T005 [P] [BACKEND] Create Permission entity (inherits BaseEntity; Code, Name, ModuleCode varchar(10); nav: RolePermissions; NOT tenant-scoped) — `src/PhanMemKeToan.Domain/Entities/Permission.cs`
- [x] T006 [BACKEND] Create UserRole join entity (no inheritance; UserId FK, RoleId FK; nav: User, Role) — requires T003, T004 — `src/PhanMemKeToan.Domain/Entities/UserRole.cs`
- [x] T007 [BACKEND] Create RolePermission join entity (no inheritance; RoleId FK, PermissionId FK; nav: Role, Permission) — requires T004, T005 — `src/PhanMemKeToan.Domain/Entities/RolePermission.cs`
- [x] T008 [BACKEND] Create RefreshToken entity (no inheritance; UserId FK, TokenHash varchar(64), TokenFamily Guid, ExpiresAt, IssuedAt, IsRevoked bool; nav: User) — requires T003 — `src/PhanMemKeToan.Domain/Entities/RefreshToken.cs`
- [x] T009 [P] [BACKEND] Create `InvalidCredentialsException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/InvalidCredentialsException.cs`
- [x] T010 [P] [BACKEND] Create `AccountDeactivatedException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/AccountDeactivatedException.cs`
- [x] T011 [P] [BACKEND] Create `AccountLockedException` (HTTP 429; includes `LockedUntil` DateTimeOffset) — `src/PhanMemKeToan.Domain/Common/Exceptions/AccountLockedException.cs`
- [x] T012 [P] [BACKEND] Create `TenantNotFoundException` (HTTP 403) — `src/PhanMemKeToan.Domain/Common/Exceptions/TenantNotFoundException.cs`
- [x] T013 [P] [BACKEND] Create `TenantDeactivatedException` (HTTP 403) — `src/PhanMemKeToan.Domain/Common/Exceptions/TenantDeactivatedException.cs`
- [x] T014 [P] [BACKEND] Create `TokenExpiredException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/TokenExpiredException.cs`
- [x] T015 [P] [BACKEND] Create `TokenRevokedException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/TokenRevokedException.cs`
- [x] T016 [P] [BACKEND] Create `DuplicateEmailException` (HTTP 409; includes `email` string property) — `src/PhanMemKeToan.Domain/Common/Exceptions/DuplicateEmailException.cs`
- [x] T017 [P] [BACKEND] Create `RoleInUseException` (HTTP 409; includes `IReadOnlyList<string> AffectedUserNames`) — `src/PhanMemKeToan.Domain/Common/Exceptions/RoleInUseException.cs`
- [x] T018 [P] [BACKEND] Create `IJwtService` + `UserClaimsDto` record (UserId, TenantId, Email, Roles, Permissions, Jti); methods: GenerateAccessToken, GenerateRefreshToken→(plaintext, hash), ValidateToken, GetJwks — `src/PhanMemKeToan.Application/Common/Interfaces/IJwtService.cs`
- [x] T019 [P] [BACKEND] Create `IPasswordHasher`: HashPassword(string)→string, VerifyPassword(string,string)→bool — `src/PhanMemKeToan.Application/Common/Interfaces/IPasswordHasher.cs`
- [x] T020 [P] [BACKEND] Create `ITokenBlacklistService`: BlacklistAsync(jti, ttl), IsBlacklistedAsync(jti) — `src/PhanMemKeToan.Application/Common/Interfaces/ITokenBlacklistService.cs`
- [x] T021 [P] [BACKEND] Create `ITenantContext`: scoped TenantId (Guid? get/set) — `src/PhanMemKeToan.Application/Common/Interfaces/ITenantContext.cs`
- [x] T022 [P] [BACKEND] Create `ITenantRepository` + `TenantDto`: GetByCodeAsync, GetByIdAsync — `src/PhanMemKeToan.Application/Common/Interfaces/ITenantRepository.cs`
- [x] T023 [P] [BACKEND] Extend `IApplicationDbContext` with DbSet<> for all 7 new entities — `src/PhanMemKeToan.Application/Common/Interfaces/IApplicationDbContext.cs`

---

## Phase 2: Infrastructure — Persistence (Day 1–2)

- [x] T024 [CONFIG] Add NuGet packages to Infrastructure.csproj: BCrypt.Net-Next 4.0.3, System.IdentityModel.Tokens.Jwt 8.x, Microsoft.IdentityModel.Tokens 8.x, EFCore.NamingConventions 10.x, StackExchange.Redis 2.8.x — `src/PhanMemKeToan.Infrastructure/PhanMemKeToan.Infrastructure.csproj`
- [x] T025 [BACKEND] Update AppDbContext: inject ITenantContext; add 7 DbSet<> properties; call UseSnakeCaseNamingConvention(); apply HasQueryFilter on User and Role ONLY (NOT on Tenant/Permission/RefreshToken/UserRole/RolePermission) — `src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs`
- [x] T026 [P] [BACKEND] TenantConfiguration: ToTable("sys_tenants"), unique index Code — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- [x] T027 [P] [BACKEND] UserConfiguration: ToTable("sys_users"), unique (TenantId, Email), index TenantId, varchar lengths — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- [x] T028 [P] [BACKEND] RoleConfiguration: ToTable("sys_roles"), unique (TenantId, Name), index TenantId — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- [x] T029 [P] [BACKEND] PermissionConfiguration: ToTable("sys_permissions"), unique Code, index ModuleCode — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs`
- [x] T030 [P] [BACKEND] UserRoleConfiguration: ToTable("sys_user_roles"), composite PK (UserId, RoleId), cascade deletes — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs`
- [x] T031 [P] [BACKEND] RolePermissionConfiguration: ToTable("sys_role_permissions"), composite PK (RoleId, PermissionId), cascade deletes — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs`
- [x] T032 [P] [BACKEND] RefreshTokenConfiguration: ToTable("sys_refresh_tokens"), unique TokenHash, composite index (TokenFamily, IsRevoked), cascade delete on UserId — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- [x] T033 [MIGRATION] Run: `dotnet ef migrations add InitialAuth --project src/PhanMemKeToan.Infrastructure --startup-project src/PhanMemKeToan.Api` — requires T025–T032
- [x] T034 [MIGRATION] Data migration SeedPermissions: ~75 permission records for 15 modules (DI,GL,CA,BA,PU,SA,IN,FA,SU,JC,PA,TA,CT,IP,SYS) pattern MODULE.Resource.Action — requires T033
- [x] T035 [MIGRATION] Data migration SeedSuperAdmin: Tenant(code:SYSTEM), User(superadmin@system.local, BCrypt hash), Role(SuperAdmin), assign all permissions, assign role to user — requires T034

---

## Phase 3: Infrastructure — Services (Day 2–3)

- [x] T036 [BACKEND] Create JwtService (IJwtService): RS256 from appsettings PrivateKeyPemBase64; GenerateAccessToken with claims (sub,tid,email,roles,permissions,jti,iat,exp); GenerateRefreshToken→(plaintext, sha256Hash); ValidateToken; GetJwks() returns public key ONLY — `src/PhanMemKeToan.Infrastructure/Services/JwtService.cs`
- [x] T037 [P] [BACKEND] Create BcryptPasswordHasher (IPasswordHasher): BCrypt.Net-Next cost=12 — `src/PhanMemKeToan.Infrastructure/Services/BcryptPasswordHasher.cs`
- [x] T038 [P] [BACKEND] Create RedisTokenBlacklistService (ITokenBlacklistService): key=blacklist:jti:{jti}, TTL=remaining validity; Redis unavailable→throw ServiceUnavailableException (fail-closed) — `src/PhanMemKeToan.Infrastructure/Services/RedisTokenBlacklistService.cs`
- [x] T039 [P] [BACKEND] Create TenantContext (ITenantContext): scoped, mutable TenantId getter/setter — `src/PhanMemKeToan.Infrastructure/Services/TenantContext.cs`
- [x] T040 [P] [BACKEND] Create TenantRepository (ITenantRepository): query Tenants with IgnoreQueryFilters(); cache in Redis 5-min TTL — `src/PhanMemKeToan.Infrastructure/Services/TenantRepository.cs`
- [x] T041 [BACKEND] Create TenantMiddleware: (1) JWT tid claim→GetByIdAsync; (2) else X-Tenant-Code header→GetByCodeAsync; (3) not found or IsActive=false→403 ProblemDetails short-circuit; (4) set ITenantContext.TenantId — requires T039, T040 — `src/PhanMemKeToan.Infrastructure/Middleware/TenantMiddleware.cs`
- [x] T042 [BACKEND] Register all Phase 3 services in DependencyInjection.cs: IConnectionMultiplexer(singleton), IJwtService(singleton), IPasswordHasher(singleton), ITokenBlacklistService(scoped), ITenantContext(scoped), ITenantRepository(scoped) — `src/PhanMemKeToan.Infrastructure/DependencyInjection.cs`

---

## Phase 4: Application Layer — Auth CQRS (Day 3)

- [x] T043 [BACKEND] Verify Application/DependencyInjection.cs: ValidationBehavior + LoggingBehavior registered; AddValidatorsFromAssembly scans new validators — `src/PhanMemKeToan.Application/DependencyInjection.cs`
- [x] T044 [BACKEND] [US1] Create LoginCommand CQRS: command(Email, Password, RememberMe, IpAddress); handler (1:check Redis ratelimit:login:{ip} counter; 2:load user; 3:check IsActive→AccountDeactivatedException; 4:VerifyPassword; 5:on failure increment counter, AccountLockedException at 5; 6:update LastLoginAt, reset FailedLoginCount; 7:generate tokens; 8:persist RefreshToken hash; 9:structured Serilog log with eventType/userId/tenantId/ipAddress/success fields [FR-033]; 10:return LoginResult); validator(email format, password non-empty max 128) — `src/PhanMemKeToan.Application/Features/Auth/Commands/Login/`
- [x] T045 [BACKEND] [US2] Create RefreshTokenCommand CQRS: handler (1:find by hash; 2:check ExpiresAt+IsRevoked; 3:replay:revoked hash→revoke entire TokenFamily→TokenRevokedException; 4:mark old IsRevoked=true; 5:generate+persist new pair; 6:structured Serilog log with eventType/userId/tenantId/success/isReplay fields [FR-033]; 7:return LoginResult) — `src/PhanMemKeToan.Application/Features/Auth/Commands/RefreshToken/`
- [x] T046 [BACKEND] [US1] Create LogoutCommand CQRS: handler (1:BlacklistAsync(jti, remainingTtl); 2:mark RefreshToken.IsRevoked=true; 3:save; 4:structured Serilog log with eventType/userId/tenantId/jti fields [FR-033]) — `src/PhanMemKeToan.Application/Features/Auth/Commands/Logout/`
- [x] T047 [BACKEND] [US6] Create GetCurrentUserQuery CQRS: handler loads User with UserRoles→Role→RolePermissions→Permission; returns CurrentUserDto(Id, Email, FullName, IsActive, TenantId, TenantName, Roles[], Permissions[], LastLoginAt, CreatedAt) — `src/PhanMemKeToan.Application/Features/Auth/Queries/GetCurrentUser/`
- [x] T048 [P] [BACKEND] Extend GlobalExceptionHandler: map all 9 new domain exceptions to RFC 7807 ProblemDetails with errorCode strings — `src/PhanMemKeToan.Api/Infrastructure/GlobalExceptionHandler.cs`

---

## Phase 5: Application Layer — User/Role/Permission CQRS (Day 3–4)

- [x] T049 [P] [US4] [BACKEND] CreateUserCommand CQRS: handler(email uniqueness check; hash password; create User+UserRole rows; DuplicateEmailException on conflict); validator(email format, OWASP password complexity) — `src/PhanMemKeToan.Application/Features/Users/Commands/CreateUser/`
- [x] T050 [P] [US4] [BACKEND] UpdateUserCommand CQRS: handler(re-validate email if changed; replace UserRole rows atomically) — `src/PhanMemKeToan.Application/Features/Users/Commands/UpdateUser/`
- [x] T051 [P] [US4] [BACKEND] ToggleUserActivationCommand CQRS: handler(set IsActive; on deactivate→revoke all RefreshTokens for user) — `src/PhanMemKeToan.Application/Features/Users/Commands/ToggleUserActivation/`
- [x] T052 [P] [US4] [BACKEND] UnlockUserCommand CQRS: handler(reset FailedLoginCount=0, LockedUntil=null) — `src/PhanMemKeToan.Application/Features/Users/Commands/UnlockUser/`
- [x] T053 [P] [US4] [BACKEND] GetUsersQuery CQRS: paginated+filtered; include Roles; return PaginatedResult<UserListDto>; create PaginatedResult<T> model — `src/PhanMemKeToan.Application/Features/Users/Queries/GetUsers/`, `src/PhanMemKeToan.Application/Common/Models/PaginatedResult.cs`
- [x] T054 [P] [US4] [BACKEND] GetUserByIdQuery CQRS: load User with UserRoles→Role; throw NotFoundException if missing — `src/PhanMemKeToan.Application/Features/Users/Queries/GetUserById/`
- [x] T055 [P] [US6] [BACKEND] UpdateProfileCommand CQRS: read UserId from ICurrentUserService; update FullName — `src/PhanMemKeToan.Application/Features/Users/Commands/UpdateProfile/`
- [x] T056 [P] [US6] [BACKEND] ChangePasswordCommand CQRS: VerifyPassword(current)→InvalidCredentialsException on fail; HashPassword(new)→update; validator(OWASP complexity) — `src/PhanMemKeToan.Application/Features/Users/Commands/ChangePassword/`
- [x] T057 [P] [US5] [BACKEND] CreateRoleCommand CQRS: unique name within tenant; create Role+RolePermission rows — `src/PhanMemKeToan.Application/Features/Roles/Commands/CreateRole/`
- [x] T058 [P] [US5] [BACKEND] UpdateRoleCommand CQRS: update name/description; re-validate uniqueness if name changes — `src/PhanMemKeToan.Application/Features/Roles/Commands/UpdateRole/`
- [x] T059 [P] [US5] [BACKEND] DeleteRoleCommand CQRS: query active UserRole rows→RoleInUseException if any; else set Role.IsDeleted=true (soft-delete) + hard-delete all RolePermission join rows for that role — `src/PhanMemKeToan.Application/Features/Roles/Commands/DeleteRole/`
- [x] T060 [P] [US5] [BACKEND] GetRolesQuery CQRS: list all roles for tenant with UserCount and PermissionCount — `src/PhanMemKeToan.Application/Features/Roles/Queries/GetRoles/`
- [x] T061 [P] [US5] [BACKEND] GetRoleByIdQuery CQRS: load Role with RolePermissions→Permission — `src/PhanMemKeToan.Application/Features/Roles/Queries/GetRoleById/`
- [x] T062 [P] [US5] [BACKEND] AssignPermissionsToRoleCommand CQRS: bulk replace RolePermission rows; validate PermissionIds exist — `src/PhanMemKeToan.Application/Features/Roles/Commands/AssignPermissions/`
- [x] T063 [P] [US5] [BACKEND] GetPermissionsQuery CQRS: load all permissions grouped by ModuleCode — `src/PhanMemKeToan.Application/Features/Permissions/Queries/GetPermissions/`
- [x] T064 [P] [US5] [BACKEND] GetPermissionMatrixQuery CQRS: build cross-tab {moduleCode, permissions: [{permissionId, code, name, roleAssignments: {roleId: bool}}]} — `src/PhanMemKeToan.Application/Features/Permissions/Queries/GetPermissionMatrix/`

---

## Phase 6: API Layer (Day 4–5)

- [x] T065 [CONFIG] Add NuGet to Api.csproj: Microsoft.AspNetCore.Authentication.JwtBearer 10.x, AspNetCore.HealthChecks.Redis 10.x, AspNetCore.HealthChecks.NpgSql 10.x — `src/PhanMemKeToan.Api/PhanMemKeToan.Api.csproj`
- [x] T066 [BACKEND] Register JWT Bearer in Program.cs: AddAuthentication().AddJwtBearer() with RSA public key from IJwtService.GetJwks(); validate iss/aud/exp/RS256; OnTokenValidated→IsBlacklistedAsync(jti)→fail if blacklisted — `src/PhanMemKeToan.Api/Program.cs`
- [x] T067 [BACKEND] Register middleware in Program.cs: AddRateLimiter (fixed-window on /api/auth/login); AddCors (allowlist from appsettings, no wildcard in prod); UseMiddleware<TenantMiddleware> after UseAuthentication before UseAuthorization; AddHealthChecks().AddRedis()+AddNpgSql(); MapHealthChecks("/health") — `src/PhanMemKeToan.Api/Program.cs`
- [x] T068 [P] [BACKEND] Create AuthController [AllowAnonymous]: POST /api/auth/login (set HttpOnly+Secure+SameSite=Strict refresh cookie, Max-Age if rememberMe); POST /api/auth/refresh (read cookie→SHA256 hash→RefreshTokenCommand; rotate cookie); POST /api/auth/logout [Authorize] (extract JTI+expiry from claims, cookie hash→LogoutCommand; clear cookie) — `src/PhanMemKeToan.Api/Controllers/AuthController.cs`
- [x] T069 [P] [BACKEND] Create JwksController [AllowAnonymous]: GET /.well-known/jwks.json → IJwtService.GetJwks(); Cache-Control: public, max-age=3600 — `src/PhanMemKeToan.Api/Controllers/JwksController.cs`
- [x] T070 [P] [BACKEND] Create MeController [Authorize]: GET /api/me; PUT /api/me/profile; PUT /api/me/password — `src/PhanMemKeToan.Api/Controllers/MeController.cs`
- [x] T071 [P] [BACKEND] Create UsersController [Authorize]: GET /api/users (SYS.Users.View); GET /api/users/{id}; POST /api/users (SYS.Users.Manage, 201); PUT /api/users/{id}; POST /api/users/{id}/deactivate (SYS.Users.Manage, cannot deactivate self); POST /api/users/{id}/reactivate (SYS.Users.Manage); POST /api/users/{id}/unlock (SYS.Users.Manage) — `src/PhanMemKeToan.Api/Controllers/UsersController.cs`
- [x] T072 [P] [BACKEND] Create RolesController [Authorize]: GET /api/roles (SYS.Roles.View); GET /api/roles/{id}; POST /api/roles (SYS.Roles.Manage, 201); PUT /api/roles/{id}; DELETE /api/roles/{id} — `src/PhanMemKeToan.Api/Controllers/RolesController.cs`
- [x] T073 [P] [BACKEND] Create PermissionsController [Authorize, SYS.Roles.Manage]: GET /api/permissions; GET /api/permissions/matrix; PUT /api/permissions/matrix (roleId in request body, not path) — `src/PhanMemKeToan.Api/Controllers/PermissionsController.cs`
- [x] T074 [P] [CONFIG] Add config sections to appsettings: JwtSettings (Issuer, Audience, AccessTokenTtlMinutes:15, RefreshTokenTtlDays:7, PrivateKeyPemBase64 via user-secrets, PublicKeyPemBase64); CorsSettings.AllowedOrigins — `src/PhanMemKeToan.Api/appsettings.json`, `src/PhanMemKeToan.Api/appsettings.Development.json`

---

## Phase 7: Backend Tests (Day 5)

- [x] T075 [TEST] [US1] LoginCommandHandlerTests (5 scenarios): valid credentials→LoginResult; wrong password→InvalidCredentialsException; deactivated user→AccountDeactivatedException BEFORE VerifyPassword; 6th attempt→AccountLockedException; unknown email→same generic error as wrong password — `tests/PhanMemKeToan.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs`
- [x] T076 [P] [TEST] [US2] RefreshTokenCommandHandlerTests (4 scenarios): valid→new pair+old revoked; expired→TokenExpiredException; revoked→TokenRevokedException; replay→all TokenFamily tokens IsRevoked=true in DB — `tests/PhanMemKeToan.Application.Tests/Features/Auth/RefreshTokenCommandHandlerTests.cs`
- [x] T077 [P] [TEST] JwtServiceTests (5 scenarios): all claims present; RS256 signature validates with public key; expired rejected; tampered rejected; GetJwks() does NOT expose private exponent d — `tests/PhanMemKeToan.Infrastructure.Tests/Services/JwtServiceTests.cs`
- [x] T078 [P] [TEST] BcryptPasswordHasherTests (3 scenarios): hash+verify roundtrip; wrong password→false; same input→different hashes (salted) — `tests/PhanMemKeToan.Infrastructure.Tests/Services/BcryptPasswordHasherTests.cs`
- [x] T079 [P] [TEST] [US4] CreateUserCommandHandlerTests (3 scenarios): duplicate email→DuplicateEmailException; weak password→ValidationException; cross-tenant role→NotFoundException — `tests/PhanMemKeToan.Application.Tests/Features/Users/CreateUserCommandHandlerTests.cs`
- [x] T080 [P] [TEST] [US5] DeleteRoleCommandHandlerTests (2 scenarios): role has active users→RoleInUseException with AffectedUserNames; no users→Role.IsDeleted=true (soft-deleted) AND RolePermission rows hard-deleted from DB — `tests/PhanMemKeToan.Application.Tests/Features/Roles/DeleteRoleCommandHandlerTests.cs`
- [x] T081 [P] [TEST] TenantMiddlewareTests (4 scenarios): JWT tid→TenantContext set; X-Tenant-Code header fallback; deactivated tenant→403; unknown code→403 — `tests/PhanMemKeToan.Infrastructure.Tests/Middleware/TenantMiddlewareTests.cs`
- [x] T082 [TEST] [US3] TenantIsolationIntegrationTests (TestContainers PostgreSQL): 2 tenants; user-A record invisible to user-B on read+write+existence-probe — `tests/PhanMemKeToan.Api.Tests/Integration/TenantIsolationIntegrationTests.cs`
- [x] T083 [P] [TEST] [US5] PermissionEnforcementIntegrationTests: missing SYS.Users.Manage→403; with SYS.Users.Manage→200 — `tests/PhanMemKeToan.Api.Tests/Integration/PermissionEnforcementIntegrationTests.cs`
- [x] T084 [P] [TEST] [US1] RateLimitingIntegrationTests: 5 failed→6th returns 429; TTL expiry→counter reset — `tests/PhanMemKeToan.Api.Tests/Integration/RateLimitingIntegrationTests.cs`
- [x] T085 [P] [TEST] [US2] TokenReplayIntegrationTests: login; refresh; replay old hash→401; assert ALL TokenFamily tokens IsRevoked=true in DB — `tests/PhanMemKeToan.Api.Tests/Integration/TokenReplayIntegrationTests.cs`

---

## Phase 8: Frontend Core (Day 5–6)

- [x] T086 [CONFIG] Verify package.json: @ngrx/signals 20.x, @ngx-translate/core 15.x, primeng 20.x, primeicons 7.x, tailwindcss 4.x; add missing; npm install — `webapp/package.json`
- [x] T087 [FRONTEND] Configure app.config.ts: provideHttpClient(withInterceptors([authInterceptor])), provideTranslateModule with HttpLoaderFactory, PrimeNG theme, provideRouter(appRoutes) — `webapp/src/app/app.config.ts`
- [x] T088 [FRONTEND] Create AuthStore (signalStore): signals token(string|null), user(CurrentUserDto|null); computed isAuthenticated, permissions; methods setAuth(token,user), clearAuth(), updateUser(user); in-memory ONLY — `webapp/src/app/core/auth/auth.store.ts`
- [x] T089 [FRONTEND] Create AuthService: login()→POST /api/auth/login→store.setAuth(); logout()→POST /api/auth/logout→store.clearAuth(); refresh()→POST /api/auth/refresh→store.setAuth()→return token; getMe()→GET /api/me→store.updateUser() — `webapp/src/app/core/auth/auth.service.ts`
- [x] T090 [FRONTEND] Create authInterceptor (HttpInterceptorFn): inject Bearer token; on 401 (not /auth/refresh)→BehaviorSubject<boolean> mutex: first caller invokes refresh(), sets isRefreshing=true; others wait; on token update retry all; on refresh fail→clearAuth()+navigate('/login') — `webapp/src/app/core/auth/auth.interceptor.ts`
- [x] T091 [P] [FRONTEND] Create authGuard (CanActivateFn): isAuthenticated→true; else navigate('/login', {queryParams:{returnUrl}})→false — `webapp/src/app/core/auth/auth.guard.ts`
- [x] T092 [P] [FRONTEND] Create tenantGuard (CanActivateFn): user().tenantId present→true; else navigate('/login')→false — `webapp/src/app/core/auth/tenant.guard.ts`
- [x] T093 [FRONTEND] Configure app.routes.ts: /login public; all routes canActivate:[authGuard]; /sys lazy-loads sys.routes with canActivate:[tenantGuard]; /me→MeComponent — `webapp/src/app/app.routes.ts`
- [x] T094 [P] [FRONTEND] Create vi.json with all SYS.AUTH.*, SYS.USERS.*, SYS.ROLES.*, SYS.PERMISSIONS.*, SYS.ME.*, COMMON.* i18n keys — `webapp/src/assets/i18n/vi.json`

---

## Phase 9: Frontend Pages (Day 6–7)

- [x] T095 [FRONTEND] [US1] LoginComponent (/login): p-fluid form with p-inputtext(email), p-password, p-checkbox(rememberMe), p-button(loading signal); inline server error display via translate pipe; on success navigate to returnUrl — `webapp/src/app/features/login/login.component.ts`
- [x] T096 [FRONTEND] [US4] SysUsersComponent (/sys/users): p-table lazy pagination; columns: FullName, Email, Roles(p-tag), Status badge, LastLoginAt; toolbar: debounced search, Create button; row actions: Edit, Activate/Deactivate, Unlock; column resize/reorder→localStorage; loading/empty/error states — `webapp/src/app/features/sys/users/sys-users.component.ts`
- [x] T097 [P] [FRONTEND] [US4] SysUsersFormComponent (p-dialog modal): p-inputtext(fullName,email), p-password(create only), p-multiSelect(roles); client-side validation; emit (saved) event; dirty guard on close — `webapp/src/app/features/sys/users/sys-users-form.component.ts`
- [x] T098 [FRONTEND] [US5] SysRolesComponent (/sys/roles): p-table role list; columns: Name, Description, UserCount(p-badge), PermissionCount; Create/Edit/Delete actions; p-confirmDialog; Delete disabled with tooltip when userCount>0 — `webapp/src/app/features/sys/roles/sys-roles.component.ts`
- [x] T099 [P] [FRONTEND] [US5] SysRolesFormComponent (p-dialog modal): Name, Description; permission assignment grouped by ModuleCode with p-checkbox; dirty guard — `webapp/src/app/features/sys/roles/sys-roles-form.component.ts`
- [x] T100 [FRONTEND] [US5] SysPermissionsComponent (/sys/permissions): GET /api/permissions/matrix; grid rows=permissions by ModuleCode, cols=roles; cells=p-checkbox; track changes; single Save→PUT /api/permissions/matrix (body: {roleId, permissionIds[]}) per modified role; p-toast success/error — `webapp/src/app/features/sys/permissions/sys-permissions.component.ts`
- [x] T101 [FRONTEND] [US6] MeComponent (/me): two p-card sections: Profile(read-only email, editable FullName, Save→PUT /api/me/profile) and Security(p-password inputs currentPassword/newPassword/confirmPassword, Save→PUT /api/me/password); p-toast on success — `webapp/src/app/features/sys/me/me.component.ts`
- [x] T102 [FRONTEND] Create sys.routes.ts: /sys/users→SysUsersComponent, /sys/roles→SysRolesComponent, /sys/permissions→SysPermissionsComponent; canActivate:[tenantGuard] on all — `webapp/src/app/features/sys/sys.routes.ts`

---

## Phase 10: E2E Tests (Day 7)

- [x] T103 [TEST] [US1] login.spec.ts: valid credentials→redirect to /→tenant name visible; wrong password→inline error; deactivated→ACCOUNT_DEACTIVATED error; use client-side nav (history.pushState) NOT page.goto() — `tests/e2e/login.spec.ts`
- [x] T104 [TEST] [US2] token-refresh.spec.ts: configure AccessTokenTtlMinutes to 10s; login; page.route intercept; wait 12s; GET /api/me; assert 200; assert exactly 1 POST /api/auth/refresh; no 401 surfaced to user — `tests/e2e/token-refresh.spec.ts`
- [x] T105 [P] [TEST] [US4] user-management.spec.ts: create user; verify in table; deactivate; assert badge; login as deactivated→assert ACCOUNT_DEACTIVATED — `tests/e2e/user-management.spec.ts`
- [x] T106 [P] [TEST] [US5] permission-matrix.spec.ts: create role via API; open /sys/permissions; toggle 3 checkboxes; save; navigate away (client-side); navigate back; assert 3 checkboxes still checked — `tests/e2e/permission-matrix.spec.ts`

- [x] T107 [BACKEND] Create ExpiredTokenCleanupService (BackgroundService): runs every 6 hours; hard-deletes RefreshToken rows where ExpiresAt < UtcNow - 24h; register as hosted service in DependencyInjection.cs — `src/PhanMemKeToan.Infrastructure/Services/ExpiredTokenCleanupService.cs`
- [x] T108 [P] [TEST] [US4] ToggleActivationCommandHandlerTests (3 scenarios): deactivate active user→IsActive=false+all RefreshTokens revoked; reactivate inactive user→IsActive=true; attempt deactivate already-inactive→ALREADY_DEACTIVATED error — `tests/PhanMemKeToan.Application.Tests/Features/Users/ToggleActivationCommandHandlerTests.cs`
- [x] T109 [P] [TEST] [US4] UnlockUserCommandHandlerTests (2 scenarios): unlock locked user→FailedLoginCount=0+LockedUntil=null; unlock already-unlocked user→no-op/success — `tests/PhanMemKeToan.Application.Tests/Features/Users/UnlockUserCommandHandlerTests.cs`

---

## Dependency Summary

```
Phase 1 → Phase 2, Phase 3, Phase 4, Phase 5 (parallel after P1)
Phase 2 + 3 + 4 + 5 → Phase 6
Phase 6 → Phase 7 (backend tests) + Phase 8 (frontend core)
Phase 8 → Phase 9 (frontend pages)
Phase 9 → Phase 10 (E2E)
```

**Story→Phase Mapping**:
- US3 Tenant Isolation: Phase 1–3
- US1 Login: Phase 1–4 + T068
- US2 Token Refresh: US1 + T045 + T068
- US4 User Management: US1 + T049–T054 + T071
- US5 Role & Permission: US4 + T057–T064 + T072–T073
- US6 My Profile: US1 + T047 + T055–T056 + T070

**TASKS_TOTAL: 109 | PHASES: 10 | ESTIMATED_DAYS: 7–10**

---

## Phase 2: Dual-DB Migration (Phương án B Kết hợp)

> **Prerequisite**: Phase 1 (above) completed. All T001–T109 done.
> **Reference**: `/memories/repo/architecture-plan-b-combined.md` §6
> **Goal**: Migrate from single shared DB to Dual-DB architecture (Master DB + Tenant DB per company).

### Phase 2.0 — Bug Fixes (3 files)

- [ ] T2-001 [BUGFIX] Fix claim key `"tenant_id"` → `"tid"` in `CurrentUserService.cs` — `src/PhanMemKeToan.Api/Services/CurrentUserService.cs`
- [ ] T2-002 [BUGFIX] Fix BCrypt hash in SeedSuperAdmin migration — verify with `BCrypt.Verify()` before seed — `src/PhanMemKeToan.Infrastructure/Persistence/Migrations/`
- [ ] T2-003 [BUGFIX] Add `TenantId` property to `RefreshToken` entity — `src/PhanMemKeToan.Domain/Entities/RefreshToken.cs`

### Phase 2.1 — Domain Layer (6 files)

- [ ] T2-004 [NEW] Create `DatabaseMode` enum (CloudManaged, OnPremise) — `src/PhanMemKeToan.Domain/Enums/DatabaseMode.cs`
- [ ] T2-005 [NEW] Create `TenantDbStatus` enum (Online, Offline, Migrating) — `src/PhanMemKeToan.Domain/Enums/TenantDbStatus.cs`
- [ ] T2-006 [NEW] Create `MasterUser` entity (Id, Email, PasswordHash, FullName, IsActive, FailedLoginCount, LockedUntil, audit) — `src/PhanMemKeToan.Domain/Entities/MasterUser.cs`
- [ ] T2-007 [NEW] Create `MasterUserTenant` entity (MasterUserId, TenantId, IsDefault, DisplayRole, JoinedAt) — `src/PhanMemKeToan.Domain/Entities/MasterUserTenant.cs`
- [ ] T2-008 [MODIFY] Extend `Tenant` entity: +DatabaseMode, +DbStatus, +ConnectionStringEncrypted, +CloudflareSubdomain, +DbHost — `src/PhanMemKeToan.Domain/Entities/Tenant.cs`
- [ ] T2-009 [MODIFY] Make `User.PasswordHash` nullable (password lives in MasterUser now) — `src/PhanMemKeToan.Domain/Entities/User.cs`

### Phase 2.2 — Application Layer Interfaces (7 files)

- [ ] T2-010 [NEW] Create `IMasterDbContext` interface — `src/PhanMemKeToan.Application/Common/Interfaces/IMasterDbContext.cs`
- [ ] T2-011 [NEW] Create `ITenantConnectionResolver` interface — `src/PhanMemKeToan.Application/Common/Interfaces/ITenantConnectionResolver.cs`
- [ ] T2-012 [NEW] Create `IConnectionStringEncryptor` interface — `src/PhanMemKeToan.Application/Common/Interfaces/IConnectionStringEncryptor.cs`
- [ ] T2-013 [NEW] Create `SelectCompanyCommand` + `SelectCompanyCommandHandler` — `src/PhanMemKeToan.Application/Features/Auth/Commands/SelectCompany/`
- [ ] T2-014 [NEW] Create `SwitchCompanyCommand` + `SwitchCompanyCommandHandler` — `src/PhanMemKeToan.Application/Features/Auth/Commands/SwitchCompany/`
- [ ] T2-015 [MODIFY] Update `IApplicationDbContext` — remove auth tables (moved to MasterDbContext) — `src/PhanMemKeToan.Application/Common/Interfaces/IApplicationDbContext.cs`
- [ ] T2-016 [MODIFY] Update `LoginCommandHandler` result → return tempToken + companies instead of JWT — `src/PhanMemKeToan.Application/Features/Auth/Commands/Login/`

### Phase 2.3 — Infrastructure: Master DB (10 files)

- [ ] T2-017 [NEW] Create `MasterDbContext` with MasterUser, MasterUserTenant, Tenant, RefreshToken DbSets — `src/PhanMemKeToan.Infrastructure/Persistence/MasterDbContext.cs`
- [ ] T2-018 [NEW] Create `MasterUserConfiguration` (EF config) — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/Master/MasterUserConfiguration.cs`
- [ ] T2-019 [NEW] Create `MasterUserTenantConfiguration` (composite PK) — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/Master/MasterUserTenantConfiguration.cs`
- [ ] T2-020 [NEW] Create `TenantDbContextFactory` — resolves tenant connection + creates scoped ApplicationDbContext — `src/PhanMemKeToan.Infrastructure/Persistence/TenantDbContextFactory.cs`
- [ ] T2-021 [NEW] Create `DataProtectionEncryptor` (IConnectionStringEncryptor impl) — `src/PhanMemKeToan.Infrastructure/Services/DataProtectionEncryptor.cs`
- [ ] T2-022 [NEW] Create `TenantConnectionResolver` (ITenantConnectionResolver impl) — `src/PhanMemKeToan.Infrastructure/Services/TenantConnectionResolver.cs`
- [ ] T2-023 [MODIFY] Update `TenantConfiguration` — add DatabaseMode, DbStatus, ConnectionStringEncrypted columns — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- [ ] T2-024 [MODIFY] Update `RefreshTokenConfiguration` — add TenantId column + index — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- [ ] T2-025 [MODIFY] Update `ApplicationDbContext` — remove Tenant/RefreshToken DbSets (moved to Master) — `src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs`
- [ ] T2-026 [MODIFY] Update `DependencyInjection.cs` — register MasterDbContext + TenantDbContextFactory + new services — `src/PhanMemKeToan.Infrastructure/DependencyInjection.cs`

### Phase 2.4 — Infrastructure: Update Existing Services (3 files)

- [ ] T2-027 [MODIFY] Update `TenantRepository` — query MasterDbContext for tenant info — `src/PhanMemKeToan.Infrastructure/Persistence/Repositories/TenantRepository.cs`
- [ ] T2-028 [MODIFY] Update `ExpiredTokenCleanupService` — use MasterDbContext — `src/PhanMemKeToan.Infrastructure/Services/ExpiredTokenCleanupService.cs`
- [ ] T2-029 [MODIFY] Update `TenantMiddleware` — resolve tenant from JWT `tid` claim, use TenantConnectionResolver — `src/PhanMemKeToan.Infrastructure/Services/TenantMiddleware.cs`

### Phase 2.5 — Application: Rewrite Auth Handlers (6 files)

- [ ] T2-030 [REWRITE] `LoginCommandHandler` — Step 1: validate credentials against MasterUser, return tempToken + companies — `src/PhanMemKeToan.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- [ ] T2-031 [NEW] `SelectCompanyCommandHandler` — Step 2: validate tempToken, issue JWT with tid claim — `src/PhanMemKeToan.Application/Features/Auth/Commands/SelectCompany/SelectCompanyCommandHandler.cs`
- [ ] T2-032 [NEW] `SwitchCompanyCommandHandler` — verify MasterUserTenant access, issue new JWT — `src/PhanMemKeToan.Application/Features/Auth/Commands/SwitchCompany/SwitchCompanyCommandHandler.cs`
- [ ] T2-033 [MODIFY] `RefreshTokenCommandHandler` — use MasterDbContext for RefreshToken lookup, resolve TenantId — `src/PhanMemKeToan.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- [ ] T2-034 [MODIFY] `LogoutCommandHandler` — revoke in MasterDbContext — `src/PhanMemKeToan.Application/Features/Auth/Commands/Logout/LogoutCommandHandler.cs`
- [ ] T2-035 [MODIFY] `GetCurrentUserQueryHandler` — add companies list from MasterUserTenant — `src/PhanMemKeToan.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`

### Phase 2.6 — API Layer (5 files)

- [ ] T2-036 [MODIFY] `AuthController` — add POST /select-company + POST /switch-company endpoints — `src/PhanMemKeToan.Api/Controllers/AuthController.cs`
- [ ] T2-037 [MODIFY] `CurrentUserService` — fix `tid` claim, add CompanyId property — `src/PhanMemKeToan.Api/Services/CurrentUserService.cs`
- [ ] T2-038 [MODIFY] `Program.cs` — register MasterDbContext, update auth config — `src/PhanMemKeToan.Api/Program.cs`
- [ ] T2-039 [MODIFY] `MeController` — add companies to /api/me response — `src/PhanMemKeToan.Api/Controllers/MeController.cs`
- [ ] T2-040 [NEW] `TenantsController` — admin tenant management endpoints — `src/PhanMemKeToan.Api/Controllers/TenantsController.cs`

### Phase 2.7 — Database Migrations

- [ ] T2-041 [NEW] Master DB initial migration — create sys_master_users, sys_master_user_tenants tables — `Migrations/Master/`
- [ ] T2-042 [MODIFY] Tenant DB migration — extend sys_tenants (database_mode, db_status, connection_string_encrypted) — `Migrations/Tenant/`
- [ ] T2-043 [MODIFY] Add tenant_id column to sys_refresh_tokens — `Migrations/`
- [ ] T2-044 [DATA] Migrate existing superadmin → MasterUser + MasterUserTenant records

### Phase 2.8 — Frontend Changes (6 files)

- [ ] T2-045 [MODIFY] Update `auth.models.ts` — add LoginStep1Response, CompanyInfo, SelectCompanyRequest types — `src/webapp/src/app/features/auth/models/auth.models.ts`
- [ ] T2-046 [MODIFY] Update `auth.service.ts` — add selectCompany(), switchCompany() API calls — `src/webapp/src/app/features/auth/services/auth.service.ts`
- [ ] T2-047 [MODIFY] Update `auth.store.ts` — add tempToken, companies, selectedCompany signals — `src/webapp/src/app/features/auth/store/auth.store.ts`
- [ ] T2-048 [MODIFY] Update `app.routes.ts` — add /select-company route — `src/webapp/src/app/app.routes.ts`
- [ ] T2-049 [MODIFY] Update `shell.component.ts` — add company switcher dropdown in header — `src/webapp/src/app/layout/shell.component.ts`
- [ ] T2-050 [NEW] Create `company-select.component.ts` — company selection page after login step 1 — `src/webapp/src/app/features/auth/pages/company-select/company-select.component.ts`

### Phase 2.9 — Configuration & Docker (2 files)

- [ ] T2-051 [MODIFY] Update `appsettings.json` — add MasterConnection, CloudDatabaseHost, DataProtection section — `src/PhanMemKeToan.Api/appsettings.json`
- [ ] T2-052 [MODIFY] Update `docker-compose.yml` — add master DB volume/config if needed — `docker-compose.yml`

### Phase 2.10 — E2E Verification

- [ ] T2-053 [TEST] Build + run all migrations — verify no errors
- [ ] T2-054 [TEST] Superadmin login — 1 company → auto-select → redirect to dashboard
- [ ] T2-055 [TEST] Create new tenant + assign user → login with 2 companies → company select page → switch company
- [ ] T2-056 [TEST] Token refresh flow across company switch
- [ ] T2-057 [TEST] Password change → verify revocation across all tenants

### Phase 2 Dependency Graph

```
Phase 2.0 → Phase 2.1 → Phase 2.2 → Phase 2.3 + 2.4 (parallel after 2.2)
Phase 2.3 + 2.4 → Phase 2.5 → Phase 2.6 → Phase 2.7
Phase 2.7 → Phase 2.8 + Phase 2.9 (parallel)
Phase 2.8 + 2.9 → Phase 2.10
```

**PHASE_2_TASKS: 57 (T2-001 to T2-057) | SUB-PHASES: 11 | ESTIMATED_DAYS: 5–8**
