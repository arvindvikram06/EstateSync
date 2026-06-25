export type EmploymentType = 'Salaried' | 'SelfEmployed' | 'BusinessOwner' | 'Student' | 'Other';

export interface FinancialDocumentResponse {
  documentId: string;
  documentType: string;
  documentUrl: string;
  originalFileName: string;
  mimeType: string;
  fileSize: number;
  reviewStatus: string;
  uploadedAt: string;
}

export interface SubmitTenantFinancialProfileRequest {
  employmentType: string;
  employerName: string;
  declaredMonthlyIncome: number;
}

export interface TenantFinancialProfileResponse {
  profileId: string;
  tenantId: string;
  status: 'Draft' | 'Pending' | 'Approved' | 'Rejected';
  employmentType: EmploymentType;
  employerName: string;
  declaredMonthlyIncome: number;
  documents: FinancialDocumentResponse[];
  createdAt: string;
  updatedAt: string;
}
