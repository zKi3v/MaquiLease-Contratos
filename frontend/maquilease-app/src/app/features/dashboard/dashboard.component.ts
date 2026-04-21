import { Component, OnInit, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartModule } from 'primeng/chart';
import { ThemeService } from '../../core/services/theme.service';
import { DashboardService, DashboardKpi, ForecastPoint, AssetDistribution } from '../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ChartModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  dashboardService = inject(DashboardService);
  themeService = inject(ThemeService);

  kpis: DashboardKpi | null = null;
  
  lineChartData: any;
  lineChartOptions: any;

  doughnutChartData: any;
  doughnutChartOptions: any;

  constructor() {
    effect(() => {
      // Escuchar cambios de tema interactuando con la signal isDarkMode()
      const isDark = this.themeService.isDarkMode();
      this.initChartOptions(isDark);
      if (this.lineChartData) this.lineChartData = { ...this.lineChartData };
      if (this.doughnutChartData) this.doughnutChartData = { ...this.doughnutChartData };
    });
  }

  ngOnInit() {
    this.dashboardService.getKpis().subscribe(data => {
      this.kpis = data;
    });

    this.dashboardService.getAssetStatus().subscribe(data => {
      this.initDoughnutChart(data);
    });

    this.dashboardService.getRevenueForecast().subscribe(data => {
      this.initLineChart(data);
    });
  }

  initLineChart(data: ForecastPoint[]) {
    const labels = data.map(d => d.month);
    const realData = data.map(d => d.realRevenue > 0 ? d.realRevenue : null);
    const predictedData = data.map(d => d.predictedRevenue);

    this.lineChartData = {
      labels: labels,
      datasets: [
        {
          label: 'Ingresos Reales',
          data: realData,
          fill: true,
          borderColor: '#22c55e',
          backgroundColor: 'rgba(34, 197, 94, 0.2)',
          tension: 0.4
        },
        {
          label: 'Proyección (Modelo)',
          data: predictedData,
          fill: false,
          borderColor: '#3b82f6',
          borderDash: [5, 5],
          tension: 0.4
        }
      ]
    };
  }

  initDoughnutChart(data: AssetDistribution) {
    this.doughnutChartData = {
      labels: ['Disponibles', 'Arrendados', 'En Mantenimiento'],
      datasets: [
        {
          data: [data.available, data.rented, data.maintenance],
          backgroundColor: [
            '#3b82f6',
            '#22c55e',
            '#f59e0b'
          ],
          hoverBackgroundColor: [
            '#2563eb',
            '#16a34a',
            '#d97706'
          ]
        }
      ]
    };
  }

  initChartOptions(isDark?: boolean) {
    if (isDark === undefined) {
      isDark = this.themeService.isDarkMode();
    }
    const textColor = isDark ? '#f8fafc' : '#495057';
    const textColorSecondary = isDark ? '#94a3b8' : '#6c757d';
    const gridColor = isDark ? 'rgba(255,255,255,0.1)' : '#ebedef';

    this.lineChartOptions = {
      plugins: {
        legend: { labels: { color: textColor } }
      },
      scales: {
        x: {
          ticks: { color: textColorSecondary },
          grid: { color: gridColor, drawBorder: false }
        },
        y: {
          ticks: { color: textColorSecondary },
          grid: { color: gridColor, drawBorder: false }
        }
      }
    };

    this.doughnutChartOptions = {
      plugins: {
        legend: {
          labels: { color: textColor }
        }
      },
      cutout: '60%'
    };
  }
}
