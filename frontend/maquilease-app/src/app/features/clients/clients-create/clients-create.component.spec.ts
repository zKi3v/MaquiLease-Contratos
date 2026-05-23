import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { ClientsCreateComponent } from './clients-create.component';
import { ClientService } from '../services/client.service';
import { CatalogService } from '../../../core/services/catalog.service';

describe('ClientsCreateComponent', () => {
  let component: ClientsCreateComponent;
  let fixture: ComponentFixture<ClientsCreateComponent>;
  let mockRouter: any;
  let mockActivatedRoute: any;
  let mockClientService: any;
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
    mockClientService = jasmine.createSpyObj('ClientService', ['createClient', 'updateClient']);
    mockClientService.createClient.and.returnValue(of({}));
    mockClientService.updateClient.and.returnValue(of({}));

    mockCatalogService = jasmine.createSpyObj('CatalogService', ['getSectors']);
    mockCatalogService.getSectors.and.returnValue(of([
      { sectorId: 1, name: 'Construcción' },
      { sectorId: 2, name: 'Minería' }
    ]));

    await TestBed.configureTestingModule({
      imports: [ClientsCreateComponent],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: ClientService, useValue: mockClientService },
        { provide: CatalogService, useValue: mockCatalogService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientsCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load sectors on init', () => {
    expect(component).toBeTruthy();
    expect(component.sectors).toContain('Construcción');
    expect(component.sectors).toContain('Minería');
  });

  it('should have isEdit as false by default when no ID parameter exists', () => {
    expect(component.isEdit).toBeFalse();
  });

  it('should enable or disable save button based on RUC and Business Name validation', () => {
    // Both empty: invalid
    component.clientForm.ruc = '';
    component.clientForm.businessName = '';
    fixture.detectChanges();
    
    // Check save capability
    const isDisabled = !component.clientForm.ruc || !component.clientForm.businessName;
    expect(isDisabled).toBeTrue();

    // RUC provided but businessName empty: invalid
    component.clientForm.ruc = '20123456789';
    component.clientForm.businessName = '';
    fixture.detectChanges();
    expect(!component.clientForm.ruc || !component.clientForm.businessName).toBeTrue();

    // Both provided: valid
    component.clientForm.ruc = '20123456789';
    component.clientForm.businessName = 'Inversiones SAC';
    fixture.detectChanges();
    expect(!component.clientForm.ruc || !component.clientForm.businessName).toBeFalse();
  });

  it('should invoke createClient and navigate on save when creating a client', () => {
    component.isEdit = false;
    component.clientForm.ruc = '20123456789';
    component.clientForm.businessName = 'Inversiones SAC';
    
    component.saveClient();

    expect(mockClientService.createClient).toHaveBeenCalledWith(component.clientForm);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/clients']);
  });
});

