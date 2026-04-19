import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule],
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchBarComponent {
  // Inputs
  placeholder = input<string>('Nhập từ khóa tìm kiếm...');

  // Outputs
  search = output<{ keyword: string }>();

  // Internal state
  keyword = signal<string>('');

  // Computed
  hasKeyword = computed(() => this.keyword().length > 0);

  onSearch(): void {
    this.search.emit({ keyword: this.keyword() });
  }

  onClear(): void {
    this.keyword.set('');
    this.search.emit({ keyword: '' });
  }
}
