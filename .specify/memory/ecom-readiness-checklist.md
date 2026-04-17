# E-Commerce Readiness Checklist — Thiết kế NGAY trong Accounting Webapp

> **Dual-DB scope**: All readiness items below apply to the **Tenant DB** (per-company database), not the Master DB.

> **Mục đích:** Danh sách chi tiết, step-by-step, TỪNG chức năng cần thiết kế sẵn trong webapp kế toán
> từ ngày 1, để khi tích hợp e-commerce sau này KHÔNG phải breaking change.
>
> **Nguyên tắc:** Chỉ thêm schema fields + interfaces + abstractions. KHÔNG build e-com logic.
> **Ngày tạo:** 2026-04-15

---

## MỨC ĐỘ ƯU TIÊN

| Icon | Mức | Ý nghĩa |
|:---:|------|---------|
| 🔴 | CRITICAL | Thiếu = breaking change khi thêm e-com |
| 🟡 | IMPORTANT | Thiếu = refactor lớn, nhưng không break schema |
| 🟢 | NICE-TO-HAVE | Dễ thêm sau, nhưng nên làm luôn cho gọn |

---

## 1. CROSS-CUTTING (Áp dụng TẤT CẢ modules)

### 1.1 🔴 Source Tracking — Phân biệt nguồn gốc chứng từ

**Vấn đề:** Khi có e-com, mọi voucher đều có thể tạo bởi user hoặc API. Nếu không track từ đầu, sau này không biết voucher nào từ e-com, nào từ manual.

**Thiết kế:**
```
Trên EVERY Voucher header (SA, PU, IN, CA, BA, GL):
  - Source: enum {Manual, EcomAPI, Import, System}   -- default: Manual
  - ExternalRefId: nvarchar(100), nullable           -- ID từ hệ thống ngoài
  - ExternalRefType: nvarchar(50), nullable           -- "EcomOrder", "EcomReturn", etc.
```

**Áp dụng cho bảng:** Voucher (unified), SAOrder, INOutward, INInward, CAReceipt, BADeposit, GLVoucher
**Chứng từ cụ thể:**
- SAOrder: `Source=EcomAPI, ExternalRefId="EC-20260415-0001"`
- INOutward (auto xuất kho): `Source=System, ExternalRefId → link về SAOrder`
- CAReceipt (COD): `Source=EcomAPI, ExternalRefId="PAY-xxx"`
- BADeposit (online payment): `Source=EcomAPI, ExternalRefId="VNPAY-xxx"`

### 1.2 🔴 Idempotency Key Storage

**Vấn đề:** E-com gửi request tạo đơn hàng → network timeout → retry → tạo duplicate voucher.

**Thiết kế:**
```
Bảng: IdempotencyKey
  - Key: nvarchar(200) PK
  - ResourceType: nvarchar(50)      -- "SAOrder", "InventoryHold"
  - ResourceId: uniqueidentifier     -- VoucherId đã tạo
  - ResponseBody: nvarchar(max)      -- Cached response JSON  
  - CreatedDate: datetime2
  - ExpiresAt: datetime2             -- TTL 72 giờ
  - INDEX: IX_ExpiresAt (cleanup job)
```

**Giá trị ngay:** Cả manual import (CSV/Excel) cũng cần idempotency. Không riêng e-com.

### 1.3 🔴 Domain Events Bus (In-Memory)

**Vấn đề:** Khi e-com tích hợp, cần notify e-com khi voucher posted, stock changed, price changed. Nếu không có event bus từ đầu, phải chèn notification code vào mọi service.

**Thiết kế:**
```csharp
// Interface định nghĩa sẵn
public interface IDomainEvent { DateTime OccurredAt { get; } }
public interface IDomainEventBus { Task Publish<T>(T @event) where T : IDomainEvent; }

// Events cần từ ngày 1 (dù chưa có subscriber nào dùng):
VoucherPostedEvent     { VoucherId, RefType, RefNo, RefDate, TotalAmount }
VoucherUnpostedEvent   { VoucherId, RefType }
StockChangedEvent      { InventoryItemId, StockId, QuantityDelta, NewQuantityOnHand }
ProductUpdatedEvent    { InventoryItemId, ChangedFields[] }
ProductPriceChangedEvent { InventoryItemId, OldPrice, NewPrice, EffectiveDate }
AccountObjectCreatedEvent { AccountObjectId, Type }
OrderStatusChangedEvent  { VoucherId, OldStatus, NewStatus }
```

**Hiện tại:** In-memory bus (MediatR pattern). Sau này swap sang RabbitMQ outbox.
**NON-NEGOTIABLE:** Logic phát event PHẢI nằm trong Domain/Application layer, KHÔNG ở controller.

### 1.4 🟡 Audit Trail (Data-Level)

**Vấn đề:** E-com dispute: "Tôi đặt giá 100K mà sao tính 120K?" → cần history ai đổi gì lúc nào.

**Thiết kế:**
```
Bảng: AuditLog
  - Id: bigint IDENTITY
  - EntityType: nvarchar(100)       -- "InventoryItem", "SAPolicyPrice"
  - EntityId: uniqueidentifier
  - Action: enum {Create, Update, Delete}
  - OldValues: nvarchar(max) JSON
  - NewValues: nvarchar(max) JSON
  - ChangedBy: uniqueidentifier
  - ChangedAt: datetime2
  - Source: enum {Manual, EcomAPI, System}
  - INDEX: IX_EntityType_EntityId, IX_ChangedAt
```

