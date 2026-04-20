# Data Model: DI Module — Step 3: Master Data

**Date**: 2026-04-18 | **Updated**: 2026-04-18 (Review v4 — All fixes applied: Bug1/2, Gaps1-5, DD-006, DD-007, 19 entities final) | **Spec**: `spec.md` | **Status**: Final Review

---

## 1. AccountObject (Đối tượng kế toán)

### C# Domain Entity

```csharp
// Domain/Entities/AccountObject.cs
namespace PhanMemKeToan.Domain.Entities;

public class AccountObject : AuditableEntity  // TenantId, IsDeleted, CreatedAt/By, ModifiedAt/By
{
    public string ObjectCode { get; set; } = string.Empty;      // max 25, unique per tenant
    public string ObjectName { get; set; } = string.Empty;      // max 255, required
    public string? ObjectNameEnglish { get; set; }               // max 255
    public string? Address { get; set; }                         // max 500
    public string? TaxCode { get; set; }                         // max 50 — Inhongha nvarchar(50), MST quốc tế có thể > 20 ký tự (BR-B)
    public string? Email { get; set; }                           // max 255
    public string? Phone { get; set; }                           // max 50
    public string? Fax { get; set; }                             // max 50
    public string? Website { get; set; }                         // max 255
    public string? ContactPerson { get; set; }                   // max 255
    public string? ContactPhone { get; set; }                    // max 50
    public string? Description { get; set; }                     // text, unlimited

    /// <summary>Bitmask: 1=Customer, 2=Vendor, 4=Employee. Must be ≥ 1. (BR-DI04)</summary>
    public int ObjectType { get; set; }

    public decimal CreditLimit { get; set; }                     // numeric(18,2) — hỗ trợ ngoại tệ (BR-F)
    public int PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;                   // false = Inactive (FR-AO-007)

    public int RowVersion { get; set; }                          // optimistic concurrency

    /// <summary>Optional grouping FK — CRUD deferred to later sprint (BR-E)</summary>
    public Guid? AccountObjectGroupId { get; set; }

    // Navigation
    public AccountObjectGroup? AccountObjectGroup { get; set; }
    public ICollection<AccountObjectBankAccount> BankAccounts { get; set; } = [];
    public ICollection<AccountObjectOpeningBalance> OpeningBalances { get; set; } = [];
}
```

### C# Child Entity — BankAccount

```csharp
// Domain/Entities/AccountObjectBankAccount.cs
namespace PhanMemKeToan.Domain.Entities;

public class AccountObjectBankAccount : AuditableEntity
{
    public Guid AccountObjectId { get; set; }                    // FK → AccountObject.Id
    public string BankName { get; set; } = string.Empty;         // max 255
    public string? BankBranch { get; set; }                      // max 255
    public string AccountNumber { get; set; } = string.Empty;    // max 50, required
    public string? SwiftCode { get; set; }                       // max 20

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
}
```

### C# Child Entity — OpeningBalance

```csharp
// Domain/Entities/AccountObjectOpeningBalance.cs
namespace PhanMemKeToan.Domain.Entities;

public class AccountObjectOpeningBalance : AuditableEntity
{
    public Guid AccountObjectId { get; set; }                    // FK → AccountObject.Id
    public Guid CurrencyId { get; set; }                         // FK → Currency.Id
    public decimal DebitAmount { get; set; }                     // numeric(18,0) — VND equivalent
    public decimal CreditAmount { get; set; }                    // numeric(18,0) — VND equivalent
    public decimal DebitAmountOC { get; set; }                   // numeric(18,3) — nguyên tệ Nợ (BR-G)
    public decimal CreditAmountOC { get; set; }                  // numeric(18,3) — nguyên tệ Có (BR-G)
    public decimal ExchangeRate { get; set; } = 1m;              // numeric(18,2) — tỷ giá tại thời điểm nhập; 1 với VND (BR-G)

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
}
```

---

### C# Entity — AccountObjectGroup (Nhóm đối tượng kế toán)

```csharp
// Domain/Entities/AccountObjectGroup.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Optional grouping for AccountObjects. FK added to AccountObject; CRUD deferred to later sprint (BR-E).
/// Mirrors Inhongha AccountObjectGroup (61 rows production).</summary>
public class AccountObjectGroup : AuditableEntity
{
    public string GroupCode { get; set; } = string.Empty;        // max 25, unique per tenant
    public string GroupName { get; set; } = string.Empty;        // max 255, required
    /// <summary>Bitmask: 1=Customer groups, 2=Vendor groups, 4=Employee groups</summary>
    public int ObjectType { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AccountObject> AccountObjects { get; set; } = [];
}
```

---

### C# Extension Entity — AccountObjectEmployeeProfile (DD-007)

```csharp
// Domain/Entities/AccountObjectEmployeeProfile.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Thông tin nhân viên bổ sung cho AccountObject có ObjectType bao gồm Employee (bit 4).
/// 1:1 optional với AccountObject — chỉ tồn tại khi IsEmployee.
/// Phạm vi DI Module: đủ cho khai báo PIT (TNCN) và BHXH/BHYT/BHTN.
/// Thông tin lương/hợp đồng chi tiết (bậc lương, hợp đồng) → PA Module (EmployeePayrollConfig, EmployeeContract).
/// Design Decision DD-007.</summary>
public class AccountObjectEmployeeProfile : AuditableEntity
{
    public Guid AccountObjectId { get; set; }                    // PK + FK → AccountObject.Id (1:1)

    /// <summary>Số CMND / CCCD / Hộ chiếu. Dùng khai báo Thuế TNCN Form 05, 09 (TT111/2013).</summary>
    public string? CitizenId { get; set; }                       // max 20 (CCCD 12 số; hộ chiếu ≤ 20 ký tự)

    /// <summary>Ngày sinh. Dùng tính tuổi; khai báo PIT và BHXH (TT59/2015).</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Giới tính. Dùng báo cáo BHXH/BHYT và báo cáo lao động (NĐ145/2020).</summary>
    public Gender? Gender { get; set; }

    /// <summary>Số sổ BHXH / Mã số BHXH. Dùng khai báo BHXH/BHYT/BHTN (TT59/2015-BHXH).</summary>
    public string? SocialInsuranceNumber { get; set; }           // max 10 (Mã số BHXH Việt Nam: 10 ký tự số)

    /// <summary>Ngày ký hợp đồng / vào công ty. Dùng tính thâm niên, trợ cấp thôi việc (BLLĐ 2019).</summary>
    public DateTime? HireDate { get; set; }

    /// <summary>Bộ phận / Phòng ban làm việc. FK → Department. Dùng phân bổ chi phí lương theo bộ phận (GL).</summary>
    public Guid? DepartmentId { get; set; }                      // FK → Department.Id, nullable

    /// <summary>Số người phụ thuộc giảm trừ gia cảnh (Giảm trừ gia cảnh PIT: 4.4tr/người/tháng — TT111/2013).</summary>
    public int DependentCount { get; set; } = 0;                 // 0 = không có người phụ thuộc

    // Navigation
    public AccountObject AccountObject { get; set; } = null!;
    public Department? Department { get; set; }
}
```

```csharp
// Domain/Enums/Gender.cs
namespace PhanMemKeToan.Domain.Enums;

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}
```

### Navigation — thêm vào AccountObject

```csharp
// Thêm vào class AccountObject (1:1 optional):
public AccountObjectEmployeeProfile? EmployeeProfile { get; set; }
```

### PostgreSQL DDL

