# API Contract: Inventory Items

**Module**: DI | **Resources**: `/api/inventory-items`, `/api/inventory-item-categories`, `/api/formula-templates`, `/api/item-attribute-types`  
**Auth**: Bearer JWT required on all endpoints  
**Tenant**: Resolved from JWT `tenant_id` claim; all queries auto-filtered by TenantId  
**Updated**: 2026-04-18 (Review v4 — ItemType, UnitPrice, child collections, FormulaTemplate, AttributeType)

---

## Inventory Item Categories

### GET /api/inventory-item-categories/tree

Returns the full category hierarchy as a nested tree.

**Response 200**

```json
[
  {
    "id": "uuid",
    "categoryCode": "CNTT",
    "categoryName": "Công nghệ thông tin",
    "level": 1,
    "parentId": null,
    "isActive": true,
    "sortOrder": 0,
    "children": [
      {
        "id": "uuid",
        "categoryCode": "CNTT-MT",
        "categoryName": "Máy tính",
        "level": 2,
        "parentId": "uuid",
        "isActive": true,
        "sortOrder": 0,
        "children": []
      }
    ]
  }
]
```

---

### POST /api/inventory-item-categories

Creates a new category.

**Request Body**

```json
{
  "categoryCode": "CNTT-MT",
  "categoryName": "Máy tính",
  "parentId": "uuid-or-null"
}
```

**Validation Rules**

| Field | Rule |
|-------|------|
| `categoryCode` | Required, max 25 chars, unique per tenant |
| `categoryName` | Required, max 255 chars |
| `parentId` | If provided, must reference existing category in tenant |
| Depth | Parent's level must be ≤ 4 (so new node is ≤ level 5; BR-DI05) |

**Response 201** — Created category

**Response 409** — `duplicate_code`

**Response 422** — `max_depth_exceeded`

```json
{
  "status": 422,
  "detail": "Tối đa 5 cấp nhóm hàng hóa. Không thể thêm cấp thứ 6.",
  "extensions": { "code": "max_depth_exceeded" }
}
```

---

### PUT /api/inventory-item-categories/{id}

Updates a category.

**Request Body** — Same as POST

**Response 200** — Updated category

---

### DELETE /api/inventory-item-categories/{id}

Deletes a category.

**Response 204** — Deleted

**Response 422** — `has_children` (block if child categories exist)

**Response 422** — `has_items` (block if any InventoryItem references this category)

---

## Inventory Items

### GET /api/inventory-items

Returns paginated inventory items.

**Query Parameters**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | 1-based page |
| `pageSize` | int | 25 | Records per page |
| `categoryId` | Guid | — | Filter to items in category + all descendants (recursive) |
| `status` | string | `"all"` | `"active"` \| `"inactive"` \| `"all"` |
| `itemType` | int | — | Filter by ItemType enum: 0=RawMaterial, 1=FinishedProduct, 2=Goods, 3=Service. Omit for all types. |
| `search` | string | — | Contains on ItemCode or ItemName |
| `sortBy` | string | `"itemCode"` | Column to sort |
| `sortDir` | string | `"asc"` | `"asc"` \| `"desc"` |

**Response 200**

```json
{
  "data": [
    {
      "id": "uuid",
      "itemCode": "IT001",
      "itemName": "Laptop Dell XPS 13",
      "unitId": "uuid",
      "unitCode": "Cái",
      "categoryId": "uuid",
      "categoryName": "Máy tính",
      "itemType": 2,
      "itemTypeLabel": "Goods",
      "costingMethod": 1,
      "costingMethodLabel": "FIFO",
      "unitPrice": 25000000,
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 480
}
```

---

### GET /api/inventory-items/{id}

Returns full detail of an inventory item including child collections (unitConverts, barcodes, attributes, openingBalances).

**Response 200**

