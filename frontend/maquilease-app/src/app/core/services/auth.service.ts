import { Injectable, inject, signal, Injector } from '@angular/core';
import { Auth, signInWithEmailAndPassword, signOut, user, User } from '@angular/fire/auth';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private auth = inject(Auth);
  private router = inject(Router);
  private injector = inject(Injector);

  // Observable que emite el usuario de Firebase o null
  public firebaseUser$ = user(this.auth);
  
  // Signal para acceder sincrónicamente al estado de Firebase
  public currentUser = signal<User | null>(null);

  // Signal para acceder al perfil de la base de datos (con roles de SQL Server)
  public userDbProfile = signal<any | null>(null);

  constructor() {
    this.firebaseUser$.subscribe(async (u) => {
      this.currentUser.set(u);
      if (u) {
        try {
          await this.syncUserProfile();
        } catch (err) {
          console.error('Error al sincronizar perfil de usuario:', err);
          // Si el usuario está inactivo o no autorizado, forzar logout
          this.logout();
        }
      } else {
        this.userDbProfile.set(null);
      }
    });
  }

  private get http(): HttpClient {
    return this.injector.get(HttpClient);
  }

  async syncUserProfile(): Promise<any> {
    try {
      const res = await firstValueFrom(
        this.http.post<any>(`${environment.apiUrl}/auth/sync`, {})
      );
      this.userDbProfile.set(res);
      return res;
    } catch (error) {
      this.userDbProfile.set(null);
      throw error;
    }
  }

  async login(email: string, pass: string) {
    try {
      const result = await signInWithEmailAndPassword(this.auth, email, pass);
      // Forzar sincronización inmediata tras el login
      await this.syncUserProfile();
      return result;
    } catch (error) {
      console.error('Login error', error);
      throw error;
    }
  }

  async logout() {
    this.userDbProfile.set(null);
    await signOut(this.auth);
    this.router.navigate(['/login']);
  }

  async getToken(): Promise<string | null> {
    const user = this.auth.currentUser;
    if (user) {
      return await user.getIdToken();
    }
    return null;
  }

  // Helpers de Roles
  isAdmin(): boolean {
    return this.userDbProfile()?.role === 'admin';
  }

  isGerente(): boolean {
    return this.userDbProfile()?.role === 'gerente';
  }

  isOperador(): boolean {
    return this.userDbProfile()?.role === 'operador';
  }

  hasRole(allowedRoles: string[]): boolean {
    const role = this.userDbProfile()?.role;
    return allowedRoles.includes(role);
  }
}
