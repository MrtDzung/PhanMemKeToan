# API Contract: Account Objects

**Module**: DI | **Resource**: `/api/account-objects`  
**Auth**: Bearer JWT required on all endpoints  
**Tenant**: Resolved from JWT `tenant_id` claim; all queries auto-filtered by TenantId  
**Updated**: 2026-04-18 (Review v4 — EmployeeProfile DD-007, accountObjectGroupId)

---

## Endpoints

### GET /api/account-objects

Returns a paginated list of account objects.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | 1-based page number |
| `pageSize` | int | 25 | Records per page (10, 25, 50, 100) |
| `type` | int | 0 | Bitmask filter. Returns objects where `ObjectType & type != 0`. 0 = all types. |
| `status` | string | `"all"` | `"active"` \| `"inactive"` \| `"all"` |
| `search` | string | — | Case-insensitive contains on ObjectCode or ObjectName |
| `sortBy` | string | `"objectCode"` | Column to sort by |
| `sortDir` | string | `"asc"` | `"asc"` \| `"desc"` |

**Response 200**

```json
{
  "data": [
    {
      "id": "uuid",
      "objectCode": "KH001",
      "objectName": "Công ty TNHH ABC",
      "objectType": 1,
      "taxCode": "0123456789",
      "phone": "024-1234567",
      "isActive": true,
      "createdAt": "2026-04-18T09:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 152
}
```

---

### GET /api/account-objects/{id}

Returns full detail of a single account object including bank accounts, opening balances, and employee profile (when Employee bit is set).

**Path Parameters**: `id` — Guid

**Response 200**

> `employeeProfile` is `null` when the AccountObject does not have the Employee bit (4) set in ObjectType. `gender`: 1=Male, 2=Female, 3=Other (null = not specified, field is nullable).

```json
{
  "id": "uuid",
  "objectCode": "KH001",
  "objectName": "Công ty TNHH ABC",
  "objectNameEnglish": "ABC Company Ltd",
  "address": "123 Nguyễn Huệ, Q1, TP.HCM",
  "taxCode": "0123456789",
  "email": "info@abc.vn",
  "phone": "024-1234567",
  "fax": null,
  "website": "https://abc.vn",
  "contactPerson": "Nguyễn Văn A",
  "contactPhone": "090-1234567",
  "description": null,
  "objectType": 1,
  "creditLimit": 50000000,
  "paymentTermDays": 30,
  "isActive": true,
  "rowVersion": 3,
  "bankAccounts": [
    {
      "id": "uuid",
      "bankName": "Vietcombank",
      "bankBranch": "Hà Nội",
      "accountNumber": "1234567890",
      "swiftCode": "BFTVVNVX"
    }
  ],
  "openingBalances": [
    {
      "id": "uuid",
      "currencyId": "uuid",
      "currencyCode": "VND",
      "debitAmount": 5000000,
      "debitAmountOC": 5000000,
      "creditAmount": 0,
      "creditAmountOC": 0,
      "exchangeRate": 1
    }
  ],
  "employeeProfile": {
    "id": "uuid",
    "citizenId": "012345678901",
    "dateOfBirth": "1990-05-15",
    "gender": 1,
    "socialInsuranceNumber": "0123456789",
    "hireDate": "2020-01-15",
    "departmentId": "uuid",
    "departmentName": "Phòng Kế Toán",
    "dependentCount": 2
  },
  "accountObjectGroupId": "uuid-or-null"
}
```

**Response 404** — `account_object_not_found`

---

### POST /api/account-objects

Creates a new account object.

**Request Body**

```json
{
  "objectCode": "KH001",
  "objectName": "Công ty TNHH ABC",
  "objectNameEnglish": "ABC Company Ltd",
  "address": "123 Nguyễn Huệ, Q1, TP.HCM",
  "taxCode": "0123456789",
  "email": "info@abc.vn",
  "phone": "024-1234567",
  "fax": null,
  "website": null,
  "contactPerson": null,
  "contactPhone": null,
  "description": null,
  "objectType": 1,
  "creditLimit": 50000000,
  "paymentTermDays": 30,
  "isActive": true,
  "bankAccounts": [
    {
      "bankName": "Vietcombank",
      "bankBranch": "Hà Nội",
      "accountNumber": "1234567890",
      "swiftCode": null
    }
  ],
  "openingBalances": [
    {
      "currencyId": "uuid",
      "debitAmount": 5000000,
      "debitAmountOC": 5000000,
      "creditAmount": 0,
      "creditAmountOC": 0,
      "exchangeRate": 1
    }
  ],
  "employeeProfile": {
    "citizenId": "012345678901",
    "dateOfBirth": "1990-05-15",
    "gender": 1,
    "socialInsuranceNumber": "0123456789",
    "hireDate": "2020-01-15",
    "departmentId": "uuid-or-null",
    "dependentCount": 2
  },
  "accountObjectGroupId": "uuid-or-null"
}
```

