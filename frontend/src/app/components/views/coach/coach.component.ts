import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { AICoachService } from '../../../services/ai-coach-service';

@Component({
  selector: 'app-coach',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coach.component.html',
  styleUrl: './coach.component.scss',
})
export class CoachComponent {
  message = '';
  response = '';
  errorMessage = '';
  loading = false;

  constructor(
    private aiCoachService: AICoachService,
    private cdr: ChangeDetectorRef,
  ) { }

  send(): void {
    if (!this.message.trim()) {
      return;
    }

    this.loading = true;
    this.response = '';
    this.errorMessage = '';

    this.aiCoachService.sendMessage({
      userId: 1,
      message: this.message.trim()
    }).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (result) => {
        const normalizedResponse = result.response?.trim() ?? '';
        const normalizedError = result.error?.trim() ?? '';

        this.response = result.success ? normalizedResponse : '';
        this.errorMessage = result.success ? '' : (normalizedError || normalizedResponse || 'Não foi possível obter uma resposta do coach.');
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Não foi possível se comunicar com o coach. Verifique sua conexão e tente novamente.';
        this.response = '';
        this.cdr.detectChanges();
      }
    });
  }
}
