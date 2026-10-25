
import { inject } from '@angular/core';
import {
  HttpInterceptorFn
} from '@angular/common/http';
import { switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CsrfTokenService } from '../services/csrf-token.service';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = environment.apiUrl;
  const isApiRequest = req.url.startsWith(apiUrl);

  if (!isApiRequest) {
    return next(req);
  }

  const csrfService = inject(CsrfTokenService);

  const request = req.clone({
    withCredentials: true
  });

  const isUnsafeMethod = [
    'POST',
    'PUT',
    'PATCH',
    'DELETE'
  ].includes(req.method.toUpperCase());

  if (!isUnsafeMethod) {
    return next(request);
  }

  return csrfService.getToken().pipe(
    switchMap(token =>
      next(
        request.clone({
          setHeaders: {
            'X-CSRF-TOKEN': token
          }
        })
      )
    )
  );
};
