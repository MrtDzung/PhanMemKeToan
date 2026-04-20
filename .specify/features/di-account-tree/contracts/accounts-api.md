# API Contract: Accounts

**Feature**: DI Module — Account Tree  
**Date**: 2026-04-17  
**Base URL**: `/api/accounts`  
**Auth**: `Authorization: Bearer {jwt}` required on all endpoints.

---

## Response Envelope

```json
{
  "data": { ... },
  "errors": []
}
```

Error format:
```json
{
  "data": null,
  "errors": [
    { "code": "duplicate_code", "message": "Mã tài khoản '1111' đã tồn tại" }
  ]
}
```

---

## GET /api/accounts

Get accounts as a tree or flat list.

### Query Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `format` | `tree` \| `flat` | `tree` | Response structure |
| `includeInactive` | bool | `false` | Include inactive (`Inactive=true`) accounts |
| `q` | string | — | Filter by code/name (flat format only, ignored for tree) |

### Response 200 — Tree Format

```json
{
  "data": [
    {
      "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "accountNumber": "1",
      "accountName": "TÀI SẢN",
      "accountNameEnglish": "ASSETS",
      "grade": 1,
      "isParent": true,
      "accountCategoryKind": 0,
      "inactive": false,
      "isPostableInForeignCurrency": false,
      "hasTransactions": false,
      "children": [
        {
          "accountId": "...",
          "accountNumber": "11",
          "accountName": "Tiền và các khoản tương đương tiền",
          "grade": 2,
          "isParent": true,
          "accountCategoryKind": 0,
          "inactive": false,
          "isPostableInForeignCurrency": true,
          "hasTransactions": false,
          "children": [...]
        }
      ]
    }
  ],
  "errors": []
}
```

### Response 200 — Flat Format

```json
{
  "data": [
    {
      "accountId": "...",
      "accountNumber": "1111",
      "accountName": "Tiền mặt VND",
      "accountCategoryKind": 0,
      "inactive": false,
      "isParent": false
    }
  ],
  "errors": []
}
```

---

## GET /api/accounts/{id}

Get full account detail including all DetailBy* flags.

### Response 200

```json
{
  "data": {
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountNumber": "1311",
    "accountName": "Phải thu của khách hàng",
    "accountNameEnglish": "Trade receivables",
    "parentId": "guid-of-131",
    "parentNumber": "131",
    "parentName": "Phải thu khách hàng",
    "grade": 3,
    "isParent": false,
    "accountCategoryKind": 0,
    "inactive": false,
    "isPostableInForeignCurrency": true,
    "hasTransactions": true,
    "detailByAccountObject": true,
    "accountObjectType": 2,
    "detailByBankAccount": false,
    "detailByJob": false,
    "detailByProjectWork": false,
    "detailByOrder": false,
    "detailByContract": false,
    "detailByExpenseItem": false,
    "detailByDepartment": false,
    "detailByListItem": false,
    "detailByPUContract": false,
    "rowVersion": 3,
    "createdAt": "2026-04-17T00:00:00Z",
    "createdBy": "admin@system.com",
    "modifiedAt": "2026-04-17T10:00:00Z",
    "modifiedBy": "admin@system.com"
  },
  "errors": []
}
```

### Errors

| HTTP | Code | When |
|------|------|------|
| 404 | `account_not_found` | Account ID doesn't exist for this tenant |

---

## POST /api/accounts

Create a new account.

### Request Body

```json
{
  "accountNumber": "1111",
  "accountName": "Tiền mặt VND",
  "accountNameEnglish": "Cash in VND",
  "parentId": "guid-of-111",
  "accountCategoryKind": 0,
  "isPostableInForeignCurrency": false,
  "detailByAccountObject": false,
  "accountObjectType": 0,
  "detailByBankAccount": false,
  "detailByJob": false,
  "detailByProjectWork": false,
  "detailByOrder": false,
  "detailByContract": false,
  "detailByExpenseItem": false,
  "detailByDepartment": false,
  "detailByListItem": false,
  "detailByPUContract": false
}
```

### Response 201

```json
{
  "data": {
    "accountId": "new-guid",
    "accountNumber": "1111",
    "accountName": "Tiền mặt VND",
    "grade": 3,
    "isParent": false,
    "rowVersion": 0
  },
  "errors": []
}
```

### Errors

| HTTP | Code | When |
|------|------|------|
| 400 | `duplicate_code` | AccountNumber already exists in tenant |
| 400 | `code_must_start_with_parent` | AccountNumber doesn't start with parent's AccountNumber |
| 400 | `validation_error` | Missing required fields or field length exceeded |
| 404 | `parent_not_found` | Provided parentId doesn't exist |

