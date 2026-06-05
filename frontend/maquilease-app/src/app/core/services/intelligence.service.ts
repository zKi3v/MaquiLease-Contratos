import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

// ── Risk Score ──────────────────────────────────
export interface RiskFactor {
  name: string;
  weight: number;
  rawValue: number;
  weightedScore: number;
  description: string;
}

export interface RiskScore {
  clientId: number;
  clientName: string;
  score: number;
  category: string;
  categoryColor: string;
  factors: RiskFactor[];
  calculatedAt: string;
}

// ── Pricing Recommendation ──────────────────────
export interface PricingRequest {
  assetId?: number;
  serviceId?: number;
  clientId?: number;
  durationMonths: number;
}

export interface PricingFactor {
  name: string;
  impact: string;
  description: string;
}

export interface PricingResponse {
  minPrice: number;
  suggestedPrice: number;
  maxPrice: number;
  currency: string;
  explanation: string;
  factors: PricingFactor[];
}

// ── Revenue Forecast ────────────────────────────
export interface ForecastBandPoint {
  month: string;
  optimistic: number;
  expected: number;
  pessimistic: number;
  isHistorical: boolean;
}

export interface RevenueForecast {
  points: ForecastBandPoint[];
  historicalCollectionRate: number;
  summary: string;
}

// ── Client Segmentation ─────────────────────────
export interface ClientSegment {
  clientId: number;
  clientName: string;
  sector: string;
  segment: string;
  segmentColor: string;
  segmentIcon: string;
  riskScore: number;
  overdueInstallments: number;
  totalContractValue: number;
  suggestedAction: string;
}

export interface SegmentationSummary {
  clients: ClientSegment[];
  segmentCounts: { [key: string]: number };
}

// ── Asset Health Analysis ───────────────────────
export interface AssetHealth {
  assetId: number;
  assetName: string;
  assetCode: string;
  category: string;
  healthIndex: number;
  wearPercentage: number;
  contractsCount: number;
  status: string;
  recommendation: string;
}

// ── Matchmaker Recommendations ──────────────────
export interface MatchmakerRecommendation {
  clientId: number;
  clientName: string;
  sector: string;
  assetId: number;
  assetName: string;
  assetCategory: string;
  affinityScore: number;
  suggestedMonthlyRate: number;
  confidenceLevel: string;
  reasoning: string;
}

// ── Risk Simulation ─────────────────────────────
export interface RiskSimulationRequest {
  sector: string;
  totalAmount: number;
  downPayment: number;
  installmentsCount: number;
  onTimePaymentRate: number;
}

export interface SimulatedRisk {
  score: number;
  category: string;
  categoryColor: string;
  recommendations: string[];
}

@Injectable({
  providedIn: 'root'
})
export class IntelligenceService {
  private api = inject(ApiService);

  getRiskScore(clientId: number): Observable<RiskScore> {
    return this.api.get<RiskScore>(`intelligence/default-risk/${clientId}`);
  }

  getPricingRecommendation(request: PricingRequest): Observable<PricingResponse> {
    return this.api.post<PricingResponse>('intelligence/pricing-recommendation', request);
  }

  getRevenueForecast(): Observable<RevenueForecast> {
    return this.api.get<RevenueForecast>('intelligence/revenue-forecast');
  }

  getClientScoring(): Observable<SegmentationSummary> {
    return this.api.get<SegmentationSummary>('intelligence/client-scoring');
  }

  getAssetHealth(): Observable<AssetHealth[]> {
    return this.api.get<AssetHealth[]>('intelligence/asset-health');
  }

  getMatchmaker(): Observable<MatchmakerRecommendation[]> {
    return this.api.get<MatchmakerRecommendation[]>('intelligence/matchmaker');
  }

  simulateRisk(request: RiskSimulationRequest): Observable<SimulatedRisk> {
    return this.api.post<SimulatedRisk>('intelligence/simulate', request);
  }

  getRiskHistory(clientId: number): Observable<any[]> {
    return this.api.get<any[]>(`intelligence/client/${clientId}/risk-history`);
  }
}