**MISA chỉ có:** MSC_AudittingLog (action log, không có data diff). Đây là cải tiến.

### 1.5 🟡 API Route Namespace + Auth Infrastructure

**Vấn đề:** Khi thêm e-com API, cần route structure + service-to-service auth.

**Thiết kế:**
```
/api/v1/                       -- Internal user API (JWT user token)
/api/v1/integration/           -- Reserved cho external system API
/api/v1/integration/inventory/ -- Stock queries
/api/v1/integration/orders/    -- Order creation
/api/v1/integration/catalog/   -- Product sync
/api/v1/webhooks/              -- Incoming webhooks

Auth roles:
  - internal-user    → full webapp access
  - integration-api  → restricted to /api/v1/integration/** 
  - webhook-sender   → restricted to /api/v1/webhooks/**
```

**Ngay bây giờ:** Chỉ cần DEFINE route convention + role enum. Chưa cần implement integration endpoints.

---

## 2. MODULE DI (Danh mục / Master Data) — 🔴 CRITICAL

### 2.1 🔴 InventoryItem — Thêm E-com Ready Fields

**Bảng MISA:** InventoryItem (78,491 rows)

| Field cần thêm | Kiểu | Mặc định | Lý do |
|----------------|------|---------|-------|
| `SKU` | nvarchar(50), UNIQUE | NULL | E-com dùng SKU làm product identifier. MISA dùng InventoryItemCode nhưng format không phù hợp e-com |
| `Barcode` | nvarchar(50) | NULL | Scan barcode khi nhập kho / ship hàng |
| `IsActive` | bit | 1 | Ngưng kinh doanh ≠ xóa. E-com cần biết sản phẩm còn bán không |
| `IsPublishable` | bit | 0 | Flag: sản phẩm này CÓ THỂ đăng lên e-com (chưa phải đã đăng) |
| `ShortDescription` | nvarchar(500) | NULL | Mô tả ngắn cho e-com listing |
| `Weight` | decimal(18,4) | NULL | Tính phí ship |
| `DimensionLength` | decimal(18,2) | NULL | Tính phí ship (dài) |
| `DimensionWidth` | decimal(18,2) | NULL | Tính phí ship (rộng) |
| `DimensionHeight` | decimal(18,2) | NULL | Tính phí ship (cao) |
| `DefaultWarehouseId` | uniqueidentifier | NULL | Kho xuất mặc định khi bán online |

**KHÔNG thêm:** SEO title, SEO slug, media URLs — đó là dữ liệu e-com, không phải accounting.

**Lưu ý về InventoryItemUnitConvert:** MISA đã có bảng quy đổi đơn vị (VD: 1 Thùng = 12 Chai). E-com sẽ hiển thị đơn vị bán (Thùng) nhưng kho track đơn vị nhỏ nhất (Chai). Interface `IUnitConversionService` cần expose từ ngày 1.

### 2.2 🔴 AccountObject — E-com Customer Matching

**Bảng MISA:** AccountObject (23,708 rows) — unified KH + NCC + NV

| Field cần thêm | Kiểu | Mặc định | Lý do |
|----------------|------|---------|-------|
| `Source` | enum {Manual, Ecom, Import} | Manual | Biết khách từ đâu |
| `ExternalCustomerId` | nvarchar(100) | NULL | Map khách hàng e-com ↔ kế toán |
| `MergeTargetId` | uniqueidentifier | NULL | Gộp khách trùng (self-FK) |
| `PhoneNormalized` | nvarchar(20) | NULL | SĐT chuẩn hóa (+84...) cho matching |
| `EmailNormalized` | nvarchar(255) | NULL | Email lowercase trim cho matching |

**Logic matching (design interface, chưa implement):**
```csharp
public interface IAccountObjectMatcher
{
    Task<AccountObject?> FindByPhone(string phone);
    Task<AccountObject?> FindByEmail(string email);
    Task<AccountObject?> FindByExternalId(string externalId);
    Task<AccountObject> FindOrCreate(EcomCustomerDto dto);  // upsert logic
}
```

**Customer Address Book:**
```
Bảng: AccountObjectAddress (MỚI)
  - Id: uniqueidentifier PK
  - AccountObjectId: FK → AccountObject
  - AddressType: enum {Billing, Shipping, Both}
  - ReceiverName: nvarchar(200)
  - Phone: nvarchar(20)
  - AddressLine1: nvarchar(500)
  - Ward: nvarchar(100)      -- Phường/Xã
  - District: nvarchar(100)  -- Quận/Huyện
  - Province: nvarchar(100)  -- Tỉnh/Thành phố
  - IsDefault: bit
```

**Lý do:** MISA chỉ có 1 address trên AccountObject. E-com khách có nhiều địa chỉ ship. Webapp kế toán cũng cần (ship khác billing cho hóa đơn).

### 2.3 🟡 InventoryItemCategory — Category Mapping

| Field cần thêm | Kiểu | Mặc định | Lý do |
|----------------|------|---------|-------|
| `ExternalCategoryId` | nvarchar(100) | NULL | Map danh mục kế toán ↔ danh mục e-com |

