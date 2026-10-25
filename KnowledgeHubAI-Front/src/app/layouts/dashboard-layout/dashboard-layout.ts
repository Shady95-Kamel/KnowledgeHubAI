import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { inject, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CurrentUser } from '../../core/models/current-user.model';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.scss'
})
export class DashboardLayoutComponent {

private readonly authService = inject(AuthService);
private readonly router = inject(Router);
private readonly cdr = inject(ChangeDetectorRef);

currentUser: CurrentUser | null = null;
isLoadingUser = true;
userMenuOpen = false;
isLoggingOut = false;

ngOnInit(): void {
  this.loadCurrentUser();
}

loadCurrentUser(): void {
  this.isLoadingUser = true;

  this.authService.getMe().subscribe({
    next: (response) => {
      this.currentUser = response.success ? response.data : null;
      this.isLoadingUser = false;
      this.cdr.detectChanges();
    },
    error: (error) => {
      console.error('Failed to load current user:', error);
      this.currentUser = null;
      this.isLoadingUser = false;
      this.cdr.detectChanges();
    }
  });
}

get displayName(): string {
  const email = this.currentUser?.email;

  if (!email) {
    return 'My Account';
  }

  return email.split('@')[0];
}

get userInitial(): string {
  return this.displayName.charAt(0).toUpperCase() || 'U';
}

toggleUserMenu(): void {
  this.userMenuOpen = !this.userMenuOpen;
}

onLogout(): void {
  if (this.isLoggingOut) {
    return;
  }

  this.isLoggingOut = true;

  this.authService.logout().subscribe({
    next: () => {
      this.router.navigate(['/login']);
    },
    error: (error) => {
      console.error('Logout failed:', error);
      this.isLoggingOut = false;
    }
  });
}

}

