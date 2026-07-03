export interface LeaseBillResponse {
  leaseBillId: string;
  leaseId: string;
  propertyTitle: string;
  billType: 'Rent' | 'Deposit' | 'Penalty' | 'Maintenance' | 'Other';
  dueDate: string;
  amount: number;
  pendingAmount: number;
  status: 'Pending' | 'Paid' | 'Failed';
  paidAt: string | null;
}

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
  status: 'paid' | 'pending' | 'failed';
  createdAt: string;
}

export interface OwnerEarningsSummaryResponse {
  totalPaid: number;
  totalPending: number;
  totalPayouts: number;
  lastPayoutDate: string | null;
  lastPayoutAmount: number | null;
}

export interface OwnerAvailableBalance {
  available: number;
  instantAvailable: number;
  pending: number;
  currency: string;
  payoutsEnabled: boolean;
}

export interface RequestPayoutResult {
  stripePayoutId: string;
  amount: number;
  currency: string;
  arrivalDate: string;
  status: string;
  method: string;
}