```json
{
  "id": "uuid",
  "itemCode": "IT001",
  "itemName": "Laptop Dell XPS 13",
  "itemNameEnglish": "Laptop Dell XPS 13",
  "description": null,
  "barcode": "8934563012345",
  "itemType": 2,
  "unitId": "uuid",
  "unitCode": "Cái",
  "unitName": "Cái",
  "categoryId": "uuid",
  "categoryCode": "CNTT-MT",
  "categoryName": "Máy tính",
  "costingMethod": 1,
  "unitPrice": 25000000,
  "salePrice1": 30000000,
  "salePrice2": 28000000,
  "salePrice3": null,
  "defaultTaxRate": 10.00,
  "isFollowSerial": false,
  "isFollowLot": false,
  "isFollowExpiry": false,
  "isPanelItem": false,
  "panelUnitId": null,
  "formulaTemplateId": null,
  "formulaTemplateName": null,
  "minStockLevel": 2.00,
  "maxStockLevel": 50.00,
  "leadTimeDays": 7,
  "isActive": true,
  "rowVersion": 1,
  "unitConverts": [
    {
      "id": "uuid",
      "unitId": "uuid",
      "unitCode": "Thùng",
      "unitName": "Thùng",
      "convertRate": 10.000000,
      "isDefaultSaleUnit": false,
      "isDefaultPurchaseUnit": true,
      "sortOrder": 0
    }
  ],
  "barcodes": [
    {
      "id": "uuid",
      "barcodeType": 0,
      "barcodeTypeLabel": "Code128",
      "barcodeValue": "8934563012345",
      "unitId": null,
      "isPrimary": true
    }
  ],
  "itemAttributes": [
    {
      "id": "uuid",
      "attributeTypeId": "uuid",
      "attributeCode": "BRAND",
      "attributeName": "Thương hiệu",
      "attributeValue": "Dell",
      "sortOrder": 0
    }
  ],
  "openingBalances": [
    {
      "id": "uuid",
      "warehouseId": "uuid",
      "warehouseCode": "KHO-HN",
      "warehouseName": "Kho Hà Nội",
      "unitId": "uuid",
      "unitCode": "Cái",
      "quantity": 50.0000,
      "unitCost": 25000000.000000,
      "amount": 1250000000.00,
      "currencyId": null,
      "foreignAmount": null,
      "exchangeRate": null,
      "openingDate": "2026-01-01"
    }
  ]
}
```

---

### POST /api/inventory-items

Creates a new inventory item.

**Request Body**

```json
{
  "itemCode": "IT001",
  "itemName": "Laptop Dell XPS 13",
  "itemNameEnglish": "Laptop Dell XPS 13",
  "description": null,
  "barcode": null,
  "itemType": 2,
  "unitId": "uuid",
  "categoryId": "uuid-or-null",
  "costingMethod": 1,
  "unitPrice": 25000000,
  "salePrice1": 30000000,
  "salePrice2": null,
  "salePrice3": null,
  "defaultTaxRate": 10.00,
  "isFollowSerial": false,
  "isFollowLot": false,
  "isFollowExpiry": false,
  "isPanelItem": false,
  "panelUnitId": null,
  "formulaTemplateId": null,
  "minStockLevel": 2,
  "maxStockLevel": 50,
  "leadTimeDays": 7,
  "isActive": true,
  "unitConverts": [
    {
      "unitId": "uuid",
      "convertRate": 10.000000,
      "isDefaultSaleUnit": false,
      "isDefaultPurchaseUnit": true,
      "sortOrder": 0
    }
  ],
  "barcodes": [
    {
      "barcodeType": 0,
      "barcodeValue": "8934563012345",
      "unitId": null,
      "isPrimary": true
    }
  ],
  "itemAttributes": [
    {
      "attributeTypeId": "uuid",
      "attributeValue": "Dell",
      "sortOrder": 0
    }
  ],
  "openingBalances": [
    {
      "warehouseId": "uuid",
      "unitId": "uuid",
      "quantity": 50,
      "unitCost": 25000000,
      "currencyId": null,
      "foreignAmount": null,
      "exchangeRate": null,
      "openingDate": "2026-01-01"
    }
  ]
}
```

