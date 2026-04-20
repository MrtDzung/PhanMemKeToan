export interface CurrencyDto {
  id: string;
  currencyCode: string;
  currencyName: string;
  currencyNameEnglish?: string;
  symbol: string;
  exchangeRate: number;
  isActive: boolean;
}

export interface UpsertCurrencyDto {
  id?: string;
  currencyCode: string;
  currencyName: string;
  currencyNameEnglish?: string;
  symbol: string;
  exchangeRate: number;
  isActive: boolean;
}

export interface UnitDto {
  id: string;
  unitCode: string;
  unitName: string;
  isActive: boolean;
}

export interface UpsertUnitDto {
  id?: string;
  unitCode: string;
  unitName: string;
  isActive: boolean;
}

export interface WarehouseDto {
  id: string;
  warehouseCode: string;
  warehouseName: string;
  address?: string;
  warehouseKeeper?: string;
  isActive: boolean;
}

export interface UpsertWarehouseDto {
  id?: string;
  warehouseCode: string;
  warehouseName: string;
  address?: string;
  warehouseKeeper?: string;
  isActive: boolean;
}

export interface DepartmentDto {
  id: string;
  departmentCode: string;
  departmentName: string;
  parentId?: string;
  level: number;
  isActive: boolean;
}

export interface UpsertDepartmentDto {
  id?: string;
  departmentCode: string;
  departmentName: string;
  parentId?: string;
  isActive: boolean;
}

export interface ExpenseItemDto {
  id: string;
  expenseCode: string;
  expenseName: string;
  isActive: boolean;
}

export interface UpsertExpenseItemDto {
  id?: string;
  expenseCode: string;
  expenseName: string;
  isActive: boolean;
}

// Department tree node (built client-side)
export interface DepartmentTreeNode {
  data: DepartmentDto;
  children: DepartmentTreeNode[];
  expanded?: boolean;
}

// ---- AccountObjects ----

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AccountObjectListItem {
  id: string;
  objectCode: string;
  objectName: string;
  objectType: number;
  taxCode?: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
}

export interface BankAccountDto {
  id: string;
  bankName: string;
  bankBranch?: string;
  accountNumber: string;
  swiftCode?: string;
}

export interface OpeningBalanceDto {
  id: string;
  currencyId: string;
  currencyCode: string;
  debitAmount: number;
  creditAmount: number;
  debitAmountOC: number;
  creditAmountOC: number;
  exchangeRate: number;
}

export interface EmployeeProfileDto {
  id: string;
  citizenId?: string;
  dateOfBirth?: string;
  gender?: number;
  socialInsuranceNumber?: string;
  hireDate?: string;
  departmentId?: string;
  departmentName?: string;
  dependentCount: number;
}

export interface AccountObjectDetail {
  id: string;
  objectCode: string;
  objectName: string;
  objectNameEnglish?: string;
  address?: string;
  taxCode?: string;
  email?: string;
  phone?: string;
  fax?: string;
  website?: string;
  contactPerson?: string;
  contactPhone?: string;
  description?: string;
  objectType: number;
  creditLimit: number;
  paymentTermDays: number;
  isActive: boolean;
  rowVersion: number;
  accountObjectGroupId?: string;
  createdAt: string;
  bankAccounts: BankAccountDto[];
  openingBalances: OpeningBalanceDto[];
  employeeProfile?: EmployeeProfileDto;
}

// ---- InventoryItems ----

export interface CategoryTreeNode {
  id: string;
  categoryCode: string;
  categoryName: string;
  parentId: string | null;
  level: number;
  isActive: boolean;
  sortOrder: number;
  children: CategoryTreeNode[];
}

export interface InventoryItemListItem {
  id: string;
  itemCode: string;
  itemName: string;
  itemType: number;
  unitId: string;
  unitCode: string;
  categoryId: string | null;
  categoryName: string | null;
  isActive: boolean;
  unitPrice: number | null;
  salePrice1: number | null;
}

export interface InventoryItemDetail extends InventoryItemListItem {
  itemNameEnglish: string;
  description: string;
  costingMethod: number;
  defaultTaxRate: number | null;
  salePrice2: number | null;
  salePrice3: number | null;
  minStockLevel: number;
  maxStockLevel: number;
  leadTimeDays: number;
  isFollowSerial: boolean;
  isFollowLot: boolean;
  isFollowExpiry: boolean;
  isPanelItem: boolean;
  panelUnitId: string | null;
  formulaTemplateId: string | null;
  rowVersion: number;
  unitConverts: UnitConvertItem[];
  barcodes: BarcodeItem[];
  itemAttributes: ItemAttributeItem[];
  openingBalances: InventoryOpeningBalance[];
}

export interface UnitConvertItem {
  id: string;
  unitId: string;
  unitCode: string;
  unitName: string;
  convertRate: number;
}

export interface BarcodeItem {
  id: string;
  barcodeValue: string;
  barcodeType: number;
  unitId: string | null;
  isPrimary: boolean;
}

export interface ItemAttributeItem {
  id: string;
  attributeTypeId: string;
  attributeCode: string;
  attributeName: string;
  attributeValue: string;
}

export interface InventoryOpeningBalance {
  id: string;
  warehouseId: string;
  warehouseName: string;
  unitId: string;
  unitCode: string;
  quantity: number;
  unitCost: number;
  amount: number;
  currencyId: string | null;
  currencyCode: string | null;
  foreignAmount: number | null;
  exchangeRate: number | null;
}

export interface InventoryItemFilters {
  itemType?: number;
  search?: string;
  isActive?: boolean;
  categoryId?: string;
}

export interface CreateInventoryItemDto {
  itemCode: string;
  itemName: string;
  itemNameEnglish?: string;
  description?: string;
  unitId: string;
  categoryId?: string;
  itemType: number;
  costingMethod: number;
  defaultTaxRate?: number;
  unitPrice?: number;
  salePrice1?: number;
  salePrice2?: number;
  salePrice3?: number;
  minStockLevel: number;
  maxStockLevel: number;
  leadTimeDays: number;
  isFollowSerial: boolean;
  isFollowLot: boolean;
  isFollowExpiry: boolean;
  isPanelItem: boolean;
  panelUnitId?: string;
  formulaTemplateId?: string;
  isActive: boolean;
  unitConverts: { unitId: string; convertRate: number }[];
  barcodes: { barcodeValue: string; barcodeType: number; unitId?: string; isPrimary: boolean }[];
  itemAttributes: { attributeTypeId: string; attributeValue: string }[];
  openingBalances: { warehouseId: string; unitId: string; quantity: number; unitCost: number; currencyId?: string; foreignAmount?: number; exchangeRate?: number }[];
}

export interface UpdateInventoryItemDto extends CreateInventoryItemDto {
  rowVersion: number;
}
