export interface LoginResponse {
  tempToken: string;
  companies: CompanyInfo[];
  rememberMe: boolean;
}

export interface CompanyInfo {
  tenantId: string;
  name: string;
  code: string;
  databaseMode: string;
  dbStatus: string;
  displayRole: string | null;
  isDefault: boolean;
}

export interface SelectCompanyRequest {
  tempToken: string;
  tenantId: string;
  rememberMe: boolean;
}

export interface SelectCompanyResponse {
  accessToken: string;
  expiresAt: string;
}

export interface SwitchCompanyRequest {
  targetTenantId: string;
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
  companies: CompanyInfo[];
  currentCompany: CompanyInfo | null;
}