```sql
CREATE TABLE "account_object_employee_profiles" (
    "Id"                    uuid        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              uuid        NOT NULL,
    "AccountObjectId"       uuid        NOT NULL,                   -- 1:1 với account_objects
    "CitizenId"             varchar(20) NULL,                       -- CMND/CCCD/Hộ chiếu (TT111/2013)
    "DateOfBirth"           date        NULL,                       -- Ngày sinh
    "Gender"                int         NULL,                       -- 1=Nam, 2=Nữ, 3=Khác
    "SocialInsuranceNumber" varchar(10) NULL,                       -- Mã số BHXH (10 ký tự)
    "HireDate"              date        NULL,                       -- Ngày vào công ty (BLLĐ 2019)
    "DepartmentId"          uuid        NULL,                       -- FK → departments.Id
    "DependentCount"        int         NOT NULL DEFAULT 0,         -- Số người phụ thuộc giảm trừ PIT
    -- Audit
    "CreatedAt"             timestamptz NOT NULL DEFAULT now(),
    "CreatedBy"             uuid        NULL,
    "UpdatedAt"             timestamptz NULL,
    "UpdatedBy"             uuid        NULL,
    "IsDeleted"             boolean     NOT NULL DEFAULT false,
    "DeletedAt"             timestamptz NULL,
    CONSTRAINT "PK_account_object_employee_profiles" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_account_obj_emp_profile_obj_id"
        UNIQUE ("AccountObjectId")                                  -- 1:1 enforcement
);

ALTER TABLE "account_object_employee_profiles"
    ADD CONSTRAINT "FK_emp_profile_account_object"
        FOREIGN KEY ("AccountObjectId") REFERENCES "account_objects"("Id") ON DELETE CASCADE,
    ADD CONSTRAINT "FK_emp_profile_department"
        FOREIGN KEY ("DepartmentId") REFERENCES "departments"("Id") ON DELETE SET NULL;

-- Unique BHXH per tenant (mỗi tenant: 1 người = 1 mã BHXH)
CREATE UNIQUE INDEX "UX_emp_profile_social_insurance"
    ON "account_object_employee_profiles" ("TenantId", "SocialInsuranceNumber")
    WHERE "SocialInsuranceNumber" IS NOT NULL AND "IsDeleted" = false;

-- Unique CCCD per tenant
CREATE UNIQUE INDEX "UX_emp_profile_citizen_id"
    ON "account_object_employee_profiles" ("TenantId", "CitizenId")
    WHERE "CitizenId" IS NOT NULL AND "IsDeleted" = false;
```

### EF Core Configuration

```csharp
public class AccountObjectEmployeeProfileConfiguration : IEntityTypeConfiguration<AccountObjectEmployeeProfile>
{
    public void Configure(EntityTypeBuilder<AccountObjectEmployeeProfile> builder)
    {
        builder.ToTable("account_object_employee_profiles");
        builder.Property(e => e.CitizenId).HasMaxLength(20);
        builder.Property(e => e.SocialInsuranceNumber).HasMaxLength(10);

        // 1:1 relationship — Id trùng với AccountObjectId (alternate key pattern)
        builder.HasIndex(e => e.AccountObjectId).IsUnique();

        // Unique indexes (partial — chỉ khi giá trị non-null)
        builder.HasIndex(e => new { e.TenantId, e.SocialInsuranceNumber })
               .IsUnique()
               .HasFilter("\"SocialInsuranceNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(e => new { e.TenantId, e.CitizenId })
               .IsUnique()
               .HasFilter("\"CitizenId\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasOne(e => e.AccountObject)
               .WithOne(a => a.EmployeeProfile)
               .HasForeignKey<AccountObjectEmployeeProfile>(e => e.AccountObjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
               .WithMany()
               .HasForeignKey(e => e.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## 2. InventoryItem (Hàng hóa / Vật tư / Dịch vụ)

### C# Enums

```csharp
// Domain/Enums/CostingMethod.cs
namespace PhanMemKeToan.Domain.Enums;

public enum CostingMethod
{
    FIFO = 1,
    LIFO = 2,
    WeightedAverage = 3,     // Default (BR-IN01)
    SpecificIdentification = 4
}
```

```csharp
// Domain/Enums/InventoryItemType.cs
namespace PhanMemKeToan.Domain.Enums;

/// <summary>Mirrors Inhongha InventoryItemType table (4 rows).
/// Drives default GL account selection and stock movement behavior (BR-A).</summary>
public enum InventoryItemType
{
    RawMaterial = 0,          // Nguyên vật liệu — default account 152, tạo stock movement
    FinishedProduct = 1,      // Thành phẩm — default account 155, tạo stock movement
    Goods = 2,                // Hàng hóa — default account 156, tạo stock movement (DEFAULT)
    Service = 3               // Dịch vụ — KHÔNG tạo stock movement, không nhập/xuất kho
}
```

```csharp
// Domain/Enums/BarcodeType.cs
namespace PhanMemKeToan.Domain.Enums;

/// <summary>Loại mã vạch/QR hỗ trợ. Dùng trong InventoryItemBarcode (Gap P).</summary>
public enum BarcodeType
{
    Code128 = 0,        // Barcode 1D thông thường
    EAN13 = 1,          // Barcode bán lẻ 13 chữ số (GTIN-13)
    EAN8 = 2,           // Barcode bán lẻ 8 chữ số
    QRCode = 3,         // QR Code 2D
    DataMatrix = 4,     // Data Matrix 2D
    UPC_A = 5           // UPC-A (thị trường Mỹ)
}
```

### C# Domain Entity

```csharp
// Domain/Entities/InventoryItem.cs
namespace PhanMemKeToan.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    public string ItemCode { get; set; } = string.Empty;         // max 25, unique per tenant
    public string ItemName { get; set; } = string.Empty;         // max 255, required
    public string? ItemNameEnglish { get; set; }                  // max 255
    public string? Description { get; set; }                     // text
    public string? Barcode { get; set; }                         // max 255 (legacy single barcode — quick lookup; Gap P adds multi-barcode table)

    public Guid UnitId { get; set; }                             // FK → Unit.Id, required
    public Guid? CategoryId { get; set; }                        // FK → InventoryItemCategory.Id, nullable

    public CostingMethod CostingMethod { get; set; } = CostingMethod.WeightedAverage;
    /// <summary>Item type — drives GL account and stock movement logic. (BR-A)</summary>
    public InventoryItemType ItemType { get; set; } = InventoryItemType.Goods;
    /// <summary>Default VAT rate prefilled on SA/PU voucher lines. Nullable = chưa set. (BR-D)</summary>
    public decimal? DefaultTaxRate { get; set; }                 // numeric(5,2) — e.g. 10.00 = 10%, null = không set
    /// <summary>Giá mua / giá vốn mặc định. Prefill vào đơn giá khi tạo phiếu mua. (Gap 4 — Inhongha: UnitPrice decimal)</summary>
    public decimal? UnitPrice { get; set; }                      // numeric(18,2) — null = chưa thiết lập

    // Stock settings (FR-IN-004)
    public decimal MinStockLevel { get; set; }                   // decimal(18,2)
    public decimal MaxStockLevel { get; set; }                   // decimal(18,2)
    public int LeadTimeDays { get; set; }

    // Serial / Lot / Expiry tracking flags (Gap K, L — Inhongha IsFollowSerial, LotNo, ExpiryDate on voucher lines)
    public bool IsFollowSerial { get; set; } = false;            // Theo dõi số sê-ri — kích hoạt SerialNumber1/2 trên dòng chứng từ
    public bool IsFollowLot { get; set; } = false;               // Theo dõi số lô/batch — kích hoạt LotNo trên dòng chứng từ
    public bool IsFollowExpiry { get; set; } = false;            // Theo dõi hạn sử dụng — kích hoạt ExpiryDate trên dòng chứng từ

    // Panel / Dimension item (Gap M — ngành kính/vải/tôn/gỗ: tính theo chiều cao×rộng×dài)
    public bool IsPanelItem { get; set; } = false;               // Có tính số lượng theo kích thước tấm không
    public Guid? PanelUnitId { get; set; }                       // FK → Unit (đơn vị bề mặt, e.g., m2 / m)

    // BOM — Bill of Materials template for FinishedProduct items (Gap J — Inhongha BaseOnFormula)
    public Guid? FormulaTemplateId { get; set; }                 // FK → InventoryQuantityFormulaTemplate (null = không có định mức)

    // Sale prices (Inhongha SalePrice1/2/3 — 3 mức giá bán)
    public decimal? SalePrice1 { get; set; }                     // numeric(18,2) — giá bán mức 1 (mặc định)
    public decimal? SalePrice2 { get; set; }                     // numeric(18,2) — giá bán mức 2
    public decimal? SalePrice3 { get; set; }                     // numeric(18,2) — giá bán mức 3

    public bool IsActive { get; set; } = true;
    public int RowVersion { get; set; }

    // Navigation
    // NOTE: Dual FK → Unit (UnitId + PanelUnitId) requires EXPLICIT EF config — see Section 6 InventoryItemConfiguration
    public Unit Unit { get; set; } = null!;
    public Unit? PanelUnit { get; set; }                         // navigation PanelUnitId
    public InventoryItemCategory? Category { get; set; }
    public InventoryQuantityFormulaTemplate? FormulaTemplate { get; set; }
    public ICollection<InventoryItemUnitConvert> UnitConverts { get; set; } = [];   // Gap I — đa đơn vị tính
    public ICollection<InventoryItemBarcode> Barcodes { get; set; } = [];           // Gap P — multi barcode/QR
    public ICollection<InventoryItemAttribute> ItemAttributes { get; set; } = [];   // Gap N — thuộc tính tùy chỉnh
}
```

### C# Category Entity

```csharp
// Domain/Entities/InventoryItemCategory.cs
namespace PhanMemKeToan.Domain.Entities;

