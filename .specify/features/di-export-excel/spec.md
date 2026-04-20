# Feature Specification: Xuất Excel — Danh mục tài khoản

**Feature Branch**: `di-export-excel`  
**Created**: 2026-04-18  
**Status**: Draft  
**Input**: User description: "Export the Vietnamese Chart of Accounts (danh mục tài khoản) from the account tree page to an Excel (.xlsx) file"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Xuất toàn bộ danh mục tài khoản ra Excel (Priority: P1)

Kế toán viên hoặc quản trị viên mở trang Danh mục tài khoản (`/di/accounts`), nhấn nút "Xuất Excel" trên thanh công cụ. Hệ thống tạo file Excel chứa toàn bộ danh mục tài khoản theo cấu trúc phân cấp được phẳng hóa (cha trước con, cột Cấp chỉ rõ độ sâu), tự động tải xuống trình duyệt.

**Why this priority**: Đây là chức năng duy nhất của tính năng này — mọi yêu cầu khác (loading state, filter behavior) đều phụ thuộc vào hành vi export cốt lõi này.

**Independent Test**: Có thể kiểm tra độc lập bằng cách nhấn nút "Xuất Excel" trên trang `/di/accounts` (không có bộ lọc) và xác nhận file `.xlsx` tải về chứa đúng số lượng, đúng cấu trúc, đúng định dạng.

**Acceptance Scenarios**:

1. **Given** người dùng đang ở trang `/di/accounts` không có bộ lọc nào, **When** nhấn nút "Xuất Excel", **Then** file `danh-muc-tai-khoan_YYYYMMDD.xlsx` được tải về tự động trong vòng 3 giây.
2. **Given** file Excel đã được tải về, **When** mở file, **Then** có đúng 1 sheet tên "Danh mục tài khoản" với hàng tiêu đề in đậm, nền xanh, chứa đủ 7 cột đúng thứ tự: Số hiệu TK | Tên tài khoản | Loại TK | Cấp | Tài khoản mẹ | Tiền tệ | Trạng thái.
3. **Given** file Excel đã được tải về, **When** xem dữ liệu, **Then** danh sách là phẳng, tài khoản cấp 1 đứng trước, tài khoản con theo sau cha của nó; cột Cấp chứa số nguyên (1, 2, 3...); cột Tài khoản mẹ để trống cho tài khoản gốc.
4. **Given** file Excel đã được tải về, **When** xem cột Loại TK và Trạng thái, **Then** hiển thị nhãn tiếng Việt ("Tài sản", "Nguồn vốn", "Đang dùng", "Ngừng dùng") — không phải mã kỹ thuật.
5. **Given** file Excel đã được tải về, **When** xem cột Cấp, **Then** cột được căn phải; **And** tất cả ký tự tiếng Việt hiển thị chính xác không bị lỗi encoding.

---

### User Story 2 - Hành vi xuất khi có bộ lọc đang hoạt động (Priority: P2)

Kế toán viên đang tìm kiếm theo từ khóa hoặc lọc theo trạng thái trên trang Danh mục tài khoản, rồi nhấn "Xuất Excel". Hệ thống xuất **đúng tập dữ liệu đang hiển thị** (theo bộ lọc hiện tại), không phải toàn bộ danh mục. Nếu không có bộ lọc, xuất toàn bộ.

**Why this priority**: Hành vi này ảnh hưởng trực tiếp đến tính đầy đủ của báo cáo — người dùng có thể vô tình xuất dữ liệu không đầy đủ nếu không rõ ràng.

**Independent Test**: Áp dụng bộ lọc chỉ hiển thị tài khoản "Đang dùng" (giả sử 30 tài khoản), nhấn "Xuất Excel", kiểm tra file chứa đúng 30 dòng dữ liệu — không nhiều hơn.

**Acceptance Scenarios**:

1. **Given** người dùng đang tìm kiếm bằng từ khóa và kết quả hiển thị N tài khoản, **When** nhấn "Xuất Excel", **Then** file Excel chứa đúng N dòng dữ liệu tương ứng với kết quả lọc.
2. **Given** người dùng đang lọc theo trạng thái "Ngừng dùng", **When** nhấn "Xuất Excel", **Then** file Excel chỉ chứa các tài khoản trạng thái "Ngừng dùng".
3. **Given** không có bộ lọc nào đang hoạt động, **When** nhấn "Xuất Excel", **Then** file Excel chứa toàn bộ danh mục tài khoản.

