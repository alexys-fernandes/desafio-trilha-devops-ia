import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { User } from '../models/user.model';
import { UserResponse } from '../models/user-responde.model';
import { UserRequest } from '../models/user-request.model';
import { BaseService } from './base-service';

@Injectable({
  providedIn: 'root',
})
export class UserService extends BaseService<User> {
  public users$ = this.data$;

  private currentUserSubject = new BehaviorSubject<UserResponse | null>(
    JSON.parse(localStorage.getItem('habitapp_user') || 'null')
  );

  constructor(http: HttpClient) {
    super(http, `${environment.apiUrl}/user`);

    if (this.getUserValue()) {
      this.refresh();
    }
  }

  public getUser(): Observable<UserResponse | null> {
    return this.currentUserSubject.asObservable();
  }

  public getUserValue(): UserResponse | null {
    return this.currentUserSubject.value;
  }

  public override update(item: User): Observable<User> {
    return super.update(item).pipe(
      tap((updatedUser: User) => {
        const currentUser = this.getUserValue();

        if (currentUser && currentUser.id === updatedUser.id) {
          const updatedResponse: UserResponse = {
            id: updatedUser.id,
            name: updatedUser.name,
            email: updatedUser.email
          };

          localStorage.setItem('habitapp_user', JSON.stringify(updatedResponse));

          this.currentUserSubject.next(updatedResponse);
        }
      })
    );
  }

  public login(userRequest: UserRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.apiUrl}/login`, userRequest).pipe(
      tap((user: UserResponse) => {
        if (user) {
          localStorage.setItem('habitapp_user', JSON.stringify(user));
          this.currentUserSubject.next(user);
          this.refresh();
        }
      })
    );
  }

  public register(userData: Partial<User>): Observable<User> {
    return this.http.post<User>(this.apiUrl, userData).pipe(
      tap((user: User) => {
        if (user) {
          localStorage.setItem('habitapp_user', JSON.stringify(user));
          this.currentUserSubject.next(user as unknown as UserResponse);
          this.refresh();
        }
      })
    );
  }

  public logout(): void {
    localStorage.removeItem('habitapp_user');
    this.currentUserSubject.next(null);
  }
}
