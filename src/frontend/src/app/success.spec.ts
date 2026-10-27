import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Success } from './success';

describe('Success', () => {
  let fixture: ComponentFixture<Success>;
  let component: Success;
  let httpTesting: HttpTestingController;

  function createComponent(email: string | null) {
    TestBed.configureTestingModule({
      imports: [Success],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(email !== null ? { email } : {}) }
          }
        }
      ]
    });
    fixture = TestBed.createComponent(Success);
    component = fixture.componentInstance;
    httpTesting = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    fixture.destroy();
  });

  it('should render without an email query param', () => {
    createComponent(null);
    fixture.detectChanges();

    expect(component.email()).toBe('');
    expect(component.summary()).toBeNull();
  });

  it('should fetch the summary for the email in the query param on init', () => {
    createComponent('user@example.com');
    fixture.detectChanges();

    const req = httpTesting.expectOne('/api/analysis/summary?email=user%40example.com');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'abc-123', status: 'Pending', score: 0, critical: 0, serious: 0, moderate: 0, minor: 0 });

    expect(component.summary()?.status).toBe('Pending');
  });

  it('should fetch violation details once the analysis is completed', () => {
    createComponent('user@example.com');
    fixture.detectChanges();

    httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
      .flush({ id: 'abc-123', status: 'Completed', score: 90, critical: 0, serious: 0, moderate: 0, minor: 1 });

    const detailReq = httpTesting.expectOne('/api/analysis/abc-123');
    expect(detailReq.request.method).toBe('GET');
    detailReq.flush({ results: [] });
  });

  describe('scoreClass / scoreLabel', () => {
    const cases: Array<[number, string, string]> = [
      [100, 'good', 'Good'],
      [80, 'good', 'Good'],
      [79, 'average', 'Needs Improvement'],
      [50, 'average', 'Needs Improvement'],
      [49, 'poor', 'Poor'],
      [0, 'poor', 'Poor'],
    ];

    for (const [score, expectedClass, expectedLabel] of cases) {
      it(`should classify score ${score} as ${expectedClass}/${expectedLabel}`, () => {
        createComponent('user@example.com');
        fixture.detectChanges();

        httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
          .flush({ id: 'abc-123', status: 'Completed', score, critical: 0, serious: 0, moderate: 0, minor: 0 });
        httpTesting.expectOne('/api/analysis/abc-123').flush({ results: [] });

        expect(component.scoreClass()).toBe(expectedClass);
        expect(component.scoreLabel()).toBe(expectedLabel);
      });
    }
  });

  describe('noViolations', () => {
    it('should be true when all impact counts are zero', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 100, critical: 0, serious: 0, moderate: 0, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({ results: [] });

      expect(component.noViolations()).toBe(true);
    });

    it('should be false when any impact count is non-zero', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 70, critical: 0, serious: 0, moderate: 1, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({ results: [] });

      expect(component.noViolations()).toBe(false);
    });
  });

  describe('criterionGroups', () => {
    it('should be empty when there are no violations', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 100, critical: 0, serious: 0, moderate: 0, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({ results: [] });

      expect(component.criterionGroups()).toEqual([]);
    });

    it('should deduplicate repeated rule violations and count instances', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 60, critical: 1, serious: 0, moderate: 0, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({
        results: [
          { ruleId: 'color-contrast', impact: 'critical', description: 'desc', htmlElement: null },
          { ruleId: 'color-contrast', impact: 'critical', description: 'desc', htmlElement: null },
        ]
      });

      const groups = component.criterionGroups();
      expect(groups).toHaveLength(1);
      expect(groups[0].rules).toHaveLength(1);
      expect(groups[0].rules[0].count).toBe(2);
    });

    it('should group violations by WCAG criterion and use the known friendly name', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 60, critical: 1, serious: 1, moderate: 0, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({
        results: [
          { ruleId: 'color-contrast', impact: 'critical', description: 'desc', htmlElement: null },
          { ruleId: 'image-alt', impact: 'serious', description: 'desc', htmlElement: null },
        ]
      });

      const groups = component.criterionGroups();
      const contrastGroup = groups.find(g => g.criterion === '1.4.3');
      expect(contrastGroup).toBeDefined();
      expect(contrastGroup!.rules[0].name).toBe('Low Color Contrast');
    });

    it('should fall back to a slugified rule id for unknown rules', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 60, critical: 0, serious: 0, moderate: 1, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({
        results: [
          { ruleId: 'some-unmapped-rule', impact: 'moderate', description: 'desc', htmlElement: null },
        ]
      });

      const groups = component.criterionGroups();
      expect(groups).toHaveLength(1);
      expect(groups[0].criterion).toBe('Other');
      expect(groups[0].rules[0].name).toBe('some unmapped rule');
    });

    it('should order groups by worst impact, most severe first', () => {
      createComponent('user@example.com');
      fixture.detectChanges();

      httpTesting.expectOne('/api/analysis/summary?email=user%40example.com')
        .flush({ id: 'abc-123', status: 'Completed', score: 40, critical: 1, serious: 0, moderate: 1, minor: 0 });
      httpTesting.expectOne('/api/analysis/abc-123').flush({
        results: [
          { ruleId: 'table-missing-caption', impact: 'moderate', description: 'desc', htmlElement: null },
          { ruleId: 'color-contrast', impact: 'critical', description: 'desc', htmlElement: null },
        ]
      });

      const groups = component.criterionGroups();
      expect(groups[0].worstImpact).toBe('critical');
      expect(groups[1].worstImpact).toBe('moderate');
    });
  });
});
