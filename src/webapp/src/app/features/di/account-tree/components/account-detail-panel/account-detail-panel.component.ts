import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AccountFormComponent } from '../account-form/account-form.component';
import { AccountTreeStore } from '../../store/account-tree.store';
import { AccountCategoryKind } from '../../../models/account.models';

@Component({
  selector: 'app-account-detail-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [CommonModule, ButtonModule, ConfirmDialogModule, AccountFormComponent],
  template: `
    <p-confirmDialog />
    <div class="detail-panel">
      @if (store.formMode() === 'create') {
        <div class="panel-header">
          <span class="panel-title">Thêm tài khoản mới</span>
          <p-button
            icon="pi pi-times"
            severity="secondary"
            [text]="true"
            size="small"
            (onClick)="store.setFormMode('view'); store.selectAccount(store.selectedAccountId() ?? '')"
            aria-label="Đóng"
          />
        </div>
        <app-account-form
          [isEditMode]="false"
          (saved)="store.setFormMode('view')"
          (cancelled)="store.setFormMode('view')"
        />
      } @else if (store.selectedAccountDetail(); as detail) {
        <div class="panel-header">
          <div class="header-info">
            <span class="account-number-header">{{ detail.accountNumber }}</span>
            <span class="account-name-header">{{ detail.accountName }}</span>
          </div>
          <div class="header-actions">
            @if (store.formMode() === 'view') {
              <p-button
                icon="pi pi-pencil"
                label="Chỉnh sửa"
                severity="secondary"
                size="small"
                (onClick)="store.setFormMode('edit')"
              />
              <p-button
                icon="pi pi-trash"
                label="Xóa"
                severity="danger"
                size="small"
                [outlined]="true"
                (onClick)="onDelete(detail.accountId, detail.rowVersion)"
                [disabled]="detail.hasTransactions || detail.isParent"
              />
            }
          </div>
        </div>

        <div class="status-badges">
          @if (detail.inactive) {
            <span class="badge badge--inactive">Ngừng dùng</span>
          } @else {
            <span class="badge badge--active">Đang dùng</span>
          }
          <span class="badge badge--category" [class.debit]="detail.accountCategoryKind === AccountCategoryKind.Debit" [class.credit]="detail.accountCategoryKind === AccountCategoryKind.Credit">
            {{ detail.accountCategoryKind === AccountCategoryKind.Debit ? 'Tài khoản Nợ' : 'Tài khoản Có' }}
          </span>
          @if (detail.isParent) {
            <span class="badge badge--parent">Tài khoản tổng hợp</span>
          }
          @if (detail.hasTransactions) {
            <span class="badge badge--has-tx">Có phát sinh</span>
          }
        </div>

        @if (store.formMode() === 'edit') {
          <app-account-form
            [account]="detail"
            [isEditMode]="true"
            [parentAccountNumber]="detail.parentNumber"
            (saved)="store.setFormMode('view')"
            (cancelled)="store.setFormMode('view')"
          />
        } @else {
          <div class="detail-view">
            <div class="detail-row">
              <span class="detail-label">Mã tài khoản</span>
              <span class="detail-value monospace">{{ detail.accountNumber }}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Tên tài khoản</span>
              <span class="detail-value">{{ detail.accountName }}</span>
            </div>
            @if (detail.accountNameEnglish) {
              <div class="detail-row">
                <span class="detail-label">Tên tiếng Anh</span>
                <span class="detail-value">{{ detail.accountNameEnglish }}</span>
              </div>
            }
            @if (detail.parentNumber) {
              <div class="detail-row">
                <span class="detail-label">Tài khoản cha</span>
                <span class="detail-value monospace">{{ detail.parentNumber }} — {{ detail.parentName }}</span>
              </div>
            }
            @if (detail.isPostableInForeignCurrency) {
              <div class="detail-row">
                <span class="detail-label">Ngoại tệ</span>
                <span class="detail-value">Có theo dõi</span>
              </div>
            }
            <div class="detail-row">
              <span class="detail-label">Cập nhật</span>
              <span class="detail-value text-secondary">{{ detail.modifiedAt ?? detail.createdAt | date:'dd/MM/yyyy HH:mm' }}</span>
            </div>
          </div>
        }
      } @else {
        <div class="empty-state">
          <i class="pi pi-info-circle" style="font-size: 2rem; color: var(--text-disabled)"></i>
          <p>Chọn một tài khoản để xem chi tiết</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .detail-panel {
      height: 100%;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: var(--surface-card);
    }

    .panel-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      padding: 12px 16px;
      border-bottom: 1px solid var(--surface-border);
      gap: 8px;
    }

    .panel-title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .header-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .account-number-header {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 14px;
      font-weight: 700;
      color: var(--primary);
    }

    .account-name-header {
      font-size: 13px;
      color: var(--text-primary);
    }

    .header-actions {
      display: flex;
      gap: 6px;
    }

    .status-badges {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      padding: 8px 16px;
      border-bottom: 1px solid var(--surface-border);
    }

    .badge {
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 3px;
      font-weight: 500;
    }

    .badge--active {
      background: var(--success-bg);
      color: var(--success);
    }

    .badge--inactive {
      color: var(--text-disabled);
      border: 1px solid var(--text-disabled);
    }

    .badge--category.debit {
      color: var(--debit);
      border: 1px solid var(--debit);
      background: var(--debit-bg);
    }

    .badge--category.credit {
      color: var(--credit);
      border: 1px solid var(--credit);
      background: var(--credit-bg);
    }

    .badge--parent {
      color: var(--info);
      border: 1px solid var(--info);
      background: var(--info-bg);
    }

    .badge--has-tx {
      color: var(--warning);
      border: 1px solid var(--warning);
      background: var(--warning-bg);
    }

    .detail-view {
      flex: 1;
      overflow-y: auto;
      padding: 12px 16px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .detail-row {
      display: grid;
      grid-template-columns: 140px 1fr;
      gap: 8px;
      font-size: 13px;
      padding: 4px 0;
      border-bottom: 1px solid var(--surface-border);
    }

    .detail-label {
      color: var(--text-secondary);
      font-size: 12px;
    }

    .detail-value {
      color: var(--text-primary);
    }

    .monospace {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 12px;
    }

    .text-secondary {
      color: var(--text-secondary);
    }

    .empty-state {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      color: var(--text-disabled);
      font-size: 13px;
    }
  `],
})
export class AccountDetailPanelComponent {
  readonly store = inject(AccountTreeStore);
  readonly AccountCategoryKind = AccountCategoryKind;
  private readonly confirmationService = inject(ConfirmationService);

  onDelete(accountId: string, rowVersion: number): void {
    this.confirmationService.confirm({
      message: 'Bạn có chắc chắn muốn xóa tài khoản này?',
      header: 'Xác nhận xóa',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Xóa',
      rejectLabel: 'Hủy',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.store.deleteAccount(accountId, rowVersion);
      },
    });
  }
}