### 2.4 🟡 Stock (Warehouse) — E-com Enablement

| Field cần thêm | Kiểu | Mặc định | Lý do |
|----------------|------|---------|-------|
| `IsEcomFulfillment` | bit | 0 | Kho này phục vụ đơn e-com không |
| `FulfillmentPriority` | int | 0 | Thứ tự ưu tiên khi chọn kho xuất (0=không dùng cho e-com) |
| `Address` | nvarchar(500) | NULL | Địa chỉ kho (tính phí ship) |
| `Latitude` | decimal(9,6) | NULL | Tọa độ (chọn kho gần nhất) |
| `Longitude` | decimal(9,6) | NULL | Tọa độ |

---

## 3. MODULE SA (Bán hàng) — 🔴 MOST CRITICAL

### 3.1 🔴 SAOrder / Voucher Header — Order Source + Status

**Bảng liên quan:** SAOrder (31K), SAVoucher (53K)

Trên Voucher header (unified model):

| Field | Kiểu | Mặc định | Lý do |
|-------|------|---------|-------|
| `Source` | enum | Manual | Đã nêu ở §1.1 |
| `ExternalRefId` | nvarchar(100) | NULL | Đã nêu ở §1.1 |
| `ChannelType` | enum {Direct, Website, Shopee, Lazada, Tiki, TikTokShop, Other} | Direct | Kênh bán hàng — quan trọng cho báo cáo doanh thu theo kênh |
| `OrderStatus` | enum {Draft, Pending, Approved, Processing, Shipped, Completed, Cancelled} | Draft | Luồng trạng thái. Manual sale thường Draft→Completed. E-com cần full lifecycle |
| `CancellationReason` | nvarchar(500) | NULL | Lý do hủy (e-com bắt buộc) |
| `CancelledAt` | datetime2 | NULL | Thời điểm hủy |
| `CancelledBy` | uniqueidentifier | NULL | Ai hủy |
| `Notes` | nvarchar(max) | NULL | Ghi chú đơn hàng (e-com buyer note) |

### 3.2 🔴 Shipping Information — Trên Voucher

| Field | Kiểu | Mặc định | Lý do |
|-------|------|---------|-------|
| `ShippingAddress` | nvarchar(500) | NULL | Địa chỉ giao hàng |
| `ShippingReceiverName` | nvarchar(200) | NULL | Tên người nhận |
| `ShippingReceiverPhone` | nvarchar(20) | NULL | SĐT người nhận |
| `ShippingMethod` | nvarchar(100) | NULL | GHTK, GHN, Viettel Post, etc. |
| `ShippingTrackingNo` | nvarchar(100) | NULL | Mã vận đơn |
| `ShippingFee` | decimal(18,0) | 0 | Phí ship (VND, 0 decimal) |
| `ShippingFeeAccount` | nvarchar(20) | NULL | TK hạch toán phí ship (VD: 6418) |
| `EstimatedDeliveryDate` | datetime2 | NULL | Dự kiến giao |

**Manual sale cũng cần:** DN bán hàng có giao hàng cũng cần shipping info NGAY, không riêng e-com. Đây là chức năng kế toán thực sự.

### 3.3 🔴 Payment Information — Trên Voucher

| Field | Kiểu | Mặc định | Lý do |
|-------|------|---------|-------|
| `PaymentMethod` | enum {Cash, BankTransfer, COD, Momo, VNPay, ZaloPay, Card, Other} | NULL | Phương thức thanh toán |
| `PaymentStatus` | enum {Unpaid, PartiallyPaid, Paid, Refunded, PartiallyRefunded} | Unpaid | Trạng thái thanh toán. Link với CA/BA module |
| `PaymentReference` | nvarchar(200) | NULL | Mã giao dịch thanh toán (VNPAY transaction ID, etc.) |
| `PaidAmount` | decimal(18,0) | 0 | Số tiền đã thanh toán |
| `PaidAt` | datetime2 | NULL | Thời điểm thanh toán |

### 3.4 🔴 SAReturn — Return/Exchange E-com Fields

**Bảng MISA:** SAReturn (1,926 rows), SAReturnDetail (3,272 rows)

| Field | Kiểu | Mặc định | Lý do |
|-------|------|---------|-------|
| `ReturnType` | enum {Return, Exchange, PartialReturn} | Return | Loại trả hàng |
| `ReturnReason` | enum {Defective, WrongItem, Changed Mind, NotAsDescribed, Other} | NULL | Lý do (e-com bắt buộc) |
| `ReturnReasonNote` | nvarchar(500) | NULL | Chi tiết lý do |
| `RMANumber` | nvarchar(50) | NULL | Return Merchandise Authorization (mã xác nhận trả hàng) |
| `ExchangeVoucherId` | uniqueidentifier | NULL | FK → Voucher mới (đổi hàng) |
| `RefundMethod` | enum {OriginalPayment, Cash, BankTransfer, StoreCredit} | NULL | Hoàn tiền qua đâu |
| `RefundAmount` | decimal(18,0) | 0 | Số tiền hoàn |
| `RefundStatus` | enum {Pending, Processing, Completed} | NULL | Trạng thái hoàn |
| `ReturnShippingTrackingNo` | nvarchar(100) | NULL | Mã vận đơn trả hàng |

### 3.5 🟡 SAPolicyPrice — Pricing Engine Ready

