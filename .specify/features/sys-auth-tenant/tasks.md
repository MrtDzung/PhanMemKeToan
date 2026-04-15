# Tasks: Auth & Tenant Core (SYS Module)

**Feature**: `sys-auth-tenant`
**Generated**: 2026-04-15
**Total Tasks**: 109 | **Phases**: 10 | **Backend**: 66 | **Frontend**: 24 | **Tests**: 17 | **Config/Migration**: 12

**Input**: `.specify/features/sys-auth-tenant/`
**Prerequisites**: plan.md ✅ spec.md ✅ data-model.md ✅ contracts/ ✅

---

## Phase 1: Domain Layer (Day 1)

- [ ] T001 [BACKEND] Fix AuditableEntity: add `IsDeleted` (bool, default false) — `src/PhanMemKeToan.Domain/Common/AuditableEntity.cs`, `src/PhanMemKeToan.Domain/Common/IAuditableEntity.cs`
- [ ] T002 [P] [BACKEND] Create Tenant entity (inherits BaseEntity; Code, Name, DatabaseSchemaName?, IsActive, CreatedAt; does NOT inherit AuditableEntity) — `src/PhanMemKeToan.Domain/Entities/Tenant.cs`
- [ ] T003 [P] [BACKEND] Create User entity (inherits AuditableEntity; Email, PasswordHash, FullName, IsActive, LastLoginAt?, FailedLoginCount, LockedUntil?; nav: UserRoles, RefreshTokens) — requires T001 — `src/PhanMemKeToan.Domain/Entities/User.cs`
- [ ] T004 [P] [BACKEND] Create Role entity (inherits AuditableEntity; Name, Description?; nav: UserRoles, RolePermissions) — requires T001 — `src/PhanMemKeToan.Domain/Entities/Role.cs`
- [ ] T005 [P] [BACKEND] Create Permission entity (inherits BaseEntity; Code, Name, ModuleCode varchar(10); nav: RolePermissions; NOT tenant-scoped) — `src/PhanMemKeToan.Domain/Entities/Permission.cs`
- [ ] T006 [BACKEND] Create UserRole join entity (no inheritance; UserId FK, RoleId FK; nav: User, Role) — requires T003, T004 — `src/PhanMemKeToan.Domain/Entities/UserRole.cs`
- [ ] T007 [BACKEND] Create RolePermission join entity (no inheritance; RoleId FK, PermissionId FK; nav: Role, Permission) — requires T004, T005 — `src/PhanMemKeToan.Domain/Entities/RolePermission.cs`
- [ ] T008 [BACKEND] Create RefreshToken entity (no inheritance; UserId FK, TokenHash varchar(64), TokenFamily Guid, ExpiresAt, IssuedAt, IsRevoked bool; nav: User) — requires T003 — `src/PhanMemKeToan.Domain/Entities/RefreshToken.cs`
- [ ] T009 [P] [BACKEND] Create `InvalidCredentialsException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/InvalidCredentialsException.cs`
- [ ] T010 [P] [BACKEND] Create `AccountDeactivatedException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/AccountDeactivatedException.cs`
- [ ] T011 [P] [BACKEND] Create `AccountLockedException` (HTTP 429; includes `LockedUntil` DateTimeOffset) — `src/PhanMemKeToan.Domain/Common/Exceptions/AccountLockedException.cs`
- [ ] T012 [P] [BACKEND] Create `TenantNotFoundException` (HTTP 403) — `src/PhanMemKeToan.Domain/Common/Exceptions/TenantNotFoundException.cs`
- [ ] T013 [P] [BACKEND] Create `TenantDeactivatedException` (HTTP 403) — `src/PhanMemKeToan.Domain/Common/Exceptions/TenantDeactivatedException.cs`
- [ ] T014 [P] [BACKEND] Create `TokenExpiredException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/TokenExpiredException.cs`
- [ ] T015 [P] [BACKEND] Create `TokenRevokedException` (HTTP 401) — `src/PhanMemKeToan.Domain/Common/Exceptions/TokenRevokedException.cs`
- [ ] T016 [P] [BACKEND] Create `DuplicateEmailException` (HTTP 409; includes `email` string property) — `src/PhanMemKeToan.Domain/Common/Exceptions/DuplicateEmailException.cs`
- [ ] T017 [P] [BACKEND] Create `RoleInUseException` (HTTP 409; includes `IReadOnlyList<string> AffectedUserNames`) — `src/PhanMemKeToan.Domain/Common/Exceptions/RoleInUseException.cs`
- [ ] T018 [P] [BACKEND] Create `IJwtService` + `UserClaimsDto` record (UserId, TenantId, Email, Roles, Permissions, Jti); methods: GenerateAccessToken, GenerateRefreshToken→(plaintext, hash), ValidateToken, GetJwks — `src/PhanMemKeToan.Application/Common/Interfaces/IJwtService.cs`
- [ ] T019 [P] [BACKEND] Create `IPasswordHasher`: HashPassword(string)→string, VerifyPassword(string,string)→bool — `src/PhanMemKeToan.Application/Common/Interfaces/IPasswordHasher.cs`
- [ ] T020 [P] [BACKEND] Create `ITokenBlacklistService`: BlacklistAsync(jti, ttl), IsBlacklistedAsync(jti) — `src/PhanMemKeToan.Application/Common/Interfaces/ITokenBlacklistService.cs`
- [ ] T021 [P] [BACKEND] Create `ITenantContext`: scoped TenantId (Guid? get/set) — `src/PhanMemKeToan.Application/Common/Interfaces/ITenantContext.cs`
- [ ] T022 [P] [BACKEND] Create `ITenantRepository` + `TenantDto`: GetByCodeAsync, GetByIdAsync — `src/PhanMemKeToan.Application/Common/Interfaces/ITenantRepository.cs`
- [ ] T023 [P] [BACKEND] Extend `IApplicationDbContext` with DbSet<> for all 7 new entities — `src/PhanMemKeToan.Application/Common/Interfaces/IApplicationDbContext.cs`

