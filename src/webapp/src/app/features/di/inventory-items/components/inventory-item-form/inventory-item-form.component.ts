import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  Injector,
  OnInit,
  computed,
  inject,
  signal,
  effect,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import {
  FormArray,
  FormBuilder,
  FormGroup,
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
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { TreeSelectModule } from 'primeng/treeselect';
import { MessageService, TreeNode } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';
import { InventoryItemsStore } from '../../store/inventory-items.store';
import { InventoryItemsApiService } from '../../services/inventory-items-api.service';
import { LookupsApiService } from '../../../setup/services/lookups-api.service';
import { UnitDto, WarehouseDto, CategoryTreeNode, UpdateInventoryItemDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-inventory-item-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    ToggleSwitchModule,
    SelectModule,
    CheckboxModule,
    TabsModule,
    TableModule,
    ToastModule,
    TreeSelectModule,
    TranslateModule,
  ],
  providers: [MessageService],
  templateUrl: './inventory-item-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host { display: block; height: 100%; overflow-y: auto; }
      .form-container { padding: 16px; }
      .form-toolbar { display: flex; gap: 8px; margin-bottom: 16px; align-items: center; }
      .form-group { display: flex; align-items: flex-start; margin-bottom: 12px; }
      .form-group label { width: 140px; padding-top: 6px; color: var(--text-secondary); flex-shrink: 0; }
      .form-group .field { flex: 1; }
      .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
      .required { color: var(--error); margin-left: 2px; }
      .field-error { font-size: 12px; color: var(--error); display: block; margin-top: 4px; }
    `,
  ],
})
export class InventoryItemFormComponent implements OnInit {
  readonly store = inject(InventoryItemsStore);
  private readonly api = inject(InventoryItemsApiService);
  private readonly lookupsApi = inject(LookupsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  units = signal<UnitDto[]>([]);
  warehouses = signal<WarehouseDto[]>([]);
  loadingDetail = signal(false);
  activeTab = signal('0');
  rowVersion = signal<number>(0);

  categoryTreeNodes = computed<TreeNode[]>(() =>
    this.buildTreeSelectNodes(this.store.categories())
  );

  itemTypeOptions = [
    { label: 'Hàng hóa', value: 1 },
    { label: 'Nguyên vật liệu', value: 2 },
    { label: 'Thành phẩm', value: 3 },
    { label: 'Dịch vụ', value: 4 },
  ];

  costingMethodOptions = [
    { label: 'Bình quân gia quyền', value: 3 },
    { label: 'Nhập trước xuất trước (FIFO)', value: 1 },
    { label: 'Nhập sau xuất trước (LIFO)', value: 2 },
    { label: 'Đích danh', value: 4 },
  ];

  barcodeTypeOptions = [
    { label: 'Code 128', value: 1 },
    { label: 'EAN-13', value: 2 },
    { label: 'EAN-8', value: 4 },
    { label: 'QR Code', value: 3 },
  ];

  operatorOptions = [
    { label: '×', value: '*' },
    { label: '÷', value: '/' },
  ];

  form: FormGroup = this.fb.group({
    itemCode: ['', [Validators.required, Validators.maxLength(25)]],
    itemName: ['', [Validators.required, Validators.maxLength(255)]],
    itemNameEnglish: [''],
    unitId: ['', Validators.required],
    categoryId: [null],
    itemType: [1, Validators.required],
    costingMethod: [3, Validators.required],
    defaultTaxRate: [null],
    salePrice1: [null],
    salePrice2: [null],
    minStockLevel: [0],
    maxStockLevel: [0],
    leadTimeDays: [0],
    isFollowSerial: [false],
    isFollowLot: [false],
    isFollowExpiry: [false],
    isPanelItem: [false],
    panelUnitId: [{ value: null, disabled: true }],
    formulaTemplateId: [null],
    isActive: [true],
    unitConverts: this.fb.array([]),
    barcodes: this.fb.array([]),
    itemAttributes: this.fb.array([]),
  });

  get unitConvertsArray(): FormArray { return this.form.get('unitConverts') as FormArray; }
  get barcodesArray(): FormArray { return this.form.get('barcodes') as FormArray; }
  get itemAttributesArray(): FormArray { return this.form.get('itemAttributes') as FormArray; }

  ngOnInit(): void {
    this.lookupsApi.getUnits().subscribe((data) => this.units.set(data));
    this.lookupsApi.getWarehouses().subscribe((data) => this.warehouses.set(data));

    this.form.get('isPanelItem')?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((isPanelItem) => {
      const panelUnitCtrl = this.form.get('panelUnitId');
      if (isPanelItem) { panelUnitCtrl?.enable(); } 
      else { panelUnitCtrl?.disable(); panelUnitCtrl?.setValue(null); }
    });

    effect(() => {
      const mode = this.store.formMode();
      const id = this.store.selectedId();
      if (mode === 'edit' && id) {
        this.loadDetail(id);
      } else if (mode === 'create') {
        this.resetForm();
        this.cdr.markForCheck();
      }
    }, { allowSignalWrites: true, injector: this.injector });
  }

  @HostListener('document:keydown.control.s', ['$event'])
  onCtrlS(event: Event): void { event.preventDefault(); this.save(); }

  @HostListener('document:keydown.control.shift.s', ['$event'])
  onCtrlShiftS(event: Event): void { event.preventDefault(); this.saveAndNew(); }

  @HostListener('document:keydown.control.d', ['$event'])
  onCtrlD(event: Event): void { event.preventDefault(); /* duplicate logic if needed */ }

  private resetForm(): void {
    this.form.reset({
      itemCode: '', itemName: '', itemNameEnglish: '', unitId: '', categoryId: null,
      itemType: 1, costingMethod: 3, defaultTaxRate: null, salePrice1: null, salePrice2: null,
      minStockLevel: 0, maxStockLevel: 0, leadTimeDays: 0, isFollowSerial: false, isFollowLot: false,
      isFollowExpiry: false, isPanelItem: false, panelUnitId: null, formulaTemplateId: null, isActive: true,
    });
    this.unitConvertsArray.clear(); this.barcodesArray.clear(); this.itemAttributesArray.clear();
    this.rowVersion.set(0);
  }

  private loadDetail(id: string): void {
    this.loadingDetail.set(true);
    this.api.getById(id).subscribe({
      next: (detail) => {
        this.rowVersion.set(detail.rowVersion || 0);
        this.form.patchValue({
          itemCode: detail.itemCode, itemName: detail.itemName, itemNameEnglish: detail.itemNameEnglish ?? '',
          unitId: detail.unitId, categoryId: this._findCategoryNode(this.categoryTreeNodes(), detail.categoryId) ?? null, itemType: detail.itemType,
          costingMethod: detail.costingMethod, defaultTaxRate: detail.defaultTaxRate ?? null,
          salePrice1: detail.salePrice1 ?? null, salePrice2: detail.salePrice2 ?? null,
          minStockLevel: detail.minStockLevel ?? 0, maxStockLevel: detail.maxStockLevel ?? 0,
          leadTimeDays: detail.leadTimeDays ?? 0, isFollowSerial: detail.isFollowSerial,
          isFollowLot: detail.isFollowLot, isFollowExpiry: detail.isFollowExpiry,
          isPanelItem: detail.isPanelItem, panelUnitId: detail.panelUnitId ?? null,
          formulaTemplateId: detail.formulaTemplateId ?? null, isActive: detail.isActive,
        });

        if (detail.isPanelItem) { this.form.get('panelUnitId')?.enable(); } 
        else { this.form.get('panelUnitId')?.disable(); }

        this.unitConvertsArray.clear();
        detail.unitConverts?.forEach((uc) => {
          let op = '*'; let rate = uc.convertRate;
          if (rate < 1) { op = '/'; rate = 1 / rate; }
          this.unitConvertsArray.push(
            this.fb.group({
              id: [uc.id], unitId: [uc.unitId, Validators.required],
              convertRate: [rate, [Validators.required, Validators.min(0.0001)]], operator: [op]
            })
          );
        });

        this.barcodesArray.clear();
        detail.barcodes?.forEach((bc) =>
          this.barcodesArray.push(
            this.fb.group({
              id: [bc.id], barcodeValue: [bc.barcodeValue, Validators.required],
              barcodeType: [bc.barcodeType, Validators.required], isPrimary: [bc.isPrimary]
            })
          )
        );

        this.itemAttributesArray.clear();
        detail.itemAttributes?.forEach((attr) =>
          this.itemAttributesArray.push(
            this.fb.group({
              id: [attr.id], attributeTypeId: [attr.attributeTypeId, Validators.required],
              attributeValue: [attr.attributeValue],
            })
          )
        );

        this.loadingDetail.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingDetail.set(false);
        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải chi tiết' });
        this.cdr.markForCheck();
      },
    });
  }

  buildTreeSelectNodes(categories: CategoryTreeNode[]): TreeNode[] { return categories.map((c) => this.mapCategoryTreeNode(c)); }
  private mapCategoryTreeNode(cat: CategoryTreeNode): TreeNode {
    return { label: cat.categoryName, data: cat.id, children: cat.children?.map((c) => this.mapCategoryTreeNode(c)) ?? [], };
  }
  private _findCategoryNode(nodes: TreeNode[], id: string | null | undefined): TreeNode | null {
    if (!id) return null;
    for (const node of nodes) {
      if (node.data === id) return node;
      if (node.children?.length) {
        const found = this._findCategoryNode(node.children, id);
        if (found) return found;
      }
    }
    return null;
  }

  isFieldInvalid(path: string): boolean {
    const ctrl = this.form.get(path);
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched));
  }

  addUnitConvert(): void {
    this.unitConvertsArray.push(this.fb.group({ id: [null], unitId: ['', Validators.required], convertRate: [1, [Validators.required, Validators.min(0.0001)]], operator: ['*'] }));
  }
  removeUnitConvert(i: number): void { this.unitConvertsArray.removeAt(i); }
  
  getUnitConvertNote(i: number): string {
    const group = this.unitConvertsArray.at(i);
    const unitId = group.get('unitId')?.value;
    const rate = group.get('convertRate')?.value || 1;
    const op = group.get('operator')?.value || '*';
    const mainUnitId = this.form.get('unitId')?.value;
    
    if (!unitId || !mainUnitId) return '';
    const mainUnit = this.units().find(u => u.id === mainUnitId);
    const subUnit = this.units().find(u => u.id === unitId);
    if (!mainUnit || !subUnit) return '';
    
    return op === '*' ? `1 ${subUnit.unitName} = ${rate} ${mainUnit.unitName}` : `1 ${subUnit.unitName} = 1/${rate} ${mainUnit.unitName}`;
  }

  addBarcode(): void {
    this.barcodesArray.push(this.fb.group({ id: [null], barcodeValue: ['', Validators.required], barcodeType: [1, Validators.required], isPrimary: [false] }));
  }
  removeBarcode(i: number): void { this.barcodesArray.removeAt(i); }

  addAttribute(): void {
    this.itemAttributesArray.push(this.fb.group({ id: [null], attributeTypeId: ['', Validators.required], attributeValue: [''] }));
  }
  removeAttribute(i: number): void { this.itemAttributesArray.removeAt(i); }

  save(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this._doSave(false); }
  saveAndNew(): void { if (this.form.invalid) { this.form.markAllAsTouched(); return; } this._doSave(true); }

  private _doSave(andNew: boolean): void {
    const raw = this.form.getRawValue();
    const mode = this.store.formMode();
    const id = this.store.selectedId();
    
    const cleanUnitConverts = raw.unitConverts.map((uc: any) => ({
      unitId: uc.unitId, convertRate: uc.operator === '/' ? (1 / uc.convertRate) : uc.convertRate
    }));

    const dto = {
      itemCode: raw.itemCode, itemName: raw.itemName, itemNameEnglish: raw.itemNameEnglish || undefined,
      unitId: raw.unitId, categoryId: (raw.categoryId?.data ?? raw.categoryId) || undefined, itemType: raw.itemType,
      costingMethod: raw.costingMethod, defaultTaxRate: raw.defaultTaxRate ?? undefined,
      salePrice1: raw.salePrice1 ?? undefined, salePrice2: raw.salePrice2 ?? undefined,
      minStockLevel: raw.minStockLevel ?? 0, maxStockLevel: raw.maxStockLevel ?? 0,
      leadTimeDays: raw.leadTimeDays ?? 0, isFollowSerial: raw.isFollowSerial,
      isFollowLot: raw.isFollowLot, isFollowExpiry: raw.isFollowExpiry,
      isPanelItem: raw.isPanelItem, panelUnitId: raw.panelUnitId || undefined,
      formulaTemplateId: raw.formulaTemplateId || undefined, isActive: raw.isActive,
      unitConverts: cleanUnitConverts,
      barcodes: raw.barcodes.map((b: any) => ({ barcodeValue: b.barcodeValue, barcodeType: b.barcodeType, isPrimary: b.isPrimary || false })),
      itemAttributes: raw.itemAttributes,
      openingBalances: []
    };

    if (mode === 'create') {
      this.api.create(dto as any).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu hàng tồn kho' });
          if (andNew) this.resetForm(); else this.store.closeForm();
          this.store.loadList();
        },
        error: (err) => this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Lỗi khi lưu' })
      });
    } else {
      this.api.update(id!, { ...dto, rowVersion: this.rowVersion() } as UpdateInventoryItemDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã cập nhật' });
          if (andNew) { this.store.openCreate(); } else this.store.closeForm();
          this.store.loadList();
        },
        error: (err) => this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err?.error?.errors?.[0] ?? 'Lỗi khi lưu' })
      });
    }
  }
}
