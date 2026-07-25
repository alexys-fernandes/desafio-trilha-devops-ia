import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { of } from 'rxjs';
import { catchError, finalize, switchMap, take } from 'rxjs/operators';
import {
  HabitStreakStatus,
  MotivationHabitAtRisk,
  StreakCenter
} from '../../../models/motivation.model';
import { MotivationService } from '../../../services/motivation-service';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-streaks',
  templateUrl: './streaks.html',
  styleUrl: './streaks.scss',
  standalone: false,
})
export class Streaks implements OnInit {
  streakCenter: StreakCenter | null = null;
  loading = false;
  errorMessage = '';

  constructor(
    private motivationService: MotivationService,
    private userService: UserService,
    private changeDetectorRef: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.loadStreaks();
  }

  loadStreaks(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getUser().pipe(
      take(1),
      switchMap((user) => user?.id
        ? this.motivationService.getStreakCenter(user.id).pipe(take(1))
        : of(null)),
      catchError(() => {
        this.errorMessage = 'Não foi possível carregar as sequências agora.';
        return of(null);
      }),
      finalize(() => {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      })
    ).subscribe((streakCenter) => {
      this.loading = false;
      this.streakCenter = streakCenter;
      this.changeDetectorRef.markForCheck();
    });
  }

  hasStreakData(streakCenter: StreakCenter): boolean {
    return streakCenter.habitStreaks.length > 0;
  }

  getStatusLabel(habit: HabitStreakStatus): string {
    switch (habit.status) {
      case 'protected':
        return 'Protegido';
      case 'at-risk':
        return 'Em risco';
      case 'rebuilding':
        return 'Reconstruindo';
      case 'new':
      default:
        return 'Novo';
    }
  }

  getRiskLabel(habit: MotivationHabitAtRisk): string {
    return habit.riskLevel === 'high' ? 'Risco alto' : 'Precisa de atenção';
  }

  formatDate(date?: string | null): string {
    if (!date) {
      return 'Sem data ainda';
    }

    return new Date(`${date}T00:00:00`).toLocaleDateString('pt-BR', {
      month: 'short',
      day: 'numeric',
    });
  }
}
