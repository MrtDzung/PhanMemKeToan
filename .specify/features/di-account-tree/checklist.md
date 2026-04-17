# DI Module — Account Tree: Requirements Quality Checklist

**Purpose**: Unit tests for requirements — validate completeness, clarity, consistency, and measurability of the spec/plan before implementation begins.
**Created**: 2026-04-17
**Source**: `spec.md` v1.0 Draft + `plan.md`
**Audience**: Spec author + reviewer (pre-implementation gate)

---

## 1. Business Rules Completeness

- [ ] CHK001 — Is the behavior of `IsParent` defined when ALL child accounts are deleted (should it revert to `false`)? [Gap, Spec §3.2]
- [ ] CHK002 — Are deactivation rules for accounts with **active child accounts** specified? `US-DI-004` mentions unposted vouchers but does not address child accounts. [Gap, Spec §2, US-DI-004]
- [ ] CHK003 — Is the allowed range for `AccountObjectType = None (0)` when `DetailByAccountObject = true` explicitly defined? The relationship between the flag and its enum value `0` is ambiguous. [Ambiguity, Spec §3.4, FR-042]
- [ ] CHK004 — Is BR-DI01 (child prefix rule) defined for the case where the parent code is changed after children exist? FR-016 defers to OQ-001 (reject or cascade) but the business rule ID is unresolved. [Conflict, Spec §3.3, OQ-001]
- [ ] CHK005 — Is re-activation (reversing `Inactive = true`) subject to any guard conditions? `US-DI-004` says "reversible" and `FR-020` permits it, but no guard rules are stated. [Completeness, Spec §3.2, FR-020]
- [ ] CHK006 — Are the business rules for changing `ParentID` (when no GL entries exist) fully specified beyond "MAY be edited"? `FR-016` only addresses `AccountNumber`, leaving `ParentID` change rules implicit. [Gap, Spec §3.2, FR-016]
- [ ] CHK007 — Is the rule for `AccountCategoryKind` inheritance explicitly scoped — does it apply only at creation time, or does it cascade on parent change? [Ambiguity, Spec §3.5, FR-052]
- [ ] CHK008 — Are the 10 `DetailBy*` flags independently toggleable without any mutual exclusion or dependency rules? If `DetailByJob` and `DetailByProjectWork` are related, a rule is missing. [Completeness, Spec §3.4, FR-040]
- [ ] CHK009 — Is there a maximum depth (Grade) limit for the account hierarchy? Vietnamese accounting standards may constrain nesting levels. [Gap, Spec §3.2]
- [ ] CHK010 — Is `MISACodeID` required, optional, or excluded from the data model in v1? OQ-004 recommends hiding it in UI but does not resolve whether it is stored or omitted from the entity. [Ambiguity, Spec §11, OQ-004]

---

## 2. API Contract Coverage

- [ ] CHK011 — Is the re-activation flow mapped to a specific API operation? Neither a dedicated endpoint nor a PUT field pattern is described for setting `Inactive = false`. [Gap, Spec §6]
- [ ] CHK012 — Are the two search surfaces (`GET /api/accounts?format=flat&q=...` and `GET /api/accounts/search`) differentiated by use-case with clear rules for when to use each? They appear to overlap. [Ambiguity, Spec §6, FR-073]
- [ ] CHK013 — Is pagination specified for `GET /api/accounts?format=flat` when account count is large (e.g., 500+)? No `page`/`pageSize` parameters are defined. [Gap, Spec §6]
- [ ] CHK014 — Is the import response schema defined for **partial failure** (some accounts valid, some invalid)? FR-083 says atomic all-or-nothing, but the response shows an `errors` array — are they mutually exclusive? [Ambiguity, Spec §6, FR-083]
- [ ] CHK015 — Is the `DELETE /api/accounts/{id}` endpoint also responsible for re-activation, or is that a separate PUT? The spec mentions soft-delete but not un-delete via DELETE. [Gap, Spec §6]
- [ ] CHK016 — Are rate limiting or throttle requirements defined for the import endpoint? A single import can write 330+ rows and is a potential DoS vector. [Gap, Spec §4, NFR-005]
- [ ] CHK017 — Is the `GET /api/accounts/search` endpoint's `postableOnly` parameter behavior when `false` (returns parent/summary accounts) documented with a use case? [Completeness, Spec §6]
- [ ] CHK018 — Is tenant isolation enforced at the API layer through an explicit specification (header, claim, or middleware), rather than only relying on implicit EF Core global filter? [Completeness, Spec §4, NFR-005]
- [ ] CHK019 — Are CORS or client-origin requirements specified for the accounts API? [Gap, Spec §6]
- [ ] CHK020 — Is the `conflictResolution` field in the import request defined as required or optional, and what is the default behavior if omitted? [Ambiguity, Spec §6]

