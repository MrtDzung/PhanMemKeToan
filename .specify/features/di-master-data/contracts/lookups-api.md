# API Contract: Lookup Data

**Module**: DI | **Auth**: Bearer JWT required  
**Tenant**: Resolved from JWT `tenant_id` claim; all queries auto-filtered by TenantId  
**Updated**: 2026-04-18 (Review v4 — isActive on all entities, currencyNameEnglish)

Covers: **Currency**, **Unit**, **Warehouse**, **Department**, **ExpenseItem**

> ItemAttributeType and FormulaTemplate CRUD are defined in `inventory-items-api.md` (FR-IN-015, FR-IN-016).

All lookup entities follow the same standard CRUD pattern. Deviations are noted per entity.

---

## Standard CRUD Pattern

For each lookup entity:

```
GET    /api/{resource}          — List all (with optional search)
POST   /api/{resource}          — Create
PUT    /api/{resource}/{id}     — Update
DELETE /api/{resource}/{id}     — Delete (blocked if referenced)
```

---

## 1. Currencies — `/api/currencies`

### GET /api/currencies

Returns all currencies for the tenant.

**Response 200**

```json
[
  {
    "id": "uuid",
    "currencyCode": "VND",
    "currencyName": "Đồng Việt Nam",
    "currencyNameEnglish": "Vietnamese Dong",
    "symbol": "₫",
    "exchangeRate": 1.00,
    "isActive": true
  },
  {
    "id": "uuid",
    "currencyCode": "USD",
    "currencyName": "Đô la Mỹ",
    "currencyNameEnglish": "US Dollar",
    "symbol": "$",
    "exchangeRate": 25430.00,
    "isActive": true
  }
]
```

### POST /api/currencies

**Request Body**

```json
{
  "currencyCode": "USD",
  "currencyName": "Đô la Mỹ",
  "currencyNameEnglish": "US Dollar",
  "symbol": "$",
  "exchangeRate": 25430.00,
  "isActive": true
}
```

**Validation**

| Field | Rule |
|-------|------|
| `currencyCode` | Required, max 10 chars, unique per tenant (ISO 4217 recommended) |
| `currencyName` | Required, max 100 chars |
| `currencyNameEnglish` | Optional, max 100 chars |
| `symbol` | Required, max 10 chars |
| `exchangeRate` | Required, decimal > 0 |
| `isActive` | Boolean, default true |

**Response 201** — Created currency

**Response 409** — `duplicate_code`

### PUT /api/currencies/{id}

**Request Body** — Same as POST

**Response 200** — Updated

**Notes**:
- VND exchange rate must remain 1.00 (system base currency). Consider blocking edits to VND rate.
- Updating exchange rate does not affect existing vouchers (Assumption §6 of spec).

### DELETE /api/currencies/{id}

**Response 422** — `has_references` if any `AccountObjectOpeningBalance` references this currency.

---

## 2. Units — `/api/units`

### GET /api/units

**Query Parameters**: `search` (contains on UnitCode or UnitName)

**Response 200**

```json
[
  { "id": "uuid", "unitCode": "Cái", "unitName": "Cái", "isActive": true },
  { "id": "uuid", "unitCode": "Kg", "unitName": "Ki-lô-gam", "isActive": true }
]
```

### POST /api/units

**Request Body**

```json
{
  "unitCode": "Tấn",
  "unitName": "Tấn",
  "isActive": true
}
```

**Validation**

| Field | Rule |
|-------|------|
| `unitCode` | Required, max 25 chars, unique per tenant |
| `unitName` | Required, max 100 chars |
| `isActive` | Boolean, default true |

**Response 201** — Created unit

### PUT /api/units/{id}

**Response 200** — Updated

### DELETE /api/units/{id}

**Response 422** — `has_references` if any `InventoryItem`, `InventoryItemUnitConvert`, `InventoryItemOpeningBalance`, or `FormulaDetail` references this unit (FR-LK-008)

**Response 422** body:

```json
{
  "status": 422,
  "detail": "Không thể xóa đơn vị tính đang được sử dụng bởi 15 hàng hóa.",
  "extensions": { "code": "has_references", "referenceCount": 15 }
}
```

---

## 3. Warehouses — `/api/warehouses`

### GET /api/warehouses

**Response 200**

```json
[
  {
    "id": "uuid",
    "warehouseCode": "KHO-HN",
    "warehouseName": "Kho Hà Nội",
    "address": "123 Đường Láng, Đống Đa, Hà Nội",
    "managerName": "Nguyễn Văn B",
    "isActive": true
  }
]
```

