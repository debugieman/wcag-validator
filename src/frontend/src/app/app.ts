import { Component, inject, signal } from '@angular/core';
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
  message = signal('');
  messageType = signal<'success' | 'error'>('success');
  isLoading = signal(false);

  private urlPattern = /^https?:\/\/([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(\/.*)?$/;

  isValidUrl(): boolean {
    return this.urlPattern.test(this.url().trim());
  }

  onAnalyze() {
    const value = this.url().trim();

    if (!value) {
      this.messageType.set('error');
      this.message.set('Please enter a URL');
      return;
    }

    if (!this.isValidUrl()) {
      this.messageType.set('error');
      this.message.set('Invalid URL format. Use: https://example.com');
      return;
    }

    this.isLoading.set(true);
    this.message.set('');

    this.http.post<any>('https://localhost:7268/api/analysis', { url: value })
      .subscribe({
        next: (res) => {
          this.messageType.set('success');
          this.message.set(`URL saved! Analysis ID: ${res.id}`);
          this.isLoading.set(false);
        },
        error: () => {
          this.messageType.set('error');
          this.message.set('Error: Could not connect to API');
          this.isLoading.set(false);
        }
      });
  }
}