---

## 3. UI/UX Requirements

- [ ] CHK021 — Is the right-click context menu referenced in §7.3 ("Add New Account — Triggered from… right-click context menu") fully specified with items, keyboard trigger, and behavior? It is mentioned once but never detailed. [Gap, Spec §7.3]
- [ ] CHK022 — Is the resizable panel split behavior specified (min/max width per panel, handle visibility, mobile fallback)? §7.1 says "resizable two-panel split" but provides no constraints. [Ambiguity, Spec §7.1]
- [ ] CHK023 — Is the empty-tree state (zero accounts, brand-new tenant) defined with a specific UI treatment (illustration, CTA to import, etc.)? [Gap, Spec §7.1]
- [ ] CHK024 — Is the tree node hover action row (`[Sửa] [Ngừng sử dụng]`) behavior specified when the account is already inactive (should "Ngừng sử dụng" become "Kích hoạt lại")? [Completeness, Spec §7.1]
- [ ] CHK025 — Is the import wizard Step 3 progress display specified as real-time streaming or polling-based? For a 5-second operation, the UX differs significantly. [Ambiguity, Spec §7.4]
- [ ] CHK026 — Are loading skeleton requirements defined for the initial tree load? NFR-001 targets <500ms but there is no spec for the loading state UI. [Gap, Spec §7.1]
- [ ] CHK027 — Is the confirmation dialog for deactivation vs. hard delete differentiated in the UI spec? Both operations use `p-confirmDialog` but require distinct messaging. [Completeness, Spec §7.2]
- [ ] CHK028 — Are keyboard shortcut requirements for the tree (not the form) specified? NFR-010 covers the form (Ctrl+S, Esc) but tree navigation keys (Arrow, Enter, Space for expand/collapse) are only in NFR-007/NFR-008. [Completeness, Spec §4, NFR-010]
- [ ] CHK029 — Is the `AccountNameEnglish` field displayed anywhere in the tree view or only in the detail form? FR-004 lists tree node display fields but omits `AccountNameEnglish`. [Ambiguity, Spec §3.1, FR-004]
- [ ] CHK030 — Is the "Has transactions" badge (US-DI-008) a computed real-time indicator or a cached/lazy attribute? If real-time, performance implications should be specified. [Ambiguity, Spec §2, US-DI-008]

---

## 4. Data Model Correctness

