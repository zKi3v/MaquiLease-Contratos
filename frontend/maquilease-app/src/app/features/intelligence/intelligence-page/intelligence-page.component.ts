import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChartModule } from 'primeng/chart';
import { TabViewModule } from 'primeng/tabview';
import { KnobModule } from 'primeng/knob';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressBarModule } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { InputNumberModule } from 'primeng/inputnumber';
import { ThemeService } from '../../../core/services/theme.service';
import { ApiService } from '../../../core/services/api.service';
import {
  IntelligenceService,
  RiskScore,
  PricingRequest,
  PricingResponse,
  RevenueForecast,
  ForecastBandPoint,
  SegmentationSummary,
  ClientSegment
} from '../../../core/services/intelligence.service';

interface ClientOption {
  label: string;
  value: number;
}

interface AssetOption {
  label: string;
  value: number;
}

@Component({
  selector: 'app-intelligence-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ChartModule, TabViewModule, KnobModule,
    DropdownModule, ButtonModule, TagModule, ProgressBarModule,
    TableModule, CardModule, TooltipModule, InputNumberModule
  ],
  templateUrl: './intelligence-page.component.html',
  styleUrl: './intelligence-page.component.scss'
})
export class IntelligencePageComponent implements OnInit {
  private intelligenceService = inject(IntelligenceService);
  private apiService = inject(ApiService);
  themeService = inject(ThemeService);

  // ── Risk Score ───────────────────────────────
  clients: ClientOption[] = [];
  selectedClientId: number | null = null;
  riskScore: RiskScore | null = null;
  riskLoading = false;

  // ── Pricing ──────────────────────────────────
  assets: AssetOption[] = [];
  selectedAssetId: number | null = null;
  selectedPricingClientId: number | null = null;
  durationMonths: number = 6;
  pricingResult: PricingResponse | null = null;
  pricingLoading = false;

  // ── Forecast ─────────────────────────────────
  forecast: RevenueForecast | null = null;
  forecastChartData: any = null;
  forecastChartOptions: any = null;
  forecastLoading = false;

  // ── Segmentation ─────────────────────────────
  segmentation: SegmentationSummary | null = null;
  segmentChartData: any = null;
  segmentChartOptions: any = null;
  segmentationLoading = false;

  constructor() {
    effect(() => {
      const isDark = this.themeService.isDarkMode();
      this.initChartOptions(isDark);
      if (this.forecastChartData) this.forecastChartData = { ...this.forecastChartData };
      if (this.segmentChartData) this.segmentChartData = { ...this.segmentChartData };
    });
  }

  ngOnInit() {
    this.loadClients();
    this.loadAssets();
    this.loadForecast();
    this.loadSegmentation();
  }

  // ── Data Loaders ─────────────────────────────
  loadClients() {
    this.apiService.get<any[]>('clients').subscribe(data => {
      this.clients = data.map(c => ({ label: `${c.businessName} (${c.ruc})`, value: c.clientId }));
    });
  }

  loadAssets() {
    this.apiService.get<any[]>('assets').subscribe(data => {
      this.assets = data.map(a => ({ label: `${a.code} — ${a.name}`, value: a.assetId }));
    });
  }

  // ── 1. Risk Score ────────────────────────────
  calculateRisk() {
    if (!this.selectedClientId) return;
    this.riskLoading = true;
    this.intelligenceService.getRiskScore(this.selectedClientId).subscribe({
      next: (data) => {
        this.riskScore = data;
        this.riskLoading = false;
      },
      error: () => this.riskLoading = false
    });
  }

  getRiskColor(score: number): string {
    if (score <= 25) return '#22c55e';
    if (score <= 50) return '#eab308';
    if (score <= 75) return '#f97316';
    return '#ef4444';
  }