**Bảng MISA:** SAPolicyPrice (0 rows — DN mẫu không dùng, nhưng MISA có sẵn schema)

| Field | Kiểu | Mặc định | Lý do |
|-------|------|---------|-------|
| `PriceListName` | nvarchar(200) | | Tên bảng giá |
| `EffectiveFrom` | datetime2 | | Ngày bắt đầu hiệu lực |
| `EffectiveTo` | datetime2 | NULL | Ngày hết hiệu lực |
| `IsDefault` | bit | 0 | Bảng giá mặc định |
| `AppliesTo` | enum {All, CustomerGroup, SpecificCustomer} | All | Áp dụng cho ai |

**Trên PriceListDetail:**
| Field | Kiểu | Lý do |
|-------|------|-------|
| `InventoryItemId` | FK | Sản phẩm |
| `UnitId` | FK | Đơn vị bán |
| `UnitPrice` | decimal(18,2) | Giá |
| `MinQuantity` | decimal(18,2) | Mua từ X cái thì giá này |
| `CurrencyId` | FK | Tiền tệ |

**NON-NEGOTIABLE Rule:** "Price Snapshot" — Giá tại thời điểm confirm đơn hàng PHẢI được lưu vào VoucherDetail.UnitPrice. Không bao giờ tham chiếu bảng giá hiện tại để tính tiền đơn đã confirm.

### 3.6 🟡 Promotion / Discount Tracking

| Field trên VoucherDetail | Kiểu | Lý do |
|--------------------------|------|-------|
| `PromotionCode` | nvarchar(50) | Mã khuyến mãi đã áp dụng |
| `DiscountType` | enum {None, Percentage, FixedAmount, FreeItem} | Loại giảm giá |
| `DiscountValue` | decimal(18,2) | Giá trị giảm |
| `OriginalUnitPrice` | decimal(18,2) | Giá gốc trước giảm |

### 3.7 🟡 Partial Fulfillment Support

**Vấn đề:** E-com order 10 items → kho chỉ có 7 → ship 7 trước, 3 sau. Cần 1 SAOrder → nhiều INOutward.

**Thiết kế:**
- Trên SAOrderDetail: thêm `QuantityFulfilled` (decimal) — số lượng đã giao
- Logic: có thể tạo nhiều SAVoucher/INOutward từ 1 SAOrder
- VoucherReference table (đã có pattern trong MISA: SaleOutwardReference) liên kết chúng

### 3.8 🟢 Backorder Flag

| Field trên SAOrderDetail | Kiểu | Lý do |
|--------------------------|------|-------|
| `IsBackordered` | bit | Default 0. Set khi stock insufficient |
| `BackorderedQuantity` | decimal(18,2) | Số lượng đang chờ hàng |
| `ExpectedRestockDate` | datetime2 | Dự kiến có hàng |

---

## 4. MODULE IN (Kho) — 🔴 CRITICAL

### 4.1 🔴 Stock Quantity Model — 4 loại số lượng

**Hiện tại MISA:** InventoryBalance chỉ có QuantityOnHand (tồn kho thực tế).

**Cần thêm (trên InventoryBalance hoặc computed):**

| Field | Kiểu | Ý nghĩa |
|-------|------|---------|
| `QuantityOnHand` | decimal(18,2) | Tồn kho thực tế (đã có) |
| `QuantityReserved` | decimal(18,2) | Đã xác nhận bán nhưng chưa xuất (SAOrder approved) |
| `QuantityOnHold` | decimal(18,2) | Tạm giữ cho giỏ hàng e-com (TTL 15-30 phút) |
| `QuantityDamaged` | decimal(18,2) | Hàng hỏng / đang kiểm tra (không bán được) |

**Công thức NON-NEGOTIABLE:**
```
QuantityAvailable = QuantityOnHand - QuantityReserved - QuantityOnHold - QuantityDamaged
```

**PHẢI centralized:**
```csharp
public interface IInventoryService
{
    // Query
    Task<decimal> GetAvailableQuantity(Guid itemId, Guid stockId);
    Task<Dictionary<Guid, decimal>> GetAvailableQuantityBatch(Guid[] itemIds, Guid stockId);
    Task<WarehouseStockDto[]> GetStockByWarehouses(Guid itemId);
    
    // Commands (e-com sẽ gọi qua API sau)
    Task<HoldResult> HoldStock(Guid itemId, Guid stockId, decimal qty, TimeSpan ttl);
    Task ReleaseHold(Guid holdId);
    Task<bool> ReserveStock(Guid itemId, Guid stockId, decimal qty, Guid orderId);
    Task ReleaseReserve(Guid orderId);
    
    // Internal (posting engine gọi)
    Task AdjustOnHand(Guid itemId, Guid stockId, decimal delta, Guid voucherId);
}
```

**Ngay bây giờ:** Chỉ implement GetAvailableQuantity (= OnHand vì chưa có e-com). Reserve/Hold logic để sau.

### 4.2 🔴 Stock Hold Table (Tạm giữ tồn kho)

