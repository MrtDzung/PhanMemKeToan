# API Contracts: Authentication (`/api/auth`)

> **Architecture**: Dual-DB — 2-step login (email/password → company selection → JWT)
> **Version**: 2.0.0 | **Updated**: 2026-04-16 | **Previous**: v1.0 single-step login (see git history)

---

## POST /api/auth/login (Step 1 — Verify Credentials)

**Authentication**: None (public endpoint)
**Rate Limit**: 5 requests per 5-minute sliding window per IP → 429 on violation
**Database**: Master DB only (MasterUser lookup)

### Request

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "admin@acme.vn",
  "password": "P@ssw0rd!",
  "rememberMe": false
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | Yes | Valid email format, max 256 chars |
| password | string | Yes | Non-empty, max 128 chars |
| rememberMe | boolean | No | Defaults to `false`. Stored in tempToken claims for use in Step 2. |

### Success Response — 200 OK

```json
{
  "tempToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tempTokenExpiresIn": 60,
  "companies": [
    {
      "tenantId": "a1b2c3d4-0000-0000-0000-000000000001",
      "name": "Công ty ACME",
      "code": "ACME",
      "databaseMode": "CloudManaged",
      "dbStatus": "Online",
      "displayRole": "Kế toán trưởng",
      "isDefault": true
    },
    {
      "tenantId": "b2c3d4e5-0000-0000-0000-000000000002",
      "name": "Công ty XYZ",
      "code": "XYZ",
      "databaseMode": "OnPremise",
      "dbStatus": "Online",
      "displayRole": "Admin",
      "isDefault": false
    }
  ]
}
```

| Field | Type | Description |
|-------|------|-------------|
| tempToken | string | Short-lived JWT (TTL 60s). Claims: `sub` (MasterUser.Id), `rmb` (rememberMe). Signed with HMAC-SHA256 (separate secret from RS256 access tokens). |
| tempTokenExpiresIn | number | Always `60` (seconds). |
| companies | CompanyInfo[] | List of active companies this user has access to. |

**CompanyInfo** schema:

| Field | Type | Description |
|-------|------|-------------|
| tenantId | uuid | Tenant ID — used in Step 2 |
| name | string | Company display name |
| code | string | Tenant code (e.g. "ACME") |
| databaseMode | string | `"CloudManaged"` or `"OnPremise"` |
| dbStatus | string | `"Online"`, `"Offline"`, `"Provisioning"`, `"Migrating"` |
| displayRole | string? | Optional display label from `sys_master_user_tenants.display_role_name` |
| isDefault | boolean | Whether this is the user's default company |

**Side effects**:
- `MasterUser.LastLoginAt` updated; `MasterUser.FailedLoginCount` reset to 0.
- **NO `refresh_token` cookie set** — JWT is not issued yet (company not selected).
- `rememberMe` value is encoded in tempToken claims for use in Step 2.