---

## Phase 2: Infrastructure — Persistence (Day 1–2)

- [ ] T024 [CONFIG] Add NuGet packages to Infrastructure.csproj: BCrypt.Net-Next 4.0.3, System.IdentityModel.Tokens.Jwt 8.x, Microsoft.IdentityModel.Tokens 8.x, EFCore.NamingConventions 10.x, StackExchange.Redis 2.8.x — `src/PhanMemKeToan.Infrastructure/PhanMemKeToan.Infrastructure.csproj`
- [ ] T025 [BACKEND] Update AppDbContext: inject ITenantContext; add 7 DbSet<> properties; call UseSnakeCaseNamingConvention(); apply HasQueryFilter on User and Role ONLY (NOT on Tenant/Permission/RefreshToken/UserRole/RolePermission) — `src/PhanMemKeToan.Infrastructure/Persistence/ApplicationDbContext.cs`
- [ ] T026 [P] [BACKEND] TenantConfiguration: ToTable("sys_tenants"), unique index Code — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- [ ] T027 [P] [BACKEND] UserConfiguration: ToTable("sys_users"), unique (TenantId, Email), index TenantId, varchar lengths — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- [ ] T028 [P] [BACKEND] RoleConfiguration: ToTable("sys_roles"), unique (TenantId, Name), index TenantId — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- [ ] T029 [P] [BACKEND] PermissionConfiguration: ToTable("sys_permissions"), unique Code, index ModuleCode — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs`
- [ ] T030 [P] [BACKEND] UserRoleConfiguration: ToTable("sys_user_roles"), composite PK (UserId, RoleId), cascade deletes — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs`
- [ ] T031 [P] [BACKEND] RolePermissionConfiguration: ToTable("sys_role_permissions"), composite PK (RoleId, PermissionId), cascade deletes — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs`
- [ ] T032 [P] [BACKEND] RefreshTokenConfiguration: ToTable("sys_refresh_tokens"), unique TokenHash, composite index (TokenFamily, IsRevoked), cascade delete on UserId — `src/PhanMemKeToan.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- [ ] T033 [MIGRATION] Run: `dotnet ef migrations add InitialAuth --project src/PhanMemKeToan.Infrastructure --startup-project src/PhanMemKeToan.Api` — requires T025–T032
- [ ] T034 [MIGRATION] Data migration SeedPermissions: ~75 permission records for 15 modules (DI,GL,CA,BA,PU,SA,IN,FA,SU,JC,PA,TA,CT,IP,SYS) pattern MODULE.Resource.Action — requires T033
- [ ] T035 [MIGRATION] Data migration SeedSuperAdmin: Tenant(code:SYSTEM), User(superadmin@system.local, BCrypt hash), Role(SuperAdmin), assign all permissions, assign role to user — requires T034

