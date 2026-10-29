import { HttpXsrfTokenExtractor, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Shell } from './shell';

@Component({ template: '' })
class Page {}

describe('Shell', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Shell],
      providers: [
        provideRouter([{ path: 'somewhere', component: Page }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: HttpXsrfTokenExtractor, useValue: { getToken: () => 'xsrf-token' } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  async function render(user: { name: string; roles: string[] } | null): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(Shell);
    const request = http.expectOne('/bff/user');
    if (user) {
      request.flush(user);
    } else {
      request.flush(null, { status: 401, statusText: 'Unauthorized' });
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the login link when nobody is signed in', async () => {
    const page = await render(null);

    const login = page.querySelector<HTMLAnchorElement>('a[data-test=login]');
    expect(login?.getAttribute('href')).toBe('/bff/login?returnUrl=%2F');
    expect(page.querySelector('form[data-test=logout]')).toBeNull();
  });

  it('points the login link back to the page the user navigated to', async () => {
    const fixture = TestBed.createComponent(Shell);
    http.expectOne('/bff/user').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    await TestBed.inject(Router).navigateByUrl('/somewhere');
    await fixture.whenStable();

    const login = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('a[data-test=login]');
    expect(login?.getAttribute('href')).toBe('/bff/login?returnUrl=%2Fsomewhere');
  });

  it("shows the user's name and a logout form when signed in", async () => {
    const page = await render({ name: 'alice', roles: ['editor'] });

    expect(page.querySelector('[data-test=user]')?.textContent).toContain('alice');
    const logout = page.querySelector<HTMLFormElement>('form[data-test=logout]');
    expect(logout?.getAttribute('action')).toBe('/bff/logout');
    expect(logout?.getAttribute('method')).toBe('post');
    expect(page.querySelector('a[data-test=login]')).toBeNull();
  });

  it('sends the XSRF token with the logout form', async () => {
    const page = await render({ name: 'alice', roles: [] });

    const token = page.querySelector<HTMLInputElement>('form[data-test=logout] input[name=__RequestVerificationToken]');
    expect(token?.value).toBe('xsrf-token');
  });
});
