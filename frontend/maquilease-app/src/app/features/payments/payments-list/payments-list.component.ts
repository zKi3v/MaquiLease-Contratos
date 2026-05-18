import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { Payment } from '../models/payment.interface';
import { PaymentService } from '../services/payment.service';

@Component({
  selector: 'app-payments-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    DropdownModule,
    ToastModule,
    CardModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './payments-list.component.html',
  styleUrls: []
})
export class PaymentsListComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private messageService = inject(MessageService);

  payments: Payment[] = [];
  loading: boolean = false;

  // Stats
  totalCollected: number = 0;
  transactionsCount: number = 0;
  averagePayment: number = 0;

  ngOnInit() {
    this.loadPayments();
  }

  loadPayments() {
    this.loading = true;
    this.paymentService.getPayments().subscribe({
      next: (data) => {
        this.payments = data;
        this.calculateStats(data);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'No se pudo cargar el historial de pagos globales'
        });
      }
    });
  }

  calculateStats(list: Payment[]) {
    this.transactionsCount = list.length;
    this.totalCollected = list.reduce((acc, curr) => acc + curr.amount, 0);
    this.averagePayment = this.transactionsCount > 0 ? this.totalCollected / this.transactionsCount : 0;
  }

  downloadReceiptPdf(p: Payment) {
    this.messageService.add({
      severity: 'info',
      summary: 'Descargando',
      detail: `Generando PDF del comprobante ${p.documentNumber || p.paymentId}...`
    });

    this.paymentService.downloadReceipt(p.paymentId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Comprobante_${p.documentNumber || ('Pago_' + p.paymentId)}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        
        this.messageService.add({
          severity: 'success',
          summary: 'Descarga Completa',
          detail: 'Comprobante PDF guardado correctamente.'
        });
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error de Descarga',
          detail: 'No se pudo generar el comprobante PDF desde QuestPDF'
        });
      }
    });
  }
}