---

## Phase 3: Infrastructure — Services (Day 2–3)

- [ ] T036 [BACKEND] Create JwtService (IJwtService): RS256 from appsettings PrivateKeyPemBase64; GenerateAccessToken with claims (sub,tid,email,roles,permissions,jti,iat,exp); GenerateRefreshToken→(plaintext, sha256Hash); ValidateToken; GetJwks() returns public key ONLY — `src/PhanMemKeToan.Infrastructure/Services/JwtService.cs`
- [ ] T037 [P] [BACKEND] Create BcryptPasswordHasher (IPasswordHasher): BCrypt.Net-Next cost=12 — `src/PhanMemKeToan.Infrastructure/Services/BcryptPasswordHasher.cs`
- [ ] T038 [P] [BACKEND] Create RedisTokenBlacklistService (ITokenBlacklistService): key=blacklist:jti:{jti}, TTL=remaining validity; Redis unavailable→throw ServiceUnavailableException (fail-closed) — `src/PhanMemKeToan.Infrastructure/Services/RedisTokenBlacklistService.cs`
- [ ] T039 [P] [BACKEND] Create TenantContext (ITenantContext): scoped, mutable TenantId getter/setter — `src/PhanMemKeToan.Infrastructure/Services/TenantContext.cs`
- [ ] T040 [P] [BACKEND] Create TenantRepository (ITenantRepository): query Tenants with IgnoreQueryFilters(); cache in Redis 5-min TTL — `src/PhanMemKeToan.Infrastructure/Services/TenantRepository.cs`
- [ ] T041 [BACKEND] Create TenantMiddleware: (1) JWT tid claim→GetByIdAsync; (2) else X-Tenant-Code header→GetByCodeAsync; (3) not found or IsActive=false→403 ProblemDetails short-circuit; (4) set ITenantContext.TenantId — requires T039, T040 — `src/PhanMemKeToan.Infrastructure/Middleware/TenantMiddleware.cs`
- [ ] T042 [BACKEND] Register all Phase 3 services in DependencyInjection.cs: IConnectionMultiplexer(singleton), IJwtService(singleton), IPasswordHasher(singleton), ITokenBlacklistService(scoped), ITenantContext(scoped), ITenantRepository(scoped) — `src/PhanMemKeToan.Infrastructure/DependencyInjection.cs`

---

## Phase 4: Application Layer — Auth CQRS (Day 3)