> `employeeProfile` is optional. Include it only when ObjectType has the Employee bit (4) set. If ObjectType does not include Employee and `employeeProfile` is provided, the server ignores it. If Employee bit is removed on PUT and `employeeProfile` existed, the server soft-deletes the profile.

**Validation Rules**

| Field | Rule |
|-------|------|
| `objectCode` | Required, max 25 chars, unique per tenant |
| `objectName` | Required, max 255 chars |
| `objectType` | Required, integer ≥ 1 (at least one type bit set, BR-DI01) |
| `bankAccounts[].accountNumber` | Required when bank account row present |
| `openingBalances[].currencyId` | Must reference existing Currency in tenant |
| `employeeProfile.citizenId` | Optional, max 20 chars, unique per tenant (BR-DI07) |
| `employeeProfile.gender` | 1=Male, 2=Female, 3=Other (null if not specified) |
| `employeeProfile.socialInsuranceNumber` | Optional, max 10 chars, unique per tenant (BR-DI08) |
| `employeeProfile.departmentId` | Optional; if provided must reference existing active Department |
| `employeeProfile.dependentCount` | Integer ≥ 0, default 0 |
| `accountObjectGroupId` | Optional; if provided must reference existing AccountObjectGroup |

**Response 201** — Returns the created object (same shape as GET by id)

**Response 400** — Validation failure (RFC 7807 problem details)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "objectType": ["Phải chọn ít nhất một loại đối tượng (Khách hàng / Nhà cung cấp / Nhân viên)"]
  }
}
```

**Response 409** — `duplicate_code`

```json
{
  "type": "...",
  "title": "Conflict",
  "status": 409,
  "detail": "Mã đối tượng KH001 đã tồn tại trong hệ thống"
}
```

---

### PUT /api/account-objects/{id}

Updates an existing account object. Bank accounts and opening balances are replaced entirely (full replace, not patch).

**Request Body** — Same as POST, plus:

```json
{
  "rowVersion": 3,
  ...
}
```

**Validation Rules** — Same as POST plus:

| Field | Rule |
|-------|------|
| `rowVersion` | Required; must match current DB value |
| `objectCode` | Cannot change if any posted voucher references this object (BR-DI03) |

**Response 200** — Updated object

**Response 404** — Not found

**Response 409** — `row_version_conflict` (optimistic concurrency)

```json
{
  "status": 409,
  "detail": "Bản ghi đã được chỉnh sửa bởi người dùng khác. Vui lòng tải lại trang.",
  "extensions": { "code": "row_version_conflict" }
}
```

**Response 422** — `code_change_locked` (has voucher references)

---

### DELETE /api/account-objects/{id}

Deletes an account object. Soft-delete if vouchers reference it; hard delete if no references.

**Response 204** — Deleted successfully

**Response 404** — Not found

**Response 422** — `has_voucher_references` (when hard delete is attempted but blocked)

> Note: Soft-deactivation is always allowed. This endpoint performs the correct operation automatically based on reference check.

---

## Type Bitmask Reference

| Value | Meaning |
|-------|---------|
| 1 | Customer (Khách hàng) |
| 2 | Vendor (Nhà cung cấp) |
| 3 | Customer + Vendor |
| 4 | Employee (Nhân viên) |
| 5 | Customer + Employee |
| 6 | Vendor + Employee |
| 7 | All three types |

**Filter Logic**: `GET /api/account-objects?type=1` returns all objects where `ObjectType & 1 != 0` (i.e., Customers, Customer+Vendors, Customer+Employees, All).

---

## Error Codes Reference

| HTTP | Code | Message (Vietnamese) |
|------|------|---------------------|
| 400 | `validation_error` | See `errors` map |
| 404 | `account_object_not_found` | Không tìm thấy đối tượng kế toán |
| 409 | `duplicate_code` | Mã đối tượng đã tồn tại |
| 409 | `duplicate_citizen_id` | Số CMND/CCCD đã tồn tại |
| 409 | `duplicate_social_insurance_number` | Mã số BHXH đã tồn tại |
| 409 | `row_version_conflict` | Bản ghi đã được chỉnh sửa bởi người dùng khác |
| 422 | `code_change_locked` | Không thể thay đổi mã khi đã có chứng từ liên kết |
| 422 | `has_voucher_references` | Không thể xóa khi đã có chứng từ liên kết |
