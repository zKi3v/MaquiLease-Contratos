import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';

interface UserDto {
  userId: number;
  username: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
}

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, DropdownModule, TagModule, DialogModule, InputTextModule, TooltipModule],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnInit {
  users: UserDto[] = [];
  loading: boolean = false;
  
  displayDialog: boolean = false;
  newUser: any = { username: '', email: '', fullName: '', role: 'operador' };
  
  private apiService = inject(ApiService);
  authService = inject(AuthService);

  roleOptions = [
    { label: 'Administrador', value: 'admin' },
    { label: 'Gerente', value: 'gerente' },
    { label: 'Operador', value: 'operador' }
  ];

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading = true;
    this.apiService.get<UserDto[]>('auth/users').subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error al cargar usuarios:', err);
        this.loading = false;
      }
    });
  }

  changeRole(user: UserDto, newRole: string) {
    if (user.email === this.authService.userDbProfile()?.email) {
      alert('No puedes cambiar tu propio rol de administrador.');
      this.loadUsers();
      return;
    }

    this.apiService.put<any>(`auth/users/${user.userId}/role`, { role: newRole }).subscribe({
      next: (res) => {
        user.role = newRole;
      },
      error: (err) => {
        console.error('Error al actualizar rol:', err);
        alert(err.error?.message || 'No se pudo actualizar el rol.');
        this.loadUsers(); // Revert changes
      }
    });
  }

  toggleStatus(user: UserDto) {
    if (user.email === this.authService.userDbProfile()?.email) {
      alert('No puedes desactivar tu propia cuenta de administrador.');
      return;
    }

    const action = user.isActive ? 'desactivar' : 'activar';
    if (confirm(`¿Estás seguro de que deseas ${action} la cuenta de ${user.fullName}?`)) {
      this.apiService.put<any>(`auth/users/${user.userId}/status`, {}).subscribe({
        next: (res) => {
          user.isActive = !user.isActive;
        },
        error: (err) => {
          console.error('Error al actualizar estado:', err);
          alert(err.error?.message || 'No se pudo actualizar el estado.');
        }
      });
    }
  }

  getRoleSeverity(role: string): 'success' | 'info' | 'warning' | 'danger' {
    switch (role) {
      case 'admin':
        return 'danger';
      case 'gerente':
        return 'info';
      case 'operador':
        return 'success';
      default:
        return 'info';
    }
  }

  openCreateDialog() {
    this.newUser = { username: '', email: '', fullName: '', role: 'operador', password: '' };
    this.displayDialog = true;
  }

  createUser() {
    if (!this.newUser.email.trim() || !this.newUser.fullName.trim() || !this.newUser.password?.trim()) {
      alert('El correo, nombre completo y contraseña son obligatorios.');
      return;
    }
    if (this.newUser.password.trim().length < 6) {
      alert('La contraseña debe tener al menos 6 caracteres.');
      return;
    }

    this.apiService.post<UserDto>('auth/users', {
      email: this.newUser.email.trim(),
      fullName: this.newUser.fullName.trim(),
      role: this.newUser.role,
      password: this.newUser.password.trim()
    }).subscribe({
      next: (res) => {
        this.users = [...this.users, res];
        this.displayDialog = false;
      },
      error: (err) => {
        console.error('Error al crear usuario:', err);
        alert(err.error?.message || 'No se pudo crear el usuario.');
      }
    });
  }

  deleteUser(user: UserDto) {
    if (user.email === this.authService.userDbProfile()?.email) {
      alert('No puedes eliminar tu propia cuenta de administrador.');
      return;
    }

    if (confirm(`¿Estás seguro de que deseas eliminar permanentemente a ${user.fullName} de MaquiLease y Firebase?`)) {
      this.apiService.delete<any>(`auth/users/${user.userId}`).subscribe({
        next: (res) => {
          this.users = this.users.filter(u => u.userId !== user.userId);
          alert('Usuario eliminado correctamente.');
        },
        error: (err) => {
          console.error('Error al eliminar usuario:', err);
          alert(err.error?.message || 'No se pudo eliminar al usuario.');
        }
      });
    }
  }
}
