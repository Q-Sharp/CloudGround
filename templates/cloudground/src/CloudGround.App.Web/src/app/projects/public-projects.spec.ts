import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PublicProjects } from './public-projects';

describe('PublicProjects', () => {
  it('lists the projects it receives', async () => {
    await TestBed.configureTestingModule({
      imports: [PublicProjects],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(PublicProjects);

    TestBed.inject(HttpTestingController)
      .expectOne('/api/projects')
      .flush([{ id: '1', name: 'Website relaunch', description: null, createdAt: '2026-10-28T09:00:00Z', archivedAt: null }]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Website relaunch');
  });
});
