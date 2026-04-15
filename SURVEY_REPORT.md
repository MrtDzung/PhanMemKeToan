# BÁO CÁO KHẢO SÁT DATABASE INHONGHA
# (MISA AMIS Accounting — Phần mềm kế toán doanh nghiệp)

> **Server:** MRT_DZUNG\SQLEXPRESS | **Database:** INHONGHA | **Auth:** Windows
> **Ngày khảo sát:** 14/04/2026
> **Mục đích:** Khảo sát toàn diện DB kế toán MISA để thiết kế webapp kế toán mới cho DN Việt Nam
>
> ⚠️ **LƯU Ý QUAN TRỌNG:** Database INHONGHA là dữ liệu mẫu của **một doanh nghiệp cụ thể** (sản xuất/thương mại). Phần mềm đang xây dựng phục vụ **nhiều loại hình doanh nghiệp** (sản xuất, thương mại, dịch vụ, xây dựng...). Các bảng/module có 0 bản ghi trong DB mẫu này (PA, BO, PrepaidExpenses...) **KHÔNG phải không dùng** — chỉ là DN mẫu này không phát sinh nghiệp vụ đó. Các DN khác sẽ sử dụng đầy đủ. Số liệu row count chỉ mang tính tham khảo cơ cấu dữ liệu, không phản ánh tầm quan trọng của module.

---

## 1. TỔNG QUAN DATABASE

| Chỉ số | Giá trị |
|--------|---------|
| Tổng số bảng | **780** (694 dbo + 86 Report cache) |
| Tổng số view | **107** |
| Tổng số stored procedure | **5,410** |
| Tổng số function | **304** (tất cả đều table-valued) |
| Dung lượng | **6,127 MB** (~6 GB) |
| Schemas | `dbo` (dữ liệu nghiệp vụ) + `Report` (cache báo cáo) |
| Platform | MISA AMIS ACT2, Version 2017, MVC 72.0.0.3 |
| Chuẩn kế toán | **Thông tư 200** (AccountingSystem = 15) |
| Tiền tệ chính | VND |
| Ngày bắt đầu | 01/01/2019 |
| Phương pháp tính giá xuất kho | **Bình quân gia quyền** (DefaultCostMethod = 0) |

---

## 2. KIẾN TRÚC HỆ THỐNG — 15 MODULES

```
┌──────────────────────────────────────────────────────────────┐
│                    KIẾN TRÚC MISA AMIS                       │
├──────────┬──────────┬──────────┬──────────┬─────────────────┤
│  DI      │  GL      │  SA      │  PU      │  IN             │
│  Danh mục│  Sổ cái  │  Bán hàng│  Mua hàng│  Kho            │
│  (Core)  │  (Core)  │          │          │                 │
│  335 TK  │  4.8K CT │  53K CT  │  28K CT  │  210K phiếu     │
│  23K ĐT  │          │          │          │  78K vật tư     │
│  78K VT  │          │          │          │                 │
├──────────┼──────────┼──────────┼──────────┼─────────────────┤
│  CA      │  BA      │  FA      │  SU      │  JC             │
│  Tiền mặt│  Ngân hàng│ TSCĐ   │  CCDC    │  Giá thành      │
│  9K phiếu│  30K GD  │  220 TS  │  173 CT  │  852K phân bổ   │
├──────────┼──────────┼──────────┼──────────┼─────────────────┤
│  PA      │  TA      │  CT      │  IP/EI   │  SYS/MSC        │
│  Tiền    │  Thuế    │  Hợp đồng│ HĐĐT    │  Hệ thống       │
│  lương   │  3 TK khai│ 1.6K HĐ │ E-invoice│ 25 users        │
│  (*)     │          │          │          │  19 roles       │
└──────────┴──────────┴──────────┴──────────┴─────────────────┘
(*) PA module: 0 bản ghi trong DN mẫu — DN khác (có nhân viên) sẽ dùng đầy đủ
```

### Dữ liệu DN mẫu (INHONGHA — DN sản xuất/thương mại):

> ⚠️ Row count bên dưới chỉ phản ánh hoạt động của **1 DN mẫu**, không phản ánh tầm quan trọng module.

