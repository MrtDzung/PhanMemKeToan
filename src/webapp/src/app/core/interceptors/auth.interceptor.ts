import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, throwError, filter, take, switchMap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { from } from 'rxjs';
import { AuthStore } from '../stores/auth.store';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authStore = inject(AuthStore);

  const token = authStore.accessToken();
  const requestWithAuth = token
    ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
    : req;

  return next(requestWithAuth).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/api/auth/')) {
        if (!isRefreshing) {
          isRefreshing = true;
          refreshTokenSubject.next(null); // block queued requests

          return from(authStore.refreshToken()).pipe(
            switchMap((newToken: string) => {
              isRefreshing = false;
              refreshTokenSubject.next(newToken); // unblock queued requests
              const retryReq = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${newToken}`)
              });
              return next(retryReq);
            }),
            catchError((refreshError) => {
              isRefreshing = false;
              refreshTokenSubject.next(null);
              authStore.logout();
              return throwError(() => refreshError);
            })
          );
        } else {
          // Queue: wait for refresh to complete, then retry with new token
          return refreshTokenSubject.pipe(
            filter((token): token is string => token !== null),
            take(1),
            switchMap((newToken) => {
              const retryReq = req.clone({
                headers: req.headers.set('Authorization', `Bearer ${newToken}`)
              });
              return next(retryReq);
            })
          );
        }
      }
      return throwError(() => error);
    })
  );
};
