import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

interface AnalysisSummary {
  status: string;
  score: number;
  critical: number;
  serious: number;
  moderate: number;
  minor: number;
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
        <div class="success-icon-wrap">
          <div class="success-icon">✓</div>
        </div>

        <h1>Payment confirmed</h1>
        <p class="success-lead">
          Your report is being prepared and will be sent to <strong>{{ email() }}</strong>.
        </p>

        @if (summary() && summary()!.status === 'Completed') {
          <div class="score-card">
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
      .subscribe({ next: s => this.summary.set(s), error: () => {} });
  }
}
