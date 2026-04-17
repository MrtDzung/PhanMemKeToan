import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AccountFormComponent } from '../account-form/account-form.component';
import { AccountTreeStore } from '../../store/account-tree.store';
import { AccountCategoryKind, AccountDetailDto } from '../../../models/account.models';

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
            (onClick)="store.clearSelection()"
            aria-label="Đóng"
          />
        </div>
        <app-account-form
          [isEditMode]="false"
          (saved)="store.setFormMode('view')"
          (cancelled)="store.clearSelection()"
        />
      } @else if (store.selectedAccountDetail(); as detail) {
        <div class="panel-header">
          <div class="header-info">
            <span class="acct-num-large">{{ detail.accountNumber }}</span>
            <span class="acct-name-sub">{{ detail.accountName }}</span>
          </div>
          <p-button
            icon="pi pi-times"
            severity="secondary"
            [text]="true"
            size="small"
            (onClick)="store.clearSelection()"
            aria-label="Đóng panel"
          />
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
          <div class="detail-scroll">

            @if (detail.accountNameEnglish) {
              <div class="field-group">
                <div class="field-label">Tên tiếng Anh</div>
                <div class="field-value">{{ detail.accountNameEnglish }}</div>
              </div>
            }

            @if (detail.parentNumber) {
              <div class="field-group">
                <div class="field-label">Tài khoản tổng hợp</div>
                <div class="field-value monospace">{{ detail.parentNumber }} - {{ detail.parentName }}</div>
              </div>
            }

            <div class="field-group">
              <div class="field-label">Tính chất</div>
              <div class="field-value">
                @if (detail.accountCategoryKind === AccountCategoryKind.Mixed) {
                  <span class="cat-badge mixed">Lưỡng tính</span>
                } @else if (detail.accountCategoryKind === AccountCategoryKind.Debit) {
                  <span class="cat-badge debit">Dư Nợ</span>
                } @else {
                  <span class="cat-badge credit">Dư Có</span>
                }
              </div>
            </div>

            @if (hasAnyDetailFlag(detail)) {
              <div class="field-group">
                <div class="field-label">Chi tiết theo</div>
                <div class="detail-flags">
                  @if (detail.isPostableInForeignCurrency) {
                    <div class="flag-item"><i class="pi pi-check"></i> Có hạch toán ngoại tệ</div>
                  }
                  @if (detail.detailByAccountObject) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo đối tượng</div>
                  }
                  @if (detail.detailByBankAccount) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo TK ngân hàng</div>
                  }
                  @if (detail.detailByJob) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo công việc</div>
                  }
                  @if (detail.detailByProjectWork) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo công trình</div>
                  }
                  @if (detail.detailByOrder) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo đơn đặt hàng</div>
                  }
                  @if (detail.detailByContract) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo hợp đồng</div>
                  }
                  @if (detail.detailByExpenseItem) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo khoản mục CP</div>
                  }
                  @if (detail.detailByDepartment) {
                    <div class="flag-item"><i class="pi pi-check"></i> Chi tiết theo phòng ban</div>
                  }
                </div>
              </div>
            }

            <div class="field-group">
              <div class="field-label">Trạng thái</div>
              <div class="field-value">
                @if (detail.inactive) {
                  <span class="status-badge inactive">Ngừng dùng</span>
                } @else {
                  <span class="status-badge active">Đang dùng</span>
                }
              </div>
            </div>

          </div>

          <div class="panel-footer">
            <p-button
              icon="pi pi-pencil"
              label="Sửa"
              severity="secondary"
              size="small"
              [outlined]="true"
              (onClick)="store.setFormMode('edit')"
            />
            <p-button
              icon="pi pi-trash"
              severity="danger"
              size="small"
              [text]="true"
              (onClick)="onDelete(detail.accountId, detail.rowVersion)"
              [disabled]="detail.hasTransactions || detail.isParent"
              aria-label="Xóa tài khoản"
            />
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
      padding: 12px 16px 10px;
      border-bottom: 1px solid var(--surface-border);
      gap: 8px;
      flex-shrink: 0;
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

    .acct-num-large {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 18px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1.2;
    }

    .acct-name-sub {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .detail-scroll {
      flex: 1;
      overflow-y: auto;
      padding: 12px 16px;
      display: flex;
      flex-direction: column;
      gap: 0;
    }

    .field-group {
      padding: 8px 0;
      border-bottom: 1px solid var(--surface-border);
    }

    .field-group:last-child {
      border-bottom: none;
    }

    .field-label {
      font-size: 11px;
      color: var(--text-secondary);
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      margin-bottom: 4px;
    }

    .field-value {
      font-size: 13px;
      color: var(--text-primary);
    }

    .monospace {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 12px;
    }

    .cat-badge {
      font-size: 11px;
      padding: 2px 10px;
      border-radius: 3px;
      font-weight: 600;
    }

    .cat-badge.mixed {
      color: var(--text-secondary);
      border: 1px solid var(--surface-border);
      background: var(--surface-ground);
    }

    .cat-badge.debit {
      color: var(--debit);
      border: 1px solid var(--debit);
      background: rgba(21, 101, 192, 0.06);
    }

    .cat-badge.credit {
      color: var(--credit);
      border: 1px solid var(--credit);
      background: rgba(198, 40, 40, 0.06);
    }

    .detail-flags {
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-top: 2px;
    }

    .flag-item {
      font-size: 12px;
      color: var(--text-primary);
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .flag-item .pi-check {
      color: var(--success);
      font-size: 11px;
    }

    .status-badge {
      font-size: 12px;
      padding: 2px 10px;
      border-radius: 10px;
      font-weight: 500;
    }

    .status-badge.active {
      background: rgba(46, 125, 50, 0.1);
      color: var(--success);
      border: 1px solid rgba(46, 125, 50, 0.3);
    }

    .status-badge.inactive {
      color: var(--text-disabled);
      border: 1px solid var(--text-disabled);
    }

    .panel-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      border-top: 1px solid var(--surface-border);
      flex-shrink: 0;
      background: var(--surface-card);
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

  hasAnyDetailFlag(detail: AccountDetailDto): boolean {
    return (
      detail.isPostableInForeignCurrency ||
      detail.detailByAccountObject ||
      detail.detailByBankAccount ||
      detail.detailByJob ||
      detail.detailByProjectWork ||
      detail.detailByOrder ||
      detail.detailByContract ||
      detail.detailByExpenseItem ||
      detail.detailByDepartment
    );
  }

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

