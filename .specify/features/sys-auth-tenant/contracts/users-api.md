# API Contracts: User Management (`/api/users`)

**Base Path**: `/api/users`  
**Authentication**: Bearer JWT required for all endpoints  
**Required Permission**: `SYS.Users.View` (GET list/single) or `SYS.Users.Manage` (all write operations)

---

## GET /api/users

Returns a paginated, filterable list of users within the authenticated user''s tenant.

### Request

```http
GET /api/users?page=1&pageSize=20&search=nguyen&isActive=true
Authorization: Bearer <token>
```

| Query Param | Type | Default | Description |
|------------|------|---------|-------------|
| page | int | 1 | 1-based page number |
| pageSize | int | 20 | Items per page (max 100) |
| search | string | null | Filter by email or fullName (case-insensitive) |
| isActive | bool | null | Filter by active status; null = all |

### Success Response — 200 OK

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "nva@acme.vn",
      "fullName": "Nguyễn Văn A",
      "isActive": true,
      "roles": ["Accountant", "Reviewer"],
      "lastLoginAt": "2026-04-14T08:30:00Z",
      "createdAt": "2026-01-10T00:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 45,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `UNAUTHORIZED` | Missing or invalid Bearer token |
| 403 | `FORBIDDEN` | Missing `SYS.Users.View` permission |

---

## POST /api/users

Creates a new user in the authenticated admin''s tenant.

### Request

```http
POST /api/users
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "email": "nva@acme.vn",
  "fullName": "Nguyễn Văn A",
  "password": "InitialP@ss1!",
  "roleIds": [
    "a1b2c3d4-aaaa-bbbb-cccc-000000000001",
    "a1b2c3d4-aaaa-bbbb-cccc-000000000002"
  ]
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| email | string | Yes | Valid email, max 256 chars; unique within tenant |
| fullName | string | Yes | Non-empty, max 200 chars |
| password | string | Yes | 8–128 chars; upper + lower + digit + special |
| roleIds | UUID[] | No | All IDs must belong to the same tenant; empty = no roles |

### Success Response — 201 Created

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "nva@acme.vn",
  "fullName": "Nguyễn Văn A",
  "isActive": true,
  "roles": ["Accountant"],
  "createdAt": "2026-04-15T10:00:00Z"
}
```

**Response header**: `Location: /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6`

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Field validation failure (see detail) |
| 403 | `FORBIDDEN` | Missing `SYS.Users.Manage` permission |
| 409 | `DUPLICATE_EMAIL` | Email already used in this tenant |
| 422 | `INVALID_ROLE` | One or more roleIds not found or belong to another tenant |

---

## GET /api/users/{id}

Returns a single user by ID within the authenticated user''s tenant.

### Request

```http
GET /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <token>
```

### Success Response — 200 OK

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "nva@acme.vn",
  "fullName": "Nguyễn Văn A",
  "isActive": true,
  "roles": [
    { "id": "a1b2c3d4-...", "name": "Accountant" }
  ],
  "lastLoginAt": "2026-04-14T08:30:00Z",
  "failedLoginCount": 0,
  "lockedUntil": null,
  "createdAt": "2026-01-10T00:00:00Z",
  "createdBy": "superadmin@system.local",
  "updatedAt": "2026-04-01T09:00:00Z",
  "updatedBy": "admin@acme.vn"
}
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `UNAUTHORIZED` | Missing or invalid token |
| 403 | `FORBIDDEN` | Missing `SYS.Users.View` permission |
| 404 | `NOT_FOUND` | User ID not found within tenant (EF filter prevents cross-tenant data leaks) |

---

## PUT /api/users/{id}

Updates a user''s profile and role assignments. Cannot update password (use change-password flow).

### Request

```http
PUT /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "email": "nva.new@acme.vn",
  "fullName": "Nguyễn Văn An",
  "roleIds": ["a1b2c3d4-aaaa-bbbb-cccc-000000000003"]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| email | string | Yes | If changed, re-validates uniqueness within tenant |
| fullName | string | Yes | Non-empty, max 200 chars |
| roleIds | UUID[] | Yes | Full replacement of role assignments (not additive) |

### Success Response — 200 OK

Returns the updated user object (same shape as GET /api/users/{id}).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Field validation failure |
| 403 | `FORBIDDEN` | Missing `SYS.Users.Manage` permission |
| 404 | `NOT_FOUND` | User not found in tenant |
| 409 | `DUPLICATE_EMAIL` | New email conflicts with existing user in tenant |

---

## POST /api/users/{id}/deactivate

Deactivates a user account (soft-delete). All existing refresh tokens for the user are revoked.

### Request

```http
POST /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/deactivate
Authorization: Bearer <token>
```

Body: empty.

### Success Response — 204 No Content

No response body.

**Side effects**: `User.IsActive = false`; all `RefreshToken.IsRevoked = true` for this user.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `ALREADY_DEACTIVATED` | User is already inactive |
| 403 | `FORBIDDEN` | Missing `SYS.Users.Manage` permission |
| 403 | `CANNOT_DEACTIVATE_SELF` | Admin attempting to deactivate their own account |
| 404 | `NOT_FOUND` | User not found in tenant |

---

## POST /api/users/{id}/reactivate

Re-enables a previously deactivated user account.

### Request

```http
POST /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/reactivate
Authorization: Bearer <token>
```

Body: empty.

### Success Response — 204 No Content

**Side effects**: `User.IsActive = true`.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `ALREADY_ACTIVE` | User is already active |
| 403 | `FORBIDDEN` | Missing `SYS.Users.Manage` |
| 404 | `NOT_FOUND` | User not found in tenant |

---

## POST /api/users/{id}/unlock

Clears the failed login counter and removes the account lock (resets `FailedLoginCount = 0` and `LockedUntil = null`).

### Request

```http
POST /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/unlock
Authorization: Bearer <token>
```

Body: empty.

### Success Response — 204 No Content

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `NOT_LOCKED` | User has no lock to clear |
| 403 | `FORBIDDEN` | Missing `SYS.Users.Manage` |
| 404 | `NOT_FOUND` | User not found in tenant |
