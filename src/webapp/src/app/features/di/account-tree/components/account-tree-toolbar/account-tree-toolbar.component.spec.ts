import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AccountTreeToolbarComponent } from './account-tree-toolbar.component';
import { AccountTreeStore } from '../../store/account-tree.store';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

describe('AccountTreeToolbarComponent — exportExcel & isExporting', () => {
  let component: AccountTreeToolbarComponent;
  let fixture: ComponentFixture<AccountTreeToolbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountTreeToolbarComponent, NoopAnimationsModule],
      providers: [
        AccountTreeStore,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AccountTreeToolbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit exportExcel when "Xuất Excel" button is clicked', () => {
    let emitted = false;
    component.exportExcel.subscribe(() => (emitted = true));

    const buttons = fixture.debugElement.queryAll(By.css('p-button'));
    const exportBtn = buttons.find(b => {
      const label = b.nativeElement.getAttribute('label') || b.nativeElement.textContent;
      return label?.includes('Xuất Excel');
    });

    if (exportBtn) {
      exportBtn.triggerEventHandler('onClick', {});
    }

    expect(emitted).toBeTrue();
  });

  it('should disable the export button when isExporting input is true', () => {
    fixture.componentRef.setInput('isExporting', true);
    fixture.detectChanges();
    expect(component.isExporting()).toBeTrue();
  });

  it('isExporting defaults to false', () => {
    expect(component.isExporting()).toBeFalse();
  });
});
