import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Header } from './components/static/header/header';
import { Body } from './components/static/body/body';
import { Footer } from './components/static/footer/footer';
import { HabitDialog } from './components/views/dialogs/habit-dialog/habit-dialog';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NotificationInterceptor } from './interceptors/notification-interceptor';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { Login } from './components/views/login/login';
import { Habits } from './components/views/habits/habits';
import { ConfirmDialog } from './components/views/dialogs/confirm-dialog/confirm-dialog';
import { UserDialog } from './components/views/dialogs/user-dialog/user-dialog';
import { Recurrences } from './components/views/recurrences/recurrences';
import { Insights } from './components/views/insights/insights';
import { Streaks } from './components/views/streaks/streaks';
import { Achievements } from './components/views/achievements/achievements';
import { Notifications } from './components/views/notifications/notifications';
import { CoachComponent } from './components/views/coach/coach.component';

@NgModule({
  declarations: [
    App,
    Header,
    Body,
    Footer,
    HabitDialog,
    Login,
    Habits,
    ConfirmDialog,
    UserDialog,
    Recurrences,
    Insights,
    Streaks,
    Achievements,
    Notifications
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    CoachComponent
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: NotificationInterceptor,
      multi: true,
    },
  ],
  bootstrap: [App],
})
export class AppModule { }