> Child collections (`unitConverts`, `barcodes`, `itemAttributes`, `openingBalances`) are optional arrays. Omit or pass `[]` if no data. On PUT, child collections are **full-replaced** (server deletes existing children and re-inserts from request).

**Validation Rules**

| Field | Rule |
|-------|------|
| `itemCode` | Required, max 25 chars, unique per tenant |
| `itemName` | Required, max 255 chars |
| `itemType` | Must be 0 (RawMaterial), 1 (FinishedProduct), 2 (Goods), or 3 (Service). Default: 2 |
| `unitId` | Required, must reference existing Unit in tenant |
| `categoryId` | Optional; if provided must reference existing category in tenant |
| `costingMethod` | Must be 1, 2, 3, or 4 (BR-IN02). Default: 3 |
| `maxStockLevel` | Must be > `minStockLevel` when both are non-zero (FR-IN-026) |
| `panelUnitId` | Optional; if provided must reference existing Unit |
| `formulaTemplateId` | Optional; if provided must reference existing FormulaTemplate |
| `unitConverts[].unitId` | Must reference existing Unit; must be unique per item; must differ from item's main unitId (BR-IN06) |
| `unitConverts[].convertRate` | Required, decimal > 0 |
| `barcodes[].barcodeValue` | Required, max 255 chars, unique per tenant (BR-IN05) |
| `barcodes[].barcodeType` | 0=Code128, 1=EAN13, 2=EAN8, 3=QRCode, 4=DataMatrix, 5=UPC_A |
| `itemAttributes[].attributeTypeId` | Must reference existing ItemAttributeType; unique per item (one value per type) |
| `openingBalances[].warehouseId` | Must reference existing Warehouse; unique per item+warehouse+unit (BR-IN03) |
| `openingBalances[].quantity` | Decimal ≥ 0 |
| `openingBalances[].unitCost` | Decimal ≥ 0 |
| `openingBalances` | Not allowed when `itemType` = 3 (Service, BR-IN04) |

**Response 201** — Created item

**Response 409** — `duplicate_code`

---

### PUT /api/inventory-items/{id}

Updates an existing inventory item.

**Request Body** — Same as POST, plus `rowVersion`.

**Response 200** — Updated item

**Response 409** — `row_version_conflict`

---

### DELETE /api/inventory-items/{id}

Deletes an inventory item.

- Hard delete if no stock transactions reference this item
- Soft delete (IsActive=false, IsDeleted=true) if stock transactions exist

**Response 204** — Deleted

**Response 404** — Not found

---

## CostingMethod Reference

| Value | Label (Vietnamese) | Label (English) |
|-------|-------------------|-----------------|
| 1 | Nhập trước xuất trước | FIFO |
| 2 | Nhập sau xuất trước | LIFO |
| 3 | Bình quân gia quyền | Weighted Average |
| 4 | Đích danh | Specific Identification |

Default: 3 (Weighted Average, BR-IN01)

## ItemType Reference

| Value | Label (Vietnamese) | Label (English) |
|-------|-------------------|-----------------|
| 0 | Nguyên vật liệu | RawMaterial |
| 1 | Thành phẩm | FinishedProduct |
| 2 | Hàng hóa | Goods |
| 3 | Dịch vụ | Service |

Default: 2 (Goods)

## BarcodeType Reference

| Value | Label |
|-------|-------|
| 0 | Code128 |
| 1 | EAN13 |
| 2 | EAN8 |
| 3 | QRCode |
| 4 | DataMatrix |
| 5 | UPC_A |

---

## Formula Templates — `/api/formula-templates` (FR-IN-015)

BOM (Bill of Materials) master templates for finished products.

### GET /api/formula-templates

Returns all formula templates for the tenant.

**Query Parameters**: `search` (contains on FormulaCode or FormulaName), `isActive` (boolean, optional)

**Response 200**

```json
[
  {
    "id": "uuid",
    "formulaCode": "BOM-001",
    "formulaName": "Công thức sản xuất Bàn gỗ",
    "isActive": true,
    "detailCount": 3
  }
]
```

