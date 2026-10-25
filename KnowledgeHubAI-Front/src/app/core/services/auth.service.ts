
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap ,switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUserResponse } from '../models/current-user.model';

import {
  RegisterRequest,
  RegisterResponse,
  VerifyEmailRequest,
  VerifyEmailResponse,
  LoginRequest,
  LoginResponse,
  ResetPasswordRequest,
  ForgotPasswordRequest,
  ApiMessage
} from '../models/auth.model';

import { CsrfTokenService } from '../services/csrf-token.service';
import { AuthSessionService } from '../auth/auth-session';

interface AuthMessageResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly csrfTokenService = inject(CsrfTokenService);
  private readonly authSession = inject(AuthSessionService);

  private readonly apiUrl = `${environment.apiUrl}/auth`;

  register(
    request: RegisterRequest
  ): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(
      `${this.apiUrl}/register`,
      request
    );
  }

  verifyEmail(
    request: VerifyEmailRequest
  ): Observable<VerifyEmailResponse> {
    return this.http.post<VerifyEmailResponse>(
      `${this.apiUrl}/verify-email`,
      request
    );
  }


  login(
    request: LoginRequest
  ): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/login`,
      request
    ).pipe(
      tap((response) => {
        if (response.success) {
          this.authSession.sessionChanged();
        }
      })
    );
  }


  refresh(): Observable<AuthMessageResponse> {
    return this.http.post<AuthMessageResponse>(
      `${this.apiUrl}/refresh`,
      {}
    );
  }

logout(): Observable<AuthMessageResponse> {
  this.csrfTokenService.clearToken();

  return this.csrfTokenService.getToken().pipe(
    switchMap(() =>
      this.http.post<AuthMessageResponse>(
        `${this.apiUrl}/logout`,
        {},
        { withCredentials: true }
      )
    ),
    tap((response) => {
      if (response.success) {
        this.authSession.clearSession();
        this.csrfTokenService.clearToken();

        sessionStorage.removeItem('accessToken');
        sessionStorage.removeItem('refreshToken');
        sessionStorage.removeItem('accessTokenExpiresAt');
        sessionStorage.removeItem('refreshTokenExpiresAt');
      }
    })
  );
}

  googleLogin(): void {
    window.location.href = `${this.apiUrl}/google`;
  }

  forgotPassword(
    request: ForgotPasswordRequest
  ): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/forgot-password`,
      request
    );
  }

  resetPassword(
    request: ResetPasswordRequest
  ): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(
      `${this.apiUrl}/reset-password`,
      request
    );
  }

  getMe(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(
      `${this.apiUrl}/me`,
      { withCredentials: true }
    );
  }
}