```
Bảng: InventoryHold (MỚI)
  - Id: uniqueidentifier PK
  - InventoryItemId: FK → InventoryItem
  - StockId: FK → Stock
  - Quantity: decimal(18,2)
  - HoldType: enum {CartHold, OrderReserve}
  - ExternalRefId: nvarchar(100)     -- Cart session ID hoặc Order ID
  - CreatedAt: datetime2
  - ExpiresAt: datetime2             -- NULL cho OrderReserve (không hết hạn)
  - ReleasedAt: datetime2            -- NULL = đang hold
  - INDEX: IX_ItemId_StockId_Active (WHERE ReleasedAt IS NULL)
```

**Cleanup job:** Cron mỗi 5 phút → release expired holds (ExpiresAt < NOW AND ReleasedAt IS NULL)

### 4.3 🔴 Anti-Oversell Validation (Nâng cấp BR-SYS04)

**MISA hiện có:** `AllowOverOutwardStock = FALSE` → không cho xuất vượt tồn.

**Nâng cấp:** Check PHẢI dùng `QuantityAvailable` (trừ reserved + hold), KHÔNG dùng `QuantityOnHand`.

```csharp
// TRONG service tạo INOutward:
var available = await _inventoryService.GetAvailableQuantity(itemId, stockId);
if (requestedQty > available)
    throw new InsufficientStockException(itemId, stockId, available, requestedQty);
```

### 4.4 🟡 INOutward — Auto-creation from Sales

**Luồng MISA:** SAVoucher → SaleOutwardReference → INOutward (manual link)
**E-com luồng:** SAOrder approved → auto-create INOutward (picking list)

**Thiết kế interface:**
```csharp
public interface IFulfillmentService
{
    Task<INOutward> CreateOutwardFromOrder(Guid orderId);
    Task<INOutward[]> CreatePartialOutward(Guid orderId, FulfillmentLineDto[] lines);
}
```

**Ngay bây giờ:** Chỉ cần interface + manual linking. Auto-creation khi có e-com.

### 4.5 🟡 Multi-Warehouse Stock API

**MISA đã có:** Stock (52 warehouses), InventoryBalance per warehouse.

**Cần service:**
```csharp
public interface IWarehouseStockService
{
    Task<WarehouseStockDto[]> GetStockPerWarehouse(Guid itemId);
    Task<Guid> SelectOptimalWarehouse(Guid itemId, decimal qty, string? shippingProvince);
}
```

**Ngay bây giờ:** GetStockPerWarehouse hoạt động. SelectOptimalWarehouse chỉ cần interface.

### 4.6 🟡 Serial Number & Lot/Batch Tracking

**MISA đã có:** INSerialNumber (0 rows), LotNo/ExpiryDate trên VoucherDetail.

**E-com cần:** Gán serial khi ship → customer nhận biết serial → return check serial.

**Thiết kế:** Đảm bảo INSerialNumber đủ fields:
```
- SerialNumber: nvarchar(100)
- InventoryItemId: FK
- Status: enum {InStock, Sold, Returned, Damaged}
- SoldVoucherId: FK (nullable)
- ReturnedVoucherId: FK (nullable)
```

### 4.7 🟢 INTransfer — Auto-trigger for E-com

**Vấn đề:** Đơn e-com chọn kho A nhưng hết hàng → chuyển từ kho B.

**Chỉ cần interface:**
```csharp
public interface IStockTransferService
{
    Task<bool> ShouldAutoTransfer(Guid itemId, Guid targetStockId, decimal qty);
    Task<INTransfer> CreateTransfer(Guid fromStockId, Guid toStockId, TransferLineDto[] lines);
}
```

---

## 5. MODULE GL (Sổ cái) — 🟡 IMPORTANT

### 5.1 🟡 Posting Engine — Source Tracking

**Hiện tại:** Posting engine chỉ cần biết RefType + Master/Detail → generate GL entries.

**Thêm:** Mỗi GeneralLedger entry kế thừa `Source` từ Voucher header. Cho phép báo cáo lọc P&L theo source.

```
GeneralLedger:
  + Source: enum {Manual, EcomAPI, Import, System}   -- copy từ voucher
  + ChannelType: enum {Direct, Website, Shopee...}   -- copy từ voucher (nullable)
```

### 5.2 🟡 Report Filter by Source/Channel

**SP MISA:** Proc_GLR_GetB02_DN (Income Statement), Proc_GLR_GetF01 (Trial Balance)...

**Cần từ đầu:** Mọi report query đều có optional filter `WHERE Source = @Source AND ChannelType = @Channel`. Nếu NULL → lấy tất cả (backward compatible).

### 5.3 🟡 VoucherReference — Chuỗi chứng từ

**MISA đã có:** VoucherReference (369K rows) — link giữa các chứng từ liên quan.

**E-com chuỗi hoàn chỉnh:**
```
EcomOrder (external) 
  → SAOrder (kế toán)
    → SAVoucher (delivery)
      → INOutward (xuất kho)
        → SAInvoice (hóa đơn)
          → IPPublish (e-invoice)
            → CAReceipt/BADeposit (thu tiền)
```

**Tất cả link qua VoucherReference.** Thiết kế unified model đã có field cho cross-ref. Chỉ cần đảm bảo VoucherReference có thêm:
- `ExternalRefId` — link ngược về e-com order ID
- `ReferenceType` — enum mô tả relationship (Order→Delivery, Delivery→Invoice, etc.)

---

## 6. MODULE CA (Tiền mặt) — 🟡 IMPORTANT

### 6.1 🟡 CAReceipt — COD Payment Tracking

