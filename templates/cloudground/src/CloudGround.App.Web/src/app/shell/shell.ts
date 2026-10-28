import { HttpXsrfTokenExtractor } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
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

  protected readonly session = inject(SessionService);
  protected readonly loginUrl = computed(() => `/bff/login?returnUrl=${encodeURIComponent(this.router.url)}`);

  protected xsrfToken(): string {
    return this.xsrf.getToken() ?? '';
  }
}
