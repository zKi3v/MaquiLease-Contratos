import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ClientService } from '../services/client.service';
import { Client } from '../models/client.interface';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-clients-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, InputTextModule, RouterModule],
  templateUrl: './clients-list.component.html'
})
export class ClientsListComponent implements OnInit {
  clients: Client[] = [];
  loading: boolean = false;
  
  clientsService = inject(ClientService);
  router = inject(Router);

  ngOnInit() {
    this.loadClients();
  }

  loadClients() {
    this.loading = true;
    this.clientsService.getClients().subscribe({
      next: (data) => {
        this.clients = data;
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  goToEdit(client: Client) {
    this.router.navigate(['/clients/edit', client.clientId], { state: { client } });
  }

  deleteClient(client: Client) {
    if(confirm(`¿Estás seguro de eliminar a ${client.businessName}?`)) {
      this.clientsService.deleteClient(client.clientId).subscribe(() => {
        this.loadClients();
      });
    }
  }
}