**Vấn đề:** COD (Cash on Delivery) là phương thức thanh toán #1 tại VN. Shipper thu tiền → nộp về công ty.

| Field | Kiểu | Lý do |
|-------|------|-------|
| `Source` | enum | Đã nêu ở §1.1 |
| `ExternalRefId` | nvarchar(100) | Link về e-com order + payment ID |
| `CODCollector` | nvarchar(200) | Tên shipper/đơn vị vận chuyển thu hộ |
| `CODCollectionDate` | datetime2 | Ngày shipper thu tiền |
| `CODRemittanceDate` | datetime2 | Ngày shipper nộp tiền về (reconciliation) |

### 6.2 🟢 Auto-Receipt from Order

**Interface sẵn:**
```csharp
public interface IPaymentService
{
    Task<CAReceipt> CreateReceiptFromOrder(Guid orderId, PaymentDto payment);
    Task<BADeposit> CreateDepositFromOnlinePayment(Guid orderId, OnlinePaymentDto payment);
}
```

---

## 7. MODULE BA (Ngân hàng) — 🟡 IMPORTANT

### 7.1 🟡 BADeposit — Payment Gateway Reconciliation

| Field | Kiểu | Lý do |
|-------|------|-------|
| `Source` | enum | Đã nêu ở §1.1 |
| `ExternalRefId` | nvarchar(100) | Payment gateway transaction ID |
| `PaymentGateway` | nvarchar(50) | VNPay, Momo, ZaloPay, etc. |
| `GatewayTransactionId` | nvarchar(200) | Mã giao dịch cổng thanh toán |
| `GatewayFee` | decimal(18,0) | Phí cổng thanh toán (để hạch toán chi phí) |
| `GatewayFeeAccount` | nvarchar(20) | TK hạch toán phí (VD: 6358) |
| `SettlementDate` | datetime2 | Ngày cổng thanh toán quyết toán về TK ngân hàng |

### 7.2 🟡 Bank Reconciliation Enhancement

**MISA đã có:** BAReconcile (0 rows — cơ bản).

**E-com cần:** Auto-matching deposit records với payment gateway settlement files.

**Interface:**
```csharp
public interface IBankReconciliationService
{
    Task<ReconciliationResult> ReconcileGatewaySettlement(
        GatewaySettlementFileDto file, 
        Guid bankAccountId,
        DateRange period);
}
```

---

## 8. MODULE PU (Mua hàng) — 🟢 NICE-TO-HAVE

### 8.1 🟢 Auto-PO from Low Stock (Future)

**Vấn đề:** E-com bán mạnh → tồn kho thấp → auto tạo PO cho supplier.

**Chỉ cần trên InventoryItem:**
| Field | Kiểu | Lý do |
|-------|------|-------|
| `ReorderPoint` | decimal(18,2) | Mức tồn kho tối thiểu → alert/auto PO |
| `ReorderQuantity` | decimal(18,2) | Số lượng đặt hàng mặc định |
| `PreferredSupplierId` | FK → AccountObject | NCC mặc định |
| `LeadTimeDays` | int | Thời gian giao hàng từ NCC |

---

## 9. MODULE TA (Thuế) — 🟡 IMPORTANT

### 9.1 🟡 VAT Handling — Volume & Automation

**Không cần thêm schema.** Nhưng cần:
- Posting engine PHẢI tạo TaxLedger entries chính xác cho mọi chứng từ (dù manual hay e-com)
- VATAmount trên VoucherDetail PHẢI tính tự động từ VAT rate (đã có trong MISA)

### 9.2 🟡 E-Invoice Auto-Publish Queue

**Vấn đề:** Mỗi đơn e-com → tự động xuất HĐĐT. Manual publish từng cái = bottleneck.

**Thiết kế:**
```
Bảng: EInvoiceQueue (MỚI)
  - Id: bigint IDENTITY
  - VoucherId: FK → Voucher
  - Status: enum {Pending, Publishing, Published, Failed, Retrying}
  - RetryCount: int default 0
  - LastError: nvarchar(500)
  - CreatedAt: datetime2
  - ProcessedAt: datetime2
  - INDEX: IX_Status_CreatedAt
```

**Background job:** Poll bảng này mỗi 30s → batch publish → update status.

---

## 10. MODULE IP/EI (Hóa đơn điện tử) — 🟡 IMPORTANT

### 10.1 🟡 Auto E-Invoice for E-com Orders

**MISA flow:** SAVoucher → click "Phát hành HĐĐT" → IPPublish → PublishStatus 0→4.

**E-com flow:** SAVoucher auto-created → auto-enqueue E-Invoice → published → email PDF to customer.

**Cần từ đầu:**
- `AutoPublishEInvoice` flag trên Voucher (default FALSE, set TRUE for e-com orders)
- `CustomerEmail` trên Voucher (để auto-send HĐĐT qua email)
- EInvoiceQueue table (§9.2 above)

---

## 11. MODULE FA, SU, JC, PA — 🟢 KHÔNG CẦN E-COM FIELDS

### 11.1 FA (Fixed Assets) — Không liên quan trực tiếp
### 11.2 SU (CCDC) — Không liên quan trực tiếp
### 11.3 JC (Giá thành) — Gián tiếp: chi phí sản xuất → COGS → but no schema change
### 11.4 PA (Tiền lương) — Không liên quan trực tiếp

