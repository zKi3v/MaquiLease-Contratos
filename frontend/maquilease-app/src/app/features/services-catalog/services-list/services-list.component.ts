import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ServiceCatalogService as ServiceService } from '../services/service-catalog.service';
import { ServiceObj as ServiceDto } from '../models/service.interface';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-services-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, InputTextModule, RouterModule],
  template: `
    <div class="card p-3 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-3">
        <h2>Catálogo de Servicios</h2>
        <p-button label="+ Nuevo Servicio" routerLink="/services/new"></p-button>
      </div>

      <p-table [value]="services" [loading]="loading" [paginator]="true" [rows]="10" styleClass="p-datatable-sm p-datatable-striped">
        <ng-template pTemplate="header">
          <tr>
            <th pSortableColumn="code">Código <p-sortIcon field="code"></p-sortIcon></th>
            <th pSortableColumn="name">Servicio <p-sortIcon field="name"></p-sortIcon></th>
            <th pSortableColumn="serviceType">Tipo <p-sortIcon field="serviceType"></p-sortIcon></th>
            <th pSortableColumn="basePrice">Precio Base <p-sortIcon field="basePrice"></p-sortIcon></th>
            <th>Unidad</th>
            <th>Req. Activo</th>
            <th style="width: 10rem">Acciones</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-svc>
          <tr>
            <td>{{svc.code}}</td>
            <td>{{svc.name}}</td>
            <td>{{svc.serviceType | uppercase}}</td>
            <td>{{svc.basePrice | currency:'PEN':'S/. '}}</td>
            <td>{{svc.priceUnit}}</td>
            <td>
              <i class="pi" [ngClass]="{'pi-check-circle text-green-500': svc.requiresAsset, 'pi-times-circle text-red-500': !svc.requiresAsset}"></i>
            </td>
            <td>
              <p-button icon="pi pi-pencil" styleClass="p-button-warning p-button-sm mr-2" (onClick)="goToEdit(svc)"></p-button>
              <p-button icon="pi pi-trash" styleClass="p-button-danger p-button-sm" (onClick)="deleteService(svc)"></p-button>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="7">No se encontraron servicios.</td>
          </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class ServicesListComponent implements OnInit {
  services: ServiceDto[] = [];
  loading: boolean = false;
  
  servicesService = inject(ServiceService);
  router = inject(Router);

  ngOnInit() {
    this.loadServices();
  }

  loadServices() {
    this.loading = true;
    this.servicesService.getServices().subscribe({
      next: (data) => {
        this.services = data;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  goToEdit(service: ServiceDto) {
    this.router.navigate(['/services/edit', service.serviceId], { state: { service } });
  }

  deleteService(service: ServiceDto) {
    if(confirm(`¿Estás seguro de eliminar el servicio ${service.name}?`)) {
      this.servicesService.deleteService(service.serviceId).subscribe(() => {
        this.loadServices();
      });
    }
  }
}
