import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../core/services/theme.service';
import { SidebarService } from '../../core/services/sidebar.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="app-header">
      <div class="header-left">
        <!-- Hamburger (mobile) -->
        <button class="hamburger" (click)="sidebarService.toggle()">
          <i class="pi pi-bars"></i>
        </button>
        <span class="header-greeting">¡Bienvenido, <strong>{{ authService.currentUser()?.email || 'Usuario' }}</strong></span>
      </div>

      <div class="header-right">
        <div class="header-action" title="Buscar">
          <i class="pi pi-search"></i>
        </div>
        <div class="header-action" title="Notificaciones">
          <i class="pi pi-bell"></i>
          <span class="notif-dot"></span>
        </div>
        <div class="header-action"
             (click)="themeService.toggleTheme()"
             [title]="themeService.isDarkMode() ? 'Modo Claro' : 'Modo Oscuro'">
          <i class="pi" [ngClass]="{'pi-sun': themeService.isDarkMode(), 'pi-moon': !themeService.isDarkMode()}"></i>
        </div>

        <div class="header-divider hide-mobile"></div>

        <div class="header-profile hide-mobile" (click)="authService.logout()" title="Cerrar Sesión">
          <div class="profile-avatar"><i class="pi pi-sign-out"></i></div>
          <div class="profile-info">
            <span class="profile-name">Cerrar Sesión</span>
            <span class="profile-role">Salir del sistema</span>
          </div>
        </div>
      </div>
    </header>
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
      background: linear-gradient(135deg, #ef4444, #b91c1c);
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
export class HeaderComponent {
  themeService = inject(ThemeService);
  sidebarService = inject(SidebarService);
  authService = inject(AuthService);
}