| Module | Bảng chính | Số lượng bản ghi | Đánh giá |
|--------|-----------|------------------:|----------|
| **Bán hàng (SA)** | SAVoucher | 53,768 | Module lớn nhất — 161K dòng chi tiết |
| **Mua hàng (PU)** | PUVoucher | 28,573 | 88K dòng chi tiết |
| **Kho (IN)** | INInward + INOutward | 79K + 130K = 210K | Outward detail: 399K dòng |
| **Ngân hàng (BA)** | BADeposit + BAWithDraw | 23K + 7K = 30K | |
| **Tiền mặt (CA)** | CAReceipt + CAPayment | 2.3K + 6.8K = 9K | |
| **Sổ cái (GL)** | GLVoucher | 4,860 | 11K dòng detail |
| **Giá thành (JC)** | JCCostAllocationDetail | **852,590** | Bảng lớn nhất DB |
| **Hợp đồng (CT)** | Contract | 1,641 | |
| **TSCĐ (FA)** | FixedAsset | 220 | |
| **CCDC (SU)** | SUIncrement | 173 | |
| **Tiền lương (PA)** | PASalarySheet | 0 | DN mẫu không phát sinh — DN khác sẽ dùng |
| **Thuế (TA)** | TADeclaration | 3 | DN mẫu có 3 tờ khai — DN khác sẽ nhiều hơn |

---

## 3. KIẾN TRÚC DỮ LIỆU — CÁC PATTERN QUAN TRỌNG

### 3.1 Pattern Master-Detail (Bút toán kép)

Mọi chứng từ đều theo pattern: **Header (Master) → Detail lines**

```
[CAPayment]  ←──RefID──→  [CAPaymentDetail]
   RefID (PK)                  RefID (FK)
   RefDate                     DebitAccount    ← TK Nợ
   RefNo                       CreditAccount   ← TK Có
   ContactName                 Amount          ← Số tiền
   TotalAmount                 AmountOC        ← Số tiền nguyên tệ
                               Description     ← Diễn giải dòng
```

**Pattern này áp dụng cho TẤT CẢ 30+ loại chứng từ** (CA*, BA*, GL*, SA*, PU*, IN*, FA*, SU*, JC*)

### 3.2 Hệ thống sổ kép (Dual-Book)

MISA hỗ trợ **2 hệ thống sổ song song**:

| DisplayOnBook | Ý nghĩa | Mục đích |
|:------------:|---------|---------|
| 0 | Sổ tài chính | Báo cáo cho cơ quan thuế, kiểm toán |
| 1 | Sổ quản trị | Báo cáo nội bộ cho quản lý |
| 2 | Cả hai sổ | Ghi đồng thời 2 sổ |

→ Bảng `SYSBook` chỉ có 3 dòng. Mỗi chứng từ đều có cột `DisplayOnBook`.

### 3.3 Soft Delete + Cloud Sync

- **IsDeleted**: Không xóa vật lý, chỉ đánh dấu `IsDeleted = 1`
- **UploadState**: Trạng thái đồng bộ cloud (Views filter `UploadState = 1`)
- **ServerRowVersion**: Optimistic concurrency + cloud sync tracking
- **Tất cả ID đều là GUID** (uniqueidentifier) — client tạo ID trước khi insert

### 3.4 SYSRefType — Registry trung tâm (262 loại chứng từ)

Mỗi loại chứng từ có một mã `RefType` duy nhất → quyết định:
- Bảng Master/Detail nào được sử dụng
- Hàm posting nào được gọi
- Thuộc module/subsystem nào
- Quyền truy cập (permission) nào áp dụng

---

## 4. POSTING ENGINE — TRÁI TIM HỆ THỐNG

### 4.1 Tổng quan

Posting engine chuyển đổi dữ liệu chứng từ → bút toán sổ cái. Đây là thành phần quan trọng nhất.

