import { PlatformLocation } from '@angular/common';
import { FetchBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { Injectable, InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

export const API_ORIGIN = new InjectionToken<string | null>('API_ORIGIN', {
  providedIn: 'root',
  factory: () => null,
});

@Injectable()
export class ApiOriginBackend extends FetchBackend {
  private readonly apiOrigin = inject(API_ORIGIN);
  private readonly location = inject(PlatformLocation);

  override handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    return super.handle(this.toApi(request));
  }

  private toApi(request: HttpRequest<unknown>): HttpRequest<unknown> {
    if (!this.apiOrigin) {
      return request;
    }
    const page = new URL(this.location.href);
    const url = new URL(request.url, page);
    return url.origin === page.origin && url.pathname.startsWith('/api/')
      ? request.clone({ url: `${this.apiOrigin}${url.pathname}${url.search}` })
      : request;
  }
}
