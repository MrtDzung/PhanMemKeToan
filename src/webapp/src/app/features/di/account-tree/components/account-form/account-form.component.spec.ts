import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { AccountFormComponent } from './account-form.component';
import { AccountTreeStore } from '../../store/account-tree.store';
import { AccountValidationService } from '../../services/account-validation.service';
import {
  AccountCategoryKind,
  AccountObjectType,
  AccountDetailDto,
  AccountTreeNodeDto,
} from '../../../models/account.models';

// --- Test helpers ---

function makeNode(
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

function makeDetail(
  overrides: Partial<AccountDetailDto> & { accountId: string; accountNumber: string }
): AccountDetailDto {
  return {
    accountId: overrides.accountId,
    accountNumber: overrides.accountNumber,
    accountName: overrides.accountName ?? 'Test Account',
    accountNameEnglish: overrides.accountNameEnglish ?? null,
    parentId: overrides.parentId ?? null,
    parentNumber: overrides.parentNumber ?? null,
    parentName: overrides.parentName ?? null,
    grade: overrides.grade ?? 1,
    isParent: overrides.isParent ?? false,
    accountCategoryKind: overrides.accountCategoryKind ?? AccountCategoryKind.Debit,
    inactive: overrides.inactive ?? false,
    isPostableInForeignCurrency: overrides.isPostableInForeignCurrency ?? false,
    hasTransactions: overrides.hasTransactions ?? false,
    accountObjectType: overrides.accountObjectType ?? AccountObjectType.None,
    detailByAccountObject: overrides.detailByAccountObject ?? false,
    detailByBankAccount: overrides.detailByBankAccount ?? false,
    detailByJob: overrides.detailByJob ?? false,
    detailByProjectWork: overrides.detailByProjectWork ?? false,
    detailByOrder: overrides.detailByOrder ?? false,
    detailByContract: overrides.detailByContract ?? false,
    detailByExpenseItem: overrides.detailByExpenseItem ?? false,
    detailByDepartment: overrides.detailByDepartment ?? false,
    detailByListItem: overrides.detailByListItem ?? false,
    detailByPuContract: overrides.detailByPuContract ?? false,
    rowVersion: overrides.rowVersion ?? 0,
    createdAt: '2026-01-01T00:00:00Z',
    createdBy: 'admin',
    modifiedAt: null,
    modifiedBy: null,
    children: overrides.children ?? [],
  };
}

// --- Test suite ---

describe('AccountFormComponent', () => {
  let fixture: ComponentFixture<AccountFormComponent>;
  let component: AccountFormComponent;
  let mockStore: {
    accounts: WritableSignal<AccountTreeNodeDto[]>;
    saving: WritableSignal<boolean>;
    updateAccount: jasmine.Spy;
    createAccount: jasmine.Spy;
  };
  let mockValidation: jasmine.SpyObj<AccountValidationService>;

  // Rule: configureTestBed() MUST be called exactly ONCE per test, in that test's own beforeEach.
  // Do NOT call it again inside an it() that already has a beforeEach calling it.
  function configureTestBed(storeAccounts: AccountTreeNodeDto[] = []): void {
    mockStore = {
      accounts: signal(storeAccounts),
      saving: signal(false),
      updateAccount: jasmine.createSpy('updateAccount').and.returnValue(Promise.resolve()),
      createAccount: jasmine.createSpy('createAccount').and.returnValue(Promise.resolve()),
    };
    mockValidation = jasmine.createSpyObj<AccountValidationService>('AccountValidationService', [
      'validatePrefix',
    ]);
    mockValidation.validatePrefix.and.returnValue(true);

    TestBed.configureTestingModule({
      imports: [AccountFormComponent, NoopAnimationsModule],
      providers: [
        { provide: AccountTreeStore, useValue: mockStore },
        { provide: AccountValidationService, useValue: mockValidation },
      ],
    });

    fixture = TestBed.createComponent(AccountFormComponent);
    component = fixture.componentInstance;
  }

  // -----------------------------------------------------------------------
  // GROUP 1a: Form initialization — create mode, no parentId
  // -----------------------------------------------------------------------
  describe('initialization — create mode (no parentId)', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('init_CreateMode_Should_HaveEmptyFormFields', () => {
      expect(component.form.get('accountNumber')?.value).toBe('');
      expect(component.form.get('accountName')?.value).toBe('');
    });

    it('init_CreateModeNoParentId_Should_HaveNullSelectedParent', () => {
      expect(component.selectedParent).toBeNull();
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 1b: Form initialization — create mode, with parentId input
  // detectChanges is called in each test (AFTER setInput) so patchForm()
  // reads the correct parentId during ngOnInit.
  // -----------------------------------------------------------------------
  describe('initialization — create mode (with parentId)', () => {
    beforeEach(() => {
      configureTestBed(); // do NOT call detectChanges here
    });

    it('init_CreateModeWithParentIdFoundInStore_Should_PrePopulateSelectedParent', () => {
      const parentNode = makeNode({ accountId: 'p-1', accountNumber: '111', accountName: 'Tiền mặt' });
      mockStore.accounts.set([parentNode]);
      fixture.componentRef.setInput('parentId', 'p-1');
      fixture.detectChanges(); // triggers ngOnInit → patchForm with parentId='p-1'

      expect(component.selectedParent?.accountId).toBe('p-1');
      expect(component.selectedParent?.displayLabel).toBe('111 — Tiền mặt');
    });

    it('init_CreateModeWithParentIdNotInStore_Should_LeaveSelectedParentNull', () => {
      // store is empty by default
      fixture.componentRef.setInput('parentId', 'unknown-id');
      fixture.detectChanges(); // triggers ngOnInit → no match in store → null

      expect(component.selectedParent).toBeNull();
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 2a: patchForm — edit mode (empty store)
  // -----------------------------------------------------------------------
  describe('patchForm — edit mode (empty store)', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('patchForm_EditMode_Should_PatchFormWithAccountValues', () => {
      const acc = makeDetail({
        accountId: 'acc-1',
        accountNumber: '1111',
        accountName: 'Tiền Việt Nam',
        accountCategoryKind: AccountCategoryKind.Credit,
        isPostableInForeignCurrency: true,
        inactive: false,
        rowVersion: 3,
      });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.form.get('accountNumber')?.value).toBe('1111');
      expect(component.form.get('accountName')?.value).toBe('Tiền Việt Nam');
      expect(component.form.get('accountCategoryKind')?.value).toBe(AccountCategoryKind.Credit);
      expect(component.form.get('isPostableInForeignCurrency')?.value).toBeTrue();
      expect(component.form.get('rowVersion')?.value).toBe(3);
    });

    it('patchForm_EditModeParentNotInStore_Should_UseFallbackFromDetail', () => {
      const acc = makeDetail({
        accountId: 'acc-1',
        accountNumber: '1111',
        parentId: 'p-1',
        parentNumber: '111',
        parentName: 'Tiền mặt',
      });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.selectedParent?.accountId).toBe('p-1');
      expect(component.selectedParent?.accountNumber).toBe('111');
      expect(component.selectedParent?.displayLabel).toBe('111 — Tiền mặt');
    });

    it('patchForm_EditModeNoParent_Should_SetSelectedParentNull', () => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1', parentId: null });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.selectedParent).toBeNull();
    });

    it('patchForm_EditModeHasTransactions_Should_DisableAccountNumberField', () => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', hasTransactions: true });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.form.get('accountNumber')?.disabled).toBeTrue();
    });

    it('patchForm_EditModeNoTransactions_Should_EnableAccountNumberField', () => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', hasTransactions: false });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.form.get('accountNumber')?.disabled).toBeFalse();
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 2b: patchForm — edit mode with parent in store
  // -----------------------------------------------------------------------
  describe('patchForm — edit mode (parent in store)', () => {
    const parentNode = makeNode({ accountId: 'p-1', accountNumber: '111', accountName: 'Tiền mặt' });

    beforeEach(() => {
      configureTestBed([parentNode]);
      fixture.detectChanges();
    });

    it('patchForm_EditModeParentFoundInStore_Should_SetSelectedParent', () => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', parentId: 'p-1' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      expect(component.selectedParent?.accountId).toBe('p-1');
      expect(component.selectedParent?.displayLabel).toBe('111 — Tiền mặt');
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 3: onSave — create mode
  // -----------------------------------------------------------------------
  describe('onSave — create mode', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('onSave_InvalidForm_Should_NotCallCreateAccount', () => {
      // form has empty required fields by default
      component.onSave();
      expect(mockStore.createAccount).not.toHaveBeenCalled();
    });

    it('onSave_InvalidForm_Should_MarkAllFieldsTouched', () => {
      component.onSave();
      expect(component.form.touched).toBeTrue();
    });

    it('onSave_ValidFormNoParent_Should_CreateWithUndefinedParentId', fakeAsync(() => {
      component.form.patchValue({ accountNumber: '9999', accountName: 'Test TK' });
      component.onSave();
      tick();

      expect(mockStore.createAccount).toHaveBeenCalledOnceWith(
        jasmine.objectContaining({ accountNumber: '9999', parentId: undefined })
      );
    }));

    it('onSave_ValidFormWithSelectedParent_Should_CreateWithParentId', fakeAsync(() => {
      const parentNode = makeNode({ accountId: 'p-id', accountNumber: '9', accountName: 'Root' });
      component.selectedParent = { ...parentNode, displayLabel: '9 — Root' };
      component.form.patchValue({ accountNumber: '99', accountName: 'Child TK' });
      component.onSave();
      tick();

      const cmd = mockStore.createAccount.calls.first().args[0];
      expect(cmd.parentId).toBe('p-id');
    }));

    it('onSave_PrefixValidationFails_Should_SetPrefixErrorOnAccountNumber', fakeAsync(() => {
      mockValidation.validatePrefix.and.returnValue(false);
      const parentNode = makeNode({ accountId: 'p-id', accountNumber: '111', accountName: 'TM' });
      component.selectedParent = { ...parentNode, displayLabel: '111 — TM' };
      component.form.patchValue({ accountNumber: '999', accountName: 'Bad Prefix' });
      component.onSave();
      tick();

      expect(mockStore.createAccount).not.toHaveBeenCalled();
      expect(component.form.get('accountNumber')?.errors?.['prefixError']).toBeTrue();
    }));

    it('onSave_ValidCreate_Should_EmitSavedEvent', fakeAsync(() => {
      const savedSpy = jasmine.createSpy('saved');
      component.saved.subscribe(savedSpy);
      component.form.patchValue({ accountNumber: '9999', accountName: 'Test TK' });
      component.onSave();
      tick();

      expect(savedSpy).toHaveBeenCalledTimes(1);
    }));
  });

  // -----------------------------------------------------------------------
  // GROUP 4: onSave — edit mode
  // -----------------------------------------------------------------------
  describe('onSave — edit mode', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('onSave_EditModeInvalidForm_Should_NotCallUpdateAccount', () => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', accountName: 'TK' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      component.form.patchValue({ accountNumber: '', accountName: '' });
      component.onSave();

      expect(mockStore.updateAccount).not.toHaveBeenCalled();
    });

    it('onSave_EditModeValidFormWithParent_Should_UpdateWithParentId', fakeAsync(() => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', accountName: 'TK1111' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      const parentNode = makeNode({ accountId: 'p-id', accountNumber: '111', accountName: 'TM' });
      component.selectedParent = { ...parentNode, displayLabel: '111 — TM' };
      component.onSave();
      tick();

      expect(mockStore.updateAccount).toHaveBeenCalledOnceWith(
        'acc-1',
        jasmine.objectContaining({ parentId: 'p-id' })
      );
    }));

    it('onSave_EditModeValidFormNoParent_Should_UpdateWithUndefinedParentId', fakeAsync(() => {
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', accountName: 'TK1111' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      component.selectedParent = null;
      component.onSave();
      tick();

      const cmd = mockStore.updateAccount.calls.first().args[1];
      expect(cmd.parentId).toBeUndefined();
    }));

    it('onSave_EditModePrefixFails_Should_SetPrefixErrorAndNotUpdate', fakeAsync(() => {
      mockValidation.validatePrefix.and.returnValue(false);
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', accountName: 'TK' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      const parentNode = makeNode({ accountId: 'p-id', accountNumber: '222', accountName: 'Wrong' });
      component.selectedParent = { ...parentNode, displayLabel: '222 — Wrong' };
      component.onSave();
      tick();

      expect(mockStore.updateAccount).not.toHaveBeenCalled();
      expect(component.form.get('accountNumber')?.errors?.['prefixError']).toBeTrue();
    }));

    it('onSave_EditModeValid_Should_EmitSavedEvent', fakeAsync(() => {
      const savedSpy = jasmine.createSpy('saved');
      component.saved.subscribe(savedSpy);
      const acc = makeDetail({ accountId: 'acc-1', accountNumber: '1111', accountName: 'TK1111' });
      fixture.componentRef.setInput('account', acc);
      fixture.componentRef.setInput('isEditMode', true);
      fixture.detectChanges();

      component.selectedParent = null;
      component.onSave();
      tick();

      expect(savedSpy).toHaveBeenCalledTimes(1);
    }));
  });

  // -----------------------------------------------------------------------
  // GROUP 5: onSearchParent
  // -----------------------------------------------------------------------
  describe('onSearchParent', () => {
    const storeAccounts = [
      makeNode({ accountId: 'n1', accountNumber: '111', accountName: 'Tiền mặt' }),
      makeNode({ accountId: 'n2', accountNumber: '112', accountName: 'Ngân hàng' }),
      makeNode({ accountId: 'n3', accountNumber: '331', accountName: 'Nhà cung cấp' }),
    ];

    beforeEach(() => {
      configureTestBed(storeAccounts);
      fixture.detectChanges();
    });

    it('onSearchParent_QueryMatchesAccountNumber_Should_ReturnMatchingItems', () => {
      component.onSearchParent({ query: '111' });

      expect(component.parentSuggestions().some(r => r.accountId === 'n1')).toBeTrue();
    });

    it('onSearchParent_QueryMatchesAccountName_Should_ReturnMatchingItems', () => {
      component.onSearchParent({ query: 'ngân hàng' });

      expect(component.parentSuggestions().some(r => r.accountId === 'n2')).toBeTrue();
    });

    it('onSearchParent_CaseInsensitiveQuery_Should_ReturnMatch', () => {
      component.onSearchParent({ query: 'TIỀN MẶT' });

      expect(component.parentSuggestions().some(r => r.accountId === 'n1')).toBeTrue();
    });

    it('onSearchParent_CurrentAccountIsEdited_Should_ExcludeFromSuggestions', () => {
      const acc = makeDetail({ accountId: 'n1', accountNumber: '111', accountName: 'Tiền mặt' });
      fixture.componentRef.setInput('account', acc);
      fixture.detectChanges();

      component.onSearchParent({ query: '11' });

      expect(component.parentSuggestions().some(r => r.accountId === 'n1')).toBeFalse();
    });

    it('onSearchParent_ManyResults_Should_CapsAt20', () => {
      const manyAccounts = Array.from({ length: 25 }, (_, i) =>
        makeNode({ accountId: `id-${i}`, accountNumber: `${100 + i}`, accountName: `Account ${i}` })
      );
      // Update the signal directly — no need to reconfigure TestBed
      mockStore.accounts.set(manyAccounts);

      component.onSearchParent({ query: '1' });

      expect(component.parentSuggestions().length).toBeLessThanOrEqual(20);
    });

    it('onSearchParent_Results_Should_HaveDisplayLabel', () => {
      component.onSearchParent({ query: '111' });

      const item = component.parentSuggestions().find(r => r.accountId === 'n1');
      expect(item?.displayLabel).toBe('111 — Tiền mặt');
    });

    it('onSearchParent_NoMatch_Should_ReturnEmptyArray', () => {
      component.onSearchParent({ query: 'xyz-does-not-exist' });

      expect(component.parentSuggestions()).toEqual([]);
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 6: onParentSelected / onParentCleared
  // -----------------------------------------------------------------------
  describe('onParentSelected and onParentCleared', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('onParentSelected_Should_SetSelectedParent', () => {
      const node = makeNode({ accountId: 'p1', accountNumber: '111', accountName: 'TM' });
      const withLabel = { ...node, displayLabel: '111 — TM' };

      component.onParentSelected(withLabel);

      expect(component.selectedParent).toBe(withLabel);
    });

    it('onParentCleared_Should_SetSelectedParentToNull', () => {
      const node = makeNode({ accountId: 'p1', accountNumber: '111', accountName: 'TM' });
      component.selectedParent = { ...node, displayLabel: '111 — TM' };

      component.onParentCleared();

      expect(component.selectedParent).toBeNull();
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 7: isInvalid
  // -----------------------------------------------------------------------
  describe('isInvalid', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('isInvalid_UntouchedInvalidField_Should_ReturnFalse', () => {
      component.form.get('accountNumber')?.setValue('');

      expect(component.isInvalid('accountNumber')).toBeFalse();
    });

    it('isInvalid_TouchedInvalidField_Should_ReturnTrue', () => {
      component.form.get('accountNumber')?.markAsTouched();
      component.form.get('accountNumber')?.setValue('');

      expect(component.isInvalid('accountNumber')).toBeTrue();
    });

    it('isInvalid_DirtyInvalidField_Should_ReturnTrue', () => {
      component.form.get('accountNumber')?.markAsDirty();
      component.form.get('accountNumber')?.setValue('');

      expect(component.isInvalid('accountNumber')).toBeTrue();
    });

    it('isInvalid_TouchedValidField_Should_ReturnFalse', () => {
      component.form.get('accountNumber')?.markAsTouched();
      component.form.get('accountNumber')?.setValue('1111');

      expect(component.isInvalid('accountNumber')).toBeFalse();
    });

    it('isInvalid_UnknownFieldName_Should_ReturnFalse', () => {
      expect(component.isInvalid('nonExistentField')).toBeFalse();
    });
  });

  // -----------------------------------------------------------------------
  // GROUP 8: Keyboard shortcuts
  // -----------------------------------------------------------------------
  describe('keyboard shortcuts', () => {
    beforeEach(() => {
      configureTestBed();
      fixture.detectChanges();
    });

    it('onEscape_DirtyForm_Should_TriggerCancelConfirmation', () => {
      component.form.markAsDirty();
      const confirmSpy = spyOn<any>(component['confirmationService'], 'confirm');

      component.onEscape();

      expect(confirmSpy).toHaveBeenCalled();
    });

    it('onEscape_CleanForm_Should_EmitCancelled', () => {
      const cancelledSpy = jasmine.createSpy('cancelled');
      component.cancelled.subscribe(cancelledSpy);

      component.onEscape();

      expect(cancelledSpy).toHaveBeenCalledTimes(1);
    });

    it('onCtrlS_ValidForm_Should_CallCreateAccount', fakeAsync(() => {
      component.form.patchValue({ accountNumber: '9999', accountName: 'Test' });
      const mockEvent = new Event('keydown');
      spyOn(mockEvent, 'preventDefault');

      component.onCtrlS(mockEvent);
      tick();

      expect(mockStore.createAccount).toHaveBeenCalled();
    }));

    it('onCtrlS_InvalidForm_Should_NotCallCreateAccount', fakeAsync(() => {
      // form fields are empty = invalid
      const mockEvent = new Event('keydown');
      spyOn(mockEvent, 'preventDefault');

      component.onCtrlS(mockEvent);
      tick();

      expect(mockStore.createAccount).not.toHaveBeenCalled();
    }));
  });
});
