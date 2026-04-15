# Feature Specification: Auth & Tenant Core (SYS Module)

**Feature Branch**: `sys-auth-tenant`  
**Created**: 2026-04-15  
**Status**: Draft  
**Module**: SYS — System Administration  
**Priority**: CRITICAL — Foundation for all other modules

---

## User Scenarios & Testing

### User Story 1 — Tenant Administrator Login (Priority: P1)

A tenant administrator opens the accounting app, enters their email and password, and gains access to the system. The system identifies which tenant they belong to and loads only that tenant's data. On logout, the session is fully invalidated server-side.

**Why this priority**: This is the entry gate to the entire application. No other feature can be used without it. All other modules depend on a valid, tenant-scoped session.

**Independent Test**: Can be fully tested by navigating to `/login`, submitting valid credentials, verifying the dashboard loads with correct tenant data, then logging out and verifying that the old access token is rejected with 401.

**Acceptance Scenarios**:

1. **Given** a valid user account exists for tenant "ACME", **When** the user submits correct email + password, **Then** the system returns an access token, a refresh token, and the user is redirected to the dashboard showing only ACME's data.
2. **Given** a user submits incorrect password 5 times within 5 minutes, **When** the 6th attempt is made from the same IP, **Then** the system rejects the request without processing credentials and returns a rate-limit error.
3. **Given** a logged-in user clicks Logout, **When** the logout request is processed, **Then** the access token JTI is blacklisted, the refresh token is revoked, and any subsequent request with the old access token is rejected with 401.
4. **Given** a user submits an email that does not exist, **When** the login endpoint is called, **Then** the response is a generic "invalid credentials" message — not revealing whether the email exists in the system.
5. **Given** a deactivated user account, **When** valid credentials are submitted, **Then** login is rejected with an "account deactivated" message that does not reveal password correctness.

---

### User Story 2 — Silent Token Refresh (Priority: P1)

A user actively working in the application has their access token expire. The app transparently requests a new access token using the stored refresh token, without interrupting the user's workflow or losing any in-progress data.

**Why this priority**: Without transparent refresh, users would be logged out mid-task, destroying unsaved voucher data. This is a prerequisite for a usable session model across all modules.

**Independent Test**: Can be tested by shortening access token TTL to 10 seconds in test configuration, performing a login, waiting 11 seconds, then making an API call — the call should succeed with a new access token issued silently.

**Acceptance Scenarios**:

1. **Given** an expired access token and a valid refresh token, **When** the Angular HTTP interceptor detects a 401 response, **Then** it automatically calls the refresh endpoint, obtains a new access token, and retries the original request transparently.
2. **Given** a valid refresh token that has not been used before, **When** the refresh endpoint is called, **Then** a new access token and a new refresh token are issued (token rotation), and the previous refresh token is invalidated.
3. **Given** an expired or revoked refresh token, **When** the refresh endpoint is called, **Then** the system returns 401, the Angular auth store is cleared, and the user is redirected to `/login`.
4. **Given** multiple parallel API requests that all receive 401 simultaneously, **When** the HTTP interceptor handles them, **Then** exactly one refresh request is made and all queued requests are retried with the new token.

---

### User Story 3 — Multi-Tenant Data Isolation (Priority: P1)

A user from tenant "ACME" can only see and operate on "ACME"'s data. A superadmin provisioning a new tenant creates a new tenant record and activates it, after which users of that tenant can log in and access a fully isolated data scope.

**Why this priority**: Tenant isolation is a fundamental data integrity and compliance requirement. A breach would expose confidential financial data across customers — a critical security failure.

**Independent Test**: Can be tested by creating two tenants (A and B), each with one user, logging in as user-A and creating a journal entry, then logging in as user-B and verifying the journal entry is not visible or accessible.

**Acceptance Scenarios**:

