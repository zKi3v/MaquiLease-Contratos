import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  isDarkMode = signal(false);

  constructor() {
    this.initTheme();
  }

  private initTheme() {
    const saved = localStorage.getItem('maquilease-theme');
    if (saved === 'dark') {
      this.isDarkMode.set(true);
      document.documentElement.setAttribute('data-theme', 'dark');
    } else {
      this.isDarkMode.set(false);
      document.documentElement.setAttribute('data-theme', 'light');
    }
  }

  toggleTheme() {
    const current = this.isDarkMode();
    if (current) {
      document.documentElement.setAttribute('data-theme', 'light');
      localStorage.setItem('maquilease-theme', 'light');
      this.isDarkMode.set(false);
    } else {
      document.documentElement.setAttribute('data-theme', 'dark');
      localStorage.setItem('maquilease-theme', 'dark');
      this.isDarkMode.set(true);
    }
  }
}