public class InventoryItemCategory : AuditableEntity
{
    public string CategoryCode { get; set; } = string.Empty;    // max 25, unique per tenant
    public string CategoryName { get; set; } = string.Empty;    // max 255, required
    public Guid? ParentId { get; set; }                          // FK → self, null = root
    public int Level { get; set; } = 1;                          // 1-5, computed on write

    public bool IsActive { get; set; } = true;                   // Gap 1 — đồng bộ với các lookup khác (BR-C)
    public int SortOrder { get; set; }                           // M1 — thứ tự hiển thị

    // Navigation
    public InventoryItemCategory? Parent { get; set; }
    public ICollection<InventoryItemCategory> Children { get; set; } = [];
    public ICollection<InventoryItem> Items { get; set; } = [];
}
```

---

### C# Child Entity — InventoryItemUnitConvert (Đơn vị tính phụ / Quy đổi)

```csharp
// Domain/Entities/InventoryItemUnitConvert.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Gap I — Multi-unit conversion. Mirrors Inhongha InventoryItemUnitConvert.
/// Cho phép 1 item có nhiều đơn vị tính (e.g., Cái=đơn vị chính, Hộp×12Cái, Thùng×144Cái).
/// Trên dòng chứng từ: UnitID=giao dịch, MainUnitID=đơn vị chính, ConvertRate=tỷ lệ.</summary>
public class InventoryItemUnitConvert : AuditableEntity
{
    public Guid InventoryItemId { get; set; }                    // FK → InventoryItem
    public Guid UnitId { get; set; }                             // FK → Unit (đơn vị phụ)
    /// <summary>Số lượng đơn vị chính tương đương 1 đơn vị phụ này.
    /// VD: 1 Hộp = 12 Cái → ConvertRate = 12.</summary>
    public decimal ConvertRate { get; set; }                     // numeric(18,6) — hỗ trợ tỷ lệ thập phân
    public bool IsDefaultSaleUnit { get; set; } = false;         // Đơn vị mặc định khi tạo phiếu bán
    public bool IsDefaultPurchaseUnit { get; set; } = false;     // Đơn vị mặc định khi tạo phiếu mua
    public int SortOrder { get; set; }

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}
```

---

### C# Entity — InventoryQuantityFormulaTemplate (Mẫu định mức nguyên vật liệu — BOM)

```csharp
// Domain/Entities/InventoryQuantityFormulaTemplate.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Gap J — BOM master template. Mirrors Inhongha InventoryQuantityFormulaTemplate.
/// FK từ InventoryItem.FormulaTemplateId (BaseOnFormula trong Inhongha).
/// Dùng cho thành phẩm (ItemType=FinishedProduct) để định nghĩa định mức NVL chuẩn.
/// Khi tạo lệnh sản xuất, hệ thống copy định mức này vào INProductionOrderDetail.</summary>
public class InventoryQuantityFormulaTemplate : AuditableEntity
{
    public string FormulaCode { get; set; } = string.Empty;      // max 25, unique per tenant
    public string FormulaName { get; set; } = string.Empty;      // max 255
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryQuantityFormulaDetail> Details { get; set; } = [];
    public ICollection<InventoryItem> InventoryItems { get; set; } = [];
}
```

```csharp
// Domain/Entities/InventoryQuantityFormulaDetail.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Dòng định mức NVL trong BOM template. Mỗi dòng = 1 loại vật liệu + số lượng cần.</summary>
public class InventoryQuantityFormulaDetail : AuditableEntity
{
    public Guid FormulaTemplateId { get; set; }                  // FK → InventoryQuantityFormulaTemplate
    public Guid MaterialItemId { get; set; }                     // FK → InventoryItem (vật liệu/NVL)
    public Guid UnitId { get; set; }                             // FK → Unit
    public decimal Quantity { get; set; }                        // numeric(18,6) — số lượng cần cho 1 đơn vị thành phẩm
    public string? Description { get; set; }                     // max 500
    public int SortOrder { get; set; }

    // Navigation
    public InventoryQuantityFormulaTemplate FormulaTemplate { get; set; } = null!;
    public InventoryItem MaterialItem { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}
```

---

### C# Entity — InventoryItemBarcode (Mã vạch / QR Code)

```csharp
// Domain/Entities/InventoryItemBarcode.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Gap P — Multi-barcode/QR per item. Inhongha chỉ có 1 Barcode field trên InventoryItem.
/// Hệ thống mới hỗ trợ nhiều mã per item, mỗi mã có thể gắn theo đơn vị tính cụ thể
/// (VD: barcode Cái khác barcode Hộp). Hỗ trợ EAN13, Code128, QR Code, DataMatrix.</summary>
public class InventoryItemBarcode : AuditableEntity
{
    public Guid InventoryItemId { get; set; }                    // FK → InventoryItem
    public Guid? UnitId { get; set; }                            // FK → Unit (null = áp dụng cho đơn vị chính)
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
    public string BarcodeValue { get; set; } = string.Empty;     // max 255
    public bool IsPrimary { get; set; } = false;                 // Mã vạch chính dùng khi scan

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Unit? Unit { get; set; }
}
```

---

### C# Entity — ItemAttributeType + InventoryItemAttribute (Thuộc tính hàng hóa)

```csharp
// Domain/Entities/ItemAttributeType.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Gap N — Custom attribute type. Mirrors Inhongha ListItem (27 rows).
/// Định nghĩa loại thuộc tính (VD: "Thương hiệu", "Màu sắc", "Kích thước", "Xuất xứ").
/// Được dùng kết hợp với InventoryItemAttribute để lưu giá trị thuộc tính per-item.</summary>
public class ItemAttributeType : AuditableEntity
{
    public string AttributeCode { get; set; } = string.Empty;   // max 25, unique per tenant
    public string AttributeName { get; set; } = string.Empty;   // max 100 (e.g., "Thương hiệu")
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InventoryItemAttribute> ItemAttributes { get; set; } = [];
}
```

```csharp
// Domain/Entities/InventoryItemAttribute.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Giá trị thuộc tính của 1 item cụ thể.
/// VD: TV Samsung 65" → Thương hiệu="Samsung", Kích thước="65 inch", Màu="Đen".</summary>
public class InventoryItemAttribute : AuditableEntity
{
    public Guid InventoryItemId { get; set; }                    // FK → InventoryItem
    public Guid AttributeTypeId { get; set; }                    // FK → ItemAttributeType
    /// <summary>Giá trị thuộc tính dạng text tự do. VD: "Sony", "65 inch", "Đen".</summary>
    public string AttributeValue { get; set; } = string.Empty;  // max 255
    public int SortOrder { get; set; }

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public ItemAttributeType AttributeType { get; set; } = null!;
}
```

---

## 2.5. Opening Stock Balance Entity (Gap 5 — InventoryItemOpeningBalance)

> **Research finding (Inhongha/MISA ACT2)**:  
> Inhongha models opening stock as a dedicated SYSRefType `OpeningInventoryEntry` — a voucher that posts directly to `InventoryLedger` via `Func_POST_OpeningInventoryEntryInventoryLedger`. There is **no separate master data table** for opening stock balance; it lives in the voucher tables.
>
> **Design Decision DD-006** for this system:  
> Add a flat `InventoryItemOpeningBalance` entity in the **DI module setup wizard** so accountants can enter opening stock quantities/values without needing the full IN module voucher infrastructure.  
> When the IN module is built (future sprint), a batch job will convert these rows into proper `INInward` vouchers (RefType=OpeningInventoryEntry) that post to `InventoryLedger` and reconcile stock.  
> This is the same pattern used for `AccountObjectOpeningBalance` (AR/AP opening balances → later posted to `GeneralLedger`).

### C# Entity

```csharp
// Domain/Entities/InventoryItemOpeningBalance.cs
namespace PhanMemKeToan.Domain.Entities;

/// <summary>Số dư tồn kho đầu kỳ theo từng kho.
/// Nhập liệu ở màn hình thiết lập DI; sẽ được chuyển đổi thành phiếu INInward
/// (RefType=OpeningInventoryEntry) khi module IN được triển khai.</summary>
public class InventoryItemOpeningBalance : AuditableEntity
{
    public Guid InventoryItemId { get; set; }                    // FK → InventoryItem
    public Guid WarehouseId { get; set; }                        // FK → Warehouse
    public Guid UnitId { get; set; }                             // FK → Unit (đơn vị tính khi nhập số dư)
    /// <summary>Số lượng tồn đầu kỳ (theo đơn vị UnitId).</summary>
    public decimal Quantity { get; set; }                        // numeric(18,4)
    /// <summary>Đơn giá vốn (giá nhập / giá thành). Dùng tính trị giá kho.</summary>
    public decimal UnitCost { get; set; }                        // numeric(18,6)
    /// <summary>Trị giá tồn kho = Quantity × UnitCost (quy đổi về VND).</summary>
    public decimal Amount { get; set; }                          // numeric(18,2) — tính tự động
    public Guid? CurrencyId { get; set; }                        // FK → Currency (null = VND)
    /// <summary>Số tiền theo nguyên tệ (null nếu tiền VND).</summary>
    public decimal? ForeignAmount { get; set; }                  // numeric(18,2)
    public decimal? ExchangeRate { get; set; }                   // numeric(18,6)
    /// <summary>Ngày số dư đầu kỳ (thường = ngày đầu năm tài chính).</summary>
    public DateTime OpeningDate { get; set; }