---

### User Story 3 - Trạng thái loading và phản hồi lỗi (Priority: P3)

Khi quá trình xuất đang thực hiện, nút "Xuất Excel" chuyển sang trạng thái loading để tránh người dùng nhấn lại. Nếu có lỗi, hiển thị thông báo rõ ràng bằng tiếng Việt.

**Why this priority**: Cải thiện trải nghiệm người dùng và ngăn xuất trùng lặp, nhưng không ảnh hưởng đến tính năng cốt lõi.

**Independent Test**: Nhấn nút "Xuất Excel", quan sát trạng thái nút trong khi xử lý, sau khi hoàn thành nút trở về bình thường; mô phỏng lỗi server và xác nhận thông báo lỗi xuất hiện.

**Acceptance Scenarios**:

1. **Given** người dùng nhấn "Xuất Excel", **When** quá trình tạo file đang diễn ra, **Then** nút bị vô hiệu hóa và hiển thị chỉ báo đang tải (spinner).
2. **Given** quá trình xuất hoàn thành thành công, **When** file đã được tải về, **Then** nút trở về trạng thái hoạt động bình thường.
3. **Given** quá trình xuất thất bại (lỗi mạng, lỗi server), **When** có lỗi xảy ra, **Then** hiển thị thông báo lỗi tiếng Việt thân thiện; nút trở về trạng thái hoạt động.

---

### Edge Cases

