import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { API_ORIGIN } from './api-origin';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    { provide: API_ORIGIN, useValue: process.env['services__api__http__0'] ?? null },
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
