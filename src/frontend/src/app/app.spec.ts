import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();
  });

  afterEach(() => {
    httpTesting = TestBed.inject(HttpTestingController);
    httpTesting.verify();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('WCAG Analyzer');
  });

  describe('isValidUrl', () => {
    it('should accept valid http URL', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('http://example.com');
      expect(app.isValidUrl()).toBe(true);
    });

    it('should accept valid https URL', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com');
      expect(app.isValidUrl()).toBe(true);
    });

    it('should accept URL with path', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com/page/subpage');
      expect(app.isValidUrl()).toBe(true);
    });

    it('should accept URL with subdomain', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://www.example.com');
      expect(app.isValidUrl()).toBe(true);
    });

    it('should reject empty string', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('');
      expect(app.isValidUrl()).toBe(false);
    });

    it('should reject URL without protocol', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('example.com');
      expect(app.isValidUrl()).toBe(false);
    });

    it('should reject URL without domain', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://');
      expect(app.isValidUrl()).toBe(false);
    });

    it('should reject random text', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('not a url');
      expect(app.isValidUrl()).toBe(false);
    });
  });

  describe('onAnalyze', () => {
    it('should show error when URL is empty', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('');

      app.onAnalyze();

      expect(app.message()).toBe('Please enter a URL');
      expect(app.messageType()).toBe('error');
    });

    it('should show error when URL format is invalid', () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('invalid-url');

      app.onAnalyze();

      expect(app.message()).toBe('Invalid URL format. Use: https://example.com');
      expect(app.messageType()).toBe('error');
    });

    it('should set loading state and send HTTP POST for valid URL', () => {
      httpTesting = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com');

      app.onAnalyze();

      expect(app.isLoading()).toBe(true);
      expect(app.message()).toBe('');

      const req = httpTesting.expectOne('/api/analysis');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ url: 'https://example.com' });

      req.flush({ id: 'test-id-123' });

      expect(app.isLoading()).toBe(false);
      expect(app.messageType()).toBe('success');
      expect(app.message()).toContain('test-id-123');
    });

    it('should show error message on HTTP failure', () => {
      httpTesting = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com');

      app.onAnalyze();

      const req = httpTesting.expectOne('/api/analysis');
      req.error(new ProgressEvent('error'));

      expect(app.isLoading()).toBe(false);
      expect(app.messageType()).toBe('error');
      expect(app.message()).toBe('Error: Could not connect to API');
    });

    it('should disable button during loading', async () => {
      httpTesting = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com');

      app.onAnalyze();
      fixture.detectChanges();
      await fixture.whenStable();

      const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent?.trim()).toBe('Saving...');

      const req = httpTesting.expectOne('/api/analysis');
      req.flush({ id: 'test-id' });

      fixture.detectChanges();
      await fixture.whenStable();

      expect(button.disabled).toBe(false);
      expect(button.textContent?.trim()).toBe('Analyze');
    });
  });

  describe('message display', () => {
    it('should display success message in template', async () => {
      httpTesting = TestBed.inject(HttpTestingController);
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('https://example.com');

      app.onAnalyze();
      const req = httpTesting.expectOne('/api/analysis');
      req.flush({ id: 'abc-123' });

      fixture.detectChanges();
      await fixture.whenStable();

      const messageEl = fixture.nativeElement.querySelector('.message') as HTMLElement;
      expect(messageEl).toBeTruthy();
      expect(messageEl.textContent).toContain('abc-123');
      expect(messageEl.classList.contains('error')).toBe(false);
    });

    it('should display error message with error class', async () => {
      const fixture = TestBed.createComponent(App);
      const app = fixture.componentInstance;
      app.url.set('');

      app.onAnalyze();
      fixture.detectChanges();
      await fixture.whenStable();

      const messageEl = fixture.nativeElement.querySelector('.message') as HTMLElement;
      expect(messageEl).toBeTruthy();
      expect(messageEl.textContent).toContain('Please enter a URL');
      expect(messageEl.classList.contains('error')).toBe(true);
    });
  });
});
