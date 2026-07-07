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

const WCAG_RULE_MAP: Record<string, { criterion: string; name: string }> = {
  'color-contrast':                   { criterion: '1.4.3',  name: 'Contrast (Minimum)' },
  'image-alt':                        { criterion: '1.1.1',  name: 'Non-text Content' },
  'svg-image-missing-alt':            { criterion: '1.1.1',  name: 'Non-text Content' },
  'button-name':                      { criterion: '4.1.2',  name: 'Name, Role, Value' },
  'link-name':                        { criterion: '2.4.4',  name: 'Link Purpose' },
  'input-missing-label':              { criterion: '1.3.1',  name: 'Info and Relationships' },
  'select-textarea-missing-label':    { criterion: '1.3.1',  name: 'Info and Relationships' },
  'label':                            { criterion: '1.3.1',  name: 'Info and Relationships' },
  'html-has-lang':                    { criterion: '3.1.1',  name: 'Language of Page' },
  'heading-level-skipped':            { criterion: '1.3.1',  name: 'Info and Relationships' },
  'heading-first-not-h1':             { criterion: '1.3.1',  name: 'Info and Relationships' },
  'skip-navigation-missing':          { criterion: '2.4.1',  name: 'Bypass Blocks' },
  'landmark-one-main':                { criterion: '1.3.6',  name: 'Identify Purpose' },
  'landmark-unique':                  { criterion: '1.3.6',  name: 'Identify Purpose' },
  'region':                           { criterion: '1.3.6',  name: 'Identify Purpose' },
  'list':                             { criterion: '1.3.1',  name: 'Info and Relationships' },
  'table-missing-caption':            { criterion: '1.3.1',  name: 'Info and Relationships' },
  'aria-allowed-role':                { criterion: '4.1.2',  name: 'Name, Role, Value' },
  'focus-visible-missing':            { criterion: '2.4.7',  name: 'Focus Visible' },
  'interactive-not-focusable':        { criterion: '2.1.1',  name: 'Keyboard' },
  'keyboard-trap':                    { criterion: '2.1.2',  name: 'No Keyboard Trap' },
  'reflow-horizontal-scroll':         { criterion: '1.4.10', name: 'Reflow' },
  'touch-target-too-small':           { criterion: '2.5.5',  name: 'Target Size' },
  'animation-reduced-motion-missing': { criterion: '2.3.3',  name: 'Animation from Interactions' },
};

const IMPACT_RANK: Record<string, number> = { critical: 0, serious: 1, moderate: 2, minor: 3 };

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

interface UniqueRule {
  ruleId: string;
  impact: string;
  name: string;
  count: number;
  description: string;
}

interface CriterionGroup {
  criterion: string;
  criterionName: string;
  worstImpact: string;
  rules: UniqueRule[];
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

