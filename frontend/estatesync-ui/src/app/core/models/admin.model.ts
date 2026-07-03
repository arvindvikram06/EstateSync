// Admin dashboard statistics
export interface TimeSeriesPoint {
  date: string;
  count: number;
  amount: number;
}

export interface AdminDashboardStats {
  totalUsers: number;
  totalOwners: number;
  totalTenants: number;
  totalVerifiers: number;
  pendingKycCount: number;
  pendingFinancialProfileCount: number;
  pendingPropertyApprovalCount: number;
  pendingLeaseApprovalCount: number;
  openComplaintsCount: number;
  activeLeasesCount: number;
  totalRevenueThisMonth: number;
  totalPlatformFeesThisMonth: number;
  propertyStatusBreakdown: Record<string, number>;
  leaseStatusBreakdown: Record<string, number>;
  complaintStatusBreakdown: Record<string, number>;
  kycStatusBreakdown: Record<string, number>;
  billStatusBreakdown: Record<string, number>;
  revenueByBillType: Record<string, number>;
  payoutStatusBreakdown: Record<string, number>;
  newUsersTimeSeries: TimeSeriesPoint[];
  paymentsTimeSeries: TimeSeriesPoint[];
}

// KYC review
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
  documents: KycDocumentSummary[];
}

export interface KycDocumentSummary {
  documentId: string;
  documentType: string;
  documentUrl: string;
  originalFileName: string;
  mimeType: string;
  reviewStatus: string;
  uploadedAt: string;
}

export interface ReviewVerificationRequest {
  status: 'Approved' | 'Rejected';
  reviewRemarks: string;
}

// Financial profile review
export interface FinancialProfileResponse {
  profileId: string;
  tenantId: string;
  tenantName: string;
  tenantEmail: string;
  status: 'Draft' | 'Pending' | 'Approved' | 'Rejected';
  employmentType: string;
  employerName: string | null;
  declaredMonthlyIncome: number;
  reviewRemarks: string | null;
  reviewedAt: string | null;
  createdAt: string;
  documents: KycDocumentSummary[];
}

// Property approval
export interface PropertySummaryResponse {
  propertyId: string;
  title: string;
  monthlyRent: number;
  state: string;
  city: string;
  addressLine: string | null;
  primaryImage: string | null;
  propertyType: string | null;
  bedrooms: number;
  bathrooms: number;
  status: string | null;
  ownerName: string;
  ownerEmail: string;
  createdAt: string;
}

export interface ApprovePropertyRequest {
  status: 'Active' | 'Rejected';
  approveRemarks: string;
}

// Staff (verifiers)
export interface StaffResponse {
  userId: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateStaffRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

// Settlements
export interface OwnerSettlementResponse {
  ownerSettlementId: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  stripePayoutId: string;
  amount: number;
  currency: string;
  destinationId: string;
  arrivalDate: string;
  status: string;
  createdAt: string;
}

export interface SettlementSummaryResponse {
  totalPaid: number;
  totalPending: number;
  totalFailed: number;
  totalSettlements: number;
  totalPlatformFeesCollected: number;
  totalPaymentsVolume: number;
}

// Connected accounts
export interface ConnectedAccountResponse {
  id: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  stripeAccountId: string;
  chargesEnabled: boolean;
  payoutsEnabled: boolean;
  detailsSubmitted: boolean;
  lastSyncedAt: string | null;
  createdAt: string;
}

// Payments ledger
export interface AdminPaymentResponse {
  paymentId: string;
  leaseBillId: string;
  paidByUserId: string;
  paidByName: string;
  paidByEmail: string;
  amount: number;
  platformFee: number;
  transactionId: string;
  paymentStatus: 'Paid' | 'Failed' | 'Pending';
  paidDate: string;
  createdAt: string;
}

// Complaints (admin view)
export interface AdminComplaintResponse {
  complaintId: string;
  leaseId: string;
  title: string;
  description: string;
  complaintType: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  reviewRemarks: string | null;
  propertyTitle?: string;
  createdAt: string;
}

// Audit logs
export interface AuditLogResponse {
  auditLogId: number;
  entityId: string | null;
  entityName: string | null;
  entityType: string | null;
  action: string | null;
  oldValues: string | null;
  newValues: string | null;
  ipAddress: string | null;
  createdAt: string;
}

// Lease approval
export interface AdminLeaseApprovalRequest {
  approve: boolean;
  remarks: string;
}

export interface AdminLeaseSummary {
  leaseId: string;
  propertyTitle: string;
  tenantName: string;
  ownerName: string;
  startDate: string;
  endDate: string;
  rentAmount: number;
  status: string;
  createdAt: string;
}
