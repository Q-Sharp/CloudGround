import { PlatformLocation } from '@angular/common';
import { FetchBackend, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { EMPTY } from 'rxjs';
import { API_ORIGIN, ApiOriginBackend } from './api-origin';

describe('ApiOriginBackend', () => {
  let sentUrls: string[];

  beforeEach(() => {
    sentUrls = [];
    vi.spyOn(FetchBackend.prototype, 'handle').mockImplementation((request) => {
      sentUrls.push(request.url);
      return EMPTY;
    });
    TestBed.configureTestingModule({
      providers: [
        ApiOriginBackend,
        { provide: API_ORIGIN, useValue: 'http://api.internal:5000' },
        { provide: PlatformLocation, useValue: { href: 'http://web.internal:4000/explore' } },
      ],
    });
  });

  afterEach(() => vi.restoreAllMocks());

  function send(url: string): string {
    TestBed.inject(ApiOriginBackend).handle(new HttpRequest('GET', url)).subscribe();
    return sentUrls[0];
  }

  it("sends the rendered page's API requests to the API", () => {
    expect(send('http://web.internal:4000/api/projects?page=2')).toBe('http://api.internal:5000/api/projects?page=2');
  });

  it('leaves requests to other hosts alone', () => {
    expect(send('https://elsewhere.example/api/projects')).toBe('https://elsewhere.example/api/projects');
  });

  it("leaves the page's other requests alone", () => {
    expect(send('http://web.internal:4000/assets/logo.svg')).toBe('http://web.internal:4000/assets/logo.svg');
  });
});