1. **Given** a JWT token containing TenantId = "acme-uuid", **When** any data-fetching API is called, **Then** only records where TenantId = "acme-uuid" are returned; records from other tenants are completely absent from all responses.
2. **Given** a request arrives without a JWT but with header `X-Tenant-Code: acme`, **When** the request reaches the TenantMiddleware, **Then** tenant is resolved from the header and TenantId is attached to the request context.
3. **Given** a request with both a JWT TenantId claim and an `X-Tenant-Code` header pointing to different tenants, **When** the request is processed, **Then** the JWT claim takes priority and the header value is ignored.
4. **Given** a request with an unrecognized tenant code or deactivated TenantId, **When** any protected endpoint is called, **Then** the system returns HTTP 403 Forbidden.
5. **Given** a new tenant is created by superadmin with IsActive = true, **When** a user assigned to that tenant attempts to log in, **Then** login succeeds and the session is correctly scoped to the new tenant.

---

### User Story 4 — User Management by Tenant Admin (Priority: P2)

A tenant administrator manages users within their tenant: creating new accounts, assigning roles, updating profiles, and deactivating users who have left the organization, all from a dedicated management screen.

**Why this priority**: User and role management is the second-most critical operation after login. Without it, system administrators cannot onboard staff or control access across the system.

**Independent Test**: Can be tested end-to-end on `/sys/users`: create a user with a role, log in as that user to verify access, deactivate the user, verify login is denied.

**Acceptance Scenarios**:

1. **Given** a tenant admin on the user management screen, **When** they create a new user with email + initial password + assigned roles, **Then** the user record is created within the admin's TenantId scope and the new user can log in immediately.
2. **Given** an admin attempts to create a user with an email already registered in the same tenant, **When** the form is submitted, **Then** a validation error is shown without creating a duplicate.
3. **Given** an admin deactivates a user, **When** the deactivated user attempts to log in, **Then** access is denied and existing sessions for that user are invalidated within the current token TTL window.
4. **Given** an admin assigns or removes a role from a user, **When** the change is saved, **Then** the change is reflected in the user's permissions on their next token refresh.
5. **Given** a non-admin user, **When** they attempt to access the user management API, **Then** the response is HTTP 403 Forbidden.

---

### User Story 5 — Role & Permission Management (Priority: P2)

A tenant administrator defines roles (e.g., "Accountant", "Reviewer", "Viewer") and assigns specific permissions to each role. Permissions map to fine-grained actions across the 15 accounting modules.

**Why this priority**: Role-based access control is required before any transactional module is usable. Without it, all users would have unrestricted access or no access at all.

**Independent Test**: Can be tested by creating a role with only GL "view" permission, assigning it to a user, logging in as that user, and verifying GL list loads but create/edit operations are rejected with 403.

**Acceptance Scenarios**:

1. **Given** an admin on the Role management screen, **When** they create a new role with selected permissions, **Then** the role is saved under the current TenantId and becomes available for assignment to users.
2. **Given** an admin opens the Permission Matrix screen, **When** they toggle permission checkboxes for a role, **Then** the changes are persisted and a user with that role reflects updated access on next token refresh.
3. **Given** a user with role "Viewer" (read-only permissions only), **When** they attempt a write operation on any module API, **Then** the system returns HTTP 403 Forbidden.
4. **Given** an admin attempts to delete a role that is currently assigned to one or more active users, **When** the delete request is submitted, **Then** the operation is rejected and a message lists the affected users.
5. **Given** a role is deleted after all user assignments have been removed, **When** the delete is confirmed, **Then** the role and all its RolePermission associations are removed.

---

### User Story 6 — Current User Profile (Priority: P3)

A logged-in user can view their own profile information and update their display name and account password.

**Why this priority**: Important for user experience and self-service password management, but does not block any transactional workflows.

**Independent Test**: Can be fully tested by calling `GET /api/me` with a valid token and verifying only the authenticated user's data is returned, then changing password and verifying the new password is required for subsequent login.

**Acceptance Scenarios**:

1. **Given** a valid access token, **When** `GET /api/me` is called, **Then** the response contains UserId, Email, FullName, TenantId, list of role names, and flat list of permission codes.
2. **Given** a user submits a password change with incorrect current password, **When** the request is processed, **Then** the change is rejected with a validation error.
3. **Given** a user successfully changes their password, **When** they attempt to log in with the old password, **Then** login is rejected; the new password succeeds.
4. **Given** a user updates their FullName, **When** `GET /api/me` is called afterward, **Then** the updated name is returned.

---

### Edge Cases

- What happens when `X-Tenant-Code` header is present but the tenant is deactivated? → 403 Forbidden with clear message.
- What happens if Redis is temporarily unavailable for token blacklist checks? → System fails safely — tokens that cannot be validated against the blacklist are rejected (fail-closed security model).
- What happens when a user's role is changed while they have an active session? → The change propagates at the user's next token refresh (maximum 15-minute lag).
- What happens when attempting to delete a role still assigned to active users? → Operation is rejected with the list of affected users; admin must remove assignments first.
- What happens when a blank or whitespace-only password is submitted? → Server-side input validation rejects it before any processing or hashing.
- What happens if the same email is used across two different tenants? → Allowed. Email uniqueness is enforced per-tenant, not globally.
- What happens when concurrent logout requests arrive for the same token? → Blacklist write is idempotent; both requests succeed without duplication errors.
- What happens if an attacker replays a revoked refresh token? → Server detects the token hash has been revoked and returns 401; token family revocation triggers full session invalidation for that user.

---

## Requirements

### Functional Requirements

#### Authentication

- **FR-001**: The system MUST authenticate users via email and password, returning a short-lived access token (JWT, RS256) and a long-lived refresh token upon successful login.
- **FR-002**: Access tokens MUST embed the following claims: UserId, TenantId, Email, Roles (list of role names), Permissions (flat list of permission codes).
- **FR-003**: Access tokens MUST expire after a configurable TTL (default: 15 minutes). Refresh tokens MUST expire after a configurable TTL (default: 7 days).
- **FR-004**: Refresh tokens MUST be stored as a SHA-256 hash in the database — the plaintext value is never persisted.
- **FR-005**: The system MUST implement token rotation on refresh: each successful refresh call issues a new refresh token and invalidates the previous one. Replay detection via token family (lineage) tracking revokes the entire family on reuse detection.
- **FR-006**: On logout, the system MUST blacklist the access token by its JTI (JWT ID) in a distributed cache with TTL matching the token's remaining validity period.
- **FR-007**: The login endpoint MUST enforce rate limiting: maximum 5 failed attempts per IP address within any 5-minute sliding window.
- **FR-008**: All user passwords MUST be hashed using BCrypt with a minimum cost factor of 12. Plaintext passwords MUST never be stored, logged, or transmitted after initial receipt.
- **FR-009**: Authentication error responses for invalid credentials MUST NOT differentiate between "email not found" and "wrong password" — a single generic "invalid credentials" message prevents user enumeration. However, a deactivated account (FR-010) MAY return a distinct "account deactivated" message because the account's existence is assumed known to its owner; this is an intentional UX decision that does not constitute enumeration of unknown emails.
- **FR-010**: Deactivated user accounts MUST be rejected at login before any password comparison is performed.
- **FR-010b**: User accounts that have exceeded the per-user failed-login threshold (5 failed attempts within 5 minutes from any IP) MUST be temporarily locked (`LockedUntil` timestamp set) and all subsequent login attempts rejected until the lock expires or an admin unlocks the account. This is separate from the IP-level rate limit (FR-007) and protects against distributed password-spray attacks.
- **FR-010c**: Password complexity policy: minimum 8 characters, maximum 128 characters, must include uppercase, lowercase, digit, and special character (OWASP ASVS §2.1.1).
- **FR-010d**: "Remember Me" behavior: when enabled, the refresh token cookie persists for the full 7-day TTL; when disabled, the cookie is session-scoped (deleted when the browser closes).