**Lưu ý:** Các module này hưởng lợi từ Domain Events (§1.3). VD: JC posting → StockChangedEvent (nếu xuất NVL). Nhưng không cần thêm field nào.

---

## 12. MODULE CT (Hợp đồng) — 🟢 NICE-TO-HAVE

### 12.1 🟢 B2B E-com Contract Linking

| Field | Kiểu | Lý do |
|-------|------|-------|
| `ExternalContractRef` | nvarchar(100) | Map hợp đồng kế toán ↔ e-com B2B agreement |

---

## 13. MODULE SYS (Hệ thống) — 🟡 IMPORTANT

### 13.1 🟡 SYSRefType — Không thêm RefType, chỉ thêm Source

**Decision:** E-com orders dùng CÙNG RefType với manual orders (VD: RefType cho "Phiếu bán hàng" = same). Phân biệt bằng `Source` field trên voucher. Lý do: cùng nghiệp vụ kế toán, cùng posting rule, cùng report. Chỉ khác cách tạo.

### 13.2 🟡 Permission + API Key Management

```
Bảng: APIClient (MỚI)
  - Id: uniqueidentifier PK
  - ClientName: nvarchar(200)        -- "Shopee Integration", "Website"
  - ApiKey: nvarchar(256)             -- Hashed
  - ApiSecret: nvarchar(256)          -- Hashed
  - Role: enum {IntegrationAPI, WebhookSender}
  - IsActive: bit
  - RateLimit: int                    -- Requests per minute
  - AllowedIPs: nvarchar(500)         -- Comma-separated IP whitelist
  - CreatedAt: datetime2
  - LastUsedAt: datetime2
```

### 13.3 🟡 Webhook Configuration

```
Bảng: WebhookSubscription (MỚI)
  - Id: uniqueidentifier PK
  - ClientId: FK → APIClient
  - EventType: nvarchar(100)          -- "stock.updated", "order.status_changed"
  - TargetUrl: nvarchar(500)          -- Endpoint nhận webhook
  - Secret: nvarchar(256)             -- HMAC-SHA256 signing key
  - IsActive: bit
  - CreatedAt: datetime2

Bảng: WebhookDeliveryLog (MỚI)
  - Id: bigint IDENTITY
  - SubscriptionId: FK → WebhookSubscription
  - EventType: nvarchar(100)
  - Payload: nvarchar(max)            -- JSON payload
  - ResponseStatusCode: int
  - ResponseBody: nvarchar(max)
  - Attempt: int
  - DeliveredAt: datetime2
  - NextRetryAt: datetime2 nullable
```

### 13.4 🟢 SYSDBOption — E-com Config

Thêm vào system settings:

| Setting | Kiểu | Default | Ý nghĩa |
|---------|------|---------|---------|
| `EcomIntegrationEnabled` | bit | 0 | Bật/tắt tích hợp e-com |
| `CartHoldTTLMinutes` | int | 15 | Thời gian giữ chỗ tồn kho cho giỏ hàng |
| `AutoPublishEInvoiceForEcom` | bit | 0 | Tự phát hành HĐĐT cho đơn e-com |
| `DefaultEcomWarehouseId` | GUID | NULL | Kho mặc định cho đơn e-com |
| `PriceTolerancePercent` | decimal | 5.0 | % chênh lệch giá cho phép (anti-tamper) |

---

## 14. SERVICE INTERFACES — TỔNG HỢP

Tất cả interfaces cần DEFINE từ ngày 1 (implement tối thiểu):

| # | Interface | Module | Implement ngay | Implement khi e-com |
|---|-----------|--------|:-:|:-:|
| 1 | `IInventoryService` | IN | GetAvailableQuantity (= OnHand) | Hold, Reserve, Release |
| 2 | `IOrderService` | SA | CreateOrder, UpdateStatus | CreateFromEcom, Cancel, Exchange, Return |
| 3 | `IProductCatalogService` | DI | GetProduct, GetPrice | SyncToEcom, GetChanges |
| 4 | `IAccountObjectMatcher` | DI | FindById | FindByPhone, FindByEmail, FindOrCreate |
| 5 | `IFulfillmentService` | IN+SA | Manual link | Auto-create, Partial fulfillment |
| 6 | `IPaymentService` | CA+BA | Manual receipt/deposit | Auto from order |
| 7 | `IUnitConversionService` | DI | Convert(item, fromUnit, toUnit) | Same |
| 8 | `IBankReconciliationService` | BA | Manual reconcile | Gateway auto-match |
| 9 | `IWarehouseStockService` | IN | GetStockPerWarehouse | SelectOptimalWarehouse |
| 10 | `IDomainEventBus` | Cross | In-memory MediatR | RabbitMQ outbox |
| 11 | `IWebhookService` | SYS | — | Publish events to subscribers |
| 12 | `IEInvoiceQueueService` | IP | — | Auto-queue + batch publish |

---

## 15. DB SCHEMA CHANGES SUMMARY — BẢNG MỚI

