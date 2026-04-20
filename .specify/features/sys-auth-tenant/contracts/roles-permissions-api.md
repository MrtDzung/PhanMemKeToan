# API Contracts: Roles & Permissions (`/api/roles`, `/api/permissions`)

> **Scope note**: All endpoints operate on the **Tenant DB** (per-company). Roles and permissions are tenant-scoped — changes do NOT affect other companies. Master DB has no role/permission tables.

**Authentication**: Bearer JWT required for all endpoints  
**Required Permission**:
- `SYS.Roles.View` — GET list/single role endpoints
- `SYS.Roles.Manage` — all role write operations + permission matrix endpoints
- `SYS.Roles.Manage` — GET /api/permissions (admin must have manage to view the matrix)

---

## GET /api/roles

Returns all roles for the authenticated user''s tenant, with user count per role.

### Request

```http
GET /api/roles
Authorization: Bearer <token>
```

### Success Response — 200 OK

```json
[
  {
    "id": "a1b2c3d4-aaaa-bbbb-cccc-000000000001",
    "name": "Admin",
    "description": "Full system administrator access",
    "userCount": 2,
    "permissionCount": 87,
    "createdAt": "2026-01-01T00:00:00Z",
    "updatedAt": null
  },
  {
    "id": "a1b2c3d4-aaaa-bbbb-cccc-000000000002",
    "name": "Accountant",
    "description": "General ledger and cash management",
    "userCount": 5,
    "permissionCount": 24,
    "createdAt": "2026-01-05T00:00:00Z",
    "updatedAt": "2026-03-10T09:00:00Z"
  }
]
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `UNAUTHORIZED` | Missing or invalid token |
| 403 | `FORBIDDEN` | Missing `SYS.Roles.View` permission |

---

## POST /api/roles

Creates a new role in the authenticated user''s tenant.

### Request

```http
POST /api/roles
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "Reviewer",
  "description": "Can view and approve vouchers across all modules",
  "permissionIds": [
    "uuid-for-GL.Journal.View",
    "uuid-for-GL.Journal.Post"
  ]
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| name | string | Yes | Non-empty, max 100 chars; unique within tenant |
| description | string | No | Max 500 chars |
| permissionIds | UUID[] | No | IDs must exist in `sys_permissions`; empty = role with no permissions |

### Success Response — 201 Created

```json
{
  "id": "a1b2c3d4-aaaa-bbbb-cccc-000000000003",
  "name": "Reviewer",
  "description": "Can view and approve vouchers across all modules",
  "userCount": 0,
  "permissionCount": 2,
  "createdAt": "2026-04-15T10:00:00Z",
  "updatedAt": null
}
```

**Response header**: `Location: /api/roles/a1b2c3d4-aaaa-bbbb-cccc-000000000003`

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Field validation failure |
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |
| 409 | `DUPLICATE_ROLE_NAME` | Role name already exists within tenant |
| 422 | `INVALID_PERMISSION` | One or more permissionIds not found |

---

## GET /api/roles/{id}

Returns a single role with its full permission list.

### Request

```http
GET /api/roles/a1b2c3d4-aaaa-bbbb-cccc-000000000002
Authorization: Bearer <token>
```

### Success Response — 200 OK

