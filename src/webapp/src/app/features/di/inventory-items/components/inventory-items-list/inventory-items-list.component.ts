import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { TreeModule } from 'primeng/tree';
import { SplitterModule } from 'primeng/splitter';
import { PaginatorModule } from 'primeng/paginator';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService, TreeNode } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';
import { GridActionBarComponent } from '../../../../../shared/components/toolbar/grid-action-bar/grid-action-bar.component';
import { SearchBarComponent } from '../../../../../shared/components/toolbar/search-bar/search-bar.component';
import { InventoryItemsStore } from '../../store/inventory-items.store';
import { CategoryTreeNode, InventoryItemListItem } from '../../../models/master-data.models';

@Component({
  selector: 'app-inventory-items-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TagModule,
    SelectModule,
    TreeModule,
    SplitterModule,
    PaginatorModule,
    ConfirmDialogModule,
    ToastModule,
    TranslateModule,
    GridActionBarComponent,
    SearchBarComponent,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './inventory-items-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; }
      .page-container { display: flex; height: 100%; box-sizing: border-box; overflow: hidden; }
      .category-panel { display: flex; flex-direction: column; height: 100%; padding: 8px; overflow: hidden; }
      .category-title { font-size: 12px; font-weight: 600; color: var(--text-secondary); padding: 4px 8px 8px; text-transform: uppercase; letter-spacing: 0.04em; }
      .right-panel { display: flex; flex-direction: column; height: 100%; padding: 16px; box-sizing: border-box; gap: 8px; overflow: hidden; }
      .table-wrapper { flex: 1; overflow: hidden; }
      :host ::ng-deep .p-splitter { height: 100%; }
    `,
  ],
})
export class InventoryItemsListComponent implements OnInit {
  readonly store = inject(InventoryItemsStore);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  selectedRow = signal<InventoryItemListItem | null>(null);
  selectedTreeNode = signal<TreeNode | null>(null);

  itemTypeOptions = [
    { label: 'Tất cả loại', value: 0 },
    { label: 'Hàng hóa', value: 1 },
    { label: 'Nguyên vật liệu', value: 2 },
    { label: 'Thành phẩm', value: 3 },
    { label: 'Dịch vụ', value: 4 },
  ];

  selectedItemType = signal<number>(0);

  treeNodes = computed<TreeNode[]>(() =>
    this.buildTreeNodes(this.store.categories())
  );

  ngOnInit(): void {
    this.store.loadCategories();
    this.store.loadList();
  }

  @HostListener('document:keydown.f3')
  onF3(): void {
    // Focus search bar
  }

  buildTreeNodes(categories: CategoryTreeNode[]): TreeNode[] {
    const allNode: TreeNode = {
      label: 'Tất cả hàng hóa',
      data: null,
      expandedIcon: 'pi pi-folder-open',
      collapsedIcon: 'pi pi-folder',
      children: categories.map((c) => this.mapCategoryNode(c)),
    };
    return [allNode];
  }

  private mapCategoryNode(cat: CategoryTreeNode): TreeNode {
    return {
      label: cat.categoryName,
      data: cat.id,
      expandedIcon: 'pi pi-folder-open',
      collapsedIcon: 'pi pi-folder',
      children: cat.children?.map((c: CategoryTreeNode) => this.mapCategoryNode(c)) ?? [],
    };
  }

  onCategorySelect(event: { node: TreeNode }): void {
    this.store.setCategory(event.node.data ?? null);
  }

  onSearch(event: { keyword: string }): void {
    this.store.setFilters({ search: event.keyword });
  }

  onItemTypeChange(event: any): void {
    this.store.setFilters({ itemType: event.value });
  }

  onRowSelect(event: any): void {
    this.store.selectItem(event.data.id);
  }

  onRowUnselect(): void {
    this.store.selectItem(null);
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
      message: 'Bạn có chắc muốn xóa hàng này?',
      header: 'Xác nhận xóa',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.store.deleteItem(id).then(
          () => {
            this.selectedRow.set(null);
            this.messageService.add({
              severity: 'success',
              summary: 'Thành công',
              detail: 'Đã xóa hàng tồn kho thành công',
            });
          },
          (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Lỗi',
              detail: err?.error?.errors?.[0] ?? 'Không thể xóa hàng tồn kho',
            });
          }
        );
      },
    });
  }

  onPage(event: any): void {
    this.store.setPage(event.page + 1);
  }

  getItemTypeBadge(type: number): { label: string; class: string; style: any } {
    const baseStyle = { padding: '2px 8px', borderRadius: '4px', fontSize: '12px', fontWeight: '500', display: 'inline-block', lineHeight: '1' };
    switch (type) {
      case 1: return { label: 'HH', class: 'badge-hh', style: { ...baseStyle, background: 'var(--primary-light)', color: 'var(--debit)', border: '1px solid var(--debit)' } };
      case 2: return { label: 'NVL', class: 'badge-nvl', style: { ...baseStyle, background: 'color-mix(in srgb, var(--warning) 12%, white)', color: 'var(--warning)', border: '1px solid var(--warning)' } };
      case 3: return { label: 'TP', class: 'badge-tp', style: { ...baseStyle, background: 'color-mix(in srgb, var(--success) 12%, white)', color: 'var(--success)', border: '1px solid var(--success)' } };
      case 4: return { label: 'DV', class: 'badge-dv', style: { ...baseStyle, background: 'var(--surface-ground)', color: 'var(--text-secondary)', border: '1px solid var(--surface-border)' } };
      default: return { label: '?', class: '', style: baseStyle };
    }
  }
}