```
┌─────────────┐     ┌──────────────────┐     ┌────────────────────────────┐
│  User saves  │────→│ Proc_Post_       │────→│ Func_POST_*GeneralLedger   │
│  voucher     │     │ GetLedgerData    │     │ (table-valued function)    │
│  & clicks    │     │ (dynamic SQL)    │     │                            │
│  "Ghi sổ"   │     └──────────────────┘     │ SELECT Debit rows          │
└─────────────┘              │                │ UNION ALL                  │
                             │                │ SELECT Credit rows         │
                             ▼                └────────────┬───────────────┘
                    ┌──────────────────┐                   │
                    │ SYSPostMapping   │                   ▼
                    │ (config table)   │        ┌──────────────────────┐
                    │ MasterTable →    │        │ INSERT INTO          │
                    │ LedgerTable →    │        │ GeneralLedger        │
                    │ Func_POST_*      │        │ + TaxLedger          │
                    └──────────────────┘        │ + InventoryLedger    │
                                                │ + PurchaseLedger     │
                                                │ + SaleLedger         │
                                                │ + FixedAssetLedger   │
                                                │ + SupplyLedger       │
                                                │ + CustomFieldLedger  │
                                                └──────────────────────┘
                                                           │
                                                           ▼
                                                ┌──────────────────────┐
                                                │ Proc_Post_After      │
                                                │ PostAndUnpost        │
                                                │ - Update TaxLedger   │
                                                │ - Recalc inventory   │
                                                │   weighted avg price │
                                                │ - Insert relations   │
                                                └──────────────────────┘
```

### 4.2 SYSPostMapping — Bảng cấu hình posting

**80+ dòng** ánh xạ mỗi bảng master → sổ cái đích → hàm posting.

Ví dụ cho module mua hàng:

| Master Table | → Target Ledger | → Posting Function |
|-------------|----------------|-------------------|
| PUVoucher | GeneralLedger | Func_POST_PUVoucherGeneralLedger |
| PUVoucher | TaxLedger | Func_POST_PUVoucherTaxLedger |
| PUVoucher | PurchaseLedger | Func_POST_PUVoucherPurchaseLedger |
| PUVoucher | InventoryLedger | Func_POST_PUVoucherInventoryLedger |

→ Một chứng từ mua hàng posting vào **4 sổ cái** cùng lúc!

### 4.3 Cách Func_POST_* hoạt động (phân tích từ Func_POST_CAPaymentGeneralLedger)

```sql
-- Mỗi dòng chi tiết → 2 bút toán GeneralLedger:

-- Bút toán NỢ (Debit)
SELECT AccountNumber = D.DebitAccount,
       DebitAmount = ROUND(D.Amount, @AmountDecimalDigits),
       CreditAmount = 0
FROM CAPayment M
JOIN CAPaymentDetail D ON M.RefID = D.RefID
JOIN Account ACC ON ACC.AccountNumber = D.DebitAccount

UNION ALL

-- Bút toán CÓ (Credit)
SELECT AccountNumber = D.CreditAccount,
       DebitAmount = 0,
       CreditAmount = ROUND(D.Amount, @AmountDecimalDigits)
FROM CAPayment M
JOIN CAPaymentDetail D ON M.RefID = D.RefID
JOIN Account ACC ON ACC.AccountNumber = D.CreditAccount
```

**Đặc điểm chính:**
- Mỗi hàm trả về bảng ~80 cột khớp schema GeneralLedger
- Đọc decimal settings từ SYSDBOption (AmountDecimalDigits=0, AmountOCDecimalDigits=3...)
- Xử lý ngoại tệ: nếu `IsPostableInForeignCurrency = 0` → dùng VND thay cho nguyên tệ
- Resolve thông tin thừa: tên khách hàng, mã nhân viên, tên ngân hàng...

### 4.4 Tám loại sổ cái (Ledger Tables)

| Sổ cái | Mục đích | Nguồn dữ liệu |
|--------|---------|----------------|
| **GeneralLedger** | Sổ cái tổng hợp — TẤT CẢ bút toán | Mọi module |
| **TaxLedger** | Sổ thuế (VAT đầu vào/đầu ra) | CA, BA, GL, PU, SA |
| **InventoryLedger** | Nhập/xuất/tồn kho | IN, PU |
| **PurchaseLedger** | Theo dõi mua hàng | PU |
| **SaleLedger** | Theo dõi bán hàng | SA |
| **FixedAssetLedger** | Giá trị & khấu hao TSCĐ | FA |
| **SupplyLedger** | Theo dõi CCDC | SU |
| **CustomFieldLedger** | Trường tùy chỉnh | Mọi module |

---

## 5. HỆ THỐNG VIEW — LỚP TRUY CẬP DỮ LIỆU

### 5.1 Phân loại 107 views

