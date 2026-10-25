
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface CurrentUserResponse {
  userId: string;
  email: string;
  role: string;
}

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './auth-callback.html',
  styleUrl: './auth-callback.scss'
})
export class AuthCallback implements OnInit {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  message = signal('Completing Google sign-in...');
  error = signal('');

  async ngOnInit(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.get<CurrentUserResponse>(
          'https://localhost:7261/api/auth/me',
          { withCredentials: true }
        )
      );

      await this.router.navigateByUrl('/dashboard');
    }  catch (error: any) {
      console.error('Google callback error:', error);
      console.error('HTTP status:', error?.status);
      console.error('HTTP response:', error?.error);
      console.error('HTTP headers:', error?.headers);
      console.error('Request URL:', error?.url);

      this.error.set(
        `Google sign-in failed. HTTP status: ${error?.status ?? 'Unknown'}`
      );

      this.message.set('');
    }
  }
}
