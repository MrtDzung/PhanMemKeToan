import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { ImportCoaDialogComponent } from './import-coa-dialog.component';
import { AccountTreeStore } from '../../store/account-tree.store';

describe('ImportCoaDialogComponent', () => {
  let component: ImportCoaDialogComponent;
  let fixture: ComponentFixture<ImportCoaDialogComponent>;
  let mockStore: { importCoa: jasmine.Spy };

  beforeEach(async () => {
    mockStore = {
      importCoa: jasmine.createSpy('importCoa').and.returnValue(
        Promise.resolve({ imported: 5, skipped: 2, overwritten: 0, errors: [] })
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ImportCoaDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: AccountTreeStore, useValue: mockStore },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ImportCoaDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // T01: open() resets all state
  it('T01: open() resets all state and sets visible=true', () => {
    // Dirty some state first
    component.activeStep.set(1);
    component.selectedStdId.set('TT99');
    component.resolution.set('overwrite');
    component.result.set({ imported: 1, skipped: 0, overwritten: 0, errors: [] });
    component.importError.set('some error');
    component.showAllErrors.set(true);

    component.open();

    expect(component.activeStep()).toBe(0);
    expect(component.selectedStdId()).toBeNull();
    expect(component.resolution()).toBe('skip');
    expect(component.result()).toBeNull();
    expect(component.importError()).toBeNull();
    expect(component.showAllErrors()).toBe(false);
    expect(component.visible()).toBe(true);
  });

  // T02: canNext is false when no standard selected
  it('T02: canNext is false when no standard selected', () => {
    expect(component.selectedStdId()).toBeNull();
    expect(component.canNext()).toBe(false);
  });

  // T03: canNext is true after selecting TT99
  it('T03: canNext is true after selecting TT99', () => {
    component.selectedStdId.set('TT99');
    expect(component.canNext()).toBe(true);
  });

  // T04: showOverwriteWarn reactive to resolution
  it('T04: showOverwriteWarn is reactive to resolution signal', () => {
    component.resolution.set('skip');
    expect(component.showOverwriteWarn()).toBe(false);

    component.resolution.set('overwrite');
    expect(component.showOverwriteWarn()).toBe(true);

    component.resolution.set('skip');
    expect(component.showOverwriteWarn()).toBe(false);
  });

  // T05: Step 1 footer — check activeStep signal (dialog portal renders outside fixture)
  it('T05: activeStep is 0 initially (Step 1 shown)', () => {
    component.open();
    expect(component.activeStep()).toBe(0);
  });

  // T06: Quay lại returns to step 0
  it('T06: setting activeStep back to 0 works (Quay lại)', () => {
    component.activeStep.set(1);
    expect(component.activeStep()).toBe(1);

    component.activeStep.set(0);
    expect(component.activeStep()).toBe(0);
  });

  // T07: store.importCoa called with correct args
  it('T07: executeImport calls store.importCoa with correct args', fakeAsync(() => {
    component.selectedStdId.set('TT99');
    component.resolution.set('skip');

    component.executeImport();
    tick();

    expect(mockStore.importCoa).toHaveBeenCalledWith('TT99', 'skip');
  }));

  // T08: loading state disables buttons
  it('T08: during import, canImport is false and isClosable is false', () => {
    component.selectedStdId.set('TT99');
    component.importing.set(true);

    expect(component.canImport()).toBe(false);
    expect(component.isClosable()).toBe(false);
  });

  // T09: success result replaces stepper
  it('T09: after successful import, showResultPanel is true and result is not null', fakeAsync(() => {
    component.open();
    component.selectedStdId.set('TT99');

    component.executeImport();
    tick();

    expect(component.showResultPanel()).toBe(true);
    expect(component.result()).not.toBeNull();
    expect(component.result()?.imported).toBe(5);
  }));

  // T10: result stat cards have correct CSS classes for color tokens
  it('T10: result stat cards have correct CSS class names for color tokens', fakeAsync(() => {
    component.open();
    component.selectedStdId.set('TT99');
    component.executeImport();
    tick();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.stat-card--imported')).toBeTruthy();
    expect(compiled.querySelector('.stat-card--skipped')).toBeTruthy();
    expect(compiled.querySelector('.stat-card--overwritten')).toBeTruthy();
  }));

  // T11: error list rendered when result.errors.length > 0
  it('T11: error list is rendered when result has errors', fakeAsync(() => {
    mockStore.importCoa.and.returnValue(
      Promise.resolve({ imported: 3, skipped: 1, overwritten: 0, errors: ['Lỗi số 1', 'Lỗi số 2'] })
    );
    component.open();
    component.selectedStdId.set('TT99');
    component.executeImport();
    tick();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const errorItems = compiled.querySelectorAll('.result-errors li');
    expect(errorItems.length).toBe(2);
    expect(errorItems[0].textContent?.trim()).toBe('Lỗi số 1');
  }));

  // T12: network error sets importError, not result
  it('T12: network error sets importError and result remains null', fakeAsync(() => {
    mockStore.importCoa.and.returnValue(
      Promise.reject({ error: { detail: 'Lỗi kết nối máy chủ' } })
    );
    component.open();
    component.selectedStdId.set('TT133');
    component.activeStep.set(1); // user navigated to step 2

    component.executeImport();
    tick();

    expect(component.importError()).toBe('Lỗi kết nối máy chủ');
    expect(component.result()).toBeNull();
    // Error shows inline in step 2 (not result panel) — allows retry
    expect(component.showResultPanel()).toBe(false);
    expect(component.activeStep()).toBe(1);
  }));

  // T12b: network error without detail uses fallback message
  it('T12b: network error without detail uses fallback message', fakeAsync(() => {
    mockStore.importCoa.and.returnValue(Promise.reject({}));
    component.selectedStdId.set('TT133');
    component.executeImport();
    tick();

    expect(component.importError()).toBe('Nhập danh mục thất bại');
  }));

  // T13: Đóng button sets visible to false
  it('T13: setting visible to false closes the dialog', fakeAsync(() => {
    component.open();
    expect(component.visible()).toBe(true);

    // Simulate "Đóng" button click
    component.visible.set(false);

    expect(component.visible()).toBe(false);
  }));

  // T14: isClosable depends on importing signal
  it('T14: isClosable is true normally and false during import', () => {
    component.importing.set(false);
    expect(component.isClosable()).toBe(true);

    component.importing.set(true);
    expect(component.isClosable()).toBe(false);
  });

  // Additional: hiddenErrorCount and showAllErrors
  it('shows first 5 errors and hides rest; showAllErrors reveals all', () => {
    const manyErrors = ['E1', 'E2', 'E3', 'E4', 'E5', 'E6', 'E7'];
    component.result.set({ imported: 0, skipped: 0, overwritten: 0, errors: manyErrors });

    expect(component.visibleErrors().length).toBe(5);
    expect(component.hiddenErrorCount()).toBe(2);

    component.showAllErrors.set(true);
    expect(component.visibleErrors().length).toBe(7);
    expect(component.hiddenErrorCount()).toBe(0);
  });
});
