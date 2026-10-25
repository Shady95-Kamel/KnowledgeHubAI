
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-forgotpassword',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgotpassword.html',
  styleUrl: './forgotpassword.scss'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef)

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  loading = false;
  errorMessage = '';
  successMessage = '';

  submit(): void {
    if (this.form.invalid || this.loading) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService
      .forgotPassword(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response) => {
          console.log('Forgot password response:', response);

          this.successMessage =
            response?.message ??
            'If the email exists, a password reset link will be sent.';
            this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Forgot password error:', error);

          this.errorMessage =
            error?.error?.message ??
            error?.error?.error?.description ??
            'Unable to process your request. Please try again.';
            this.cdr.detectChanges();
        }
      });
  }
}