```json
{
  "id": "a1b2c3d4-aaaa-bbbb-cccc-000000000002",
  "name": "Accountant",
  "description": "General ledger and cash management",
  "userCount": 5,
  "permissions": [
    { "id": "uuid1", "code": "GL.Journal.View", "name": "Xem bút toán", "moduleCode": "GL" },
    { "id": "uuid2", "code": "GL.Journal.Create", "name": "Tạo bút toán", "moduleCode": "GL" },
    { "id": "uuid3", "code": "CA.Receipt.View", "name": "Xem phiếu thu", "moduleCode": "CA" }
  ],
  "createdAt": "2026-01-05T00:00:00Z",
  "createdBy": "admin@acme.vn",
  "updatedAt": "2026-03-10T09:00:00Z",
  "updatedBy": "admin@acme.vn"
}
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 403 | `FORBIDDEN` | Missing `SYS.Roles.View` |
| 404 | `NOT_FOUND` | Role not found within tenant |

---

## PUT /api/roles/{id}

Updates a role''s name, description, and permission assignments. Permission assignment is a full replacement (not additive).

### Request

```http
PUT /api/roles/a1b2c3d4-aaaa-bbbb-cccc-000000000002
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "Accountant",
  "description": "Updated description",
  "permissionIds": [
    "uuid-for-GL.Journal.View",
    "uuid-for-GL.Journal.Create",
    "uuid-for-GL.Journal.Post",
    "uuid-for-CA.Receipt.View"
  ]
}
```

### Success Response — 200 OK

Returns the updated role object (same shape as GET /api/roles/{id}).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Field validation failure |
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |
| 404 | `NOT_FOUND` | Role not found |
| 409 | `DUPLICATE_ROLE_NAME` | New name conflicts with existing role in tenant |
| 422 | `INVALID_PERMISSION` | Unknown permission IDs |

---

## DELETE /api/roles/{id}

Deletes a role and all its permission assignments. Rejected if any active user is currently assigned to this role.

### Request

```http
DELETE /api/roles/a1b2c3d4-aaaa-bbbb-cccc-000000000002
Authorization: Bearer <token>
```

### Success Response — 204 No Content

**Side effects**: Role deleted; all `sys_role_permissions` records for this role deleted (cascade).

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |
| 404 | `NOT_FOUND` | Role not found |
| 409 | `ROLE_IN_USE` | Role is assigned to one or more active users |

```json
{
  "status": 409,
  "code": "ROLE_IN_USE",
  "detail": "Không thể xóa vai trò đang được gán cho người dùng.",
  "affectedUsers": [
    { "id": "uuid1", "email": "nva@acme.vn", "fullName": "Nguyễn Văn A" },
    { "id": "uuid2", "email": "nvb@acme.vn", "fullName": "Nguyễn Văn B" }
  ]
}
```

---

## GET /api/permissions

Returns all permissions grouped by module code. Used to populate the permission assignment UI.

### Request

```http
GET /api/permissions
Authorization: Bearer <token>
```

### Success Response — 200 OK

```json
[
  {
    "moduleCode": "SYS",
    "moduleName": "Quản trị hệ thống",
    "permissions": [
      { "id": "uuid1", "code": "SYS.Users.View", "name": "Xem danh sách người dùng" },
      { "id": "uuid2", "code": "SYS.Users.Manage", "name": "Quản lý người dùng" },
      { "id": "uuid3", "code": "SYS.Roles.View", "name": "Xem danh sách vai trò" },
      { "id": "uuid4", "code": "SYS.Roles.Manage", "name": "Quản lý vai trò" }
    ]
  },
  {
    "moduleCode": "GL",
    "moduleName": "Sổ cái chung",
    "permissions": [
      { "id": "uuid5", "code": "GL.Journal.View", "name": "Xem bút toán" },
      { "id": "uuid6", "code": "GL.Journal.Create", "name": "Tạo bút toán" },
      { "id": "uuid7", "code": "GL.Journal.Edit", "name": "Sửa bút toán" },
      { "id": "uuid8", "code": "GL.Journal.Delete", "name": "Xóa bút toán" },
      { "id": "uuid9", "code": "GL.Journal.Post", "name": "Ghi sổ bút toán" }
    ]
  }
]
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |

---

## GET /api/permissions/matrix

Returns a cross-tab of all roles × all permissions. Used to render the permission matrix grid.

### Request

```http
GET /api/permissions/matrix
Authorization: Bearer <token>
```

### Success Response — 200 OK

```json
{
  "roles": [
    { "id": "role-uuid-1", "name": "Admin" },
    { "id": "role-uuid-2", "name": "Accountant" }
  ],
  "modules": [
    {
      "moduleCode": "GL",
      "moduleName": "Sổ cái chung",
      "permissions": [
        {
          "id": "perm-uuid-1",
          "code": "GL.Journal.View",
          "name": "Xem bút toán",
          "roleAssignments": {
            "role-uuid-1": true,
            "role-uuid-2": true
          }
        },
        {
          "id": "perm-uuid-2",
          "code": "GL.Journal.Post",
          "name": "Ghi sổ bút toán",
          "roleAssignments": {
            "role-uuid-1": true,
            "role-uuid-2": false
          }
        }
      ]
    }
  ]
}
```

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |

---

## PUT /api/permissions/matrix

Bulk-assigns or removes permissions for a single role. Replaces the entire permission set for that role.

### Request

```http
PUT /api/permissions/matrix
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "roleId": "role-uuid-2",
  "permissionIds": [
    "perm-uuid-1",
    "perm-uuid-2",
    "perm-uuid-5",
    "perm-uuid-8"
  ]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| roleId | UUID | Yes | Must belong to the authenticated user''s tenant |
| permissionIds | UUID[] | Yes | Full replacement list. Empty array = remove all permissions from role |

### Success Response — 204 No Content

**Side effects**: All existing `sys_role_permissions` rows for `roleId` are deleted and replaced with the provided list.

### Error Responses

| Status | Code | Condition |
|--------|------|-----------|
| 400 | `VALIDATION_ERROR` | Missing roleId or invalid UUID format |
| 403 | `FORBIDDEN` | Missing `SYS.Roles.Manage` |
| 404 | `ROLE_NOT_FOUND` | roleId not found in tenant |
| 422 | `INVALID_PERMISSION` | One or more permissionIds do not exist |