---

## PUT /api/accounts/{id}

Update an existing account.

### Request Body

Same as POST body plus `rowVersion`:

```json
{
  "accountNumber": "1111",
  "accountName": "Tiền mặt Việt Nam đồng",
  "accountNameEnglish": "Cash in VND",
  "parentId": "guid-of-111",
  "accountCategoryKind": 0,
  "inactive": false,
  "isPostableInForeignCurrency": false,
  "detailByAccountObject": true,
  "accountObjectType": 2,
  "detailByBankAccount": false,
  "detailByJob": false,
  "detailByProjectWork": false,
  "detailByOrder": false,
  "detailByContract": false,
  "detailByExpenseItem": false,
  "detailByDepartment": false,
  "detailByListItem": false,
  "detailByPUContract": false,
  "rowVersion": 3
}
```

### Notes
- If account has GL entries: `accountNumber` and `parentId` fields are **ignored** (cannot change).
- Server returns `422 locked_has_transactions` if these fields differ from current values AND GL entries exist.

### Response 200

```json
{
  "data": {
    "accountId": "...",
    "rowVersion": 4
  },
  "errors": []
}
```

### Errors

| HTTP | Code | When |
|------|------|------|
| 400 | `duplicate_code` | New AccountNumber conflicts with existing |
| 400 | `code_must_start_with_parent` | New AccountNumber doesn't match parent prefix |
| 404 | `account_not_found` | Account ID not found |
| 409 | `row_version_conflict` | RowVersion mismatch — concurrent edit |
| 422 | `locked_has_transactions` | Trying to change AccountNumber/ParentID when GL entries exist |

---

## DELETE /api/accounts/{id}

Soft-delete an account (`IsDeleted = true`, `Inactive = true`).

### Query Parameters

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `rowVersion` | int | Yes | Optimistic concurrency token |

### Response 204

No body.

### Errors

| HTTP | Code | When |
|------|------|------|
| 404 | `account_not_found` | Account ID not found |
| 409 | `row_version_conflict` | RowVersion mismatch |
| 422 | `has_transactions` | Account has GeneralLedger entries — cannot delete |
| 422 | `has_children` | Account has child accounts — must delete children first |

---

## POST /api/accounts/import

Import standard chart of accounts from built-in template.

### Request Body

```json
{
  "standard": "TT99",
  "conflictResolution": "skip",
  "dryRun": false
}
```

| Field | Values | Description |
|-------|--------|-------------|
| `standard` | `"TT99"` \| `"TT133"` | Which standard to import |
| `conflictResolution` | `"skip"` \| `"overwrite"` | How to handle duplicate AccountNumbers |
| `dryRun` | bool (default `false`) | When `true`, returns preview counts without committing any DB changes |

### Response 200

```json
{
  "data": {
    "imported": 310,
    "skipped": 25,
    "overwritten": 0,
    "errors": []
  },
  "errors": []
}
```

### Notes
- Import is **atomic** — rolled back on any error.
- `errors` in `data` lists non-fatal warnings (e.g., skipped accounts with reasons).
- When `dryRun: true`: same response schema is returned, `imported` shows what **would** be imported, no DB changes are committed.

### Errors

| HTTP | Code | When |
|------|------|------|
| 400 | `invalid_standard` | Unknown standard value |
| 400 | `invalid_conflict_resolution` | Unknown conflictResolution value |
| 500 | `import_failed` | Unexpected error during import (with rollback) |

---

## GET /api/accounts/search

Typeahead search for account lookup in voucher forms.

### Query Parameters

| Name | Type | Default | Required | Description |
|------|------|---------|----------|-------------|
| `q` | string | — | Yes | AccountNumber prefix OR AccountName contains |
| `postableOnly` | bool | `true` | No | Return only `IsParent=false` accounts |
| `limit` | int | `20` | No | Max results (cap: 50) |

### Response 200

```json
{
  "data": [
    {
      "accountId": "...",
      "accountNumber": "1111",
      "accountName": "Tiền mặt VND",
      "accountCategoryKind": 0,
      "inactive": false,
      "isParent": false
    }
  ],
  "errors": []
}
```

### Notes
- Only returns accounts with `Inactive = false` (active accounts only).
- Response time SLA: **<200ms** (uses `text_pattern_ops` index for prefix match + GIN for name search).
- Results ordered: exact AccountNumber match first, then prefix match, then name match.

### Errors

| HTTP | Code | When |
|------|------|------|
| 400 | `query_required` | `q` param is missing or empty |
