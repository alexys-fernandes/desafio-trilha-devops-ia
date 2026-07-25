import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  NotificationPayload,
  NotificationPreference,
  ReminderDashboard
} from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly apiUrl = `${environment.apiUrl}/reminder`;
  private dashboardSubject = new BehaviorSubject<ReminderDashboard | null>(null);

  public dashboard$ = this.dashboardSubject.asObservable();

  constructor(private http: HttpClient) { }

  public refreshByUserId(userId: number): void {
    this.getDashboard(userId).subscribe({
      error: (err) => console.error(`Erro ao carregar lembretes do usuário ${userId}: `, err),
    });
  }

  public getPreferences(userId: number): Observable<NotificationPreference> {
    return this.http.get<NotificationPreference>(`${this.apiUrl}/user/${userId}/preferences`);
  }

  public updatePreferences(
    userId: number,
    preferences: Partial<NotificationPreference>
  ): Observable<NotificationPreference> {
    return this.http.put<NotificationPreference>(
      `${this.apiUrl}/user/${userId}/preferences`,
      preferences
    );
  }

  public getDashboard(userId: number): Observable<ReminderDashboard> {
    return this.http.get<ReminderDashboard>(`${this.apiUrl}/user/${userId}/dashboard`).pipe(
      tap((dashboard) => this.dashboardSubject.next(dashboard))
    );
  }

  public generatePayloads(userId: number, mode = 'Smart'): Observable<NotificationPayload[]> {
    return this.http.get<NotificationPayload[]>(
      `${this.apiUrl}/user/${userId}/payloads`,
      { params: { mode } }
    );
  }
}
