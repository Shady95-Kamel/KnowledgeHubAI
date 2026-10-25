
import { Component, inject , ChangeDetectorRef  } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  FormGroupDirective,
  NgForm,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';

import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AuthService } from '../../../core/services/auth.service';

export class ConfirmPasswordErrorStateMatcher
  implements ErrorStateMatcher {

  isErrorState(
    control: AbstractControl | null,
    form: FormGroupDirective | NgForm | null
  ): boolean {
    if (!control) {
      return false;
    }

    const interacted = control.touched || !!form?.submitted;
    const passwordMismatch =
      !!control.parent?.hasError('passwordMismatch');

    return interacted &&
      (control.invalid || passwordMismatch);
  }
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  registerForm: FormGroup;

  confirmPasswordMatcher =
    new ConfirmPasswordErrorStateMatcher();
  showPassword = false;
  showConfirmPassword = false;
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  private readonly cdr = inject(ChangeDetectorRef);

  constructor() {
    this.registerForm = this.fb.group(
      {
        firstName: [
          '',
          [Validators.required, Validators.maxLength(50)]
        ],
        lastName: [
          '',
          [Validators.required, Validators.maxLength(50)]
        ],
        email: [
          '',
          [Validators.required, Validators.email]
        ],
        password: [
          '',
          [Validators.required, Validators.minLength(8)]
        ],
        confirmPassword: [
          '',
          [Validators.required]
        ]
      },
      {
        validators: this.passwordMatchValidator
      }
    );
  }

  passwordMatchValidator(
    control: AbstractControl
  ): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword =
      control.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword
      ? null
      : { passwordMismatch: true };
  }

  onSubmit(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const request = {
      firstName: this.registerForm.value.firstName,
      lastName: this.registerForm.value.lastName,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password
    };

    this.authService.register(request).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.isLoading = false;
        this.registerForm.reset();
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading = false;

        if (error.status === 400) {
          const validationErrors = error.error?.errors;

          if (validationErrors) {
            this.errorMessage = Object.values(
              validationErrors as Record<string, string[]>
            ).flat().join(' ');
          } else {
            this.errorMessage =
              error.error?.detail ??
              error.error?.message ??
              'Please check the entered data.';
          }
        } else if (error.status === 409) {
          this.errorMessage =
            error.error?.detail ?? 'This email is already registered.';
        } else {
          this.errorMessage =
            error.error?.detail ?? 'Registration failed. Please try again.';
        }

        this.cdr.detectChanges();

        console.error('Registration error:', error);
      }
    });
  }
}
