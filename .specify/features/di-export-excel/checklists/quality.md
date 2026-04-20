# Requirements Quality Checklist: Xuất Excel — Danh mục tài khoản

**Purpose**: Unit-test the requirements for completeness, clarity, consistency, and implementation-readiness  
**Created**: 2026-04-18  
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md)  
**Feature ID**: `di-export-excel`  
**Audience**: Reviewer (pre-implementation gate)  
**Depth**: Standard

---

## Requirement Completeness

- [ ] CHK001 - Are requirements defined for the "empty dataset" edge case — specifically, does the spec require the file to be created with only a header row when no accounts match the filter? [Gap, Edge Case — listed in Edge Cases section but no corresponding FR]
- [ ] CHK002 - Is there a requirement specifying behavior when the browser's popup blocker prevents automatic download? [Gap, Edge Case — listed in Edge Cases section but no FR maps to it]
- [ ] CHK003 - Are requirements defined for the concurrent-export edge case (multiple browser tabs triggering export simultaneously)? [Gap, Edge Case — listed in Edge Cases section but no FR maps to it]
- [ ] CHK004 - Is a requirement specified for what happens when special characters (`&`, `<`, `>`, full Vietnamese diacritics) appear in account names — specifically in the Excel cell rendering, not just the file? [Gap, Edge Case — raised in spec Edge Cases but not mapped to an FR]
- [ ] CHK005 - Are requirements defined for browser compatibility (minimum supported browsers for the `XLSX.writeFile` anchor-click download mechanism)? [Gap, Non-Functional]
- [ ] CHK006 - Are requirements specified for the exact format and text of the Vietnamese error message in FR-013 — or only that it must be "thân thiện tiếng Việt"? [Completeness, Spec §FR-013]
- [ ] CHK007 - Is there a requirement specifying behavior when `filteredAccounts()` returns an empty array (no results after filtering) — should export be blocked, or should the file still be created with only headers? [Completeness, Gap, Spec §FR-014]

---

## Requirement Clarity

- [ ] CHK008 - Is "auto-fit" in FR-007 precisely defined? The plan provides hardcoded widths `[15, 40, 12, 6, 15, 10, 12]` — does the spec require true dynamic auto-fit or are approximate widths acceptable? [Ambiguity, Spec §FR-007]
- [ ] CHK009 - Is "pre-order traversal" in FR-003 explicitly defined in the spec, or is this implementation detail only in the plan? A reader of spec.md alone should understand the ordering algorithm. [Clarity, Spec §FR-003]
- [ ] CHK010 - Is SC-001's "3 seconds" SLA precisely scoped: does it apply to all export operations or only when accounts ≤ 500? What is the expected behavior for tenants with >500 accounts? [Clarity, Measurability, Spec §SC-001]
- [ ] CHK011 - Is the term "phẳng hóa" (flattened) sufficiently defined in spec.md — does it specify that cha-trước-con ordering must be preserved even in filtered exports? [Clarity, Spec §FR-003, §FR-014]
- [ ] CHK012 - Is "màu nền primary blue" in FR-006 linked to a specific, authoritative value? The spec says "#1B5E9E từ design tokens" but does not reference the design token name `--primary` — is this unambiguous for implementers? [Clarity, Spec §FR-006]
- [ ] CHK013 - Is the distinction between "nút bị vô hiệu hóa" (disabled) and "hiển thị trạng thái đang tải" (loading spinner) in FR-012 defined precisely enough for visual implementation? Are both disabled AND loading spinner required simultaneously? [Clarity, Spec §FR-012]

---

## Requirement Consistency

