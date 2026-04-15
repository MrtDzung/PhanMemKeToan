import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageModule } from 'primeng/message';
import { AuthStore } from '../../../core/stores/auth.store';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    CheckboxModule,
    MessageModule,
  ],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <h1 class="login-title">Phần Mềm Kế Toán</h1>
          <p class="login-subtitle">Đăng nhập để tiếp tục</p>
        </div>

        @if (authStore.error()) {
          <p-message
            severity="error"
            [text]="authStore.error() ?? ''"
            styleClass="login-error"
          />
        }

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
          <div class="form-field">
            <label for="email" class="form-label">Email <span class="required">*</span></label>
            <input
              id="email"
              type="email"
              pInputText
              formControlName="email"
              placeholder="email@example.com"
              autocomplete="email"
              [class.ng-invalid]="email.invalid && email.touched"
              class="form-input"
            />
            @if (email.invalid && email.touched) {
              <small class="form-error">Email không hợp lệ</small>
            }
          </div>

          <div class="form-field">
            <label for="password" class="form-label">Mật khẩu <span class="required">*</span></label>
            <p-password
              inputId="password"
              formControlName="password"
              [feedback]="false"
              [toggleMask]="true"
              autocomplete="current-password"
              styleClass="form-input w-full"
            />
            @if (password.invalid && password.touched) {
              <small class="form-error">Vui lòng nhập mật khẩu</small>
            }
          </div>

          <div class="form-field-row">
            <p-checkbox
              inputId="rememberMe"
              formControlName="rememberMe"
              [binary]="true"
              label="Ghi nhớ đăng nhập"
            />
          </div>

          <p-button
            type="submit"
            label="Đăng nhập"
            styleClass="w-full login-btn"
            [loading]="authStore.isLoading()"
            [disabled]="loginForm.invalid || authStore.isLoading()"
          />
        </form>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: var(--surface-ground);
    }
    .login-card {
      background: var(--surface-card);
      border: 1px solid var(--surface-border);
      border-radius: 8px;
      padding: var(--spacing-xl, 32px);
      width: 100%;
      max-width: 400px;
      box-shadow: var(--shadow-card, 0 2px 8px rgba(0,0,0,0.08));
    }
    .login-header { text-align: center; margin-bottom: var(--spacing-lg, 24px); }
    .login-title { font-size: 20px; font-weight: 700; color: var(--primary); margin: 0 0 4px; }
    .login-subtitle { font-size: 13px; color: var(--text-secondary); margin: 0; }
    .login-error { margin-bottom: var(--spacing-md, 16px); }
    .login-form { display: flex; flex-direction: column; gap: var(--spacing-md, 16px); }
    .form-field { display: flex; flex-direction: column; gap: 4px; }
    .form-field-row { display: flex; align-items: center; }
    .form-label { font-size: 13px; font-weight: 500; color: var(--text-primary); }
    .required { color: var(--error); margin-left: 2px; }
    .form-input { width: 100%; }
    .form-error { color: var(--error); font-size: 12px; }
    .login-btn { margin-top: var(--spacing-sm, 8px); }
  `]
})
export class LoginComponent {
  readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
    rememberMe: [false],
  });

  get email() { return this.loginForm.controls.email; }
  get password() { return this.loginForm.controls.password; }

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) return;
    const { email, password, rememberMe } = this.loginForm.value;
    try {
      await this.authStore.login(email!, password!, rememberMe ?? false);
    } catch {
      // Error handled by store
    }
  }
}
