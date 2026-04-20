import { TestBed } from '@angular/core/testing';
import * as XLSX from 'xlsx-js-style';
import { ExcelExportService } from './excel-export.service';
import { AccountTreeNodeDto, AccountCategoryKind } from '../../features/di/models/account.models';

function makeAccount(overrides: Partial<AccountTreeNodeDto> = {}): AccountTreeNodeDto {
  return {
    accountId: 'acc-1',
    accountNumber: '111',
    accountName: 'Tiền mặt VND',
    accountNameEnglish: null,
    grade: 1,
    isParent: false,
    accountCategoryKind: AccountCategoryKind.Debit,
    inactive: false,
    isPostableInForeignCurrency: false,
    hasTransactions: false,
    parentId: null,
    children: [],
    ...overrides,
  };
}

describe('ExcelExportService', () => {
  let service: ExcelExportService;
  let writeFileSpy: jasmine.Spy;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ExcelExportService);
    writeFileSpy = spyOn(XLSX, 'writeFile').and.callFake(() => {});
    jasmine.clock().install();
  });

  afterEach(() => {
    jasmine.clock().uninstall();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should NOT call writeFile when accounts array is empty', () => {
    service.exportAccountsToExcel([], []);
    jasmine.clock().tick(10);
    expect(writeFileSpy).not.toHaveBeenCalled();
  });

  it('should call writeFile with correct filename pattern', () => {
    const account = makeAccount();
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    expect(writeFileSpy).toHaveBeenCalledOnceWith(
      jasmine.any(Object),
      jasmine.stringMatching(/^danh-muc-tai-khoan_\d{8}\.xlsx$/)
    );
  });

  it('should map accountCategoryKind 0 to "Dư Nợ"', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ accountCategoryKind: AccountCategoryKind.Debit });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[2].v).toBe('Dư Nợ');
  });

  it('should map accountCategoryKind 1 to "Dư Có"', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ accountCategoryKind: AccountCategoryKind.Credit });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[2].v).toBe('Dư Có');
  });

  it('should map accountCategoryKind 2 to "Lưỡng tính"', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ accountCategoryKind: AccountCategoryKind.Mixed });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[2].v).toBe('Lưỡng tính');
  });

  it('should show "Ngoại tệ" when isPostableInForeignCurrency is true', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ isPostableInForeignCurrency: true });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[5].v).toBe('Ngoại tệ');
  });

  it('should show empty string for currency when isPostableInForeignCurrency is false', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ isPostableInForeignCurrency: false });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[5].v).toBe('');
  });

  it('should show "Ngừng dùng" when inactive is true', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ inactive: true });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[6].v).toBe('Ngừng dùng');
  });

  it('should show "Đang dùng" when inactive is false', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ inactive: false });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[6].v).toBe('Đang dùng');
  });

  it('should resolve parentId to accountNumber using allAccounts map', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const parent = makeAccount({ accountId: 'parent-1', accountNumber: '1', parentId: null });
    const child = makeAccount({ accountId: 'child-1', accountNumber: '111', parentId: 'parent-1' });
    service.exportAccountsToExcel([child], [parent, child]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[4].v).toBe('1');
  });

  it('should show empty string for parentNumber when parentId is null', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount({ parentId: null });
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    const dataRow = wsData[1] as Array<{ v: unknown }>;
    expect(dataRow[4].v).toBe('');
  });

  it('should include header row as first row with 7 columns', () => {
    const aoa_to_sheet_spy = spyOn(XLSX.utils, 'aoa_to_sheet').and.callThrough();
    const account = makeAccount();
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wsData: unknown[][] = aoa_to_sheet_spy.calls.first().args[0] as unknown[][];
    expect(wsData[0].length).toBe(7);
  });

  it('should set column widths on worksheet', () => {
    const account = makeAccount();
    service.exportAccountsToExcel([account], [account]);
    jasmine.clock().tick(10);
    const wb = writeFileSpy.calls.first().args[0];
    const ws = wb.Sheets['Danh mục tài khoản'];
    expect(ws['!cols']).toBeDefined();
    expect(ws['!cols'].length).toBe(7);
  });
});
