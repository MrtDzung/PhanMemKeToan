import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostListener,
  Injector,
  OnInit,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { DatePickerModule } from 'primeng/datepicker';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';
import { AccountObjectsStore } from '../../store/account-objects.store';
import { AccountObjectsApiService } from '../../services/account-objects-api.service';
import { LookupsApiService } from '../../../setup/services/lookups-api.service';
import { CurrencyDto, DepartmentDto } from '../../../models/master-data.models';
import {
  CreateAccountObjectDto,
  UpdateAccountObjectDto,
  BankAccountInputDto,
  OpeningBalanceInputDto,
  EmployeeProfileInputDto,
} from '../../models/account-object-request.models';

@Component({
  selector: 'app-account-object-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    ToggleSwitchModule,
    SelectModule,
    CheckboxModule,
    DatePickerModule,
    TabsModule,
    TableModule,
    ToastModule,
    TranslateModule,
  ],
  providers: [MessageService],
  templateUrl: './account-object-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; overflow-y: auto; }
      .form-container { padding: 16px; }
      .form-toolbar { display: flex; gap: 8px; margin-bottom: 16px; align-items: center; }
      .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
      .form-group { display: flex; align-items: flex-start; margin-bottom: 12px; }
      .form-group > label { width: 140px; padding-top: 6px; color: var(--text-secondary); flex-shrink: 0; font-size: 13px; }
      .form-group .field { flex: 1; }
      .required { color: var(--error); margin-left: 2px; }
      .error-msg { color: var(--error); font-size: 12px; margin-top: 4px; display: block; }
            .field-error { font-size: 12px; color: var(--error); }
      .section-title { font-size: 13px; font-weight: 600; color: var(--text-primary); margin-bottom: 8px; }
      .type-checkboxes { display: flex; flex-direction: column; gap: 8px; }
    `,
  ],
})
export class AccountObjectFormComponent implements OnInit {
  readonly store = inject(AccountObjectsStore);
  private readonly api = inject(AccountObjectsApiService);
  private readonly lookupsApi = inject(LookupsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly injector = inject(Injector);

  currencies = signal<CurrencyDto[]>([]);
  departments = signal<DepartmentDto[]>([]);
  loadingDetail = signal(false);
  activeTab = signal('0');
  rowVersion = signal<number>(0);

  genderOptions = [
    { label: 'Nam', value: 0 },
    { label: 'Nữ', value: 1 },
    { label: 'Khác', value: 2 },
  ];

  form: FormGroup = this.fb.group({
    objectCode: ['', [Validators.required, Validators.maxLength(25)]],
    objectName: ['', [Validators.required, Validators.maxLength(255)]],
    objectNameEnglish: [''],
    objectType: [1, [Validators.min(1)]],
    address: [''],
    taxCode: [''],
    email: [''],
    phone: [''],
    fax: [''],
    website: [''],
    contactPerson: [''],
    contactPhone: [''],
    description: [''],
    accountObjectGroupId: [null],
    creditLimit: [0],
    paymentTermDays: [0],
    isActive: [true],
    employeeProfile: this.fb.group({
      citizenId: [''],
      dateOfBirth: [null],
      gender: [null],
      socialInsuranceNumber: [''],
      hireDate: [null],
      departmentId: [null],
      dependentCount: [0],
    }),
    bankAccounts: this.fb.array([]),
  });

  get bankAccountsArray(): FormArray {
    return this.form.get('bankAccounts') as FormArray;
  }

  ngOnInit(): void {
    this.lookupsApi.getCurrencies().subscribe((data) => this.currencies.set(data));
    this.lookupsApi.getDepartments().subscribe((data) => this.departments.set(data));

    effect(() => {
      const mode = this.store.formMode();
      const id = this.store.selectedId();
      if (mode === 'edit' && id) {
        this.loadDetail(id);
      } else if (mode === 'create') {
        this.form.reset({
          objectCode: '',
          objectName: '',
          objectNameEnglish: '',
          objectType: 1,
          address: '',
          taxCode: '',
          email: '',
          phone: '',
          fax: '',
          website: '',
          contactPerson: '',
          contactPhone: '',
          description: '',
          accountObjectGroupId: null,
          creditLimit: 0,
          paymentTermDays: 0,
          isActive: true,
        });
        this.bankAccountsArray.clear();
        this.cdr.markForCheck();
      }
    }, { allowSignalWrites: true, injector: this.injector });
  }

  private loadDetail(id: string): void {
    this.loadingDetail.set(true);
    this.api.getById(id).subscribe({
      next: (detail) => {
        // Patch main fields
        this.form.patchValue({
          objectCode: detail.objectCode,
          objectName: detail.objectName,
          objectNameEnglish: detail.objectNameEnglish ?? '',
          objectType: detail.objectType,
          address: detail.address ?? '',
          taxCode: detail.taxCode ?? '',
          email: detail.email ?? '',
          phone: detail.phone ?? '',
          fax: detail.fax ?? '',
          website: detail.website ?? '',
          contactPerson: detail.contactPerson ?? '',
          contactPhone: detail.contactPhone ?? '',
          description: detail.description ?? '',
          accountObjectGroupId: detail.accountObjectGroupId ?? null,
          creditLimit: detail.creditLimit,
          paymentTermDays: detail.paymentTermDays,
          isActive: detail.isActive,
        });

        // Employee profile
        if (detail.employeeProfile) {
          this.form.get('employeeProfile')!.patchValue({
            citizenId: detail.employeeProfile.citizenId ?? '',
            dateOfBirth: detail.employeeProfile.dateOfBirth
              ? new Date(detail.employeeProfile.dateOfBirth)
              : null,
            gender: detail.employeeProfile.gender ?? null,
            socialInsuranceNumber: detail.employeeProfile.socialInsuranceNumber ?? '',
            hireDate: detail.employeeProfile.hireDate
              ? new Date(detail.employeeProfile.hireDate)
              : null,
            departmentId: detail.employeeProfile.departmentId ?? null,
            dependentCount: detail.employeeProfile.dependentCount,
          });
        }

        // Bank accounts
        this.bankAccountsArray.clear();
        for (const ba of detail.bankAccounts) {
          this.bankAccountsArray.push(this.createBankAccountRow(ba.id, ba.bankName, ba.bankBranch, ba.accountNumber, ba.swiftCode));
        }

        // Store rowVersion for updates
        this.rowVersion.set(detail.rowVersion ?? 0);

        this.loadingDetail.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingDetail.set(false);
        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải dữ liệu đối tượng' });
        this.cdr.markForCheck();
      },
    });
  }

  private createBankAccountRow(
    id?: string,
    bankName = '',
    bankBranch?: string,
    accountNumber = '',
    swiftCode?: string
  ): FormGroup {
    return this.fb.group({
      id: [id ?? null],
      bankName: [bankName, Validators.required],
      bankBranch: [bankBranch ?? ''],
      accountNumber: [accountNumber, Validators.required],
      swiftCode: [swiftCode ?? ''],
    });
  }

  addBankAccount(): void {
    this.bankAccountsArray.push(this.createBankAccountRow());
  }

  removeBankAccount(index: number): void {
    this.bankAccountsArray.removeAt(index);
  }

  isCustomer(): boolean {
    return (this.form.value.objectType! & 1) !== 0;
  }

  isVendor(): boolean {
    return (this.form.value.objectType! & 2) !== 0;
  }

  isEmployee(): boolean {
    return (this.form.value.objectType! & 4) !== 0;
  }

  toggleType(bit: number): void {
    const current = this.form.value.objectType ?? 0;
    const newVal = current ^ bit;
    if (newVal < 1) return;
    this.form.patchValue({ objectType: newVal });
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.ctrlKey && event.shiftKey && event.key === 'S') {
      event.preventDefault();
      this.saveAndNew();
      return;
    }
    if (event.ctrlKey && event.key === 's') {
      event.preventDefault();
      this.save();
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    const dto = this.buildDto();
    const mode = this.store.formMode();
    if (mode === 'create') {
      this.store.createItem(dto).then(
        () => {
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu đối tượng thành công' });
        },
        (err) => {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Không thể lưu đối tượng' });
        }
      );
    } else {
      const id = this.store.selectedId()!;
      const rowVersion = this.rowVersion();
      this.store.updateItem(id, { ...dto, rowVersion }).then(
        () => {
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu đối tượng thành công' });
        },
        (err) => {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Không thể lưu đối tượng' });
        }
      );
    }
  }

  saveAndNew(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }
    const dto = this.buildDto();
    const mode = this.store.formMode();
    const afterSave = () => {
      this.store.openCreate();
    };
    if (mode === 'create') {
      this.store.createItem(dto).then(afterSave, (err) => {
        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Không thể lưu đối tượng' });
      });
    } else {
      const id = this.store.selectedId()!;
      const rowVersion = this.rowVersion();
      this.store.updateItem(id, { ...dto, rowVersion }).then(afterSave, (err) => {
        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Không thể lưu đối tượng' });
      });
    }
  }

  private buildDto(): CreateAccountObjectDto {
    const v = this.form.value;

    const bankAccounts: BankAccountInputDto[] = (v.bankAccounts ?? []).map((ba: any) => ({
      id: ba.id ?? undefined,
      bankName: ba.bankName,
      bankBranch: ba.bankBranch || undefined,
      accountNumber: ba.accountNumber,
      swiftCode: ba.swiftCode || undefined,
    }));

    let employeeProfile: EmployeeProfileInputDto | undefined;
    if (this.isEmployee()) {
      const ep = v.employeeProfile;
      employeeProfile = {
        citizenId: ep?.citizenId || undefined,
        dateOfBirth: ep?.dateOfBirth ? (ep.dateOfBirth as Date).toISOString().split('T')[0] : undefined,
        gender: ep?.gender ?? undefined,
        socialInsuranceNumber: ep?.socialInsuranceNumber || undefined,
        hireDate: ep?.hireDate ? (ep.hireDate as Date).toISOString().split('T')[0] : undefined,
        departmentId: ep?.departmentId || undefined,
        dependentCount: ep?.dependentCount ?? 0,
      };
    }

    return {
      objectCode: v.objectCode,
      objectName: v.objectName,
      objectNameEnglish: v.objectNameEnglish || undefined,
      objectType: v.objectType,
      address: v.address || undefined,
      taxCode: v.taxCode || undefined,
      email: v.email || undefined,
      phone: v.phone || undefined,
      fax: v.fax || undefined,
      website: v.website || undefined,
      contactPerson: v.contactPerson || undefined,
      contactPhone: v.contactPhone || undefined,
      description: v.description || undefined,
      accountObjectGroupId: v.accountObjectGroupId || undefined,
      creditLimit: v.creditLimit ?? 0,
      paymentTermDays: v.paymentTermDays ?? 0,
      isActive: v.isActive ?? true,
      bankAccounts,
      openingBalances: [],
      employeeProfile,
    };
  }

  isFieldInvalid(path: string): boolean {
    const ctrl = this.form.get(path);
    return !!(ctrl?.invalid && ctrl?.touched);
  }
}
