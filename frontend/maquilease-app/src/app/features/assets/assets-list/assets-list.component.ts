import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AssetService } from '../services/asset.service';
import { Asset } from '../models/asset.interface';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-assets-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, InputTextModule, RouterModule],
  template: `
    <div class="card p-3 m-3 surface-card border-round shadow-2">
      <div class="flex justify-content-between align-items-center mb-3">
        <h2>Catálogo de Maquinaria y Activos</h2>
        <p-button label="+ Registro de Activo" routerLink="/assets/new"></p-button>
      </div>

      <p-table [value]="assets" [loading]="loading" [paginator]="true" [rows]="10" styleClass="p-datatable-sm p-datatable-striped">
        <ng-template pTemplate="header">
          <tr>
            <th pSortableColumn="code">Código <p-sortIcon field="code"></p-sortIcon></th>
            <th pSortableColumn="name">Nombre <p-sortIcon field="name"></p-sortIcon></th>
            <th pSortableColumn="category">Categoría <p-sortIcon field="category"></p-sortIcon></th>
            <th pSortableColumn="brand">Marca <p-sortIcon field="brand"></p-sortIcon></th>
            <th pSortableColumn="status">Estado <p-sortIcon field="status"></p-sortIcon></th>
            <th pSortableColumn="currentValue">Valor S/. <p-sortIcon field="currentValue"></p-sortIcon></th>
            <th style="width: 10rem">Acciones</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-asset>
          <tr>
            <td>{{asset.code}}</td>
            <td>{{asset.name}}</td>
            <td>{{asset.category}}</td>
            <td>{{asset.brand}}</td>
            <td><span class="p-badge" [ngClass]="{'p-badge-success': asset.status === 'disponible', 'p-badge-warning': asset.status === 'mantenimiento', 'p-badge-danger': asset.status === 'vendido'}">{{asset.status | uppercase}}</span></td>
            <td>{{asset.currentValue | currency:'PEN':'S/. '}}</td>
            <td>
              <p-button icon="pi pi-pencil" styleClass="p-button-warning p-button-sm mr-2" (onClick)="goToEdit(asset)"></p-button>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="7">No se encontraron activos.</td>
          </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class AssetsListComponent implements OnInit {
  assets: Asset[] = [];
  loading: boolean = false;
  
  assetsService = inject(AssetService);
  router = inject(Router);

  ngOnInit() {
    this.loadAssets();
  }

  loadAssets() {
    this.loading = true;
    this.assetsService.getAssets().subscribe({
      next: (data) => {
        this.assets = data;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  goToEdit(asset: Asset) {
    this.router.navigate(['/assets/edit', asset.assetId], { state: { asset } });
  }
}
