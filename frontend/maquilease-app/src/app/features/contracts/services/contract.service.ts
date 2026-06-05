import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';
import { Contract, CreateContractDto } from '../models/contract.interface';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ContractService {
  private api = inject(ApiService);
  private http = inject(HttpClient);
  private endpoint = 'contracts';

  getContracts(): Observable<Contract[]> {
    return this.api.get<Contract[]>(this.endpoint);
  }

  getContract(id: number): Observable<Contract> {
    return this.api.get<Contract>(`${this.endpoint}/${id}`);
  }

  createContract(contract: CreateContractDto): Observable<any> {
    return this.api.post<any>(this.endpoint, contract);
  }

  downloadContractPdf(contractId: number): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/${this.endpoint}/${contractId}/pdf`, {
      responseType: 'blob'
    });
  }
}
