import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { Client } from '../models/client.interface';
import { ClientService } from '../services/client.service';
import { CatalogService } from '../../../core/services/catalog.service';

@Component({
  selector: 'app-clients-create',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, DropdownModule, AutoCompleteModule],
  template: `
    <div class="card p-4 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-4 border-bottom-1 pb-3 surface-border">
        <h2 class="text-2xl font-bold text-900 m-0">{{ isEdit ? 'Editar Cliente' : 'Registrar Nuevo Cliente' }}</h2>
        <p-button label="Volver al Catálogo" icon="pi pi-arrow-left" styleClass="p-button-text" (onClick)="goBack()"></p-button>
      </div>

      <form #clientFormElement="ngForm" (ngSubmit)="clientFormElement.form.valid && saveClient()" novalidate>
        <div class="p-fluid grid">
          <!-- RUC -->
          <div class="field col-12 md:col-6">
            <label for="ruc" class="font-semibold block mb-2 text-800">RUC <span class="text-red-500">*</span></label>
            <input pInputText 
              id="ruc" 
              [(ngModel)]="clientForm.ruc" 
              name="ruc" 
              #rucField="ngModel" 
              required 
              pattern="^\\d{11}$" 
              [class.ng-invalid]="rucField.invalid && rucField.touched"
              [class.ng-dirty]="rucField.touched"
              placeholder="Ej. 20123456789 (11 dígitos)" />
            <small class="p-error block mt-1" *ngIf="rucField.invalid && rucField.touched">
              <span *ngIf="rucField.errors?.['required']">El RUC es obligatorio.</span>
              <span *ngIf="rucField.errors?.['pattern']">El RUC debe tener exactamente 11 dígitos numéricos.</span>
            </small>
          </div>

          <!-- Razón Social -->
          <div class="field col-12 md:col-6">
            <label for="businessName" class="font-semibold block mb-2 text-800">Razón Social <span class="text-red-500">*</span></label>
            <input pInputText 
              id="businessName" 
              [(ngModel)]="clientForm.businessName" 
              name="businessName" 
              #businessNameField="ngModel" 
              required 
              minlength="3" 
              maxlength="200"
              [class.ng-invalid]="businessNameField.invalid && businessNameField.touched"
              [class.ng-dirty]="businessNameField.touched"
              placeholder="Ej. Constructora Andina S.A.C." />
            <small class="p-error block mt-1" *ngIf="businessNameField.invalid && businessNameField.touched">
              <span *ngIf="businessNameField.errors?.['required']">La Razón Social es obligatoria.</span>
              <span *ngIf="businessNameField.errors?.['minlength']">Debe tener al menos 3 caracteres.</span>
            </small>
          </div>

          <!-- Contacto -->
          <div class="field col-12 md:col-6">
            <label for="contactName" class="font-semibold block mb-2 text-800">Contacto</label>
            <input pInputText 
              id="contactName" 
              [(ngModel)]="clientForm.contactName" 
              name="contactName" 
              #contactNameField="ngModel" 
              minlength="3" 
              maxlength="150"
              [class.ng-invalid]="contactNameField.invalid && contactNameField.touched"
              [class.ng-dirty]="contactNameField.touched"
              placeholder="Ej. Ing. Carlos Mendoza" />
            <small class="p-error block mt-1" *ngIf="contactNameField.invalid && contactNameField.touched">
              <span *ngIf="contactNameField.errors?.['minlength']">Debe tener al menos 3 caracteres.</span>
            </small>
          </div>

          <!-- Email -->
          <div class="field col-12 md:col-6">
            <label for="email" class="font-semibold block mb-2 text-800">Email</label>
            <input pInputText 
              id="email" 
              [(ngModel)]="clientForm.email" 
              name="email" 
              #emailField="ngModel" 
              pattern="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
              [class.ng-invalid]="emailField.invalid && emailField.touched"
              [class.ng-dirty]="emailField.touched"
              placeholder="Ej. contacto@empresa.com" />
            <small class="p-error block mt-1" *ngIf="emailField.invalid && emailField.touched">
              <span *ngIf="emailField.errors?.['pattern']">El formato del correo electrónico no es válido.</span>
            </small>
          </div>

          <!-- Teléfono -->
          <div class="field col-12 md:col-6">
            <label for="phone" class="font-semibold block mb-2 text-800">Teléfono</label>
            <input pInputText 
              id="phone" 
              [(ngModel)]="clientForm.phone" 
              name="phone" 
              #phoneField="ngModel" 
              pattern="^\\+?[\\d\\s\\-()]{7,20}$"
              [class.ng-invalid]="phoneField.invalid && phoneField.touched"
              [class.ng-dirty]="phoneField.touched"
              placeholder="Ej. 987654321 o +51 (1) 456-7890" />
            <small class="p-error block mt-1" *ngIf="phoneField.invalid && phoneField.touched">
              <span *ngIf="phoneField.errors?.['pattern']">El formato del teléfono no es válido (ej. 987654321).</span>
            </small>
          </div>

          <!-- Sector -->
          <div class="field col-12 md:col-6">
            <label for="sector" class="font-semibold block mb-2 text-800">Sector</label>
            <p-autoComplete 
              id="sector" 
              [(ngModel)]="clientForm.sector" 
              name="sector"
              #sectorField="ngModel"
              [suggestions]="filteredSectors" 
              (completeMethod)="filterSectors($event)" 
              [completeOnFocus]="true"
              [minLength]="0"
              placeholder="Seleccione o escriba un Sector"
              [dropdown]="false"
              styleClass="w-full"
              inputStyleClass="w-full"
              appendTo="body">
            </p-autoComplete>
          </div>

          <!-- Dirección -->
          <div class="field col-12">
            <label for="address" class="font-semibold block mb-2 text-800">Dirección</label>
            <input pInputText 
              id="address" 
              [(ngModel)]="clientForm.address" 
              name="address" 
              #addressField="ngModel" 
              minlength="5" 
              maxlength="300"
              [class.ng-invalid]="addressField.invalid && addressField.touched"
              [class.ng-dirty]="addressField.touched"
              placeholder="Ej. Av. Javier Prado Este 1234, San Isidro" />
            <small class="p-error block mt-1" *ngIf="addressField.invalid && addressField.touched">
              <span *ngIf="addressField.errors?.['minlength']">La dirección debe tener al menos 5 caracteres.</span>
            </small>
          </div>
        </div>
        
        <div class="flex justify-content-end mt-4 border-top-1 pt-3 surface-border">
          <p-button label="Cancelar" icon="pi pi-times" styleClass="p-button-text mr-3" (onClick)="goBack()"></p-button>
          <p-button label="Guardar Cliente" icon="pi pi-check" type="submit" [disabled]="clientFormElement.invalid"></p-button>
        </div>
      </form>
    </div>
  `
})
export class ClientsCreateComponent implements OnInit {
  router = inject(Router);
  route = inject(ActivatedRoute);
  clientsService = inject(ClientService);
  catalogService = inject(CatalogService);

  sectors: string[] = [];
  filteredSectors: string[] = [];

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
    this.loadSectors();
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

  loadSectors() {
    this.catalogService.getSectors().subscribe(data => {
      this.sectors = data.map(s => s.name);
    });
  }

  filterSectors(event: any) {
    const query = event.query || '';
    this.filteredSectors = this.sectors.filter(s => s.includes(query));
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