- [ ] CHK014 - Is there a conflict between the spec's "Loại TK" column labels ("Tài sản", "Nguồn vốn", "Doanh thu", "Chi phí" — Spec §FR-010, §User Story 1 Scenario 2) and the plan's actual mapping ("Dư Nợ", "Dư Có", "Lưỡng tính" — Plan §1.2)? The spec describes `AccountType` categories but the DTO exposes `AccountCategoryKind` (Debit/Credit/Mixed). [Conflict, Spec §FR-010 vs Plan §Known Gaps Gap 3]
- [ ] CHK015 - Are the 7 column names listed consistently across all spec sections: FR-002, User Story 1 Scenario 2, and Key Entities? Specifically, does the order match exactly in all three locations? [Consistency, Spec §FR-002]
- [ ] CHK016 - Is FR-003 (pre-order traversal) consistent with FR-014 (filtered export)? Does filtering a subtree of accounts and then flattening produce a valid, hierarchically-correct pre-order sequence? Is this ambiguity resolved in spec? [Consistency, Spec §FR-003, §FR-014]
- [ ] CHK017 - Does SC-003 ("Tổng số dòng khớp 100% với tổng số tài khoản được xuất") align with FR-002 (exactly 7 columns)? Is there a risk that count validation excludes the header row? [Consistency, Spec §SC-003, §FR-002]
- [ ] CHK018 - Is the spec's assumption "SheetJS (`xlsx`) được cài như devDependency" consistent with the plan's decision to use `xlsx-js-style` (a different package)? The assumption names the wrong package. [Conflict, Spec §Assumptions vs Plan §Gap 2]

---

## Acceptance Criteria Quality

- [ ] CHK019 - Is User Story 1 Scenario 2's acceptance criterion ("đúng 7 cột đúng thứ tự") objectively verifiable? Are the column names in the scenario quoted verbatim to enable exact matching? [Measurability, Spec §User Story 1 Scenario 2]
- [ ] CHK020 - Is SC-002 ("mở được trong Microsoft Excel và LibreOffice Calc không có lỗi định dạng") measurable — are specific Excel/LibreOffice versions defined as the target environment? [Measurability, Spec §SC-002]
- [ ] CHK021 - Is SC-006 ("pre-order traversal") testable by a non-developer? Does the spec provide a concrete example of expected row ordering that a reviewer can verify without understanding the algorithm? [Measurability, Spec §SC-006]
- [ ] CHK022 - Is SC-005 ("nút bắt đầu phản hồi trong vòng 1 giây") measurable in an automated test? Is there a defined threshold for "phản hồi" (e.g., spinner visible, button disabled)? [Measurability, Spec §SC-005]

---

## Scenario Coverage

- [ ] CHK023 - Are requirements specified for the recovery scenario when SheetJS (`xlsx-js-style`) fails to load or throws a runtime error — does the error toast from FR-013 also cover client-side library failures? [Coverage, Exception Flow, Spec §FR-013]
- [ ] CHK024 - Are requirements defined for the scenario where the user navigates away from `/di/accounts` during an export in progress — is the `isExporting` state cleaned up? [Coverage, Exception Flow, Gap]
- [ ] CHK025 - Is there a scenario covering what happens when `store.filteredAccounts()` and `store.accounts()` return inconsistent data (e.g., a parent referenced in filtered list is not found in `allAccounts`)? [Coverage, Edge Case, Plan §1.2]
- [ ] CHK026 - Are success criteria defined for User Story 2 (filter-scoped export) with the same rigor as User Story 1 — specifically, is a measurable outcome (like SC-003 row count match) explicitly linked to Story 2 scenarios? [Coverage, Spec §User Story 2]

---

## Edge Case Coverage

- [ ] CHK027 - Is the behavior for a root account (parentId = null) consistently specified for both filtered and unfiltered exports? FR-011 covers this but is it explicitly stated in Story 2 scenarios? [Coverage, Spec §FR-011, §User Story 2]
- [ ] CHK028 - Are requirements specified for accounts whose `accountName` is extremely long — does FR-007 (auto-fit column width) have a maximum width cap to prevent unusable Excel files? [Edge Case, Gap, Spec §FR-007]
- [ ] CHK029 - Are requirements defined for the encoding of the downloaded filename itself (not just file content) — specifically for browsers that may not handle `danh-muc-tai-khoan_` with hyphens in the `Content-Disposition` header? [Edge Case, Gap]
- [ ] CHK030 - Is the edge case "download popup blocker" sufficiently addressed? The plan mentions `XLSX.writeFile` uses anchor-click to avoid this, but spec does not have a corresponding requirement or fallback UX. [Gap, Spec §Edge Cases]

