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
