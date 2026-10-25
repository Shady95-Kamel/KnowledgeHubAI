
import {
  Component,
  ChangeDetectorRef,
  OnInit,
  inject
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.scss'
})
export class VerifyEmail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  isLoading = true;
  isSuccess = false;
  message = 'Verifying your email...';

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token?.trim()) {
      this.isLoading = false;
      this.isSuccess = false;
      this.message = 'The verification link is invalid or missing its token.';
      this.cdr.detectChanges();
      return;
    }

    this.authService.verifyEmail({ token }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.isSuccess = response.success;
        this.message = response.message;

        this.cdr.detectChanges();
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading = false;
        this.isSuccess = false;

        const apiError = error.error;

        this.message =
          apiError?.description ??
          apiError?.message ??
          apiError?.detail ??
          this.getFallbackMessage(error.status);

        this.cdr.detectChanges();

        console.error('Email verification error:', error);
      }
    });
  }

  private getFallbackMessage(status: number): string {
    if (status === 0) {
      return 'Unable to connect to the server. Please try again.';
    }

    if (status === 400) {
      return 'The verification link is invalid, expired, or already used.';
    }

    return 'Email verification failed. Please try again later.';
  }
}
