import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { AccountTreePageComponent } from '../account-tree-page.component';
import { AccountTreeStore } from '../store/account-tree.store';

export const accountTreeDirtyGuard: CanDeactivateFn<AccountTreePageComponent> = (component) => {
  const store = inject(AccountTreeStore);
  const mode = store.formMode();
  if (mode === 'edit' || mode === 'create') {
    return confirm('Bạn có thay đổi chưa lưu. Rời khỏi trang?');
  }
  return true;
};
