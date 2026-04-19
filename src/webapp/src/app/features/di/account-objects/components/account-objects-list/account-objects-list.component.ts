import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';
import { GridActionBarComponent } from '../../../../../shared/components/toolbar/grid-action-bar/grid-action-bar.component';
import { SearchBarComponent } from '../../../../../shared/components/toolbar/search-bar/search-bar.component';
import { AccountObjectsStore } from '../../store/account-objects.store';
import { AccountObjectListItem } from '../../../models/master-data.models';

@Component({
  selector: 'app-account-objects-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    TabsModule,
    ButtonModule,
    TagModule,
    SelectModule,
    ConfirmDialogModule,
    ToastModule,
    TooltipModule,
    TranslateModule,
    GridActionBarComponent,
    SearchBarComponent,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './account-objects-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; }
      .page-container { display: flex; flex-direction: column; height: 100%; padding: 16px; box-sizing: border-box; gap: 8px; }
      .table-wrapper { flex: 1; overflow: hidden; }
    `,
  ],
})
export class AccountObjectsListComponent implements OnInit {
  readonly store = inject(AccountObjectsStore);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  selectedRow = signal<AccountObjectListItem | null>(null);

  typeFilterOptions = [
    { label: 'Táº¥t cáº£ loáº¡i', value: 0 },
    { label: 'KhÃ¡ch hÃ ng', value: 1 },
    { label: 'NhÃ  cung cáº¥p', value: 2 },
    { label: 'NhÃ¢n viÃªn', value: 4 },
  ];

  selectedTypeFilter = signal<number>(0);

  selectedStatus = signal<string>('all');

  onStatusChange(status: string | number | undefined): void {
    const s = String(status || 'all');
    this.selectedStatus.set(s);
    this.store.setFilters({ status: s });
  }

  @ViewChild('searchBar') searchBarRef: any;

  ngOnInit(): void {
    this.store.loadList();
  }

  @HostListener('document:keydown.F3')
  onF3(): void {
    // Focus search bar if available
  }

  onSearch(event: { keyword: string }): void {
    this.store.setFilters({ search: event.keyword });
  }

  onTypeFilterChange(event: any): void {
    this.store.setFilters({ typeFilter: event.value });
  }

  onEdit(): void {
    const id = this.store.selectedId();
    if (id) {
      this.store.openEdit(id);
    }
  }

  onDelete(): void {
    const id = this.store.selectedId();
    if (!id) return;
    this.confirmationService.confirm({
      message: 'Báº¡n cÃ³ cháº¯c muá»‘n xÃ³a Ä‘á»‘i tÆ°á»£ng nÃ y?',
      header: 'XÃ¡c nháº­n xÃ³a',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.store.deleteItem(id).then(
          () => {
            this.selectedRow.set(null);
            this.messageService.add({
              severity: 'success',
              summary: 'ThÃ nh cÃ´ng',
              detail: 'ÄÃ£ xÃ³a Ä‘á»‘i tÆ°á»£ng thÃ nh cÃ´ng',
            });
          },
          (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Lá»—i',
              detail: err?.error?.errors?.[0] ?? 'KhÃ´ng thá»ƒ xÃ³a Ä‘á»‘i tÆ°á»£ng',
            });
          }
        );
      },
    });
  }

  onImport(): void {
    this.messageService.add({ severity: 'info', summary: 'ThÃ´ng bÃ¡o', detail: 'Chá»©c nÄƒng Ä‘ang phÃ¡t triá»ƒn' });
  }

  onExport(): void {
    this.messageService.add({ severity: 'info', summary: 'ThÃ´ng bÃ¡o', detail: 'Chá»©c nÄƒng Ä‘ang phÃ¡t triá»ƒn' });
  }

  onPrint(): void {
    this.messageService.add({ severity: 'info', summary: 'ThÃ´ng bÃ¡o', detail: 'Chá»©c nÄƒng Ä‘ang phÃ¡t triá»ƒn' });
  }

  onRowSelect(event: any): void {
    this.store.selectItem(event.data.id);
  }

  onRowUnselect(): void {
    this.store.selectItem(null);
  }

  onPage(event: any): void {
    this.store.setPage(event.page + 1);
  }

  hasTypeBit(objectType: number, bit: number): boolean {
    return (objectType & bit) !== 0;
  }
}