#### Multi-Tenant Resolution

- **FR-011**: Every protected API request MUST be resolved to a specific Tenant context before any business logic executes.
- **FR-012**: Tenant resolution MUST use the TenantId JWT claim as the primary source when a valid token is present.
- **FR-013**: For requests without a JWT (public endpoints / tenant provisioning flows), the system MUST resolve the tenant from the `X-Tenant-Code` HTTP request header. In localhost/development, a fallback to a configured default tenant code is permitted.
- **FR-014**: Requests that resolve to an unrecognized or deactivated TenantId MUST be rejected with HTTP 403 Forbidden.
- **FR-015**: The EF Core global query filter on `ITenantEntity` MUST automatically scope ALL database queries to the resolved TenantId — no individual query may bypass this filter outside of explicitly designated superadmin contexts.
- **FR-016**: Automated integration tests MUST verify that a user authenticated for Tenant A cannot read, write, or detect the existence of data belonging to Tenant B.

#### User Management

- **FR-017**: Tenant administrators MUST be able to create, read, update, and deactivate (soft-delete) users within their own tenant only.
- **FR-018**: Email addresses MUST be unique within a tenant. The same email may exist across different tenants.
- **FR-019**: User creation MUST support specifying an initial password and one or more role assignments in a single operation.
- **FR-020**: User records MUST NEVER be hard-deleted. Deactivation (IsActive = false) is the only removal mechanism, preserving full audit trail integrity.
- **FR-021**: Any authenticated user MUST be able to retrieve their own profile via `GET /api/me`.
- **FR-022**: Any authenticated user MUST be able to update their own FullName and change their own password (requiring current password verification).
- **FR-023**: User management APIs (list, create, edit, deactivate) MUST require the `SYS.Users.Manage` permission.

#### Role & Permission Management

