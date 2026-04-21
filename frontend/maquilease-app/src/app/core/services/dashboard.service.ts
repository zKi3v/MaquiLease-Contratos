import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface DashboardKpi {
  totalAssets: number;
  activeContracts: number;
  totalExpectedRevenue: number;
  totalCollectedRevenue: number;
  defaultRatePercentage: number;
}

export interface ForecastPoint {
  month: string;
  realRevenue: number;
  predictedRevenue: number;
}

export interface AssetDistribution {
  available: number;
  rented: number;
  maintenance: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private api = inject(ApiService);

  getKpis(): Observable<DashboardKpi> {
    return this.api.get<DashboardKpi>('dashboard/kpis');
  }

  getRevenueForecast(): Observable<ForecastPoint[]> {
    return this.api.get<ForecastPoint[]>('dashboard/revenue-forecast');
  }

  getAssetStatus(): Observable<AssetDistribution> {
    return this.api.get<AssetDistribution>('dashboard/asset-status');
  }
}