- [ ] T043 [BACKEND] Verify Application/DependencyInjection.cs: ValidationBehavior + LoggingBehavior registered; AddValidatorsFromAssembly scans new validators — `src/PhanMemKeToan.Application/DependencyInjection.cs`
- [ ] T044 [BACKEND] [US1] Create LoginCommand CQRS: command(Email, Password, RememberMe, IpAddress); handler (1:check Redis ratelimit:login:{ip} counter; 2:load user; 3:check IsActive→AccountDeactivatedException; 4:VerifyPassword; 5:on failure increment counter, AccountLockedException at 5; 6:update LastLoginAt, reset FailedLoginCount; 7:generate tokens; 8:persist RefreshToken hash; 9:structured Serilog log with eventType/userId/tenantId/ipAddress/success fields [FR-033]; 10:return LoginResult); validator(email format, password non-empty max 128) — `src/PhanMemKeToan.Application/Features/Auth/Commands/Login/`
- [ ] T045 [BACKEND] [US2] Create RefreshTokenCommand CQRS: handler (1:find by hash; 2:check ExpiresAt+IsRevoked; 3:replay:revoked hash→revoke entire TokenFamily→TokenRevokedException; 4:mark old IsRevoked=true; 5:generate+persist new pair; 6:structured Serilog log with eventType/userId/tenantId/success/isReplay fields [FR-033]; 7:return LoginResult) — `src/PhanMemKeToan.Application/Features/Auth/Commands/RefreshToken/`
- [ ] T046 [BACKEND] [US1] Create LogoutCommand CQRS: handler (1:BlacklistAsync(jti, remainingTtl); 2:mark RefreshToken.IsRevoked=true; 3:save; 4:structured Serilog log with eventType/userId/tenantId/jti fields [FR-033]) — `src/PhanMemKeToan.Application/Features/Auth/Commands/Logout/`
- [ ] T047 [BACKEND] [US6] Create GetCurrentUserQuery CQRS: handler loads User with UserRoles→Role→RolePermissions→Permission; returns CurrentUserDto(Id, Email, FullName, IsActive, TenantId, TenantName, Roles[], Permissions[], LastLoginAt, CreatedAt) — `src/PhanMemKeToan.Application/Features/Auth/Queries/GetCurrentUser/`
- [ ] T048 [P] [BACKEND] Extend GlobalExceptionHandler: map all 9 new domain exceptions to RFC 7807 ProblemDetails with errorCode strings — `src/PhanMemKeToan.Api/Infrastructure/GlobalExceptionHandler.cs`

---

## Phase 5: Application Layer — User/Role/Permission CQRS (Day 3–4)

- [ ] T049 [P] [US4] [BACKEND] CreateUserCommand CQRS: handler(email uniqueness check; hash password; create User+UserRole rows; DuplicateEmailException on conflict); validator(email format, OWASP password complexity) — `src/PhanMemKeToan.Application/Features/Users/Commands/CreateUser/`
- [ ] T050 [P] [US4] [BACKEND] UpdateUserCommand CQRS: handler(re-validate email if changed; replace UserRole rows atomically) — `src/PhanMemKeToan.Application/Features/Users/Commands/UpdateUser/`
- [ ] T051 [P] [US4] [BACKEND] ToggleUserActivationCommand CQRS: handler(set IsActive; on deactivate→revoke all RefreshTokens for user) — `src/PhanMemKeToan.Application/Features/Users/Commands/ToggleUserActivation/`
- [ ] T052 [P] [US4] [BACKEND] UnlockUserCommand CQRS: handler(reset FailedLoginCount=0, LockedUntil=null) — `src/PhanMemKeToan.Application/Features/Users/Commands/UnlockUser/`
- [ ] T053 [P] [US4] [BACKEND] GetUsersQuery CQRS: paginated+filtered; include Roles; return PaginatedResult<UserListDto>; create PaginatedResult<T> model — `src/PhanMemKeToan.Application/Features/Users/Queries/GetUsers/`, `src/PhanMemKeToan.Application/Common/Models/PaginatedResult.cs`
- [ ] T054 [P] [US4] [BACKEND] GetUserByIdQuery CQRS: load User with UserRoles→Role; throw NotFoundException if missing — `src/PhanMemKeToan.Application/Features/Users/Queries/GetUserById/`
- [ ] T055 [P] [US6] [BACKEND] UpdateProfileCommand CQRS: read UserId from ICurrentUserService; update FullName — `src/PhanMemKeToan.Application/Features/Users/Commands/UpdateProfile/`
- [ ] T056 [P] [US6] [BACKEND] ChangePasswordCommand CQRS: VerifyPassword(current)→InvalidCredentialsException on fail; HashPassword(new)→update; validator(OWASP complexity) — `src/PhanMemKeToan.Application/Features/Users/Commands/ChangePassword/`
- [ ] T057 [P] [US5] [BACKEND] CreateRoleCommand CQRS: unique name within tenant; create Role+RolePermission rows — `src/PhanMemKeToan.Application/Features/Roles/Commands/CreateRole/`
- [ ] T058 [P] [US5] [BACKEND] UpdateRoleCommand CQRS: update name/description; re-validate uniqueness if name changes — `src/PhanMemKeToan.Application/Features/Roles/Commands/UpdateRole/`
- [ ] T059 [P] [US5] [BACKEND] DeleteRoleCommand CQRS: query active UserRole rows→RoleInUseException if any; else set Role.IsDeleted=true (soft-delete) + hard-delete all RolePermission join rows for that role — `src/PhanMemKeToan.Application/Features/Roles/Commands/DeleteRole/`
- [ ] T060 [P] [US5] [BACKEND] GetRolesQuery CQRS: list all roles for tenant with UserCount and PermissionCount — `src/PhanMemKeToan.Application/Features/Roles/Queries/GetRoles/`
- [ ] T061 [P] [US5] [BACKEND] GetRoleByIdQuery CQRS: load Role with RolePermissions→Permission — `src/PhanMemKeToan.Application/Features/Roles/Queries/GetRoleById/`
- [ ] T062 [P] [US5] [BACKEND] AssignPermissionsToRoleCommand CQRS: bulk replace RolePermission rows; validate PermissionIds exist — `src/PhanMemKeToan.Application/Features/Roles/Commands/AssignPermissions/`
- [ ] T063 [P] [US5] [BACKEND] GetPermissionsQuery CQRS: load all permissions grouped by ModuleCode — `src/PhanMemKeToan.Application/Features/Permissions/Queries/GetPermissions/`
- [ ] T064 [P] [US5] [BACKEND] GetPermissionMatrixQuery CQRS: build cross-tab {moduleCode, permissions: [{permissionId, code, name, roleAssignments: {roleId: bool}}]} — `src/PhanMemKeToan.Application/Features/Permissions/Queries/GetPermissionMatrix/`

