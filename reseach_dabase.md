# DATABASE MAP — INHONGHA (MISA Accounting)

> Research document for database `INHONGHA` on server `MRT_DZUNG\SQLEXPRESS`
> Authentication: Windows Authentication
> Platform: MISA AMIS Accounting (Vietnamese enterprise accounting software)
> Last updated: 2026-04-14

---

## TABLE OF CONTENTS

- [Task List](#task-list)
- [Database Overview](#database-overview)
- [Module Map](#module-map)
- [Module Details](#module-details)
- [Table Definitions](#table-definitions)
- [Foreign Key Map](#foreign-key-map)
- [Data Flows](#data-flows)
- [Business Rules Summary](#business-rules-summary)
- [Views](#views)
- [Stored Procedures & Functions](#stored-procedures--functions)
- [Appendix: Sample Data](#appendix-sample-data)
- [Appendix: Abbreviations & Multi-language](#appendix-abbreviations--multi-language)
- [Appendix: SYSRefType Registry](#appendix-sysreftype-registry)

---

## TASK LIST

| # | Task | Status | Notes |
|---|------|:------:|-------|
| 1 | Connect & collect DB overview | ✅ | 780 tables, 6.1 GB |
| 2 | List all schemas | ✅ | dbo + Report |
| 3 | List & classify all tables by business module | ✅ | 15 modules identified |
| 4 | Survey table structures (columns, types, PKs, FKs) | ✅ | 250+ FK relationships mapped |
| 5 | Find abbreviations/dictionary table | ✅ | Inline English columns, not separate table |
| 6 | Analyze sample data & infer business rules | ✅ | Account chart of VN standard confirmed |
| 7 | Survey Views — purpose & logic | 🔄 | 107 views cataloged |
| 8 | Survey Stored Procedures — processing flows | 🔄 | 5410 SPs (mostly report procs) |
| 9 | Draw ER relationships & data flows | ✅ | FK map + workflow diagrams |
| 10 | Consolidate business rules & recommendations | 🔄 | In progress |

> ⬜ Not started | 🔄 In progress | ✅ Completed

---

## DATABASE OVERVIEW

| Property | Value |
|----------|-------|
| Server | `MRT_DZUNG\SQLEXPRESS` |
| Database | `INHONGHA` |
| Auth | Windows Authentication |
| Total Tables | **780** (694 dbo + 86 Report) |
| Total Views | **107** |
| Total Stored Procedures | **5,410** |
| Total Functions | **304** |
| Schemas | `dbo` (business data), `Report` (report cache/data) |
| Collation | `SQL_Latin1_General_CP1_CI_AS` |
| DB Size | **6,127 MB** (~6 GB) |

---

## MODULE MAP

> Tables classified by prefix naming convention used by MISA

```
┌────────────────────────────────────────────────────────────────────────┐
│                      INHONGHA DATABASE MAP                             │
│                      (MISA AMIS Accounting)                            │
├─────────────────┬─────────────────┬─────────────────┬─────────────────┤
│ MOD:DI          │ MOD:GL          │ MOD:SA          │ MOD:PU          │
│ Master Data     │ General Ledger  │ Sales           │ Purchases       │
│ (Dictionary)    │ & Journals      │                 │                 │
│                 │                 │                 │                 │
│ Account(335)    │ GLVoucher(4860) │ SAVoucher(53K)  │ PUVoucher(28K)  │
│ AccountObj(23K) │ GLVoucherDtl    │ SAOrder(31K)    │ PUOrder(19K)    │
│ InventoryItem   │ GeneralLedger   │ SAInvoice(13K)  │ PUInvoice(3.6K) │
│ (78K)           │ CustomFieldLdgr │ SAReturn(1.9K)  │ PUReturn(252)   │
│ Bank/BankAcct   │                 │ SAQuote(630)    │ PUService(109)  │
│ CCY(169)        │                 │ SADiscount(424) │ PUDiscount(9)   │
│ Unit(87)        │                 │ SAPolicy        │ PUContract      │
│ Stock(52)       │                 │                 │                 │
├─────────────────┼─────────────────┼─────────────────┼─────────────────┤
│ MOD:CA          │ MOD:BA          │ MOD:IN          │ MOD:FA          │
│ Cash (Quỹ)     │ Bank Txn        │ Inventory       │ Fixed Assets    │
│                 │                 │ (Warehouse)     │                 │
│ CAReceipt(2312) │ BADeposit(23K)  │ INInward(79K)   │ FixedAsset(220) │
│ CAPayment(6782) │ BAWithDraw(7K)  │ INOutward(130K) │ FADepreciatn    │
│ CAAudit         │ BAIntTransfer   │ INTransfer(5K)  │ FADecrement(8)  │
│ CACashBook      │ BAReconcile     │ INAssembly(250) │ FAAdjustment(2) │
│                 │ EBTransferInfo  │ INAudit(221)    │ FAAudit         │
│                 │                 │ INProductionOrd │ FATransfer(1)   │
│                 │                 │ (83K)           │                 │
├─────────────────┼─────────────────┼─────────────────┼─────────────────┤
│ MOD:JC          │ MOD:PA          │ MOD:TA          │ MOD:SU          │
│ Job Costing     │ Payroll         │ Tax             │ Supplies/Tools  │
│                 │                 │                 │ (CCDC)          │
│ JCPeriod(33)    │ PASalarySheet   │ TADeclaration(3)│ SUIncrement(173)│
│ JCPeriodDtl(65K)│ PASalaryExpense │ TADeclAppendix  │ SUDecrement     │
│ JCCostAlloc     │ PATimeSheet     │ TA_01xGTGT_*    │ SUAllocation(53)│
│ (852K!)         │ PATimeSheetSum  │ TA_TNCN_*       │ SUAudit         │
│ JCExpenseTrnf   │                 │ TATaxAgentInfo  │ SUTransfer      │
│ JCUncomplete    │                 │ TaxLedger       │ SUAdjustment    │
├─────────────────┼─────────────────┼─────────────────┼─────────────────┤
│ MOD:CT          │ MOD:FR          │ MOD:IP          │ MOD:SYS         │
│ Contract        │ Financial       │ Invoice         │ System &        │
│ (Sale)          │ Reports         │ Publishing      │ Config          │
│                 │                 │                 │                 │
│ Contract(1641)  │ FRTemplate(576) │ IPRegister      │ SYSRefType(262) │
│ ContractDtl*    │ FRReportDetail  │ IPPublishAnncmt │ SYSDBOption(529)│
│ ContractSaleDry │ FRB09DN*        │ IPAdjustAnncmt  │ MSC_User(25)    │
│ ContractHist    │ FRF01*          │ IPCancelAnncmt  │ MSC_Role(19)    │
│                 │ FRObligGovTmpl  │ EInvoice*       │ MSC_Permission  │
│                 │                 │ InvoiceBot      │ SYSAutoID       │
│                 │                 │ InvTemplate     │ SYSBook(3)      │
├─────────────────┼─────────────────┼─────────────────┼─────────────────┤
│ MOD:EMP         │ MOD:OPENING     │ MOD:SYNC        │ MOD:REPORT      │
│ Employee        │ Opening Balance │ Cloud Sync      │ Report Cache    │
│ Advance/Request │                 │                 │ (schema:Report) │
│                 │                 │                 │                 │
│ EMPAdvancePaymt │ OpeningAcctEntry│ SyncChangeLog   │ 86 tables named │
│ EMPAdvanceReq   │ (740)           │ SyncDeleteObj   │ ReportData_*    │
│ EMPPaymentReq   │ OpeningInvEntry │ SyncDownload*   │ (report SP      │
│                 │ (546)           │ SyncResult*     │  result cache)  │
└─────────────────┴─────────────────┴─────────────────┴─────────────────┘
```

---

## MODULE DETAILS

<!-- [MOD:DI] -->
### MOD:DI — Master Data (Dictionary)

Core reference/master data tables used across all modules.

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| Account | Chart of accounts (VN standard) | 335 | self-ref via ParentID; ← AccountDefault |
| AccountObject | Customers, Suppliers, Employees (unified) | 23,708 | ← PULastedUnitPrice, AccountObjectBankAccount |
| AccountObjectGroup | Customer/Supplier groups | 61 | |
| AccountObjectBankAccount | Bank accounts of AccountObjects | 54 | → AccountObject |
| AccountDefault | Default account mappings per voucher type | 423 | → Account |
| AccountTransfer | Period-end transfer rules (closing entries) | 20 | |
| InventoryItem | Products, Materials, Services | 78,491 | ← InventoryItemUnitConvert, PurchaseUnitPrice |
| InventoryItemCategory | Item categories | 123 | |
| InventoryItemType | Item types (goods/service/material) | 4 | |
| Unit | Units of measurement | 87 | ← SAPolicyPrice, PULastedUnitPrice |
| CCY | Currencies | 169 | |
| Bank | Banks | 28 | |
| BankAccount | Company bank accounts | 14 | ← EBTransferInfo |
| Stock | Warehouses | 52 | |
| Location | Locations | 11,712 | |
| OrganizationUnit | Departments / Branches | 12 | ← EMPAdvance*, Image |
| OrganizationUnitInfo | Extended org unit info | 38 | → OrganizationUnit |
| BudgetItem | Budget items | 29 | |
| ExpenseItem | Expense items | 17 | ← ProjectWorkCostEstimate |
| Job | Jobs/Work orders | 56,678 | ← JobProduct |
| JobProduct | Products per job | 4,066 | → Job |
| ProjectWork | Construction projects | 0 | |
| ListItem | Custom list items | 27 | |
| PaymentTerm | Payment terms | 29 | |
| AutoBusiness | Auto journal entry templates | 81 | |
| PurchasePurpose | Purchase purposes (VAT categories) | 5 | ← PUVoucherDetail, BAWithdrawDetailTax |
| DebtPeriod | Debt tracking periods | 15 | |
| ContractStatus | Contract statuses | 4 | |

<!-- [MOD:GL] -->
### MOD:GL — General Ledger & Journals

Central accounting — all vouchers post journal entries here.

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| GLVoucher | General journal voucher header | 4,860 | ← GLVoucherDetail, GLVoucherCrossEntryDetail |
| GLVoucherDetail | Journal entry lines (Debit/Credit) | 11,450 | → GLVoucher |
| GLVoucherCrossEntryDetail | Cross-entry details | 80,657 | → GLVoucher |
| GLVoucherCrossEntryDetail_OtherInfo | Extra info for cross entries | — | → GLVoucherCrossEntryDetail |
| GLVoucherDetailAdvancedPayment | Advance payment settlement lines | 713 | → GLVoucher |
| GLVoucherDetailDebtPayment | Debt payment details | — | → GLVoucher |
| GLVoucherDetailExpenses | Expense allocation in GL | 0 | → GLVoucher |
| GLVoucherDetailExpensesAllocation | Expense distribution | — | → GLVoucher |
| GLVoucherDetailForeignExchange | FX gain/loss entries | 0 | → GLVoucher |
| GLVoucherDetailTax | Tax entries in GL | 350 | → GLVoucher, TACareerGroup |
| GLVoucherList | Voucher list header | — | ← GLVoucherListDetail |
| GLVoucherListDetail | Voucher list lines | 0 | → GLVoucherList |
| GLParalellVoucher | Parallel voucher (dual-book) | — | |
| GeneralLedger | **Posted ledger** (denormalized) | — | Main query target for reports |
| CustomFieldLedger | Custom fields on ledger | — | |

<!-- [MOD:SA] -->
### MOD:SA — Sales

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| SAOrder | Sales order header | 31,043 | ← SAOrderDetail |
| SAOrderDetail | Sales order lines | 88,079 | → SAOrder |
| SAOrderDetailExpense | Order-level expenses | — | → SAOrder |
| SAVoucher | Sales voucher (delivery) header | 53,768 | ← SAVoucherDetail |
| SAVoucherDetail | Sales voucher lines | 161,207 | → SAVoucher, SAInvoice, TACareerGroup |
| SAInvoice | Sales invoice header | 13,369 | ← SAInvoiceDetail, SAInvoiceReference |
| SAInvoiceDetail | Invoice lines | 8,143 | → SAInvoice, TACareerGroup |
| SAInvoiceReference | Invoice references/links | 8,701 | → SAInvoice |
| SAReturn | Sales return header | 1,926 | ← SAReturnDetail; → PUInvoice |
| SAReturnDetail | Sales return lines | 3,272 | → SAReturn, PurchasePurpose, TACareerGroup |
| SAReturnInwardReferenceDetail | Return ↔ Inward mapping | 3,263 | → SAReturnDetail, INInwardDetail |
| SAQuote | Sales quote header | 630 | ← SAQuoteDetail |
| SAQuoteDetail | Quote lines | 4,470 | → SAQuote |
| SADiscount | Discount voucher header | 424 | ← SADiscountDetail |
| SADiscountDetail | Discount lines | 745 | → SADiscount, SAInvoice, TACareerGroup |
| SAAllocation | Revenue allocation | — | ← SAAllocationDetail* |
| SAAllocationConfig | Allocation config | — | |
| SAPolicy | Sales pricing policy | 0 | ← SAPolicySaleGroup |
| SAPolicySaleGroup | Policy ↔ customer groups | 0 | → SAPolicy |
| SAPolicySaleCustomer | Policy ↔ customers | 0 | → SAPolicySaleGroup |
| SAPolicyPrice | Policy prices | 0 | → SAPolicySaleGroup, Unit |
| SASaleGroup | Sales groups | 0 | ← SASaleGroupDetail |
| SAInvoiceRequest | Invoice issuance request | — | ← SAInvoiceRequestDetail |
| SAVoucherRequest | Voucher request | — | ← SAVoucherRequestDetail |
| SaleOutwardReference | Sale ↔ Outward mapping | 34,384 | |
| SaleOutwardReferenceDetail | Detail mapping Sale ↔ Outward | 132,003 | → SAVoucherDetail, INOutwardDetail |
| SaleLedger | Posted sales ledger | 0 | |

<!-- [MOD:PU] -->
### MOD:PU — Purchases

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| PUOrder | Purchase order header | 19,811 | ← PUOrderDetail |
| PUOrderDetail | PO lines | 61,859 | → PUOrder |
| PUVoucher | Purchase voucher header | 28,573 | ← PUVoucherDetail; → PUInvoice |
| PUVoucherDetail | PV lines | 88,792 | → PUVoucher, PurchasePurpose, PUInvoice |
| PUVoucherDetailCost | Landed cost on PV | 115 | → PUVoucher |
| PUInvoice | Purchase invoice header | 3,640 | ← PUInvoiceDetail, PUDiscount |
| PUInvoiceDetail | PI lines | 24 | → PUInvoice, PurchasePurpose |
| PUReturn | Purchase return header | 252 | ← PUReturnDetail; → SAInvoice |
| PUReturnDetail | Return lines | 343 | → PUReturn, PurchasePurpose, SAInvoice |
| PUService | Service purchase header | 109 | ← PUServiceDetail |
| PUServiceDetail | Service lines | 248 | → PUService, PurchasePurpose |
| PUDiscount | Purchase discount header | 9 | → PUInvoice; ← PUDiscountDetail |
| PUDiscountDetail | Discount lines | 9 | → PUDiscount, PurchasePurpose, PUInvoice |
| PUContract | Purchase contract | 0 | ← PUContractDetailInventoryItem |
| PUContractDetailInventoryItem | Contract item lines | — | → PUContract |
| PULastedUnitPrice | Last purchase price tracker | 10,923 | → AccountObject, InventoryItem, Unit |
| PUDebtPeriod | Purchase debt periods | 40 | |
| PurchaseLedger | Posted purchase ledger | 0 | |

<!-- [MOD:CA] -->
### MOD:CA — Cash Management (Quỹ)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| CAReceipt | Cash receipt voucher header | 2,312 | ← CAReceiptDetail |
| CAReceiptDetail | Receipt lines | 2,763 | → CAReceipt, DebtAgreement |
| CAPayment | Cash payment voucher header | 6,782 | ← CAPaymentDetail* |
| CAPaymentDetail | Payment lines | 8,137 | → CAPayment, DebtAgreement |
| CAPaymentDetailSalary | Salary payment lines | — | → CAPayment |
| CAPaymentDetailTax | Tax payment lines | 1 | → CAPayment, PurchasePurpose |
| CAPaymentDetailImportVAT | Import VAT payment | 0 | → CAPayment |
| CAPaymentDetailPersonalIncomeTax | PIT payment | 0 | → CAPayment |
| CAReceiptPaymentList | Receipt/Payment list | 11,106 | |
| CAAudit | Cash audit header | 0 | ← CAAuditDetail, CAAuditMemberDetail |
| CACashbook | Cash book | — | |
| CACashflowForeCastList | Cash flow forecast | — | ← CACashflowForeCastDetail |
| CABAReasonType | Receipt/Payment reason types | 16 | |

<!-- [MOD:BA] -->
### MOD:BA — Bank Transactions

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| BADeposit | Bank deposit header | 23,426 | ← BADepositDetail |
| BADepositDetail | Deposit lines | 24,478 | → BADeposit, DebtAgreement |
| BADepositWithdrawList | Deposit/Withdraw list | 30,933 | |
| BAWithDraw | Bank withdrawal header | 7,144 | ← BAWithDrawDetail* |
| BAWithDrawDetail | Withdrawal lines | 11,258 | → BAWithDraw, DebtAgreement |
| BAWithDrawDetailSalary | Salary withdrawal lines | — | → BAWithDraw |
| BAWithdrawDetailTax | Tax withdrawal lines | — | → BAWithDraw, PurchasePurpose |
| BAWithdrawDetailImportVAT | Import VAT withdrawal | 0 | → BAWithDraw |
| BAWithdrawDetailPersonalIncomeTax | PIT withdrawal | 0 | → BAWithDraw |
| BAInternalTransfer | Internal bank transfer | 363 | ← BAInternalTransferDetail |
| BAInternalTransferDetail | Transfer lines | 660 | → BAInternalTransfer |
| BAReconcile | Bank reconciliation | 0 | |
| EBTransferInfo | E-Banking transfer info | — | → BankAccount |
| EBTransferInfoLog | E-Banking transfer log | — | → EBTransferInfo |
| EBBankBranch | Bank branch directory | — | |
| EBBankReference | Bank reference codes | — | |

<!-- [MOD:IN] -->
### MOD:IN — Inventory / Warehouse

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| INInward | Goods receipt (stock-in) header | 79,697 | ← INInwardDetail |
| INInwardDetail | Inward lines | 96,826 | → INInward |
| INOutward | Goods issue (stock-out) header | 130,940 | ← INOutwardDetail |
| INOutwardDetail | Outward lines | 399,046 | → INOutward |
| INTransfer | Stock transfer header | 5,441 | ← INTransferDetail |
| INTransferDetail | Transfer lines | 28,865 | → INTransfer |
| INAssemblyDisassembly | Assembly/Disassembly header | 250 | ← INAssemblyDisassemblyDetail |
| INAssemblyDisassemblyDetail | Assembly lines | 284 | → INAssemblyDisassembly |
| INProductionOrder | Production order header | 83,477 | ← INProductionOrderProduct |
| INProductionOrderProduct | Production products | 95,287 | → INProductionOrder |
| INProductionOrderDetail | Production material lines | 96,741 | → INProductionOrderProduct |
| INAudit | Inventory audit header | 221 | ← INAuditDetail, INAuditMemberDetail |
| INAuditDetail | Audit count lines | 26,141 | → INAudit |
| INAuditMemberDetail | Audit committee members | 14 | → INAudit |
| INInventoryBook | Inventory book | — | ← INInventoryBookDetail |
| INInwardOutwardList | Inward/Outward summary list | 226,193 | |
| INSerialNumber | Serial number tracking | 0 | |
| InventoryBalance | Inventory balance snapshot | 120,561 | |
| InventoryLedger | Posted inventory ledger | — | |
| InventoryItemPriceFIFO | FIFO price calculation | — | |
| InventoryItemPriceOutwardImmediate | Immediate outward pricing | — | |
| InventoryItemHistory | Item transaction history | — | |

<!-- [MOD:FA] -->
### MOD:FA — Fixed Assets (TSCĐ)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| FixedAsset | Fixed asset master | 220 | ← FADepreciationDetail, FADecrementDetail, etc. |
| FixedAssetCategory | Asset categories | 16 | |
| FixedAssetDetail | Asset detail/components | 0 | → FixedAsset |
| FixedAssetDetailAccessory | Accessories | — | → FixedAsset |
| FixedAssetDetailAllocation | Allocation to departments | 220 | → FixedAsset |
| FixedAssetDetailSource | Source documents | 67 | → FixedAsset |
| FixedAssetAttachment | Attachments | 5 | → FixedAsset |
| FixedAssetLedger | Posted FA ledger | 0 | |
| FADepreciation | Depreciation run header | 53 | ← FADepreciationDetail |
| FADepreciationDetail | Depreciation per asset | 4,529 | → FADepreciation, FixedAsset |
| FADepreciationDetailAllocation | Depreciation allocation | 4,529 | → FADepreciation, FixedAsset |
| FADepreciationDetailPost | Depreciation posting | 296 | → FADepreciation |
| FADecrement | Asset disposal header | 8 | ← FADecrementDetail |
| FADecrementDetail | Disposal lines | 13 | → FADecrement, FixedAsset |
| FADecrementDetailPost | Disposal posting | 17 | → FADecrement |
| FAAdjustment | Asset revaluation header | 2 | ← FAAdjustmentDetail |
| FAAdjustmentDetail | Revaluation lines | 3 | → FAAdjustment, FixedAsset |
| FATransfer | Asset transfer header | 1 | ← FATransferDetail |
| FATransferDetail | Transfer lines | 2 | → FATransfer, FixedAsset (self-ref on RefDetailID) |
| FAAudit | Asset audit header | — | ← FAAuditDetail |
| FAChangeFinancialLeasingToOwner | Finance lease → owned | — | → FixedAsset |

<!-- [MOD:JC] -->
### MOD:JC — Job Costing (Giá thành)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| JCPeriod | Costing period | 33 | ← JCPeriodDetail, JCCostAllocationConfig, etc. |
| JCPeriodDetail | Period detail per job/project | 65,362 | → JCPeriod |
| JCCostAllocationDetail | Cost allocation results | **852,590** | → JCPeriodDetail |
| JCCostAllocationConfig | Allocation configuration | — | → JCPeriod |
| JCCostVoucher | Cost-related vouchers | 8,406 | → JCPeriod |
| JCExpenseTranfer | Expense transfer header | 8 | → JCPeriod; ← JCExpenseTranferDetail |
| JCExpenseTranferDetail | Transfer lines | 167,786 | → JCExpenseTranfer |
| JCUncomplete | WIP (Work in Progress) header | 52,625 | → JCPeriodDetail |
| JCUncompleteDetail | WIP detail | 52,625 | → JCUncomplete |
| JCUncompleteDetailInventoryItem | WIP material detail | 52,625 | → JCUncompleteDetail |
| JCProductCostDetail | Finished product cost | 40,553 | → JCPeriodDetail |
| JCProductCostAllocationConfig | Product cost config | 247 | → JCPeriodDetail |
| JCAllocationQuantumConfig | Allocation quantum config | 76,528 | |
| JCProjectAllocationQuantumConfig | Project allocation config | — | |
| JCOPN | Opening balances for JC | 505 | |
| JCOPNConfig | OPN configuration | 1 | |
| JCAccepted | Accepted/completed works | — | → JCPeriod |

<!-- [MOD:PA] -->
### MOD:PA — Payroll (Tiền lương)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| PASalarySheet | Salary sheet header | 0 | ← PASalarySheetDetail |
| PASalarySheetDetail | Salary per employee | 0 | → PASalarySheet |
| PASalarySheetColumn | Salary sheet columns | 0 | → PASalarySheetTemplate |
| PASalarySheetTemplate | Sheet templates | — | |
| PASalarySheetTemplateUser | User-specific templates | — | → PASalarySheetTemplate |
| PASalaryExpense | Salary expense header | 0 | ← PASalaryExpenseDetail |
| PASalaryExpenseDetail | Expense lines | 0 | → PASalaryExpense |
| PASalaryExpenseAllocation | Salary allocation | 0 | → PASalarySheet |
| PASalaryExpenseAllocationDetail | Allocation detail | — | → PASalaryExpenseAllocation |
| PASalaryTaxInsuranceRegulation | Tax/insurance rules | 0 | |
| PATimeSheet | Timesheet header | 0 | ← PATimeSheetDetail |
| PATimeSheetDetail | Timesheet lines | 0 | → PATimeSheet |
| PATimeSheetSummary | Timesheet summary | 0 | ← PATimeSheetSummaryDetail |
| TimeSheetSign | Timesheet signs/symbols | 23 | |

<!-- [MOD:TA] -->
### MOD:TA — Tax (Thuế)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| TADeclaration | Tax declaration header | 3 | → TATemplate; ← TADeclarationAppendix |
| TADeclarationAppendix | Declaration appendices | 8 | → TADeclaration, TAAppendixType |
| TADeclarationDetail | Declaration detail | 84 | → TADeclaration |
| TADeclarationGeneral | General info | 78 | → TADeclaration |
| TADeclarationConfig | Declaration config | 22 | |
| TATemplate | Tax form templates | — | |
| TAAppendixType | Appendix types | 90 | |
| TACareerGroup | Industry groups (for special tax) | 5 | ← GLVoucherDetailTax, SAInvoiceDetail, etc. |
| TACareerList | Career/industry list | 13 | |
| TaxRate | Tax rates | 0 | |
| TaxLedger | Posted tax ledger | — | |
| TaxLocation | Tax locations | 12,633 | |
| TA_01xGTGT_Detail | VAT declaration details (Form 01) | varies | → TADeclarationAppendix |
| TA_012GTGT_Detail | VAT purchase details | 443 | → TADeclarationAppendix |
| TA_012GTGT_DetailVoucher | VAT voucher details | 1,005 | → TA_012GTGT_Detail |
| TA_TNCN_05xBKDetail | PIT appendix details | 0 | → TADeclarationAppendix |
| TA_BC26AC_InvoiceStatement | Invoice statement | 0 | |
| TANoTaxableGoods | Non-taxable goods | — | → TADeclaration |
| ResourcesTaxTable | Tax resource tables | 1,075 | |
| ResourcesTaxTableDetail | Tax resource details | 1,177 | |
| PersonalIncomeTaxRate | PIT brackets | 7 | |

<!-- [MOD:SU] -->
### MOD:SU — Supplies & Tools (CCDC - Công cụ dụng cụ)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| SUIncrement | Tool/supply receipt header | 173 | ← SUIncrementDetail* |
| SUIncrementDetail | Receipt lines | 4 | → SUIncrement |
| SUIncrementDetailAllocation | Allocation to departments | 173 | → SUIncrement |
| SUIncrementDetailDepartment | Department assignment | 173 | → SUIncrement |
| SUIncrementDetailSource | Source documents | 168 | → SUIncrement |
| SUDecrement | Tool disposal/write-off | 0 | ← SUDecrementDetail |
| SUDecrementDetail | Disposal lines | 0 | → SUDecrement, SUIncrement |
| SUAllocation | Allocation header | 53 | ← SUAllocationDetail* |
| SUAllocationDetailExpense | Expense allocation | 1,336 | → SUAllocation, SUIncrement |
| SUAllocationDetailPost | Posted allocation | 415 | → SUAllocation |
| SUAllocationDetailTable | Allocation table | 1,336 | → SUAllocation, SUIncrement |
| SUTransfer | Tool transfer | 0 | ← SUTransferDetail |
| SUAdjustment | Tool adjustment | — | ← SUAdjustmentDetail |
| SUAudit | Tool audit | — | ← SUAuditDetail |
| SupplyCategory | Supply categories | 1 | |
| SupplyLedger | Posted supply ledger | — | |

<!-- [MOD:CT] -->
### MOD:CT — Contracts (Hợp đồng bán)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| Contract | Sale contract header | 1,641 | ← ContractDetail*, ContractSaleDiary |
| ContractDetailContact | Contract contacts | 1,635 | → Contract |
| ContractDetailInventoryItem | Contract items | 1,683 | → Contract |
| ContractDetailRevenue | Revenue breakdown | 1,647 | → Contract |
| ContractDetailExpense | Expense breakdown | — | → Contract |
| ContractDetailExpensesLastYear | Last year expenses | — | → Contract |
| ContractSaleDiary | Sales diary per contract | 2,866 | → Contract |
| ContractHistoryDept | Historical debt | — | → Contract |
| ContractAttachment | Attachments | 0 | → Contract |
| ContractAttachmentFile | Files | — | → ContractAttachment |
| ContractReceiptAmountByDate | Receipt tracking by date | — | |
| ContractStatus | Status codes | 4 | |

<!-- [MOD:IP] -->
### MOD:IP — Invoice Publishing (Hóa đơn điện tử)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| IPRegister | Invoice registration | 0 | ← IPRegisterDetail |
| IPRegisterDetail | Registration detail | 0 | → IPRegister, IPTemplate |
| IPRegisterEInvoice | E-Invoice registration | — | |
| IPPublishAnnouncement | Publish announcement | 1 | ← IPPublishAnnouncementDetail |
| IPPublishAnnouncementDetail | Announcement detail | 1 | → IPPublishAnnouncement, IPTemplate |
| IPAdjustAnnouncement | Adjustment announcement | 0 | ← IPAdjustAnnouncementDetail |
| IPCancelAnnouncement | Cancellation announcement | 0 | |
| IPLBDAnnouncement | Lost/Damaged announcement | 0 | |
| IPTemplate | Invoice templates | 1 | → SYSReportList |
| IPUsingState | Usage status reports | — | |
| IPDeletedAnnouncement | Deleted announcements | — | |
| InvoiceBot | E-Invoice bot integration | — | |
| InvoiceBotDetail | Bot detail | — | → InvoiceBot |
| InvoiceTemplate | Invoice print templates | 10 | |
| InvType | Invoice types | 7 | |
| InvTemplate | Invoice templates | 7 | |
| EInvoiceReplacement | E-Invoice replacements | — | |
| EInvoiceStatus | E-Invoice statuses | — | |
| EIHSMConfig | HSM (digital signature) config | — | |

<!-- [MOD:FR] -->
### MOD:FR — Financial Reports (Báo cáo tài chính)

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| FRTemplate | Report template lines | 576 | |
| FRTemplateDefault | Default template lines | 576 | |
| FRReportList | Report instances | 2 | ← FRReportDetail, FRB09DN* |
| FRReportDetail | Report cell data | 237 | → FRReportList |
| FRB09DNTemplate | Balance sheet template (B09-DN) | 599 | |
| FRB09DNTemplateDefault | Default B09-DN template | 599 | |
| FRB09DNReportDetail | Balance sheet data | 601 | → FRReportList |
| FRB09DNNTemplate | Business result template (B09-DNN) | 149 | |
| FRB09DNNReportDetail | Business result data | — | → FRReportList |
| FRF01ReportDetail | Trial balance (F01) data | — | → FRReportList |
| FRObligationToGovTemplate | Government obligation template | 48 | |
| FRB03GTBussiness | Cash flow statement templates | — | |

<!-- [MOD:SYS] -->
### MOD:SYS — System & Configuration

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| SYSRefType | **Voucher type registry** (master) | 262 | → SYSRefTypeCategory; ← SYSRefTypeDetail |
| SYSRefTypeCategory | Voucher type categories | 92 | |
| SYSRefTypeDetail | Voucher type detail config | 333 | → SYSRefType, SYSRefTypeCategory |
| SYSDBOption | Database-level options/settings | 529 | |
| SYSDBInfo | Database info | 1 | |
| SYSAutoID | Auto-numbering config | 165 | |
| SYSBook | Book types (Financial/Management/Both) | 3 | |
| SYSSubSystem | Subsystem registry | 11 | |
| SYSSubSystemRefType | Subsystem ↔ RefType mapping | 237 | → SYSSubSystem |
| SYSBusinessVisible | Feature visibility config | 125 | |
| SYSNewRefNo | New reference number tracking | — | |
| SYSOperand | Formula operands | 120 | |
| MSC_User | System users | 25 | ← MSC_UserJoinRole |
| MSC_Role | Security roles | 19 | ← MSC_RolePermissionMaping, MSC_UserJoinRole |
| MSC_Permission | Permissions | 50 | ← MSC_RolePermissionMaping |
| MSC_SubSystem | Permission subsystems | 414 | ← MSC_RolePermissionMaping |
| MSC_UserJoinRole | User ↔ Role mapping | 63 | → MSC_User, MSC_Role |
| MSC_RolePermissionMaping | Role ↔ Permission mapping | 10,210 | → MSC_SubSystem, MSC_Role, MSC_Permission |
| MSC_RegisPermisionForSubSystem | Registered permissions | 1,712 | → MSC_SubSystem, MSC_Permission |
| MSC_AudittingLog | Audit log | 117,151 | ← MSC_AudittingLogDetail |
| SYSReportList | Report registry | 681 | → SYSReportGroup; ← many |
| SYSReportGroup | Report groups | 24 | |
| SYSReportTemplate | Report cell formulas | — | → SYSReportList |
| SYSReportFormula | Report formulas | 201 | → SYSReportList, SYSReportTemplate |
| SYSVoucherTemplate | Voucher layout templates | 605 | ← SYSVoucherTemplateDetail |
| SYSUserInfo | User extended info | 178 | |
| SYSUserSetting | User settings | 8 | |
| SearchConfig | Search configuration | — | |
| SearchField | Searchable fields | 36 | |
| SearchVoucher | Quick search vouchers | 11,497 | ← SearchVoucherDetail |

---

<!-- [TBL:SYSVoucherTemplate] -->
#### DEEP DIVE: SYSVoucherTemplate + SYSVoucherTemplateDetail — Metadata-Driven Form Builder

> **CRITICAL**: This is the **UI form definition system**. It defines which tabs, grids, and columns appear on every voucher form in the application. Essential for building the new webapp's form rendering engine.

**Architecture Overview:**
```
SYSVoucherTemplate (605 rows)  →  SYSVoucherTemplateDetail (1,993 rows)
       ↓                                    ↓
  1 template per RefType+VoucherType    N tabs/grids per template
  + user customizations                 TemplateConfig = XML column definitions
```

**SYSVoucherTemplate Columns:**
| Column | Type | Description |
|--------|------|-------------|
| LayoutID | uniqueidentifier PK | Template ID |
| LayoutName | nvarchar(255) | Template name (e.g., "Mẫu ngầm định", "Mẫu chuẩn", "Mẫu nhiều đơn vị tính") |
| RefType | int FK→SYSRefType | Voucher type code |
| VoucherType | tinyint | Form variant (see enum below) |
| IsSystem | bit | 1 = system default (read-only), 0 = user customized |
| IsPublic | bit | Always 0 in data |
| UserID | uniqueidentifier | Template owner (NULL for system templates) |

**SYSVoucherTemplateDetail Columns:**
| Column | Type | Description |
|--------|------|-------------|
| LayoutDetailID | uniqueidentifier PK | Detail row ID |
| LayoutID | uniqueidentifier FK→SYSVoucherTemplate | Parent template |
| GridType | tinyint | 0 = master section (2 rows), 1 = grid section (1,991 rows) |
| TabIndex | tinyint | Tab order: 0-8 |
| TabCaption | nvarchar(50) | Tab display name (Vietnamese) |
| TabVisible | bit | 1 = visible, 0 = hidden by default |
| IsTabCustomField | bit | 1 = tab for user-defined custom fields |
| GridName | nvarchar(50) | Grid control identifier |
| IsHiddenTabByVATMethod | bit | 1 = hide when business uses Direct VAT method |
| IsShowOnMaster | bit | Show on master (header) section |
| TemplateConfig | ntext | **XML grid column configuration** (2.7K–31.6K chars) |

<!-- [BR:SYS04] -->
**BR-SYS04: VoucherType Enum (Form Variants)**
| VoucherType | Meaning | Used By |
|-------------|---------|---------|
| NULL | Single layout (no variants) | FA, IN, GL, PA, CT, SU, most others |
| 0 | Special (e.g., revenue allocation) | SA 3570 |
| 1 | Variant A (typically goods/inventory forms) | PU 302-378, SA 3530-3545, Debt Offset 4014 |
| 2 | Variant B (same RefTypes as 1, different column visibility) | PU 302-378, SA 3530-3545, Debt Offset 4014 |
| 10-13 | CA Receipt sub-forms (Phiếu thu) | CA 1010 |
| 20-23 | CA Payment sub-forms (Phiếu chi) | CA 1020, 1013 (tax refund) |
| 30-34 | BA Deposit sub-forms (Thu tiền gửi) | BA 1500 |
| 40-43 | BA Payment sub-forms (UNC/Séc) | BA 1510, 1520, 1530 |

<!-- [BR:SYS05] -->
**BR-SYS05: Template Hierarchy — IsSystem vs User Templates**
- Every RefType has at least 1 system template (IsSystem=1, IsPublic=0) = "Mẫu ngầm định" (default layout)
- Users can create custom templates (IsSystem=0) = "Mẫu chuẩn" or "Mẫu nhiều đơn vị tính"
- PU/SA RefTypes with VoucherType 1+2 have both system and user variants (up to 6 per RefType)
- System: 237 templates, User: 368 templates

**Template Distribution by Module:**
| Module | RefType Range | Unique RefTypes | Templates | Details |
|--------|---------------|----------------:|----------:|--------:|
| PU (Purchase) | 301-378 | 48 | 210 | 913 |
| SA (Sales) | 3400-3571 | 34 | 128 | 498 |
| IN (Inventory) | 2010-2094 | 27 | 78 | 170 |
| BA (Bank) | 1500-1560 | 15 | 57 | 140 |
| CA (Cash) | 1010-1026 | 11 | 38 | 96 |
| GL Other | 4010-4092 | 19 | 40 | 76 |
| PU Return | 3030-3043 | 8 | 20 | 50 |
| FA (Fixed Assets) | 251-256 | 7 | 14 | 24 |
| SU (Summary/GL) | 412-454 | 5 | 10 | 14 |
| PA (Price Alloc) | 6010-6030 | 3 | 6 | 6 |
| CT (Contract) | 9040-9041 | 2 | 4 | 6 |

**Tab Types (TabCaption) — Top 15:**
| TabCaption (Vietnamese) | English | Count | Notes |
|------------------------|---------|------:|-------|
| Thông tin bổ sung | Additional Info | 500 | 499 hidden by default (custom fields tab) |
| Hàng tiền | Items & Amounts | 388 | Main detail grid for PU/SA/IN |
| Thống kê | Statistics | 258 | Dimension tracking (Job, Project, etc.) |
| Thuế | Tax | 236 | VAT/Import/Export/Special tax; 60 hidden by VAT method |
| Hạch toán | Accounting | 173 | Main grid for CA/BA/GL |
| Chi phí | Costs | 120 | PU import cost allocation |
| Phí trước hải quan | Pre-customs Charges | 61 | Import purchase docs only |
| Phân bổ | Allocation | 54 | Cost/revenue allocation |
| Giá vốn | Cost of Goods | 42 | COGS for SA vouchers |
| Chi phí mua hàng | Purchase Costs | 31 | Freight/charges for PU |

**Grid Names (22 distinct):**
| GridName | Count | Used In |
|----------|------:|---------|
| grdDetail | 1,181 | Main detail grid (all modules) |
| *(blank)* | 471 | Custom field tabs, master sections |
| grdFreight | 181 | Purchase freight allocation |
| grdImportChange | 61 | Import charge allocation |
| grdDetailOther | 60 | SA "other" charges |
| grdAccountingDetail | 38 | SA accounting entries |
| grdAccountAllocate | 27 | Cost allocation grids |
| grdReturnDetail, grdTax, grdCostDetail, etc. | 2-20 each | Specialized grids |

<!-- [BR:SYS06] -->
**BR-SYS06: TemplateConfig XML Format**
Each `TemplateConfig` contains a `<ROOT>` element with `<GridColumnConfig>` children:
```xml
<ROOT>
  <GridColumnConfig 
    Key="DebitAccount"          <!-- Maps to DB column name -->
    Hidden="0"                  <!-- 0=visible, 1=hidden -->
    IsHiddenSystem="0"          <!-- System-enforced hide -->
    IsReadOnly="0"              <!-- User can edit -->
    IsReadOnlySystem="0"        <!-- System-enforced readonly -->
    VisiblePosition="3"         <!-- Column display order -->
    Width="100"                 <!-- Column width in pixels -->
    Caption="TK Nợ"            <!-- Vietnamese display label -->
    DefaultCaption="TK Nợ"     <!-- Default caption -->
    EnglishCaption=""           <!-- English label (often empty) -->
    Fixed="0"                   <!-- Frozen column -->
    IsGroupBy="0"               <!-- Grouping column -->
    SortIndicator="None"        <!-- Sort state -->
    TabStop="1"                 <!-- Tab navigation -->
    HasGroupColumn="0"          <!-- Group header -->
    Note=""                     <!-- Developer notes -->
    IsCustomColumn="0"          <!-- User-defined column -->
    DataType="0"                <!-- Custom column data type -->
    CustomColumnSortOrder="0"   <!-- Custom column order -->
  />
  <!-- ... more GridColumnConfig elements -->
</ROOT>
```

**KEY INSIGHT: Template Key ↔ DB Column Mapping**
- XML `Key` attribute maps **directly** to database column names in detail tables
- Example: CAPayment template Key="DebitAccount" → `CAPaymentDetail.DebitAccount`
- Unmapped DB columns (never shown in UI): audit columns (Created/Modified*), IsDeleted, management-book variants (*Management), internal FK IDs (PUContractID, PUOrderID)
- Some Keys are display-only (readonly): AccountObjectName, ProjectWorkName, FixedAssetCategoryName

**Complete Column Key Vocabulary — grdDetail (141 unique keys):**

*Accounting:* DebitAccount, CreditAccount, CostAccount, DepreciationAccount, RemainingAccount, DiscountAccount, VATAccount, DeductionDebitAccount, OrgPriceAccount, EnvironmentalTaxAccount, ExportTaxAccount, ImportTaxAccount, SpecialConsumeTaxAccount, CashOutDiffAccountNumberFinance

*Amounts:* Amount, AmountOC, AmountResonableCost, AmountUnResonableCost, UnResonableCost, DiscountAmount, DiscountAmountOC, DiscountRate, FOBAmount, FOBAmountOC, FreightAmount, ImportChargeAmount, InwardAmount, RemainingAmount, DepreciationAmount, MonthlyDepreciationAmount, AccumDepreciationAmount, CashOutAmountFinance, CashOutDiffAmountFinance, CashOutDiffVATAmountFinance, CashOutVATAmountFinance, CashOutExchangeRateFinance, ExchangeRateOperator

*Tax:* VATRate, VATAmount, VATAmountOC, VATDescription, VATRateOther, ImportTaxRate, ImportTaxAmount, ImportTaxRatePrice, ImportTaxRatePriceOC, ExportTaxRate, ExportTaxAmount, ExportTaxAmountOC, SpecialConsumeTaxRate, SpecialConsumeTaxAmount, EnvironmentalTaxAmount, ImportChargeBeforeCustomAmountMainCurrency, ImportChargeBeforeCustomAmountOC, ImportChargeExchangeRate

*Inventory:* InventoryItemCode, Quantity, UnitID, UnitPrice, UnitPriceAfterTax, MainQuantity, MainUnitID, MainUnitPrice, ConvertRate, MainConvertRate, UnitConvert, UnitPriceConvert, QuantityConvert, QuantityReceipt, QuantityReceiptLastYear, StockID, LotNo, ExpiryDate, SerialNumber1, SerialNumber2, Warranty, InventoryResaleTypeID, PanelQuantity, PanelHeightQuantity, PanelLengthQuantity, PanelRadiusQuantity, PanelWidthQuantity

*Dimensions:* AccountObjectCode, AccountObjectID, AccountObjectName, OrganizationUnitID, FromOrganizationUnitID, ToOrganizationUnitID, ExpenseItemID, BudgetItemID, BudgetItemName, JobID, ProjectWorkID, ProjectWorkName, OrderID, ContractNo, ContractID, ContractCode, DebtAgreementID, ListItemID, PurchasePurposeID, ProductionCode, ProductionOrderRefNo

*Invoice:* InvTemplateNo, InvSeries, InvNo, InvDate, TaxAccountObjectCode, TaxAccountObjectName, TaxAccountObjectTaxCode, TaxAccountObjectAddress

*References:* PUOrderRefNo, PUContractCode, SAOrderRefNo, RefID, RefDetailID, SortOrder, Description

*Fixed Assets:* FixedAssetCode, FixedAssetName, FixedAssetCategoryName

*Custom Fields:* CustomField1–10 (+ Vietnamese aliases "Trường mở rộng 1–10")

**Additional Keys in Specialized Grids (not in grdDetail):**

*grdFreight / grdImportChange:* AccountObjectName, AccumulatedAllocateAmount, AccumulatedAllocateAmountOC, Amount, AmountOC, CurrencyID, ExchangeRate, PostedDate, RefDate, RefNoFinance, TotalFreightAmount, TotalFreightAmountOC

*grdDetailOther:* PayableAmount, RemainningAmount, VoucherPostedDate, VoucherRefDate, VoucherRefNoFinance, VoucherRefNoManagement

*Tax Tab (Thuế):* NotInVATDeclaration, TurnoverAmount, VATRate406 (special rate for direct VAT)

**IsHiddenTabByVATMethod Logic:**
- 76 tabs flagged: 60 "Thuế" (Tax) + 16 "Thông tin bổ sung" (Additional Info)
- When `SYSDBOption.CalculateMethodOnVAT = direct_method`, flagged tabs are hidden
- Affects PU/SA/CA/BA vouchers that have Tax tabs

---

<!-- [MOD:EMP] -->
### MOD:EMP — Employee Advance & Requests

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| EMPAdvancePayment | Advance payment received | — | → OrganizationUnit (Dept, Branch) |
| EMPAdvancePaymentDetail | Detail lines | — | → EMPAdvancePayment |
| EMPAdvanceRequest | Advance request | — | → OrganizationUnit |
| EMPAdvanceRequestDetail | Request lines | — | → EMPAdvanceRequest |
| EMPPaymentRequest | Payment request | — | → OrganizationUnit |
| EMPPaymentRequestDetail | Request lines | — | → EMPPaymentRequest |

<!-- [MOD:MISC] -->
### MOD:MISC — Other / Cross-module

| Table | Description | Rows | Key FKs |
|-------|-------------|-----:|---------|
| VoucherReference | Cross-voucher references | 369,131 | |
| VoucherType | Voucher type descriptors | 36 | |
| VoucherTypeCategory | Voucher type categories | 23 | |
| VoucherTypeCategoryRefType | Category ↔ RefType | 131 | → VoucherTypeCategory, SYSRefType |
| HistoryVoucher | Voucher edit history | — | |
| HistoryRecentPrice | Recent price history | 700 | |
| FileAttachment | General file attachments | — | |
| DocumentManager | Document management | — | |
| ScheduleAppointment | Appointments | — | |
| ScheduleTask | Tasks | — | |
| DebtAgreement | Debt agreements | — | ← BADepositDetail, CAReceiptDetail, etc. |
| DebtList | Debt lists | 0 | ← DebtListDetail |
| PrepaidExpenses | Prepaid expenses | 0 | ← PrepaidExpensesDetail |
| CompanySearch | Company search | 336 | |
| ComparisonReport | Comparison reports | 13 | |
| BOAllocationExpense | Overhead allocation | — | ← BOAllocationExpenseDetail |
| BUExpenditure | Budget expenditure | — | ← BUExpenditureDetail |
| ProfitSalePlaning | Sales/profit planning | — | |
| DataTableDictionary | Table metadata dictionary | 327 | |
| DataColumnDictionary | Column metadata dictionary | 107 | |
| ConfigChangeDictionary | Change config mapping | 655 | |
| ImportDictionary | Import column name mapping (VN) | 1,085 | |
| MappingInfoForDictionary | Dictionary mapping info | — | |
| ImportTable/ImportItem/ImportColumn | Excel import config | varies | |

---

## TABLE DEFINITIONS

> Each table has a unique TAG: `[TBL:TableName]`
> FK references use: `ref:[TBL:TargetTable]`
> Related rules use: `ref:[BR:RuleCode]`

<!-- [TBL:Account] -->
### Account — Chart of Accounts (Hệ thống tài khoản)

| Column | Type | Null | Description | Notes |
|--------|------|:----:|-------------|-------|
| AccountID | uniqueidentifier | ❌ | PK | GUID |
| AccountNumber | nvarchar(20) | ❌ | Account code | VN standard: 111, 112, 131, 331, 511... |
| AccountName | nvarchar(128) | ❌ | Name (Vietnamese) | "Tiền mặt", "Tiền gửi NH"... |
| AccountNameEnglish | nvarchar(128) | ✅ | Name (English) | "Cash in hand", "Cash in bank"... |
| ParentID | uniqueidentifier | ✅ | FK → self (tree) | NULL = root account |
| Grade | int | ✅ | Account level | 1=parent, 2=child, 3=grandchild |
| IsParent | bit | ❌ | Has children? | 1=summary, 0=postable |
| AccountCategoryKind | int | ❌ | Debit/Credit nature | 0=Debit, 1=Credit (inferred) |
| Inactive | bit | ❌ | Disabled? | |
| IsPostableInForeignCurrency | bit | ❌ | Multi-currency | |
| DetailByAccountObject | bit | ❌ | Track by customer/supplier | |
| AccountObjectType | int | ✅ | 0=None, 1=Supplier, 2=Customer, 3=Employee | |
| DetailByBankAccount | bit | ❌ | Track by bank account | |
| DetailByJob | bit | ❌ | Track by job | |
| DetailByProjectWork | bit | ❌ | Track by project | |
| DetailByOrder | bit | ❌ | Track by order | |
| DetailByContract | bit | ❌ | Track by contract | |
| DetailByExpenseItem | bit | ❌ | Track by expense item | |
| DetailByDepartment | bit | ❌ | Track by department | |
| DetailByListItem | bit | ❌ | Track by custom list | |
| DetailByPUContract | bit | ❌ | Track by purchase contract | |
| IsDeleted | bit | ❌ | Soft delete | ref:[BR:SYS01] |
| RowVersion | int | ❌ | Optimistic concurrency | |
| MISACodeID | nvarchar(100) | ✅ | MISA standard code mapping | |
| RevenueAccountID | uniqueidentifier | ✅ | Linked revenue account | |

<!-- [FK:Account] -->
**Relations:**
- self.ParentID → self.AccountID (tree structure)
- ◄ ref:[TBL:AccountDefault].DefaultValue → self.AccountNumber
- ◄ ref:[TBL:GeneralLedger] (via AccountNumber)
- ◄ ref:[TBL:GLVoucherDetail] (via DebitAccount/CreditAccount)

**Rules:** ref:[BR:DI01], ref:[BR:DI02], ref:[BR:DI03], ref:[BR:DI04]

<!-- [TBL:AccountObject] -->
### AccountObject — Customers/Suppliers/Employees (Đối tượng)

Unified table for all business partners. Type determined by columns.

| Column (key) | Type | Description |
|--------|------|-------------|
| AccountObjectID | uniqueidentifier | PK |
| AccountObjectCode | nvarchar(25) | Unique code |
| AccountObjectName | nvarchar(255) | Name |
| AccountObjectType | int | 0=Vendor, 1=Customer, 2=Employee, 3=Both |
| IsCustomer | bit | Is customer |
| IsVendor | bit | Is vendor |
| IsEmployee | bit | Is employee |
| Address | nvarchar(255) | |
| TaxCode | nvarchar(50) | Tax ID |
| Tel | nvarchar(50) | Phone |
| Email | nvarchar(100) | |
| BranchID | uniqueidentifier | FK → OrganizationUnit |
| AccountObjectGroupID | uniqueidentifier | FK → AccountObjectGroup |

**FK:** ← AccountObjectBankAccount, ← PULastedUnitPrice

<!-- [TBL:InventoryItem] -->
### InventoryItem — Products/Materials/Services (Vật tư hàng hóa)

| Column (key) | Type | Description |
|--------|------|-------------|
| InventoryItemID | uniqueidentifier | PK |
| InventoryItemCode | nvarchar(25) | Unique code |
| InventoryItemName | nvarchar(255) | Name |
| InventoryItemCategoryID | uniqueidentifier | FK → InventoryItemCategory |
| InventoryItemType | int | 0=Raw material, 1=Product, 2=Goods, 3=Service... |
| UnitID | uniqueidentifier | FK → Unit |
| UnitPrice | decimal | Default unit price |
| SalePrice1/2/3 | decimal | Sale prices |
| IsFollowSerial | bit | Track serial numbers |
| TaxRate | decimal | Default VAT rate |
| BaseOnFormula | uniqueidentifier | FK → InventoryQuantityFormulaTemplate |

**FK:** ← InventoryItemUnitConvert, ← InventoryItemPurchaseUnitPrice, ←PULastedUnitPrice, ← InventoryItemDetail*

*(More table definitions will be added as we survey deeper — the above covers the core master data tables)*

---

## FOREIGN KEY MAP

> Complete FK relationship listing (250+ relationships)
> Format: ChildTable.Column → ParentTable.Column

### Master Data FKs
```
AccountDefault.DefaultValue ──────────→ Account.AccountNumber
AccountObjectBankAccount.AccountObjectID → AccountObject.AccountObjectID
InventoryItem.BaseOnFormula ──────────→ InventoryQuantityFormulaTemplate.FormulaID
InventoryItemDetail*.InventoryItemID ─→ InventoryItem.InventoryItemID
InventoryItemPurchase*.InventoryItemID → InventoryItem.InventoryItemID
InventoryItemUnitConvert.InventoryItemID → InventoryItem.InventoryItemID
PULastedUnitPrice.AccountObjectID ────→ AccountObject.AccountObjectID
PULastedUnitPrice.InventoryItemID ────→ InventoryItem.InventoryItemID
PULastedUnitPrice.UnitID ────────────→ Unit.UnitID
OrganizationUnitInfo.OrganizationUnitID → OrganizationUnit.OrganizationUnitID
Image.OrganizationUnitID ────────────→ OrganizationUnit.OrganizationUnitID
EBTransferInfo.FromBankAccountID ────→ BankAccount.BankAccountID
JobProduct.JobID ────────────────────→ Job.JobID
ProjectWorkCostEstimate.ProjectWorkID → ProjectWork.ProjectWorkID
ProjectWorkCostEstimate.ExpenseItemID → ExpenseItem.ExpenseItemID
```

### Cash Module FKs
```
CAReceiptDetail.RefID ───────────────→ CAReceipt.RefID
CAReceiptDetail.DebtAgreementID ─────→ DebtAgreement.DebtAgreementID
CAPaymentDetail.RefID ───────────────→ CAPayment.RefID
CAPaymentDetail.DebtAgreementID ─────→ DebtAgreement.DebtAgreementID
CAPaymentDetailSalary.RefID ─────────→ CAPayment.RefID
CAPaymentDetailTax.RefID ───────────→ CAPayment.RefID
CAPaymentDetailTax.PurchasePurposeID → PurchasePurpose.PurchasePurposeID
CACashflowForeCastDetail.RefID ─────→ CACashflowForeCastList.RefID
CAAuditDetail.RefID ────────────────→ CAAudit.RefID
CAAuditMemberDetail.RefID ──────────→ CAAudit.RefID
```

### Bank Module FKs
```
BADepositDetail.RefID ───────────────→ BADeposit.RefID
BADepositDetail.DebtAgreementID ─────→ DebtAgreement.DebtAgreementID
BAWithDrawDetail.RefID ──────────────→ BAWithDraw.RefID
BAWithDrawDetail.DebtAgreementID ────→ DebtAgreement.DebtAgreementID
BAWithdrawDetailTax.PurchasePurposeID → PurchasePurpose.PurchasePurposeID
BAInternalTransferDetail.RefID ──────→ BAInternalTransfer.RefID
EBTransferInfoLog.RefID ────────────→ EBTransferInfo.RefID
```

### GL Module FKs
```
GLVoucherDetail.RefID ───────────────→ GLVoucher.RefID
GLVoucherCrossEntryDetail.GLVoucherRefID → GLVoucher.RefID
GLVoucherDetailAdvancedPayment.RefID → GLVoucher.RefID
GLVoucherDetailDebtPayment.RefID ───→ GLVoucher.RefID
GLVoucherDetailExpenses*.RefID ──────→ GLVoucher.RefID
GLVoucherDetailForeignExchange.RefID → GLVoucher.RefID
GLVoucherDetailTax.RefID ──────────→ GLVoucher.RefID
GLVoucherDetailTax.TACareerGroupID ─→ TACareerGroup.TACareerGroupID
GLVoucherListDetail.RefID ──────────→ GLVoucherList.RefID
```

### Sales Module FKs
```
SAOrderDetail.RefID ────────────────→ SAOrder.RefID
SAVoucherDetail.RefID ──────────────→ SAVoucher.RefID
SAVoucherDetail.SAInvoiceRefID ─────→ SAInvoice.RefID
SAVoucherDetail.TACareerGroupID ────→ TACareerGroup.TACareerGroupID
SAInvoiceDetail.RefID ──────────────→ SAInvoice.RefID
SAInvoiceReference.SAInvoiceRefID ──→ SAInvoice.RefID
SAReturnDetail.RefID ───────────────→ SAReturn.RefID
SAReturnDetail.PurchasePurposeID ───→ PurchasePurpose.PurchasePurposeID
SAReturn.PUInvoiceRefID ───────────→ PUInvoice.RefID
SADiscountDetail.SAInvoiceRefID ────→ SAInvoice.RefID
SAQuoteDetail.RefID ────────────────→ SAQuote.RefID
SaleOutwardReferenceDetail.SAVoucherRefDetailID → SAVoucherDetail.RefDetailID
SaleOutwardReferenceDetail.INOutwardRefDetailID → INOutwardDetail.RefDetailID
SAPolicySaleGroup.SAPolicyID ───────→ SAPolicy.SAPolicyID
SAPolicyPrice.SAPolicySaleGroupID ──→ SAPolicySaleGroup.SAPolicySaleGroupID
SAPolicyPrice.UnitID ──────────────→ Unit.UnitID
```

### Purchase Module FKs
```
PUOrderDetail.RefID ────────────────→ PUOrder.RefID
PUVoucher.PUInvoiceRefID ──────────→ PUInvoice.RefID
PUVoucherDetail.RefID ──────────────→ PUVoucher.RefID
PUVoucherDetail.PurchasePurposeID ──→ PurchasePurpose.PurchasePurposeID
PUVoucherDetail.PUInvoiceRefID ────→ PUInvoice.RefID
PUVoucherDetailCost.RefID ─────────→ PUVoucher.RefID
PUInvoiceDetail.RefID ──────────────→ PUInvoice.RefID
PUReturnDetail.RefID ───────────────→ PUReturn.RefID
PUReturn.SAInvoiceRefID ───────────→ SAInvoice.RefID
PUServiceDetail.RefID ──────────────→ PUService.RefID
PUDiscount.PUInvoiceRefID ─────────→ PUInvoice.RefID
PUContractDetailInventoryItem ──────→ PUContract.PUContractID
```

### Inventory Module FKs
```
INInwardDetail.RefID ───────────────→ INInward.RefID
INOutwardDetail.RefID ──────────────→ INOutward.RefID
INTransferDetail.RefID ─────────────→ INTransfer.RefID
INAssemblyDisassemblyDetail.RefID ──→ INAssemblyDisassembly.RefID
INProductionOrderProduct.RefID ─────→ INProductionOrder.RefID
INProductionOrderDetail.ProductionID → INProductionOrderProduct.ProductionID
INAuditDetail.RefID ────────────────→ INAudit.RefID
INInventoryBookDetail.InventoryBookID → INInventoryBook.InventoryBookID
SAReturnInwardReferenceDetail.InwardRefDetailID → INInwardDetail.RefDetailID
```

### Fixed Asset FKs
```
FADepreciationDetail.RefID ─────────→ FADepreciation.RefID
FADepreciationDetail.FixedAssetID ──→ FixedAsset.FixedAssetID
FADecrementDetail.FixedAssetID ────→ FixedAsset.FixedAssetID
FAAdjustmentDetail.FixedAssetID ───→ FixedAsset.FixedAssetID
FATransferDetail.FixedAssetID ─────→ FixedAsset.FixedAssetID
FixedAssetDetail*.FixedAssetID ────→ FixedAsset.FixedAssetID
```

### Job Costing FKs
```
JCPeriodDetail.JCPeriodID ─────────→ JCPeriod.JCPeriodID
JCCostAllocationDetail.JCPeriodDetailID → JCPeriodDetail.JCPeriodDetailID
JCCostVoucher.JCPeriodID ──────────→ JCPeriod.JCPeriodID
JCExpenseTranfer.JCPeriodID ───────→ JCPeriod.JCPeriodID
JCExpenseTranferDetail.RefID ──────→ JCExpenseTranfer.RefID
JCUncomplete.JCPeriodDetailID ────→ JCPeriodDetail.JCPeriodDetailID
JCProductCostDetail.JCPeriodDetailID → JCPeriodDetail.JCPeriodDetailID
```

### Tax FKs
```
TADeclaration.TemplateID ──────────→ TATemplate.TemplateID
TADeclarationAppendix.RefID ───────→ TADeclaration.RefID
TADeclarationAppendix.AppendixTypeID → TAAppendixType.AppendixTypeID
TA_01xGTGT_Detail.AppendixID ─────→ TADeclarationAppendix.AppendixID
TA_012GTGT_DetailVoucher.RefDetailID → TA_012GTGT_Detail.RefDetailID
```

### Contract FKs
```
ContractDetail*.ContractID ────────→ Contract.ContractID
ContractSaleDiary.ContractID ──────→ Contract.ContractID
ContractHistoryDept.ContractID ────→ Contract.ContractID
ContractAttachment.ContractID ─────→ Contract.ContractID
ContractAttachmentFile.AttachmentID → ContractAttachment.AttachmentID
```

### System/Security FKs
```
MSC_UserJoinRole.UserID ───────────→ MSC_User.UserID
MSC_UserJoinRole.RoleID ───────────→ MSC_Role.RoleID
MSC_RolePermissionMaping.RoleID ───→ MSC_Role.RoleID
MSC_RolePermissionMaping.PermissionID → MSC_Permission.PermissionID
MSC_RolePermissionMaping.SubSystemCode → MSC_SubSystem.SubSystemCode
SYSRefType.RefTypeCategory ────────→ SYSRefTypeCategory.RefTypeCategory
SYSRefTypeDetail.RefType ──────────→ SYSRefType.RefType
SYSReportList.GroupID ─────────────→ SYSReportGroup.GroupID
SYSReportFormula.ReportID ─────────→ SYSReportList.ReportID
IPTemplate.MISAReportID ──────────→ SYSReportList.ReportID
VoucherTypeCategoryRefType.RefType → SYSRefType.RefType
```

---

## DATA FLOWS

<!-- [FLOW:Purchase] -->
### FLOW:Purchase — Purchase → Stock-in → Payment

```
PUOrder ─────────────────────────────────────────────────────────┐
(Purchase Order)                                                  │
    │                                                             │
    ▼                                                             │
PUVoucher ──→ PUVoucherDetail ──→ INInward ──→ INInwardDetail    │
(Purchase     (lines with        (Stock-in     (items received)   │
 Voucher)      account entries)   voucher)                        │
    │                                                             │
    ├──→ PUInvoice ──→ PUInvoiceDetail                           │
    │    (Vendor invoice, tax info)                                │
    │                                                             │
    ▼                                                             │
CAPayment / BAWithDraw ──→ Detail lines                          │
(Cash/Bank payment to supplier)                                   │
    │                                                             │
    ▼                                                             │
GeneralLedger (posted entries: Debit 152/156 | Credit 331)       │
InventoryLedger (posted inventory movements)                      │
VoucherReference (cross-links between all vouchers) ◄────────────┘
```

<!-- [FLOW:Sales] -->
### FLOW:Sales — Sales → Stock-out → Collection

```
SAOrder ──────→ SAOrderDetail                              
(Sales Order)   (order lines)                              
    │                                                      
    ▼                                                      
SAVoucher ───→ SAVoucherDetail ──→ INOutward → INOutwardDetail
(Sales         (lines with        (Stock-out    (items issued)
 Voucher)       account entries)   voucher)
    │
    ├──→ SAInvoice ──→ SAInvoiceDetail
    │    (Customer invoice, e-invoice)
    │         │
    │         ├──→ IPPublishAnnouncement (e-invoice publishing)
    │         └──→ SAInvoiceReference (invoice links)
    │
    ▼
CAReceipt / BADeposit ──→ Detail lines
(Cash/Bank receipt from customer)
    │
    ▼
GeneralLedger (posted: Debit 131 | Credit 511/512)
SaleOutwardReference ──→ SaleOutwardReferenceDetail
(links SAVoucherDetail ↔ INOutwardDetail)
```

<!-- [FLOW:Journal] -->
### FLOW:Journal — Journal Entry → Ledger → Reports

```
GLVoucher ──→ GLVoucherDetail (Debit/Credit lines)
    │              │
    │              ├──→ GLVoucherDetailTax (VAT entries)
    │              ├──→ GLVoucherDetailAdvancedPayment
    │              ├──→ GLVoucherDetailDebtPayment
    │              └──→ GLVoucherCrossEntryDetail
    │
    ▼
GeneralLedger (denormalized posted data — main query source)
    │
    ▼
FRTemplate / FRReportDetail
(Financial statements: B01-DN Balance Sheet,
 B02-DN Income Statement, B03-DN Cash Flow)
```

<!-- [FLOW:Period] -->
### FLOW:Period — Period Close → Financial Statements

```
AccountTransfer (closing entry rules)
    │
    ▼
GLVoucher (closing journal entries: Revenue→P&L, Expense→P&L)
    │
    ▼
JCPeriod ──→ JCPeriodDetail ──→ JCCostAllocationDetail (852K rows!)
(Job costing period calculation)
    │
    ▼
FRReportList ──→ FRReportDetail / FRB09DNReportDetail
(Generate financial reports: Balance Sheet, P&L)
    │
    ▼
TADeclaration ──→ TADeclarationAppendix ──→ TA_*_Detail
(Tax declarations: VAT, CIT, PIT)
```

<!-- [FLOW:FixedAsset] -->
### FLOW:FixedAsset — Asset Lifecycle

```
FixedAsset (master record)
    │
    ├──→ FADepreciation ──→ FADepreciationDetail (monthly depreciation)
    │         └──→ FADepreciationDetailAllocation (allocate to departments)
    │         └──→ FADepreciationDetailPost (journal entries)
    │
    ├──→ FAAdjustment ──→ FAAdjustmentDetail (revaluation)
    │
    ├──→ FATransfer ──→ FATransferDetail (transfer between depts)
    │
    └──→ FADecrement ──→ FADecrementDetail (disposal/sale)
              └──→ FADecrementDetailPost (disposal entries)
```

---

## BUSINESS RULES SUMMARY

> Single source of truth. Each rule defined once with TAG `[BR:Code]`

<!-- [BR:DI01] -->
**BR-DI01**: Account tree structure — child AccountNumber starts with parent's number
- Source: Sample data (111 → 1111, 1112; 112 → 1121, 1122)
- Tables: ref:[TBL:Account]
- ParentID is a self-referencing FK; Grade indicates depth level

<!-- [BR:DI02] -->
**BR-DI02**: Only leaf accounts (IsParent=0) can be used in journal entries
- Source: Inferred from IsParent column design
- Tables: ref:[TBL:Account], ref:[TBL:GLVoucherDetail]

<!-- [BR:DI03] -->
**BR-DI03**: Account "DetailBy*" flags control what sub-ledger tracking is required
- Source: Account table has 10+ DetailBy* boolean columns
- Example: DetailByAccountObject=1 on account 131 means every 131 entry must specify a customer
- Tables: ref:[TBL:Account]

<!-- [BR:DI04] -->
**BR-DI04**: AccountObject is a unified table — same entity can be Customer + Supplier + Employee
- Source: IsCustomer, IsVendor, IsEmployee are independent flags
- Tables: ref:[TBL:AccountObject]

<!-- [BR:GL01] -->
**BR-GL01**: Master-Detail pattern — all vouchers use RefID as the FK from detail to master
- Source: Universal pattern across all modules (CAPaymentDetail.RefID → CAPayment.RefID, etc.)
- Tables: All voucher tables

<!-- [BR:GL02] -->
**BR-GL02**: Soft delete pattern — IsDeleted flag, never physical delete
- Source: Account.IsDeleted, account search often filters IsDeleted=0
- Tables: Most master data tables

<!-- [BR:GL03] -->
**BR-GL03**: All IDs are uniqueidentifier (GUID) — not auto-increment integers
- Source: AccountID, InventoryItemID, RefID are all GUIDs
- Impact: Client generates IDs before insert; no server-side auto-ID

<!-- [BR:GL04] -->
**BR-GL04**: Optimistic concurrency via RowVersion column
- Source: Account.RowVersion, many tables have RowVersion
- Pattern: Check RowVersion on update, reject if changed

<!-- [BR:GL05] -->
**BR-GL05**: Multi-book support — SYSBook has 3 entries (Financial/Management/Both)
- Source: SYSBook table (3 rows)
- Tables: ref:[TBL:SYSBook]

<!-- [BR:GL06] -->
**BR-GL06**: Cloud sync pattern — UploadState, ServerRowVersion columns
- Source: Account has UploadState, ServerRowVersion columns
- Purpose: MISA cloud sync mechanism

<!-- [BR:SA01] -->
**BR-SA01**: Sales flow: Order → Voucher → Invoice → Return (each step references prior)
- Source: SAVoucherDetail.SAInvoiceRefID, SAReturn.PUInvoiceRefID (cross-module)
- Tables: ref:[TBL:SAOrder], ref:[TBL:SAVoucher], ref:[TBL:SAInvoice], ref:[TBL:SAReturn]

<!-- [BR:SA02] -->
**BR-SA02**: Sale-Outward link via SaleOutwardReference/SaleOutwardReferenceDetail
- Source: FK to SAVoucherDetail.RefDetailID and INOutwardDetail.RefDetailID
- Purpose: Link sales voucher lines to stock-out lines 1:1

<!-- [BR:PU01] -->
**BR-PU01**: Purchase flow: Order → Voucher → Invoice → Return
- Source: PUVoucher.PUInvoiceRefID, PUVoucherDetail.PUInvoiceRefID
- Similar pattern to sales

<!-- [BR:IN01] -->
**BR-IN01**: Inventory tracked by FIFO pricing
- Source: InventoryItemPriceFIFO*, InventoryItemPriceFIFOAdjust* tables
- Tables: Multiple FIFO-related tables

<!-- [BR:IN02] -->
**BR-IN02**: Production uses 3-level hierarchy: Order → Product → Detail(materials)
- Source: INProductionOrder → INProductionOrderProduct → INProductionOrderDetail
- Very high volume: 83K orders, 95K products, 96K material lines

<!-- [BR:FA01] -->
**BR-FA01**: Fixed asset depreciation runs monthly with allocation to departments
- Source: FADepreciationDetailAllocation table
- Tables: ref:[TBL:FADepreciation], ref:[TBL:FADepreciationDetailAllocation]

<!-- [BR:JC01] -->
**BR-JC01**: Job costing is the most data-intensive module (852K allocation rows)
- Source: JCCostAllocationDetail is the largest table
- Tables: ref:[TBL:JCCostAllocationDetail]

<!-- [BR:TA01] -->
**BR-TA01**: Tax declarations follow Vietnam tax forms (01/GTGT, 03/TNDN, etc.)
- Source: TA_* table naming matches VN tax form numbering
- GTGT = VAT, TNDN = Corporate Income Tax, TNCN = Personal Income Tax, TTDB = Special Consumption Tax

<!-- [BR:SYS01] -->
**BR-SYS01**: SYSRefType is the central voucher type registry (262 types)
- Source: All voucher operations reference RefType codes
- Controls: which master/detail tables to use, posting behavior, subsystem assignment

<!-- [BR:SYS02] -->
**BR-SYS02**: Audit trail — MSC_AudittingLog tracks all user actions (117K entries)
- Source: MSC_AudittingLog + MSC_AudittingLogDetail
- Tables: ref:[TBL:MSC_AudittingLog]

<!-- [BR:SYS03] -->
**BR-SYS03**: RBAC security — User → Role → Permission per SubSystem
- Source: MSC_User → MSC_UserJoinRole → MSC_Role → MSC_RolePermissionMaping
- 25 users, 19 roles, 50 permissions, 10,210 permission mappings

<!-- [BR:GL07] -->
**BR-GL07**: Posting Engine is Configuration-Driven via SYSPostMapping
- Each master table is mapped to target ledger(s) and posting function(s) in SYSPostMapping
- Adding a new voucher type requires: create tables + create Func_POST_* function + insert SYSPostMapping row
- Source: SYSPostMapping (80+ rows), Proc_Post_GetLedgerData (dynamic SQL)

<!-- [BR:GL08] -->
**BR-GL08**: Double-Entry — Each voucher detail generates BOTH debit and credit GeneralLedger rows
- One detail line → 2 GeneralLedger rows (DebitAmount>0/CreditAmount=0 + DebitAmount=0/CreditAmount>0)
- AccountNumber comes from D.DebitAccount and D.CreditAccount respectively
- Pattern: Func_POST_*GeneralLedger uses UNION ALL for debit/credit halves

<!-- [BR:GL09] -->
**BR-GL09**: Dual-Book Posting controlled by DisplayOnBook + IsPostToManagementBook
- DisplayOnBook=0 → Finance book only, =1 → Management book only, =2 → Both
- Posting functions accept @IsPostToManagementBook parameter
- Finance and Management books use SAME GeneralLedger table with BookType discriminator

<!-- [BR:GL10] -->
**BR-GL10**: Decimal Precision is centrally controlled via SYSDBOption
- ALL posting functions read SYSDBOption for rounding: AmountDecimalDigits(0), AmountOCDecimalDigits(3), UnitPriceDecimalDigits(2), QuantityDecimalDigits(2), ExchangeRateDecimalDigits(2)
- ROUND(value, @DecimalDigits) applied to every monetary calculation
- New system MUST replicate exact decimal behavior to match MISA ledger values

<!-- [BR:IN02] -->
**BR-IN02**: Inventory outward price = Weighted Average (DefaultCostMethod=0)
- After posting, Proc_Post_AfterPostAndUnpost triggers inventory price recalculation
- IsCaculateOutwardPriceOld=1 → uses legacy calculation method
- Price recalculation cascades to all subsequent outward vouchers after the posted date

<!-- [BR:SA02] -->
**BR-SA02**: E-Invoice integration tracks publish status and ND406 tax reduction
- View_SAVoucher resolves EInvoiceStatus (0=NotPublished, 1=Publishing, 2=Published, 3=Failed, 4=Replaced)
- IsReductionInvoice flag (ND406) marks 2% VAT reduction invoices
- Cannot delete voucher with published e-invoice (Proc_CheckExistInvoiceIPCancelBeforeDeleteVoucher)

---

## VIEWS

> 107 views total. Sorted by complexity (definition length).
> Architecture: Views are the main data access layer for list screens and reports.

### Mega Views (System-Critical)

| View | Size | Pattern | Purpose |
|------|-----:|---------|---------|
| View_Search_Voucher | 308K | 86× UNION ALL | **Universal voucher search** across ALL modules. Sources ALL 90+ voucher tables (CA*, BA*, GL*, SA*, PU*, IN*, FA*, SU*, JC*, Opening*). Used for global search screen. |
| View_ProcessUnPosted_Voucher | 291K | 84× UNION ALL | Clone of View_Search_Voucher for unposted voucher listing. Filters IsPosted=0. |
| View_Post_AllMasterVoucher | 18K | 30× UNION ALL | Lists ALL master voucher headers with posting status (IsPostedFinance, IsPostedManagement). Each UNION branch: `SELECT TableName, RefType, DisplayOnBook, BranchID, RefID, PostedDate, IsPosted* FROM [MasterTable]`. Used by posting engine. |
| View_ProcessUnPosted | 31K | | Simplified unposted voucher list |

### Cash & Bank Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_CACashBook | 2.9K | CAReceiptPaymentList + SYSRefType | UNION of unposted-finance + unposted-management entries. Separates by DisplayOnBook (0=Finance, 1=Management, 2=Both). Shows payer/receiver via CASE on CAType. |
| View_CACashBook_Posted | 2.9K | CACashbook | Posted cash book entries |
| View_CABookReceiptPayment | 1.3K | CAReceipt/CAPayment | Receipt/payment book |
| View_CAReceiptPayment | 2.4K | CAReceiptPaymentList | Combined receipt/payment list |
| View_CAReceiptPaymentListForPostCACashBook | 10K | Multiple CA/BA/SA/PU tables | Complex: aggregates all cash-affecting vouchers for cash book posting |
| View_CashOut_MasterVoucher | 7.8K | Multiple master tables | Cash outflow master vouchers |
| View_BADepositWithDraw | 2.7K | BADeposit + BAWithDraw | Bank deposit/withdrawal combined |
| View_BAWithDraw | 1.3K | BAWithDraw + AccountObject | Withdrawal details with counterparty |
| View_EBTransferInfo | 6.5K | EBTransferInfo + BankAccount | E-Banking transfer details |

### Sales & Purchase Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_SAVoucher | 9.8K | SAVoucher + AccountObject×3 + SYSRefType + SAInvoiceReference + SAInvoice + EInvoiceStatus + SAInvoiceRequest | Main sales voucher list. JOINs to resolve customer, employee, supplier names. Includes e-invoice status (PublishStatus 0-4), outward export status, tax reduction (ND406). |
| View_SAInvoice | 14K | SAInvoice + related | Sales invoice with full detail |
| View_SAOrder | 5.9K | SAOrder + related | Sales orders |
| View_SAReturn | 3.6K | SAReturn + related | Sales returns |
| View_SADiscount | 2.6K | SADiscount + SAInvoice | Sales discounts |
| View_SAQuote | — | SAQuote + related | Sales quotes |
| View_SAAllocation | 0.8K | SAAllocation | Revenue allocation |
| View_PUVoucher | 6.2K | PUVoucher + PUVoucherDetail + AccountObject | Purchase voucher. Uses **GROUP BY** with SUM for totals (Amount, ImportTax, VAT, Discount, Freight). Handles `IsPULotVoucher` flag for lot purchases. |
| View_PUVoucherService | 14K | PUService + extended joins | Service purchase vouchers |
| View_PUOrder | 2.6K | PUOrder + related | Purchase orders |
| View_PUInvoice | 2.3K | PUInvoice | Purchase invoices |
| View_PUReturn | 2.7K | PUReturn | Purchase returns |
| View_PUDiscount | 2.2K | PUDiscount | Purchase discounts |
| View_PUContract | 4.3K | PUContract + related | Purchase contracts |

### Inventory Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_INInwardOutward | 3.1K | INInward UNION ALL INOutward + SYSRefType | Combined inward/outward. Adds `INType` (0=Inward, 1=Outward). Resolves RevenueStatus for outward type 220 (sales delivery). |
| View_INInwardOutwardList | 5.3K | INInwardOutwardList | Summary list view |
| View_INInwardOutwardList_Posted | 3.7K | INInwardOutwardList | Posted only |
| View_INBook | 4.0K | INInventoryBook | Inventory book view |
| View_INBook_Posted | 3.6K | INInventoryBook | Posted inventory book |
| View_DetailedInventoryBooks | 19.8K | Multiple IN tables | Complex detailed inventory books |
| View_INAssemblyDisassembly | 6.0K | INAssemblyDisassembly + related | Assembly/disassembly operations |
| View_InTransfer | 4.7K | INTransfer + related | Stock transfers |
| View_IN_InwardVoucher | 1.7K | INInward + related | Inward voucher details |
| View_InventoryLedger_LotNo | 0.8K | InventoryLedger | Lot-level tracking |
| View_INProductionOrderProduct | 1.5K | INProductionOrderProduct | Production order products |

### GL & Financial Report Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_GLVoucher | 1.9K | GLVoucher + SYSRefType | Simple: joins voucher header with RefTypeName. Exposes DisplayOnBook, IsPosted*, CustomField1-10. |
| View_GLVoucher_VATAmount | 0.4K | GLVoucher | VAT amount on GL vouchers |
| View_GeneralLedger_SumByProjectWorkID | 0.7K | GeneralLedger | Ledger grouped by project |
| View_EInvoice | 28.6K | Multiple SA/PU tables + EInvoiceStatus | Complex e-invoice combined view |
| View_RelationVoucher | 3.7K | VoucherReference + related | Cross-voucher relations |
| View_VoucherReference | 2.5K | VoucherReference | Voucher reference links |
| View_VoucherRefNoFinance | 16K | Multiple master tables | Finance ref number lookup |

### Contract Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_CTContract_Finance | 99K | Contract + many ledger queries | Contract finance data with actual receipt/payment calculation from GeneralLedger. The 2nd/3rd largest views. |
| View_CTContract_Management | 95K | Contract + management ledger | Same as above for management book |
| View_CTContract | 4.6K | Contract + related | Basic contract list |
| View_ContractInventoryDetail | 3.4K | ContractDetailInventoryItem | Contract items |

### Master Data Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_DIAccountObject | 5.0K | AccountObject + AccountObjectGroup + related | Full account object with group info |
| View_DIAccountObject_Employee | 4.6K | AccountObject filtered | Employee subset |
| View_DIInventoryItem | 3.0K | InventoryItem + InventoryItemCategory | Items with category |
| View_DIInventoryItemByParam | 3.7K | InventoryItem + related | Parameterized item view |
| View_DIInventoryItemCategory | 0.7K | InventoryItemCategory | Item categories with parent |
| View_DIViewAllocationObject | 4.7K | Multiple master tables | Allocation target objects (items, FA, supplies) |
| View_OrganizationUnitWithBranch | 1.0K | OrganizationUnit self-join | Org units with parent branch name |

### System & Posting Views

| View | Size | Source Tables | Logic |
|------|-----:|---------------|-------|
| View_Search_Voucher | 308K | ALL 90+ voucher tables | **See Mega Views above** |
| View_Search_Voucher_ForReconcile | 13K | Subset of voucher tables | Bank reconciliation search |
| View_Post_AllMasterVoucher | 18K | ALL 30 master tables | **See Mega Views above** |
| View_ProcessUnPosted | 31K | Cached unposted list | Unposted voucher listing |
| View_MSC_RegisPermisionForSubSystemDetail | 0.7K | MSC_RegisPermisionForSubSystem + MSC_SubSystem | Permission registration detail |
| View_SYS_AllVoucherReference | 2.3K | VoucherReference | All cross-voucher references |

### Key Architecture Patterns (from View Analysis)

1. **Dual-book pattern**: Most views have parallel Finance/Management book logic via `DisplayOnBook` (0=Finance, 1=Management, 2=Both) and `IsPostedFinance`/`IsPostedManagement` flags
2. **SYSRefType JOIN**: Almost every view JOINs `SYSRefType` to resolve `RefTypeName` from `RefType` integer code
3. **Mega UNION pattern**: System views (Search, Post, ProcessUnPosted) are massive UNION ALLs across all module tables — one SELECT per voucher type
4. **Soft delete filter**: Views commonly filter `IsDeleted = 0` and `UploadState = 1` (synced)
5. **View is display layer**: Views add computed columns like status names (Vietnamese: "Đã lập"/"Chưa lập"), resolved FKs (employee name, customer name), and aggregated totals

---

## STORED PROCEDURES & FUNCTIONS

> 5,410 SPs total. 304 functions. Deep analysis below.
> Platform: MISA AMIS ACT2, Version 2017, MVC 72.0.0.3

### SP Category Breakdown

| Category | Count | Description |
|----------|------:|-------------|
| Report SPs | **4,443** | 82% of all SPs — data retrieval for printed/screen reports |
| DI (Master Data) | 587 | CRUD for master data entities |
| SA (Sales) | 524 | Sales module operations + reports |
| PU (Purchase) | 246 | Purchase module operations + reports |
| JC (Job Cost) | 246 | Cost calculation + allocation |
| CT (Contract) | 173 | Contract management + reports |
| IP (Invoice Publishing) | 170 | E-invoice operations |
| TA (Tax) | 149 | Tax declaration generation |
| SU (Supply) | 142 | Supply/tool management |
| FA (Fixed Asset) | 135 | Asset lifecycle + depreciation |
| SYNC | 109 | Cloud sync operations |
| GL (General Ledger) | 90 | Core accounting operations |
| EI (E-Invoice) | 75 | E-invoice integration |
| IN (Inventory) | 69 | Stock management + pricing |
| OPN (Opening) | 69 | Opening balance operations |
| EB (E-Banking) | 37 | E-banking integration |
| NewDB (Setup) | 24 | New database initialization |
| CA (Cash) | 20 | Cash book operations |
| Posting | ~90 | Post/Unpost vouchers (see below) |
| Check/Validate | ~40 | Data validation |
| Async | ~26 | Background sync tasks |

### SP Naming Convention

| Prefix | Module | Example |
|--------|--------|---------|
| `Proc_BAR_*` | Bank reports | Proc_BAR_GetBACashDetailBook |
| `Proc_CAR_*` | Cash reports | Proc_CAR_GetCACashDetailBook, Proc_CAR_GetCACashFlow |
| `Proc_GLR_*` | GL & Financial reports | Proc_GLR_GetB01_DN (Balance Sheet), Proc_GLR_GetB02_DN (Income Statement), Proc_GLR_GetB03_DN (Cash Flow), Proc_GLR_GetB09_DN (Notes), Proc_GLR_GetF01 (Trial Balance) |
| `Proc_INR_*` | Inventory reports | Proc_INR_GetINInventoryBalanceSummary |
| `Proc_INV_*` | Inventory vouchers | Proc_INV_Get01_VT (Stock Card), Proc_INV_Get02_VT |
| `Proc_SAR_*` | Sales reports | Proc_SAR_GetSalesSummaryByCustomer |
| `Proc_SAV_*` | Sales voucher print | Proc_SAV_GetDataSAInvoiceWithAssembly_SME2012 |
| `Proc_PUR_*` | Purchase reports | Proc_PUR_GetPUDetailPurchaseByInventoryItem |
| `Proc_FAR_*` | Fixed asset reports | Proc_FAR_GetS21_DN (Asset Register) |
| `Proc_JCR_*` | Job costing reports | Proc_JCR_GetJCSummaryCostByProjectWork |
| `Proc_CTR_*` | Contract reports | Proc_CTR_GetDebtContractDetailBySale |
| `Proc_TAR_*` | Tax reports | Proc_TAR_GetFollowTax |
| `Proc_PAR_*` | Payroll reports | Proc_PAR_PASalarySummaryByEmployee |
| `Proc_SUR_*` | Supply reports | Proc_SUR_GetTrackingToolBook |
| `Proc_EI_*` | E-Invoice integration | Proc_EI_GetMultiFullEInvoice (268K — the largest non-report SP) |
| `Proc_Post_*` | Posting engine | Proc_Post_GetLedgerData, Proc_Post_AfterPostAndUnpost |
| `Proc_Check_*` | Validation | Proc_CheckExistsRefNo, Proc_CheckErrorResultDataConvert |
| `Proc_Async_*` | Cloud sync | Proc_Async_GetDataSyncing |
| `Proc_Sync*` | Sync data | Proc_SyncV2_GetSyncDataByTableAndTime |
| `Proc_NewDB_*` | DB initialization | Proc_NewDB_GetClosingAccountObjectAsOpening |
| `Proc_DI_*` | Master data ops | Proc_DI_ChangePostAccount |

### Vietnamese Financial Report SPs (Thông tư 200)

| SP | VN Report Code | English Name |
|----|---------------|--------------|
| Proc_GLR_GetB01_DN | B01-DN | Balance Sheet (Bảng cân đối kế toán) |
| Proc_GLR_GetB02_DN | B02-DN | Income Statement (Báo cáo kết quả HĐKD) |
| Proc_GLR_GetB03_DN | B03-DN Direct | Cash Flow Statement — Direct Method |
| Proc_GLR_GetB03_DN_Indirect | B03-DN Indirect | Cash Flow Statement — Indirect Method |
| Proc_GLR_GetB09_DN | B09-DN | Notes to Financial Statements (Thuyết minh) |
| Proc_GLR_GetF01 | F01 | Trial Balance (Bảng cân đối phát sinh) |

### Posting Engine Architecture

<!-- [SP:PostingEngine] -->

The posting engine is the most critical subsystem. It transforms voucher data into ledger entries.

**Core workflow:**
```
1. User saves voucher (e.g., CAPayment)
2. User clicks "Post" (Ghi sổ)
3. Proc_Post_GetVoucherInfo → retrieves voucher metadata
4. Proc_Post_GetLedgerData → calls the appropriate Func_POST_* function
5. Func_POST_*GeneralLedger → generates debit/credit entries from voucher data
6. Data inserted into GeneralLedger + specialized ledgers
7. Proc_Post_AfterPostAndUnpost → updates related data:
   - Updates TaxLedger for purchase vouchers
   - Updates InventoryBalance for stock vouchers
   - Recalculates FIFO/weighted-average prices
   - Inserts SYSPostRelationInLedger (cross-references)
```

**SYSPostMapping — Posting Configuration Table:**
Maps each voucher master table to its posting function(s) and target ledger(s).

| Master Table | Target Ledger | Posting Function |
|-------------|---------------|-----------------|
| CAPayment | GeneralLedger | Func_POST_CAPaymentGeneralLedger |
| CAPayment | TaxLedger | Func_POST_CAPaymentTaxLedger |
| CAPayment | CustomFieldLedger | Func_Post_GetLedgerData_CustomFieldLedger |
| CAReceipt | GeneralLedger | Func_POST_CAReceiptGeneralLedger |
| BADeposit | GeneralLedger | Func_POST_BADepositGeneralLedger |
| BAWithDraw | GeneralLedger | Func_POST_BAWithDrawGeneralLedger |
| BAWithDraw | TaxLedger | Func_POST_BAWithDrawTaxLedger |
| BAInternalTransfer | GeneralLedger | Func_POST_BAInternalTransferGeneralLedger |
| GLVoucher | GeneralLedger | Func_POST_GLVoucherGeneralLedger |
| GLVoucher | TaxLedger | Func_POST_GLVoucherTaxLedger |
| SAVoucher | GeneralLedger | Func_POST_SAVoucherGeneralLedger |
| SAVoucher | SaleLedger | Func_POST_SAVoucherSaleLedger |
| SAReturn | GeneralLedger | Func_POST_SAReturnGeneralLedger |
| SAReturn | SaleLedger | Func_POST_SAReturnSaleLedger |
| SADiscount | GeneralLedger | Func_POST_SADiscountGeneralLedger |
| SADiscount | SaleLedger | Func_POST_SADiscountSaleLedger |
| SAInvoice | TaxLedger | Func_POST_SAInvoiceTaxLedger |
| SAAllocation | GeneralLedger | Func_POST_SAAllocationGeneralLedger |
| PUVoucher | GeneralLedger | Func_POST_PUVoucherGeneralLedger |
| PUVoucher | TaxLedger | Func_POST_PUVoucherTaxLedger |
| PUVoucher | PurchaseLedger | Func_POST_PUVoucherPurchaseLedger |
| PUVoucher | InventoryLedger | Func_POST_PUVoucherInventoryLedger |
| PUReturn | GeneralLedger | Func_POST_PUReturnGeneralLedger |
| PUReturn | PurchaseLedger | Func_POST_PUReturnPurchaseLedger |
| PUReturn | InventoryLedger | Func_POST_PUReturnInventoryLedger |
| PUDiscount | GeneralLedger | Func_POST_PUDiscountGeneralLedger |
| PUDiscount | PurchaseLedger | Func_POST_PUDiscountPurchaseLedger |
| PUDiscount | InventoryLedger | Func_POST_PUDiscountInventoryLedger |
| PUInvoice | TaxLedger | Func_POST_PUInvoiceTaxLedger |
| PUInvoice | GeneralLedger | Func_POST_PUInvoiceGeneralLedger |
| PUService | GeneralLedger | Func_POST_PUServiceGeneralLedger |
| PUService | TaxLedger | Func_POST_PUServiceTaxLedger |
| PUService | PurchaseLedger | Func_POST_PUServicePurchaseLedger |
| INInward | GeneralLedger | Func_POST_INInwardGeneralLedger |
| INInward | InventoryLedger | Func_POST_INInwardInventoryLedger |
| INOutward | GeneralLedger | Func_POST_INOutwardGeneralLedger |
| INOutward | InventoryLedger | Func_POST_INOutwardInventoryLedger |
| INTransfer | GeneralLedger | Func_POST_INTransferGeneralLedger |
| INTransfer | InventoryLedger | Func_POST_INTransferInventoryLedger |
| FADepreciation | GeneralLedger | Func_POST_FADepreciationGeneralLedger |
| FADepreciation | FixedAssetLedger | Func_POST_FADepreciationFixedAssetLedger |
| FADecrement | GeneralLedger | Func_POST_FADecrementGeneralLedger |
| FADecrement | FixedAssetLedger | Func_POST_FADecrementFixedAssetLedger |
| FAAdjustment | GeneralLedger | Func_POST_FAAdjustmentGeneralLedger |
| FAAdjustment | FixedAssetLedger | Func_POST_FAAdjustmentFixedAssetLedger |
| FATransfer | FixedAssetLedger | Func_POST_FATransferFixedAssetLedger |
| FixedAsset | FixedAssetLedger | Func_POST_FixedAssetFixedAssetLedger |
| FAChangeFinancialLeasingToOwner | GeneralLedger + FixedAssetLedger | Func_POST_FAChange*GeneralLedger / *FixedAssetLedger |
| SUIncrement | SupplyLedger | Func_POST_SUIncrementSupplyLedger |
| SUDecrement | SupplyLedger | Func_POST_SUDecrementSupplyLedger |
| SUAllocation | GeneralLedger | Func_POST_SUAllocationGeneralLedger |
| SUAllocation | SupplyLedger | Func_POST_SUAllocationSupplyLedger |
| SUTransfer | SupplyLedger | Func_POST_SUTransferSupplyLedger |
| SUAdjustment | SupplyLedger | Func_POST_SUAdjustmentSupplyLedger |
| JCExpenseTranfer | GeneralLedger | Func_POST_JCExpenseTranferGeneralLedger |
| JCAccepted | GeneralLedger | Func_POST_JCAcceptGeneralLedger |
| OpeningAccountEntry | GeneralLedger | Func_POST_OpeningAccountEntryGeneralLedger |
| OpeningInventoryEntry | InventoryLedger | Func_POST_OpeningInventoryEntryInventoryLedger |

**6 Target Ledger Tables:**

| Ledger | Purpose | Source Modules |
|--------|---------|----------------|
| **GeneralLedger** | Main accounting ledger (all journal entries) | ALL modules |
| **TaxLedger** | Tax-specific entries (VAT input/output) | CA, BA, GL, PU, SA |
| **InventoryLedger** | Inventory movement & valuation | IN, PU |
| **PurchaseLedger** | Purchase tracking | PU |
| **SaleLedger** | Sales tracking | SA |
| **FixedAssetLedger** | Asset value & depreciation | FA |
| **SupplyLedger** | Tool/supply tracking | SU |
| **CustomFieldLedger** | User-defined fields | ALL modules |

**Posting Function Internals (Func_POST_*GeneralLedger):**

Each function follows the same pattern (analyzed from `Func_POST_CAPaymentGeneralLedger`):

1. Returns a table matching `GeneralLedger` schema (~80 columns)
2. Reads `SYSDBOption` for decimal precision settings:
   - `AmountDecimalDigits` (0) — converted amount rounding
   - `AmountOCDecimalDigits` (3) — original currency rounding
   - `UnitPriceDecimalDigits` (2), `QuantityDecimalDigits` (2)
   - `ExchangeRateDecimalDigits` (2)
   - `MainCurrency` (VND)
3. JOINs Master (M) + Detail (D) + Account (ACC) tables:
   ```sql
   FROM dbo.CAPayment M
   INNER JOIN dbo.CAPaymentDetail D ON M.RefID = D.RefID
   INNER JOIN dbo.Account ACC ON ACC.AccountNumber = D.DebitAccount
   WHERE M.RefID = @RefID
   ```
4. Generates **2 rows per detail line** (UNION ALL):
   - **Debit entry**: AccountNumber = D.DebitAccount, DebitAmount = D.Amount, CreditAmount = 0
   - **Credit entry**: AccountNumber = D.CreditAccount, CreditAmount = D.Amount, DebitAmount = 0
5. Handles foreign currency: If `ACC.IsPostableInForeignCurrency = 0`, uses MainCurrency (VND) and Amount (not AmountOC)
6. Resolves redundant info from master data (AccountObjectName, EmployeeCode, BankName, etc.)

### Cash Book Posting (Proc_CA_PostCACashBook)

Separate posting flow for the cash book specifically:
- Takes a list of RefIDs (`;`-separated GUIDs)
- Inserts into `CACashbook` table (not GeneralLedger)
- Determines currency by checking if any detail line has a Debit account starting with `111%` that `IsPostableInForeignCurrency = 1`
- Handles receipts (CAReceipt), payments (CAPayment), sales with cash (SAVoucher), purchase returns with cash (PUReturn), purchase discounts with cash (PUDiscount)

### Inventory Pricing SPs

| SP | Purpose |
|----|---------|
| Proc_Post_CorrectDataBeforeCaculatePrice | Corrects assembly inward amounts to match outward material costs. Only for RefType=2011 (assembly inward). |
| Proc_IN_UpdatePrice_Oward_IM_Insert_WhenPosted_MultiVouchers | Recalculates weighted-average outward prices after posting |

**DefaultCostMethod** (from SYSDBOption):
- `0` = Weighted Average (Bình quân gia quyền) — **Active for this database**
- `1` = Immediate Weighted Average (BQGQ tức thời)
- `2` = FIFO
- `3` = Specific Identification

### Key Business Logic SPs (Non-Report)

| SP | Size | Purpose |
|----|-----:|---------|
| Proc_EI_GetMultiFullEInvoice | 268K | Generate full e-invoice data for batch publishing |
| Proc_EI_GetMultiFullEInvoiceByCompanyCode | 377K | Same, filtered by company code |
| Proc_BAV_GetDebitAnnouncement | 106K | Bank debit announcement printout |
| Proc_DI_ChangePostAccount | 49K | Change account numbers in posted data (dangerous!) |
| Proc_NewDB_GetClosingAccountObjectAsOpening | 48K | Year-end: generate opening balances from closing |
| Proc_GetNew_SAAllocationDetailTable | 56K | Revenue allocation calculation |
| Proc_IP_UpdateSAInvoiceByIPDeletedAnnouncement | 56K | Update invoice status after deletion announcement |
| Proc_GetCACashBook | 45K | Cash book data retrieval |
| Proc_SA_GetPaging_GetAccountObjectDebt | 42K | Customer debt paging query |
| Proc_JC_GetOPNByType | 42K | Job costing opening balances |
| Proc_GL_Get_GLPostedVoucher | 37K | Get posted voucher details |
| Proc_Post_UnpostAllVoucher | 19K | Unpost all vouchers (period close reset) |
| Proc_Post_ClearLedger | 11K | Clear ledger data (for rebuild) |

### Validation SPs (Proc_Check_*)

| SP | Purpose |
|----|---------|
| Proc_CheckErrorResultDataConvert | 47K — validates data conversion results |
| Proc_CheckExistInvoiceIPCancelBeforeDeleteVoucher | 30K — prevents deleting vouchers with active e-invoices |
| Proc_CheckExistInvoiceIPCancelBeforeSaveVoucher | 29K — prevents saving over active e-invoices |
| Proc_CheckExistsRefNo | 6K — validates unique reference numbers |
| Proc_CheckExistsCodeAlias | 4K — validates unique code aliases |
| Proc_CheckOpeningAccountEntry | 2K — validates opening balance entries |
| Proc_CheckHasArisenForVoucher | 2K — checks if voucher has downstream transactions |

### Report Cache Pattern
Report SPs write results to `Report.ReportData_Proc_*` tables (86 cache tables).
`Report.ReportCacheList` tracks cache validity.
Pattern: SP runs → results stored → subsequent requests read cache → invalidated on data change.

### System Configuration (SYSDBOption — Key Values)

| OptionID | Value | Description |
|----------|-------|-------------|
| AccountingSystem | 15 | VN Accounting Standard (Thông tư 200 = 15) |
| DefaultCostMethod | 0 | Weighted Average pricing |
| MainCurrency | VND | Vietnamese Dong |
| StartDate | 01/01/2019 | Fiscal start date |
| AmountDecimalDigits | 0 | No decimals for VND amounts |
| AmountOCDecimalDigits | 3 | 3 decimals for foreign currency amounts |
| UnitPriceDecimalDigits | 2 | 2 decimals for unit prices |
| UnitPriceOCDecimalDigits | 4 | 4 decimals for foreign currency prices |
| QuantityDecimalDigits | 2 | 2 decimals for quantities |
| ExchangRateDecimalDigits | 2 | 2 decimals for exchange rates |
| CoefficientDecimalDigits | 2 | 2 decimals for coefficients |
| AllocationDecimalDigits | 10 | 10 decimals for allocation ratios |
| AllowOverOutwardStock | False | **Cannot issue more than stock on hand** |
| AllowOverCashPayment | False | **Cannot pay more than cash balance** |
| AllowOverQuantityByLotNo | False | Cannot exceed lot quantity |
| IsCaculateOutwardPriceOld | 1 | Use legacy outward price calculation |

### MISA AMIS Platform Info (SYSDBInfo)

| Field | Value |
|-------|-------|
| Application | MISA AMIS ACT2 |
| Version | 2017 |
| MVC | 72.0.0.3 |
| Created | 2022-09-07 |

---

## APPENDIX: SAMPLE DATA

<!-- [DATA:Account] -->
**Account — Sample (TOP 15):**

| AccountNumber | AccountName | AccountNameEnglish | Grade | Kind | IsParent |
|---------------|-------------|-------------------|-------|------|----------|
| 111 | Tiền mặt | Cash in hand | 1 | 0 | 1 |
| 1111 | Tiền Việt Nam | Vietnam dong | 2 | 0 | 1 |
| 11111 | Tiền VN - Nhà máy Hà Nội | Vietnam dong | 3 | 0 | 0 |
| 11112 | Tiền VN - Nhà máy Bắc Ninh | Vietnam dong | 3 | 0 | 0 |
| 1112 | Ngoại tệ | Foreign currency | 2 | 0 | 0 |
| 1113 | Vàng tiền tệ | Monetary gold | 2 | 0 | 0 |
| 112 | Tiền gửi Ngân hàng | Cash in bank | 1 | 0 | 1 |
| 1121 | Tiền Việt Nam | Vietnam dong | 2 | 0 | 1 |
| 1122 | Ngoại tệ | Foreign currency | 2 | 0 | 0 |

**Stats:** 335 rows | VN standard chart of accounts (Thông tư 200)

---

## APPENDIX: ABBREVIATIONS & MULTI-LANGUAGE

### Multi-language Strategy in Database
The database does **NOT** use a separate dictionary/translation table. Instead, English translations are stored as **inline columns** alongside Vietnamese columns:

| Table | Vietnamese Column | English Column |
|-------|-------------------|----------------|
| Account | AccountName | AccountNameEnglish |
| Bank | BankName | BankNameEnglish |
| CCY | CCYName | CCYNameENG |
| FRTemplate | ItemName | ItemNameEnglish |
| FRB09DN* | ItemName | ItemNameEnglish |
| SYSReportList | ReportName | ReportNameEnglish |
| SYSReportGroup | GroupName | GroupNameEnglish |
| SYSReportCustom | ReportName | ReportNameEnglish |
| SYSReportLayoutConfig | Description | DescriptionEnglish |
| SYSReportCopyConfigDetail | — | CCPurposeEnglish, EnglishNameOfCopy |

### Column Name Mapping (ImportDictionary)
Table `ImportDictionary` (1,085 rows) maps Vietnamese UI column names to their non-diacritic equivalents for Excel import:

| ColumnName (Vietnamese) | ColumnNameExcel (Normalized) |
|------------------------|------------------------------|
| Email người liên hệ | Email nguoi lien he |
| Số CMND | So CMND |
| Hợp đồng bán | Hop dong ban |
| TK ngân hàng | TK ngan hang |

### Module/Table Prefix Abbreviations
*(Discovered from database naming conventions)*

| Prefix | Vietnamese | English |
|--------|-----------|---------|
| BA | (Bank Account) | Bank transactions |
| CA | (Cash Account) | Cash management |
| GL | (General Ledger) | General Ledger |
| IN | (Inventory) | Inventory/Warehouse |
| SA | (Sales) | Sales |
| PU | (Purchase) | Purchases |
| FA | (Fixed Asset) | Fixed Assets |
| JC | (Job Costing) | Job/Product Costing |
| PA | (Payroll) | Payroll |
| TA | (Tax) | Tax |
| SU | (Supply) | Supplies & Tools (CCDC) |
| CT | (Contract) | Contracts |
| IP | (Invoice Publishing) | E-Invoice |
| FR | (Financial Report) | Financial Reports |
| EMP | (Employee) | Employee requests |
| MSC | (MISA Security) | Security/Auth |
| SYS | (System) | System config |
| DI | (Dictionary) | Master data / Category |
| EB | (E-Banking) | E-Banking |
| BO | (Budget/Overhead) | Budget allocation |
| BU | (Budget) | Budget/Expenditure |

### Vietnamese Accounting Terms

| Vietnamese | Abbr | English |
|-----------|------|---------|
| Tài khoản | TK | Account |
| Khách hàng | KH | Customer |
| Nhà cung cấp | NCC | Supplier/Vendor |
| Hạch toán | HT | Journal Entry |
| Phát sinh | PS | Transaction/Movement |
| Sổ cái | SC | General Ledger |
| Phiếu thu | PT | Cash Receipt |
| Phiếu chi | PC | Cash Payment |
| Nhập kho | NK | Stock-in / Goods Receipt |
| Xuất kho | XK | Stock-out / Goods Issue |
| Hóa đơn | HD | Invoice |
| Báo cáo | BC | Report |
| Tài sản cố định | TSCĐ | Fixed Asset |
| Công cụ dụng cụ | CCDC | Tools & Supplies |
| Giá thành | GT | Cost/Costing |
| Tiền lương | TL | Salary |
| Thuế GTGT | VAT | Value Added Tax |
| Thuế TNDN | CIT | Corporate Income Tax |
| Thuế TNCN | PIT | Personal Income Tax |
| Thuế TTĐB | SCT | Special Consumption Tax |
| Đối tượng | DT | Object (Customer/Vendor/Employee) |
| Chứng từ | CT | Voucher/Document |
| Kỳ kế toán | KKT | Accounting Period |
| Bảng cân đối | BCD | Balance Sheet |
| Kết quả kinh doanh | KQKD | Income Statement |
| Lưu chuyển tiền tệ | LCTT | Cash Flow Statement |

---

## APPENDIX: SYSREFTYPE REGISTRY

> SYSRefType (262 rows) is the central voucher type registry.
> Each RefType number maps to a MasterTable + DetailTable.

Key entries (from MSC_SubSystem subsystem codes):

| Code | Vietnamese Name | English Name | Module |
|------|----------------|--------------|--------|
| BA | Ngân hàng | Bank | Bank |
| BADeposit | Thu tiền | Bank Deposit | Bank |
| BAWithDraw | Chi tiền | Bank Withdrawal | Bank |
| CA | Quỹ | Cash | Cash |
| CAReceipt | Thu tiền | Cash Receipt | Cash |
| CAPayment | Chi tiền | Cash Payment | Cash |
| CAAudit | Kiểm kê quỹ | Cash Audit | Cash |
| Category | Danh mục | Master Data | Dictionary |
| DIAccount | Tài khoản | Account | Dictionary |
| DIAccountingObject | Đối tượng | Accounting Object | Dictionary |
| DIInventoryItem | Vật tư hàng hóa | Inventory Item | Dictionary |
| 6 | Mua hàng | Purchase | Purchase |
| 7 | Bán hàng | Sales | Sales |
| 8 | Kho | Warehouse | Inventory |
| 9 | Tài sản cố định | Fixed Assets | Fixed Asset |
| 11 | Tiền lương | Payroll | Payroll |
| 12 | Giá thành | Job Costing | Job Costing |
| 13 | Thuế | Tax | Tax |
| 14 | Công cụ dụng cụ | Supplies/Tools | Supplies |
| 15 | Hợp đồng bán | Sale Contract | Contract |
| 1 | Báo cáo tài chính | Financial Reports | Reports |

---

## TAG INDEX

> Quick reference — all TAGs in this document

| What you need | Grep pattern | Result |
|---------------|-------------|--------|
| Table structure | `[TBL:Account]` | Column definitions, types |
| FK of a table | `[FK:Account]` | All relationships |
| Business rule | `[BR:DI01]` | Single rule definition |
| All rules in module | `[BR:DI` | All Dictionary module rules |
| Module overview | `[MOD:DI]` | Table list for module |
| Data flow | `[FLOW:Purchase]` | Workflow diagram |
| Sample data | `[DATA:Account]` | Sample rows + stats |
| View list | `View_SA` | All Sales views |
| SP pattern | `Proc_GLR_` | All GL report SPs |
