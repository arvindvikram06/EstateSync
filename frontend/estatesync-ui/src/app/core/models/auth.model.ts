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

export interface AuthResponse {
  accessToken: string;
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  isKycVerified: boolean;
  isOwnerVerified: boolean;
}