- [ ] CHK031 — Is `TenantId` explicitly documented in the entity spec, or is its presence only assumed via the `AuditableEntity` base class? If omitted from spec, reviewers cannot validate the migration. [Gap, Spec §5.1]
- [ ] CHK032 — Are `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` fields (from `AuditableEntity`) explicitly listed in the data model section for auditors? Relying on base class inheritance makes the spec incomplete for standalone review. [Completeness, Spec §5.1]
- [ ] CHK033 — Is a `SequenceNumber` (or equivalent ordering field) included in the entity, required for US-DI-009 (drag-and-drop reorder)? It is absent from the Account entity definition. [Gap, Spec §5.1, US-DI-009]
- [ ] CHK034 — Is the database index for `TenantId` (required for all tenant-isolated queries) specified in §5.3? Only `AccountNumber`, GIN, and `ParentID` indexes are listed. [Gap, Spec §5.3]
- [ ] CHK035 — Is the uniqueness constraint for `AccountNumber` scoped per-tenant explicitly stated in the unique index definition? The partial index `WHERE "IsDeleted" = false` does not include a TenantId partition. [Correctness, Spec §5.3]
- [ ] CHK036 — Is the `RowVersion` column type (`int`) sufficient for the optimistic concurrency pattern, or should it be `timestamp`/`rowversion` for PostgreSQL compatibility? The plan notes "application-managed" but this deviation from PostgreSQL native `xmin` should be rationale-documented. [Completeness, Spec §5.1, plan §Optimistic Concurrency]
- [ ] CHK037 — Are referential integrity requirements for `ParentID` self-reference specified (ON DELETE behavior for cascade vs. restrict)? [Gap, Spec §5.1]
- [ ] CHK038 — Is the character encoding constraint for `AccountNumber` (alphanumeric only, FR-030) enforced at the database level via a CHECK constraint, or only at the application layer? [Completeness, Spec §5.3, FR-030]
- [ ] CHK039 — Is the max length of `AccountName` (128 chars per VAL-003) reflected in the entity definition and database column? The entity spec shows `string AccountName` with no length annotation. [Completeness, Spec §5.1, VAL-003]

---

## 5. Security Requirements

- [ ] CHK040 — Are role-based authorization requirements specified for each endpoint? NFR-005 only states "valid JWT required" but does not define which roles/permissions can create, update, or delete accounts. [Gap, Spec §4, NFR-005]
- [ ] CHK041 — Is cross-tenant data isolation tested and specified at the query level? The spec relies on EF Core global TenantId filter but does not require explicit test coverage for tenant boundary violations. [Gap, Spec §4, NFR-005]
- [ ] CHK042 — Is input sanitization specified for `AccountName` and `AccountNameEnglish` to prevent XSS via stored content rendered in the tree? [Gap, Spec §3.2]
- [ ] CHK043 — Is protection against mass-assignment attacks specified for the import endpoint? A malicious payload could attempt to overwrite system accounts if tenant resolution is based solely on JWT claims. [Gap, Spec §6]
- [ ] CHK044 — Are audit logging requirements defined beyond `AuditableEntity` timestamps? For a foundational master-data feature, who changed the CoA and when may be a compliance requirement. [Gap, Spec §4]
- [ ] CHK045 — Is the import endpoint protected against bulk-import abuse (repeated large imports)? No rate-limit or idempotency key is specified. [Gap, Spec §6, FR-080]

---

## 6. Performance Requirements

- [ ] CHK046 — Is the NFR-003 virtual scroll threshold (1,000+ nodes) consistent with FR-007 (virtual scrolling kicks in at 200 nodes)? These two thresholds differ — which is the trigger? [Conflict, Spec §3.1 FR-007, §4 NFR-003]
- [ ] CHK047 — Is the Redis cache key strategy for the tree endpoint (per-tenant? per-tenantId+includeInactive?) specified in the plan? Plan says TTL=5min but cache key design is missing. [Gap, plan §API Design]
- [ ] CHK048 — Is a caching strategy defined for `GET /api/accounts/{id}`? Only the tree endpoint has Redis caching specified. [Gap, plan §API Design]
- [ ] CHK049 — Is the <500ms tree render time (NFR-001) measured from API response received, or from user action trigger? The boundary conditions for this SLA are not defined. [Ambiguity, Spec §4, NFR-001]
- [ ] CHK050 — Is the <200ms typeahead SLA (NFR-002) defined as p99 or average? Without percentile specification, the requirement is not verifiable. [Ambiguity, Spec §4, NFR-002]
- [ ] CHK051 — Is performance degradation behavior specified when Redis is unavailable (fallback to DB, or serve stale)? [Gap, plan §API Design]
- [ ] CHK052 — Is the 5-second import SLA (NFR-004) defined under a specific load condition (single concurrent import, no other tenant writes)? [Ambiguity, Spec §4, NFR-004]
- [ ] CHK053 — Is a performance requirement defined for the client-side tree-building step (`AccountTreeBuilderService.buildTree()`) for large datasets (500+ accounts)? [Gap, plan §Frontend Architecture]

