import { Injectable, inject, signal } from '@angular/core';
import { Auth, signInWithEmailAndPassword, signOut, user, User } from '@angular/fire/auth';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private auth = inject(Auth);
  private router = inject(Router);

  // Observable que emite el usuario de Firebase o null
  public firebaseUser$ = user(this.auth);
  
  // Signal para acceder sincrónicamente al estado
  public currentUser = signal<User | null>(null);

  constructor() {
    this.firebaseUser$.subscribe((u) => {
      this.currentUser.set(u);
    });
  }

  async login(email: string, pass: string) {
    try {
      const result = await signInWithEmailAndPassword(this.auth, email, pass);
      return result;
    } catch (error) {
      console.error('Login error', error);
      throw error;
    }
  }

  async logout() {
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
}
