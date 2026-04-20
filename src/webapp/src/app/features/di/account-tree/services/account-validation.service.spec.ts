import { TestBed } from '@angular/core/testing';
import { AccountValidationService } from './account-validation.service';

describe('AccountValidationService', () => {
  let service: AccountValidationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AccountValidationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('validatePrefix — BR-DI01', () => {
    it('validatePrefix_ChildStartsWithParent_Should_ReturnTrue', () => {
      expect(service.validatePrefix('1111', '111')).toBeTrue();
    });

    it('validatePrefix_ChildDoesNotStartWithParent_Should_ReturnFalse', () => {
      expect(service.validatePrefix('211', '111')).toBeFalse();
    });

    it('validatePrefix_ChildEqualsParent_Should_ReturnTrue', () => {
      // Same code is technically a prefix of itself
      expect(service.validatePrefix('111', '111')).toBeTrue();
    });

    it('validatePrefix_ChildShorterThanParent_Should_ReturnFalse', () => {
      expect(service.validatePrefix('11', '111')).toBeFalse();
    });

    it('validatePrefix_ChildIsValidPrefixVariant_Should_ReturnTrue', () => {
      // Parent 1, child 11 — valid per BR-DI01
      expect(service.validatePrefix('11', '1')).toBeTrue();
    });

    it('validatePrefix_RootAccount_EmptyParent_Should_ReturnTrue', () => {
      // Root accounts have no parent — treat empty parentNumber as always valid
      expect(service.validatePrefix('1', '')).toBeTrue();
    });

    it('validatePrefix_InvalidPrefix_LooksLike_Should_ReturnFalse', () => {
      // "113" does NOT start with "12"
      expect(service.validatePrefix('113', '12')).toBeFalse();
    });

    it('validatePrefix_ValidMultiLevel_Should_ReturnTrue', () => {
      // Parent "131", child "1311" — valid
      expect(service.validatePrefix('1311', '131')).toBeTrue();
    });
  });

  describe('validateAccountNumber', () => {
    it('validateAccountNumber_ValidAlphanumeric_Should_ReturnTrue', () => {
      expect(service.validateAccountNumber('1111')).toBeTrue();
    });

    it('validateAccountNumber_ExactlyTwentyChars_Should_ReturnTrue', () => {
      expect(service.validateAccountNumber('12345678901234567890')).toBeTrue();
    });

    it('validateAccountNumber_TwentyOneChars_Should_ReturnFalse', () => {
      expect(service.validateAccountNumber('123456789012345678901')).toBeFalse();
    });

    it('validateAccountNumber_Empty_Should_ReturnFalse', () => {
      expect(service.validateAccountNumber('')).toBeFalse();
    });

    it('validateAccountNumber_WithSpecialChars_Should_ReturnFalse', () => {
      expect(service.validateAccountNumber('111-A')).toBeFalse();
    });

    it('validateAccountNumber_WithSpaces_Should_ReturnFalse', () => {
      expect(service.validateAccountNumber('111 A')).toBeFalse();
    });

    it('validateAccountNumber_WithLetters_Should_ReturnTrue', () => {
      expect(service.validateAccountNumber('ACCT001')).toBeTrue();
    });
  });
});
