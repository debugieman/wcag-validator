import { Component, inject, signal, computed, AfterViewInit, ElementRef, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements AfterViewInit {
  private http = inject(HttpClient);
  private el = inject(ElementRef);
  private zone = inject(NgZone);

  url = signal('');
  email = signal('');
  scanMode = signal<'quick' | 'deep'>('quick');
  message = signal('');
  messageType = signal<'success' | 'error'>('success');
  isLoading = signal(false);

  private urlPattern = /^https:\/\/([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(\/.*)?$/;
  private emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

  private blockedHosts = [
    /^localhost$/i,
    /^127\.\d+\.\d+\.\d+$/,
    /^10\.\d+\.\d+\.\d+$/,
    /^172\.(1[6-9]|2\d|3[01])\.\d+\.\d+$/,
    /^192\.168\.\d+\.\d+$/,
    /^\[?::1\]?$/,
    /^0\.0\.0\.0$/
  ];

  ngAfterViewInit() {
    this.zone.runOutsideAngular(() => {
      this.initFadeInObserver();
      this.initCounterObserver();
    });
  }

  private initFadeInObserver() {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12 }
    );

    this.el.nativeElement.querySelectorAll('.fade-in').forEach((el: Element) => {
      observer.observe(el);
    });
  }

  private initCounterObserver() {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            const el = entry.target as HTMLElement;
            const target = Number(el.dataset['target']);
            const suffix = el.dataset['suffix'] ?? '';
            const prefix = el.dataset['prefix'] ?? '';
            this.animateCounter(el, target, prefix, suffix);
            observer.unobserve(el);
          }
        });
      },
      { threshold: 0.5 }
    );

    this.el.nativeElement.querySelectorAll('.counter').forEach((el: Element) => {
      observer.observe(el);
    });
  }

  private animateCounter(el: HTMLElement, target: number, prefix: string, suffix: string) {
    const duration = 1600;
    const start = performance.now();

    const tick = (now: number) => {
      const elapsed = now - start;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      const current = Math.round(eased * target);
      el.textContent = prefix + current.toLocaleString() + suffix;

      if (progress < 1) requestAnimationFrame(tick);
    };

    requestAnimationFrame(tick);
  }

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
      if (parsed.pathname.length > 1 && parsed.pathname.endsWith('/')) {
        parsed.pathname = parsed.pathname.replace(/\/+$/, '');
      }
      return parsed.toString();
    } catch {
      return url.trim();
    }
  }

  scrollToAnalyzer() {
    document.getElementById('analyzer')?.scrollIntoView({ behavior: 'smooth' });
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

    this.http.post<any>('/api/analysis', { url: normalizedUrl, email: emailValue, deepScan: this.scanMode() === 'deep' })
      .subscribe({
        next: () => {
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
