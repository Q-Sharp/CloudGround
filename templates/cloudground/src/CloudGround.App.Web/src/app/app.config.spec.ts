import { HttpXsrfTokenExtractor } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { appConfig } from './app.config';

describe('appConfig', () => {
  afterEach(() => {
    document.cookie = 'cloudground-app-xsrf=; expires=Thu, 01 Jan 1970 00:00:00 GMT';
  });

  it('reads the XSRF token from the cookie the API sets', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });
    document.cookie = 'cloudground-app-xsrf=token-from-the-api';

    expect(TestBed.inject(HttpXsrfTokenExtractor).getToken()).toBe('token-from-the-api');
  });
});
