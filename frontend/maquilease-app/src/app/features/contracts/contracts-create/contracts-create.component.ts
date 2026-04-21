import { Component, ElementRef, inject, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { StepsModule } from 'primeng/steps';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { InputTextareaModule } from 'primeng/inputtextarea';
import SignaturePad from 'signature_pad';

import { ContractService } from '../services/contract.service';
import { CreateContractDto } from '../models/contract.interface';
import { ClientService } from '../../clients/services/client.service';
import { AssetService } from '../../assets/services/asset.service';
import { ServiceCatalogService } from '../../services-catalog/services/service-catalog.service';

@Component({
  selector: 'app-contracts-create',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ButtonModule, StepsModule,
    DropdownModule, InputNumberModule, CalendarModule, InputTextareaModule
  ],
  templateUrl: './contracts-create.component.html'
})
export class ContractsCreateComponent implements OnInit, AfterViewInit {
  private contractService = inject(ContractService);
  private clientService = inject(ClientService);
  private assetService = inject(AssetService);
  private svcCatalog = inject(ServiceCatalogService);
  private router = inject(Router);

  items: any[] | undefined;
  activeIndex: number = 0;

  clients: any[] = [];
  assets: any[] = [];
  services: any[] = [];

  contractTypes = [
    { label: 'Arrendamiento Puro', value: 'arrendamiento' },
    { label: 'Prestación de Servicios', value: 'servicios' },
    { label: 'Arrendamiento + Servicios', value: 'mixto' }
  ];

  contractForm: CreateContractDto = {
    clientId: 0,
    contractType: 'arrendamiento',
    startDate: new Date().toISOString(),
    endDate: new Date().toISOString(),
    totalAmount: 0,
    currency: 'PEN',
    paymentFrequency: 'mensual',
    numberOfInstallments: 1,
    initialPayment: 0,
    leasingTerms: '',
    signatureHash: ''
  };

  @ViewChild('signatureCanvas') signatureCanvas!: ElementRef<HTMLCanvasElement>;
  signaturePad!: SignaturePad;

  ngOnInit() {
    this.items = [
      { label: 'Partes y Objeto' },
      { label: 'Términos Financieros' },
      { label: 'Firma y Acuerdo' }
    ];
    this.loadCatalogs();
  }

  ngAfterViewInit() {
    this.initSignaturePad();
  }

  initSignaturePad() {
    if (this.signatureCanvas) {
      this.signaturePad = new SignaturePad(this.signatureCanvas.nativeElement, {
        backgroundColor: 'rgb(255, 255, 255)',
        penColor: 'rgb(0, 0, 0)'
      });
    }
  }

  clearSignature() {
    this.signaturePad.clear();
  }

  loadCatalogs() {
    this.clientService.getClients().subscribe(data => this.clients = data.map(c => ({ label: c.businessName, value: c.clientId })));
    this.assetService.getAssets().subscribe(data => this.assets = data.filter(a => a.status === 'disponible').map(a => ({ label: a.name + ' (' + a.code + ')', value: a.assetId })));
    this.svcCatalog.getServices().subscribe(data => this.services = data.map(s => ({ label: s.name, value: s.serviceId })));
  }

  next() {
    if (this.activeIndex < 2) {
      this.activeIndex++;
      if (this.activeIndex === 2) {
        // Redraw canvas if needed due to display:none issues
        setTimeout(() => this.initSignaturePad(), 100);
      }
    }
  }

  prev() {
    if (this.activeIndex > 0) {
      this.activeIndex--;
    }
  }

  save() {
    if (this.signaturePad && !this.signaturePad.isEmpty()) {
      this.contractForm.signatureHash = this.signaturePad.toDataURL('image/png');
    }

    this.contractService.createContract(this.contractForm).subscribe(() => {
      this.router.navigate(['/contracts']);
    });
  }

  get canGoNextStep1(): boolean {
    const hasClient = this.contractForm.clientId > 0;
    const hasAsset = this.contractForm.assetId !== undefined && this.contractForm.assetId > 0;
    const hasService = this.contractForm.serviceId !== undefined && this.contractForm.serviceId > 0;
    return hasClient && (hasAsset || hasService);
  }

  get canGoNextStep2(): boolean {
    return this.contractForm.totalAmount > 0 && this.contractForm.numberOfInstallments > 0;
  }
}