---

## 7. Test Coverage Specification

- [ ] CHK054 — Are E2E / Playwright test requirements specified? The acceptance criteria (§12) lists manual testing and unit/integration tests but no automated E2E coverage. [Gap, Spec §12]
- [ ] CHK055 — Are the 10+ unit test cases for `AccountValidationService` (prefix rule) enumerated in the spec/plan so completeness can be verified? The plan says "10+ test cases" without listing them. [Completeness, plan §Phase E]
- [ ] CHK056 — Are integration test scenarios for the import atomic rollback explicitly defined (e.g., partial failure at row 150 of 330 should roll back all 150)? [Completeness, Spec §12, plan §Phase E]
- [ ] CHK057 — Are concurrency test cases for 409 RowVersion conflict specified with multi-user setup steps? [Gap, Spec §12]
- [ ] CHK058 — Is test coverage for the `GetAccountTreeQueryHandler` (flat-to-tree building with orphan accounts, circular references) required? [Gap, plan §Phase E]
- [ ] CHK059 — Are accessibility test specifications (how WCAG 2.1 AA will be validated — automated axe scan, manual keyboard walkthrough) defined? [Gap, Spec §4, NFR-007]
- [ ] CHK060 — Are load/performance test requirements defined (e.g., what tool, what concurrency level, at what account count the SLAs must hold)? [Gap, Spec §4]

---

## 8. i18n / Localization Requirements

- [ ] CHK061 — Are i18n keys defined for all **import wizard step labels** (Step 1, 2, 3, 4 titles)? The `account.` key block in §10 does not include import wizard step labels. [Gap, Spec §10]
- [ ] CHK062 — Are i18n keys defined for all **DetailBy\* checkbox labels** displayed in Section 3 of the form? The two-column checkbox grid in §7.2 uses Vietnamese labels not present in §10. [Gap, Spec §10, FR-041]
- [ ] CHK063 — Are i18n keys defined for `AccountCategoryKind` display values (Nợ / Có in radio buttons)? [Gap, Spec §10, FR-050]
- [ ] CHK064 — Are i18n keys defined for `AccountObjectType` dropdown options (None / Supplier / Customer / Employee)? [Gap, Spec §10, FR-042]
- [ ] CHK065 — Are i18n keys defined for tree node hover action labels (`[Sửa]`, `[Ngừng sử dụng]`)? [Gap, Spec §7.1, §10]
- [ ] CHK066 — Are i18n keys defined for the `Grade` auto-computed badge (e.g., "Cấp 1", "Cấp 2")? [Gap, Spec §7.2, §10]
- [ ] CHK067 — Are i18n keys defined for tree status labels ("Đang sử dụng", "(Ngừng sử dụng)")? [Gap, Spec §7.1, §10]
- [ ] CHK068 — Is `AccountNameEnglish` the English translation of the account name or a distinct identifier for English-language reporting? Its role in the i18n architecture is not clarified. [Ambiguity, Spec §5.1, §7.2]
- [ ] CHK069 — Is the locale format for accent-insensitive search (`AccountName contains, case/accent-insensitive`, FR-070) specified with a concrete normalization strategy (PostgreSQL `unaccent` extension, ICU collation)? [Completeness, Spec §3.7, FR-070]
- [ ] CHK070 — Are date/time values in audit fields (`CreatedAt`, `ModifiedAt`) required to display in a localized format in the UI? [Gap, Spec §5.1]

---

## 9. Scenario Coverage