| Loại | Số lượng | Kích thước | Đặc điểm |
|------|:--------:|-----------|-----------|
| **Mega views** (Search/Post) | 4 | 18K–308K | UNION ALL tất cả bảng voucher (86 unions!) |
| **Module views** (SA/PU/IN/CA/BA) | ~40 | 1K–14K | JOIN master+detail+SYSRefType, resolve tên |
| **Contract views** | 4 | 4K–99K | Tính doanh thu/chi phí thực từ GeneralLedger |
| **Master data views** | 7 | 0.7K–5K | Danh mục + group info |
| **Inventory views** | 11 | 0.8K–20K | Inward/Outward/Book |
| **E-Invoice views** | 2 | 6K–29K | Combined e-invoice status |

**View lớn nhất:** `View_Search_Voucher` (308K chars, 86 UNION ALL từ 91 bảng)
→ Đây là view cho màn hình **tìm kiếm chứng từ toàn hệ thống**.

### 5.2 Pattern kiến trúc từ Views

1. **Dual-book**: Hầu hết views có logic `DisplayOnBook` + `IsPostedFinance`/`IsPostedManagement`
2. **SYSRefType JOIN**: Gần như mọi view JOIN `SYSRefType` để lấy `RefTypeName`
3. **Mega UNION**: View hệ thống = UNION ALL tất cả bảng chứng từ — mỗi SELECT cho 1 loại
4. **Soft delete filter**: Filter `IsDeleted = 0` và `UploadState = 1`
5. **View = lớp hiển thị**: Thêm computed columns (tên trạng thái tiếng Việt, resolve FK → tên)

---

## 6. STORED PROCEDURES — 5,410 SPs

### 6.1 Phân bổ theo chức năng

| Loại | Số lượng | % | Mô tả |
|------|:--------:|:-:|-------|
| **Report** | 4,443 | 82% | Truy xuất dữ liệu cho báo cáo in/màn hình |
| **Master Data (DI)** | 587 | 11% | CRUD danh mục |
| **Sales (SA)** | 524 | 10% | Nghiệp vụ bán hàng |
| **Purchase (PU)** | 246 | 5% | Nghiệp vụ mua hàng |
| **Job Costing (JC)** | 246 | 5% | Tính giá thành |
| **Posting** | ~90 | 2% | Ghi sổ/bỏ ghi |
| **Check/Validate** | ~40 | <1% | Kiểm tra dữ liệu |
| **Sync** | ~135 | 3% | Đồng bộ cloud |
| Khác | ~600 | 11% | CT, IP, TA, SU, FA, IN, CA, BA, EMP... |

### 6.2 Báo cáo tài chính Việt Nam (Thông tư 200)

| Mã báo cáo | Tên VN | Tên EN | SP |
|------------|--------|--------|-----|
| **B01-DN** | Bảng cân đối kế toán | Balance Sheet | Proc_GLR_GetB01_DN |
| **B02-DN** | Báo cáo kết quả HĐKD | Income Statement | Proc_GLR_GetB02_DN |
| **B03-DN** | Báo cáo lưu chuyển tiền tệ | Cash Flow Statement | Proc_GLR_GetB03_DN |
| **B09-DN** | Thuyết minh BCTC | Notes to FS | Proc_GLR_GetB09_DN |
| **F01** | Bảng cân đối phát sinh | Trial Balance | Proc_GLR_GetF01 |

### 6.3 SP nghiệp vụ quan trọng

| SP | Kích thước | Chức năng |
|----|-----------|-----------|
| Proc_EI_GetMultiFullEInvoice | 268K–377K | Xuất HĐĐT hàng loạt |
| Proc_DI_ChangePostAccount | 49K | Đổi mã TK trong dữ liệu đã ghi sổ |
| Proc_NewDB_GetClosingAccountObjectAsOpening | 48K | Kết chuyển số dư cuối → đầu năm mới |
| Proc_Post_UnpostAllVoucher | 19K | Bỏ ghi sổ toàn bộ (reset kỳ) |
| Proc_Post_GetLedgerData | — | Core: gọi dynamic SQL → Func_POST_* |
| Proc_Post_AfterPostAndUnpost | — | Hậu xử lý: cập nhật thuế, tính lại giá kho |

### 6.4 Report Cache Pattern

```
SP chạy → Kết quả lưu vào Report.ReportData_Proc_* (86 bảng cache)
         → Report.ReportCacheList theo dõi tính hợp lệ
         → Yêu cầu tiếp theo đọc cache → Invalidate khi dữ liệu thay đổi
```

---

