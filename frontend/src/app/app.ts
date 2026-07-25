import { Component } from '@angular/core';
import { ThemeService } from './services/theme-service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.scss',
})
export class App {
  constructor(_themeService: ThemeService) {}
}
