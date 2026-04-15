# API Contracts: Authentication (`/api/auth`)

---

## POST /api/auth/login

**Authentication**: None (public endpoint)  
**Rate Limit**: 5 requests per 5-minute sliding window per IP → 429 on violation

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
| rememberMe | boolean | No | Defaults to `false` |

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
  - `Max-Age`: 604800 (7 days) when `rememberMe = true`; no `Max-Age` (session cookie) when `false`.
- `User.LastLoginAt` updated; `User.FailedLoginCount` reset to 0.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing/invalid email format, empty password |
| 401 | `INVALID_CREDENTIALS` | Wrong password OR email not found (same message — no enumeration) |
| 401 | `ACCOUNT_DEACTIVATED` | User `IsActive = false` (checked before password verification) |
| 403 | `TENANT_INACTIVE` | Resolved tenant `IsActive = false` |
| 403 | `TENANT_NOT_FOUND` | Unknown `X-Tenant-Code` header on public request |
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
- The `refresh_token` cookie value (if present) is revoked (`IsRevoked = true` in `sys_refresh_tokens`).
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
- Previous `refresh_token` cookie value marked `IsRevoked = true`.
- New `refresh_token` cookie set (same family UUID, new token value and hash).
- New token preserves the `rememberMe` behavior of the original session (TTL vs. session cookie).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `REFRESH_TOKEN_MISSING` | Cookie absent or empty |
| 401 | `REFRESH_TOKEN_EXPIRED` | `ExpiresAt < now` |
| 401 | `REFRESH_TOKEN_REVOKED` | `IsRevoked = true` (normal rotation) |
| 401 | `REFRESH_TOKEN_REPLAY` | Hash not found AND previous token in family detected as replayed → family revocation triggered; full re-login required |
| 403 | `TENANT_INACTIVE` | Tenant deactivated since token was issued |

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
| `sub` | UUID string | User ID |
| `tid` | UUID string | Tenant ID |
| `email` | string | User email |
| `name` | string | User full name |
| `roles` | string[] | List of role names |
| `permissions` | string[] | Flat list of all permission codes |
| `jti` | UUID string | JWT ID — used for blacklisting on logout |
| `iat` | Unix timestamp | Issued at |
| `exp` | Unix timestamp | Expires at (`iat + 900` seconds by default) |
| `iss` | string | Issuer — configured in `appsettings.json` |
| `aud` | string | Audience — configured in `appsettings.json` |
