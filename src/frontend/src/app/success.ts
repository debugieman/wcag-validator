import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

const RULE_NAMES: Record<string, string> = {
  'color-contrast':                   'Low Color Contrast',
  'image-alt':                        'Missing Image Alt Text',
  'svg-image-missing-alt':            'Missing SVG Alt Text',
  'button-name':                      'Unlabelled Button',
  'link-name':                        'Unlabelled Link',
  'input-missing-label':              'Form Field Without Label',
  'select-textarea-missing-label':    'Dropdown / Text Area Without Label',
  'label':                            'Missing Form Label',
  'html-has-lang':                    'Missing Page Language',
  'heading-level-skipped':            'Skipped Heading Level',
  'heading-first-not-h1':             'First Heading Is Not H1',
  'skip-navigation-missing':          'Missing Skip Navigation Link',
  'landmark-one-main':                'Missing Main Landmark',
  'landmark-unique':                  'Duplicate Landmark Regions',
  'region':                           'Content Outside Landmark Regions',
  'list':                             'Incorrect List Structure',
  'table-missing-caption':            'Table Without Caption',
  'aria-allowed-role':                'Invalid ARIA Role',
  'focus-visible-missing':            'Invisible Keyboard Focus',
  'interactive-not-focusable':        'Element Not Keyboard Accessible',
  'keyboard-trap':                    'Keyboard Focus Trap',
  'reflow-horizontal-scroll':         'Horizontal Scroll at Small Viewport',
  'touch-target-too-small':           'Touch Target Too Small',
  'animation-reduced-motion-missing': 'Animation Ignores Reduced Motion',
};

const IMPACT_ORDER = ['critical', 'serious', 'moderate', 'minor'];

interface AnalysisSummary {
  id: string;
  status: string;
  score: number;
  critical: number;
  serious: number;
  moderate: number;
  minor: number;
}

interface Violation {
  ruleId: string;
  impact: string;
  description: string;
  htmlElement: string | null;
}

interface ViolationGroup {
  impact: string;
  label: string;
  items: Violation[];
}

@Component({
  selector: 'app-success',
  imports: [RouterLink],
  template: `
    <div class="success-page">
      <div class="success-header">
        <img src="/logo.svg" alt="WCAG Analyzer" class="success-logo" />
      </div>

      <div class="success-container">

        <!-- Animated checkmark -->
        <div class="success-icon-wrap">
          <svg class="success-checkmark" viewBox="0 0 52 52" xmlns="http://www.w3.org/2000/svg">
            <circle class="checkmark-circle" cx="26" cy="26" r="24" fill="none"/>
            <polyline class="checkmark-tick" points="14,27 22,35 38,19"/>
          </svg>
        </div>

        <h1>Payment successful</h1>
        <p class="success-lead">
          Your report for <strong>{{ email() }}</strong> is on its way.
        </p>

        <!-- 3-step progress -->
        <div class="success-steps">
          <div class="step done">
            <div class="step-dot"></div>
            <div class="step-label">Payment confirmed</div>
          </div>
          <div class="step-line" [class.done]="summary() !== null"></div>
          <div class="step"
               [class.done]="summary()?.status === 'Completed'"
               [class.active]="summary() !== null && summary()!.status !== 'Completed'">
            <div class="step-dot"></div>
            <div class="step-label">Scanning website</div>
          </div>
          <div class="step-line" [class.done]="summary()?.status === 'Completed'"></div>
          <div class="step" [class.done]="summary()?.status === 'Completed'">
            <div class="step-dot"></div>
            <div class="step-label">Report sent</div>
          </div>
        </div>

        <!-- Score card + violations (revealed when done) -->
        @if (summary() && summary()!.status === 'Completed') {
          <div class="score-card score-card--reveal">
            <div class="score-number" [class]="scoreClass()">{{ summary()!.score }}</div>
            <div class="score-label-text">out of 100</div>
            <div class="score-bar-wrap">
              <div class="score-bar-fill" [class]="scoreClass()" [style.width.%]="summary()!.score"></div>
            </div>
            <div class="score-tag" [class]="scoreClass()">{{ scoreLabel() }}</div>
            <div class="impact-breakdown">
              @if (summary()!.critical > 0) {
                <span class="impact-chip critical">{{ summary()!.critical }} Critical</span>
              }
              @if (summary()!.serious > 0) {
                <span class="impact-chip serious">{{ summary()!.serious }} Serious</span>
              }
              @if (summary()!.moderate > 0) {
                <span class="impact-chip moderate">{{ summary()!.moderate }} Moderate</span>
              }
              @if (summary()!.minor > 0) {
                <span class="impact-chip minor">{{ summary()!.minor }} Minor</span>
              }
              @if (noViolations()) {
                <span class="impact-chip good">No violations found</span>
              }
            </div>
            <p class="score-report-note">Full report with all details sent to your email.</p>
          </div>

          <!-- Violations list -->
          @if (violationGroups().length > 0) {
            <div class="violations-preview">
              <h2 class="violations-heading">Issues found on your site</h2>
              @for (group of violationGroups(); track group.impact) {
                <div class="violation-group">
                  <div class="violation-group-header">
                    <span class="impact-chip {{ group.impact }}">{{ group.label }}</span>
                    <span class="violation-group-count">{{ group.items.length }} issue{{ group.items.length !== 1 ? 's' : '' }}</span>
                  </div>
                  @for (v of group.items; track v.ruleId) {
                    <div class="violation-item">
                      <div class="violation-item-top">
                        <span class="violation-rule-name">{{ ruleName(v.ruleId) }}</span>
                      </div>
                      <p class="violation-description">{{ v.description }}</p>
                      @if (v.htmlElement) {
                        <code class="violation-html">{{ v.htmlElement }}</code>
                      }
                    </div>
                  }
                </div>
              }
              <p class="violations-pdf-note">
                Full technical details and fix guidance are in your PDF report.
              </p>
            </div>
          }

        } @else {
          <div class="analyzing-state">
            <div class="analyzing-steps">
              <div class="analyzing-step step-1">
                <div class="step-dot"></div>
                <span>Launching browser</span>
              </div>
              <div class="analyzing-step step-2">
                <div class="step-dot"></div>
                <span>Running 100+ accessibility checks</span>
              </div>
              <div class="analyzing-step step-3">
                <div class="step-dot"></div>
                <span>Generating your PDF report</span>
              </div>
              <div class="analyzing-step step-4">
                <div class="step-dot"></div>
                <span>Sending to your inbox</span>
              </div>
            </div>
            <p class="analyzing-eta">Usually ready in 1–3 minutes.</p>
          </div>
        }

        <p class="success-note">
          Don't see the email? Check your spam folder or contact us at
          <a href="mailto:hello@wcag-analyzer.com">hello@wcag-analyzer.com</a>.
        </p>
        <a class="success-btn" routerLink="/">Scan another website</a>
      </div>
    </div>
  `,
  styleUrl: './app.scss'
})
export class Success implements OnInit, OnDestroy {
  email   = signal('');
  summary = signal<AnalysisSummary | null>(null);
  private violations = signal<Violation[]>([]);

