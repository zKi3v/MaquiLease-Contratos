import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Contract, CreateContractDto } from '../models/contract.interface';

@Injectable({
  providedIn: 'root'
})
export class ContractService {
  private api = inject(ApiService);
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
}
