import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-privacy',
  imports: [RouterLink],
  template: `
    <div class="legal-page">
      <div class="legal-container">
        <a class="legal-back" routerLink="/">← Back</a>
        <h1>Privacy Policy</h1>
        <p class="legal-date">Last updated: April 2025</p>

        <h2>What we collect</h2>
        <p>When you submit a scan request, we collect your email address and the URL you provide. We use your email solely to deliver your accessibility report.</p>

        <h2>How we use your data</h2>
        <p>Your email address is used to send the PDF report you requested. We do not sell, rent, or share your personal data with third parties for marketing purposes.</p>

        <h2>Data retention</h2>
        <p>Scan results are stored for up to 30 days to allow re-delivery of reports if needed. After that period, results are permanently deleted.</p>

        <h2>Cookies</h2>
        <p>This website does not use tracking cookies or third-party analytics.</p>

        <h2>Contact</h2>
        <p>For any privacy-related questions, contact us at <a href="mailto:hello@wcag-analyzer.com">hello@wcag-analyzer.com</a>.</p>
      </div>
    </div>
  `,
  styleUrl: './app.scss'
})
export class Privacy {}
