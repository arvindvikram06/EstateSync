export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

export type OwnerType = 'Individual' | 'Company';
export type CompanyType = 'PvtLtd' | 'LLP' | 'Proprietorship' | 'Partnership' | 'Other';

export interface BecomeOwnerRequest {
  ownerType: OwnerType;
  companyName?: string;
  companyType?: CompanyType;
  cin?: string;
  gstin?: string;
}

export interface UserProfileResponse {
  fullName: string;
  email: string;
  phoneNumber: string;
  createdAt: string;
  isKycVerified: boolean;
  isFinanciallyVerified: boolean;
  isOwnerVerified: boolean;
  ownerType?: OwnerType;
  companyName?: string;
  companyType?: CompanyType;
  gstin?: string;
}

export interface AuthResponse {
  accessToken: string;
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
}
