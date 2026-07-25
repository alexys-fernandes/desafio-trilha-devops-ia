import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AchievementSet,
  MonthlyChallengeSet,
  MotivationSummary,
  StreakCenter
} from '../models/motivation.model';

@Injectable({ providedIn: 'root' })
export class MotivationService {
  private readonly apiUrl = `${environment.apiUrl}/motivation`;

  constructor(private http: HttpClient) { }

  public getSummary(userId: number): Observable<MotivationSummary> {
    return this.http.get<MotivationSummary>(`${this.apiUrl}/user/${userId}/summary`);
  }

  public getStreakCenter(userId: number): Observable<StreakCenter> {
    return this.http.get<StreakCenter>(`${this.apiUrl}/user/${userId}/streaks`);
  }

  public getAchievements(userId: number): Observable<AchievementSet> {
    return this.http.get<AchievementSet>(`${this.apiUrl}/user/${userId}/achievements`);
  }

  public getMonthlyChallenges(userId: number): Observable<MonthlyChallengeSet> {
    return this.http.get<MonthlyChallengeSet>(
      `${this.apiUrl}/user/${userId}/monthly-challenges`
    );
  }
}
