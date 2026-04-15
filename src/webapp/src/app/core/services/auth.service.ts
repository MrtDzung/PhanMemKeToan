import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser, LoginResponse } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  async login(email: string, password: string, rememberMe: boolean): Promise<LoginResponse> {
    return firstValueFrom(
      this.http.post<LoginResponse>(`${this.baseUrl}/api/auth/login`, { email, password, rememberMe }, { withCredentials: true })
    );
  }

  async refreshToken(): Promise<LoginResponse> {
    return firstValueFrom(
      this.http.post<LoginResponse>(`${this.baseUrl}/api/auth/refresh`, {}, { withCredentials: true })
    );
  }

  async logout(): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.baseUrl}/api/auth/logout`, {}, { withCredentials: true })
    );
  }

  async getCurrentUser(): Promise<CurrentUser> {
    return firstValueFrom(
      this.http.get<CurrentUser>(`${this.baseUrl}/api/me`, { withCredentials: true })
    );
  }
}