| # | Bảng | Module | Mục đích |
|---|------|--------|---------|
| 1 | `IdempotencyKey` | Cross | Tránh duplicate write |
| 2 | `AuditLog` | Cross | Data-level change tracking |
| 3 | `AccountObjectAddress` | DI | Customer address book (multi-address) |
| 4 | `InventoryHold` | IN | Stock hold/reserve tracking |
| 5 | `EInvoiceQueue` | IP | Auto e-invoice publication queue |
| 6 | `APIClient` | SYS | API key management |
| 7 | `WebhookSubscription` | SYS | Webhook config |
| 8 | `WebhookDeliveryLog` | SYS | Webhook delivery tracking |

---

## 16. EXISTING FIELDS TO ADD ON UNIFIED VOUCHER MODEL

| # | Field | Type | Default | Module áp dụng |
|---|-------|------|---------|-----------------|
| 1 | `Source` | enum | Manual | ALL |
| 2 | `ExternalRefId` | nvarchar(100) | NULL | ALL |
| 3 | `ExternalRefType` | nvarchar(50) | NULL | ALL |
| 4 | `ChannelType` | enum | Direct | SA |
| 5 | `OrderStatus` | enum | Draft | SA |
| 6 | `CancellationReason` | nvarchar(500) | NULL | SA |
| 7 | `ShippingAddress` | nvarchar(500) | NULL | SA |
| 8 | `ShippingReceiverName` | nvarchar(200) | NULL | SA |
| 9 | `ShippingReceiverPhone` | nvarchar(20) | NULL | SA |
| 10 | `ShippingMethod` | nvarchar(100) | NULL | SA |
| 11 | `ShippingTrackingNo` | nvarchar(100) | NULL | SA |
| 12 | `ShippingFee` | decimal(18,0) | 0 | SA |
| 13 | `PaymentMethod` | enum | NULL | SA, CA, BA |
| 14 | `PaymentStatus` | enum | Unpaid | SA |
| 15 | `PaymentReference` | nvarchar(200) | NULL | SA, CA, BA |
| 16 | `ReturnType` | enum | Return | SA (return) |
| 17 | `ReturnReason` | enum | NULL | SA (return) |
| 18 | `RMANumber` | nvarchar(50) | NULL | SA (return) |
| 19 | `RefundMethod` | enum | NULL | SA (return) |

---

## 17. CHECKLIST TỔNG KẾT — DO NOW vs DO LATER

### ✅ DO NOW (In accounting webapp day 1)

| # | Hạng mục | Effort | Lý do bắt buộc |
|---|----------|:---:|-------|
| 1 | Source + ExternalRefId trên mọi voucher | S | Breaking change nếu thiếu |
| 2 | IdempotencyKey table | S | Cần cho import luôn |
| 3 | Domain Event Bus (MediatR in-memory) | M | Foundation cho mọi integration |
| 4 | IInventoryService interface + GetAvailableQuantity | M | Centralized stock = anti-oversell |
| 5 | InventoryHold table schema | S | Schema-only, logic sau |
| 6 | InventoryItem: SKU, Barcode, IsActive, IsPublishable, Weight/Dimensions | S | Schema fields |
| 7 | AccountObject: Source, ExternalCustomerId, PhoneNormalized, EmailNormalized | S | Schema fields |
| 8 | AccountObjectAddress table | S | Multi-address cần cho manual sale nữa |
| 9 | Stock: IsEcomFulfillment, FulfillmentPriority | S | Schema fields |
| 10 | SAOrder: OrderStatus, ChannelType, Shipping fields, Payment fields | M | Core order lifecycle |
| 11 | SAReturn: ReturnType, ReturnReason, RMANumber, RefundMethod | S | Return flow |
| 12 | VoucherReference: ExternalRefId, ReferenceType | S | Schema fields |
| 13 | GL: Source + ChannelType on GeneralLedger | S | Report filter |
| 14 | Anti-oversell check dùng QuantityAvailable (not just OnHand) | M | Correctness |
| 15 | API route convention + role enum | S | Convention only |
| 16 | AuditLog table | S | Good practice regardless |
| 17 | InventoryItem: ReorderPoint, PreferredSupplierId | S | Schema fields |
| 18 | SAPolicyPrice / PriceList model | M | Pricing engine design |
| 19 | Price Snapshot rule: lock price into VoucherDetail | S | Business rule enforcement |
| 20 | EInvoiceQueue table | S | Schema-only, processor sau |

### ⏳ DO LATER (When e-com integration starts)

| # | Hạng mục | Lý do chờ |
|---|----------|----------|
| 1 | REST API endpoints (/api/v1/integration/*) | Không ai gọi |
| 2 | RabbitMQ integration (swap MediatR) | Infrastructure cost |
| 3 | Webhook delivery engine | Không có subscriber |
| 4 | APIClient + WebhookSubscription tables | Chưa cần manage |
| 5 | Cart hold logic (15min TTL) | E-com specific |
| 6 | Auto-fulfill (SAOrder → INOutward) | E-com specific |
| 7 | Auto-receipt from COD | E-com specific |
| 8 | Bank reconciliation with payment gateway | E-com specific |
| 9 | SelectOptimalWarehouse logic | E-com specific |
| 10 | Stock sync polling / webhook to e-com | E-com specific |
| 11 | Daily reconciliation cron | E-com specific |
| 12 | Rate limiting middleware | E-com specific |
| 13 | SYSDBOption e-com settings (CartHoldTTL, etc.) | Config khi cần |

---

**Version:** 1.0.0 | **Created:** 2026-04-15