- Điều gì xảy ra khi danh mục tài khoản trống (không có bản ghi nào) — file Excel có được tạo với chỉ hàng tiêu đề không?
- Điều gì xảy ra khi tên tài khoản chứa ký tự đặc biệt (dấu tiếng Việt đầy đủ, ký tự &, <, >)?
- Điều gì xảy ra khi có lỗi mạng hoặc server timeout trong quá trình xuất?
- Điều gì xảy ra khi trình duyệt chặn tải file tự động (download popup blocker)?
- Điều gì xảy ra nếu cùng lúc có nhiều tab nhấn "Xuất Excel"?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hệ thống PHẢI cho phép người dùng xuất danh mục tài khoản ra file Excel (.xlsx) bằng cách nhấn nút "Xuất Excel" trên thanh công cụ trang `/di/accounts`.
- **FR-002**: File Excel PHẢI chứa đúng 7 cột theo thứ tự: Số hiệu TK, Tên tài khoản, Loại TK, Cấp, Tài khoản mẹ, Tiền tệ, Trạng thái.
- **FR-003**: Dữ liệu PHẢI được xuất dưới dạng danh sách phẳng (flattened), với tài khoản cha xuất hiện trước tài khoản con (duyệt pre-order của cây phân cấp).
- **FR-004**: Tên file PHẢI theo định dạng `danh-muc-tai-khoan_YYYYMMDD.xlsx` trong đó YYYYMMDD là ngày xuất file.
- **FR-005**: Sheet trong file Excel PHẢI có tên "Danh mục tài khoản".
- **FR-006**: Hàng tiêu đề PHẢI được định dạng in đậm, màu nền là primary blue (#1B5E9E từ design tokens — không hardcode ở component).
- **FR-007**: Độ rộng cột PHẢI được tự động điều chỉnh vừa với nội dung (auto-fit).
- **FR-008**: Cột số (Cấp — AccountLevel) PHẢI căn phải trong Excel.
- **FR-009**: Giá trị cột Trạng thái PHẢI hiển thị nhãn tiếng Việt thân thiện: "Đang dùng" hoặc "Ngừng dùng" — không phải giá trị enum kỹ thuật.
- **FR-010**: Giá trị cột Loại TK (AccountType) PHẢI hiển thị nhãn tiếng Việt (ví dụ: "Tài sản", "Nguồn vốn", "Doanh thu", "Chi phí").
- **FR-011**: Cột Tài khoản mẹ (ParentCode) PHẢI để trống cho tài khoản gốc (không có cha).
- **FR-012**: Nút "Xuất Excel" PHẢI bị vô hiệu hóa và hiển thị trạng thái đang tải trong khi file đang được tạo, để tránh xuất trùng lặp.
- **FR-013**: Hệ thống PHẢI hiển thị thông báo lỗi tiếng Việt thân thiện nếu quá trình xuất thất bại.
- **FR-014**: Phạm vi dữ liệu xuất PHẢI phản ánh bộ lọc hiện tại: nếu có bộ lọc tìm kiếm hoặc trạng thái đang hoạt động → xuất chỉ các tài khoản khớp bộ lọc (danh sách phẳng, giữ đúng thứ tự phân cấp); nếu không có bộ lọc → xuất toàn bộ danh mục. Dữ liệu được lấy từ `AccountTreeStore` (đã có sẵn trong bộ nhớ), không gọi API mới.
- **FR-015**: File .xlsx PHẢI được tạo hoàn toàn phía client bằng thư viện **SheetJS/xlsx** (không cần API endpoint mới). Hệ thống đọc dữ liệu từ `AccountTreeStore`, áp dụng filter hiện tại, tạo workbook và trigger download trực tiếp trong trình duyệt.

### Key Entities

- **Account**: Đơn vị cơ bản trong danh mục tài khoản. Các thuộc tính xuất ra Excel: AccountCode, AccountName, AccountType, AccountLevel, ParentCode, CurrencyCode, Status.
- **AccountType**: Phân loại tài khoản — được ánh xạ sang nhãn tiếng Việt khi xuất (Tài sản, Nguồn vốn, Doanh thu, Chi phí, v.v.).
- **AccountStatus**: Trạng thái hoạt động của tài khoản — được ánh xạ sang "Đang dùng" / "Ngừng dùng" khi xuất.
- **ExcelExportResult**: Đầu ra của quá trình xuất — file .xlsx với đúng tên, sheet, định dạng tiêu đề, và dữ liệu.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Người dùng nhận được file Excel trong vòng 3 giây kể từ khi nhấn nút "Xuất Excel" (với tối đa 500 bản ghi).
- **SC-002**: File Excel mở được ngay lập tức trong Microsoft Excel và LibreOffice Calc mà không có lỗi định dạng hoặc cảnh báo.
- **SC-003**: Tổng số dòng dữ liệu trong file Excel khớp 100% với tổng số tài khoản được xuất (không thiếu, không trùng).
- **SC-004**: Tất cả ký tự tiếng Việt trong tên tài khoản và nhãn cột hiển thị chính xác trong file Excel (zero lỗi encoding).
- **SC-005**: Nút "Xuất Excel" bắt đầu phản hồi (chuyển sang trạng thái loading) trong vòng 1 giây sau khi nhấn.
- **SC-006**: Thứ tự dòng trong file Excel luôn đúng cấu trúc phân cấp: tài khoản cấp 1 xuất trước, theo sau là các tài khoản con theo cây (pre-order traversal).

## Assumptions

- Trang danh mục tài khoản có tối đa ~500 bản ghi — không cần streaming, phân trang hay xử lý đặc biệt cho volume lớn.
- Người dùng đã được xác thực và có quyền xem danh mục tài khoản trước khi thực hiện xuất.
- Ánh xạ AccountType → nhãn tiếng Việt đã tồn tại trong hệ thống (dùng lại, không tạo mới).
- Màu nền header Excel sử dụng giá trị `--primary: #1B5E9E` từ design tokens dự án — giá trị màu được định nghĩa là hằng số, không hardcode trực tiếp tại component.
- File Excel sử dụng encoding UTF-8 để hỗ trợ đầy đủ ký tự tiếng Việt.
- Nút "Xuất Excel" hiện đang tồn tại trong toolbar với thuộc tính `[disabled]="true"` — task là bật lên và kết nối logic.
- Dữ liệu xuất được đọc từ `AccountTreeStore` (signal store) — không có API call mới khi xuất.
- Nếu bộ lọc đang hoạt động, áp dụng filter trên `flatAccounts` signal trước khi tạo Excel.
- SheetJS (`xlsx`) được cài như devDependency frontend — không thêm package backend.

## Clarifications

### Session 2026-04-18

- Q: Khi có bộ lọc đang hoạt động, export phạm vi nào? → A: Export dữ liệu đã lọc (Option B) — nếu filter đang active thì chỉ xuất tài khoản khớp bộ lọc; nếu không có filter thì xuất toàn bộ.
- Q: Phương thức tạo file Excel là gì? → A: Frontend (SheetJS/xlsx) (Option B) — tạo file client-side từ dữ liệu đã có trong `AccountTreeStore`, không cần API endpoint mới.
