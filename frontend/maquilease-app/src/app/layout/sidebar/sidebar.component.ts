import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { SidebarService } from '../../core/services/sidebar.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <!-- Backdrop overlay (mobile only) -->
    <div class="sidebar-backdrop" 
         [class.visible]="sidebarService.isOpen()" 
         (click)="sidebarService.close()"></div>

    <div class="sidebar" [class.open]="sidebarService.isOpen()">
      <!-- Logo -->
      <div class="sidebar-logo">
        <div class="logo-icon">
          <i class="pi pi-box"></i>
        </div>
        <span class="logo-text">MaquiLease</span>
        <!-- Close button (mobile) -->
        <button class="sidebar-close" (click)="sidebarService.close()">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <!-- Navigation -->
      <nav class="sidebar-nav">
        <span class="nav-section-label">Analítica</span>
        <a routerLink="/dashboard" routerLinkActive="active" class="nav-item" (click)="closeMobile()">
          <div class="nav-icon"><i class="pi pi-chart-bar"></i></div>
          <span>Dashboard</span>
        </a>

        <span class="nav-section-label mt-spacer">Catálogos</span>
        <a routerLink="/clients" routerLinkActive="active" class="nav-item" (click)="closeMobile()">
          <div class="nav-icon"><i class="pi pi-users"></i></div>
          <span>Clientes</span>
        </a>
        <a routerLink="/assets" routerLinkActive="active" class="nav-item" (click)="closeMobile()">
          <div class="nav-icon"><i class="pi pi-truck"></i></div>
          <span>Maquinaria</span>
        </a>
        <a routerLink="/services" routerLinkActive="active" class="nav-item" (click)="closeMobile()">
          <div class="nav-icon"><i class="pi pi-wrench"></i></div>
          <span>Servicios</span>
        </a>

        <span class="nav-section-label mt-spacer">Operaciones</span>
        <a routerLink="/contracts" routerLinkActive="active" class="nav-item" (click)="closeMobile()">
          <div class="nav-icon"><i class="pi pi-file-edit"></i></div>
          <span>Contratos</span>
        </a>
      </nav>

      <!-- Footer -->
      <div class="sidebar-footer">
        <button class="logout-btn" (click)="logout()">
          <i class="pi pi-sign-out"></i>
          <span>Cerrar sesión</span>
        </button>
      </div>
    </div>
  `,
  styles: [`
    /* ── BACKDROP ───────────────────────────────── */
    .sidebar-backdrop {
      display: none;
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.4);
      z-index: 199;
      opacity: 0;
      pointer-events: none;
      transition: opacity 0.25s ease;
    }

    .sidebar {
      width: 15rem;
      height: 100vh;
      position: fixed;
      top: 0;
      left: 0;
      z-index: 200;
      display: flex;
      flex-direction: column;
      background-color: var(--sidebar-bg);
      border-right: 1px solid var(--glass-border);
      transition: background-color 0.3s ease, transform 0.3s cubic-bezier(0.4,0,0.2,1);
    }

    /* ── LOGO ───────────────────────────────────── */
    .sidebar-logo {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 1.25rem;
      border-bottom: 1px solid var(--glass-border);
    }
    .logo-icon {
      width: 34px; height: 34px; border-radius: 9px;
      background: linear-gradient(135deg, #2563eb, #3b82f6);
      display: flex; align-items: center; justify-content: center;
      box-shadow: 0 2px 8px rgba(37,99,235,0.3);
    }
    .logo-icon i { color: #fff; font-size: 1rem; }
    .logo-text {
      font-family: 'Outfit', sans-serif; font-size: 1.2rem;
      font-weight: 700; color: var(--text-color); letter-spacing: -0.02em;
      flex: 1;
    }
    .sidebar-close {
      display: none;
      width: 32px; height: 32px; border: none; border-radius: 8px;
      background: transparent; color: var(--text-color-secondary);
      cursor: pointer; font-size: 1rem;
      align-items: center; justify-content: center;
    }
    .sidebar-close:hover { background: var(--nav-active-bg); }

    /* ── NAV ────────────────────────────────────── */
    .sidebar-nav { flex: 1; padding: 1rem 0.75rem; overflow-y: auto; }
    .nav-section-label {
      display: block; font-size: 0.68rem; font-weight: 600;
      text-transform: uppercase; letter-spacing: 0.08em;
      color: var(--text-color-secondary); padding: 0 0.75rem; margin-bottom: 0.5rem;
    }
    .mt-spacer { margin-top: 1.5rem; }

    .nav-item {
      display: flex; align-items: center; gap: 10px;
      padding: 0.6rem 0.75rem; margin-bottom: 2px; border-radius: 9px;
      color: var(--text-color-secondary); text-decoration: none;
      font-size: 0.88rem; font-weight: 500; transition: all 0.15s ease; cursor: pointer;
    }
    .nav-item:hover { background-color: var(--nav-active-bg); color: var(--text-color); }
    .nav-item.active { background-color: var(--nav-active-bg); color: var(--nav-active-color); font-weight: 600; }
    .nav-icon {
      width: 30px; height: 30px; border-radius: 8px;
      display: flex; align-items: center; justify-content: center; font-size: 0.95rem;
    }

    /* ── FOOTER ─────────────────────────────────── */
    .sidebar-footer { padding: 1rem 0.75rem; border-top: 1px solid var(--glass-border); }
    .logout-btn {
      width: 100%; display: flex; align-items: center; gap: 10px;
      padding: 0.6rem 0.75rem; border: none; border-radius: 9px;
      background: transparent; color: var(--text-color-secondary);
      font-size: 0.88rem; font-weight: 500; font-family: 'Inter', sans-serif;
      cursor: pointer; transition: all 0.15s ease;
    }
    .logout-btn:hover { background: rgba(244,63,94,0.08); color: #f43f5e; }

    /* ── MOBILE ─────────────────────────────────── */
    @media (max-width: 768px) {
      .sidebar-backdrop { display: block; pointer-events: none; }
      .sidebar-backdrop.visible { opacity: 1; pointer-events: auto; }

      .sidebar {
        transform: translateX(-100%);
        box-shadow: 4px 0 25px rgba(0,0,0,0.15);
      }
      .sidebar.open { transform: translateX(0); }

      .sidebar-close { display: flex; }
    }
  `]
})
export class SidebarComponent {
  sidebarService = inject(SidebarService);
  private router = inject(Router);

  closeMobile() {
    if (window.innerWidth <= 768) {
      this.sidebarService.close();
    }
  }

  logout() {
    this.sidebarService.close();
    this.router.navigate(['/login']);
  }
}
