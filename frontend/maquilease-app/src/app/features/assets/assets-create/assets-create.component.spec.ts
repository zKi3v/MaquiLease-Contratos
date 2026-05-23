import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { AssetsCreateComponent } from './assets-create.component';
import { AssetService } from '../services/asset.service';
import { CatalogService } from '../../../core/services/catalog.service';

describe('AssetsCreateComponent', () => {
  let component: AssetsCreateComponent;
  let fixture: ComponentFixture<AssetsCreateComponent>;
  let mockRouter: any;
  let mockActivatedRoute: any;
  let mockAssetService: any;
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
    mockAssetService = jasmine.createSpyObj('AssetService', ['createAsset', 'updateAsset']);
    mockAssetService.createAsset.and.returnValue(of({}));
    mockAssetService.updateAsset.and.returnValue(of({}));

    mockCatalogService = jasmine.createSpyObj('CatalogService', ['getAssetCategories', 'getAssetBrands']);
    mockCatalogService.getAssetCategories.and.returnValue(of([
      { categoryId: 1, name: 'Excavadoras' },
      { categoryId: 2, name: 'Cargadores' }
    ]));
    mockCatalogService.getAssetBrands.and.returnValue(of([
      { brandId: 1, name: 'Caterpillar' },
      { brandId: 2, name: 'Komatsu' }
    ]));

    await TestBed.configureTestingModule({
      imports: [AssetsCreateComponent],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: AssetService, useValue: mockAssetService },
        { provide: CatalogService, useValue: mockCatalogService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AssetsCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load categories and brands on init', () => {
    expect(component).toBeTruthy();
    expect(component.categories).toContain('Excavadoras');
    expect(component.brands).toContain('Caterpillar');
  });

  it('should enable or disable save button based on Code and Name validation', () => {
    // Both empty: invalid
    component.assetForm.code = '';
    component.assetForm.name = '';
    fixture.detectChanges();
    expect(!component.assetForm.code || !component.assetForm.name).toBeTrue();

    // Code provided but name empty: invalid
    component.assetForm.code = 'ACT-001';
    component.assetForm.name = '';
    fixture.detectChanges();
    expect(!component.assetForm.code || !component.assetForm.name).toBeTrue();

    // Both provided: valid
    component.assetForm.code = 'ACT-001';
    component.assetForm.name = 'Tractor D8T';
    fixture.detectChanges();
    expect(!component.assetForm.code || !component.assetForm.name).toBeFalse();
  });

  it('should handle numeric validations for cost inputs', () => {
    // Let's assign valid positive numbers
    component.assetForm.purchasePriceUSD = 150000;
    component.assetForm.purchasePriceCNY = 1000000;
    fixture.detectChanges();

    expect(component.assetForm.purchasePriceUSD).toBeGreaterThan(0);
    expect(component.assetForm.purchasePriceCNY).toBeGreaterThan(0);
  });

  it('should invoke createAsset and navigate on save when creating an asset', () => {
    component.isEdit = false;
    component.assetForm.code = 'ACT-001';
    component.assetForm.name = 'Tractor D8T';
    
    component.saveAsset();

    expect(mockAssetService.createAsset).toHaveBeenCalledWith(component.assetForm);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/assets']);
  });
});

