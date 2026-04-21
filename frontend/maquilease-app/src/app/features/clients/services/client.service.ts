import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Client, CreateClientDto } from '../models/client.interface';

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private api = inject(ApiService);
  private endpoint = 'clients';

  getClients(): Observable<Client[]> {
    return this.api.get<Client[]>(this.endpoint);
  }

  getClient(id: number): Observable<Client> {
    return this.api.get<Client>(`${this.endpoint}/${id}`);
  }

  createClient(client: CreateClientDto): Observable<Client> {
    return this.api.post<Client>(this.endpoint, client);
  }

  updateClient(id: number, client: CreateClientDto): Observable<void> {
    return this.api.put<void>(`${this.endpoint}/${id}`, client);
  }

  deleteClient(id: number): Observable<void> {
    return this.api.delete<void>(`${this.endpoint}/${id}`);
  }
}