        <!-- Dashboard + violations (revealed when done) -->
        @if (summary() && summary()!.status === 'Completed') {

          <!-- Score card -->
          <div class="score-card score-card--reveal">
            <div class="score-number" [class]="scoreClass()">{{ summary()!.score }}</div>
            <div class="score-label-text">out of 100</div>
            <div class="score-bar-wrap">
              <div class="score-bar-fill" [class]="scoreClass()" [style.width.%]="summary()!.score"></div>
            </div>
            <div class="score-tag" [class]="scoreClass()">{{ scoreLabel() }}</div>
            <p class="score-report-note">Full report with all details sent to your email.</p>
          </div>

          <!-- Dashboard — impact tiles -->
          @if (!noViolations()) {
            <div class="impact-dashboard">
              @if (summary()!.critical > 0) {
                <div class="impact-tile critical">
                  <span class="impact-tile-count">{{ summary()!.critical }}</span>
                  <span class="impact-tile-label">Critical</span>
                </div>
              }
              @if (summary()!.serious > 0) {
                <div class="impact-tile serious">
                  <span class="impact-tile-count">{{ summary()!.serious }}</span>
                  <span class="impact-tile-label">Serious</span>
                </div>
              }
              @if (summary()!.moderate > 0) {
                <div class="impact-tile moderate">
                  <span class="impact-tile-count">{{ summary()!.moderate }}</span>
                  <span class="impact-tile-label">Moderate</span>
                </div>
              }
              @if (summary()!.minor > 0) {
                <div class="impact-tile minor">
                  <span class="impact-tile-count">{{ summary()!.minor }}</span>
                  <span class="impact-tile-label">Minor</span>
                </div>
              }
            </div>
          } @else {
            <div class="no-violations-banner">
              <span class="no-violations-icon">✓</span>
              No accessibility violations found — great work!
            </div>
          }

          <!-- Violations grouped by WCAG criterion -->
          @if (criterionGroups().length > 0) {
            <div class="violations-preview">
              <h2 class="violations-heading">Issues by WCAG criterion</h2>
              @for (group of criterionGroups(); track group.criterion) {
                <div class="criterion-group">
                  <div class="criterion-group-header">
                    <span class="criterion-badge">{{ group.criterion }}</span>
                    <span class="criterion-name">{{ group.criterionName }}</span>
                    <span class="impact-chip {{ group.worstImpact }} criterion-worst">{{ group.worstImpact }}</span>
                  </div>
                  @for (rule of group.rules; track rule.ruleId) {
                    <div class="violation-item">
                      <div class="violation-item-top">
                        <span class="impact-chip {{ rule.impact }}">{{ rule.impact }}</span>
                        <span class="violation-rule-name">{{ rule.name }}</span>
                        @if (rule.count > 1) {
                          <span class="violation-count">×{{ rule.count }}</span>
                        }
                      </div>
                      <p class="violation-description">{{ rule.description }}</p>
                    </div>
                  }
                </div>
              }
              <p class="violations-pdf-note">
                Full technical details, HTML examples and fix guidance are in your PDF report.
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

  criterionGroups = computed<CriterionGroup[]>(() => {
    const all = this.violations();
    if (all.length === 0) return [];

    // Deduplicate by ruleId, count instances
    const ruleMap = new Map<string, UniqueRule>();
    for (const v of all) {
      if (!ruleMap.has(v.ruleId)) {
        ruleMap.set(v.ruleId, {
          ruleId:      v.ruleId,
          impact:      v.impact,
          name:        RULE_NAMES[v.ruleId] ?? v.ruleId.replace(/-/g, ' '),
          count:       0,
          description: v.description,
        });
      }
      ruleMap.get(v.ruleId)!.count++;
    }

    // Group unique rules by WCAG criterion
    const criterionMap = new Map<string, CriterionGroup>();
    for (const rule of ruleMap.values()) {
      const wcag = WCAG_RULE_MAP[rule.ruleId] ?? { criterion: 'Other', name: 'Other checks' };
      const key  = wcag.criterion;
      if (!criterionMap.has(key)) {
        criterionMap.set(key, {
          criterion:     wcag.criterion,
          criterionName: wcag.name,
          worstImpact:   rule.impact,
          rules:         [],
        });
      }
      const group = criterionMap.get(key)!;
      group.rules.push(rule);
      if ((IMPACT_RANK[rule.impact] ?? 4) < (IMPACT_RANK[group.worstImpact] ?? 4)) {
        group.worstImpact = rule.impact;
      }
    }

    // Sort rules within each group by impact
    for (const g of criterionMap.values()) {
      g.rules.sort((a, b) => (IMPACT_RANK[a.impact] ?? 4) - (IMPACT_RANK[b.impact] ?? 4));
    }

    // Sort groups: worst impact first, then criterion number
    return [...criterionMap.values()].sort((a, b) => {
      const diff = (IMPACT_RANK[a.worstImpact] ?? 4) - (IMPACT_RANK[b.worstImpact] ?? 4);
      return diff !== 0 ? diff : a.criterion.localeCompare(b.criterion, undefined, { numeric: true });
    });
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
        next: detail => this.violations.set(detail.results),
        error: () => {}
      });
  }
}