> **Frontend hint**: If `companies.length === 1`, auto-call `/api/auth/select-company` immediately (no manual company selection needed).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing/invalid email format, empty password |
| 401 | `INVALID_CREDENTIALS` | Wrong password OR email not found (same message — no enumeration) |
| 401 | `ACCOUNT_DEACTIVATED` | MasterUser `IsActive = false` |
| 401 | `ACCOUNT_LOCKED` | MasterUser `LockedUntil > now` (too many failed attempts) |
| 401 | `NO_COMPANIES` | User has no active companies (all MasterUserTenant mappings are to inactive tenants) |
| 429 | `RATE_LIMITED` | IP exceeded 5 failed attempts in 5-minute window |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "code": "INVALID_CREDENTIALS",
  "detail": "Thông tin đăng nhập không đúng.",
  "traceId": "00-abc123-def456-00"
}
```

---

## POST /api/auth/select-company (Step 2 — Issue JWT)

**Authentication**: None (uses tempToken in request body, NOT Bearer header)
**Rate Limit**: Inherits from Step 1 (same IP window)
**Database**: Master DB (verify tempToken, access check) + Tenant DB (load user roles/permissions)

### Request

```http
POST /api/auth/select-company
Content-Type: application/json
```

```json
{
  "tempToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tenantId": "a1b2c3d4-0000-0000-0000-000000000001"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| tempToken | string | Yes | Valid tempToken from Step 1 (TTL 60s) |
| tenantId | uuid | Yes | Must be a tenant the user has access to |

### Success Response — 200 OK

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "admin@acme.vn",
    "fullName": "Nguyễn Văn A",
    "tenantId": "a1b2c3d4-0000-0000-0000-000000000001",
    "tenantName": "Công ty ACME",
    "roles": ["Admin", "Accountant"],
    "permissions": ["GL.Journal.View", "GL.Journal.Create", "SYS.Users.Manage"]
  }
}
```

**Side effects**:
- `HttpOnly; Secure; SameSite=Strict` cookie `refresh_token` is set in the response headers.
  - `Max-Age`: 604800 (7 days) when original `rememberMe = true` (read from tempToken `rmb` claim); no `Max-Age` (session cookie) when `false`.
- `RefreshToken` record created in Master DB with `tenant_id` = selected tenantId.
- User roles/permissions loaded from Tenant DB (resolved via `ITenantConnectionResolver`).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing tempToken or tenantId, invalid format |
| 401 | `TEMP_TOKEN_EXPIRED` | tempToken TTL exceeded (> 60s) |
| 401 | `TEMP_TOKEN_INVALID` | tempToken signature invalid or malformed |
| 403 | `COMPANY_NOT_ACCESSIBLE` | User not mapped to this tenant in `sys_master_user_tenants` |
| 403 | `TENANT_INACTIVE` | Tenant `is_active = false` |
| 503 | `TENANT_DB_OFFLINE` | Tenant `db_status != 'Online'` (on-premise tunnel down or DB migrating) |

---

## POST /api/auth/switch-company

**Authentication**: Bearer JWT (required — user must be logged in)
**Database**: Master DB (access check, new token) + Tenant DB (load new company's roles/permissions)

### Request

```http
POST /api/auth/switch-company
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
{
  "tenantId": "b2c3d4e5-0000-0000-0000-000000000002"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| tenantId | uuid | Yes | Must differ from current `tid` claim; user must have access |

### Success Response — 200 OK

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "admin@acme.vn",
    "fullName": "Nguyễn Văn A",
    "tenantId": "b2c3d4e5-0000-0000-0000-000000000002",
    "tenantName": "Công ty XYZ",
    "roles": ["Admin"],
    "permissions": ["GL.Journal.View", "GL.Journal.Create", "SYS.Users.Manage"]
  }
}
```

**Side effects**:
- Previous JWT remains valid until expiry (no blacklist — stateless design).
- Previous `refresh_token` cookie is **revoked** in Master DB (`IsRevoked = true`).
- New `refresh_token` cookie set for the new tenant.
- `RefreshToken` record created in Master DB with `tenant_id` = new tenantId.
- User roles/permissions loaded from the new Tenant DB.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing tenantId or same as current |
| 401 | `UNAUTHORIZED` | Missing, malformed, or expired access token |
| 403 | `COMPANY_NOT_ACCESSIBLE` | User not mapped to this tenant in `sys_master_user_tenants` |
| 403 | `TENANT_INACTIVE` | Tenant `is_active = false` |
| 503 | `TENANT_DB_OFFLINE` | Tenant `db_status != 'Online'` |

---

## POST /api/auth/logout

**Authentication**: Bearer JWT (required)

### Request

```http
POST /api/auth/logout
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

Body: empty `{}` or no body required.

### Success Response — 204 No Content

No response body. Side effects:
- The access token's JTI is blacklisted in Redis with TTL = remaining validity seconds.
- The `refresh_token` cookie value (if present) is revoked (`IsRevoked = true` in `sys_refresh_tokens` **in Master DB**).
- The `refresh_token` cookie is cleared (expired `Max-Age=0`) in the response.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `TOKEN_INVALID` | Missing, malformed, or expired access token |
| 401 | `TOKEN_BLACKLISTED` | Token already blacklisted (concurrent logout — idempotent, also returns 204 in practice) |

---

## POST /api/auth/refresh

**Authentication**: HttpOnly cookie `refresh_token` (required; no Authorization header needed)
**Note**: This endpoint does NOT require the current access token — it is intentionally accessible when the access token has expired.
**Database**: Master DB (read/rotate RefreshToken) + Tenant DB (load user claims from `token.TenantId`)

### Request

```http
POST /api/auth/refresh
Cookie: refresh_token=<plaintext_refresh_token_value>
Content-Type: application/json
```

Body: empty `{}` or no body required.

