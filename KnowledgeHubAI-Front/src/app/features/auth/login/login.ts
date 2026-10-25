
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule , RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';

  isLoading = signal(false);
  errorMessage = signal('');

  onSubmit(): void {
    if (this.isLoading()) {
      return;
    }

    this.errorMessage.set('');

    const request: LoginRequest = {
      email: this.email.trim(),
      password: this.password
    };

    this.isLoading.set(true);

    this.authService.login(request).subscribe({
      next: () => {
        this.isLoading.set(false);

        // The BFF manages authentication using an HttpOnly cookie.
        // Do not store access or refresh tokens in browser storage.

        this.router.navigateByUrl('/dashboard');
      },
      error: (error) => {
        this.isLoading.set(false);

        this.errorMessage.set(
          error?.error?.detail ??
          error?.error?.message ??
          'Login failed. Please check your email and password.'
        );
      }
    });
  }

  loginWithGoogle(): void {
    this.authService.googleLogin();
  }
}
