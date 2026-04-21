export interface Client {
  clientId: number;
  ruc: string;
  businessName: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  sector?: string;
  riskScore?: number;
  createdAt: string;
  isActive: boolean;
}

export interface CreateClientDto {
  ruc: string;
  businessName: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  sector?: string;
}
