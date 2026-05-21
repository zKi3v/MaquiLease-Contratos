import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { Contract, Installment } from '../models/contract.interface';
import { ContractService } from '../services/contract.service';
import { PaymentService } from '../../payments/services/payment.service';

@Component({
  selector: 'app-contract-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    DropdownModule,
    ToastModule,
    CardModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './contract-detail.component.html',
  styleUrls: ['./contract-detail.component.scss']
})
export class ContractDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private contractService = inject(ContractService);
  private paymentService = inject(PaymentService);
  private messageService = inject(MessageService);

  contractId!: number;
  contract: Contract | null = null;
  loading: boolean = false;

  // KPIs
  totalPaid: number = 0;
  totalPending: number = 0;
  overdueCount: number = 0;

  // Modal Pago
  displayPaymentModal: boolean = false;
  selectedInstallment: Installment | null = null;
  paymentForm = {
    installmentId: 0,
    amount: 0,
    paymentMethod: 'transferencia',
    documentType: 'boleta'
  };

  paymentMethods = [
    { label: 'Transferencia Bancaria', value: 'transferencia' },
    { label: 'Efectivo', value: 'efectivo' },
    { label: 'Tarjeta de Crédito', value: 'tarjeta' }
  ];

  documentTypes = [
    { label: 'Boleta de Venta', value: 'boleta' },
    { label: 'Factura Electrónica', value: 'factura' }
  ];

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.contractId = +params['id'];
      this.loadContractDetails();
    });
  }

  loadContractDetails() {
    this.loading = true;
    this.contractService.getContract(this.contractId).subscribe({
      next: (data) => {
        this.contract = data;
        this.calculateKpis(data);
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'No se pudieron cargar los detalles del contrato'
        });
      }
    });
  }

  calculateKpis(c: Contract) {
    if (!c.installments) return;
    
    this.totalPaid = c.installments
      .filter(i => i.status === 'pagado' || i.status === 'parcial')
      .reduce((acc, curr) => acc + curr.paidAmount, 0);

    const totalToFinance = c.totalAmount;
    this.totalPending = Math.max(0, totalToFinance - this.totalPaid);

    // Calcular cuotas vencidas: cuotas en mora/pendiente y cuya fecha de vencimiento es anterior a hoy
    const today = new Date();
    this.overdueCount = c.installments.filter(i => {
      const isPending = i.status === 'pendiente' || i.status === 'parcial';
      const isOverdue = new Date(i.dueDate) < today;
      return isPending && isOverdue;
    }).length;
  }

  openPayment(inst: Installment) {
    this.selectedInstallment = inst;
    const pendingAmountForInstallment = (inst.amount + inst.penaltyAmount) - inst.paidAmount;
    
    this.paymentForm = {
      installmentId: inst.installmentId,
      amount: pendingAmountForInstallment,
      paymentMethod: 'transferencia',
      documentType: 'boleta'
    };
    this.displayPaymentModal = true;
  }

  processPayment() {
    if (!this.selectedInstallment) return;

    const maxAmount = (this.selectedInstallment.amount + this.selectedInstallment.penaltyAmount) - this.selectedInstallment.paidAmount;
    if (this.paymentForm.amount <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Monto inválido',
        detail: 'El monto a pagar debe ser mayor que 0'
      });
      return;
    }

    if (this.paymentForm.amount > maxAmount) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Monto supera el saldo',
        detail: `El monto máximo a pagar es S/. ${maxAmount.toFixed(2)}`
      });
      return;
    }

    this.paymentService.registerPayment(this.paymentForm).subscribe({
      next: (res: any) => {
        this.displayPaymentModal = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Pago Registrado',
          detail: 'Se ha registrado el pago con éxito. Descargando comprobante...'
        });

        // Descarga del PDF como Blob
        this.paymentService.downloadReceipt(res.paymentId).subscribe({
          next: (blob: Blob) => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Recibo_${this.contract?.contractNumber}_Cuota_${this.selectedInstallment?.installmentNumber}.pdf`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'No se pudo generar el comprobante PDF de QuestPDF'
            });
          }
        });

        // Recargar datos para actualizar la tabla y KPIs
        this.loadContractDetails();
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'No se pudo registrar el pago. Inténtelo de nuevo.'
        });
      }
    });
  }

  isInstallmentOverdue(inst: Installment): boolean {
    if (inst.status === 'pagado') return false;
    return new Date(inst.dueDate) < new Date();
  }
}
