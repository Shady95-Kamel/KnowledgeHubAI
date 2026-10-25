
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {CsrfTokenResponse} from '../models/auth.model'

@Injectable({
  providedIn: 'root'
})
export class CsrfTokenService {
  private readonly http = inject(HttpClient);

  private token: string | null = null;
  private pendingRequest$: Observable<string> | null = null;

  getToken(): Observable<string> {
    if (this.token) {
      return new Observable<string>(subscriber => {
        subscriber.next(this.token!);
        subscriber.complete();
      });
    }

    if (!this.pendingRequest$) {
      this.pendingRequest$ = this.http
        .get<CsrfTokenResponse>(
          `${environment.apiUrl}/auth/csrf`,
          { withCredentials: true }
        )
        .pipe(
          map(response => response.data.requestToken),
          tap(token => {
            this.token = token;
          }),
          finalize(() => {
            this.pendingRequest$ = null;
          }),
          shareReplay(1)
        );
    }

    return this.pendingRequest$;
  }

  clearToken(): void {
    this.token = null;
  }
}
