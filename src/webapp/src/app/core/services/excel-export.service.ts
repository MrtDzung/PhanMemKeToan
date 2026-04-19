import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx-js-style';
import { AccountTreeNodeDto } from '../../features/di/models/account.models';

const EXCEL_PRIMARY_COLOR = 'FF1B5E9E';
const EXCEL_WHITE = 'FFFFFFFF';
const SHEET_NAME = 'Danh mục tài khoản';
const FILE_PREFIX = 'danh-muc-tai-khoan';
const COLUMN_HEADERS = ['Số hiệu TK', 'Tên tài khoản', 'Loại TK', 'Cấp', 'Tài khoản mẹ', 'Tiền tệ', 'Trạng thái'];
const COLUMN_WIDTHS: number[] = [15, 40, 12, 6, 15, 10, 12];
const CATEGORY_KIND_LABELS: Record<number, string> = { 0: 'Dư Nợ', 1: 'Dư Có', 2: 'Lưỡng tính' };

@Injectable({ providedIn: 'root' })
export class ExcelExportService {
  exportAccountsToExcel(
    accounts: AccountTreeNodeDto[],
    allAccounts: AccountTreeNodeDto[]
  ): void {
    if (accounts.length === 0) return;

    // Build parent lookup map: accountId → accountNumber
    const parentMap = new Map<string, string>(
      allAccounts.map(a => [a.accountId, a.accountNumber])
    );

    const today = new Date();
    const yyyymmdd =
      today.getFullYear().toString() +
      String(today.getMonth() + 1).padStart(2, '0') +
      String(today.getDate()).padStart(2, '0');

    const headerStyle = {
      fill: { fgColor: { rgb: EXCEL_PRIMARY_COLOR } },
      font: { color: { rgb: EXCEL_WHITE }, bold: true },
      alignment: { horizontal: 'center' },
    };

    const headerRow = COLUMN_HEADERS.map(h => ({
      v: h,
      t: 's',
      s: headerStyle,
    }));

    const dataRows = accounts.map(account => {
      const parentNumber = account.parentId ? (parentMap.get(account.parentId) ?? '') : '';
      const categoryLabel = CATEGORY_KIND_LABELS[account.accountCategoryKind] ?? '';
      const currencyLabel = account.isPostableInForeignCurrency ? 'Ngoại tệ' : '';
      const statusLabel = account.inactive ? 'Ngừng dùng' : 'Đang dùng';

      return [
        { v: account.accountNumber, t: 's' },
        { v: account.accountName, t: 's' },
        { v: categoryLabel, t: 's' },
        { v: account.grade, t: 'n', s: { alignment: { horizontal: 'right' } } },
        { v: parentNumber, t: 's' },
        { v: currencyLabel, t: 's' },
        { v: statusLabel, t: 's' },
      ];
    });

    const wsData = [headerRow, ...dataRows];

    // Wrap in setTimeout to allow spinner to render before CPU-intensive work
    setTimeout(() => {
      const ws = XLSX.utils.aoa_to_sheet(wsData);
      ws['!cols'] = COLUMN_WIDTHS.map(w => ({ wch: w }));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, SHEET_NAME);
      XLSX.writeFile(wb, `${FILE_PREFIX}_${yyyymmdd}.xlsx`);
    }, 0);
  }
}
