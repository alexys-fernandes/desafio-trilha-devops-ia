import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, finalize, map, switchMap, take } from 'rxjs/operators';
import {
  AnalyticsOverview,
  AnalyticsTrendDay,
  CalendarAnalytics,
  CalendarAnalyticsDay,
  HabitAnalytics,
  TrendAnalytics
} from '../../../models/analytics.model';
import { Habit } from '../../../models/habit.model';
import { AnalyticsService } from '../../../services/analytics-service';
import { HabitService } from '../../../services/habit-service';
import { UserService } from '../../../services/user-service';

interface InsightsLoadResult {
  overview: AnalyticsOverview;
  calendar: CalendarAnalytics;
  trends: TrendAnalytics;
  habitBreakdown: HabitAnalytics[];
}

interface InsightsViewModel extends InsightsLoadResult {
  insightMessages: string[];
}

@Component({
  selector: 'app-insights',
  templateUrl: './insights.html',
  styleUrl: './insights.scss',
  standalone: false,
})
export class Insights implements OnInit {
  view: InsightsViewModel | null = null;
  loading = false;
  errorMessage = '';

  constructor(
    private analyticsService: AnalyticsService,
    private habitService: HabitService,
    private userService: UserService,
    private changeDetectorRef: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.loadInsights();
  }

  loadInsights(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getUser().pipe(
      take(1),
      switchMap((user) => {
        if (!user?.id) {
          return of(null);
        }

        return forkJoin({
          overview: this.analyticsService.getOverview(user.id).pipe(take(1)),
          calendar: this.analyticsService.getCalendar(user.id).pipe(take(1)),
          trends: this.analyticsService.getTrends(user.id).pipe(take(1)),
          habits: this.habitService.getHabitsByUser(user.id).pipe(
            take(1),
            catchError(() => of([] as Habit[]))
          ),
        }).pipe(
          switchMap((result) =>
            this.loadHabitBreakdown(user.id, result.habits).pipe(
              map((habitBreakdown) => ({
                overview: result.overview,
                calendar: result.calendar,
                trends: result.trends,
                habitBreakdown,
              }))
            )
          )
        );
      }),
      catchError(() => {
        this.errorMessage = 'Não foi possível carregar as análises agora.';
        return of(null);
      }),
      finalize(() => {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      }),
    ).subscribe((result) => {
      this.loading = false;

      if (!result) {
        return;
      }

      this.view = {
        ...result,
        insightMessages: this.buildInsightMessages(result),
      };
      this.changeDetectorRef.markForCheck();
    });
  }

  hasInsightData(overview: AnalyticsOverview): boolean {
    return overview.totalActiveHabits > 0 && overview.totalCompletions > 0;
  }

  getBarHeight(day: AnalyticsTrendDay): number {
    if (!day.scheduledHabits) {
      return 8;
    }

    return Math.max(12, day.completionRate);
  }

  getTrendAverage(days: AnalyticsTrendDay[]): number {
    const scheduledHabits = days.reduce((total, day) => total + day.scheduledHabits, 0);
    const completedHabits = days.reduce((total, day) => total + day.completedHabits, 0);

    return scheduledHabits ? Math.round((completedHabits / scheduledHabits) * 100) : 0;
  }

  getCompletionCopy(rate: number): string {
    if (rate >= 85) {
      return 'Ritmo forte';
    }

    if (rate >= 60) {
      return 'Consistência em construção';
    }

    if (rate > 0) {
      return 'Precisa de atenção';
    }

    return 'Sem dados ainda';
  }

  getCalendarTitle(day: CalendarAnalyticsDay): string {
    const label = this.formatDate(day.date);

    if (day.status === 'none') {
      return `${label}: nenhum hábito programado`;
    }

    return `${label}: ${day.completedCount}/${day.scheduledCount} concluídos`;
  }

  formatDate(date: string): string {
    return new Date(`${date}T00:00:00`).toLocaleDateString('pt-BR', {
      month: 'short',
      day: 'numeric',
    });
  }

