export interface LoginResponse {
  accessToken: string;
  expiresAt: string; // ISO 8601 UTC
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  tenantId: string;
  tenantName: string;
  roles: string[];
  permissions: string[];
  lastLoginAt: string | null;
  createdAt: string;
}
