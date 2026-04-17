# Error Log — Bugs & Lessons Learned

## 2026-04-17: Import CoA — IsParent Not Set for Overwrite Path

**Symptom**: After importing 272 accounts with `conflictResolution: "overwrite"`, all accounts had `isParent = false`. Tree displayed flat (no expand arrows).

**Root Cause**: `ImportStandardCoaCommandHandler` only added `entry.ParentNumber` to `parentUpdates` in the `else` (new account) branch, NOT in the `overwrite` branch. When all 272 accounts already existed, the overwrite path never collected parent numbers, so the final loop that sets `IsParent = true` on parent accounts never ran.

**Fix**: Added `parentUpdates.Add(entry.ParentNumber)` to the overwrite branch as well.

**Lesson**: When import handlers have skip/overwrite/new branches, ensure all side-effects (like parent flag updates) are applied in ALL relevant branches, not just the "new" branch.

---

## 2026-04-17: Import CoA — IsParent Not Set for Newly Created Accounts

**Symptom**: First-time import of 272 accounts — all had `isParent = false`.

**Root Cause**: `ImportStandardCoaCommandHandler` parentUpdates loop used `existing.TryGetValue()` to find parent accounts and set `IsParent = true`. But `existing` only contained pre-import accounts. Newly created accounts weren't in `existing`, so their `IsParent` was never updated.

**Fix**: Added `newAccountsByNumber` dictionary to track newly created accounts. Parent update loop now checks both `existing` and `newAccountsByNumber`.

**Lesson**: When building parent-child relationships during batch import, maintain a lookup of ALL accounts (existing + newly created), not just pre-existing ones.

---

## 2026-04-17: AccountDetailDto Duplicate ParentId — CS0108 Warning

**Symptom**: Build warning `CS0108: 'AccountDetailDto.ParentId' hides inherited member 'AccountTreeNodeDto.ParentId'`.

**Root Cause**: `ParentId` was added to `AccountTreeNodeDto` (base class) for flat format support, but `AccountDetailDto` already had its own `ParentId`. The duplicate property in child class hid the inherited one.

**Fix**: Removed duplicate `ParentId` from `AccountDetailDto` since it now inherits from `AccountTreeNodeDto`.

**Lesson**: Before adding properties to a base DTO class, check all derived classes for existing properties with the same name.

---

## 2026-04-17: FlattenTree Missing ParentId/Grade in AccountListItemDto

**Symptom**: Angular tree builder received flat account list without `parentId` or `grade` fields. Tree couldn't build hierarchy — all accounts displayed at root level.

**Root Cause**: `GetAccountTreeQueryHandler.FlattenTree()` mapped to `AccountListItemDto` which didn't have `ParentId`, `Grade`, or `IsPostableInForeignCurrency`. The Angular `account-tree-builder.service.ts` depends on `parentId` to build the tree from flat data.

**Fix**: Added `ParentId`, `Grade`, `IsPostableInForeignCurrency` to `AccountListItemDto` and mapped them in `FlattenTree`.

**Lesson**: When frontend builds tree from flat API data, the flat DTO MUST include hierarchy fields (`parentId`, `grade`). Always verify the full chain: Domain → DTO → API Response → Angular interface → Tree builder.
