import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { StepsModule } from 'primeng/steps';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { RadioButtonModule } from 'primeng/radiobutton';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AccountTreeStore } from '../../store/account-tree.store';
import { ImportCoaResultDto } from '../../../models/account.models';

interface CoaStandard {
  id: 'TT99' | 'TT133';
  name: string;
  legalReference: string;
  description: string;
  accountCount: number;
}

const COA_STANDARDS: CoaStandard[] = [
  {
    id: 'TT99',
    name: 'TT99 — Doanh nghiệp',
    legalReference: 'Thông tư 99/2016/TT-BTC',
    description: 'Dành cho doanh nghiệp vừa và lớn (~200 tài khoản)',
    accountCount: 200,
  },
  {
    id: 'TT133',
    name: 'TT133 — SME',
    legalReference: 'Thông tư 133/2016/TT-BTC',
    description: 'Dành cho doanh nghiệp vừa và nhỏ (~60 tài khoản)',
    accountCount: 60,
  },
];

@Component({
  selector: 'app-import-coa-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    StepsModule,
    ButtonModule,
    MessageModule,
    RadioButtonModule,
    ProgressSpinnerModule,
  ],
  template: `
<p-dialog
  [visible]="visible()"
  (visibleChange)="visible.set($event)"
  [closable]="isClosable()"
  [modal]="true"
  [draggable]="false"
  [resizable]="false"
  header="Nhập danh mục tài khoản chuẩn"
  [style]="{ width: '520px' }"
>
  <!-- Loading state -->
  @if (importing()) {
    <div class="import-loading">
      <p-progressSpinner styleClass="spinner-sm" />
      <span>Đang nhập danh mục tài khoản...</span>
    </div>
  }

  <!-- Stepper + steps content -->
  @if (!showResultPanel() && !importing()) {
    <p-steps [model]="stepItems" [activeIndex]="activeStep()" [readonly]="true" styleClass="mb-4" />

    <!-- Step 1: Choose standard -->
    @if (activeStep() === 0) {
      <div class="coa-standard-list">
        @for (std of standards; track std.id) {
          <div
            class="coa-card"
            [class.coa-card--selected]="selectedStdId() === std.id"
            (click)="selectedStdId.set(std.id)"
          >
            <p-radioButton
              [name]="'standard'"
              [value]="std.id"
              [ngModel]="selectedStdId()"
              (ngModelChange)="selectedStdId.set($event)"
            />
            <div class="coa-card-body">
              <div class="coa-card-title">{{ std.name }}</div>
              <div class="coa-card-ref">{{ std.legalReference }}</div>
              <div class="coa-card-desc">{{ std.description }}</div>
              <span class="coa-badge">~{{ std.accountCount }} tài khoản</span>
            </div>
          </div>
        }
      </div>
    }

    <!-- Step 2: Conflict resolution -->
    @if (activeStep() === 1) {
      <div class="coa-resolution">
        <div class="resolution-option" (click)="resolution.set('skip')">
          <p-radioButton name="resolution" value="skip" [ngModel]="resolution()" (ngModelChange)="resolution.set($event)" />
          <div>
            <div class="resolution-label">Bỏ qua</div>
            <div class="resolution-desc">Giữ nguyên tài khoản hiện có nếu trùng số hiệu</div>
          </div>
        </div>
        <div class="resolution-option" (click)="resolution.set('overwrite')">
          <p-radioButton name="resolution" value="overwrite" [ngModel]="resolution()" (ngModelChange)="resolution.set($event)" />
          <div>
            <div class="resolution-label resolution-label--warn">Ghi đè</div>
            <div class="resolution-desc">Thay thế tài khoản trùng số hiệu bằng dữ liệu chuẩn</div>
          </div>
        </div>

        @if (showOverwriteWarn()) {
          <p-message
            severity="warn"
            text="Hành động này sẽ ghi đè các tài khoản hiện có có cùng số hiệu. Kiểm tra kỹ trước khi nhập."
            styleClass="w-full mt-2"
          />
        }

        <div class="import-preview">
          Sẽ nhập <strong>~{{ selectedStandard()?.accountCount }}</strong> tài khoản theo
          <strong>{{ selectedStandard()?.name }}</strong>
        </div>

        @if (importError()) {
          <p-message severity="error" [text]="importError()!" styleClass="w-full mt-2" />
        }
      </div>
    }
  }

  <!-- Result panel -->
  @if (showResultPanel()) {
    <div class="import-result">
      @if (result(); as r) {
        <div class="result-stats">
          <div class="stat-card stat-card--imported">
            <span class="stat-number">{{ r.imported }}</span>
            <span class="stat-label">Đã nhập</span>
          </div>
          <div class="stat-card stat-card--skipped">
            <span class="stat-number">{{ r.skipped }}</span>
            <span class="stat-label">Bỏ qua</span>
          </div>
          <div class="stat-card stat-card--overwritten">
            <span class="stat-number">{{ r.overwritten }}</span>
            <span class="stat-label">Ghi đè</span>
          </div>
        </div>

        @if (r.errors.length > 0) {
          <div class="result-errors">
            <div class="result-errors-header">{{ r.errors.length }} lỗi phát sinh:</div>
            <ul>
              @for (err of visibleErrors(); track err) {
                <li>{{ err }}</li>
              }
            </ul>
            @if (hiddenErrorCount() > 0) {
              <button class="show-more-btn" (click)="showAllErrors.set(true)">
                Xem thêm {{ hiddenErrorCount() }} lỗi...
              </button>
            }
          </div>
        }
      }
    </div>
  }

  <ng-template pTemplate="footer">
    @if (!showResultPanel()) {
      @if (activeStep() === 0) {
        <p-button label="Hủy" severity="secondary" [disabled]="importing()" (onClick)="visible.set(false)" />
        <p-button label="Tiếp theo" icon="pi pi-arrow-right" iconPos="right"
          [disabled]="!canNext()" (onClick)="activeStep.set(1)" />
      } @else {
        <p-button label="Quay lại" icon="pi pi-arrow-left" severity="secondary"
          [disabled]="importing()" (onClick)="activeStep.set(0)" />
        <p-button label="Nhập" icon="pi pi-download"
          [loading]="importing()" [disabled]="!canImport()" (onClick)="executeImport()" />
      }
    } @else {
      <p-button label="Đóng" (onClick)="visible.set(false)" />
    }
  </ng-template>
</p-dialog>
  `,
  styles: [`
    .import-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 32px 0;
      color: var(--text-secondary);
    }

    .coa-standard-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 4px 0;
    }

    .coa-card {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 16px;
      border: 1px solid var(--surface-border);
      border-radius: 6px;
      cursor: pointer;
      transition: border-color 0.15s, background 0.15s;
    }

    .coa-card:hover { border-color: var(--primary); }

    .coa-card--selected {
      border-color: var(--primary);
      background: var(--primary-light);
    }

    .coa-card-body { flex: 1; }
    .coa-card-title { font-weight: 600; font-size: 13px; color: var(--text-primary); }
    .coa-card-ref   { font-size: 11px; color: var(--text-secondary); margin-top: 2px; }
    .coa-card-desc  { font-size: 12px; color: var(--text-secondary); margin-top: 4px; }

    .coa-badge {
      display: inline-block;
      margin-top: 6px;
      padding: 2px 8px;
      background: var(--primary-light);
      color: var(--primary);
      border-radius: 12px;
      font-size: 11px;
      font-weight: 500;
    }

    .coa-resolution {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 4px 0;
    }

    .resolution-option {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 10px 14px;
      border: 1px solid var(--surface-border);
      border-radius: 6px;
      cursor: pointer;
    }

    .resolution-label { font-weight: 600; font-size: 13px; }
    .resolution-label--warn { color: var(--warning); }
    .resolution-desc { font-size: 12px; color: var(--text-secondary); margin-top: 2px; }

    .import-preview {
      margin-top: 4px;
      padding: 8px 12px;
      background: var(--surface-ground);
      border-radius: 4px;
      font-size: 12px;
      color: var(--text-secondary);
    }

    .import-result { padding: 8px 0; }

    .result-stats {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
    }

    .stat-card {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 12px;
      border-radius: 6px;
      border: 1px solid var(--surface-border);
    }

    .stat-number { font-size: 24px; font-weight: 700; font-family: var(--font-mono, 'JetBrains Mono', monospace); font-variant-numeric: tabular-nums; }
    .stat-label  { font-size: 11px; color: var(--text-secondary); margin-top: 4px; }

    .stat-card--imported   { background: color-mix(in srgb, var(--positive) 8%, white); }
    .stat-card--imported .stat-number   { color: var(--positive); }
    .stat-card--skipped    { background: var(--surface-ground); }
    .stat-card--skipped .stat-number    { color: var(--text-secondary); }
    .stat-card--overwritten { background: color-mix(in srgb, var(--warning) 8%, white); }
    .stat-card--overwritten .stat-number { color: var(--warning); }

    .result-errors { margin-top: 8px; }
    .result-errors-header { font-size: 12px; font-weight: 600; color: var(--error); margin-bottom: 6px; }
    .result-errors ul { margin: 0; padding-left: 20px; }
    .result-errors li { font-size: 12px; color: var(--text-secondary); margin-bottom: 4px; }

    .show-more-btn {
      background: none;
      border: none;
      color: var(--primary);
      cursor: pointer;
      font-size: 12px;
      padding: 4px 0;
      text-decoration: underline;
    }
  `],
})
export class ImportCoaDialogComponent {
  private readonly store = inject(AccountTreeStore);

