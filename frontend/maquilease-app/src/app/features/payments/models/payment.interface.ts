export interface Payment {
  paymentId: number;
  installmentId: number;
  amount: number;
  paymentDate: string;
  paymentMethod: string;
  referenceNumber?: string;
  documentType: string;
  documentNumber?: string;
}

export interface CreatePaymentDto {
  installmentId: number;
  amount: number;
  paymentMethod: string;
  referenceNumber?: string;
  documentType: string;
}
