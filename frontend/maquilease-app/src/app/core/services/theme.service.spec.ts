import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    // Reset html attribute
    document.documentElement.removeAttribute('data-theme');
    
    TestBed.configureTestingModule({
      providers: [ThemeService]
    });
    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('should be created and default to light mode if nothing is saved', () => {
    expect(service).toBeTruthy();
    expect(service.isDarkMode()).toBeFalse();
    expect(document.documentElement.getAttribute('data-theme')).toEqual('light');
  });

  it('should initialize dark mode if saved as dark in localStorage', () => {
    localStorage.setItem('maquilease-theme', 'dark');
    // Instantiate new service to trigger constructor
    const newService = new ThemeService();
    expect(newService.isDarkMode()).toBeTrue();
    expect(document.documentElement.getAttribute('data-theme')).toEqual('dark');
  });

  it('should toggle theme from light to dark', () => {
    expect(service.isDarkMode()).toBeFalse();
    service.toggleTheme();
    expect(service.isDarkMode()).toBeTrue();
    expect(document.documentElement.getAttribute('data-theme')).toEqual('dark');
    expect(localStorage.getItem('maquilease-theme')).toEqual('dark');
  });

  it('should toggle theme from dark to light', () => {
    // Start dark
    localStorage.setItem('maquilease-theme', 'dark');
    const darkService = new ThemeService();
    expect(darkService.isDarkMode()).toBeTrue();
    
    darkService.toggleTheme();
    expect(darkService.isDarkMode()).toBeFalse();
    expect(document.documentElement.getAttribute('data-theme')).toEqual('light');
    expect(localStorage.getItem('maquilease-theme')).toEqual('light');
  });
});
