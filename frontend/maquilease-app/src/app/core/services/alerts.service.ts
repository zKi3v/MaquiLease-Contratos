import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface AlertDto {
  alertId: number;
  contractId: number;
  contractNumber: string;
  installmentId?: number;
  installmentNumber?: number;
  alertType: string;
  message: string;
  sentAt: string;
  sentVia: string;
  isRead: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AlertsService {
  private api = inject(ApiService);

  getAlerts(unreadOnly?: boolean): Observable<AlertDto[]> {
    let url = 'alerts';
    if (unreadOnly) {
      url += '?unreadOnly=true';
    }
    return this.api.get<AlertDto[]>(url);
  }

  markAsRead(alertId: number): Observable<void> {
    return this.api.put<void>(`alerts/${alertId}/read`, {});
  }
}