- [ ] CHK071 — Are requirements defined for the scenario where a user attempts to create a root account (no parent) with a code that starts with an existing account's code? (e.g., new root "11" when "111" already exists). [Edge Case, Spec §3.3, FR-031]
- [ ] CHK072 — Are requirements defined for moving an account to a new parent (ParentID change with no GL entries)? FR-016 only mentions `AccountNumber` change; `ParentID` change is an implicit alternate flow. [Gap, Spec §3.2, FR-016]
- [ ] CHK073 — Are requirements defined for concurrent import attempts by two users of the same tenant? The atomic transaction spec does not address lock contention. [Gap, Spec §3.8, FR-083]
- [ ] CHK074 — Is the behavior defined when importing TT99 with `conflictResolution = overwrite` and the existing account has GL entries (locked account)? [Gap, Spec §3.8, FR-084]
- [ ] CHK075 — Are requirements defined for the tree state after a failed save (API returns 400/422)? Is the form retained with error messages, or reset? [Gap, Spec §7.2]
- [ ] CHK076 — Is the behavior defined when search returns **no matching accounts** in an otherwise populated tree? FR-070 spec mentions an empty state message (`account.no_results`) but the tree container behavior (collapse all / show empty) is unspecified. [Completeness, Spec §3.7, §7.5]
- [ ] CHK077 — Is the behavior defined for creating a child account when the parent is `Inactive = true`? Should the system warn, block, or allow? [Gap, Spec §3.2]
- [ ] CHK078 — Are requirements defined for the "Has transactions" badge (US-DI-008) for accounts with only **reversed/voided** transactions? [Gap, Spec §2, US-DI-008]

---

## 10. Open Questions / Unresolved Ambiguities

- [ ] CHK079 — Is OQ-001 (AccountNumber change cascade vs. reject) resolved before implementation? FR-016 depends on this answer and currently says "MAY be edited" with no final ruling. [Conflict, Spec §11, OQ-001]
- [ ] CHK080 — Is OQ-002 (dual-book CoA) explicitly out of scope in v1 with a deferred backlog item created? Leaving it as "TBD" risks scope creep during implementation. [Assumption, Spec §11, OQ-002]
- [ ] CHK081 — Is OQ-003 (embedded JSON vs. remote fetch) resolved? The plan recommends embedded JSON but the spec still shows it as open. This affects the Infrastructure layer implementation. [Assumption, Spec §11, OQ-003]
- [ ] CHK082 — Is OQ-004 (MISACodeID in UI) resolved? If hidden in v1, the field should still be specified as stored-but-not-displayed, with migration implications. [Assumption, Spec §11, OQ-004]
- [ ] CHK083 — Is the behavior for `AccountNumber` with leading zeros specified? (e.g., is `"0111"` a valid code under TT99?) [Ambiguity, Spec §3.3, FR-030]
- [ ] CHK084 — Is the default sort order of tree nodes (by `AccountNumber ASC`) explicitly stated as a functional requirement, not just an implementation note? [Gap, Spec §3.1]

---

## 11. Dependencies & Assumptions

- [ ] CHK085 — Is the dependency on `GeneralLedger` table (used to block deletes and code changes) formally captured as a cross-feature dependency with a defined interface/contract? [Completeness, Spec §5.4]
- [ ] CHK086 — Is the dependency of downstream voucher modules on `Account.AccountNumber` as FK documented with a breaking-change policy (what happens to vouchers if account code changes)? [Gap, Spec §1, OQ-001]
- [ ] CHK087 — Is the assumption that `ApplicationDbContext` already exists and is tenant-aware (via `TenantContextFactory`) validated against current codebase state? [Assumption, plan §Technical Context]
- [ ] CHK088 — Is the Redis dependency (for tree and typeahead cache) declared as a hard requirement with graceful-degradation behavior if Redis is unavailable? [Gap, plan §Technical Context]
- [ ] CHK089 — Is the `AuditableEntity` base class interface stable and compatible with the Account entity's requirements (especially `IsDeleted` and `TenantId`)? [Assumption, plan §Domain Layer]

---

*Total items: 89 | Domains: Business Rules (10), API Contract (10), UI/UX (10), Data Model (9), Security (6), Performance (8), Test Coverage (7), i18n (10), Scenario Coverage (8), Open Questions (6), Dependencies (5)*
