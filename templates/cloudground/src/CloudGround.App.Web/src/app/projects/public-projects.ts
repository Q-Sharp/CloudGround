import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatListModule } from '@angular/material/list';
import { catchError, of } from 'rxjs';
import { ProjectsApi } from './projects.api';

@Component({
  selector: 'app-public-projects',
  imports: [MatListModule],
  template: `
    <h1>All projects</h1>
    @if (projects(); as list) {
      <mat-list>
        @for (project of list; track project.id) {
          <mat-list-item>{{ project.name }}</mat-list-item>
        } @empty {
          <p>No projects yet.</p>
        }
      </mat-list>
    } @else if (projects() === null) {
      <p role="alert">The projects could not be loaded.</p>
    }
  `,
})
export class PublicProjects {
  protected readonly projects = toSignal(inject(ProjectsApi).list().pipe(catchError(() => of(null))));
}
