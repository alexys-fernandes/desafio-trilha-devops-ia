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
        if (event instanceof HttpResponse && !skipNotification && this.shouldNotify(req)) {
          const isSuccessful = this.isSuccessfulResponse(req, event.body);

          if (isSuccessful) {
            this.handleSuccess(req.method);
          } else {
            const message = this.getResponseErrorMessage(event.body);
            this.showError(message ?? '❌ A operação não foi concluída com sucesso.');
          }
        }
      }),
      catchError((error: HttpErrorResponse) => {
        if (!skipNotification && this.shouldNotify(req)) {
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

  private shouldNotify(req: HttpRequest<any>): boolean {
    const url = req.url.toLowerCase();
    return url.includes('/habit') || url.includes('/habits');
  }

  private isSuccessfulResponse(req: HttpRequest<any>, body: any): boolean {
    const url = req.url.toLowerCase();

    if (url.includes('/aicoach/')) {
      return body?.success === true;
    }

    if (body && typeof body === 'object' && 'success' in body) {
      return body.success === true;
    }

    return true;
  }

  private getResponseErrorMessage(body: any): string | null {
    if (!body || typeof body !== 'object') {
      return null;
    }

    if (typeof body.message === 'string' && body.message.trim()) {
      return body.message.trim();
    }

    if (typeof body.error === 'string' && body.error.trim()) {
      return body.error.trim();
    }

    if (typeof body.response === 'string' && body.response.trim()) {
      return body.response.trim();
    }

    if (typeof body.detail === 'string' && body.detail.trim()) {
      return body.detail.trim();
    }

    return null;
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Fechar', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['snackbar-error']
    });
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
    this.showError(errorMessage);
  }
}