## 7. BUSINESS RULES — QUY TẮC NGHIỆP VỤ ĐÃ PHÁT HIỆN

### 7.1 Quy tắc hệ thống kế toán

| Code | Quy tắc | Mức quan trọng |
|------|---------|:--------------:|
| **BR-GL07** | Posting engine cấu hình qua bảng SYSPostMapping (không hardcode) | 🔴 Critical |
| **BR-GL08** | Mỗi dòng detail → 2 bút toán GeneralLedger (Nợ + Có) — bút toán kép | 🔴 Critical |
| **BR-GL09** | Dual-book: DisplayOnBook (0=Tài chính, 1=Quản trị, 2=Cả hai) | 🟡 Important |
| **BR-GL10** | Decimal precision trung tâm qua SYSDBOption — ROUND() mọi tính toán | 🔴 Critical |
| **BR-GL01** | Pattern Master-Detail: tất cả voucher dùng RefID làm FK | 🔴 Critical |
| **BR-GL03** | Tất cả ID = GUID — client tạo ID trước insert | 🟡 Important |
| **BR-GL04** | Optimistic concurrency qua RowVersion | 🟢 Normal |

### 7.2 Quy tắc danh mục

| Code | Quy tắc |
|------|---------|
| **BR-DI01** | Hệ thống TK dạng cây — mã con bắt đầu bằng mã cha (111 → 1111, 1112) |
| **BR-DI02** | Chỉ TK lá (IsParent=0) mới được dùng trong bút toán |
| **BR-DI03** | DetailBy* flags trên Account kiểm soát theo dõi chi tiết (theo KH, NCC, job, project...) |
| **BR-DI04** | AccountObject = bảng hợp nhất KH + NCC + NV (cùng entity có thể là cả 3) |

### 7.3 Quy tắc nghiệp vụ module

| Code | Quy tắc |
|------|---------|
| **BR-SA01** | Luồng bán hàng: Đơn hàng → Chứng từ bán → Hóa đơn → Trả lại |
| **BR-SA02** | E-Invoice: theo dõi PublishStatus (0-4), ND406, không xóa CT đã phát hành |
| **BR-PU01** | Luồng mua hàng: Đơn hàng → Chứng từ mua → Hóa đơn → Trả lại |
| **BR-IN01** | Giá kho theo FIFO; hỗ trợ 4 phương pháp (BQGQ, BQGQ tức thời, FIFO, Đích danh) |
| **BR-IN02** | BQGQ: sau posting, tự động tính lại giá xuất cascade đến các phiếu sau |
| **BR-FA01** | Khấu hao TSCĐ chạy hàng tháng, phân bổ theo phòng ban |
| **BR-JC01** | Giá thành là module nặng nhất (852K dòng phân bổ) |
| **BR-SYS01** | SYSRefType = registry trung tâm 262 loại chứng từ |

### 7.4 Ràng buộc nghiệp vụ (từ SYSDBOption)

| Cờ | Giá trị | Ý nghĩa |
|----|:-------:|---------|
| AllowOverOutwardStock | **False** | ❌ Không cho xuất vượt tồn kho |
| AllowOverCashPayment | **False** | ❌ Không cho chi vượt quỹ tiền mặt |
| AllowOverQuantityByLotNo | **False** | ❌ Không cho vượt số lượng theo lô |

---

## 8. FK RELATIONSHIPS — 250+ LIÊN KẾT

### Tổng quan liên kết

```
                    ┌──────────┐
         ┌────────→│ Account  │←────────┐
         │         │ (335)    │         │
         │         └──────────┘         │
         │              ↑               │
    ┌────┴─────┐   ┌────┴─────┐   ┌────┴─────┐
    │GLVoucher │   │CAPayment │   │BAWithDraw│
    │Detail    │   │Detail    │   │Detail    │
    │(Debit/   │   │(Debit/   │   │(Debit/   │
    │Credit    │   │Credit    │   │Credit    │
    │Account)  │   │Account)  │   │Account)  │
    └────┬─────┘   └────┬─────┘   └────┬─────┘
         │              │               │
         ▼              ▼               ▼
    ┌──────────────────────────────────────────┐
    │           GeneralLedger                   │
    │    (sổ cái tổng hợp — tất cả bút toán)   │
    └──────────────────────────────────────────┘
         │              │               │
         ▼              ▼               ▼
    ┌──────────┐  ┌──────────┐   ┌──────────┐
    │Account   │  │Inventory │   │SYSRefType│
    │Object    │  │Item      │   │(262 loại)│
    │(23K)     │  │(78K)     │   │          │
    └──────────┘  └──────────┘   └──────────┘
```