### POST /api/warehouses

**Request Body**

```json
{
  "warehouseCode": "KHO-HN",
  "warehouseName": "Kho Hà Nội",
  "address": "123 Đường Láng, Đống Đa, Hà Nội",
  "managerName": "Nguyễn Văn B",
  "isActive": true
}
```

**Validation**

| Field | Rule |
|-------|------|
| `warehouseCode` | Required, max 25 chars, unique per tenant |
| `warehouseName` | Required, max 255 chars |
| `address` | Optional, max 500 chars |
| `managerName` | Optional, max 255 chars |
| `isActive` | Boolean, default true |

**Response 201** — Created warehouse

### PUT /api/warehouses/{id}

**Response 200** — Updated

### DELETE /api/warehouses/{id}

**Response 204** — Soft delete (IsDeleted=true) if referenced by inventory transactions; hard delete otherwise.

---

## 4. Departments — `/api/departments`

Department is a self-referential tree (max 5 levels, BR-DI05). Follows same pattern as InventoryItemCategory.

### GET /api/departments/tree

Returns full department tree.

**Response 200**

```json
[
  {
    "id": "uuid",
    "deptCode": "VAN-PHONG",
    "deptName": "Văn Phòng Công Ty",
    "level": 1,
    "parentId": null,
    "isActive": true,
    "children": [
      {
        "id": "uuid",
        "deptCode": "KE-TOAN",
        "deptName": "Phòng Kế Toán",
        "level": 2,
        "parentId": "uuid",
        "isActive": true,
        "children": []
      }
    ]
  }
]
```

### GET /api/departments

Returns flat list (for dropdowns). Optionally filter by `isActive=true` for active-only.

**Query Parameters**: `isActive` (boolean, optional)

**Response 200** — Array of `{ id, deptCode, deptName, level, parentId, isActive }`

### POST /api/departments

**Request Body**

```json
{
  "deptCode": "KE-TOAN",
  "deptName": "Phòng Kế Toán",
  "parentId": "uuid-or-null"
}
```

**Validation**

| Field | Rule |
|-------|------|
| `deptCode` | Required, max 25 chars, unique per tenant |
| `deptName` | Required, max 255 chars |
| `parentId` | Optional; if provided, parent's level must be ≤ 4 |

**Response 201** — Created

**Response 422** — `max_depth_exceeded`

```json
{
  "status": 422,
  "detail": "Tối đa 5 cấp phòng ban. Không thể thêm cấp thứ 6."
}
```

### PUT /api/departments/{id}

**Response 200** — Updated

### DELETE /api/departments/{id}

**Response 422** — `has_children` if child departments exist

---

## 5. ExpenseItems — `/api/expense-items`

### GET /api/expense-items

**Query Parameters**: `search` (contains on ExpenseCode or ExpenseName)

**Response 200**

```json
[
  {
    "id": "uuid",
    "expenseCode": "CP-LUONG",
    "expenseName": "Chi phí lương nhân viên",
    "accountCode": "6421"
  }
]
```

### POST /api/expense-items

**Request Body**

```json
{
  "expenseCode": "CP-LUONG",
  "expenseName": "Chi phí lương nhân viên",
  "accountCode": "6421"
}
```

**Validation**

| Field | Rule |
|-------|------|
| `expenseCode` | Required, max 25 chars, unique per tenant |
| `expenseName` | Required, max 255 chars |
| `accountCode` | Optional, max 20 chars; if provided, should reference an existing account (soft validation — warning, not block) |

**Response 201** — Created

### PUT /api/expense-items/{id}

**Response 200** — Updated

### DELETE /api/expense-items/{id}

**Response 204** — Soft delete

---

## Common Error Codes

| HTTP | Code | Message |
|------|------|---------|
| 400 | `validation_error` | See `errors` map |
| 404 | `not_found` | Không tìm thấy bản ghi |
| 409 | `duplicate_code` | Mã đã tồn tại trong hệ thống |
| 422 | `has_references` | Không thể xóa khi đang được sử dụng |
| 422 | `max_depth_exceeded` | Tối đa 5 cấp. Không thể thêm cấp tiếp theo. |
| 422 | `has_children` | Không thể xóa khi còn nút con |

---

## Seeded Data (FR-LK-007)

On new tenant creation, the following records are automatically seeded:

**Currencies**:
- `VND` — Đồng Việt Nam — ₫ — Rate: 1

**Units**:
- Cái, Chiếc, Hộp, Kg, Lít, M2, M3, Thùng, Bộ, Đôi
