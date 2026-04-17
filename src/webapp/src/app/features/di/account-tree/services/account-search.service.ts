import { Injectable, inject } from '@angular/core';
import { Observable, Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AccountApiService } from './account-api.service';
import { AccountListItemDto } from '../../models/account.models';

@Injectable({ providedIn: 'root' })
export class AccountSearchService {
  private readonly apiService = inject(AccountApiService);
  private readonly query$ = new Subject<string>();

  readonly results$: Observable<AccountListItemDto[]> = this.query$.pipe(
    debounceTime(200),
    distinctUntilChanged(),
    switchMap((q) => {
      if (!q.trim()) return of([]);
      return this.apiService.search(q).pipe(
        map((res) => res.data),
        catchError(() => of([]))
      );
    })
  );

  search(query: string): void {
    this.query$.next(query);
  }
}
