import { TestBed } from '@angular/core/testing';
import { AccountTreeBuilderService } from './account-tree-builder.service';
import { AccountCategoryKind, AccountTreeNodeDto } from '../../models/account.models';

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
    isPostableInForeignCurrency: overrides.isPostableInForeignCurrency ?? false,
    hasTransactions: overrides.hasTransactions ?? false,
    parentId: overrides.parentId ?? null,
    children: overrides.children ?? [],
  };
}

describe('AccountTreeBuilderService', () => {
  let service: AccountTreeBuilderService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AccountTreeBuilderService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('buildTree', () => {
    it('handles_EmptyList_Should_ReturnEmptyArray', () => {
      const result = service.buildTree([]);
      expect(result).toEqual([]);
    });

    it('handles_SingleRootAccount_Should_ReturnOneRootNode', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '111', accountName: 'Tiền mặt' }),
      ];
      const result = service.buildTree(accounts);
      expect(result.length).toBe(1);
      expect(result[0].key).toBe('id1');
      expect(result[0].label).toBe('Tiền mặt');
      expect(result[0].children?.length).toBe(0);
    });

    it('builds_MultiLevelHierarchy_Should_NestChildrenUnderParents', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '1', accountName: 'Root', isParent: true, grade: 1 }),
        makeAccount({ accountId: 'id2', accountNumber: '11', accountName: 'Level 2', isParent: true, grade: 2, parentId: 'id1' }),
        makeAccount({ accountId: 'id3', accountNumber: '111', accountName: 'Level 3', grade: 3, parentId: 'id2' }),
      ];
      const result = service.buildTree(accounts);
      expect(result.length).toBe(1);
      expect(result[0].children?.length).toBe(1);
      expect(result[0].children![0].children?.length).toBe(1);
      expect(result[0].children![0].children![0].key).toBe('id3');
    });

    it('builds_FlatList_Should_ReturnMultipleRoots', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '1', accountName: 'TÀI SẢN', grade: 1 }),
        makeAccount({ accountId: 'id2', accountNumber: '2', accountName: 'NỢ PHẢI TRẢ', grade: 1 }),
        makeAccount({ accountId: 'id3', accountNumber: '3', accountName: 'VỐN CHỦ SỞ HỮU', grade: 1 }),
      ];
      const result = service.buildTree(accounts);
      expect(result.length).toBe(3);
    });

    it('sorts_ByAccountNumber_AtEachLevel', () => {
      const accounts = [
        makeAccount({ accountId: 'id3', accountNumber: '113', grade: 2, parentId: 'id1' }),
        makeAccount({ accountId: 'id2', accountNumber: '112', grade: 2, parentId: 'id1' }),
        makeAccount({ accountId: 'id1', accountNumber: '11', isParent: true, grade: 1 }),
        makeAccount({ accountId: 'id4', accountNumber: '111', grade: 2, parentId: 'id1' }),
      ];
      const result = service.buildTree(accounts);
      // Root sorted
      expect(result[0].key).toBe('id1');
      // Children sorted by accountNumber
      const children = result[0].children!;
      expect(children[0].key).toBe('id4'); // 111
      expect(children[1].key).toBe('id2'); // 112
      expect(children[2].key).toBe('id3'); // 113
    });

    it('sets_IsParent_Account_AsNonLeaf_AndNonSelectable', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '11', isParent: true }),
      ];
      const result = service.buildTree(accounts);
      expect(result[0].leaf).toBe(false);
      expect(result[0].selectable).toBe(false);
    });

    it('sets_LeafAccount_AsLeaf_AndSelectable', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '1111', isParent: false }),
      ];
      const result = service.buildTree(accounts);
      expect(result[0].leaf).toBe(true);
      expect(result[0].selectable).toBe(true);
    });
  });

  describe('filterTree', () => {
    it('filterTree_EmptyQuery_Should_ReturnAllAccounts', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '111' }),
        makeAccount({ accountId: 'id2', accountNumber: '112' }),
      ];
      const result = service.filterTree(accounts, '');
      expect(result.length).toBe(2);
    });

    it('filterTree_MatchingCode_Should_ReturnMatchingAndAncestors', () => {
      const accounts = [
        makeAccount({ accountId: 'id1', accountNumber: '1', isParent: true }),
        makeAccount({ accountId: 'id2', accountNumber: '11', isParent: true, parentId: 'id1' }),
        makeAccount({ accountId: 'id3', accountNumber: '111', parentId: 'id2' }),
        makeAccount({ accountId: 'id4', accountNumber: '112', parentId: 'id2' }),
      ];
      const result = service.filterTree(accounts, '111');
      const ids = result.map((a) => a.accountId);
      expect(ids).toContain('id3'); // match
      expect(ids).toContain('id2'); // ancestor
      expect(ids).toContain('id1'); // ancestor
      expect(ids).not.toContain('id4'); // no match
    });
  });

  describe('highlightMatch', () => {
    it('highlightMatch_EmptyQuery_Should_ReturnOriginalText', () => {
      const result = service.highlightMatch('Tiền mặt', '');
      expect(result).toBe('Tiền mặt');
    });

    it('highlightMatch_MatchFound_Should_WrapInMarkTag', () => {
      const result = service.highlightMatch('Tiền mặt', 'mặt');
      expect(result).toContain('<mark>mặt</mark>');
    });

    it('highlightMatch_CaseInsensitive_Should_WrapMatch', () => {
      const result = service.highlightMatch('Cash and Equivalents', 'cash');
      expect(result).toContain('<mark>');
    });
  });
});