---

## Phase 6: API Layer (Day 4–5)

- [ ] T065 [CONFIG] Add NuGet to Api.csproj: Microsoft.AspNetCore.Authentication.JwtBearer 10.x, AspNetCore.HealthChecks.Redis 10.x, AspNetCore.HealthChecks.NpgSql 10.x — `src/PhanMemKeToan.Api/PhanMemKeToan.Api.csproj`
- [ ] T066 [BACKEND] Register JWT Bearer in Program.cs: AddAuthentication().AddJwtBearer() with RSA public key from IJwtService.GetJwks(); validate iss/aud/exp/RS256; OnTokenValidated→IsBlacklistedAsync(jti)→fail if blacklisted — `src/PhanMemKeToan.Api/Program.cs`
- [ ] T067 [BACKEND] Register middleware in Program.cs: AddRateLimiter (fixed-window on /api/auth/login); AddCors (allowlist from appsettings, no wildcard in prod); UseMiddleware<TenantMiddleware> after UseAuthentication before UseAuthorization; AddHealthChecks().AddRedis()+AddNpgSql(); MapHealthChecks("/health") — `src/PhanMemKeToan.Api/Program.cs`
- [ ] T068 [P] [BACKEND] Create AuthController [AllowAnonymous]: POST /api/auth/login (set HttpOnly+Secure+SameSite=Strict refresh cookie, Max-Age if rememberMe); POST /api/auth/refresh (read cookie→SHA256 hash→RefreshTokenCommand; rotate cookie); POST /api/auth/logout [Authorize] (extract JTI+expiry from claims, cookie hash→LogoutCommand; clear cookie) — `src/PhanMemKeToan.Api/Controllers/AuthController.cs`
- [ ] T069 [P] [BACKEND] Create JwksController [AllowAnonymous]: GET /.well-known/jwks.json → IJwtService.GetJwks(); Cache-Control: public, max-age=3600 — `src/PhanMemKeToan.Api/Controllers/JwksController.cs`
- [ ] T070 [P] [BACKEND] Create MeController [Authorize]: GET /api/me; PUT /api/me/profile; PUT /api/me/password — `src/PhanMemKeToan.Api/Controllers/MeController.cs`
- [ ] T071 [P] [BACKEND] Create UsersController [Authorize]: GET /api/users (SYS.Users.View); GET /api/users/{id}; POST /api/users (SYS.Users.Manage, 201); PUT /api/users/{id}; POST /api/users/{id}/deactivate (SYS.Users.Manage, cannot deactivate self); POST /api/users/{id}/reactivate (SYS.Users.Manage); POST /api/users/{id}/unlock (SYS.Users.Manage) — `src/PhanMemKeToan.Api/Controllers/UsersController.cs`
- [ ] T072 [P] [BACKEND] Create RolesController [Authorize]: GET /api/roles (SYS.Roles.View); GET /api/roles/{id}; POST /api/roles (SYS.Roles.Manage, 201); PUT /api/roles/{id}; DELETE /api/roles/{id} — `src/PhanMemKeToan.Api/Controllers/RolesController.cs`
- [ ] T073 [P] [BACKEND] Create PermissionsController [Authorize, SYS.Roles.Manage]: GET /api/permissions; GET /api/permissions/matrix; PUT /api/permissions/matrix (roleId in request body, not path) — `src/PhanMemKeToan.Api/Controllers/PermissionsController.cs`
- [ ] T074 [P] [CONFIG] Add config sections to appsettings: JwtSettings (Issuer, Audience, AccessTokenTtlMinutes:15, RefreshTokenTtlDays:7, PrivateKeyPemBase64 via user-secrets, PublicKeyPemBase64); CorsSettings.AllowedOrigins — `src/PhanMemKeToan.Api/appsettings.json`, `src/PhanMemKeToan.Api/appsettings.Development.json`

