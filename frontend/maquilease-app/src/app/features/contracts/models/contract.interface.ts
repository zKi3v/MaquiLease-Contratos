export interface Contract {
  contractId: number;
  contractNumber: string;
  clientId: number;
  clientName: string;
  assetId?: number;
  assetCode?: string;
  serviceId?: number;
  serviceName?: string;
  contractType: string;
  startDate: string;
  endDate: string;
  totalAmount: number;
  currency: string;
  paymentFrequency: string;
  initialPayment: number;
  leasingTerms: string;
  status: string;
  signatureHash?: string;
  createdAt: string;
  installments?: Installment[];
}

export interface CreateContractDto {
  clientId: number;
  assetId?: number;
  serviceId?: number;
  contractType: string;
  startDate: string;
  endDate: string;
  totalAmount: number;
  currency: string;
  paymentFrequency: string;
  numberOfInstallments: number;
  initialPayment: number;
  leasingTerms: string;
  signatureHash?: string;
}

export interface Installment {
  installmentId: number;
  installmentNumber: number;
  dueDate: string;
  amount: number;
  penaltyAmount: number;
  status: string;
  paidAmount: number;
  paidDate?: string;
}
