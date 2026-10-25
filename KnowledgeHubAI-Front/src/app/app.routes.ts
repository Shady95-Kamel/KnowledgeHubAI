import { Routes } from '@angular/router';

import { LoginComponent } from './features/auth/login/login';
import { ForgotPasswordComponent } from './features/auth/forgotpass/forgotpassword';
import { ResetPasswordComponent } from './features/auth/resetpass/resetpassword';

import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout';
import { Chat } from './features/chat/pages/chat/chat';
import { Documents } from './features/documents/documents';

import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/register',
    pathMatch: 'full'
  },

  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register')
        .then(m => m.Register)
  },

  {
    path: 'verify-email',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/verify-email/verify-email')
        .then(m => m.VerifyEmail)
  },

  {
    path: 'login',
    canActivate: [guestGuard],
    component: LoginComponent
  },

  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/auth/auth-callback/auth-callback')
        .then(m => m.AuthCallback)
  },

  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'documents',
        pathMatch: 'full'
      },
      {
        path: 'documents',
        component: Documents
      },
      {
        path: 'chat',
        component: Chat
      }
    ]
  },

  {
    path: 'chat',
    redirectTo: 'dashboard/chat',
    pathMatch: 'full'
  },

  {
    path: 'forgotpassword',
    canActivate: [guestGuard],
    component: ForgotPasswordComponent
  },

  {
    path: 'resetpassword',
    canActivate: [guestGuard],
    component: ResetPasswordComponent
  },

  {
    path: '**',
    redirectTo: 'dashboard/documents'
  }
];
