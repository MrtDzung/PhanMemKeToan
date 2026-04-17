export enum AccountCategoryKind {
  Debit = 0,
  Credit = 1,
  Mixed = 2,
}

export enum AccountObjectType {
  None = 0,
  Supplier = 1,
  Customer = 2,
  Employee = 3,
}

export interface AccountTreeNodeDto {
  accountId: string;
  accountNumber: string;
  accountName: string;
  accountNameEnglish: string | null;
  grade: number;
  isParent: boolean;
  accountCategoryKind: AccountCategoryKind;
  inactive: boolean;
  isPostableInForeignCurrency: boolean;
  hasTransactions: boolean;
  parentId: string | null;
  children: AccountTreeNodeDto[];
}

export interface AccountDetailDto extends AccountTreeNodeDto {
  parentNumber: string | null;
  parentName: string | null;
  accountObjectType: AccountObjectType;
  detailByAccountObject: boolean;
  detailByBankAccount: boolean;
  detailByJob: boolean;
  detailByProjectWork: boolean;
  detailByOrder: boolean;
  detailByContract: boolean;
  detailByExpenseItem: boolean;
  detailByDepartment: boolean;
  detailByListItem: boolean;
  detailByPuContract: boolean;
  rowVersion: number;
  createdAt: string;
  createdBy: string;
  modifiedAt: string | null;
  modifiedBy: string | null;
}

export interface AccountListItemDto {
  accountId: string;
  accountNumber: string;
  accountName: string;
  accountCategoryKind: AccountCategoryKind;
  inactive: boolean;
  isParent: boolean;
}

export interface ImportCoaResultDto {
  imported: number;
  skipped: number;
  overwritten: number;
  errors: string[];
}

export interface CreateAccountCommand {
  accountNumber: string;
  accountName: string;
  accountNameEnglish?: string;
  parentId?: string;
  accountCategoryKind: AccountCategoryKind;
  isPostableInForeignCurrency: boolean;
  accountObjectType: AccountObjectType;
  detailByAccountObject: boolean;
  detailByBankAccount: boolean;
  detailByJob: boolean;
  detailByProjectWork: boolean;
  detailByOrder: boolean;
  detailByContract: boolean;
  detailByExpenseItem: boolean;
  detailByDepartment: boolean;
  detailByListItem: boolean;
  detailByPuContract: boolean;
}

export interface UpdateAccountCommand extends CreateAccountCommand {
  rowVersion: number;
  inactive: boolean;
}

export interface ApiResponse<T> {
  data: T;
  errors: string[] | null;
}
