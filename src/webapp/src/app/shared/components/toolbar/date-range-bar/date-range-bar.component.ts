import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';

interface PresetGroup {
  label: string;
  items: { label: string; value: string }[];
}

@Component({
  selector: 'app-date-range-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, DatePickerModule, SelectModule],
  templateUrl: './date-range-bar.component.html',
  styleUrl: './date-range-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DateRangeBarComponent implements OnInit {
  // Inputs
  defaultPreset = input<string>('Đầu tháng đến hiện tại');

  // Outputs
  rangeChange = output<{ fromDate: Date; toDate: Date; preset: string }>();

  // Internal state
  selectedPreset = signal<string>('Đầu tháng đến hiện tại');
  fromDate = signal<Date | null>(null);
  toDate = signal<Date | null>(null);

  // Preset options grouped
  readonly presetGroups: PresetGroup[] = [
    {
      label: 'Ngày',
      items: [
        { label: 'Hôm nay', value: 'Hôm nay' },
        { label: 'Hôm qua', value: 'Hôm qua' },
      ],
    },
    {
      label: 'Tuần',
      items: [
        { label: 'Tuần này', value: 'Tuần này' },
        { label: 'Tuần trước', value: 'Tuần trước' },
      ],
    },
    {
      label: 'Tháng',
      items: [
        { label: 'Tháng này', value: 'Tháng này' },
        { label: 'Tháng trước', value: 'Tháng trước' },
        { label: 'Đầu tháng đến hiện tại', value: 'Đầu tháng đến hiện tại' },
      ],
    },
    {
      label: 'Quý',
      items: [
        { label: 'Đầu quý đến hiện tại', value: 'Đầu quý đến hiện tại' },
        { label: 'Quý này', value: 'Quý này' },
        { label: 'Quý trước', value: 'Quý trước' },
      ],
    },
    {
      label: 'Năm',
      items: [
        { label: 'Đầu năm đến hiện tại', value: 'Đầu năm đến hiện tại' },
        { label: 'Năm nay', value: 'Năm nay' },
        { label: 'Năm trước', value: 'Năm trước' },
        { label: '6 tháng đầu năm', value: '6 tháng đầu năm' },
        { label: '6 tháng cuối năm', value: '6 tháng cuối năm' },
      ],
    },
    {
      label: 'Tháng cụ thể',
      items: Array.from({ length: 12 }, (_, i) => ({
        label: `Tháng ${i + 1}`,
        value: `Tháng ${i + 1}`,
      })),
    },
    {
      label: 'Khác',
      items: [{ label: 'Tùy chỉnh', value: 'Tùy chỉnh' }],
    },
  ];

  // Flat list for p-select (with optionGroupLabel)
  readonly presetOptions = this.presetGroups.flatMap((g) =>
    g.items.map((item) => ({ ...item, group: g.label }))
  );

  ngOnInit(): void {
    const preset = this.defaultPreset();
    this.selectedPreset.set(preset);
    const dates = this.calculateDates(preset);
    this.fromDate.set(dates.from);
    this.toDate.set(dates.to);
  }

  onPresetChange(value: string): void {
    if (value === 'Tùy chỉnh') {
      this.selectedPreset.set('Tùy chỉnh');
      return;
    }
    this.selectedPreset.set(value);
    const dates = this.calculateDates(value);
    this.fromDate.set(dates.from);
    this.toDate.set(dates.to);
  }

  onDateChange(): void {
    this.selectedPreset.set('Tùy chỉnh');
  }

  onFetch(): void {
    const from = this.fromDate();
    const to = this.toDate();
    if (!from || !to) return;
    this.rangeChange.emit({
      fromDate: from,
      toDate: to,
      preset: this.selectedPreset(),
    });
  }

  private calculateDates(preset: string): { from: Date; to: Date } {
    const today = new Date();
    const y = today.getFullYear();
    const m = today.getMonth(); // 0-based
    const d = today.getDate();

    switch (preset) {
      case 'Hôm nay':
        return { from: new Date(y, m, d), to: new Date(y, m, d) };

      case 'Hôm qua': {
        const yesterday = new Date(y, m, d - 1);
        return { from: yesterday, to: yesterday };
      }

      case 'Tuần này': {
        const day = today.getDay(); // 0=Sun
        const monday = new Date(y, m, d - ((day + 6) % 7));
        const sunday = new Date(y, m, d + (7 - ((day + 6) % 7)) % 7);
        return { from: monday, to: sunday };
      }

      case 'Tuần trước': {
        const day = today.getDay();
        const lastMonday = new Date(y, m, d - ((day + 6) % 7) - 7);
        const lastSunday = new Date(y, m, d - ((day + 6) % 7) - 1);
        return { from: lastMonday, to: lastSunday };
      }

      case 'Tháng này':
        return { from: new Date(y, m, 1), to: new Date(y, m + 1, 0) };

      case 'Tháng trước':
        return { from: new Date(y, m - 1, 1), to: new Date(y, m, 0) };

      case 'Đầu tháng đến hiện tại':
        return { from: new Date(y, m, 1), to: new Date(y, m, d) };

      case 'Đầu quý đến hiện tại': {
        const q = Math.floor(m / 3);
        return { from: new Date(y, q * 3, 1), to: new Date(y, m, d) };
      }

      case 'Quý này': {
        const q = Math.floor(m / 3);
        return { from: new Date(y, q * 3, 1), to: new Date(y, q * 3 + 3, 0) };
      }

      case 'Quý trước': {
        const q = Math.floor(m / 3);
        const prevQ = q === 0 ? 3 : q - 1;
        const prevY = q === 0 ? y - 1 : y;
        return {
          from: new Date(prevY, prevQ * 3, 1),
          to: new Date(prevY, prevQ * 3 + 3, 0),
        };
      }

      case 'Đầu năm đến hiện tại':
        return { from: new Date(y, 0, 1), to: new Date(y, m, d) };

      case 'Năm nay':
        return { from: new Date(y, 0, 1), to: new Date(y, 11, 31) };

      case 'Năm trước':
        return { from: new Date(y - 1, 0, 1), to: new Date(y - 1, 11, 31) };

      case '6 tháng đầu năm':
        return { from: new Date(y, 0, 1), to: new Date(y, 5, 30) };

      case '6 tháng cuối năm':
        return { from: new Date(y, 6, 1), to: new Date(y, 11, 31) };

      default: {
        // Handle "Tháng X" specific months
        const monthMatch = preset.match(/^Tháng (\d{1,2})$/);
        if (monthMatch) {
          const mi = parseInt(monthMatch[1], 10) - 1; // 0-based
          return { from: new Date(y, mi, 1), to: new Date(y, mi + 1, 0) };
        }
        // Fallback: current month
        return { from: new Date(y, m, 1), to: new Date(y, m, d) };
      }
    }
  }
}
