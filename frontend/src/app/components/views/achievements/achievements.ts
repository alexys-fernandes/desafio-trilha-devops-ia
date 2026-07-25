import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap, take } from 'rxjs/operators';
import {
  AchievementProgress,
  AchievementSet,
  MonthlyChallenge,
  MonthlyChallengeSet,
  MotivationSummary
} from '../../../models/motivation.model';
import { MotivationService } from '../../../services/motivation-service';
import { UserService } from '../../../services/user-service';

interface AchievementsViewModel {
  summary: MotivationSummary;
  achievementSet: AchievementSet;
  monthlyChallenges: MonthlyChallengeSet;
  unlockedAchievements: AchievementProgress[];
  lockedAchievements: AchievementProgress[];
}

@Component({
  selector: 'app-achievements',
  templateUrl: './achievements.html',
  styleUrl: './achievements.scss',
  standalone: false,
})
export class Achievements implements OnInit {
  view: AchievementsViewModel | null = null;
  loading = false;
  errorMessage = '';

  constructor(
    private motivationService: MotivationService,
    private userService: UserService,
    private changeDetectorRef: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.loadAchievements();
  }

  loadAchievements(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getUser().pipe(
      take(1),
      switchMap((user) => {
        if (!user?.id) {
          return of(null);
        }

        return forkJoin({
          summary: this.motivationService.getSummary(user.id).pipe(take(1)),
          achievementSet: this.motivationService.getAchievements(user.id).pipe(take(1)),
          monthlyChallenges: this.motivationService.getMonthlyChallenges(user.id).pipe(take(1)),
        });
      }),
      map((result) => result ? this.createViewModel(result) : null),
      catchError(() => {
        this.errorMessage = 'Não foi possível carregar as conquistas agora.';
        return of(null);
      }),
      finalize(() => {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      })
    ).subscribe((view) => {
      this.loading = false;
      this.view = view;
      this.changeDetectorRef.markForCheck();
    });
  }

  hasAchievementData(view: AchievementsViewModel): boolean {
    return view.achievementSet.totalCount > 0;
  }

  getProgressLabel(item: AchievementProgress | MonthlyChallenge): string {
    return `${item.currentValue}/${item.targetValue}`;
  }

  private createViewModel(result: {
    summary: MotivationSummary;
    achievementSet: AchievementSet;
    monthlyChallenges: MonthlyChallengeSet;
  }): AchievementsViewModel {
    return {
      ...result,
      unlockedAchievements: result.achievementSet.achievements.filter(
        (achievement) => achievement.isUnlocked
      ),
      lockedAchievements: result.achievementSet.achievements.filter(
        (achievement) => !achievement.isUnlocked
      ),
    };
  }
}
