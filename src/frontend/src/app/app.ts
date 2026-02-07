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
  isLoading = signal(false);

  onAnalyze() {
    const value = this.url().trim();
    if (!value) return;

    this.isLoading.set(true);
    this.message.set('');

    this.http.post<any>('http://localhost:5042/api/analysis', { url: value })
      .subscribe({
        next: (res) => {
          this.message.set(`URL saved! Analysis ID: ${res.id}`);
          this.isLoading.set(false);
        },
        error: () => {
          this.message.set('Error: Could not connect to API');
          this.isLoading.set(false);
        }
      });
  }
}
