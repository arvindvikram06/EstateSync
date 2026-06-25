export interface DocumentResponse {
  documentId: string;
  documentType: string;
  documentUrl: string;
  originalFileName: string;
  mimeType: string;
  fileSize: number;
  reviewStatus: string;
  uploadedAt: string;
}

export interface KycVerificationResponse {
  kycVerificationId: string;
  userId: string;
  fullName: string;
  email: string;
  aadhaarNumber: string | null;
  panNumber: string | null;
  status: 'Draft' | 'Pending' | 'Approved' | 'Rejected';
  reviewRemarks: string | null;
  reviewedAt: string | null;
  createdAt: string;
  documents: DocumentResponse[];
}

export interface SubmitKycRequest {
  aadhaarNumber: string;
  panNumber: string;
}

export type KycDocumentType = 'AadhaarFront' | 'AadhaarBack' | 'PanCard' | 'AddressProof';

export const REQUIRED_KYC_DOCS: KycDocumentType[] = ['AadhaarFront', 'AadhaarBack', 'PanCard', 'AddressProof'];
