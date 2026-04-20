import { Injectable } from '@angular/core';

/** Tenant decimal precision configuration keys */
export type NumberFormatPrecisionKey =
  | 'amount'
  | 'foreignAmount'
  | 'unitPrice'
  | 'quantity'
  | 'exchangeRate'
  | 'allocation';

const DEFAULT_PRECISION: Record<NumberFormatPrecisionKey, number> = {
  amount: 0,
  foreignAmount: 3,
  unitPrice: 2,
  quantity: 2,
  exchangeRate: 2,
  allocation: 10,
};

/**
 * Centralized number formatting service.
 * Reads tenant NumberFormatConfig (thousandSeparator, decimalSeparator)
 * so components never hardcode Angular locale format strings like '1.2-2'.
 *
 * Usage:
 *   inject(NumberFormatService).format(value, 'exchangeRate')
 */
@Injectable({ providedIn: 'root' })
export class NumberFormatService {
  /** Thousand separator from tenant config (default: '.') */
  get thousandSeparator(): string {
    return this.getConfig('thousandSeparator', '.');
  }

  /** Decimal separator from tenant config (default: ',') */
  get decimalSeparator(): string {
    return this.getConfig('decimalSeparator', ',');
  }

  /** Format a numeric value for display using tenant config */
  format(value: number | null | undefined, precision: NumberFormatPrecisionKey | number = 'amount'): string {
    if (value === null || value === undefined) return '';
    const decimals = typeof precision === 'number' ? precision : DEFAULT_PRECISION[precision];
    return this.formatNumber(value, decimals);
  }

  /** Format exchange rate (2 decimals by default) */
  formatExchangeRate(value: number | null | undefined): string {
    return this.format(value, 'exchangeRate');
  }

  /** Format a monetary amount */
  formatAmount(value: number | null | undefined): string {
    return this.format(value, 'amount');
  }

  private formatNumber(value: number, decimals: number): string {
    const factor = Math.pow(10, decimals);
    const rounded = Math.round(value * factor) / factor;
    const [intPart, fracPart] = rounded.toFixed(decimals).split('.');

    // Apply thousand separator
    const formattedInt = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, this.thousandSeparator);

    if (decimals === 0) return formattedInt;
    return `${formattedInt}${this.decimalSeparator}${fracPart}`;
  }

  private getConfig(key: string, defaultValue: string): string {
    try {
      const raw = localStorage.getItem('tenantNumberFormat');
      if (raw) {
        const cfg = JSON.parse(raw) as Record<string, string>;
        return cfg[key] ?? defaultValue;
      }
    } catch {
      // ignore parse errors
    }
    return defaultValue;
  }
}
