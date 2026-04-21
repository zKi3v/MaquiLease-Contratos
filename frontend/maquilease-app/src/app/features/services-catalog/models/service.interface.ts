export interface ServiceObj {
  serviceId: number;
  code: string;
  name: string;
  description?: string;
  serviceType: string;
  category?: string;
  basePrice: number;
  priceUnit: string;
  currency: string;
  estimatedDuration?: string;
  requiresAsset: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreateServiceDto {
  code: string;
  name: string;
  description?: string;
  serviceType: string;
  category?: string;
  basePrice: number;
  priceUnit: string;
  currency: string;
  estimatedDuration?: string;
  requiresAsset: boolean;
  isActive: boolean;
}
