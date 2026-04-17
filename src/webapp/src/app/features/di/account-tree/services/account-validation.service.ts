import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AccountValidationService {
  validatePrefix(accountNumber: string, parentNumber: string): boolean {
    return accountNumber.startsWith(parentNumber);
  }

  validateAccountNumber(code: string): boolean {
    return /^[a-zA-Z0-9]{1,20}$/.test(code);
  }
}
