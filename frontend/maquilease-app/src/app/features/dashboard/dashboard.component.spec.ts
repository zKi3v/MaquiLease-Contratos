import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { DashboardService } from '../../core/services/dashboard.service';
import { ThemeService } from '../../core/services/theme.service';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;

  const mockDashboardService = {
    getKpis: () => of({
      totalAssets: 25,
      activeContracts: 12,
      totalExpectedRevenue: 150000,
      totalCollectedRevenue: 140000,
      defaultRatePercentage: 1.5
    }),
    getAssetStatus: () => of({
      available: 10,
      rented: 12,
      maintenance: 3
    }),
    getRevenueForecast: () => of([
      { month: 'Ene', realRevenue: 10000, predictedRevenue: 11000 },
      { month: 'Feb', realRevenue: 12000, predictedRevenue: 12500 }
    ]),
    getOverdueRate: () => of([
      { month: 'Ene', overdueRate: 1.2 },
      { month: 'Feb', overdueRate: 1.8 }
    ]),
    getContractDistribution: () => of({
      byStatus: { 'activo': 10, 'finalizado': 2 },
      byType: { 'Arrendamiento': 8, 'Servicios': 4 }
    }),
    getClientSegments: () => of({
      clients: [],
      segmentCounts: { 'Premium': 5, 'Crecimiento': 4, 'En Riesgo': 2, 'Problemático': 1 }
    })
  };

  const mockThemeService = {
    isDarkMode: jasmine.createSpy('isDarkMode').and.returnValue(false)
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        { provide: DashboardService, useValue: mockDashboardService },
        { provide: ThemeService, useValue: mockThemeService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load dashboard data correctly', () => {
    expect(component).toBeTruthy();
    expect(component.kpis).toBeDefined();
    expect(component.kpis?.totalAssets).toEqual(25);
    expect(component.kpis?.activeContracts).toEqual(12);
  });

  it('should initialize chart data sets on init', () => {
    expect(component.lineChartData).toBeDefined();
    expect(component.doughnutChartData).toBeDefined();
    expect(component.overdueBarChartData).toBeDefined();
    expect(component.contractPieChartData).toBeDefined();
    expect(component.segmentBarChartData).toBeDefined();
  });
});