  getRiskSeverity(category: string): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" | undefined {
    switch (category) {
      case 'Bajo': return 'success';
      case 'Medio': return 'warning';
      case 'Alto': return 'warning';
      case 'Crítico': return 'danger';
      default: return 'info';
    }
  }

  // ── 2. Pricing ───────────────────────────────
  calculatePricing() {
    if (!this.selectedAssetId) return;
    this.pricingLoading = true;
    const request: PricingRequest = {
      assetId: this.selectedAssetId,
      clientId: this.selectedPricingClientId ?? undefined,
      durationMonths: this.durationMonths
    };
    this.intelligenceService.getPricingRecommendation(request).subscribe({
      next: (data) => {
        this.pricingResult = data;
        this.pricingLoading = false;
      },
      error: () => this.pricingLoading = false
    });
  }

  getImpactSeverity(impact: string): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" | undefined {
    switch (impact) {
      case 'positivo': return 'success';
      case 'negativo': return 'danger';
      default: return 'info';
    }
  }

  // ── 3. Forecast ──────────────────────────────
  loadForecast() {
    this.forecastLoading = true;
    this.intelligenceService.getRevenueForecast().subscribe({
      next: (data) => {
        this.forecast = data;
        this.initForecastChart(data);
        this.forecastLoading = false;
      },
      error: () => this.forecastLoading = false
    });
  }

  initForecastChart(data: RevenueForecast) {
    const labels = data.points.map(p => p.month);
    this.forecastChartData = {
      labels,
      datasets: [
        {
          label: 'Optimista',
          data: data.points.map(p => p.optimistic),
          fill: '+1',
          borderColor: '#22c55e',
          backgroundColor: 'rgba(34, 197, 94, 0.1)',
          tension: 0.4,
          borderWidth: 2,
          pointRadius: 3
        },
        {
          label: 'Esperado',
          data: data.points.map(p => p.expected),
          fill: '+1',
          borderColor: '#3b82f6',
          backgroundColor: 'rgba(59, 130, 246, 0.15)',
          tension: 0.4,
          borderWidth: 2.5,
          pointRadius: 4
        },
        {
          label: 'Pesimista',
          data: data.points.map(p => p.pessimistic),
          fill: false,
          borderColor: '#f97316',
          backgroundColor: 'rgba(249, 115, 22, 0.1)',
          tension: 0.4,
          borderWidth: 2,
          borderDash: [5, 5],
          pointRadius: 3
        }
      ]
    };
  }

  // ── 4. Segmentation ──────────────────────────
  loadSegmentation() {
    this.segmentationLoading = true;
    this.intelligenceService.getClientScoring().subscribe({
      next: (data) => {
        this.segmentation = data;
        this.initSegmentChart(data);
        this.segmentationLoading = false;
      },
      error: () => this.segmentationLoading = false
    });
  }

  initSegmentChart(data: SegmentationSummary) {
    const segmentLabels = Object.keys(data.segmentCounts);
    const segmentValues = Object.values(data.segmentCounts);
    const colors: { [key: string]: string } = {
      'Premium': '#22c55e',
      'Crecimiento': '#3b82f6',
      'En Riesgo': '#f97316',
      'Problemático': '#ef4444'
    };

    this.segmentChartData = {
      labels: segmentLabels,
      datasets: [
        {
          data: segmentValues,
          backgroundColor: segmentLabels.map(l => colors[l] || '#94a3b8'),
          hoverBackgroundColor: segmentLabels.map(l => colors[l] || '#94a3b8'),
          borderWidth: 0
        }
      ]
    };
  }

  getSegmentTagSeverity(segment: string): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" | undefined {
    switch (segment) {
      case 'Premium': return 'success';
      case 'Crecimiento': return 'info';
      case 'En Riesgo': return 'warning';
      case 'Problemático': return 'danger';
      default: return 'info';
    }
  }

  // ── Chart Options ────────────────────────────
  initChartOptions(isDark?: boolean) {
    if (isDark === undefined) isDark = this.themeService.isDarkMode();
    const textColor = isDark ? '#f8fafc' : '#495057';
    const textColorSecondary = isDark ? '#94a3b8' : '#6c757d';
    const gridColor = isDark ? 'rgba(255,255,255,0.08)' : '#ebedef';

    this.forecastChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: textColor, usePointStyle: true, padding: 16 }
        },
        tooltip: {
          callbacks: {
            label: (ctx: any) => `${ctx.dataset.label}: S/ ${ctx.parsed.y?.toLocaleString() ?? 0}`
          }
        }
      },
      scales: {
        x: {
          ticks: { color: textColorSecondary },
          grid: { color: gridColor, drawBorder: false }
        },
        y: {
          ticks: {
            color: textColorSecondary,
            callback: (val: number) => `S/ ${(val / 1000).toFixed(0)}k`
          },
          grid: { color: gridColor, drawBorder: false }
        }
      }
    };

    this.segmentChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: textColor, usePointStyle: true, padding: 16 }
        }
      },
      cutout: '55%'
    };
  }
}
