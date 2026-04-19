# API Contract: Excel Import

**Module**: DI | **Resource**: `/api/import`  
**Auth**: Bearer JWT required  
**Tenant**: Resolved from JWT `tenant_id` claim

---

## Overview

The import API supports bulk creation of AccountObjects and InventoryItems from Excel files. It follows a **best-effort model** (DD-001): each row is validated and inserted individually. Valid rows are committed immediately; invalid rows are skipped and accumulated in the error report. There is no batch-level transaction.

**File Limits**:
- Maximum file size: 5 MB
- Maximum rows per batch: 5,000
- Accepted format: `.xlsx` (Excel 2007+)

---

## 1. Template Downloads

### GET /api/import/template/account-objects

Returns an Excel template file for AccountObject import.

**Response 200**

- Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Content-Disposition: `attachment; filename="AccountObject_Template.xlsx"`
- Body: Binary Excel file

**Template columns** (Row 1 = headers, Row 2 = example, Row 3+ = user data):

| Column | Required | Max Length | Notes |
|--------|----------|-----------|-------|
| ObjectCode | Yes | 25 | Unique identifier |
| ObjectName | Yes | 255 | Vietnamese name |
| ObjectType | Yes | — | "Khách hàng", "Nhà cung cấp", "Nhân viên", or combinations like "Khách hàng,Nhà cung cấp" |
| Address | No | 500 | |
| TaxCode | No | 50 | Mã số thuế (MST Việt Nam ≤20, quốc tế ≤50) |
| Email | No | 255 | |
| Phone | No | 50 | |
| CreditLimit | No | — | Numeric, VND, no thousand separators |
| PaymentTermDays | No | — | Integer |

**ObjectType values accepted** (case-insensitive):
- `"Khách hàng"` or `"Customer"` → ObjectType = 1
- `"Nhà cung cấp"` or `"Vendor"` → ObjectType = 2
- `"Nhân viên"` or `"Employee"` → ObjectType = 4
- Comma-separated combinations: `"Khách hàng,Nhà cung cấp"` → ObjectType = 3

---

### GET /api/import/template/inventory-items

Returns an Excel template file for InventoryItem import.

**Response 200** — Excel file as attachment

**Template columns**:

| Column | Required | Max Length | Notes |
|--------|----------|-----------|-------|
| ItemCode | Yes | 25 | Unique identifier |
| ItemName | Yes | 255 | Vietnamese name |
| UnitCode | Yes | 25 | Must match existing Unit.UnitCode |
| CategoryCode | No | 25 | Must match existing InventoryItemCategory.CategoryCode |
| ItemType | No | — | "NVL" (RawMaterial), "TP" (FinishedProduct), "HH" (Goods), "DV" (Service). Default: "HH" |
| Barcode | No | 255 | Legacy single-barcode field |
| UnitPrice | No | — | Decimal, default purchase/cost price |
| CostingMethod | No | — | "FIFO", "LIFO", "BQ" (Bình quân), "DD" (Đích danh). Default: "BQ" |
| MinStock | No | — | Decimal |
| MaxStock | No | — | Decimal |

---

## 2. Import Endpoints

### POST /api/import/account-objects

Imports AccountObjects from uploaded Excel file.

**Request**

- Content-Type: `multipart/form-data`
- Form field: `file` (binary, .xlsx)

**Example cURL**:
```
POST /api/import/account-objects
Authorization: Bearer <token>
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="file"; filename="customers.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

[binary content]
--boundary--
```

**Processing Logic**

```
1. Validate file extension (.xlsx only)
2. Validate file size ≤ 5MB
3. Open workbook with ClosedXML
4. Read headers from row 1 (validate expected columns present)
5. Count data rows; reject if > 5000
6. For each data row (row 2..N):
   a. Parse all fields
   b. Validate required fields (ObjectCode, ObjectName, ObjectType)
   c. Parse ObjectType from text to bitmask
   d. Validate ObjectType ≥ 1
   e. Check ObjectCode uniqueness against DB (tenant-scoped)
   f. If valid: INSERT into account_objects, SaveChanges
   g. If invalid: add to errors list, continue to next row
7. Return ImportResultDto
```

**Response 200** (always, even if all rows fail)

