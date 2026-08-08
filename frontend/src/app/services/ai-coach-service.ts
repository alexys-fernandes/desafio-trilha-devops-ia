import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AICoachRequest, AICoachResponse } from '../models/ai-coach.model';

@Injectable({ providedIn: 'root' })
export class AICoachService {
  private readonly apiUrl = `${environment.apiUrl}/aicoach`;

  constructor(private http: HttpClient) { }

  sendMessage(request: AICoachRequest): Observable<AICoachResponse> {
    const headers = new HttpHeaders({ 'X-Skip-Notification': 'true' });
    return this.http.post<AICoachResponse>(`${this.apiUrl}/sendMessage`, request, { headers });
  }
}
