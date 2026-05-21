import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface CatalogItem {
  name: string;
  label: string;
}

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private api = inject(ApiService);
  private endpoint = 'catalogs';

  getSectors(): Observable<CatalogItem[]> {
    return this.api.get<CatalogItem[]>(`${this.endpoint}/sectors`);
  }

  getAssetCategories(): Observable<CatalogItem[]> {
    return this.api.get<CatalogItem[]>(`${this.endpoint}/categories-assets`);
  }

  getAssetBrands(): Observable<CatalogItem[]> {
    return this.api.get<CatalogItem[]>(`${this.endpoint}/brands-assets`);
  }

  getServiceCategories(): Observable<CatalogItem[]> {
    return this.api.get<CatalogItem[]>(`${this.endpoint}/categories-services`);
  }
}
