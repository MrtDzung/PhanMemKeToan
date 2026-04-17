import { Component, inject, output, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { StepperModule } from 'primeng/stepper';
import { RadioButtonModule } from 'primeng/radiobutton';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AccountTreeStore } from '../../store/account-tree.store';
import { ImportCoaResultDto } from '../../../models/account.models';

type CoaStandard = 'TT99' | 'TT133';
type ConflictResolution = 'skip' | 'overwrite';

@Component({
  selector: 'app-import-coa-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    StepperModule,
    RadioButtonModule,
    ProgressSpinnerModule,
  ],
  template: `
    <p-dialog
      [(visible)]="visible"
      [modal]="true"
      header="Nhập danh mục tài khoản chuẩn"
      [style]="{ width: '560px' }"
      [closable]="!store.saving()"
      (onHide)="onClose()"
    >
      @if (step() === 1) {
        <div class="step-content">
          <h4 class="step-title">Bước 1: Chọn hệ thống tài khoản</h4>
          <div class="radio-group">
            <div class="radio-item">
              <p-radioButton
                name="standard"
                value="TT99"
                [(ngModel)]="selectedStandard"
                inputId="tt99"
              />
              <label for="tt99">
                <strong>TT99/2016</strong>
                <span>Thông tư 99 — Doanh nghiệp lớn (330+ tài khoản)</span>
              </label>
            </div>
            <div class="radio-item">
              <p-radioButton
                name="standard"
                value="TT133"
                [(ngModel)]="selectedStandard"
                inputId="tt133"
              />
              <label for="tt133">
                <strong>TT133/2016</strong>
                <span>Thông tư 133 — Doanh nghiệp vừa và nhỏ (200+ tài khoản)</span>
              </label>
            </div>
          </div>
        </div>
        <ng-template pTemplate="footer">
          <p-button label="Hủy" severity="secondary" (onClick)="onClose()" />
          <p-button label="Tiếp theo →" severity="primary" [disabled]="!selectedStandard" (onClick)="step.set(2)" />
        </ng-template>
      }

      @if (step() === 2) {
        <div class="step-content">
          <h4 class="step-title">Bước 2: Xử lý xung đột</h4>
          <p class="step-desc">Nếu tài khoản đã tồn tại, hệ thống sẽ:</p>
          <div class="radio-group">
            <div class="radio-item">
              <p-radioButton
                name="conflict"
                value="skip"
                [(ngModel)]="selectedConflict"
                inputId="skip"
              />
              <label for="skip">
                <strong>Bỏ qua (Skip)</strong>
                <span>Giữ nguyên tài khoản hiện tại, không thay đổi</span>
              </label>
            </div>
            <div class="radio-item">
              <p-radioButton
                name="conflict"
                value="overwrite"
                [(ngModel)]="selectedConflict"
                inputId="overwrite"
              />
              <label for="overwrite">
                <strong>Ghi đè (Overwrite)</strong>
                <span>Cập nhật tên tài khoản theo danh mục chuẩn</span>
              </label>
            </div>
          </div>
        </div>
        <ng-template pTemplate="footer">
          <p-button label="← Quay lại" severity="secondary" (onClick)="step.set(1)" />
          <p-button label="Xem trước →" severity="primary" [disabled]="!selectedConflict" (onClick)="onPreview()" [loading]="store.saving()" />
        </ng-template>
      }

      @if (step() === 3) {
        <div class="step-content">
          <h4 class="step-title">Bước 3: Xem trước kết quả</h4>
          @if (previewResult()) {
            <div class="result-summary">
              <div class="result-item">
                <span class="result-count success">{{ previewResult()?.imported }}</span>
                <span class="result-label">Sẽ được tạo mới</span>
              </div>
              <div class="result-item">
                <span class="result-count warning">{{ previewResult()?.skipped }}</span>
                <span class="result-label">Sẽ bỏ qua</span>
              </div>
              <div class="result-item">
                <span class="result-count info">{{ previewResult()?.overwritten }}</span>
                <span class="result-label">Sẽ ghi đè</span>
              </div>
            </div>
            @if (previewResult()!.errors.length > 0) {
              <div class="errors-section">
                <strong>Lỗi phát hiện:</strong>
                <ul>
                  @for (err of previewResult()!.errors; track err) {
                    <li class="error-item">{{ err }}</li>
                  }
                </ul>
              </div>
            }
          }
        </div>
        <ng-template pTemplate="footer">
          <p-button label="← Quay lại" severity="secondary" (onClick)="step.set(2)" />
          <p-button label="Xác nhận nhập →" severity="success" (onClick)="onConfirmImport()" [loading]="store.saving()" [disabled]="previewResult()!.errors.length > 0" />
        </ng-template>
      }

      @if (step() === 4) {
        <div class="step-content">
          @if (store.saving()) {
            <div class="loading-state">
              <p-progressSpinner strokeWidth="4" [style]="{ width: '48px', height: '48px' }" />
              <p>Đang nhập danh mục tài khoản...</p>
            </div>
          } @else if (finalResult()) {
            <h4 class="step-title">Nhập hoàn tất!</h4>
            <div class="result-summary">
              <div class="result-item">
                <span class="result-count success">{{ finalResult()?.imported }}</span>
                <span class="result-label">Đã tạo mới</span>
              </div>
              <div class="result-item">
                <span class="result-count warning">{{ finalResult()?.skipped }}</span>
                <span class="result-label">Đã bỏ qua</span>
              </div>
              <div class="result-item">
                <span class="result-count info">{{ finalResult()?.overwritten }}</span>
                <span class="result-label">Đã ghi đè</span>
              </div>
            </div>
          }
        </div>
        <ng-template pTemplate="footer">
          <p-button label="Đóng" severity="primary" (onClick)="onClose()" [disabled]="store.saving()" />
        </ng-template>
      }
    </p-dialog>
  `,
  styles: [`
    .step-content {
      padding: 8px 0;
      min-height: 160px;
    }

    .step-title {
      margin: 0 0 12px;
      font-size: 14px;
      color: var(--text-primary);
    }

    .step-desc {
      font-size: 13px;
      color: var(--text-secondary);
      margin-bottom: 12px;
    }

    .radio-group {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .radio-item {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 12px;
      border: 1px solid var(--surface-border);
      border-radius: 6px;
      cursor: pointer;
    }

    .radio-item label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      cursor: pointer;
    }

    .radio-item label strong {
      font-size: 13px;
      color: var(--text-primary);
    }

    .radio-item label span {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .result-summary {
      display: flex;
      gap: 24px;
      justify-content: center;
      padding: 24px 0;
    }

    .result-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .result-count {
      font-size: 32px;
      font-weight: 700;
      font-family: 'JetBrains Mono', monospace;
    }

    .result-count.success { color: var(--success); }
    .result-count.warning { color: var(--warning); }
    .result-count.info { color: var(--info); }

    .result-label {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .errors-section {
      margin-top: 12px;
      padding: 8px 12px;
      background: var(--error-bg);
      border-left: 3px solid var(--error);
      border-radius: 4px;
      font-size: 12px;
    }

    .error-item {
      color: var(--error);
      margin: 2px 0;
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 32px;
      color: var(--text-secondary);
      font-size: 13px;
    }
  `],
})
export class ImportCoaDialogComponent {
  readonly store = inject(AccountTreeStore);

  closed = output<void>();

  visible = false;
  selectedStandard: CoaStandard = 'TT99';
  selectedConflict: ConflictResolution = 'skip';

  step = signal(1);
  previewResult = signal<ImportCoaResultDto | null>(null);
  finalResult = signal<ImportCoaResultDto | null>(null);

  open(): void {
    this.step.set(1);
    this.selectedStandard = 'TT99';
    this.selectedConflict = 'skip';
    this.previewResult.set(null);
    this.finalResult.set(null);
    this.visible = true;
  }

  onClose(): void {
    this.visible = false;
    this.closed.emit();
  }

  async onPreview(): Promise<void> {
    try {
      const result = await this.store.importCoa(this.selectedStandard, this.selectedConflict);
      this.previewResult.set(result as ImportCoaResultDto);
      this.step.set(3);
    } catch {
      // error handled in store
    }
  }

  async onConfirmImport(): Promise<void> {
    this.step.set(4);
    try {
      const result = await this.store.importCoa(this.selectedStandard, this.selectedConflict);
      this.finalResult.set(result as ImportCoaResultDto);
    } catch {
      // error handled in store
    }
  }
}