**5 bảng có nhiều FK nhất:**
1. **GeneralLedger** — tham chiếu Account, AccountObject, InventoryItem, Stock, Job, Project, Department...
2. **SAVoucherDetail** — → SAVoucher, SAInvoice, TACareerGroup, Account
3. **PUVoucherDetail** — → PUVoucher, PurchasePurpose, PUInvoice, Account
4. **BAWithDrawDetail** — → BAWithDraw, DebtAgreement, PurchasePurpose
5. **CAPaymentDetail** — → CAPayment, DebtAgreement

Chi tiết đầy đủ: xem [reseach_dabase.md](reseach_dabase.md) → mục "FOREIGN KEY MAP"

---

## 9. LUỒNG DỮ LIỆU CHÍNH (DATA FLOWS)

### 9.1 Luồng Mua hàng → Nhập kho → Thanh toán

```
PUOrder → PUVoucher → INInward → CAPayment/BAWithDraw
(Đặt hàng)  (Mua hàng)   (Nhập kho)   (Thanh toán)
                │             │              │
                ▼             ▼              ▼
          PUInvoice    InventoryLedger   GeneralLedger
          (Hóa đơn)   (Sổ kho)         (Nợ 152/156 | Có 331)
```

### 9.2 Luồng Bán hàng → Xuất kho → Thu tiền

```
SAOrder → SAVoucher → INOutward → CAReceipt/BADeposit
(Đặt hàng)  (Bán hàng)  (Xuất kho)   (Thu tiền)
                │            │              │
                ▼            ▼              ▼
          SAInvoice   InventoryLedger   GeneralLedger
          (Hóa đơn)  (Sổ kho)         (Nợ 131 | Có 511/512)
              │
              ▼
          IPPublish → EInvoiceStatus
          (Phát hành HĐĐT)
```

### 9.3 Luồng Cuối kỳ → BCTC

```
AccountTransfer → GLVoucher (kết chuyển)
JCPeriod → JCCostAllocation (tính giá thành)
                    │
                    ▼
            GeneralLedger (sổ cái)
                    │
                    ▼
            FRReportList → FRReportDetail
            (B01-DN, B02-DN, B03-DN, B09-DN)
                    │
                    ▼
            TADeclaration → TA_*_Detail
            (Tờ khai thuế GTGT, TNDN, TNCN)
```

---

## 9.5 HỆ THỐNG TEMPLATE FORM CHỨNG TỪ (SYSVoucherTemplate)

> **Phát hiện quan trọng**: MISA sử dụng hệ thống **metadata-driven form builder** — toàn bộ giao diện form chứng từ được cấu hình trong DB, không hard-code.

### Kiến trúc

| Bảng | Rows | Vai trò |
|------|-----:|---------|
| SYSVoucherTemplate | 605 | Định nghĩa template form theo RefType + VoucherType |
| SYSVoucherTemplateDetail | 1,993 | Cấu hình tab/grid/cột cho mỗi template |

**Mỗi template gồm:** Template → nhiều Detail rows → mỗi Detail = 1 tab/grid → TemplateConfig (XML) định nghĩa từng cột

### VoucherType — Form Variants

| VoucherType | Ý nghĩa | Module |
|-------------|---------|--------|
| NULL | Form duy nhất (không có biến thể) | FA, IN, GL, PA, CT |
| 1 / 2 | 2 biến thể form cho cùng RefType | PU (48 RT), SA (34 RT) |
| 10-13 / 20-23 | Sub-forms cho Phiếu thu/chi | CA |
| 30-34 / 40-43 | Sub-forms cho Thu/Chi tiền gửi | BA |

### Tab Layout — 36 loại tab

Top 5: Thông tin bổ sung (500), Hàng tiền (388), Thống kê (258), Thuế (236), Hạch toán (173)

### TemplateConfig XML — Grid Column Definition

