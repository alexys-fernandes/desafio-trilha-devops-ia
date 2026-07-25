import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { LoadingService } from '../services/loading-service';

@Injectable()
export class NotificationInterceptor implements HttpInterceptor {
  constructor(
    private snackBar: MatSnackBar,
    private loadingService: LoadingService
  ) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const skipLoading = req.headers.has('X-Skip-Loading');
    const skipNotification = req.headers.has('X-Skip-Notification');

    if (!skipLoading) {
      this.loadingService.show();
    }

    return next.handle(req).pipe(
      tap((event: HttpEvent<any>) => {
        if (event instanceof HttpResponse && !skipNotification) {
          this.handleSuccess(req.method);
        }
      }),
      catchError((error: HttpErrorResponse) => {
        if (!skipNotification) {
          this.handleError(error);
        }

        return throwError(() => error);
      }),
      finalize(() => {
        if (!skipLoading) {
          this.loadingService.hide();
        }
      })
    );
  }

  private handleSuccess(method: string): void {
    let message = '';
    switch (method) {
      case 'POST': message = '🚀 Hábito criado com sucesso!'; break;
      case 'PUT': message = '✏️ Hábito atualizado com sucesso!'; break;
      case 'DELETE': message = '🗑️ Hábito excluído com sucesso!'; break;
      default: return;
    }
    this.snackBar.open(message, 'Fechar', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['snackbar-success']
    });
  }

  private handleError(error: HttpErrorResponse): void {
    let errorMessage = '❌ Ocorreu um erro inesperado na operação.';
    if (error.status === 0) {
      errorMessage = '📡 Não foi possível conectar ao servidor backend.';
    } else if (error.error && typeof error.error === 'string') {
      errorMessage = `❌ Erro: ${error.error}`;
    } else if (error.message) {
      errorMessage = `❌ Erro na requisição: ${error.statusText || 'Falha'}`;
    }
    this.snackBar.open(errorMessage, 'Fechar', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['snackbar-error']
    });
  }
}
