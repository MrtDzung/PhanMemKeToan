export interface BankAccountInputDto {
  id?: string;
  bankName: string;
  bankBranch?: string;
  accountNumber: string;
  swiftCode?: string;
}

export interface OpeningBalanceInputDto {
  id?: string;
  currencyId: string;
  debitAmountOC?: number;
  creditAmountOC?: number;
  exchangeRate?: number;
}

export interface EmployeeProfileInputDto {
  citizenId?: string;
  dateOfBirth?: string;
  gender?: number;
  socialInsuranceNumber?: string;
  hireDate?: string;
  departmentId?: string;
  dependentCount?: number;
}

export interface CreateAccountObjectDto {
  objectCode: string;
  objectName: string;
  objectNameEnglish?: string;
  objectType: number;
  address?: string;
  taxCode?: string;
  email?: string;
  phone?: string;
  fax?: string;
  website?: string;
  contactPerson?: string;
  contactPhone?: string;
  description?: string;
  accountObjectGroupId?: string;
  creditLimit?: number;
  paymentTermDays?: number;
  isActive: boolean;
  bankAccounts: BankAccountInputDto[];
  openingBalances: OpeningBalanceInputDto[];
  employeeProfile?: EmployeeProfileInputDto;
}

export interface UpdateAccountObjectDto extends CreateAccountObjectDto {
  rowVersion: number;
}