  private pollInterval: ReturnType<typeof setInterval> | null = null;

  scoreClass = computed(() => {
    const s = this.summary()?.score ?? 0;
    return s >= 80 ? 'good' : s >= 50 ? 'average' : 'poor';
  });

  scoreLabel = computed(() => {
    const s = this.summary()?.score ?? 0;
    return s >= 80 ? 'Good' : s >= 50 ? 'Needs Improvement' : 'Poor';
  });

  noViolations = computed(() => {
    const sum = this.summary();
    return sum && sum.critical === 0 && sum.serious === 0 && sum.moderate === 0 && sum.minor === 0;
  });

  violationGroups = computed<ViolationGroup[]>(() => {
    const all = this.violations();
    if (all.length === 0) return [];

    const grouped = new Map<string, Violation[]>();
    for (const v of all) {
      if (!grouped.has(v.impact)) grouped.set(v.impact, []);
      grouped.get(v.impact)!.push(v);
    }

    const labelMap: Record<string, string> = {
      critical: 'Critical',
      serious:  'Serious',
      moderate: 'Moderate',
      minor:    'Minor',
    };

    return IMPACT_ORDER
      .filter(impact => grouped.has(impact))
      .map(impact => ({
        impact,
        label: labelMap[impact] ?? impact,
        items: grouped.get(impact)!,
      }));
  });

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit() {
    const e = this.route.snapshot.queryParamMap.get('email');
    if (e) {
      this.email.set(e);
      this.startPolling(e);
    }
  }

  ngOnDestroy() {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  ruleName(ruleId: string): string {
    return RULE_NAMES[ruleId] ?? ruleId.replace(/-/g, ' ');
  }

  private startPolling(email: string) {
    this.fetchSummary(email);
    this.pollInterval = setInterval(() => {
      if (this.summary()?.status === 'Completed') {
        clearInterval(this.pollInterval!);
        return;
      }
      this.fetchSummary(email);
    }, 5000);
  }

  private fetchSummary(email: string) {
    this.http.get<AnalysisSummary>(`/api/analysis/summary?email=${encodeURIComponent(email)}`)
      .subscribe({
        next: s => {
          this.summary.set(s);
          if (s.status === 'Completed' && this.violations().length === 0) {
            this.fetchViolations(s.id);
          }
        },
        error: () => {}
      });
  }

  private fetchViolations(id: string) {
    this.http.get<{ results: Violation[] }>(`/api/analysis/${id}`)
      .subscribe({
        next: detail => {
          const impactRank: Record<string, number> = { critical: 0, serious: 1, moderate: 2, minor: 3 };
          const sorted = [...detail.results].sort(
            (a, b) => (impactRank[a.impact] ?? 4) - (impactRank[b.impact] ?? 4)
          );
          this.violations.set(sorted);
        },
        error: () => {}
      });
  }
}
