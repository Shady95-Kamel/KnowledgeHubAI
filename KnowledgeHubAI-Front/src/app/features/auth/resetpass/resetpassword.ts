
import { Component, inject , ChangeDetectorRef} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

function passwordMatchValidator(
  control: AbstractControl
): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirmation = control.get('confirmPassword')?.value;

  if (!password || !confirmation) {
    return null;
  }

  return password === confirmation
    ? null
    : { passwordMismatch: true };
}

@Component({
  selector: 'app-resetpassword',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './resetpassword.html',
  styleUrl: './resetpassword.scss'
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly token =
    this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly form = this.fb.nonNullable.group(
    {
      newPassword: [
        '',
        [Validators.required, Validators.minLength(8)]
      ],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordMatchValidator }
  );

  loading = false;
  errorMessage = '';
  successMessage = '';

  submit(): void {
    if (!this.token || this.form.invalid || this.loading) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.resetPassword({
      token: this.token,
      ...this.form.getRawValue()
    }).subscribe({
      next: response => {
        this.successMessage = response.message;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.errorMessage =
          error?.error?.error?.description ??
          error?.error?.message ??
          'The reset link is invalid or has expired.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }
}
