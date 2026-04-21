import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Asset, CreateAssetDto } from '../models/asset.interface';

@Injectable({
  providedIn: 'root'
})
export class AssetService {
  private api = inject(ApiService);
  private endpoint = 'assets';

  getAssets(): Observable<Asset[]> {
    return this.api.get<Asset[]>(this.endpoint);
  }

  getAsset(id: number): Observable<Asset> {
    return this.api.get<Asset>(`${this.endpoint}/${id}`);
  }

  createAsset(asset: CreateAssetDto): Observable<Asset> {
    return this.api.post<Asset>(this.endpoint, asset);
  }

  updateAsset(id: number, asset: CreateAssetDto): Observable<void> {
    return this.api.put<void>(`${this.endpoint}/${id}`, asset);
  }
}
