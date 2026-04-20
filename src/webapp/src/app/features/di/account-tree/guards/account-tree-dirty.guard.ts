import { CanDeactivateFn } from '@angular/router';
import { AccountTreePageComponent } from '../account-tree-page.component';

export const accountTreeDirtyGuard: CanDeactivateFn<AccountTreePageComponent> = (component) => {
  const mode = component.store.formMode();
  if (mode === 'edit' || mode === 'create') {
    return confirm('Bạn có thay đổi chưa lưu. Rời khỏi trang?');
  }
  return true;
};