  // readonly data
  readonly standards = COA_STANDARDS;
  readonly stepItems = [{ label: 'Chọn chuẩn' }, { label: 'Tùy chọn nhập' }];

  // internal state signals
  visible = signal(false);
  activeStep = signal(0);
  selectedStdId = signal<'TT99' | 'TT133' | null>(null);
  resolution = signal<'skip' | 'overwrite'>('skip');
  importing = signal(false);
  result = signal<ImportCoaResultDto | null>(null);
  importError = signal<string | null>(null);
  showAllErrors = signal(false);

  // computed signals
  showOverwriteWarn = computed(() => this.resolution() === 'overwrite');
  canNext = computed(() => this.selectedStdId() !== null);
  canImport = computed(() => !this.importing() && this.selectedStdId() !== null);
  isClosable = computed(() => !this.importing());
  showResultPanel = computed(() => this.result() !== null);
  selectedStandard = computed(() => COA_STANDARDS.find(s => s.id === this.selectedStdId()) ?? null);
  visibleErrors = computed(() => {
    const errs = this.result()?.errors ?? [];
    return this.showAllErrors() ? errs : errs.slice(0, 5);
  });
  hiddenErrorCount = computed(() => {
    const total = this.result()?.errors?.length ?? 0;
    return this.showAllErrors() ? 0 : Math.max(0, total - 5);
  });

  open(): void {
    this.activeStep.set(0);
    this.selectedStdId.set(null);
    this.resolution.set('skip');
    this.importing.set(false);
    this.result.set(null);
    this.importError.set(null);
    this.showAllErrors.set(false);
    this.visible.set(true);
  }

  async executeImport(): Promise<void> {
    const std = this.selectedStdId();
    if (!std) return;
    this.importing.set(true);
    this.importError.set(null);
    try {
      const res = await this.store.importCoa(std, this.resolution());
      this.result.set(res);
    } catch (err: unknown) {
      const msg = (err as { error?: { detail?: string } })?.error?.detail ?? 'Nhập danh mục thất bại';
      this.importError.set(msg);
    } finally {
      this.importing.set(false);
    }
  }
}
