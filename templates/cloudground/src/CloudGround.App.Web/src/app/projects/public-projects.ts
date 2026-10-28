import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatListModule } from '@angular/material/list';
import { ProjectsApi } from './projects.api';

@Component({
  selector: 'app-public-projects',
  imports: [MatListModule],
  template: `
    <h1>All projects</h1>
    <mat-list>
      @for (project of projects(); track project.id) {
        <mat-list-item>{{ project.name }}</mat-list-item>
      } @empty {
        <p>No projects yet.</p>
      }
    </mat-list>
  `,
})
export class PublicProjects {
  protected readonly projects = toSignal(inject(ProjectsApi).list(), { initialValue: [] });
}
