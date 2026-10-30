import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

export interface User {
  name: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);

  readonly user = signal<User | null>(null);

  constructor() {
    if (isPlatformBrowser(inject(PLATFORM_ID))) {
      this.http.get<User>('/account/user').subscribe({
        next: (user) => this.user.set(user),
        error: () => this.user.set(null),
      });
    }
  }

  hasRole(role: string): boolean {
    return this.user()?.roles.includes(role) ?? false;
  }
}
