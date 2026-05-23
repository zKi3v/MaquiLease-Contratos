import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { ApiService } from './api.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let mockApiService: any;

  beforeEach(() => {
    mockApiService = jasmine.createSpyObj('ApiService', ['get']);
    
    TestBed.configureTestingModule({
      providers: [
        DashboardService,
        { provide: ApiService, useValue: mockApiService }
      ]
    });
    service = TestBed.inject(DashboardService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call api.get with "dashboard/kpis" on getKpis()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getKpis().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/kpis');
  });

  it('should call api.get with "dashboard/revenue-forecast" on getRevenueForecast()', () => {
    mockApiService.get.and.returnValue(of([]));
    service.getRevenueForecast().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/revenue-forecast');
  });

  it('should call api.get with "dashboard/asset-status" on getAssetStatus()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getAssetStatus().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/asset-status');
  });

  it('should call api.get with "dashboard/overdue-rate" on getOverdueRate()', () => {
    mockApiService.get.and.returnValue(of([]));
    service.getOverdueRate().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/overdue-rate');
  });

  it('should call api.get with "dashboard/contract-distribution" on getContractDistribution()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getContractDistribution().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/contract-distribution');
  });

  it('should call api.get with "dashboard/client-segments" on getClientSegments()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getClientSegments().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('dashboard/client-segments');
  });
});
