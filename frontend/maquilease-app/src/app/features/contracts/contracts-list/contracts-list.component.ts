import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { Contract, Installment } from '../models/contract.interface';
import { ContractService } from '../services/contract.service';
import { PaymentService } from '../../payments/services/payment.service';

@Component({
  selector: 'app-contracts-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TableModule, ButtonModule, DialogModule, InputNumberModule, DropdownModule],
  templateUrl: './contracts-list.component.html'
})
export class ContractsListComponent implements OnInit {
  private contractService = inject(ContractService);
  private paymentService = inject(PaymentService);

  contracts: Contract[] = [];
  loading: boolean = false;

  displayInstallmentsModal: boolean = false;
  displayPaymentModal: boolean = false;
  selectedContractInstallments: Installment[] = [];
  selectedContractNum: string = '';

  paymentForm: any = {
    installmentId: 0,
    amount: 0,
    paymentMethod: 'transferencia',
    documentType: 'boleta'
  };

  paymentMethods = [
    { label: 'Transferencia', value: 'transferencia' },
    { label: 'Efectivo', value: 'efectivo' },
    { label: 'Tarjeta Crédito', value: 'tarjeta' }
  ];

  ngOnInit() {
    this.loadContracts();
  }

  loadContracts() {
    this.loading = true;
    this.contractService.getContracts().subscribe({
      next: (data) => {
        this.contracts = data;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  viewInstallments(contractId: number, contractNum: string) {
    this.selectedContractNum = contractNum;
    this.contractService.getContract(contractId).subscribe(data => {
      this.selectedContractInstallments = data.installments || [];
      this.displayInstallmentsModal = true;
    });
  }

  openPayment(inst: Installment) {
    this.paymentForm = {
      installmentId: inst.installmentId,
      amount: inst.amount - inst.paidAmount,
      paymentMethod: 'transferencia',
      documentType: 'boleta'
    };
    this.displayPaymentModal = true;
  }

  processPayment() {
    this.paymentService.registerPayment(this.paymentForm).subscribe((res: any) => {
      this.displayPaymentModal = false;
      // Download PDF receipt
      window.open(this.paymentService.downloadReceiptUrl(res.paymentId), '_blank');
      // Refresh installaments
      this.displayInstallmentsModal = false;
      this.loadContracts();
    });
  }
}
