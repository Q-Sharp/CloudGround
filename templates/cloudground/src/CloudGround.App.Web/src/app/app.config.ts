import { provideHttpClient, withFetch, withXsrfConfiguration } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideClientHydration, withHttpTransferCacheOptions } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withHttpTransferCacheOptions({ includePostRequests: false })),
    provideHttpClient(
      withFetch(),
      withXsrfConfiguration({ cookieName: 'cloudground-app-xsrf', headerName: 'X-XSRF-TOKEN' }),
    ),
  ],
};