- **FR-024**: Tenant administrators MUST be able to create, read, update, and delete roles within their own tenant.
- **FR-025**: Each system permission MUST have a unique Code following the pattern `<MODULE>.<Resource>.<Action>` (e.g., `GL.Journal.View`, `SA.Invoice.Create`) and must be associated with one of the 15 module codes (DI, GL, CA, BA, PU, SA, IN, FA, SU, JC, PA, TA, CT, IP, SYS).
- **FR-026**: Permissions are pre-seeded at database migration time. New permissions are added only via migrations, not through the UI.
- **FR-027**: Role-to-Permission and User-to-Role relationships are many-to-many. Assigning a role to a user grants all permissions of that role.
- **FR-028**: The flat list of permission codes for a user (union of all their roles' permissions) MUST be embedded in the access token at login and token refresh time.
- **FR-029**: A role MUST NOT be deletable if any active user is currently assigned to it.
- **FR-030**: The system MUST provide a permission matrix API that returns all roles with their assigned permissions grouped by module code.

#### Security (OWASP Compliance)

- **FR-031**: ALL API endpoints MUST perform server-side input validation. Client-side validation is additive only and MUST NOT be relied upon for security.
- **FR-032**: CORS origins MUST be configured via an allowlist. Requests from origins not on the allowlist MUST be rejected.
- **FR-033**: The system MUST log all authentication events — login (success/failure), logout, token refresh (success/failure) — with timestamp, IP address, user identifier, and tenant identifier.
- **FR-034**: All security-sensitive write operations (user create/deactivate, role change, permission change) MUST be recorded in the audit log (CreatedBy/UpdatedBy via AuditableEntity) with actor identity.
- **FR-035**: HTTP responses MUST NOT include stack traces, internal error messages, or system internals in production.
- **FR-036**: The refresh token MUST be transmitted and stored as an HttpOnly, Secure, SameSite=Strict cookie to prevent JavaScript access (XSS mitigation). The access token MAY be returned in the response body for in-memory storage by the Angular client.

#### Angular Frontend

- **FR-037**: The login page at `/login` MUST include email and password fields, a submit button, and display server-side validation errors inline beneath the relevant fields.
- **FR-038**: The Angular auth state MUST be managed in an NgRx Signals store exposing: `token` (string | null), `user` (profile object | null), `isAuthenticated` (computed boolean), `permissions` (string[] — list of permission codes).
- **FR-039**: An `AuthGuard` (CanActivate) MUST protect all routes except `/login`. Unauthenticated requests trigger a redirect to `/login` with the original URL preserved for post-login redirect.
- **FR-040**: A `TenantGuard` MUST verify the resolved tenant context before activating any accounting module route.
- **FR-041**: An HTTP interceptor MUST: (a) inject the Authorization Bearer token into all outbound API calls, and (b) handle 401 responses by calling the refresh endpoint once, updating the auth store with the new token, and retrying the original request.
- **FR-042**: The interceptor MUST guard against token refresh storms — if multiple requests simultaneously receive 401, only one refresh call is made and all queued requests are retried after the single refresh completes.
- **FR-043**: The user management screen at `/sys/users` MUST display a paginated, searchable list of users with columns: Full Name, Email, Active Roles, Account Status, Last Login At; with actions to create, edit, and toggle activation.
- **FR-044**: The role management screen at `/sys/roles` MUST allow creating, editing, and deleting roles, with an inline or modal interface for assigning/removing permissions to the role.
- **FR-045**: The permission matrix screen at `/sys/permissions` MUST display a grid with module sections as row groups and individual permissions as rows; role columns show checkbox toggles; changes are saved via a single "Save" action.
- **FR-046**: All management screens MUST handle loading, empty, and error states with appropriate user-facing messages using ngx-translate keys.

---

### Key Entities

- **Tenant**: Id (UUID), Code (unique), Name, DatabaseSchemaName, IsActive, CreatedAt
- **User**: Id (UUID), TenantId (FK), Email (unique per tenant), PasswordHash (BCrypt), FullName, IsActive, LastLoginAt, FailedLoginCount, LockedUntil, IsDeleted (soft-delete per Constitution). Inherits AuditableEntity.
- **Role**: Id (UUID), TenantId (FK), Name (unique per tenant), Description, IsDeleted (soft-delete per Constitution). Inherits AuditableEntity.
- **Permission**: Id (UUID), Code (globally unique, pattern: MODULE.Resource.Action), Name, ModuleCode. Not tenant-scoped.
- **UserRole**: UserId (FK), RoleId (FK). Enforces User.TenantId == Role.TenantId.
- **RolePermission**: RoleId (FK), PermissionId (FK).
- **RefreshToken**: Id (UUID), UserId (FK), TokenHash (SHA-256), TokenFamily (UUID — for replay detection), ExpiresAt, IssuedAt, IsRevoked.

---

## Success Criteria

- **SC-001**: A registered user completes the full login flow in under 3 seconds on standard broadband.
- **SC-002**: Token refresh is fully transparent — zero workflow interruption when access token expires during active data entry.
- **SC-003**: Tenant isolation is absolute — automated integration tests across two tenants show 0% cross-tenant data leakage.
- **SC-004**: Rate limiter blocks 100% of login attempts beyond the 5-attempt threshold within the 5-minute window.
- **SC-005**: A tenant administrator can complete user provisioning → role assignment → access verification entirely within the UI.
- **SC-006**: All CRUD operations on users, roles, and permissions complete within 2 seconds under normal load.
- **SC-007**: Authentication audit log captures 100% of login, logout, failed login, and token refresh events.
- **SC-008**: Permission enforcement is 100% accurate — unauthorized permission codes are denied every time.
- **SC-009**: System sustains 50 concurrent active user sessions without measurable response time degradation.
- **SC-010**: OWASP Top 10 categories A01, A02, A07 are protected against and verified through automated security testing.
- **SC-011**: Token family revocation correctly invalidates all refresh tokens in a family when replay is detected, verified by automated test.

---

## UI Labels (Vietnamese — ngx-translate keys)

```json
{
  "SYS.AUTH.LOGIN.TITLE": "Đăng nhập hệ thống",
  "SYS.AUTH.LOGIN.EMAIL_LABEL": "Email",
  "SYS.AUTH.LOGIN.EMAIL_PLACEHOLDER": "Nhập địa chỉ email",
  "SYS.AUTH.LOGIN.PASSWORD_LABEL": "Mật khẩu",
  "SYS.AUTH.LOGIN.PASSWORD_PLACEHOLDER": "Nhập mật khẩu",
  "SYS.AUTH.LOGIN.SUBMIT": "Đăng nhập",
  "SYS.AUTH.LOGIN.REMEMBER_ME": "Ghi nhớ đăng nhập",
  "SYS.AUTH.LOGOUT.BUTTON": "Đăng xuất",
  "SYS.AUTH.LOGOUT.CONFIRM": "Bạn có chắc muốn đăng xuất không?",
  "SYS.AUTH.ERROR.INVALID_CREDENTIALS": "Email hoặc mật khẩu không đúng",
  "SYS.AUTH.ERROR.ACCOUNT_DEACTIVATED": "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.",
  "SYS.AUTH.ERROR.ACCOUNT_LOCKED": "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.",
  "SYS.AUTH.ERROR.RATE_LIMITED": "Quá nhiều lần thử đăng nhập. Vui lòng thử lại sau {minutes} phút.",
  "SYS.AUTH.ERROR.SESSION_EXPIRED": "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.",
  "SYS.AUTH.ERROR.TENANT_NOT_FOUND": "Không tìm thấy công ty. Vui lòng kiểm tra lại địa chỉ truy cập.",
  "SYS.AUTH.ERROR.TENANT_DEACTIVATED": "Công ty đã ngừng hoạt động. Vui lòng liên hệ nhà cung cấp dịch vụ.",
  "SYS.AUTH.PASSWORD.COMPLEXITY_HINT": "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
  "SYS.AUTH.FEATURE_COMING_SOON": "Tính năng này sẽ sớm ra mắt",
  "SYS.USERS.PAGE_TITLE": "Quản lý người dùng",
  "SYS.USERS.BUTTON.CREATE": "Thêm người dùng",
  "SYS.USERS.BUTTON.EDIT": "Chỉnh sửa",
  "SYS.USERS.BUTTON.DEACTIVATE": "Vô hiệu hóa",
  "SYS.USERS.BUTTON.REACTIVATE": "Kích hoạt lại",
  "SYS.USERS.BUTTON.UNLOCK": "Mở khóa tài khoản",
  "SYS.USERS.SEARCH_PLACEHOLDER": "Tìm theo tên hoặc email...",
  "SYS.USERS.COL.FULLNAME": "Họ và tên",
  "SYS.USERS.COL.EMAIL": "Email",
  "SYS.USERS.COL.ROLES": "Vai trò",
  "SYS.USERS.COL.STATUS": "Trạng thái",
  "SYS.USERS.COL.LAST_LOGIN": "Đăng nhập lần cuối",
  "SYS.USERS.STATUS.ACTIVE": "Đang hoạt động",
  "SYS.USERS.STATUS.INACTIVE": "Đã vô hiệu hóa",
  "SYS.USERS.STATUS.LOCKED": "Tạm khóa",
  "SYS.USERS.ERROR.EMAIL_EXISTS": "Email này đã tồn tại trong hệ thống của công ty",
  "SYS.USERS.CONFIRM.DEACTIVATE": "Bạn có chắc muốn vô hiệu hóa tài khoản của {name} không?",
  "SYS.ROLES.PAGE_TITLE": "Quản lý vai trò",
  "SYS.ROLES.BUTTON.CREATE": "Thêm vai trò",
  "SYS.ROLES.BUTTON.EDIT": "Chỉnh sửa",
  "SYS.ROLES.BUTTON.DELETE": "Xóa",
  "SYS.ROLES.COL.NAME": "Tên vai trò",
  "SYS.ROLES.COL.DESCRIPTION": "Mô tả",
  "SYS.ROLES.COL.USER_COUNT": "Số người dùng",
  "SYS.ROLES.ERROR.HAS_USERS": "Không thể xóa vai trò đang được gán cho người dùng. Vui lòng gỡ gán trước.",
  "SYS.ROLES.CONFIRM.DELETE": "Bạn có chắc muốn xóa vai trò \"{name}\" không? Hành động này không thể hoàn tác.",
  "SYS.PERMISSIONS.PAGE_TITLE": "Ma trận phân quyền",
  "SYS.PERMISSIONS.LABEL.MODULE": "Phân hệ",
  "SYS.PERMISSIONS.BUTTON.SAVE": "Lưu phân quyền",
  "SYS.PERMISSIONS.SUCCESS.SAVED": "Phân quyền đã được lưu thành công.",
  "SYS.ME.PAGE_TITLE": "Thông tin tài khoản",
  "SYS.ME.SECTION.PROFILE": "Hồ sơ cá nhân",
  "SYS.ME.SECTION.SECURITY": "Bảo mật",
  "SYS.ME.LABEL.FULLNAME": "Họ và tên",
  "SYS.ME.LABEL.EMAIL": "Email",
  "SYS.ME.LABEL.TENANT": "Công ty",
  "SYS.ME.LABEL.ROLES": "Vai trò hiện tại",
  "SYS.ME.BUTTON.CHANGE_PASSWORD": "Đổi mật khẩu",
  "SYS.ME.LABEL.CURRENT_PASSWORD": "Mật khẩu hiện tại",
  "SYS.ME.LABEL.NEW_PASSWORD": "Mật khẩu mới",
  "SYS.ME.LABEL.CONFIRM_PASSWORD": "Xác nhận mật khẩu mới",
  "SYS.ME.ERROR.WRONG_CURRENT_PASSWORD": "Mật khẩu hiện tại không đúng",
  "SYS.ME.ERROR.PASSWORD_MISMATCH": "Mật khẩu xác nhận không khớp",
  "SYS.ME.SUCCESS.PASSWORD_CHANGED": "Đổi mật khẩu thành công.",
  "SYS.ME.SUCCESS.PROFILE_UPDATED": "Thông tin cá nhân đã được cập nhật."
}
```

---

## Assumptions

- Shared-database, shared-schema multi-tenant architecture. TenantId isolation via EF Core global query filters (ITenantEntity already scaffolded).
- JWT signing algorithm: RS256 (asymmetric). JWKS endpoint exposed at `/.well-known/jwks.json` for future service-to-service validation.
- A platform-level "superadmin" role exists outside any tenant scope. Superadmin provisioning UI is out of scope; a seeded superadmin account via database migration is sufficient.
- Password reset via email (forgot password flow) is out of scope for MVP. Initial account passwords are set by tenant admins. The Forgot Password button renders disabled in MVP UI with "feature coming soon" tooltip.
- OAuth2 / SSO (SAML, Google Workspace, Microsoft Entra ID) is out of scope.
- Two-factor authentication (2FA / MFA) is out of scope.
- The 15 module permission codes are seeded at migration time. New permissions are added only via migrations.
- Access token TTL: 15 minutes default. Refresh token TTL: 7 days default. Configurable via appsettings.
- Angular stores access token in memory only (NgRx Signals store) — not localStorage/sessionStorage. Refresh token in HttpOnly, Secure, SameSite=Strict cookie.
- CORS config and rate limiting thresholds are environment-level settings, not per-tenant configurable in v1.
- AuditableEntity base class applied to all domain entities in this feature.
- All timestamps stored as UTC. Frontend converts to browser local timezone for display.
- SSL/TLS terminated at reverse proxy. Application assumes HTTPS in staging and production.
- In localhost/development, X-Tenant-Code header defaults to a configured default tenant to simplify local development.