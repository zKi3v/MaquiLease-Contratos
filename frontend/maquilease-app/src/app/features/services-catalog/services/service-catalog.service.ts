import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ServiceObj, CreateServiceDto } from '../models/service.interface';

@Injectable({
  providedIn: 'root'
})
export class ServiceCatalogService {
  private api = inject(ApiService);
  private endpoint = 'services';

  getServices(): Observable<ServiceObj[]> {
    return this.api.get<ServiceObj[]>(this.endpoint);
  }

  getService(id: number): Observable<ServiceObj> {
    return this.api.get<ServiceObj>(`${this.endpoint}/${id}`);
  }

  createService(service: CreateServiceDto): Observable<ServiceObj> {
    return this.api.post<ServiceObj>(this.endpoint, service);
  }

  updateService(id: number, service: CreateServiceDto): Observable<void> {
    return this.api.put<void>(`${this.endpoint}/${id}`, service);
  }

  deleteService(id: number): Observable<void> {
    return this.api.delete<void>(`${this.endpoint}/${id}`);
  }
}
