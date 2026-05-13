import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { AlertsService, AlertDto } from '../../../core/services/alerts.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-alerts-page',
  standalone: true,
  imports: [CommonModule, TableModule, TagModule, ButtonModule, TooltipModule, RippleModule, ToastModule],
  providers: [MessageService],
  templateUrl: './alerts-page.component.html',
  styleUrl: './alerts-page.component.scss'
})
export class AlertsPageComponent implements OnInit {
  private alertsService = inject(AlertsService);
  private messageService = inject(MessageService);

  alerts: AlertDto[] = [];
  loading: boolean = false;

  ngOnInit(): void {
    this.loadAlerts();
  }

  loadAlerts() {
    this.loading = true;
    this.alertsService.getAlerts().subscribe({
      next: (data) => {
        this.alerts = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudieron cargar las alertas' });
      }
    });
  }

  markAsRead(alert: AlertDto) {
    if (alert.isRead) return;

    this.alertsService.markAsRead(alert.alertId).subscribe({
      next: () => {
        alert.isRead = true;
        this.messageService.add({ severity: 'success', summary: 'Leída', detail: 'Alerta marcada como leída' });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'No se pudo actualizar la alerta' });
      }
    });
  }

  getSeverity(alertType: string): 'success' | 'info' | 'warning' | 'danger' | 'secondary' | 'contrast' | undefined {
    switch (alertType) {
      case 'vencimiento_proximo': return 'warning';
      case 'cuota_vencida': return 'danger';
      case 'riesgo_alto': return 'danger';
      default: return 'info';
    }
  }

  getAlertLabel(alertType: string): string {
    switch (alertType) {
      case 'vencimiento_proximo': return 'Próximo a Vencer';
      case 'cuota_vencida': return 'Cuota Vencida';
      case 'riesgo_alto': return 'Riesgo Alto';
      default: return alertType;
    }
  }
}