Mỗi cột trong grid được định nghĩa bởi `<GridColumnConfig>` với attributes:
- **Key**: Tên cột → map trực tiếp đến column name trong detail table (VD: Key="DebitAccount" → CAPaymentDetail.DebitAccount)
- **Hidden/IsHiddenSystem**: Ẩn/hiện cột
- **IsReadOnly/IsReadOnlySystem**: Chỉ đọc
- **Caption**: Label tiếng Việt
- **VisiblePosition**: Thứ tự hiển thị
- **Width**: Độ rộng pixel

**141 unique column keys** trong grdDetail, bao gồm:
- Kế toán: DebitAccount, CreditAccount + 12 TK thuế/chiết khấu
- Số tiền: Amount, AmountOC, VATAmount + 20 loại tiền khác
- Kho: InventoryItemCode, Quantity, UnitPrice, LotNo, ExpiryDate, SerialNumber...
- Chiều phân tích: AccountObject, OrganizationUnit, ExpenseItem, BudgetItem, Job, ProjectWork, Order, Contract, DebtAgreement, ListItem
- Hóa đơn: InvTemplateNo, InvSeries, InvNo, InvDate, TaxAccountObject*

### Giá trị cho webapp mới

1. **Form rendering engine**: Dùng JSON thay XML, nhưng giữ nguyên kiến trúc metadata-driven
2. **Column visibility**: Cho phép user tùy chỉnh hiện/ẩn cột (IsSystem=1 là hệ thống, user chỉ override được cột có IsHiddenSystem=0)
3. **Tab system**: Mỗi voucher có 2-8 tabs, tab "Thông tin bổ sung" luôn ẩn mặc định (cho custom fields)
4. **VAT method logic**: 76 tabs tự ẩn khi DN dùng phương pháp tính thuế trực tiếp (IsHiddenTabByVATMethod=1)

---

## 10. ĐÁNH GIÁ & NHẬN XÉT

### 10.1 Điểm mạnh của MISA (nên tham khảo)

| # | Điểm mạnh | Chi tiết |
|---|-----------|----------|
| 1 | **Posting engine config-driven** | SYSPostMapping cho phép thêm loại chứng từ mới mà không sửa code posting |
| 2 | **SYSRefType centralized** | 1 bảng registry quản lý 262 loại chứng từ — dễ mở rộng |
| 3 | **Master-Detail pattern nhất quán** | Tất cả 30+ loại voucher dùng cùng pattern → code reuse cao |
| 4 | **Dual-book (Financial + Management)** | Đáp ứng nhu cầu DN lớn cần quản trị nội bộ khác sổ thuế |
| 5 | **Bút toán kép chuẩn** | Mỗi detail line → 2 GeneralLedger rows (Nợ + Có) |
| 6 | **Decimal precision centralized** | SYSDBOption kiểm soát tất cả ROUND() → tránh sai lệch |
| 7 | **VoucherReference cross-link** | 369K bản ghi liên kết chéo giữa các chứng từ |

### 10.2 Điểm yếu / Cần cải thiện

| # | Vấn đề | Chi tiết | Hướng cải thiện |
|---|--------|----------|----------------|
| 1 | **780 bảng — cấu trúc phức tạp** | Cấu trúc Master-Detail chi tiết, mỗi voucher type có 5-10 bảng con chuyên biệt (DetailTax, DetailSalary, DetailImportVAT...) | Cân nhắc gộp bảng con ít khác biệt, giữ nguyên các module (tất cả đều cần cho các loại DN khác nhau) |
| 2 | **5,410 SPs = spaghetti** | 82% là report SPs, nhiều trùng lặp logic, khó maintain | Chuyển sang query builder / ORM |
| 3 | **Report cache = 86 bảng** | Tự quản lý cache trong DB, không scalable | Dùng application-level cache (Redis) |
| 4 | **Mega views 308K chars** | View_Search_Voucher = 86 UNION ALL — chậm, khó optimize | Dùng materialized view hoặc search index |
| 5 | **Dynamic SQL trong posting** | Proc_Post_GetLedgerData dùng EXEC dynamic SQL → SQL injection risk | Posting logic nên ở application layer |
| 6 | **Tight coupling to DB** | Toàn bộ business logic trong SPs + Functions | Chuyển business logic lên application |
| 7 | **No audit on data level** | Chỉ log actions (MSC_AudittingLog), không log data changes | Implement proper audit trail |
| 8 | **GUID everywhere** | uniqueidentifier làm PK → fragmented indexes, poor join perf | Xem xét dùng int/bigint auto-increment + GUID cho external ref |

