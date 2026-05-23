import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { IntelligenceService } from './intelligence.service';
import { ApiService } from './api.service';

describe('IntelligenceService', () => {
  let service: IntelligenceService;
  let mockApiService: any;

  beforeEach(() => {
    mockApiService = jasmine.createSpyObj('ApiService', ['get', 'post']);
    
    TestBed.configureTestingModule({
      providers: [
        IntelligenceService,
        { provide: ApiService, useValue: mockApiService }
      ]
    });
    service = TestBed.inject(IntelligenceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call api.get with correct path on getRiskScore(clientId)', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getRiskScore(102).subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('intelligence/default-risk/102');
  });

  it('should call api.post with correct path and request body on getPricingRecommendation(req)', () => {
    mockApiService.post.and.returnValue(of({}));
    const req = { durationMonths: 12, assetId: 5 };
    service.getPricingRecommendation(req).subscribe();
    expect(mockApiService.post).toHaveBeenCalledWith('intelligence/pricing-recommendation', req);
  });

  it('should call api.get with "intelligence/revenue-forecast" on getRevenueForecast()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getRevenueForecast().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('intelligence/revenue-forecast');
  });

  it('should call api.get with "intelligence/client-scoring" on getClientScoring()', () => {
    mockApiService.get.and.returnValue(of({}));
    service.getClientScoring().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('intelligence/client-scoring');
  });

  it('should call api.get with "intelligence/asset-health" on getAssetHealth()', () => {
    mockApiService.get.and.returnValue(of([]));
    service.getAssetHealth().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('intelligence/asset-health');
  });

  it('should call api.get with "intelligence/matchmaker" on getMatchmaker()', () => {
    mockApiService.get.and.returnValue(of([]));
    service.getMatchmaker().subscribe();
    expect(mockApiService.get).toHaveBeenCalledWith('intelligence/matchmaker');
  });

  it('should call api.post with correct path and parameters on simulateRisk(req)', () => {
    mockApiService.post.and.returnValue(of({}));
    const req = {
      sector: 'Construcción',
      totalAmount: 100000,
      downPayment: 10000,
      installmentsCount: 24,
      onTimePaymentRate: 95
    };
    service.simulateRisk(req).subscribe();
    expect(mockApiService.post).toHaveBeenCalledWith('intelligence/simulate', req);
  });
});