```json
{
  "successCount": 8,
  "errorCount": 2,
  "errors": [
    {
      "rowNumber": 3,
      "field": "ObjectCode",
      "message": "Mã đã tồn tại: KH001"
    },
    {
      "rowNumber": 7,
      "field": "ObjectName",
      "message": "Tên đối tượng không được để trống"
    }
  ]
}
```

**Response 400** — File-level rejections (before row processing):

```json
{
  "status": 400,
  "detail": "File quá lớn. Tối đa 5MB.",
  "extensions": { "code": "file_too_large" }
}
```

```json
{
  "status": 400,
  "detail": "File chứa quá 5.000 dòng dữ liệu. Vui lòng chia nhỏ file.",
  "extensions": { "code": "row_limit_exceeded" }
}
```

```json
{
  "status": 400,
  "detail": "Định dạng file không hợp lệ. Vui lòng sử dụng file .xlsx.",
  "extensions": { "code": "invalid_file_format" }
}
```

---

### POST /api/import/inventory-items

Imports InventoryItems from uploaded Excel file.

**Request** — Same multipart format as account-objects endpoint.

**Processing Logic**

```
1–5. Same file validation as AccountObjects
6. For each data row:
   a. Parse all fields
   b. Validate required fields (ItemCode, ItemName, UnitCode)
   c. Resolve UnitCode → UnitId (lookup from DB cache)
   d. If UnitCode not found: error "Đơn vị tính không tồn tại: {code}"
   e. Resolve CategoryCode → CategoryId (lookup from DB cache, optional)
   f. If CategoryCode provided but not found: error "Nhóm hàng không tồn tại: {code}"
   g. Parse CostingMethod from text (default WeightedAverage if blank)
   h. Validate ItemCode uniqueness
   i. If valid: INSERT into inventory_items, SaveChanges
   j. If invalid: add to errors list, continue
7. Return ImportResultDto
```

**Response 200** — Same shape as AccountObjects import

---

## 3. Error Report Download

When `errorCount > 0`, the client may request an error report Excel.

### POST /api/import/error-report

Generates a downloadable Excel file containing only the failed rows with an error column appended.

**Request Body**

```json
{
  "entityType": "account-objects",
  "errors": [
    { "rowNumber": 3, "field": "ObjectCode", "message": "Mã đã tồn tại: KH001" },
    { "rowNumber": 7, "field": "ObjectName", "message": "Tên không được để trống" }
  ],
  "originalFileName": "customers.xlsx"
}
```

> Note: The client must re-upload the original file along with the error list. The server re-reads the specified row numbers from the file and appends the error column.

**Alternative approach**: The frontend builds the error report entirely in the browser using the row data already in memory from the preview step (Step 2 of the import wizard). This avoids a round-trip.

**Response 200** — Excel attachment: `ErrorReport_customers.xlsx`

**Error report columns**: All original columns + `Lỗi` (Error column at end, in red)

---

## 4. Import Result DTO

```csharp
// Application/Features/Import/DTOs/ImportResultDto.cs
public record ImportResultDto(
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportRowErrorDto> Errors
);

public record ImportRowErrorDto(
    int RowNumber,
    string Field,
    string Message
);
```

---

## Error Codes Reference

| HTTP | Code | Message |
|------|------|---------|
| 400 | `file_too_large` | File quá lớn. Tối đa 5MB. |
| 400 | `row_limit_exceeded` | Quá 5.000 dòng. Vui lòng chia nhỏ file. |
| 400 | `invalid_file_format` | Chỉ chấp nhận file .xlsx |
| 400 | `missing_required_columns` | File thiếu cột bắt buộc: {columnNames} |

**Row-level errors** (in `errors[]` array, not HTTP error):

| Field | Message |
|-------|---------|
| ObjectCode / ItemCode | "Mã đã tồn tại: {code}" (BR-DI06) |
| ObjectCode / ItemCode | "Mã không được để trống" |
| ObjectName / ItemName | "Tên không được để trống" |
| ObjectType | "Loại đối tượng không hợp lệ: {value}" |
| UnitCode | "Đơn vị tính không tồn tại: {code}" |
| CategoryCode | "Nhóm hàng không tồn tại: {code}" |
| CostingMethod | "Phương pháp tính giá không hợp lệ: {value}" |
| (any merged cell) | "Ô gộp hoặc công thức không được hỗ trợ — ô trống được xử lý là giá trị rỗng" |