    // Navigation
    public InventoryItem InventoryItem { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public Currency? Currency { get; set; }
}
```

### PostgreSQL DDL

```sql
CREATE TABLE "inventory_item_opening_balances" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "InventoryItemId"   uuid            NOT NULL,
    "WarehouseId"       uuid            NOT NULL,
    "UnitId"            uuid            NOT NULL,
    "Quantity"          numeric(18,4)   NOT NULL DEFAULT 0,
    "UnitCost"          numeric(18,6)   NOT NULL DEFAULT 0,
    "Amount"            numeric(18,2)   NOT NULL DEFAULT 0,
    "CurrencyId"        uuid            NULL,
    "ForeignAmount"     numeric(18,2)   NULL,
    "ExchangeRate"      numeric(18,6)   NULL,
    "OpeningDate"       date            NOT NULL,
    -- Audit (matching AuditableEntity pattern)
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_inventory_item_opening_balances" PRIMARY KEY ("Id")
);

-- 1 kho + 1 item + 1 đơn vị tính chỉ có 1 row số dư đầu kỳ
CREATE UNIQUE INDEX "UX_inv_item_opening_bal_item_wh_unit"
    ON "inventory_item_opening_balances" ("TenantId", "InventoryItemId", "WarehouseId", "UnitId")
    WHERE "IsDeleted" = false;

ALTER TABLE "inventory_item_opening_balances"
    ADD CONSTRAINT "FK_inv_item_ob_item"      FOREIGN KEY ("InventoryItemId") REFERENCES "inventory_items"("Id")  ON DELETE CASCADE,
    ADD CONSTRAINT "FK_inv_item_ob_warehouse" FOREIGN KEY ("WarehouseId")     REFERENCES "warehouses"("Id")       ON DELETE RESTRICT,
    ADD CONSTRAINT "FK_inv_item_ob_unit"      FOREIGN KEY ("UnitId")          REFERENCES "units"("Id")            ON DELETE RESTRICT,
    ADD CONSTRAINT "FK_inv_item_ob_currency"  FOREIGN KEY ("CurrencyId")      REFERENCES "currencies"("Id")       ON DELETE SET NULL;
```

### EF Core Configuration

```csharp
public class InventoryItemOpeningBalanceConfiguration : IEntityTypeConfiguration<InventoryItemOpeningBalance>
{
    public void Configure(EntityTypeBuilder<InventoryItemOpeningBalance> builder)
    {
        builder.ToTable("inventory_item_opening_balances");
        builder.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(e => e.UnitCost).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(e => e.ForeignAmount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.ExchangeRate).HasColumnType("numeric(18,6)");

        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.WarehouseId, e.UnitId }).IsUnique();