---

## Phase 7: Backend Tests (Day 5)

- [ ] T075 [TEST] [US1] LoginCommandHandlerTests (5 scenarios): valid credentials→LoginResult; wrong password→InvalidCredentialsException; deactivated user→AccountDeactivatedException BEFORE VerifyPassword; 6th attempt→AccountLockedException; unknown email→same generic error as wrong password — `tests/PhanMemKeToan.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs`
- [ ] T076 [P] [TEST] [US2] RefreshTokenCommandHandlerTests (4 scenarios): valid→new pair+old revoked; expired→TokenExpiredException; revoked→TokenRevokedException; replay→all TokenFamily tokens IsRevoked=true in DB — `tests/PhanMemKeToan.Application.Tests/Features/Auth/RefreshTokenCommandHandlerTests.cs`
- [ ] T077 [P] [TEST] JwtServiceTests (5 scenarios): all claims present; RS256 signature validates with public key; expired rejected; tampered rejected; GetJwks() does NOT expose private exponent d — `tests/PhanMemKeToan.Infrastructure.Tests/Services/JwtServiceTests.cs`
- [ ] T078 [P] [TEST] BcryptPasswordHasherTests (3 scenarios): hash+verify roundtrip; wrong password→false; same input→different hashes (salted) — `tests/PhanMemKeToan.Infrastructure.Tests/Services/BcryptPasswordHasherTests.cs`
- [ ] T079 [P] [TEST] [US4] CreateUserCommandHandlerTests (3 scenarios): duplicate email→DuplicateEmailException; weak password→ValidationException; cross-tenant role→NotFoundException — `tests/PhanMemKeToan.Application.Tests/Features/Users/CreateUserCommandHandlerTests.cs`
- [ ] T080 [P] [TEST] [US5] DeleteRoleCommandHandlerTests (2 scenarios): role has active users→RoleInUseException with AffectedUserNames; no users→Role.IsDeleted=true (soft-deleted) AND RolePermission rows hard-deleted from DB — `tests/PhanMemKeToan.Application.Tests/Features/Roles/DeleteRoleCommandHandlerTests.cs`
- [ ] T081 [P] [TEST] TenantMiddlewareTests (4 scenarios): JWT tid→TenantContext set; X-Tenant-Code header fallback; deactivated tenant→403; unknown code→403 — `tests/PhanMemKeToan.Infrastructure.Tests/Middleware/TenantMiddlewareTests.cs`
- [ ] T082 [TEST] [US3] TenantIsolationIntegrationTests (TestContainers PostgreSQL): 2 tenants; user-A record invisible to user-B on read+write+existence-probe — `tests/PhanMemKeToan.Api.Tests/Integration/TenantIsolationIntegrationTests.cs`
- [ ] T083 [P] [TEST] [US5] PermissionEnforcementIntegrationTests: missing SYS.Users.Manage→403; with SYS.Users.Manage→200 — `tests/PhanMemKeToan.Api.Tests/Integration/PermissionEnforcementIntegrationTests.cs`
- [ ] T084 [P] [TEST] [US1] RateLimitingIntegrationTests: 5 failed→6th returns 429; TTL expiry→counter reset — `tests/PhanMemKeToan.Api.Tests/Integration/RateLimitingIntegrationTests.cs`
- [ ] T085 [P] [TEST] [US2] TokenReplayIntegrationTests: login; refresh; replay old hash→401; assert ALL TokenFamily tokens IsRevoked=true in DB — `tests/PhanMemKeToan.Api.Tests/Integration/TokenReplayIntegrationTests.cs`

