import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  url = signal('');

  onAnalyze() {
    const value = this.url().trim();
    if (!value) return;
    console.log('Analyzing URL:', value);
  }
}
