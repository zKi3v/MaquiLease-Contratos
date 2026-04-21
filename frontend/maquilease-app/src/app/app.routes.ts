import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      { 
        path: 'clients', 
        loadComponent: () => import('./features/clients/clients-list/clients-list.component').then(m => m.ClientsListComponent)
      },
      {
        path: 'clients/new',
        loadComponent: () => import('./features/clients/clients-create/clients-create.component').then(m => m.ClientsCreateComponent)
      },
      {
        path: 'clients/edit/:id',
        loadComponent: () => import('./features/clients/clients-create/clients-create.component').then(m => m.ClientsCreateComponent)
      },
      { 
        path: 'assets', 
        loadComponent: () => import('./features/assets/assets-list/assets-list.component').then(m => m.AssetsListComponent)
      },
      {
        path: 'assets/new',
        loadComponent: () => import('./features/assets/assets-create/assets-create.component').then(m => m.AssetsCreateComponent)
      },
      {
        path: 'assets/edit/:id',
        loadComponent: () => import('./features/assets/assets-create/assets-create.component').then(m => m.AssetsCreateComponent)
      },
      { 
        path: 'services', 
        loadComponent: () => import('./features/services-catalog/services-list/services-list.component').then(m => m.ServicesListComponent)
      },
      {
        path: 'services/new',
        loadComponent: () => import('./features/services/services-create/services-create.component').then(m => m.ServicesCreateComponent)
      },
      {
        path: 'services/edit/:id',
        loadComponent: () => import('./features/services/services-create/services-create.component').then(m => m.ServicesCreateComponent)
      },
      { 
        path: 'contracts', 
        loadComponent: () => import('./features/contracts/contracts-list/contracts-list.component').then(m => m.ContractsListComponent)
      },
      { 
        path: 'contracts/new', 
        loadComponent: () => import('./features/contracts/contracts-create/contracts-create.component').then(m => m.ContractsCreateComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];

