import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AccountTreeStore } from './account-tree.store';
import { AccountApiService } from '../services/account-api.service';
import { AccountTreeBuilderService } from '../services/account-tree-builder.service';
import { AuthStore } from '../../../../core/stores/auth.store';
import {
  AccountCategoryKind,
  AccountObjectType,
  AccountTreeNodeDto,
  AccountDetailDto,
  CreateAccountCommand,
  UpdateAccountCommand,
} from '../../models/account.models';

// ─── Helpers ────────────────────────────────────────────────────────────────

function makeAccount(
  overrides: Partial<AccountTreeNodeDto> & { accountId: string; accountNumber: string }
): AccountTreeNodeDto {
  return {
    accountId: overrides.accountId,
    accountNumber: overrides.accountNumber,
    accountName: overrides.accountName ?? 'Test Account',
    accountNameEnglish: null,
    grade: overrides.grade ?? 1,
    isParent: overrides.isParent ?? false,
    accountCategoryKind: overrides.accountCategoryKind ?? AccountCategoryKind.Debit,
    inactive: overrides.inactive ?? false,
    isPostableInForeignCurrency: false,
    hasTransactions: false,
    parentId: overrides.parentId ?? null,
    children: [],
  };
}

function makeDetailDto(overrides: Partial<AccountDetailDto> & { accountId: string; accountNumber: string }): AccountDetailDto {
  return {
    ...makeAccount({ accountId: overrides.accountId, accountNumber: overrides.accountNumber }),
    parentNumber: null,
    parentName: null,
    accountObjectType: AccountObjectType.None,
    detailByAccountObject: false,
    detailByBankAccount: false,
    detailByJob: false,
    detailByProjectWork: false,
    detailByOrder: false,
    detailByContract: false,
    detailByExpenseItem: false,
    detailByDepartment: false,
    detailByListItem: false,
    detailByPuContract: false,
    rowVersion: 1,
    createdAt: '2026-01-01T00:00:00',
    createdBy: 'admin',
    modifiedAt: null,
    modifiedBy: null,
    ...overrides,
  };
}

function makeCreateCmd(overrides: Partial<CreateAccountCommand> = {}): CreateAccountCommand {
  return {
    accountNumber: '999',
    accountName: 'New Account',
    accountCategoryKind: AccountCategoryKind.Debit,
    isPostableInForeignCurrency: false,
    accountObjectType: AccountObjectType.None,
    detailByAccountObject: false,
    detailByBankAccount: false,
    detailByJob: false,
    detailByProjectWork: false,
    detailByOrder: false,
    detailByContract: false,
    detailByExpenseItem: false,
    detailByDepartment: false,
    detailByListItem: false,
    detailByPuContract: false,
    ...overrides,
  };
}

function makeUpdateCmd(overrides: Partial<UpdateAccountCommand> = {}): UpdateAccountCommand {
  return { ...makeCreateCmd(), rowVersion: 1, inactive: false, ...overrides };
}

// ─── Shared test data ────────────────────────────────────────────────────────

const ROOT    = makeAccount({ accountId: 'r1',  accountNumber: '1',   isParent: true,  inactive: false });
const PARENT  = makeAccount({ accountId: 'p1',  accountNumber: '11',  isParent: true,  inactive: false, parentId: 'r1' });
const ACTIVE  = makeAccount({ accountId: 'a1',  accountNumber: '111', isParent: false, inactive: false, parentId: 'p1' });
const INACTIVE = makeAccount({ accountId: 'i1', accountNumber: '112', isParent: false, inactive: true,  parentId: 'p1' });

const FLAT_ACCOUNTS = [ROOT, PARENT, ACTIVE, INACTIVE];

// ─── Suite ───────────────────────────────────────────────────────────────────

