
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import {
  BehaviorSubject,
  Observable,
  Subscription,
  auditTime,
  catchError,
  filter,
  finalize,
  fromEvent,
  map,
  merge,
  of,
  switchMap,
  take,
  timer,
} from 'rxjs';

import { environment } from '../../../environments/environment';

type SessionStatus =
  | 'checking'
  | 'authenticated'
  | 'unauthenticated';

interface AuthSessionInfo {
  userId: string;
  email: string;
  role: string;
  refreshTokenExpiresAtUtc: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
}

interface AuthMessageResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthSessionService {
  private readonly http = inject(HttpClient);

  private readonly meUrl = `${environment.apiUrl}/auth/me`;
  private readonly refreshUrl = `${environment.apiUrl}/auth/refresh`;

  // Refresh five minutes before the refresh token expires.
  private readonly refreshBeforeExpiryMs = 5 * 60 * 1000;

  private refreshTimer?: Subscription;
  private readonly subscriptions = new Subscription();

  private initialized = false;
  private checkingSession = false;
  private refreshing = false;
  private checkQueued = false;

  // Current authentication state.
  private readonly sessionStatusSubject =
    new BehaviorSubject<SessionStatus>('checking');

  // Prevent an old /me response from overriding a newer session.
  private sessionVersion = 0;

  initialize(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;

    // Check the current BFF session at application startup.
    this.checkSession();

    // Recheck when the user returns to the application.
    this.subscriptions.add(
      merge(
        fromEvent(window, 'focus'),
        fromEvent(document, 'visibilitychange').pipe(
          filter(
            () => document.visibilityState === 'visible'
          )
        )
      )
        .pipe(auditTime(1000))
        .subscribe(() => this.checkSession())
    );
  }

  /**
   * Wait until the initial session check finishes.
   * Used by authGuard and guestGuard.
   */
  waitForSession(): Observable<boolean> {
    if (!this.initialized) {
      this.initialize();
    }

    return this.sessionStatusSubject.asObservable().pipe(
      filter((status) => status !== 'checking'),
      take(1),
      map((status) => status === 'authenticated')
    );
  }

  /**
   * Call after successful login, or after the Google callback
   * has confirmed that the BFF session is available.
   */
  sessionChanged(): void {
    this.sessionVersion++;
    this.checkQueued = false;
    this.clearRefreshTimer();

    // Guards wait until the new session has been checked.
    this.sessionStatusSubject.next('checking');

    this.checkSession();
  }

  /**
   * Call after successful logout.
   */
  clearSession(): void {
    this.sessionVersion++;
    this.checkQueued = false;
    this.clearRefreshTimer();

    this.sessionStatusSubject.next('unauthenticated');
  }

  private checkSession(): void {
    // The refresh flow checks /me after refreshing.
    if (this.refreshing) {
      return;
    }

    // Queue another check instead of sending parallel requests.
    if (this.checkingSession) {
      this.checkQueued = true;
      return;
    }

    this.checkingSession = true;

    const requestVersion = this.sessionVersion;

    this.http
      .get<ApiResponse<AuthSessionInfo>>(this.meUrl, {
        withCredentials: true,
      })
      .pipe(
        catchError(() => of(null)),
        finalize(() => {
          this.checkingSession = false;

          if (this.checkQueued && !this.refreshing) {
            this.checkQueued = false;
            this.checkSession();
          }
        })
      )
      .subscribe((response) => {
        // Ignore responses belonging to an older session.
        if (requestVersion !== this.sessionVersion) {
          return;
        }

        if (!response?.success || !response.data) {
          this.clearRefreshTimer();
          this.sessionStatusSubject.next('unauthenticated');
          return;
        }

        this.sessionStatusSubject.next('authenticated');

        this.scheduleRefresh(
          response.data.refreshTokenExpiresAtUtc
        );
      });
  }

  private scheduleRefresh(expiresAtUtc: string): void {
    this.clearRefreshTimer();

    const expiryTime = Date.parse(expiresAtUtc);

    if (!Number.isFinite(expiryTime)) {
      return;
    }

    const refreshInMs = Math.max(
      0,
      expiryTime - Date.now() - this.refreshBeforeExpiryMs
    );

    this.refreshTimer = timer(refreshInMs).subscribe(() => {
      this.refreshTimer = undefined;
      this.refreshSession();
    });
  }

  private refreshSession(): void {
    if (this.refreshing || this.checkingSession) {
      return;
    }

    this.refreshing = true;

    this.http
      .post<AuthMessageResponse>(
        this.refreshUrl,
        {},
        { withCredentials: true }
      )
      .pipe(
        switchMap((response) => {
          // Return null instead of EMPTY so a failed refresh
          // reaches subscribe and updates the session state.
          if (!response?.success) {
            return of(null);
          }

          // Read the updated expiry after the backend refresh.
          return this.http.get<ApiResponse<AuthSessionInfo>>(
            this.meUrl,
            { withCredentials: true }
          );
        }),
        catchError(() => of(null)),
        finalize(() => {
          this.refreshing = false;

          if (this.checkQueued) {
            this.checkQueued = false;
            this.checkSession();
          }
        })
      )
      .subscribe((response) => {
        if (!response?.success || !response.data) {
          this.clearRefreshTimer();
          this.sessionStatusSubject.next('unauthenticated');
          return;
        }

        this.sessionStatusSubject.next('authenticated');

        this.scheduleRefresh(
          response.data.refreshTokenExpiresAtUtc
        );
      });
  }

  private clearRefreshTimer(): void {
    this.refreshTimer?.unsubscribe();
    this.refreshTimer = undefined;
  }
}