### 10.3 Phân loại module theo loại hình DN

> Tất cả module đều cần triển khai — tùy loại hình DN mà module nào được sử dụng nhiều.
> Bảng dưới đây phân loại module theo loại hình DN sử dụng chính.

| Module | DN Thương mại | DN Sản xuất | DN Dịch vụ | DN Xây dựng | Ghi chú |
|--------|:---:|:---:|:---:|:---:|--------|
| DI (Danh mục) | ✅ | ✅ | ✅ | ✅ | Core — tất cả DN |
| GL (Sổ cái) | ✅ | ✅ | ✅ | ✅ | Core — tất cả DN |
| CA (Tiền mặt) | ✅ | ✅ | ✅ | ✅ | Core — tất cả DN |
| BA (Ngân hàng) | ✅ | ✅ | ✅ | ✅ | Core — tất cả DN |
| SA (Bán hàng) | ✅ | ✅ | ✅ | ✅ | Hầu hết DN |
| PU (Mua hàng) | ✅ | ✅ | ✅ | ✅ | Hầu hết DN |
| IN (Kho) | ✅ | ✅ | ⚪ | ✅ | DN có hàng tồn kho |
| FA (TSCĐ) | ✅ | ✅ | ✅ | ✅ | DN có tài sản cố định |
| PA (Tiền lương) | ✅ | ✅ | ✅ | ✅ | DN có nhân viên (hầu hết) |
| TA (Thuế) | ✅ | ✅ | ✅ | ✅ | Bắt buộc theo luật |
| SU (CCDC) | ⚪ | ✅ | ⚪ | ✅ | DN có công cụ/dụng cụ |
| JC (Giá thành) | ⚪ | ✅ | ⚪ | ✅ | DN sản xuất/xây dựng |
| CT (Hợp đồng) | ⚪ | ⚪ | ✅ | ✅ | DN dịch vụ/xây dựng |
| IP/EI (HĐĐT) | ✅ | ✅ | ✅ | ✅ | Bắt buộc theo luật (2020+) |
| BO (Ngân sách) | ⚪ | ✅ | ⚪ | ✅ | DN quy mô lớn |
| PrepaidExpenses | ✅ | ✅ | ✅ | ✅ | DN có chi phí trả trước |

> ✅ = Thường xuyên sử dụng | ⚪ = Ít hoặc không sử dụng

---

## 11. CẤU HÌNH HỆ THỐNG (SYSDBOption)

| Setting | Giá trị | Ý nghĩa |
|---------|:-------:|---------|
| AccountingSystem | 15 | Thông tư 200 |
| DefaultCostMethod | 0 | Bình quân gia quyền |
| MainCurrency | VND | Đồng Việt Nam |
| StartDate | 01/01/2019 | Ngày bắt đầu |
| AmountDecimalDigits | 0 | Không thập phân cho VND |
| AmountOCDecimalDigits | 3 | 3 chữ số cho ngoại tệ |
| UnitPriceDecimalDigits | 2 | 2 chữ số cho đơn giá |
| QuantityDecimalDigits | 2 | 2 chữ số cho số lượng |
| ExchangeRateDecimalDigits | 2 | 2 chữ số cho tỷ giá |
| AllocationDecimalDigits | 10 | 10 chữ số cho tỷ lệ phân bổ |

---

## 12. TÀI LIỆU THAM KHẢO

- **File khảo sát chi tiết:** [reseach_dabase.md](reseach_dabase.md) (1,655 dòng)
  - Định nghĩa đầy đủ tất cả 780 bảng theo module
  - 250+ FK relationships
  - TAG system cho tìm kiếm nhanh: `[TBL:*]`, `[FK:*]`, `[BR:*]`, `[MOD:*]`, `[FLOW:*]`
  - Mẫu dữ liệu (Account chart, SYSRefType registry)
  - Đầy đủ SYSPostMapping (80+ dòng mapping)
  - Phân tích logic từng View quan trọng
  - Phân loại 5,410 SPs theo chức năng

---

> **Ghi chú:** Báo cáo này tổng hợp từ khảo sát trực tiếp trên database INHONGHA.
> Tất cả số liệu (row counts, SP counts, table counts) đều từ truy vấn thực tế.
> Dữ liệu phản ánh tình trạng DB tại thời điểm khảo sát (04/2026).
