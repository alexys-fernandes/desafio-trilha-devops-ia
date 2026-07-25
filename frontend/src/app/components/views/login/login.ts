import { Component, ViewChild, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';
import { UserService } from '../../../services/user-service';
import { LoadingService } from '../../../services/loading-service';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrl: './login.scss',
  standalone: false
})
export class Login implements OnInit {
  @ViewChild('authForm') authForm!: NgForm;

  isLoginMode = true;
  isLoading$!: Observable<boolean>;
  formData = {
    name: '',
    email: '',
    password: ''
  };

  constructor(
    private userService: UserService,
    private router: Router,
    private snackBar: MatSnackBar,
    private loadingService: LoadingService
  ) { }

  ngOnInit(): void {
    this.isLoading$ = this.loadingService.loading$;
  }

  toggleMode(isLogin: boolean): void {
    this.isLoginMode = isLogin;
    this.formData = { name: '', email: '', password: '' };
    if (this.authForm) {
      this.authForm.resetForm(this.formData);
    }
  }

  onSubmit(): void {
    if (!this.formData.email || !this.formData.password || (!this.isLoginMode && !this.formData.name)) {
      return;
    }

    const action$ = this.isLoginMode
      ? this.userService.login({ email: this.formData.email, password: this.formData.password })
      : this.userService.register(this.formData);

    action$.subscribe({
      next: () => {
        this.snackBar.open('✅ Autenticado com sucesso!', 'Fechar', {
          duration: 3000,
          panelClass: ['snackbar-success']
        });
        this.router.navigate(['/habits']);
      },
      error: () => { }
    });
  }
}