### GET /api/formula-templates/{id}

Returns full detail including material lines.

**Response 200**

```json
{
  "id": "uuid",
  "formulaCode": "BOM-001",
  "formulaName": "Công thức sản xuất Bàn gỗ",
  "isActive": true,
  "details": [
    {
      "id": "uuid",
      "materialItemId": "uuid",
      "materialItemCode": "NVL-001",
      "materialItemName": "Gỗ sồi",
      "unitId": "uuid",
      "unitCode": "M3",
      "quantity": 0.500000,
      "description": "Gỗ mặt bàn",
      "sortOrder": 0
    }
  ]
}
```

### POST /api/formula-templates

**Request Body**

```json
{
  "formulaCode": "BOM-001",
  "formulaName": "Công thức sản xuất Bàn gỗ",
  "isActive": true,
  "details": [
    {
      "materialItemId": "uuid",
      "unitId": "uuid",
      "quantity": 0.5,
      "description": "Gỗ mặt bàn",
      "sortOrder": 0
    }
  ]
}
```

**Validation**

| Field | Rule |
|-------|------|
| `formulaCode` | Required, max 25 chars, unique per tenant |
| `formulaName` | Required, max 255 chars |
| `details[].materialItemId` | Must reference existing InventoryItem |
| `details[].unitId` | Must reference existing Unit |
| `details[].quantity` | Required, decimal > 0 |

**Response 201** — Created template with details

**Response 409** — `duplicate_code`

### PUT /api/formula-templates/{id}

**Request Body** — Same as POST. Details are **full-replaced**.

**Response 200** — Updated

### DELETE /api/formula-templates/{id}

Soft-delete. Items referencing this template will have `FormulaTemplateId` set to NULL (ON DELETE SET NULL).

**Response 204** — Deleted

---

## Item Attribute Types — `/api/item-attribute-types` (FR-IN-016)

Tenant-level custom attribute type definitions (e.g., "Thương hiệu", "Màu sắc").

### GET /api/item-attribute-types

Returns all attribute types for the tenant.

**Query Parameters**: `search` (contains on AttributeCode or AttributeName)

**Response 200**

```json
[
  {
    "id": "uuid",
    "attributeCode": "BRAND",
    "attributeName": "Thương hiệu",
    "sortOrder": 0,
    "isActive": true
  }
]
```

### POST /api/item-attribute-types

**Request Body**

```json
{
  "attributeCode": "BRAND",
  "attributeName": "Thương hiệu",
  "sortOrder": 0,
  "isActive": true
}
```

**Validation**

| Field | Rule |
|-------|------|
| `attributeCode` | Required, max 25 chars, unique per tenant |
| `attributeName` | Required, max 100 chars |

**Response 201** — Created

**Response 409** — `duplicate_code`

### PUT /api/item-attribute-types/{id}

**Response 200** — Updated

### DELETE /api/item-attribute-types/{id}

**Response 422** — `has_references` if any InventoryItemAttribute references this type.

**Response 204** — Deleted

---

## Error Codes Reference

| HTTP | Code | Message |
|------|------|---------|
| 400 | `validation_error` | See `errors` map |
| 404 | `inventory_item_not_found` | Không tìm thấy hàng hóa |
| 404 | `category_not_found` | Không tìm thấy nhóm hàng hóa |
| 409 | `duplicate_code` | Mã đã tồn tại |
| 409 | `duplicate_barcode` | Mã vạch đã được sử dụng bởi hàng hóa khác |
| 409 | `row_version_conflict` | Bản ghi đã được chỉnh sửa bởi người dùng khác |
| 422 | `max_depth_exceeded` | Tối đa 5 cấp nhóm hàng hóa |
| 422 | `has_children` | Không thể xóa nhóm khi còn nhóm con |
| 422 | `has_items` | Không thể xóa nhóm khi còn hàng hóa thuộc nhóm này |
| 422 | `has_references` | Không thể xóa khi đang được sử dụng |
| 422 | `service_no_stock` | Dịch vụ không có tồn kho đầu kỳ |
