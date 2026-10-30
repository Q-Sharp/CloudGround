import { HttpXsrfTokenExtractor } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { SessionService } from '../session/session.service';

@Component({
  selector: 'app-shell',
  imports: [MatButtonModule, MatToolbarModule, RouterLink, RouterOutlet],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly router = inject(Router);
  private readonly xsrf = inject(HttpXsrfTokenExtractor);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  protected readonly session = inject(SessionService);
  protected readonly loginUrl = computed(() => `/account/login?returnUrl=${encodeURIComponent(this.url())}`);

  protected xsrfToken(): string {
    return this.xsrf.getToken() ?? '';
  }
}
