import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { CheckboxModule } from 'primeng/checkbox';
import { ServiceObj as ServiceDto } from '../../services-catalog/models/service.interface';
import { ServiceCatalogService as ServiceService } from '../../services-catalog/services/service-catalog.service';

@Component({
  selector: 'app-services-create',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, InputNumberModule, DropdownModule, CheckboxModule],
  template: `
    <div class="card p-3 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-4">
        <h2>{{ isEdit ? 'Editar Servicio' : 'Registrar Nuevo Servicio' }}</h2>
        <p-button label="Volver al Catálogo" icon="pi pi-arrow-left" styleClass="p-button-text" (onClick)="goBack()"></p-button>
      </div>

      <div class="p-fluid grid">
        <div class="field col-12 md:col-6">
          <label for="code">Código *</label>
          <input pInputText id="code" [(ngModel)]="serviceForm.code" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="name">Nombre del Servicio *</label>
          <input pInputText id="name" [(ngModel)]="serviceForm.name" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="serviceType">Tipo *</label>
          <p-dropdown [options]="typeOptions" [(ngModel)]="serviceForm.serviceType"></p-dropdown>
        </div>
        <div class="field col-12 md:col-6">
          <label for="category">Categoría</label>
          <input pInputText id="category" [(ngModel)]="serviceForm.category" />
        </div>
        <div class="field col-12 md:col-4">
          <label for="basePrice">Precio Base *</label>
          <p-inputNumber inputId="basePrice" [(ngModel)]="serviceForm.basePrice" mode="decimal"></p-inputNumber>
        </div>
        <div class="field col-12 md:col-4">
          <label for="currency">Moneda *</label>
          <p-dropdown [options]="currencyOptions" [(ngModel)]="serviceForm.currency"></p-dropdown>
        </div>
        <div class="field col-12 md:col-4">
          <label for="priceUnit">Unidad de Cobro *</label>
          <p-dropdown [options]="unitOptions" [(ngModel)]="serviceForm.priceUnit"></p-dropdown>
        </div>
        <div class="field col-12 md:col-8">
          <label for="description">Descripción</label>
          <input pInputText id="description" [(ngModel)]="serviceForm.description" />
        </div>
        <div class="field col-12 md:col-4 flex align-items-end mb-2">
          <div class="flex align-items-center">
            <p-checkbox [(ngModel)]="serviceForm.requiresAsset" [binary]="true" inputId="requiresAsset"></p-checkbox>
            <label for="requiresAsset" class="ml-2">Requiere Activo Físico</label>
          </div>
        </div>
      </div>

      <div class="flex justify-content-end mt-4">
        <p-button label="Cancelar" icon="pi pi-times" styleClass="p-button-text mr-3" (onClick)="goBack()"></p-button>
        <p-button label="Guardar Servicio" icon="pi pi-check" (onClick)="saveService()" [disabled]="!serviceForm.code || !serviceForm.name"></p-button>
      </div>
    </div>
  `
})
export class ServicesCreateComponent implements OnInit {
  router = inject(Router);
  route = inject(ActivatedRoute);
  servicesService = inject(ServiceService);

  serviceForm: ServiceDto = {
    serviceId: 0,
    code: '',
    name: '',
    description: '',
    serviceType: 'mantenimiento',
    category: '',
    basePrice: 0,
    currency: 'PEN',
    priceUnit: 'hora',
    requiresAsset: true, isActive: true, createdAt: new Date().toISOString()
  };

  typeOptions = [
    { label: 'Mantenimiento', value: 'mantenimiento' },
    { label: 'Reparación', value: 'reparacion' },
    { label: 'Traslado', value: 'traslado' },
    { label: 'Operación', value: 'operacion' },
    { label: 'Otro', value: 'otro' }
  ];

  currencyOptions = [
    { label: 'Soles (PEN)', value: 'PEN' },
    { label: 'Dólares (USD)', value: 'USD' }
  ];

  unitOptions = [
    { label: 'Por Hora', value: 'hora' },
    { label: 'Por Día', value: 'dia' },
    { label: 'Por Mes', value: 'mes' },
    { label: 'Global / Evento', value: 'global' },
    { label: 'Por Kilómetro', value: 'km' }
  ];

  isEdit = false;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      const statedService = window.history.state.service;
      if (statedService) {
        this.serviceForm = { ...statedService };
      }
    }
  }

  saveService() {
    if (this.isEdit) {
      this.servicesService.updateService(this.serviceForm.serviceId, this.serviceForm).subscribe(() => {
        this.goBack();
      });
    } else {
      this.servicesService.createService(this.serviceForm).subscribe(() => {
        this.goBack();
      });
    }
  }

  goBack() {
    this.router.navigate(['/services']);
  }
}
