import { HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';

export const API_ORIGIN = new InjectionToken<string | null>('API_ORIGIN', {
  providedIn: 'root',
  factory: () => null,
});

export const apiOriginInterceptor: HttpInterceptorFn = (request, next) => {
  const origin = inject(API_ORIGIN);
  return next(origin && request.url.startsWith('/api/') ? request.clone({ url: `${origin}${request.url}` }) : request);
};
