import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountObjectsStore } from './store/account-objects.store';
import { AccountObjectsApiService } from './services/account-objects-api.service';
import { AccountObjectsListComponent } from './components/account-objects-list/account-objects-list.component';
import { AccountObjectFormComponent } from './components/account-object-form/account-object-form.component';

@Component({
  selector: 'app-account-objects-page',
  standalone: true,
  imports: [CommonModule, AccountObjectsListComponent, AccountObjectFormComponent],
  providers: [AccountObjectsStore, AccountObjectsApiService],
  templateUrl: './account-objects-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; }
    `,
  ],
})
export class AccountObjectsPageComponent {
  readonly store = inject(AccountObjectsStore);
}
