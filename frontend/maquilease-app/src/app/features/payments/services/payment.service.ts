import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Payment, CreatePaymentDto } from '../models/payment.interface';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private api = inject(ApiService);
  private endpoint = 'payments';

  getPayments(): Observable<Payment[]> {
    return this.api.get<Payment[]>(this.endpoint);
  }

  registerPayment(payment: CreatePaymentDto): Observable<any> {
    return this.api.post<any>(this.endpoint, payment);
  }

  downloadReceiptUrl(paymentId: number): string {
    return `https://localhost:7154/api/payments/${paymentId}/receipt`;
  }
}