        builder.HasOne(e => e.InventoryItem)
               .WithMany()
               .HasForeignKey(e => e.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Warehouse)
               .WithMany()
               .HasForeignKey(e => e.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Currency)
               .WithMany()
               .HasForeignKey(e => e.CurrencyId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## 3. Lookup Entities

### Currency (Loại tiền tệ)

```csharp
// Domain/Entities/Currency.cs
namespace PhanMemKeToan.Domain.Entities;

public class Currency : AuditableEntity
{
    public string CurrencyCode { get; set; } = string.Empty;    // max 10, ISO 4217, unique per tenant
    public string CurrencyName { get; set; } = string.Empty;    // max 100
    public string? CurrencyNameEnglish { get; set; }             // max 100 — Inhongha: CCYNameENG (BR-H)
    public string Symbol { get; set; } = string.Empty;          // max 10 (e.g., "₫", "$")
    public decimal ExchangeRate { get; set; } = 1m;             // numeric(18,2); VND=1
    public bool IsActive { get; set; } = true;                   // (BR-C)

    // Navigation
    public ICollection<AccountObjectOpeningBalance> OpeningBalances { get; set; } = [];
}
```

### Unit (Đơn vị tính)

```csharp
// Domain/Entities/Unit.cs
namespace PhanMemKeToan.Domain.Entities;

public class Unit : AuditableEntity
{
    public string UnitCode { get; set; } = string.Empty;        // max 25, unique per tenant
    public string UnitName { get; set; } = string.Empty;        // max 100
    public bool IsActive { get; set; } = true;                   // (BR-C)

    // Navigation: KHÔNG khai báo ICollection<InventoryItem> tại đây vì Unit được FK tới từ 5 entity khác nhau.
    // Tất cả relationship được configure 1 chiều (WithMany() không có tham số) từ phía child — xem Section 6.
}
```

### Warehouse / Stock (Kho hàng)

```csharp
// Domain/Entities/Warehouse.cs
namespace PhanMemKeToan.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public string WarehouseCode { get; set; } = string.Empty;   // max 25, unique per tenant
    public string WarehouseName { get; set; } = string.Empty;   // max 255
    public string? Address { get; set; }                         // max 500
    public string? ManagerName { get; set; }                     // max 255
    public bool IsActive { get; set; } = true;                   // (BR-C)
}
```

### Department / OrganizationUnit (Bộ phận / Phòng ban)

```csharp
// Domain/Entities/Department.cs
namespace PhanMemKeToan.Domain.Entities;

public class Department : AuditableEntity
{
    public string DeptCode { get; set; } = string.Empty;        // max 25, unique per tenant
    public string DeptName { get; set; } = string.Empty;        // max 255, required
    public Guid? ParentId { get; set; }                          // FK → self, null = root
    public int Level { get; set; } = 1;                          // 1-5, computed on write
    public bool IsActive { get; set; } = true;                   // (BR-C)

    // Navigation
    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = [];
}
```

### ExpenseItem (Khoản mục chi phí)

```csharp
// Domain/Entities/ExpenseItem.cs
namespace PhanMemKeToan.Domain.Entities;

public class ExpenseItem : AuditableEntity
{
    public string ExpenseCode { get; set; } = string.Empty;     // max 25, unique per tenant
    public string ExpenseName { get; set; } = string.Empty;     // max 255
    /// <summary>References Account.AccountNumber (no FK constraint — cross-entity reference by code)</summary>
    public string? AccountCode { get; set; }                     // max 20
}
```

---

## 4. PostgreSQL Tables

### `account_objects`

```sql
CREATE TABLE "account_objects" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "ObjectCode"        varchar(25)     NOT NULL,
    "ObjectName"        varchar(255)    NOT NULL,
    "ObjectNameEnglish" varchar(255)    NULL,
    "Address"           varchar(500)    NULL,
    "TaxCode"           varchar(50)     NULL,                    -- BR-B: nvarchar(50) trong Inhongha
    "Email"             varchar(255)    NULL,
    "Phone"             varchar(50)     NULL,
    "Fax"               varchar(50)     NULL,
    "Website"           varchar(255)    NULL,
    "ContactPerson"     varchar(255)    NULL,
    "ContactPhone"      varchar(50)     NULL,
    "Description"       text            NULL,
    "ObjectType"        int             NOT NULL DEFAULT 0,
    "CreditLimit"       numeric(18,2)   NOT NULL DEFAULT 0,      -- BR-F: 2 decimals cho ngoại tệ
    "PaymentTermDays"   int             NOT NULL DEFAULT 0,
    "IsActive"          boolean         NOT NULL DEFAULT true,
    "AccountObjectGroupId" uuid         NULL,                    -- BR-E: nullable FK, CRUD deferred
    "RowVersion"        int             NOT NULL DEFAULT 0,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_account_objects" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_account_objects_account_object_groups"
        FOREIGN KEY ("AccountObjectGroupId") REFERENCES "account_object_groups" ("Id") ON DELETE SET NULL
);

-- Unique code per tenant (active only)
CREATE UNIQUE INDEX "UIX_account_objects_TenantId_ObjectCode"
    ON "account_objects" ("TenantId", "ObjectCode") WHERE "IsDeleted" = false;

-- Full-text search on ObjectName (GIN)
CREATE INDEX "IX_account_objects_ObjectName_GIN"
    ON "account_objects" USING gin (to_tsvector('simple', "ObjectName"));

-- Search by code prefix
CREATE INDEX "IX_account_objects_ObjectCode_Pattern"
    ON "account_objects" ("ObjectCode" text_pattern_ops);

-- Bitmask type filter (partial index for common queries)
CREATE INDEX "IX_account_objects_TenantId_ObjectType"
    ON "account_objects" ("TenantId", "ObjectType") WHERE "IsDeleted" = false;

CREATE INDEX "IX_account_objects_AccountObjectGroupId"
    ON "account_objects" ("AccountObjectGroupId") WHERE "AccountObjectGroupId" IS NOT NULL;
```

### `account_object_groups`

```sql
CREATE TABLE "account_object_groups" (
    "Id"            uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      uuid            NOT NULL,
    "GroupCode"     varchar(25)     NOT NULL,
    "GroupName"     varchar(255)    NOT NULL,
    "ObjectType"    int             NOT NULL DEFAULT 0,          -- bitmask: 1=Customer, 2=Vendor, 4=Employee
    "IsActive"      boolean         NOT NULL DEFAULT true,
    "IsDeleted"     boolean         NOT NULL DEFAULT false,
    "CreatedAt"     timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"     varchar(256)    NULL,
    "ModifiedAt"    timestamptz     NULL,
    "ModifiedBy"    varchar(256)    NULL,
    CONSTRAINT "PK_account_object_groups" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_account_object_groups_TenantId_GroupCode"
    ON "account_object_groups" ("TenantId", "GroupCode") WHERE "IsDeleted" = false;
```

### `account_object_bank_accounts`

```sql
CREATE TABLE "account_object_bank_accounts" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "AccountObjectId"   uuid            NOT NULL,
    "BankName"          varchar(255)    NOT NULL,
    "BankBranch"        varchar(255)    NULL,
    "AccountNumber"     varchar(50)     NOT NULL,
    "SwiftCode"         varchar(20)     NULL,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_account_object_bank_accounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_account_object_bank_accounts_account_objects"
        FOREIGN KEY ("AccountObjectId") REFERENCES "account_objects" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_account_object_bank_accounts_AccountObjectId"
    ON "account_object_bank_accounts" ("AccountObjectId");
```

### `account_object_opening_balances`

```sql
CREATE TABLE "account_object_opening_balances" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "AccountObjectId"   uuid            NOT NULL,
    "CurrencyId"        uuid            NOT NULL,
    "DebitAmount"       numeric(18,0)   NOT NULL DEFAULT 0,      -- VND equivalent
    "CreditAmount"      numeric(18,0)   NOT NULL DEFAULT 0,      -- VND equivalent
    "DebitAmountOC"     numeric(18,3)   NOT NULL DEFAULT 0,      -- BR-G: nguyên tệ Nợ (0 nếu VND)
    "CreditAmountOC"    numeric(18,3)   NOT NULL DEFAULT 0,      -- BR-G: nguyên tệ Có (0 nếu VND)
    "ExchangeRate"      numeric(18,2)   NOT NULL DEFAULT 1,      -- BR-G: tỷ giá tại thời điểm nhập; 1 với VND
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_account_object_opening_balances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_account_object_opening_balances_account_objects"
        FOREIGN KEY ("AccountObjectId") REFERENCES "account_objects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_account_object_opening_balances_currencies"
        FOREIGN KEY ("CurrencyId") REFERENCES "currencies" ("Id") ON DELETE RESTRICT
);

-- One opening balance row per object per currency per tenant
CREATE UNIQUE INDEX "UIX_account_object_opening_balances_object_currency"
    ON "account_object_opening_balances" ("AccountObjectId", "CurrencyId")
    WHERE "IsDeleted" = false;
```

### `inventory_items`

```sql
CREATE TABLE "inventory_items" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "ItemCode"          varchar(25)     NOT NULL,
    "ItemName"          varchar(255)    NOT NULL,
    "ItemNameEnglish"   varchar(255)    NULL,
    "Description"       text            NULL,
    "Barcode"           varchar(255)    NULL,                    -- M3 fix: tăng từ 100 → 255 khứp với inventory_item_barcodes.BarcodeValue
    "UnitId"            uuid            NOT NULL,
    "CategoryId"        uuid            NULL,
    "CostingMethod"     int             NOT NULL DEFAULT 3,
    "ItemType"          int             NOT NULL DEFAULT 2,       -- BR-A: 0=RawMaterial,1=Finished,2=Goods,3=Service
    "DefaultTaxRate"    numeric(5,2)    NULL,                    -- BR-D: prefilled trên dòng SA/PU
    "UnitPrice"         numeric(18,2)   NULL,                    -- Gap 4: giá mua/vốn mặc định (Inhongha: UnitPrice)
    "MinStockLevel"     numeric(18,2)   NOT NULL DEFAULT 0,
    "MaxStockLevel"     numeric(18,2)   NOT NULL DEFAULT 0,
    "LeadTimeDays"      int             NOT NULL DEFAULT 0,
    -- Gap K/L: Serial / Lot / Expiry tracking flags (Inhongha: IsFollowSerial bit, LotNo/ExpiryDate on voucher)
    "IsFollowSerial"    boolean         NOT NULL DEFAULT false,
    "IsFollowLot"       boolean         NOT NULL DEFAULT false,
    "IsFollowExpiry"    boolean         NOT NULL DEFAULT false,
    -- Gap M: Panel/Dimension items (ngành kính/vải/tôn: PanelQuantity×Height×Width×Length)
    "IsPanelItem"       boolean         NOT NULL DEFAULT false,
    "PanelUnitId"       uuid            NULL,                    -- FK → units (đơn vị bề mặt/chiều dài)
    -- Gap J: BOM template for FinishedProduct (Inhongha: BaseOnFormula)
    "FormulaTemplateId" uuid            NULL,                    -- FK → inventory_quantity_formula_templates
    -- Giá bán 3 mức (Inhongha: SalePrice1/2/3)
    "SalePrice1"        numeric(18,2)   NULL,
    "SalePrice2"        numeric(18,2)   NULL,
    "SalePrice3"        numeric(18,2)   NULL,
    "IsActive"          boolean         NOT NULL DEFAULT true,
    "RowVersion"        int             NOT NULL DEFAULT 0,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_inventory_items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_items_units"
        FOREIGN KEY ("UnitId") REFERENCES "units" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_items_inventory_item_categories"
        FOREIGN KEY ("CategoryId") REFERENCES "inventory_item_categories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_inventory_items_panel_units"
        FOREIGN KEY ("PanelUnitId") REFERENCES "units" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_inventory_items_formula_templates"
        FOREIGN KEY ("FormulaTemplateId") REFERENCES "inventory_quantity_formula_templates" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "UIX_inventory_items_TenantId_ItemCode"
    ON "inventory_items" ("TenantId", "ItemCode") WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_items_TenantId_CategoryId"
    ON "inventory_items" ("TenantId", "CategoryId") WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_items_ItemName_GIN"
    ON "inventory_items" USING gin (to_tsvector('simple', "ItemName"));

CREATE INDEX "IX_inventory_items_ItemCode_Pattern"
    ON "inventory_items" ("ItemCode" text_pattern_ops);

-- Index for barcode lookup (single-barcode legacy field)
CREATE INDEX "IX_inventory_items_Barcode"
    ON "inventory_items" ("Barcode") WHERE "Barcode" IS NOT NULL;

-- Index for formula template lookup
CREATE INDEX "IX_inventory_items_FormulaTemplateId"
    ON "inventory_items" ("FormulaTemplateId") WHERE "FormulaTemplateId" IS NOT NULL;
```

### `inventory_item_categories`

```sql
CREATE TABLE "inventory_item_categories" (
    "Id"            uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      uuid            NOT NULL,
    "CategoryCode"  varchar(25)     NOT NULL,
    "CategoryName"  varchar(255)    NOT NULL,
    "ParentId"      uuid            NULL,
    "Level"         int             NOT NULL DEFAULT 1,
    "IsActive"      boolean         NOT NULL DEFAULT true,       -- Gap 1: đồng bộ với các lookup khác
    "SortOrder"     int             NOT NULL DEFAULT 0,          -- M1: thứ tự hiển thị
    "IsDeleted"     boolean         NOT NULL DEFAULT false,
    "CreatedAt"     timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"     varchar(256)    NULL,
    "ModifiedAt"    timestamptz     NULL,
    "ModifiedBy"    varchar(256)    NULL,
    CONSTRAINT "PK_inventory_item_categories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_item_categories_parent"
        FOREIGN KEY ("ParentId") REFERENCES "inventory_item_categories" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "UIX_inventory_item_categories_TenantId_CategoryCode"
    ON "inventory_item_categories" ("TenantId", "CategoryCode") WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_item_categories_ParentId"
    ON "inventory_item_categories" ("ParentId");

CREATE INDEX "IX_inventory_item_categories_TenantId"
    ON "inventory_item_categories" ("TenantId");
```

### `inventory_item_unit_converts` (Gap I — Đơn vị tính phụ)

```sql
CREATE TABLE "inventory_item_unit_converts" (
    "Id"                    uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"              uuid            NOT NULL,
    "InventoryItemId"       uuid            NOT NULL,
    "UnitId"                uuid            NOT NULL,            -- đơn vị phụ (e.g., Hộp, Thùng)
    "ConvertRate"           numeric(18,6)   NOT NULL DEFAULT 1,  -- 1 đơn vị phụ = ConvertRate đơn vị chính
    "IsDefaultSaleUnit"     boolean         NOT NULL DEFAULT false,
    "IsDefaultPurchaseUnit" boolean         NOT NULL DEFAULT false,
    "SortOrder"             int             NOT NULL DEFAULT 0,
    "IsDeleted"             boolean         NOT NULL DEFAULT false,
    "CreatedAt"             timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"             varchar(256)    NULL,
    "ModifiedAt"            timestamptz     NULL,
    "ModifiedBy"            varchar(256)    NULL,
    CONSTRAINT "PK_inventory_item_unit_converts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_item_unit_converts_inventory_items"
        FOREIGN KEY ("InventoryItemId") REFERENCES "inventory_items" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_inventory_item_unit_converts_units"
        FOREIGN KEY ("UnitId") REFERENCES "units" ("Id") ON DELETE RESTRICT
);

-- Unique: 1 item không thể có 2 dòng cùng UnitId
CREATE UNIQUE INDEX "UIX_inventory_item_unit_converts_item_unit"
    ON "inventory_item_unit_converts" ("InventoryItemId", "UnitId")
    WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_item_unit_converts_InventoryItemId"
    ON "inventory_item_unit_converts" ("InventoryItemId");
```

### `inventory_quantity_formula_templates` (Gap J — BOM Master Template)

```sql
CREATE TABLE "inventory_quantity_formula_templates" (
    "Id"            uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      uuid            NOT NULL,
    "FormulaCode"   varchar(25)     NOT NULL,
    "FormulaName"   varchar(255)    NOT NULL,
    "IsActive"      boolean         NOT NULL DEFAULT true,
    "IsDeleted"     boolean         NOT NULL DEFAULT false,
    "CreatedAt"     timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"     varchar(256)    NULL,
    "ModifiedAt"    timestamptz     NULL,
    "ModifiedBy"    varchar(256)    NULL,
    CONSTRAINT "PK_inventory_quantity_formula_templates" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_inventory_quantity_formula_templates_TenantId_FormulaCode"
    ON "inventory_quantity_formula_templates" ("TenantId", "FormulaCode")
    WHERE "IsDeleted" = false;
```

### `inventory_quantity_formula_details` (Gap J — BOM Detail Lines)

```sql
CREATE TABLE "inventory_quantity_formula_details" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "FormulaTemplateId" uuid            NOT NULL,
    "MaterialItemId"    uuid            NOT NULL,               -- FK → inventory_items (NVL)
    "UnitId"            uuid            NOT NULL,
    "Quantity"          numeric(18,6)   NOT NULL DEFAULT 1,      -- số lượng NVL cho 1 đơn vị thành phẩm
    "Description"       varchar(500)    NULL,
    "SortOrder"         int             NOT NULL DEFAULT 0,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_inventory_quantity_formula_details" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_quantity_formula_details_templates"
        FOREIGN KEY ("FormulaTemplateId") REFERENCES "inventory_quantity_formula_templates" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_inventory_quantity_formula_details_material_items"
        FOREIGN KEY ("MaterialItemId") REFERENCES "inventory_items" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_inventory_quantity_formula_details_units"
        FOREIGN KEY ("UnitId") REFERENCES "units" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_inventory_quantity_formula_details_FormulaTemplateId"
    ON "inventory_quantity_formula_details" ("FormulaTemplateId");
```

### `inventory_item_barcodes` (Gap P — Multi Barcode/QR)

```sql
CREATE TABLE "inventory_item_barcodes" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "InventoryItemId"   uuid            NOT NULL,
    "UnitId"            uuid            NULL,                   -- NULL = đơn vị chính của item
    "BarcodeType"       int             NOT NULL DEFAULT 0,     -- 0=Code128,1=EAN13,2=EAN8,3=QRCode,4=DataMatrix,5=UPC_A
    "BarcodeValue"      varchar(255)    NOT NULL,
    "IsPrimary"         boolean         NOT NULL DEFAULT false,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_inventory_item_barcodes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_item_barcodes_inventory_items"
        FOREIGN KEY ("InventoryItemId") REFERENCES "inventory_items" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_inventory_item_barcodes_units"
        FOREIGN KEY ("UnitId") REFERENCES "units" ("Id") ON DELETE SET NULL
);

-- Unique: mỗi mã vạch chỉ được gắn 1 item (cross-item barcode = lỗi nghiệp vụ)
CREATE UNIQUE INDEX "UIX_inventory_item_barcodes_TenantId_BarcodeValue"
    ON "inventory_item_barcodes" ("TenantId", "BarcodeValue")
    WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_item_barcodes_InventoryItemId"
    ON "inventory_item_barcodes" ("InventoryItemId");
```

### `item_attribute_types` (Gap N — Loại thuộc tính)

```sql
CREATE TABLE "item_attribute_types" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "AttributeCode"     varchar(25)     NOT NULL,
    "AttributeName"     varchar(100)    NOT NULL,               -- e.g., "Thương hiệu", "Màu sắc"
    "SortOrder"         int             NOT NULL DEFAULT 0,
    "IsActive"          boolean         NOT NULL DEFAULT true,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_item_attribute_types" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_item_attribute_types_TenantId_AttributeCode"
    ON "item_attribute_types" ("TenantId", "AttributeCode")
    WHERE "IsDeleted" = false;
```

### `inventory_item_attributes` (Gap N — Giá trị thuộc tính per item)

```sql
CREATE TABLE "inventory_item_attributes" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "InventoryItemId"   uuid            NOT NULL,
    "AttributeTypeId"   uuid            NOT NULL,
    "AttributeValue"    varchar(255)    NOT NULL,               -- e.g., "Sony", "65 inch", "Đen"
    "SortOrder"         int             NOT NULL DEFAULT 0,
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_inventory_item_attributes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_inventory_item_attributes_inventory_items"
        FOREIGN KEY ("InventoryItemId") REFERENCES "inventory_items" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_inventory_item_attributes_attribute_types"
        FOREIGN KEY ("AttributeTypeId") REFERENCES "item_attribute_types" ("Id") ON DELETE RESTRICT
);

-- Unique: 1 item chỉ có 1 giá trị cho mỗi loại thuộc tính
CREATE UNIQUE INDEX "UIX_inventory_item_attributes_item_type"
    ON "inventory_item_attributes" ("InventoryItemId", "AttributeTypeId")
    WHERE "IsDeleted" = false;

CREATE INDEX "IX_inventory_item_attributes_InventoryItemId"
    ON "inventory_item_attributes" ("InventoryItemId");
```

### `currencies`

```sql
CREATE TABLE "currencies" (
    "Id"            uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      uuid            NOT NULL,
    "CurrencyCode"          varchar(10)     NOT NULL,
    "CurrencyName"          varchar(100)    NOT NULL,
    "CurrencyNameEnglish"   varchar(100)    NULL,                -- BR-H: Inhongha CCYNameENG
    "Symbol"                varchar(10)     NOT NULL,
    "ExchangeRate"          numeric(18,2)   NOT NULL DEFAULT 1,
    "IsActive"              boolean         NOT NULL DEFAULT true, -- BR-C
    "IsDeleted"             boolean         NOT NULL DEFAULT false,
    "CreatedAt"     timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"     varchar(256)    NULL,
    "ModifiedAt"    timestamptz     NULL,
    "ModifiedBy"    varchar(256)    NULL,
    CONSTRAINT "PK_currencies" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_currencies_TenantId_CurrencyCode"
    ON "currencies" ("TenantId", "CurrencyCode") WHERE "IsDeleted" = false;
```

### `units`

```sql
CREATE TABLE "units" (
    "Id"        uuid        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"  uuid        NOT NULL,
    "UnitCode"  varchar(25)  NOT NULL,
    "UnitName"  varchar(100) NOT NULL,
    "IsActive"  boolean      NOT NULL DEFAULT true,              -- BR-C
    "IsDeleted" boolean      NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" varchar(256) NULL,
    "ModifiedAt" timestamptz NULL,
    "ModifiedBy" varchar(256) NULL,
    CONSTRAINT "PK_units" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_units_TenantId_UnitCode"
    ON "units" ("TenantId", "UnitCode") WHERE "IsDeleted" = false;
```

### `warehouses`

```sql
CREATE TABLE "warehouses" (
    "Id"                uuid            NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"          uuid            NOT NULL,
    "WarehouseCode"     varchar(25)     NOT NULL,
    "WarehouseName"     varchar(255)    NOT NULL,
    "Address"           varchar(500)    NULL,
    "ManagerName"       varchar(255)    NULL,
    "IsActive"          boolean         NOT NULL DEFAULT true,   -- BR-C
    "IsDeleted"         boolean         NOT NULL DEFAULT false,
    "CreatedAt"         timestamptz     NOT NULL DEFAULT now(),
    "CreatedBy"         varchar(256)    NULL,
    "ModifiedAt"        timestamptz     NULL,
    "ModifiedBy"        varchar(256)    NULL,
    CONSTRAINT "PK_warehouses" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_warehouses_TenantId_WarehouseCode"
    ON "warehouses" ("TenantId", "WarehouseCode") WHERE "IsDeleted" = false;
```

### `departments`

```sql
CREATE TABLE "departments" (
    "Id"        uuid        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"  uuid        NOT NULL,
    "DeptCode"  varchar(25)  NOT NULL,
    "DeptName"  varchar(255) NOT NULL,
    "ParentId"  uuid         NULL,
    "Level"     int          NOT NULL DEFAULT 1,
    "IsActive"  boolean      NOT NULL DEFAULT true,              -- BR-C
    "IsDeleted" boolean      NOT NULL DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" varchar(256) NULL,
    "ModifiedAt" timestamptz NULL,
    "ModifiedBy" varchar(256) NULL,
    CONSTRAINT "PK_departments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_departments_parent"
        FOREIGN KEY ("ParentId") REFERENCES "departments" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "UIX_departments_TenantId_DeptCode"
    ON "departments" ("TenantId", "DeptCode") WHERE "IsDeleted" = false;

CREATE INDEX "IX_departments_ParentId"
    ON "departments" ("ParentId");
```

### `expense_items`

```sql
CREATE TABLE "expense_items" (
    "Id"            uuid        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      uuid        NOT NULL,
    "ExpenseCode"   varchar(25) NOT NULL,
    "ExpenseName"   varchar(255) NOT NULL,
    "AccountCode"   varchar(20) NULL,
    "IsDeleted"     boolean     NOT NULL DEFAULT false,
    "CreatedAt"     timestamptz NOT NULL DEFAULT now(),
    "CreatedBy"     varchar(256) NULL,
    "ModifiedAt"    timestamptz NULL,
    "ModifiedBy"    varchar(256) NULL,
    CONSTRAINT "PK_expense_items" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "UIX_expense_items_TenantId_ExpenseCode"
    ON "expense_items" ("TenantId", "ExpenseCode") WHERE "IsDeleted" = false;
```

---

## 5. Entity Relationships

```
AccountObjectGroup 1──N AccountObject (nullable FK — BR-E, Inhongha: 61 groups)
AccountObject 1──N AccountObjectBankAccount
AccountObject 1──N AccountObjectOpeningBalance N──1 Currency
                   └─ AmountOC (DebitAmountOC/CreditAmountOC) + ExchangeRate (BR-G)

AccountObject 1──0..1 AccountObjectEmployeeProfile (DD-007 — only when Employee bit=4 set)
                   └─ EmployeeProfile N──1 Department (DepartmentId, nullable)

InventoryItemCategory (self-referential tree, max 5 levels)
InventoryItemCategory 1──N InventoryItem
Unit 1──N InventoryItem (MainUnit)
Unit 1──N InventoryItemUnitConvert (UnitId — đơn vị phụ)
Unit 1──N InventoryItem (PanelUnitId, nullable)

InventoryItem 1──N InventoryItemUnitConvert (Gap I — đa đơn vị tính; xoá cascade)
InventoryItem 1──N InventoryItemBarcode     (Gap P — multi barcode/QR; xoá cascade)
InventoryItem 1──N InventoryItemAttribute    (Gap N — thuộc tính tuỳ chỉnh; xoá cascade)
InventoryItem 1──N InventoryItemOpeningBalance (DD-006 — per warehouseble, ON DELETE SET NULL)
  └─ FormulaTemplateId = BaseOnFormula trong Inhongha

InventoryQuantityFormulaTemplate 1──N InventoryQuantityFormulaDetail
InventoryQuantityFormulaDetail N──1 InventoryItem (MaterialItemId, ON DELETE RESTRICT)
InventoryQuantityFormulaDetail N──1 Unit

ItemAttributeType 1──N InventoryItemAttribute

Department (self-referential tree, max 5 levels)

ExpenseItem ──── AccountCode (soft reference by code, no FK constraint)

Entities với IsActive: Currency, Unit, Warehouse, Department, AccountObjectGroup,
             InventoryQuantityFormulaTemplate, ItemAttributeType (BR-C)
```

---

## 6. EF Core Configurations (Key Patterns)

### AccountObjectConfiguration

```csharp
// Infrastructure/Persistence/Configurations/AccountObjectConfiguration.cs
public class AccountObjectConfiguration : IEntityTypeConfiguration<AccountObject>
{
    public void Configure(EntityTypeBuilder<AccountObject> builder)
    {
        builder.ToTable("account_objects");
        builder.Property(e => e.ObjectCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.ObjectName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ObjectNameEnglish).HasMaxLength(255);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.TaxCode).HasMaxLength(50);      // BR-B
        builder.Property(e => e.Email).HasMaxLength(255);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Fax).HasMaxLength(50);
        builder.Property(e => e.Website).HasMaxLength(255);
        builder.Property(e => e.ContactPerson).HasMaxLength(255);
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.Property(e => e.CreditLimit).HasColumnType("numeric(18,2)");    // BR-F

        builder.HasMany(e => e.BankAccounts)
               .WithOne(b => b.AccountObject)
               .HasForeignKey(b => b.AccountObjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OpeningBalances)
               .WithOne(o => o.AccountObject)
               .HasForeignKey(o => o.AccountObjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AccountObjectGroup)   // BR-E
               .WithMany(g => g.AccountObjects)
               .HasForeignKey(e => e.AccountObjectGroupId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### InventoryItemCategory Configuration

```csharp
public class InventoryItemCategoryConfiguration : IEntityTypeConfiguration<InventoryItemCategory>
{
    public void Configure(EntityTypeBuilder<InventoryItemCategory> builder)
    {
        builder.ToTable("inventory_item_categories");
        builder.Property(e => e.CategoryCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.CategoryName).HasMaxLength(255).IsRequired();

        builder.HasOne(e => e.Parent)
               .WithMany(p => p.Children)
               .HasForeignKey(e => e.ParentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### InventoryItemConfiguration (Dual FK — Bug 2 fix)

```csharp
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.Property(e => e.ItemCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.ItemName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ItemNameEnglish).HasMaxLength(255);
        builder.Property(e => e.Barcode).HasMaxLength(255);
        builder.Property(e => e.DefaultTaxRate).HasColumnType("numeric(5,2)");
        builder.Property(e => e.UnitPrice).HasColumnType("numeric(18,2)");       // Gap 4
        builder.Property(e => e.SalePrice1).HasColumnType("numeric(18,2)");
        builder.Property(e => e.SalePrice2).HasColumnType("numeric(18,2)");
        builder.Property(e => e.SalePrice3).HasColumnType("numeric(18,2)");
        builder.Property(e => e.MinStockLevel).HasColumnType("numeric(18,2)");
        builder.Property(e => e.MaxStockLevel).HasColumnType("numeric(18,2)");

        // DUAL FK → Unit — explicit config required to avoid EF Core ambiguity (Bug 2)
        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PanelUnit)
               .WithMany()
               .HasForeignKey(e => e.PanelUnitId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Category)
               .WithMany(c => c.Items)
               .HasForeignKey(e => e.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.FormulaTemplate)
               .WithMany()
               .HasForeignKey(e => e.FormulaTemplateId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.UnitConverts)
               .WithOne(u => u.InventoryItem)
               .HasForeignKey(u => u.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Barcodes)
               .WithOne(b => b.InventoryItem)
               .HasForeignKey(b => b.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ItemAttributes)
               .WithOne(a => a.InventoryItem)
               .HasForeignKey(a => a.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### InventoryItemUnitConvertConfiguration

```csharp
public class InventoryItemUnitConvertConfiguration : IEntityTypeConfiguration<InventoryItemUnitConvert>
{
    public void Configure(EntityTypeBuilder<InventoryItemUnitConvert> builder)
    {
        builder.ToTable("inventory_item_unit_converts");
        builder.Property(e => e.ConvertRate).HasColumnType("numeric(18,6)").IsRequired();

        // Unique: 1 item không có 2 row cùng UnitId
        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.UnitId }).IsUnique();

        builder.HasOne(e => e.InventoryItem)
               .WithMany(i => i.UnitConverts)
               .HasForeignKey(e => e.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### InventoryQuantityFormulaTemplateConfiguration

```csharp
public class InventoryQuantityFormulaTemplateConfiguration : IEntityTypeConfiguration<InventoryQuantityFormulaTemplate>
{
    public void Configure(EntityTypeBuilder<InventoryQuantityFormulaTemplate> builder)
    {
        builder.ToTable("inventory_quantity_formula_templates");
        builder.Property(e => e.FormulaCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.FormulaName).HasMaxLength(255).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.FormulaCode }).IsUnique();

        builder.HasMany(e => e.Details)
               .WithOne(d => d.FormulaTemplate)
               .HasForeignKey(d => d.FormulaTemplateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### InventoryQuantityFormulaDetailConfiguration

```csharp
public class InventoryQuantityFormulaDetailConfiguration : IEntityTypeConfiguration<InventoryQuantityFormulaDetail>
{
    public void Configure(EntityTypeBuilder<InventoryQuantityFormulaDetail> builder)
    {
        builder.ToTable("inventory_quantity_formula_details");
        builder.Property(e => e.Quantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.FormulaTemplate)
               .WithMany(t => t.Details)
               .HasForeignKey(e => e.FormulaTemplateId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MaterialItem)
               .WithMany()
               .HasForeignKey(e => e.MaterialItemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### InventoryItemBarcodeConfiguration

```csharp
public class InventoryItemBarcodeConfiguration : IEntityTypeConfiguration<InventoryItemBarcode>
{
    public void Configure(EntityTypeBuilder<InventoryItemBarcode> builder)
    {
        builder.ToTable("inventory_item_barcodes");
        builder.Property(e => e.BarcodeValue).HasMaxLength(255).IsRequired();

        // Unique barcode value per tenant (global — không cho 2 item cùng barcode)
        builder.HasIndex(e => new { e.TenantId, e.BarcodeValue }).IsUnique();

        builder.HasOne(e => e.InventoryItem)
               .WithMany(i => i.Barcodes)
               .HasForeignKey(e => e.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Unit)
               .WithMany()
               .HasForeignKey(e => e.UnitId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### ItemAttributeTypeConfiguration

```csharp
public class ItemAttributeTypeConfiguration : IEntityTypeConfiguration<ItemAttributeType>
{
    public void Configure(EntityTypeBuilder<ItemAttributeType> builder)
    {
        builder.ToTable("item_attribute_types");
        builder.Property(e => e.AttributeCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.AttributeName).HasMaxLength(100).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.AttributeCode }).IsUnique();
    }
}
```

### InventoryItemAttributeConfiguration

```csharp
public class InventoryItemAttributeConfiguration : IEntityTypeConfiguration<InventoryItemAttribute>
{
    public void Configure(EntityTypeBuilder<InventoryItemAttribute> builder)
    {
        builder.ToTable("inventory_item_attributes");
        builder.Property(e => e.AttributeValue).HasMaxLength(255).IsRequired();

        // Unique: 1 item + 1 attribute type chỉ có 1 row
        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.AttributeTypeId }).IsUnique();

        builder.HasOne(e => e.InventoryItem)
               .WithMany(i => i.ItemAttributes)
               .HasForeignKey(e => e.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AttributeType)
               .WithMany()
               .HasForeignKey(e => e.AttributeTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### ApplicationDbContext additions

```csharp
// Add to ApplicationDbContext:
public DbSet<AccountObject> AccountObjects { get; set; }
public DbSet<AccountObjectGroup> AccountObjectGroups { get; set; }   // BR-E
public DbSet<AccountObjectBankAccount> AccountObjectBankAccounts { get; set; }
public DbSet<AccountObjectOpeningBalance> AccountObjectOpeningBalances { get; set; }
public DbSet<AccountObjectEmployeeProfile> AccountObjectEmployeeProfiles { get; set; }  // DD-007
public DbSet<InventoryItem> InventoryItems { get; set; }
public DbSet<InventoryItemCategory> InventoryItemCategories { get; set; }
public DbSet<InventoryItemOpeningBalance> InventoryItemOpeningBalances { get; set; }   // Gap 5
public DbSet<InventoryItemUnitConvert> InventoryItemUnitConverts { get; set; }         // Gap I
public DbSet<InventoryQuantityFormulaTemplate> InventoryQuantityFormulaTemplates { get; set; } // Gap J
public DbSet<InventoryQuantityFormulaDetail> InventoryQuantityFormulaDetails { get; set; }    // Gap J
public DbSet<InventoryItemBarcode> InventoryItemBarcodes { get; set; }            // Gap P
public DbSet<ItemAttributeType> ItemAttributeTypes { get; set; }                 // Gap N
public DbSet<InventoryItemAttribute> InventoryItemAttributes { get; set; }       // Gap N
public DbSet<Currency> Currencies { get; set; }
public DbSet<Unit> Units { get; set; }
public DbSet<Warehouse> Warehouses { get; set; }
public DbSet<Department> Departments { get; set; }
public DbSet<ExpenseItem> ExpenseItems { get; set; }

// Add to OnModelCreating — global query filters:
modelBuilder.Entity<AccountObject>().HasQueryFilter(
    e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
// ... same pattern for all 19 new entities (AccountObject, AccountObjectGroup, AccountObjectBankAccount,
//     AccountObjectOpeningBalance, AccountObjectEmployeeProfile, InventoryItem, InventoryItemCategory,
//     InventoryItemOpeningBalance, InventoryItemUnitConvert,
//     InventoryQuantityFormulaTemplate, InventoryQuantityFormulaDetail, InventoryItemBarcode,
//     ItemAttributeType, InventoryItemAttribute, Currency, Unit, Warehouse, Department, ExpenseItem)
```

---

## 7. Seed Data (FR-LK-007)

Applied via `MasterDataSeedData.cs` called from migration `Up()`:

```sql
-- Currency seed (applied per new tenant via seeding service, not hardcoded migration)
-- VND: CurrencyCode='VND', CurrencyName='Đồng Việt Nam', Symbol='₫', ExchangeRate=1

-- Default units (10 entries):
-- Cái, Chiếc, Hộp, Kg, Lít, M2, M3, Thùng, Bộ, Đôi
```

**Note**: Seed data is applied per-tenant at tenant creation time (not in migration). `MasterDataSeedData` is called by the `CreateTenantCommandHandler` after the tenant DB is provisioned.
