import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { Login } from './components/views/login/login';
import { authGuard } from './guards/auth-guard';
import { Habits } from './components/views/habits/habits';
import { Recurrences } from './components/views/recurrences/recurrences';
import { Insights } from './components/views/insights/insights';
import { Streaks } from './components/views/streaks/streaks';
import { Achievements } from './components/views/achievements/achievements';
import { Notifications } from './components/views/notifications/notifications';
import { CoachComponent } from './components/views/coach/coach.component';

const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'habits', component: Habits, canActivate: [authGuard] },
  { path: 'recurrences', component: Recurrences, canActivate: [authGuard] },
  { path: 'insights', component: Insights, canActivate: [authGuard] },
  { path: 'streaks', component: Streaks, canActivate: [authGuard] },
  { path: 'achievements', component: Achievements, canActivate: [authGuard] },
  { path: 'notifications', component: Notifications, canActivate: [authGuard] },
  { path: 'coach', component: CoachComponent },
  { path: '', redirectTo: 'habits', pathMatch: 'full' },
  { path: '**', redirectTo: 'habits' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