describe('AccountTreeStore', () => {
  let store: InstanceType<typeof AccountTreeStore>;
  let mockApiService: jasmine.SpyObj<AccountApiService>;
  let mockBuilderService: jasmine.SpyObj<AccountTreeBuilderService>;
  let currentUserSignal: ReturnType<typeof signal<{ tenantId: string } | null>>;

  beforeEach(() => {
    mockApiService = jasmine.createSpyObj<AccountApiService>('AccountApiService', [
      'getAccountTree', 'getAccountById', 'createAccount', 'updateAccount', 'deleteAccount', 'importCoa',
    ]);

    mockBuilderService = jasmine.createSpyObj<AccountTreeBuilderService>('AccountTreeBuilderService', [
      'buildTree', 'filterTree',
    ]);
    // Default pass-through for filterTree; buildTree returns empty array by default
    mockBuilderService.filterTree.and.callFake((accs: AccountTreeNodeDto[]) => accs);
    mockBuilderService.buildTree.and.returnValue([]);

    currentUserSignal = signal<{ tenantId: string } | null>({ tenantId: 'tenant-1' });

    // Default success response so loadTree never throws unexpectedly
    mockApiService.getAccountTree.and.returnValue(of({ data: FLAT_ACCOUNTS, errors: null }));

    TestBed.configureTestingModule({
      providers: [
        AccountTreeStore,
        { provide: AccountApiService, useValue: mockApiService },
        { provide: AccountTreeBuilderService, useValue: mockBuilderService },
        { provide: AuthStore, useValue: { currentUser: currentUserSignal } },
      ],
    });

    spyOn(localStorage, 'getItem').and.returnValue(null);
    spyOn(localStorage, 'setItem');

    store = TestBed.inject(AccountTreeStore);
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 1 — initial state
  // ─────────────────────────────────────────────────────────────────────────
  describe('initial state', () => {
    it('accounts_Should_BeEmpty', () => {
      expect(store.accounts()).toEqual([]);
    });

    it('loading_Should_BeFalse', () => {
      expect(store.loading()).toBe(false);
    });

    it('saving_Should_BeFalse', () => {
      expect(store.saving()).toBe(false);
    });

    it('formMode_Should_Be_View', () => {
      expect(store.formMode()).toBe('view');
    });

    it('selectedAccountId_Should_BeNull', () => {
      expect(store.selectedAccountId()).toBeNull();
    });

    it('statusFilter_Should_Be_All', () => {
      expect(store.statusFilter()).toBe('all');
    });

    it('searchQuery_Should_BeEmpty', () => {
      expect(store.searchQuery()).toBe('');
    });

    it('error_Should_BeNull', () => {
      expect(store.error()).toBeNull();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 2 — filterByStatus (tested via filteredAccounts computed)
  // ─────────────────────────────────────────────────────────────────────────
  describe('filterByStatus via filteredAccounts', () => {
    beforeEach(async () => {
      await store.loadTree();
    });

    it('all_Should_ReturnAllAccounts', () => {
      store.setStatusFilter('all');
      expect(store.filteredAccounts().length).toBe(FLAT_ACCOUNTS.length);
    });

    it('active_Should_ExcludeInactiveLeaf', () => {
      store.setStatusFilter('active');
      expect(store.filteredAccounts().find(a => a.accountId === 'i1')).toBeUndefined();
    });

    it('active_Should_IncludeActiveLeaf', () => {
      store.setStatusFilter('active');
      expect(store.filteredAccounts().find(a => a.accountId === 'a1')).toBeTruthy();
    });

    it('active_Should_IncludeAncestorsOfActiveLeaf', () => {
      store.setStatusFilter('active');
      const result = store.filteredAccounts();
      expect(result.find(a => a.accountId === 'r1')).toBeTruthy();
      expect(result.find(a => a.accountId === 'p1')).toBeTruthy();
    });

    it('inactive_Should_IncludeInactiveLeaf', () => {
      store.setStatusFilter('inactive');
      expect(store.filteredAccounts().find(a => a.accountId === 'i1')).toBeTruthy();
    });

    it('inactive_Should_ExcludeActiveLeaf', () => {
      store.setStatusFilter('inactive');
      expect(store.filteredAccounts().find(a => a.accountId === 'a1')).toBeUndefined();
    });

    it('inactive_Should_IncludeAncestorsOfInactiveLeaf', () => {
      store.setStatusFilter('inactive');
      const result = store.filteredAccounts();
      expect(result.find(a => a.accountId === 'r1')).toBeTruthy();
      expect(result.find(a => a.accountId === 'p1')).toBeTruthy();
    });

    it('all_ToActive_ToAll_Should_RestoreFullList', () => {
      store.setStatusFilter('active');
      store.setStatusFilter('all');
      expect(store.filteredAccounts().length).toBe(FLAT_ACCOUNTS.length);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 3 — loadTree
  // ─────────────────────────────────────────────────────────────────────────
  describe('loadTree', () => {
    it('success_Should_PopulateAccounts', async () => {
      await store.loadTree();
      expect(store.accounts()).toEqual(FLAT_ACCOUNTS);
    });

    it('success_Should_ClearLoadingFlag', async () => {
      await store.loadTree();
      expect(store.loading()).toBe(false);
    });

    it('success_Should_LoadExpandedIdsFromLocalStorage', async () => {
      (localStorage.getItem as jasmine.Spy).and.returnValue(JSON.stringify(['r1', 'p1']));
      await store.loadTree();
      expect(store.expandedNodeIds().has('r1')).toBe(true);
      expect(store.expandedNodeIds().has('p1')).toBe(true);
    });

    it('success_Should_UseApiWithIncludeInactiveTrue', async () => {
      await store.loadTree();
      expect(mockApiService.getAccountTree).toHaveBeenCalledWith(true, 'flat');
    });

    it('apiError_Should_SetErrorDetail', async () => {
      mockApiService.getAccountTree.and.returnValue(
        throwError(() => ({ error: { detail: 'Server unavailable' } }))
      );
      await store.loadTree();
      expect(store.error()).toBe('Server unavailable');
      expect(store.loading()).toBe(false);
    });

    it('apiError_NoDetail_Should_UseDefaultMessage', async () => {
      mockApiService.getAccountTree.and.returnValue(throwError(() => ({})));
      await store.loadTree();
      expect(store.error()).toBeTruthy();
      expect(store.loading()).toBe(false);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 4 — selectAccount
  // ─────────────────────────────────────────────────────────────────────────
  describe('selectAccount', () => {
    const detail = makeDetailDto({ accountId: 'a1', accountNumber: '111' });

    beforeEach(() => {
      mockApiService.getAccountById.and.returnValue(of({ data: detail, errors: null }));
    });

    it('should_SetSelectedAccountId', async () => {
      await store.selectAccount('a1');
      expect(store.selectedAccountId()).toBe('a1');
    });

    it('should_SetFormMode_View', async () => {
      store.setFormMode('edit');
      await store.selectAccount('a1');
      expect(store.formMode()).toBe('view');
    });

    it('should_LoadAccountDetail_FromApi', async () => {
      await store.selectAccount('a1');
      expect(mockApiService.getAccountById).toHaveBeenCalledWith('a1');
      expect(store.selectedAccountDetail()).toEqual(detail);
    });

    it('apiError_Should_SetDetailToNull', async () => {
      mockApiService.getAccountById.and.returnValue(throwError(() => new Error()));
      await store.selectAccount('a1');
      expect(store.selectedAccountDetail()).toBeNull();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 5 — synchronous state methods
  // ─────────────────────────────────────────────────────────────────────────
  describe('synchronous state methods', () => {
    it('setFormMode_Should_UpdateFormMode', () => {
      store.setFormMode('edit');
      expect(store.formMode()).toBe('edit');
    });

    it('setCreateMode_Should_SetFormMode_Create', () => {
      store.setCreateMode('p1');
      expect(store.formMode()).toBe('create');
    });

    it('setCreateMode_Should_SetParentIdForCreate', () => {
      store.setCreateMode('p1');
      expect(store.parentIdForCreate()).toBe('p1');
    });

    it('setCreateMode_Null_Should_SetParentIdNull', () => {
      store.setCreateMode(null);
      expect(store.parentIdForCreate()).toBeNull();
    });

    it('setCreateMode_Should_ClearSelection', () => {
      store.setCreateMode('p1');
      expect(store.selectedAccountId()).toBeNull();
      expect(store.selectedAccountDetail()).toBeNull();
    });

    it('clearSelection_Should_ResetAll', () => {
      store.setFormMode('edit');
      store.clearSelection();
      expect(store.selectedAccountId()).toBeNull();
      expect(store.selectedAccountDetail()).toBeNull();
      expect(store.formMode()).toBe('view');
    });

    it('setSearchQuery_Should_UpdateQuery', () => {
      store.setSearchQuery('tiền mặt');
      expect(store.searchQuery()).toBe('tiền mặt');
    });

    it('setStatusFilter_Should_UpdateFilter', () => {
      store.setStatusFilter('inactive');
      expect(store.statusFilter()).toBe('inactive');
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 6 — createAccount
  // ─────────────────────────────────────────────────────────────────────────
  describe('createAccount', () => {
    const cmd = makeCreateCmd();

    beforeEach(() => {
      mockApiService.createAccount.and.returnValue(
        of({ data: { id: 'new-id', rowVersion: 1 }, errors: null })
      );
    });

    it('should_CallApiWith_Command', async () => {
      await store.createAccount(cmd);
      expect(mockApiService.createAccount).toHaveBeenCalledWith(cmd);
    });

    it('success_Should_SetFormMode_View_And_ClearSaving', async () => {
      await store.createAccount(cmd);
      expect(store.formMode()).toBe('view');
      expect(store.saving()).toBe(false);
    });

    it('success_Should_ReloadTree', async () => {
      await store.createAccount(cmd);
      expect(mockApiService.getAccountTree).toHaveBeenCalled();
    });

    it('apiError_Should_SetErrorMessage', async () => {
      mockApiService.createAccount.and.returnValue(
        throwError(() => ({ error: { message: 'Duplicate account number' } }))
      );
      await expectAsync(store.createAccount(cmd)).toBeRejected();
      expect(store.error()).toBe('Duplicate account number');
    });

    it('apiError_Should_ClearSaving_And_Rethrow', async () => {
      mockApiService.createAccount.and.returnValue(throwError(() => ({ error: {} })));
      await expectAsync(store.createAccount(cmd)).toBeRejected();
      expect(store.saving()).toBe(false);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 7 — updateAccount
  // ─────────────────────────────────────────────────────────────────────────
  describe('updateAccount', () => {
    const cmd = makeUpdateCmd();
    const detail = makeDetailDto({ accountId: 'a1', accountNumber: '111' });

    beforeEach(() => {
      mockApiService.updateAccount.and.returnValue(of({ data: undefined as unknown as void, errors: null }));
      mockApiService.getAccountById.and.returnValue(of({ data: detail, errors: null }));
    });

    it('should_CallApiWith_IdAndCommand', async () => {
      await store.updateAccount('a1', cmd);
      expect(mockApiService.updateAccount).toHaveBeenCalledWith('a1', cmd);
    });

    it('success_Should_SetFormMode_View_And_ClearSaving', async () => {
      await store.updateAccount('a1', cmd);
      expect(store.formMode()).toBe('view');
      expect(store.saving()).toBe(false);
    });

    it('success_Should_ReloadTree_And_ReselectAccount', async () => {
      await store.updateAccount('a1', cmd);
      expect(mockApiService.getAccountTree).toHaveBeenCalled();
      expect(mockApiService.getAccountById).toHaveBeenCalledWith('a1');
    });

    it('apiError_Should_SetErrorDetail', async () => {
      mockApiService.updateAccount.and.returnValue(
        throwError(() => ({ error: { detail: 'Row version mismatch' } }))
      );
      await expectAsync(store.updateAccount('a1', cmd)).toBeRejected();
      expect(store.error()).toBe('Row version mismatch');
    });

    it('apiError_Should_ClearSaving_And_Rethrow', async () => {
      mockApiService.updateAccount.and.returnValue(throwError(() => ({ error: {} })));
      await expectAsync(store.updateAccount('a1', cmd)).toBeRejected();
      expect(store.saving()).toBe(false);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 8 — deleteAccount
  // ─────────────────────────────────────────────────────────────────────────
  describe('deleteAccount', () => {
    beforeEach(() => {
      mockApiService.deleteAccount.and.returnValue(of(undefined));
    });

    it('should_CallApiWith_IdAndRowVersion', async () => {
      await store.deleteAccount('a1', 3);
      expect(mockApiService.deleteAccount).toHaveBeenCalledWith('a1', 3);
    });

    it('success_Should_ClearSelection_And_SetView', async () => {
      await store.deleteAccount('a1', 3);
      expect(store.selectedAccountId()).toBeNull();
      expect(store.selectedAccountDetail()).toBeNull();
      expect(store.formMode()).toBe('view');
    });

    it('success_Should_ReloadTree', async () => {
      await store.deleteAccount('a1', 3);
      expect(mockApiService.getAccountTree).toHaveBeenCalled();
    });

    it('apiError_Should_SetErrorMessage', async () => {
      mockApiService.deleteAccount.and.returnValue(
        throwError(() => ({ error: { message: 'Account has transactions' } }))
      );
      await expectAsync(store.deleteAccount('a1', 3)).toBeRejected();
      expect(store.error()).toBe('Account has transactions');
    });

    it('apiError_Should_ClearSaving', async () => {
      mockApiService.deleteAccount.and.returnValue(throwError(() => ({ error: {} })));
      await expectAsync(store.deleteAccount('a1', 3)).toBeRejected();
      expect(store.saving()).toBe(false);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 9 — importCoa
  // ─────────────────────────────────────────────────────────────────────────
  describe('importCoa', () => {
    const importData = { imported: 150, skipped: 10, overwritten: 3, errors: [] };

    beforeEach(() => {
      mockApiService.importCoa.and.returnValue(of({ data: importData, errors: null }));
    });

    it('should_CallApiWith_StandardAndConflictResolution', async () => {
      await store.importCoa('TT99', 'skip');
      expect(mockApiService.importCoa).toHaveBeenCalledWith('TT99', 'skip');
    });

    it('success_Should_ReturnImportStats', async () => {
      const result = await store.importCoa('TT133', 'overwrite');
      expect(result.imported).toBe(150);
      expect(result.skipped).toBe(10);
      expect(result.overwritten).toBe(3);
    });

    it('success_Should_ReloadTree_And_ClearSaving', async () => {
      await store.importCoa('TT99', 'skip');
      expect(mockApiService.getAccountTree).toHaveBeenCalled();
      expect(store.saving()).toBe(false);
    });

    it('apiError_Should_SetErrorDetail_And_Rethrow', async () => {
      mockApiService.importCoa.and.returnValue(
        throwError(() => ({ error: { detail: 'Invalid standard code' } }))
      );
      await expectAsync(store.importCoa('TT99', 'skip')).toBeRejected();
      expect(store.error()).toBe('Invalid standard code');
      expect(store.saving()).toBe(false);
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 10 — toggleExpanded / expandAll / collapseAll
  // ─────────────────────────────────────────────────────────────────────────
  describe('toggleExpanded / expandAll / collapseAll', () => {
    beforeEach(async () => {
      await store.loadTree();
    });

    it('toggleExpanded_Should_AddId_WhenNotPresent', () => {
      store.toggleExpanded('r1');
      expect(store.expandedNodeIds().has('r1')).toBe(true);
    });

    it('toggleExpanded_Should_RemoveId_WhenAlreadyPresent', () => {
      store.toggleExpanded('r1');
      store.toggleExpanded('r1');
      expect(store.expandedNodeIds().has('r1')).toBe(false);
    });

    it('toggleExpanded_Should_SaveToLocalStorage', () => {
      store.toggleExpanded('r1');
      expect(localStorage.setItem).toHaveBeenCalled();
    });

    it('expandAll_Should_IncludeAllParentAccounts', () => {
      store.expandAll();
      // ROOT and PARENT have isParent=true
      expect(store.expandedNodeIds().has('r1')).toBe(true);
      expect(store.expandedNodeIds().has('p1')).toBe(true);
    });

    it('expandAll_Should_NotIncludeLeafAccounts', () => {
      store.expandAll();
      // ACTIVE (a1) and INACTIVE (i1) are not parents
      expect(store.expandedNodeIds().has('a1')).toBe(false);
      expect(store.expandedNodeIds().has('i1')).toBe(false);
    });

    it('collapseAll_Should_ClearAllExpandedIds', () => {
      store.toggleExpanded('r1');
      store.toggleExpanded('p1');
      store.collapseAll();
      expect(store.expandedNodeIds().size).toBe(0);
    });

    it('collapseAll_Should_SaveEmptySetToLocalStorage', () => {
      store.collapseAll();
      expect(localStorage.setItem).toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 11 — selectedAccount computed
  // ─────────────────────────────────────────────────────────────────────────
  describe('selectedAccount computed', () => {
    beforeEach(async () => {
      await store.loadTree();
    });

    it('should_ReturnNull_WhenNothingSelected', () => {
      expect(store.selectedAccount()).toBeNull();
    });

    it('should_ReturnMatchingAccount_AfterSelectAccount', async () => {
      mockApiService.getAccountById.and.returnValue(of({ data: makeDetailDto({ accountId: 'a1', accountNumber: '111' }), errors: null }));
      await store.selectAccount('a1');
      expect(store.selectedAccount()?.accountId).toBe('a1');
    });

    it('should_ReturnNull_WhenSelectedIdNotInAccounts', async () => {
      mockApiService.getAccountById.and.returnValue(throwError(() => new Error()));
      await store.selectAccount('non-existent-id');
      // selectedAccountId is set but not found in accounts
      expect(store.selectedAccount()).toBeNull();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 12 — filteredAccounts with search query
  // ─────────────────────────────────────────────────────────────────────────
  describe('filteredAccounts with search query', () => {
    beforeEach(async () => {
      await store.loadTree();
    });

    it('nonEmptyQuery_Should_CallBuilderFilterTree', () => {
      store.setSearchQuery('tiền');
      const _ = store.filteredAccounts(); // trigger computed
      expect(mockBuilderService.filterTree).toHaveBeenCalledWith(jasmine.any(Array), 'tiền');
    });

    it('emptyQuery_Should_NOT_CallBuilderFilterTree', () => {
      mockBuilderService.filterTree.calls.reset();
      store.setSearchQuery('');
      const _ = store.filteredAccounts(); // trigger computed
      expect(mockBuilderService.filterTree).not.toHaveBeenCalled();
    });

    it('whitespaceQuery_Should_NOT_CallBuilderFilterTree', () => {
      mockBuilderService.filterTree.calls.reset();
      store.setSearchQuery('   ');
      const _ = store.filteredAccounts();
      expect(mockBuilderService.filterTree).not.toHaveBeenCalled();
    });
  });

  // ─────────────────────────────────────────────────────────────────────────
  // Group 13 — treeNodes computed
  // ─────────────────────────────────────────────────────────────────────────
  describe('treeNodes computed', () => {
    const mockTreeNode = { key: 'mock', label: 'Mock', data: null };

    beforeEach(async () => {
      mockBuilderService.buildTree.and.returnValue([mockTreeNode]);
      await store.loadTree();
    });

    it('should_CallBuildTree_And_ReturnNodes', () => {
      const nodes = store.treeNodes();
      expect(mockBuilderService.buildTree).toHaveBeenCalled();
      expect(nodes).toEqual([mockTreeNode]);
    });

    it('should_PassExpandedNodeIds_ToBuildTree', () => {
      store.toggleExpanded('r1');
      const _ = store.treeNodes();
      const callArgs = mockBuilderService.buildTree.calls.mostRecent().args;
      const expandedArg = callArgs[1] as Set<string>;
      expect(expandedArg.has('r1')).toBe(true);
    });

    it('should_ApplyStatusFilter_BeforeCallingBuildTree', () => {
      store.setStatusFilter('active');
      const _ = store.treeNodes();
      const callArgs = mockBuilderService.buildTree.calls.mostRecent().args;
      const accountsArg = callArgs[0] as AccountTreeNodeDto[];
      // inactive account i1 should be excluded from active filter
      expect(accountsArg.find((a: AccountTreeNodeDto) => a.accountId === 'i1')).toBeUndefined();
    });
  });
});
