import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AnalyticsOverview,
  CalendarAnalytics,
  HabitAnalytics,
  TrendAnalytics
} from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) { }

  public getOverview(userId: number): Observable<AnalyticsOverview> {
    return this.http.get<AnalyticsOverview>(`${this.apiUrl}/user/${userId}/overview`);
  }

  public getHabitAnalytics(userId: number, habitId: number): Observable<HabitAnalytics> {
    return this.http.get<HabitAnalytics>(`${this.apiUrl}/user/${userId}/habit/${habitId}`);
  }

  public getCalendar(userId: number): Observable<CalendarAnalytics> {
    return this.http.get<CalendarAnalytics>(`${this.apiUrl}/user/${userId}/calendar`);
  }

  public getTrends(userId: number): Observable<TrendAnalytics> {
    return this.http.get<TrendAnalytics>(`${this.apiUrl}/user/${userId}/trends`);
  }
}