---

## Phase 8: Frontend Core (Day 5–6)

- [ ] T086 [CONFIG] Verify package.json: @ngrx/signals 20.x, @ngx-translate/core 15.x, primeng 20.x, primeicons 7.x, tailwindcss 4.x; add missing; npm install — `webapp/package.json`
- [ ] T087 [FRONTEND] Configure app.config.ts: provideHttpClient(withInterceptors([authInterceptor])), provideTranslateModule with HttpLoaderFactory, PrimeNG theme, provideRouter(appRoutes) — `webapp/src/app/app.config.ts`
- [ ] T088 [FRONTEND] Create AuthStore (signalStore): signals token(string|null), user(CurrentUserDto|null); computed isAuthenticated, permissions; methods setAuth(token,user), clearAuth(), updateUser(user); in-memory ONLY — `webapp/src/app/core/auth/auth.store.ts`
- [ ] T089 [FRONTEND] Create AuthService: login()→POST /api/auth/login→store.setAuth(); logout()→POST /api/auth/logout→store.clearAuth(); refresh()→POST /api/auth/refresh→store.setAuth()→return token; getMe()→GET /api/me→store.updateUser() — `webapp/src/app/core/auth/auth.service.ts`
- [ ] T090 [FRONTEND] Create authInterceptor (HttpInterceptorFn): inject Bearer token; on 401 (not /auth/refresh)→BehaviorSubject<boolean> mutex: first caller invokes refresh(), sets isRefreshing=true; others wait; on token update retry all; on refresh fail→clearAuth()+navigate('/login') — `webapp/src/app/core/auth/auth.interceptor.ts`
- [ ] T091 [P] [FRONTEND] Create authGuard (CanActivateFn): isAuthenticated→true; else navigate('/login', {queryParams:{returnUrl}})→false — `webapp/src/app/core/auth/auth.guard.ts`
- [ ] T092 [P] [FRONTEND] Create tenantGuard (CanActivateFn): user().tenantId present→true; else navigate('/login')→false — `webapp/src/app/core/auth/tenant.guard.ts`
- [ ] T093 [FRONTEND] Configure app.routes.ts: /login public; all routes canActivate:[authGuard]; /sys lazy-loads sys.routes with canActivate:[tenantGuard]; /me→MeComponent — `webapp/src/app/app.routes.ts`
- [ ] T094 [P] [FRONTEND] Create vi.json with all SYS.AUTH.*, SYS.USERS.*, SYS.ROLES.*, SYS.PERMISSIONS.*, SYS.ME.*, COMMON.* i18n keys — `webapp/src/assets/i18n/vi.json`

---

## Phase 9: Frontend Pages (Day 6–7)

