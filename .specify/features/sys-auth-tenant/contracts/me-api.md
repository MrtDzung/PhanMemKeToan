# API Contracts: Current User Profile (`/api/me`)

**Base Path**: `/api/me`  
**Authentication**: Bearer JWT required for all endpoints  
**Scope**: Each endpoint operates on the authenticated user''s own account only. No admin override — these endpoints are self-service.

---

## GET /api/me

Returns the full profile of the currently authenticated user, including tenant context, roles, and the flat list of all permission codes.

### Request

```http
GET /api/me
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Success Response — 200 OK

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "nva@acme.vn",
  "fullName": "Nguyễn Văn A",
  "isActive": true,
  "tenantId": "a1b2c3d4-0000-0000-0000-000000000001",
  "tenantName": "Công ty ACME",
  "roles": ["Accountant", "Reviewer"],
  "permissions": [
    "GL.Journal.View",
    "GL.Journal.Create",
    "GL.Journal.Post",
    "CA.Receipt.View",
    "CA.Receipt.Create",
    "SYS.Users.View"
  ],
  "lastLoginAt": "2026-04-15T07:30:00Z",
  "createdAt": "2026-01-10T00:00:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| id | UUID | User''s unique identifier |
| email | string | Login email |
| fullName | string | Display name |
| isActive | boolean | Account active status |
| tenantId | UUID | Tenant the user belongs to |
| tenantName | string | Tenant display name |
| roles | string[] | Names of all assigned roles |
| permissions | string[] | Flat union of all permission codes from all roles |
| lastLoginAt | ISO 8601 | Timestamp of most recent successful login; null if never logged in |
| createdAt | ISO 8601 | Account creation timestamp |

**Note**: The `permissions` array is the union of all permissions from all assigned roles — duplicates are removed. This is the same set embedded in the access token (useful for re-hydrating Angular auth store after page reload if needed).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `UNAUTHORIZED` | Missing, expired, or invalid Bearer token |
| 401 | `TOKEN_BLACKLISTED` | Token JTI is in the Redis blacklist (user logged out) |
| 403 | `TENANT_INACTIVE` | Tenant was deactivated after token was issued |

---

## PUT /api/me/profile

Updates the authenticated user''s display name. Only `fullName` can be changed via this endpoint. Email and password changes use separate flows.

### Request

```http
PUT /api/me/profile
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "fullName": "Nguyễn Văn An"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| fullName | string | Yes | Non-empty (not whitespace-only), max 200 chars |

### Success Response — 200 OK

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "nva@acme.vn",
  "fullName": "Nguyễn Văn An",
  "updatedAt": "2026-04-15T10:05:00Z"
}
```

**Note**: The updated `fullName` will be reflected in future access tokens after the user''s next token refresh (within the 15-minute TTL window). This is acceptable per the spec (§ User Story 6).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Empty or whitespace-only `fullName`; exceeds 200 chars |
| 401 | `UNAUTHORIZED` | Missing or invalid token |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Unprocessable Entity",
  "status": 400,
  "code": "VALIDATION_ERROR",
  "errors": {
    "fullName": ["Họ tên không được để trống."]
  },
  "traceId": "00-abc123-def456-00"
}
```

---

## PUT /api/me/password

Changes the authenticated user''s password. Requires verification of the current password. On success, all existing refresh tokens for this user are revoked (forcing re-login on other devices).

### Request

```http
PUT /api/me/password
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "currentPassword": "OldP@ssw0rd!",
  "newPassword": "NewSecureP@ss2!",
  "confirmPassword": "NewSecureP@ss2!"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| currentPassword | string | Yes | Non-empty |
| newPassword | string | Yes | 8–128 chars; must include uppercase, lowercase, digit, and special character (OWASP ASVS §2.1.1) |
| confirmPassword | string | Yes | Must exactly match `newPassword` |

### Success Response — 204 No Content

No response body.

**Side effects**:
- `User.PasswordHash` updated with new BCrypt hash (cost 12).
- All `RefreshToken` rows for this user set `IsRevoked = true`.
- The current access token continues to work until its natural expiry (TTL 15 min); the next refresh attempt will fail, prompting re-login.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing fields; `newPassword` does not meet complexity requirements |
| 400 | `PASSWORDS_DO_NOT_MATCH` | `newPassword` ≠ `confirmPassword` (server-side check) |
| 400 | `SAME_AS_CURRENT` | `newPassword` is identical to the current password |
| 401 | `UNAUTHORIZED` | Missing or invalid access token |
| 401 | `WRONG_CURRENT_PASSWORD` | `currentPassword` does not match stored hash |

```json
{
  "status": 401,
  "code": "WRONG_CURRENT_PASSWORD",
  "detail": "Mật khẩu hiện tại không đúng.",
  "traceId": "00-abc123-def456-00"
}
```

---

## Common Headers

All `/api/me` endpoints return:

```
Content-Type: application/json; charset=utf-8
X-Request-Id: <uuid>       (echoes traceId for client-side correlation)
```

On 4xx/5xx responses, the body always follows RFC 7807 `ProblemDetails` format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.x",
  "title": "<HTTP status reason phrase>",
  "status": <HTTP status code>,
  "code": "<SCREAMING_SNAKE_CASE application code>",
  "detail": "<Vietnamese user-facing message>",
  "traceId": "<ASP.NET Core trace ID>"
}
```