  formatWeekday(dayOfWeek?: string | null): string {
    return this.translateWeekday(dayOfWeek) ?? 'Nenhum padrão';
  }

  formatWeekdayPlural(dayOfWeek?: string | null): string {
    if (!dayOfWeek) {
      return 'seus dias programados';
    }

    return this.translateWeekday(dayOfWeek, true) ?? 'seus dias programados';
  }

  getMissedCountLabel(habit: HabitAnalytics): string {
    const missedCount = habit.missedScheduledDates.length;
    return missedCount === 1 ? '1 dia não concluído' : `${missedCount} dias não concluídos`;
  }

  getLastCompletedLabel(habit: HabitAnalytics): string {
    return habit.lastCompletedDate ? this.formatDate(habit.lastCompletedDate) : 'Nenhuma conclusão ainda';
  }

  private loadHabitBreakdown(userId: number, habits: Habit[]): Observable<HabitAnalytics[]> {
    const activeHabits = habits.filter((habit) => !habit.isArchived);

    if (!activeHabits.length) {
      return of([]);
    }

    return forkJoin(
      activeHabits.map((habit) =>
        this.analyticsService.getHabitAnalytics(userId, habit.id).pipe(
          take(1),
          catchError(() => of(null))
        )
      )
    ).pipe(
      map((items) => items.filter((item): item is HabitAnalytics => item !== null))
    );
  }

  private buildInsightMessages(result: InsightsLoadResult): string[] {
    const overview = result.overview;

    if (!this.hasInsightData(overview)) {
      return [
        'Conclua alguns hábitos para liberar análises.',
        'Seus padrões aparecerão aqui após alguns dias de acompanhamento.',
      ];
    }

    const messages = [
      `Você tem mais consistência em ${this.formatWeekdayPlural(overview.bestDayOfWeek?.dayOfWeek)}.`,
    ];

    if (overview.bestHabit) {
      messages.push(`${overview.bestHabit.title} tem sua sequência mais forte.`);
    }

    if (overview.weakestHabit) {
      messages.push(`${overview.weakestHabit.title} é sua maior oportunidade de melhoria.`);
    }

    messages.push(this.getTrendMessage(result.trends));

    return messages.slice(0, 4);
  }

  private getTrendMessage(trends: TrendAnalytics): string {
    if (trends.last7Days.scheduledHabits === 0 || trends.last30Days.scheduledHabits === 0) {
      return 'Seu ritmo ficará mais claro conforme mais dias programados passarem.';
    }

    if (trends.last7Days.completionRate > trends.last30Days.completionRate) {
      return 'Sua taxa de conclusão melhorou nesta semana.';
    }

    if (trends.last7Days.completionRate < trends.last30Days.completionRate) {
      return 'Esta semana está abaixo do seu ritmo de 30 dias.';
    }

    return 'Esta semana está acompanhando seu ritmo de 30 dias.';
  }

  private translateWeekday(dayOfWeek?: string | null, plural = false): string | null {
    if (!dayOfWeek) {
      return null;
    }

    const normalized = dayOfWeek.replace(/s$/, '');
    const labels: Record<string, { singular: string; plural: string }> = {
      Sunday: { singular: 'domingo', plural: 'domingos' },
      Monday: { singular: 'segunda-feira', plural: 'segundas-feiras' },
      Tuesday: { singular: 'terça-feira', plural: 'terças-feiras' },
      Wednesday: { singular: 'quarta-feira', plural: 'quartas-feiras' },
      Thursday: { singular: 'quinta-feira', plural: 'quintas-feiras' },
      Friday: { singular: 'sexta-feira', plural: 'sextas-feiras' },
      Saturday: { singular: 'sábado', plural: 'sábados' },
    };
    const label = labels[normalized];

    if (!label) {
      return dayOfWeek;
    }

    return plural ? label.plural : label.singular;
  }
}
