import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ThemeService } from '../../core/services/theme.service';
import { SidebarService } from '../../core/services/sidebar.service';
import { AuthService } from '../../core/services/auth.service';
import { AlertsService, AlertDto } from '../../core/services/alerts.service';
import { ApiService } from '../../core/services/api.service';
import { Subscription, interval } from 'rxjs';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    OverlayPanelModule,
    ButtonModule,
    DividerModule,
    DialogModule,
    InputTextModule
  ],
  template: `
    <header class="app-header">
      <div class="header-left">
        <!-- Hamburger (mobile) -->
        <button class="hamburger" (click)="sidebarService.toggle()">
          <i class="pi pi-bars"></i>
        </button>
        <span class="header-greeting">¡Bienvenido, <strong>{{ authService.userDbProfile()?.fullName || authService.currentUser()?.email || 'Usuario' }}</strong></span>
      </div>

      <div class="header-right">
        <div class="header-action" title="Buscar">
          <i class="pi pi-search"></i>
        </div>
        <div class="header-action" title="Notificaciones" (click)="op.toggle($event)">
          <i class="pi pi-bell"></i>
          <span class="notif-dot" *ngIf="unreadCount > 0"></span>
          <span class="notif-count" *ngIf="unreadCount > 0">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
        </div>

        <!-- Overlay de Notificaciones -->
        <p-overlayPanel #op [style]="{'width': '350px'}" styleClass="notif-overlay">
          <ng-template pTemplate="content">
            <div class="notif-header">
              <span class="notif-title">Notificaciones Recientes</span>
              <span class="notif-badge" *ngIf="unreadCount > 0">{{ unreadCount }} nuevas</span>
            </div>
            <p-divider></p-divider>
            <div class="notif-list" *ngIf="recentAlerts.length > 0; else noNotifs">
              <div *ngFor="let alert of recentAlerts" class="notif-item" [ngClass]="{'unread': !alert.isRead}" [routerLink]="['/alerts']" (click)="op.hide()">
                <div class="notif-icon" [ngClass]="alert.alertType">
                  <i class="pi" [ngClass]="{
                    'pi-exclamation-triangle': alert.alertType === 'vencimiento_proximo',
                    'pi-times-circle': alert.alertType === 'cuota_vencida',
                    'pi-info-circle': alert.alertType === 'riesgo_alto'
                  }"></i>
                </div>
                <div class="notif-content">
                  <span class="notif-message">{{ alert.message }}</span>
                  <span class="notif-time">{{ alert.sentAt | date:'shortTime' }} - {{ alert.sentAt | date:'dd/MM' }}</span>
                </div>
              </div>
            </div>
            <ng-template #noNotifs>
              <div class="notif-empty">No hay notificaciones recientes</div>
            </ng-template>
            <p-divider></p-divider>
            <div class="notif-footer">
              <button pButton label="Ver todas las alertas" class="p-button-text p-button-sm w-full" routerLink="/alerts" (click)="op.hide()"></button>
            </div>
          </ng-template>
        </p-overlayPanel>

        <div class="header-action"
             (click)="themeService.toggleTheme()"
             [title]="themeService.isDarkMode() ? 'Modo Claro' : 'Modo Oscuro'">
          <i class="pi" [ngClass]="{'pi-sun': themeService.isDarkMode(), 'pi-moon': !themeService.isDarkMode()}"></i>
        </div>

        <div class="header-divider hide-mobile"></div>

        <div class="header-profile hide-mobile" (click)="profileOp.toggle($event)" title="Mi Cuenta">
          <div class="profile-avatar"><i class="pi pi-user"></i></div>
          <div class="profile-info">
            <span class="profile-name">{{ authService.userDbProfile()?.fullName || 'Mi Cuenta' }}</span>
            <span class="profile-role">{{ authService.userDbProfile()?.role | titlecase }}</span>
          </div>
        </div>
      </div>
    </header>

    <!-- Overlay de Perfil -->
    <p-overlayPanel #profileOp [style]="{'width': '240px'}" styleClass="profile-overlay">
      <ng-template pTemplate="content">
        <div class="profile-menu-header p-2">
          <div class="font-bold text-base mb-1 text-900">{{ authService.userDbProfile()?.fullName || 'Usuario' }}</div>
          <div class="text-xs text-600 mb-2">{{ authService.currentUser()?.email }}</div>
          <span class="text-xs font-bold px-2 py-1 border-round surface-200 text-600">{{ authService.userDbProfile()?.role | uppercase }}</span>
        </div>
        <p-divider styleClass="my-2"></p-divider>
        <div class="flex flex-column gap-1">
          <button pButton label="Editar Perfil" icon="pi pi-cog" class="p-button-text p-button-sm text-left w-full align-items-center" (click)="profileOp.hide(); openEditProfile()"></button>
          <button pButton label="Cerrar Sesión" icon="pi pi-sign-out" class="p-button-text p-button-sm p-button-danger text-left w-full align-items-center" (click)="profileOp.hide(); authService.logout()"></button>
        </div>
      </ng-template>
    </p-overlayPanel>

    <!-- Diálogo de Edición de Perfil -->
    <p-dialog header="Editar Mi Perfil" 
              [(visible)]="displayProfileDialog" 
              [modal]="true" 
              [style]="{width: '100%', maxWidth: '500px'}" 
              [draggable]="false" 
              [resizable]="false"
              styleClass="p-fluid">
      
      <div class="py-2">
        <div class="field mb-3">
          <label for="profileFullName" class="font-bold block mb-1">Nombre Completo *</label>
          <input id="profileFullName" type="text" pInputText [(ngModel)]="profileForm.fullName" placeholder="Tu Nombre">
        </div>

        <div class="field mb-3">
          <label for="profilePassword" class="font-bold block mb-1">Nueva Contraseña (Opcional)</label>
          <input id="profilePassword" type="password" pInputText [(ngModel)]="profileForm.password" placeholder="Mínimo 12 caracteres para cambiar">
          <small class="text-500 mt-1 block">Déjalo en blanco si no deseas cambiar tu contraseña.</small>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <button pButton label="Cancelar" icon="pi pi-times" class="p-button-text p-button-secondary" (click)="displayProfileDialog = false"></button>
        <button pButton label="Guardar Cambios" icon="pi pi-check" class="p-button-primary" [disabled]="!profileForm.fullName.trim() || (profileForm.password && profileForm.password.trim().length < 12)" (click)="saveProfile()"></button>
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .app-header {
      position: fixed; top: 0; left: 15rem;
      width: calc(100% - 15rem); height: 3.75rem; z-index: 99;
      display: flex; align-items: center; justify-content: space-between;
      padding: 0 1.5rem;
      background: var(--header-bg);
      backdrop-filter: blur(16px); -webkit-backdrop-filter: blur(16px);
      border-bottom: 1px solid var(--glass-border);
      transition: background-color 0.3s ease;
    }

    .header-left { display: flex; align-items: center; gap: 12px; }
    .header-greeting { font-size: 0.88rem; color: var(--text-color-secondary); }
    .header-greeting strong { color: var(--text-color); }

    .hamburger {
      display: none;
      width: 36px; height: 36px; border: none; border-radius: 9px;
      background: transparent; color: var(--text-color);
      cursor: pointer; font-size: 1.15rem;
      align-items: center; justify-content: center;
      transition: background 0.15s ease;
    }
    .hamburger:hover { background: var(--nav-active-bg); }

    .header-right { display: flex; align-items: center; gap: 4px; }

    .header-action {
      width: 36px; height: 36px; border-radius: 9px;
      display: flex; align-items: center; justify-content: center;
      cursor: pointer; position: relative;
      color: var(--text-color-secondary); transition: all 0.15s ease;
    }
    .header-action:hover { background: var(--nav-active-bg); color: var(--text-color); }
    .header-action i { font-size: 1.05rem; }

    .notif-dot {
      position: absolute; top: 7px; right: 8px;
      width: 7px; height: 7px; border-radius: 50%;
      background: #f43f5e; border: 2px solid var(--surface-a);
    }
    .notif-count {
      position: absolute; top: 2px; right: 2px;
      background: #f43f5e; color: white;
      font-size: 0.6rem; font-weight: bold;
      border-radius: 10px; padding: 0 4px;
      border: 1.5px solid var(--surface-a);
      line-height: 1.2;
    }

    /* Notif Overlay Custom Styles */
    .notif-header { display: flex; justify-content: space-between; align-items: center; padding: 5px 0; }
    .notif-title { font-weight: 600; font-size: 0.95rem; }
    .notif-badge { background: #f43f5e; color: white; padding: 2px 8px; border-radius: 10px; font-size: 0.7rem; }
    
    .notif-list { display: flex; flex-direction: column; max-height: 300px; overflow-y: auto; }
    .notif-item { 
      display: flex; gap: 12px; padding: 10px; border-radius: 8px; cursor: pointer;
      transition: background 0.2s;
    }
    .notif-item:hover { background: var(--nav-active-bg); }
    .notif-item.unread { border-left: 3px solid #f43f5e; background: rgba(244, 63, 94, 0.05); }

    .notif-icon { 
      width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; 
      justify-content: center; flex-shrink: 0;
    }
    .notif-icon.vencimiento_proximo { background: #fef3c7; color: #d97706; }
    .notif-icon.cuota_vencida { background: #fee2e2; color: #dc2626; }
    .notif-icon.riesgo_alto { background: #e0e7ff; color: #4f46e5; }
    
    .notif-content { display: flex; flex-direction: column; gap: 2px; }
    .notif-message { font-size: 0.82rem; line-height: 1.3; }
    .notif-time { font-size: 0.72rem; color: var(--text-color-secondary); }
    .notif-empty { padding: 20px; text-align: center; color: var(--text-color-secondary); font-size: 0.85rem; }

    .header-divider {
      width: 1px; height: 28px;
      background: var(--glass-border); margin: 0 8px;
    }

    .header-profile {
      display: flex; align-items: center; gap: 8px;
      padding: 4px 8px 4px 4px; border-radius: 10px;
      cursor: pointer; transition: all 0.15s ease;
    }
    .header-profile:hover { background: var(--nav-active-bg); }

    .profile-avatar {
      width: 32px; height: 32px; border-radius: 9px;
      background: linear-gradient(135deg, #3b82f6, #1d4ed8);
      color: #fff; display: flex; align-items: center; justify-content: center;
      font-size: 1rem;
    }
    .profile-info { display: flex; flex-direction: column; }
    .profile-name { font-size: 0.82rem; font-weight: 600; color: var(--text-color); line-height: 1.1; }
    .profile-role { font-size: 0.7rem; color: var(--text-color-secondary); }

    /* ── MOBILE ─────────────────────────────────── */
    @media (max-width: 768px) {
      .app-header {
        left: 0; width: 100%;
        padding: 0 1rem;
      }
      .hamburger { display: flex; }
      .header-greeting { display: none; }
      .hide-mobile { display: none !important; }
    }
  `]
})
export class HeaderComponent implements OnInit, OnDestroy {
  themeService = inject(ThemeService);
  sidebarService = inject(SidebarService);
  authService = inject(AuthService);
  private alertsService = inject(AlertsService);
  private apiService = inject(ApiService);

