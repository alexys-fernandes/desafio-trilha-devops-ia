import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Base } from '../models/base-model';

export abstract class BaseService<T extends Base> {
  protected dataSubject = new BehaviorSubject<T[]>([]);
  public data$ = this.dataSubject.asObservable();

  constructor(
    protected http: HttpClient,
    protected apiUrl: string
  ) {
    this.refresh();
  }

  public refresh(): void {
    this.http.get<T[]>(this.apiUrl).subscribe({
      next: (items: T[]) => this.dataSubject.next(items),
      error: (err) => console.error(`Erro ao carregar dados de ${this.apiUrl}: `, err)
    });
  }

  public getAll(): T[] {
    return this.dataSubject.value;
  }

  public getById(id: number): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}/${id}`);
  }

  public add(item: Partial<T>): Observable<T> {
    return this.http.post<T>(this.apiUrl, item).pipe(
      tap(() => this.refresh())
    );
  }

  public update(item: T): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}/${item.id}`, item).pipe(
      tap(() => this.refresh())
    );
  }

  public delete(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.refresh())
    );
  }
}
