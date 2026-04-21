import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { Asset } from '../models/asset.interface';
import { AssetService } from '../services/asset.service';

@Component({
  selector: 'app-assets-create',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, InputNumberModule, DropdownModule],
  template: `
    <div class="card p-3 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-4">
        <h2>{{ isEdit ? 'Editar Activo' : 'Registrar Nuevo Activo' }}</h2>
        <p-button label="Volver al Catálogo" icon="pi pi-arrow-left" styleClass="p-button-text" (onClick)="goBack()"></p-button>
      </div>

      <div class="p-fluid grid">
        <div class="field col-12 md:col-6">
          <label for="code">Código Interno *</label>
          <input pInputText id="code" [(ngModel)]="assetForm.code" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="name">Descripción/Nombre *</label>
          <input pInputText id="name" [(ngModel)]="assetForm.name" required />
        </div>
        <div class="field col-12 md:col-6">
          <label for="category">Categoría</label>
          <input pInputText id="category" [(ngModel)]="assetForm.category" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="brand">Marca</label>
          <input pInputText id="brand" [(ngModel)]="assetForm.brand" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="model">Modelo</label>
          <input pInputText id="model" [(ngModel)]="assetForm.model" />
        </div>
        <div class="field col-12 md:col-6">
          <label for="status">Estado *</label>
          <p-dropdown [options]="statusOptions" [(ngModel)]="assetForm.status"></p-dropdown>
        </div>
        <div class="field col-12 md:col-4">
          <label for="purchasePriceCNY">Costo Compra (CNY)</label>
          <p-inputNumber inputId="purchasePriceCNY" [(ngModel)]="assetForm.purchasePriceCNY" mode="currency" currency="CNY"></p-inputNumber>
        </div>
        <div class="field col-12 md:col-4">
          <label for="purchasePriceUSD">Costo Compra (S/.)</label>
          <p-inputNumber inputId="purchasePriceUSD" [(ngModel)]="assetForm.purchasePriceUSD" mode="decimal" prefix="S/. "></p-inputNumber>
        </div>
        <div class="field col-12 md:col-4">
          <label for="currentValue">Valor Actual (S/.)</label>
          <p-inputNumber inputId="currentValue" [(ngModel)]="assetForm.currentValue" mode="decimal" prefix="S/. "></p-inputNumber>
        </div>
        <div class="field col-12">
          <label for="notes">Notas adicionales</label>
          <input pInputText id="notes" [(ngModel)]="assetForm.notes" />
        </div>
      </div>

      <div class="flex justify-content-end mt-4">
        <p-button label="Cancelar" icon="pi pi-times" styleClass="p-button-text mr-3" (onClick)="goBack()"></p-button>
        <p-button label="Guardar Activo" icon="pi pi-check" (onClick)="saveAsset()" [disabled]="!assetForm.code || !assetForm.name"></p-button>
      </div>
    </div>
  `
})
export class AssetsCreateComponent implements OnInit {
  router = inject(Router);
  route = inject(ActivatedRoute);
  assetsService = inject(AssetService);

  assetForm: Asset = {
    assetId: 0,
    code: '',
    name: '',
    category: '',
    brand: '',
    model: '',
    
    purchasePriceCNY: 0,
    purchasePriceUSD: 0,
    currentValue: 0,
    status: 'disponible',
    notes: ''
  };

  statusOptions = [
    { label: 'Disponible', value: 'disponible' },
    { label: 'En Arrendamiento', value: 'arrendado' },
    { label: 'Mantenimiento', value: 'mantenimiento' },
    { label: 'Vendido', value: 'vendido' }
  ];

  isEdit = false;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      const statedAsset = window.history.state.asset;
      if (statedAsset) {
        this.assetForm = { ...statedAsset };
      }
    }
  }

  saveAsset() {
    if (this.isEdit) {
      this.assetsService.updateAsset(this.assetForm.assetId, this.assetForm).subscribe(() => {
        this.goBack();
      });
    } else {
      this.assetsService.createAsset(this.assetForm).subscribe(() => {
        this.goBack();
      });
    }
  }

  goBack() {
    this.router.navigate(['/assets']);
  }
}