- [ ] T095 [FRONTEND] [US1] LoginComponent (/login): p-fluid form with p-inputtext(email), p-password, p-checkbox(rememberMe), p-button(loading signal); inline server error display via translate pipe; on success navigate to returnUrl — `webapp/src/app/features/login/login.component.ts`
- [ ] T096 [FRONTEND] [US4] SysUsersComponent (/sys/users): p-table lazy pagination; columns: FullName, Email, Roles(p-tag), Status badge, LastLoginAt; toolbar: debounced search, Create button; row actions: Edit, Activate/Deactivate, Unlock; column resize/reorder→localStorage; loading/empty/error states — `webapp/src/app/features/sys/users/sys-users.component.ts`
- [ ] T097 [P] [FRONTEND] [US4] SysUsersFormComponent (p-dialog modal): p-inputtext(fullName,email), p-password(create only), p-multiSelect(roles); client-side validation; emit (saved) event; dirty guard on close — `webapp/src/app/features/sys/users/sys-users-form.component.ts`
- [ ] T098 [FRONTEND] [US5] SysRolesComponent (/sys/roles): p-table role list; columns: Name, Description, UserCount(p-badge), PermissionCount; Create/Edit/Delete actions; p-confirmDialog; Delete disabled with tooltip when userCount>0 — `webapp/src/app/features/sys/roles/sys-roles.component.ts`
- [ ] T099 [P] [FRONTEND] [US5] SysRolesFormComponent (p-dialog modal): Name, Description; permission assignment grouped by ModuleCode with p-checkbox; dirty guard — `webapp/src/app/features/sys/roles/sys-roles-form.component.ts`
- [ ] T100 [FRONTEND] [US5] SysPermissionsComponent (/sys/permissions): GET /api/permissions/matrix; grid rows=permissions by ModuleCode, cols=roles; cells=p-checkbox; track changes; single Save→PUT /api/permissions/matrix (body: {roleId, permissionIds[]}) per modified role; p-toast success/error — `webapp/src/app/features/sys/permissions/sys-permissions.component.ts`
- [ ] T101 [FRONTEND] [US6] MeComponent (/me): two p-card sections: Profile(read-only email, editable FullName, Save→PUT /api/me/profile) and Security(p-password inputs currentPassword/newPassword/confirmPassword, Save→PUT /api/me/password); p-toast on success — `webapp/src/app/features/sys/me/me.component.ts`
- [ ] T102 [FRONTEND] Create sys.routes.ts: /sys/users→SysUsersComponent, /sys/roles→SysRolesComponent, /sys/permissions→SysPermissionsComponent; canActivate:[tenantGuard] on all — `webapp/src/app/features/sys/sys.routes.ts`

---

## Phase 10: E2E Tests (Day 7)

- [ ] T103 [TEST] [US1] login.spec.ts: valid credentials→redirect to /→tenant name visible; wrong password→inline error; deactivated→ACCOUNT_DEACTIVATED error; use client-side nav (history.pushState) NOT page.goto() — `tests/e2e/login.spec.ts`
- [ ] T104 [TEST] [US2] token-refresh.spec.ts: configure AccessTokenTtlMinutes to 10s; login; page.route intercept; wait 12s; GET /api/me; assert 200; assert exactly 1 POST /api/auth/refresh; no 401 surfaced to user — `tests/e2e/token-refresh.spec.ts`
- [ ] T105 [P] [TEST] [US4] user-management.spec.ts: create user; verify in table; deactivate; assert badge; login as deactivated→assert ACCOUNT_DEACTIVATED — `tests/e2e/user-management.spec.ts`
- [ ] T106 [P] [TEST] [US5] permission-matrix.spec.ts: create role via API; open /sys/permissions; toggle 3 checkboxes; save; navigate away (client-side); navigate back; assert 3 checkboxes still checked — `tests/e2e/permission-matrix.spec.ts`

- [ ] T107 [BACKEND] Create ExpiredTokenCleanupService (BackgroundService): runs every 6 hours; hard-deletes RefreshToken rows where ExpiresAt < UtcNow - 24h; register as hosted service in DependencyInjection.cs — `src/PhanMemKeToan.Infrastructure/Services/ExpiredTokenCleanupService.cs`
- [ ] T108 [P] [TEST] [US4] ToggleActivationCommandHandlerTests (3 scenarios): deactivate active user→IsActive=false+all RefreshTokens revoked; reactivate inactive user→IsActive=true; attempt deactivate already-inactive→ALREADY_DEACTIVATED error — `tests/PhanMemKeToan.Application.Tests/Features/Users/ToggleActivationCommandHandlerTests.cs`
- [ ] T109 [P] [TEST] [US4] UnlockUserCommandHandlerTests (2 scenarios): unlock locked user→FailedLoginCount=0+LockedUntil=null; unlock already-unlocked user→no-op/success — `tests/PhanMemKeToan.Application.Tests/Features/Users/UnlockUserCommandHandlerTests.cs`

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
