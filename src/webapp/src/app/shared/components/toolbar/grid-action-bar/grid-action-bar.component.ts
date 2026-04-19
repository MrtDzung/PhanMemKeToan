import { ChangeDetectionStrategy, Component, HostListener, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-grid-action-bar',
  standalone: true,
  imports: [CommonModule, ButtonModule, TooltipModule, MenuModule],
  templateUrl: './grid-action-bar.component.html',
  styleUrl: './grid-action-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GridActionBarComponent {
  // Inputs
  selectedCount = input<number>(0);
  showDuplicate = input<boolean>(false);
  showImport = input<boolean>(false);
  showExport = input<boolean>(false);
  showPrint = input<boolean>(false);

  // Outputs
  addClick = output<void>();
  editClick = output<void>();
  deleteClick = output<void>();
  duplicateClick = output<void>();
  importClick = output<void>();
  exportClick = output<void>();
  printClick = output<void>();

  // Computed
  isEditEnabled = computed(() => this.selectedCount() > 0);

  hasOptional = computed(
    () =>
      this.showImport() ||
      this.showExport() ||
      this.showPrint() ||
      this.showDuplicate()
  );

  // Responsive: true when viewport < 960px (icon-only mode — show tooltips)
  isIconOnly = signal(typeof window !== 'undefined' ? window.innerWidth < 960 : false);

  @HostListener('window:resize')
  onResize(): void {
    this.isIconOnly.set(window.innerWidth < 960);
  }

  // Overflow menu items for mobile
  overflowItems = computed<MenuItem[]>(() => {
    const items: MenuItem[] = [];
    if (this.showDuplicate()) {
      items.push({
        label: 'Nhân bản',
        icon: 'pi pi-copy',
        disabled: !this.isEditEnabled(),
        command: () => this.duplicateClick.emit(),
      });
    }
    if (this.showImport()) {
      items.push({
        label: 'Nhập Excel',
        icon: 'pi pi-file-excel',
        command: () => this.importClick.emit(),
      });
    }
    if (this.showExport()) {
      items.push({
        label: 'Xuất Excel',
        icon: 'pi pi-file-export',
        command: () => this.exportClick.emit(),
      });
    }
    if (this.showPrint()) {
      items.push({
        label: 'In',
        icon: 'pi pi-print',
        command: () => this.printClick.emit(),
      });
    }
    return items;
  });
}