### Success Response — 200 OK

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Side effects**:
- Previous `refresh_token` cookie value marked `IsRevoked = true` in **Master DB**.
- New `refresh_token` cookie set (same family UUID, new token value and hash).
- New token preserves the `rememberMe` behavior of the original session (TTL vs. session cookie).
- User claims (roles, permissions) loaded from Tenant DB resolved via `RefreshToken.TenantId`.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `REFRESH_TOKEN_MISSING` | Cookie absent or empty |
| 401 | `REFRESH_TOKEN_EXPIRED` | `ExpiresAt < now` |
| 401 | `REFRESH_TOKEN_REVOKED` | `IsRevoked = true` (normal rotation) |
| 401 | `REFRESH_TOKEN_REPLAY` | Hash not found AND previous token in family detected as replayed → family revocation triggered; full re-login required |
| 403 | `TENANT_INACTIVE` | Tenant deactivated since token was issued |
| 503 | `TENANT_DB_OFFLINE` | Cannot connect to Tenant DB to load user claims |

---

## GET /.well-known/jwks.json

**Authentication**: None (public endpoint)
**Cache**: Clients SHOULD cache this response. Response includes `Cache-Control: max-age=86400`.

### Request

```http
GET /.well-known/jwks.json
```

### Success Response — 200 OK

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "alg": "RS256",
      "kid": "phanmemketoan-2026-01",
      "n": "sI5F48f5SXlWxH3V...",
      "e": "AQAB"
    }
  ]
}
```

| Field | Description |
|-------|-------------|
| `kty` | Key type — always `RSA` |
| `use` | Usage — `sig` (signature verification) |
| `alg` | Algorithm — `RS256` |
| `kid` | Key ID — matches `kid` header in issued JWTs |
| `n` | RSA modulus (Base64url-encoded) |
| `e` | RSA public exponent (Base64url-encoded) |

**Notes**:
- Only the **public key** is exposed. Private key never leaves the API process.
- Key rotation: a new key can be added to the `keys` array before the old one is retired. Consumers should support multiple keys in the JWKS set.

---

## JWT Access Token Claims Reference

| Claim | Value | Description |
|-------|-------|-------------|
| `sub` | UUID string | User ID (= MasterUser.Id = User.Id in Tenant DB) |
| `tid` | UUID string | Tenant ID — **determines which Tenant DB to connect to for subsequent API requests** |
| `email` | string | User email |
| `name` | string | User full name |
| `roles` | string[] | List of role names (from current Tenant DB) |
| `permissions` | string[] | Flat list of all permission codes (from current Tenant DB) |
| `jti` | UUID string | JWT ID — used for blacklisting on logout |
| `iat` | Unix timestamp | Issued at |
| `exp` | Unix timestamp | Expires at (`iat + 900` seconds by default) |
| `iss` | string | Issuer — configured in `appsettings.json` |
| `aud` | string | Audience — configured in `appsettings.json` |

> **Note**: `tid` is critical — the `TenantMiddleware` reads this claim to resolve the correct Tenant DB connection string via `ITenantConnectionResolver` for all authenticated API requests.

---

## TempToken Claims Reference

| Claim | Value | Description |
|-------|-------|-------------|
| `sub` | UUID string | MasterUser.Id |
| `rmb` | boolean | Original `rememberMe` value (used in Step 2 to set cookie TTL) |
| `iat` | Unix timestamp | Issued at |
| `exp` | Unix timestamp | Expires at (`iat + 60` seconds) |

> **Security**: TempToken is signed with HMAC-SHA256 using a separate secret (NOT the RS256 private key). It has minimal claims to reduce attack surface. It is NOT a Bearer token — passed in request body only.

---

## Authentication Flow Summary

```
┌──────────┐   POST /login       ┌──────────┐   tempToken + companies
│  Client   │ ─────────────────> │  Master   │ ──────────────────────>  Client shows
│ (Angular) │   email, password   │   DB      │   (no JWT, no cookie)    company list
└──────────┘                     └──────────┘
                                                         │
     ┌───────────────────────────────────────────────────┘
     │  POST /select-company
     │  tempToken + tenantId
     ▼
┌──────────┐   verify tempToken   ┌──────────┐   load roles/perms   ┌──────────┐
│  Master   │ ──────────────────> │  Tenant   │ ──────────────────> │  Client   │
│   DB      │   check access      │   DB      │                     │  gets JWT │
└──────────┘                     └──────────┘                     └──────────┘
                                                   + refresh_token cookie

     ┌─── Later, user clicks company switcher ───┐
     │  POST /switch-company                       │
     │  Bearer JWT + tenantId                      │
     ▼                                             ▼
  Master DB (check access)  →  New Tenant DB (load roles)  →  New JWT issued
```
