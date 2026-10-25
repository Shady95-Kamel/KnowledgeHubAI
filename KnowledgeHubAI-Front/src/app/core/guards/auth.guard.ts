import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';

import { map } from 'rxjs';

import { AuthSessionService } from '../auth/auth-session';

export const authGuard: CanActivateFn = () => {
  const authSession = inject(AuthSessionService);
  const router = inject(Router);

  return authSession.waitForSession().pipe(
    map(isAuthenticated => {
      if (isAuthenticated) {
        return true;
      }

      return router.createUrlTree(['/login']);
    })
  );
};
