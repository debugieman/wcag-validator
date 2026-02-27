import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private http = inject(HttpClient);

  url = signal('');
  email = signal('');
  message = signal('');
  messageType = signal<'success' | 'error'>('success');
  isLoading = signal(false);

  // Only allow https://, block http://
  private urlPattern = /^https:\/\/([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(\/.*)?$/;
  private emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

  // Block private/internal IP ranges and localhost (SSRF protection)
  private blockedHosts = [
    /^localhost$/i,
    /^127\.\d+\.\d+\.\d+$/,
    /^10\.\d+\.\d+\.\d+$/,
    /^172\.(1[6-9]|2\d|3[01])\.\d+\.\d+$/,
    /^192\.168\.\d+\.\d+$/,
    /^\[?::1\]?$/,
    /^0\.0\.0\.0$/
  ];

  isValidUrl(): boolean {
    const trimmed = this.url().trim();
    if (!this.urlPattern.test(trimmed)) return false;

    try {
      const hostname = new URL(trimmed).hostname;
      return !this.blockedHosts.some(pattern => pattern.test(hostname));
    } catch {
      return false;
    }
  }

  isValidEmail(): boolean {
    return this.emailPattern.test(this.email().trim());
  }

  isFormReady = computed(() =>
    this.url().trim().length > 0 && this.email().trim().length > 0
  );

  private normalizeUrl(url: string): string {
    try {
      const parsed = new URL(url.trim());
      // Remove trailing slash from pathname unless it's just "/"
      if (parsed.pathname.length > 1 && parsed.pathname.endsWith('/')) {
        parsed.pathname = parsed.pathname.replace(/\/+$/, '');
      }
      return parsed.toString();
    } catch {
      return url.trim();
    }
  }

  onAnalyze() {
    const emailValue = this.email().trim();

    if (!emailValue) {
      this.messageType.set('error');
      this.message.set('Please enter an email address');
      return;
    }

    if (!this.isValidEmail()) {
      this.messageType.set('error');
      this.message.set('Invalid email format. Use: your@email.com');
      return;
    }

    const value = this.url().trim();

    if (!value) {
      this.messageType.set('error');
      this.message.set('Please enter a URL');
      return;
    }

    if (!value.startsWith('https://')) {
      this.messageType.set('error');
      this.message.set('Only HTTPS URLs are supported. Use: https://example.com');
      return;
    }

    if (!this.isValidUrl()) {
      this.messageType.set('error');
      this.message.set('Invalid or unsupported URL. Use a public HTTPS address.');
      return;
    }

    const normalizedUrl = this.normalizeUrl(value);

    this.isLoading.set(true);
    this.message.set('');

    this.http.post<any>('/api/analysis', { url: normalizedUrl, email: emailValue })
      .subscribe({
        next: (res) => {
          this.messageType.set('success');
          this.message.set(`Analysis submitted! You will receive the report at ${emailValue}`);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.messageType.set('error');

          if (err.status === 429) {
            this.message.set('This domain was already analyzed in the last 24 hours. Please try again later.');
          } else {
            this.message.set('Something went wrong. Please try again.');
          }
        }
      });
  }
}
