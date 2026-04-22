import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  template: `
    <div class="app-shell">
      <app-sidebar></app-sidebar>
      <app-header></app-header>
      <main class="app-main">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-shell {
      min-height: 100vh;
      background-color: var(--surface-b);
    }
    .app-main {
      margin-left: 15rem;
      margin-top: 3.75rem;
      padding: 1.5rem;
      min-height: calc(100vh - 3.75rem);
    }

    @media (max-width: 768px) {
      .app-main {
        margin-left: 0;
        padding: 1rem;
      }
    }
  `]
})
export class MainLayoutComponent { }
//prueba
