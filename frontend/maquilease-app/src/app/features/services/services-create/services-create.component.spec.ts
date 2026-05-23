import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { ServicesCreateComponent } from './services-create.component';
import { ServiceCatalogService } from '../../services-catalog/services/service-catalog.service';
import { CatalogService } from '../../../core/services/catalog.service';

describe('ServicesCreateComponent', () => {
  let component: ServicesCreateComponent;
  let fixture: ComponentFixture<ServicesCreateComponent>;
  let mockRouter: any;
  let mockActivatedRoute: any;
  let mockServiceCatalogService: any;
  let mockCatalogService: any;

  beforeEach(async () => {
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        }
      }
    };
    mockServiceCatalogService = jasmine.createSpyObj('ServiceCatalogService', ['createService', 'updateService']);
    mockServiceCatalogService.createService.and.returnValue(of({}));
    mockServiceCatalogService.updateService.and.returnValue(of({}));

    mockCatalogService = jasmine.createSpyObj('CatalogService', ['getServiceCategories']);
    mockCatalogService.getServiceCategories.and.returnValue(of([
      { categoryId: 1, name: 'Mantenimiento Preventivo' },
      { categoryId: 2, name: 'Reparaciones Mecánicas' }
    ]));

    await TestBed.configureTestingModule({
      imports: [ServicesCreateComponent],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: ServiceCatalogService, useValue: mockServiceCatalogService },
        { provide: CatalogService, useValue: mockCatalogService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ServicesCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load categories on init', () => {
    expect(component).toBeTruthy();
    expect(component.categories).toContain('Mantenimiento Preventivo');
    expect(component.categories).toContain('Reparaciones Mecánicas');
  });

  it('should enable or disable save button based on Code and Name validation', () => {
    // Both empty: invalid
    component.serviceForm.code = '';
    component.serviceForm.name = '';
    fixture.detectChanges();
    expect(!component.serviceForm.code || !component.serviceForm.name).toBeTrue();

    // Code provided but name empty: invalid
    component.serviceForm.code = 'SVC-001';
    component.serviceForm.name = '';
    fixture.detectChanges();
    expect(!component.serviceForm.code || !component.serviceForm.name).toBeTrue();

    // Both provided: valid
    component.serviceForm.code = 'SVC-001';
    component.serviceForm.name = 'Inspección de Motor';
    fixture.detectChanges();
    expect(!component.serviceForm.code || !component.serviceForm.name).toBeFalse();
  });

  it('should handle numeric validations for base price inputs', () => {
    component.serviceForm.basePrice = 250;
    fixture.detectChanges();
    expect(component.serviceForm.basePrice).toBeGreaterThan(0);
  });

  it('should invoke createService and navigate on save when creating a service', () => {
    component.isEdit = false;
    component.serviceForm.code = 'SVC-001';
    component.serviceForm.name = 'Inspección de Motor';
    
    component.saveService();

    expect(mockServiceCatalogService.createService).toHaveBeenCalledWith(component.serviceForm);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/services']);
  });
});

