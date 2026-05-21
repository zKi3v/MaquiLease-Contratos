import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';
import { Payment, CreatePaymentDto } from '../models/payment.interface';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private api = inject(ApiService);
  private http = inject(HttpClient);
  private endpoint = 'payments';

  getPayments(): Observable<Payment[]> {
    return this.api.get<Payment[]>(this.endpoint);
  }

  registerPayment(payment: CreatePaymentDto): Observable<any> {
    return this.api.post<any>(this.endpoint, payment);
  }

  downloadReceipt(paymentId: number): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/${this.endpoint}/${paymentId}/receipt`, {
      responseType: 'blob'
    });
  }
}

