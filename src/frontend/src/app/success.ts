import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-success',
  imports: [RouterLink],
  template: `
    <div class="success-page">
      <div class="success-container">
        <div class="success-icon">✓</div>
        <h1>Payment successful</h1>
        <p class="success-lead">
          We're scanning your website now. Your accessibility report will be
          sent to <strong>{{ email() }}</strong> within a few minutes.
        </p>
        <p class="success-note">
          Don't see it? Check your spam folder or contact us at
          <a href="mailto:hello@wcag-analyzer.com">hello@wcag-analyzer.com</a>.
        </p>
        <a class="success-btn" routerLink="/">Scan another website</a>
      </div>
    </div>
  `,
  styleUrl: './app.scss'
})
export class Success implements OnInit {
  email = signal('');

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    const e = this.route.snapshot.queryParamMap.get('email');
    if (e) this.email.set(e);
  }
}