  unreadCount = 0;
  recentAlerts: AlertDto[] = [];
  private pollingSub?: Subscription;

  displayProfileDialog = false;
  profileForm = { fullName: '', password: '' };

  ngOnInit() {
    this.checkAlerts();
    // Poll every 60 seconds
    this.pollingSub = interval(60000).subscribe(() => {
      this.checkAlerts();
    });
  }

  ngOnDestroy() {
    if (this.pollingSub) {
      this.pollingSub.unsubscribe();
    }
  }

  private checkAlerts() {
    this.alertsService.getAlerts().subscribe({
      next: (alerts) => {
        this.recentAlerts = alerts.slice(0, 5);
        this.unreadCount = alerts.filter(a => !a.isRead).length;
      },
      error: () => {}
    });
  }

  openEditProfile() {
    const profile = this.authService.userDbProfile();
    this.profileForm = {
      fullName: profile?.fullName || '',
      password: ''
    };
    this.displayProfileDialog = true;
  }

  saveProfile() {
    const payload: any = { fullName: this.profileForm.fullName.trim() };
    if (this.profileForm.password.trim()) {
      payload.password = this.profileForm.password.trim();
    }

    this.apiService.put<any>('auth/profile', payload).subscribe({
      next: (res) => {
        const currentProfile = this.authService.userDbProfile();
        this.authService.userDbProfile.set({
          ...currentProfile,
          fullName: res.fullName
        });
        alert('Perfil actualizado correctamente.');
        this.displayProfileDialog = false;
      },
      error: (err) => {
        console.error('Error al actualizar perfil:', err);
        alert(err.error?.message || 'No se pudo actualizar el perfil.');
      }
    });
  }
}
