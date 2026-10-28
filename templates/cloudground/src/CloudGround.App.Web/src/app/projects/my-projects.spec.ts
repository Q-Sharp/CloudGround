import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionService, User } from '../session/session.service';
import { MyProjects } from './my-projects';

describe('MyProjects', () => {
  async function render(user: User): Promise<{ page: HTMLElement; http: HttpTestingController; detect: () => Promise<void> }> {
    await TestBed.configureTestingModule({
      imports: [MyProjects],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SessionService, useValue: { user: signal(user), hasRole: (role: string) => user.roles.includes(role) } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(MyProjects);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/projects').flush([]);
    const detect = async () => {
      await fixture.whenStable();
      fixture.detectChanges();
    };
    await detect();
    return { page: fixture.nativeElement as HTMLElement, http, detect };
  }

  it('shows the create form for editors only', async () => {
    const editor = await render({ name: 'alice', roles: ['editor'] });
    expect(editor.page.querySelector('form[data-test=create]')).not.toBeNull();

    TestBed.resetTestingModule();
    const reader = await render({ name: 'bob', roles: [] });
    expect(reader.page.querySelector('form[data-test=create]')).toBeNull();
  });

  it('shows a message when creating is forbidden', async () => {
    const { page, http, detect } = await render({ name: 'alice', roles: ['editor'] });
    const input = page.querySelector<HTMLInputElement>('form[data-test=create] input')!;
    input.value = 'New project';
    input.dispatchEvent(new Event('input'));
    page.querySelector<HTMLFormElement>('form[data-test=create]')!.dispatchEvent(new Event('submit'));

    http.expectOne({ method: 'POST', url: '/api/projects' }).flush(null, { status: 403, statusText: 'Forbidden' });
    await detect();

    expect(page.querySelector('[role=alert]')?.textContent).toContain('You may not create projects.');
  });
});
