import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InventoryItemsStore } from './store/inventory-items.store';
import { InventoryItemsApiService } from './services/inventory-items-api.service';
import { InventoryItemsListComponent } from './components/inventory-items-list/inventory-items-list.component';
import { InventoryItemFormComponent } from './components/inventory-item-form/inventory-item-form.component';

@Component({
  selector: 'app-inventory-items-page',
  standalone: true,
  imports: [CommonModule, InventoryItemsListComponent, InventoryItemFormComponent],
  providers: [InventoryItemsStore, InventoryItemsApiService],
  template: `
    @if (store.formMode() === null) {
      <app-inventory-items-list />
    } @else {
      <app-inventory-item-form />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; }
    `,
  ],
})
export class InventoryItemsPageComponent {
  readonly store = inject(InventoryItemsStore);
}