---

## Non-Functional Requirements

- [ ] CHK031 - Are bundle size requirements defined specifically for the addition of `xlsx-js-style` (~200KB)? Does the spec verify this stays within the project-wide 300KB gzipped budget from the design system? [Non-Functional, Gap]
- [ ] CHK032 - Are offline/no-network requirements defined? Since the export is fully client-side (FR-015), should it work even when the network is down (after initial page load)? Is this a requirement or an assumption? [Non-Functional, Gap]
- [ ] CHK033 - Are requirements defined for export performance with datasets significantly larger than 500 accounts (e.g., tenant migration data with 2000+ accounts)? SC-001 only covers "tối đa 500 bản ghi". [Non-Functional, Spec §SC-001, §Assumptions]
- [ ] CHK034 - Are accessibility requirements defined for the loading state — specifically, does the loading spinner on the "Xuất Excel" button require an `aria-label` update (e.g., "Đang xuất Excel...") per WCAG 2.1 AA? [Non-Functional, Accessibility, Gap]

---

## Dependencies & Assumptions

- [ ] CHK035 - Is the assumption "Nút 'Xuất Excel' hiện đang tồn tại với `[disabled]='true'`" verified and documented — what if the button does not yet exist in the current toolbar implementation? Is this a blocking dependency? [Assumption, Spec §Assumptions]
- [ ] CHK036 - Is the assumption "Ánh xạ AccountType → nhãn tiếng Việt đã tồn tại trong hệ thống" verified — specifically, does `AccountCategoryKind` mapping already exist in a shared translation file, or does the plan need to create it? [Assumption, Conflict, Spec §Assumptions vs Plan §1.2]
- [ ] CHK037 - Is the dependency on `filteredAccounts()` signal (vs raw `accounts()`) documented in spec — specifically, is it defined which signal is the source of truth for what gets exported? [Dependency, Spec §FR-014, §Assumptions]
- [ ] CHK038 - Is the assumption about UTF-8 encoding validated — does `xlsx-js-style` (the actual library used per plan) guarantee UTF-8 for `.xlsx` files, or does this require explicit configuration? [Assumption, Spec §Assumptions vs Plan §Known Gaps Gap 2]

---

## Ambiguities & Conflicts

- [ ] CHK039 - Is it ambiguous whether the "Tiền tệ" column should display "Ngoại tệ" / blank (plan decision) or actual `CurrencyCode` values (spec intent in FR-002 and Key Entities)? The spec's Key Entities section lists `CurrencyCode` as an exported attribute, but the plan maps a boolean instead. [Conflict, Spec §FR-002, §Key Entities vs Plan §Known Gaps Gap 1]
- [ ] CHK040 - Is it clear whether the "Loại TK" column header in the exported file should match exactly "Loại TK" (spec FR-002) or a more descriptive Vietnamese label? The header text is listed but not marked as a fixed string requirement. [Ambiguity, Spec §FR-002]
- [ ] CHK041 - Is the font/encoding requirement for the Excel header row ("in đậm, màu nền xanh") in FR-006 precise enough — does the white text color for contrast on the blue header need to be explicitly required? [Ambiguity, Spec §FR-006]
- [ ] CHK042 - Is it ambiguous whether "Xuất Excel" applies only to the account tree screen or could be reused across other DI module screens? FR-015 says "ExcelExportService in core/services/" implying reuse — but scope is not defined in spec. [Ambiguity, Scope, Plan §Phase 0]
