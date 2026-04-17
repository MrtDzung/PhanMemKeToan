import {
  Component,
  inject,
  input,
  output,
  OnInit,
  OnChanges,
  SimpleChanges,
  HostListener,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { AccountDetailDto, AccountCategoryKind, AccountObjectType, CreateAccountCommand, UpdateAccountCommand } from '../../../models/account.models';
import { AccountValidationService } from '../../services/account-validation.service';
import { AccountTreeStore } from '../../store/account-tree.store';

interface CategoryOption {
  label: string;
  value: AccountCategoryKind;
}

interface ObjectTypeOption {
  label: string;
  value: AccountObjectType;
}

@Component({
  selector: 'app-account-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputTextModule,
    SelectModule,
    CheckboxModule,
    ButtonModule,
    ConfirmDialogModule,
  ],
  template: `
    <p-confirmDialog />
    <form [formGroup]="form" (ngSubmit)="onSave()" class="account-form" novalidate>
      <div class="form-grid">

        <!-- Account Number -->
        <div class="form-field">
          <label for="accountNumber">
            Mã tài khoản <span class="required-mark">*</span>
          </label>
          <input
            id="accountNumber"
            pInputText
            formControlName="accountNumber"
            [class.ng-invalid]="isInvalid('accountNumber')"
            maxlength="20"
            placeholder="VD: 111"
            autocomplete="off"
          />
          @if (isInvalid('accountNumber')) {
            <small class="form-error">
              @if (form.get('accountNumber')?.errors?.['required']) { Mã tài khoản là bắt buộc. }
              @else if (form.get('accountNumber')?.errors?.['pattern']) { Chỉ chứa chữ và số, tối đa 20 ký tự. }
              @else if (form.get('accountNumber')?.errors?.['prefixError']) { Mã phải bắt đầu bằng mã tài khoản cha. }
            </small>
          }
        </div>

        <!-- Account Name -->
        <div class="form-field">
          <label for="accountName">
            Tên tài khoản <span class="required-mark">*</span>
          </label>
          <input
            id="accountName"
            pInputText
            formControlName="accountName"
            [class.ng-invalid]="isInvalid('accountName')"
            maxlength="128"
            placeholder="Tên tài khoản"
          />
          @if (isInvalid('accountName')) {
            <small class="form-error">Tên tài khoản là bắt buộc.</small>
          }
        </div>

        <!-- Account Name English -->
        <div class="form-field">
          <label for="accountNameEnglish">Tên tiếng Anh</label>
          <input
            id="accountNameEnglish"
            pInputText
            formControlName="accountNameEnglish"
            maxlength="128"
            placeholder="Account name in English"
          />
        </div>

        <!-- Category Kind -->
        <div class="form-field">
          <label for="accountCategoryKind">
            Loại tài khoản <span class="required-mark">*</span>
          </label>
          <p-select
            inputId="accountCategoryKind"
            formControlName="accountCategoryKind"
            [options]="categoryOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Chọn loại"
            [class.ng-invalid]="isInvalid('accountCategoryKind')"
          />
        </div>

        <!-- Foreign Currency -->
        <div class="form-field form-field--checkbox">
          <p-checkbox
            formControlName="isPostableInForeignCurrency"
            [binary]="true"
            inputId="isPostableInForeignCurrency"
          />
          <label for="isPostableInForeignCurrency">Theo dõi ngoại tệ</label>
        </div>

        <!-- Inactive (edit mode only) -->
        @if (isEditMode()) {
          <div class="form-field form-field--checkbox">
            <p-checkbox
              formControlName="inactive"
              [binary]="true"
              inputId="inactive"
            />
            <label for="inactive">Ngừng sử dụng</label>
          </div>
        }

        <!-- Detail By section -->
        <div class="form-field form-field--full">
          <label class="section-label">Hạch toán chi tiết theo</label>
          <div class="checkbox-group">
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByAccountObject" [binary]="true" inputId="detailByAccountObject" />
              <label for="detailByAccountObject">Đối tượng</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByBankAccount" [binary]="true" inputId="detailByBankAccount" />
              <label for="detailByBankAccount">Tài khoản ngân hàng</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByJob" [binary]="true" inputId="detailByJob" />
              <label for="detailByJob">Công trình</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByProjectWork" [binary]="true" inputId="detailByProjectWork" />
              <label for="detailByProjectWork">Công việc</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByOrder" [binary]="true" inputId="detailByOrder" />
              <label for="detailByOrder">Đơn hàng</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByContract" [binary]="true" inputId="detailByContract" />
              <label for="detailByContract">Hợp đồng</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByExpenseItem" [binary]="true" inputId="detailByExpenseItem" />
              <label for="detailByExpenseItem">Khoản mục chi phí</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByDepartment" [binary]="true" inputId="detailByDepartment" />
              <label for="detailByDepartment">Bộ phận</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByListItem" [binary]="true" inputId="detailByListItem" />
              <label for="detailByListItem">Danh sách</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox formControlName="detailByPuContract" [binary]="true" inputId="detailByPuContract" />
              <label for="detailByPuContract">Hợp đồng mua</label>
            </div>
          </div>
        </div>

        <!-- Account Object Type (conditional) -->
        @if (form.get('detailByAccountObject')?.value) {
          <div class="form-field">
            <label for="accountObjectType">
              Loại đối tượng <span class="required-mark">*</span>
            </label>
            <p-select
              inputId="accountObjectType"
              formControlName="accountObjectType"
              [options]="objectTypeOptions"
              optionLabel="label"
              optionValue="value"
              placeholder="Chọn loại đối tượng"
            />
          </div>
        }
      </div>

      <div class="form-actions">
        <p-button
          type="submit"
          label="Lưu"
          icon="pi pi-save"
          severity="primary"
          size="small"
          [loading]="store.saving()"
          [disabled]="form.invalid || store.saving()"
        />
        <p-button
          type="button"
          label="Hủy"
          icon="pi pi-times"
          severity="secondary"
          size="small"
          (onClick)="onCancel()"
        />
      </div>
    </form>
  `,
  styles: [`
    .account-form {
      padding: 16px;
      font-size: 13px;
    }

    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .form-field {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .form-field--full {
      grid-column: 1 / -1;
    }

    .form-field--checkbox {
      flex-direction: row;
      align-items: center;
      gap: 8px;
    }

    label {
      font-size: 12px;
      font-weight: 500;
      color: var(--text-secondary);
    }

    .required-mark {
      color: var(--error);
      margin-left: 2px;
    }

    .section-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .checkbox-group {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 8px;
    }

    .checkbox-item {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
    }

    .form-error {
      color: var(--error);
      font-size: 11px;
    }

    :host ::ng-deep input.ng-invalid.ng-touched,
    :host ::ng-deep .p-select.ng-invalid.ng-touched {
      border-color: var(--error) !important;
    }

    .form-actions {
      display: flex;
      gap: 8px;
      padding-top: 16px;
      border-top: 1px solid var(--surface-border);
      margin-top: 16px;
    }
  `],
})
export class AccountFormComponent implements OnInit, OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly validationService = inject(AccountValidationService);
  readonly store = inject(AccountTreeStore);

  account = input<AccountDetailDto | null>(null);
  isEditMode = input<boolean>(false);
  parentAccountNumber = input<string | null>(null);

  saved = output<void>();
  cancelled = output<void>();

  readonly categoryOptions: CategoryOption[] = [
    { label: 'Tài khoản Nợ', value: AccountCategoryKind.Debit },
    { label: 'Tài khoản Có', value: AccountCategoryKind.Credit },
  ];

  readonly objectTypeOptions: ObjectTypeOption[] = [
    { label: 'Không xác định', value: AccountObjectType.None },
    { label: 'Nhà cung cấp', value: AccountObjectType.Supplier },
    { label: 'Khách hàng', value: AccountObjectType.Customer },
    { label: 'Nhân viên', value: AccountObjectType.Employee },
  ];

  form = this.fb.group({
    accountNumber: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]{1,20}$/)]],
    accountName: ['', [Validators.required, Validators.maxLength(128)]],
    accountNameEnglish: [''],
    accountCategoryKind: [AccountCategoryKind.Debit, Validators.required],
    isPostableInForeignCurrency: [false],
    inactive: [false],
    detailByAccountObject: [false],
    detailByBankAccount: [false],
    detailByJob: [false],
    detailByProjectWork: [false],
    detailByOrder: [false],
    detailByContract: [false],
    detailByExpenseItem: [false],
    detailByDepartment: [false],
    detailByListItem: [false],
    detailByPuContract: [false],
    accountObjectType: [AccountObjectType.None],
    rowVersion: [0],
  });

  ngOnInit(): void {
    this.patchForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['account']) {
      this.patchForm();
    }
  }

  private patchForm(): void {
    const acc = this.account();
    if (acc) {
      this.form.patchValue({
        accountNumber: acc.accountNumber,
        accountName: acc.accountName,
        accountNameEnglish: acc.accountNameEnglish ?? '',
        accountCategoryKind: acc.accountCategoryKind,
        isPostableInForeignCurrency: acc.isPostableInForeignCurrency,
        inactive: acc.inactive,
        detailByAccountObject: acc.detailByAccountObject,
        detailByBankAccount: acc.detailByBankAccount,
        detailByJob: acc.detailByJob,
        detailByProjectWork: acc.detailByProjectWork,
        detailByOrder: acc.detailByOrder,
        detailByContract: acc.detailByContract,
        detailByExpenseItem: acc.detailByExpenseItem,
        detailByDepartment: acc.detailByDepartment,
        detailByListItem: acc.detailByListItem,
        detailByPuContract: acc.detailByPuContract,
        accountObjectType: acc.accountObjectType,
        rowVersion: acc.rowVersion,
      });

      if (acc.hasTransactions) {
        this.form.get('accountNumber')?.disable();
      } else {
        this.form.get('accountNumber')?.enable();
      }
    } else {
      this.form.reset({
        accountCategoryKind: AccountCategoryKind.Debit,
        accountObjectType: AccountObjectType.None,
        rowVersion: 0,
      });
      this.form.get('accountNumber')?.enable();
    }
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched));
  }

  @HostListener('keydown.control.s', ['$event'])
  onCtrlS(event: Event): void {
    event.preventDefault();
    if (this.form.valid) this.onSave();
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    this.onCancel();
  }

  onSave(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const val = this.form.getRawValue();
    const parentNum = this.parentAccountNumber();
    if (parentNum && !this.validationService.validatePrefix(val.accountNumber!, parentNum)) {
      this.form.get('accountNumber')?.setErrors({ prefixError: true });
      return;
    }

    const acc = this.account();
    if (acc) {
      const cmd: UpdateAccountCommand = {
        accountNumber: val.accountNumber!,
        accountName: val.accountName!,
        accountNameEnglish: val.accountNameEnglish || undefined,
        accountCategoryKind: val.accountCategoryKind!,
        isPostableInForeignCurrency: val.isPostableInForeignCurrency!,
        inactive: val.inactive!,
        accountObjectType: val.accountObjectType!,
        detailByAccountObject: val.detailByAccountObject!,
        detailByBankAccount: val.detailByBankAccount!,
        detailByJob: val.detailByJob!,
        detailByProjectWork: val.detailByProjectWork!,
        detailByOrder: val.detailByOrder!,
        detailByContract: val.detailByContract!,
        detailByExpenseItem: val.detailByExpenseItem!,
        detailByDepartment: val.detailByDepartment!,
        detailByListItem: val.detailByListItem!,
        detailByPuContract: val.detailByPuContract!,
        rowVersion: val.rowVersion!,
      };
      this.store.updateAccount(acc.accountId, cmd).then(() => this.saved.emit());
    } else {
      const cmd: CreateAccountCommand = {
        accountNumber: val.accountNumber!,
        accountName: val.accountName!,
        accountNameEnglish: val.accountNameEnglish || undefined,
        accountCategoryKind: val.accountCategoryKind!,
        isPostableInForeignCurrency: val.isPostableInForeignCurrency!,
        accountObjectType: val.accountObjectType!,
        detailByAccountObject: val.detailByAccountObject!,
        detailByBankAccount: val.detailByBankAccount!,
        detailByJob: val.detailByJob!,
        detailByProjectWork: val.detailByProjectWork!,
        detailByOrder: val.detailByOrder!,
        detailByContract: val.detailByContract!,
        detailByExpenseItem: val.detailByExpenseItem!,
        detailByDepartment: val.detailByDepartment!,
        detailByListItem: val.detailByListItem!,
        detailByPuContract: val.detailByPuContract!,
      };
      this.store.createAccount(cmd).then(() => this.saved.emit());
    }
  }

  onCancel(): void {
    if (this.form.dirty) {
      this.confirmationService.confirm({
        message: 'Bạn có thay đổi chưa lưu. Bạn có muốn hủy không?',
        header: 'Xác nhận hủy',
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: 'Hủy thay đổi',
        rejectLabel: 'Tiếp tục chỉnh sửa',
        accept: () => {
          this.form.reset();
          this.cancelled.emit();
        },
      });
    } else {
      this.cancelled.emit();
    }
  }
}
