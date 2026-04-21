import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Client } from '../models/client.interface';
import { ClientService } from '../services/client.service';

@Component({
  selector: 'app-clients-create',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule],
  template: `
    <div class="card p-3 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-4">
        <h2>{{ isEdit ? 'Editar Cliente' : 'Registrar Nuevo Cliente' }}</h2>
        <p-button label="Volver al Catálogo" icon="pi pi-arrow-left" styleClass="p-button-text" (onClick)="goBack()"></p-button>
      </div>

      <div class="p-fluid grid">
        <div class="field col-12 md:col-6">
          <label for="ruc">RUC *</label>
          <input pInputText id="ruc" [(ngModel)]="clientForm.ruc" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="businessName">Razón Social *</label>
          <input pInputText id="businessName" [(ngModel)]="clientForm.businessName" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="contactName">Contacto</label>
          <input pInputText id="contactName" [(ngModel)]="clientForm.contactName" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="email">Email</label>
          <input pInputText id="email" [(ngModel)]="clientForm.email" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="phone">Teléfono</label>
          <input pInputText id="phone" [(ngModel)]="clientForm.phone" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="sector">Sector</label>
          <input pInputText id="sector" [(ngModel)]="clientForm.sector" />
        </div>
        <div class="field col-12">
          <label for="address">Dirección</label>
          <input pInputText id="address" [(ngModel)]="clientForm.address" />
        </div>
      </div>
      
      <div class="flex justify-content-end mt-4">
        <p-button label="Cancelar" icon="pi pi-times" styleClass="p-button-text mr-3" (onClick)="goBack()"></p-button>
        <p-button label="Guardar Cliente" icon="pi pi-check" (onClick)="saveClient()" [disabled]="!clientForm.ruc || !clientForm.businessName"></p-button>
      </div>
    </div>
  `
})
export class ClientsCreateComponent implements OnInit {
  router = inject(Router);
  route = inject(ActivatedRoute);
  clientsService = inject(ClientService);

  clientForm: Client = {
    clientId: 0,
    ruc: '',
    businessName: '',
    contactName: '',
    email: '',
    phone: '',
    address: '',
    sector: '',
    isActive: true, createdAt: new Date().toISOString()
  };
  
  isEdit = false;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      // Fetching is ideally done by ID, but for now we redirect or leave placeholder
      // I'll leave the ID fetch stubbed to avoid complex mapping since List handled it in memory.
      // A better way is using a shared store or router state. 
      const statedClient = window.history.state.client;
      if (statedClient) {
        this.clientForm = { ...statedClient };
      }
    }
  }

  saveClient() {
    if (this.isEdit) {
      this.clientsService.updateClient(this.clientForm.clientId, this.clientForm).subscribe(() => {
        this.goBack();
      });
    } else {
      this.clientsService.createClient(this.clientForm).subscribe(() => {
        this.goBack();
      });
    }
  }

  goBack() {
    this.router.navigate(['/clients']);
  }
}
