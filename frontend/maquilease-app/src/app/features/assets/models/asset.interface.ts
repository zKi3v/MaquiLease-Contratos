export interface Asset {
  assetId: number;
  code: string;
  name: string;
  category?: string;
  brand?: string;
  model?: string;
  purchaseDate?: string;
  purchasePriceCNY?: number;
  purchasePriceUSD?: number;
  currentValue?: number;
  status: string;
  imageUrl?: string;
  notes?: string;
  currency?: string;
}

export interface CreateAssetDto {
  code: string;
  name: string;
  category?: string;
  brand?: string;
  model?: string;
  purchaseDate?: string;
  purchasePriceCNY?: number;
  purchasePriceUSD?: number;
  currentValue?: number;
  status: string;
  imageUrl?: string;
  notes?: string;
  currency?: string;
}
