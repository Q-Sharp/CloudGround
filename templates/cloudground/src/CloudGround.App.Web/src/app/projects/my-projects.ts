import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { SessionService } from '../session/session.service';
import { Project, ProjectsApi } from './projects.api';

@Component({
  selector: 'app-my-projects',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatListModule, ReactiveFormsModule],
  templateUrl: './my-projects.html',
})
export class MyProjects {
  private readonly api = inject(ProjectsApi);

  protected readonly session = inject(SessionService);
  protected readonly projects = signal<Project[]>([]);
  protected readonly message = signal<string | null>(null);
  protected readonly name = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] });

  constructor() {
    this.api.list().subscribe((projects) => this.projects.set(projects));
  }

  protected create(): void {
    this.api.create(this.name.value, null).subscribe({
      next: (project) => {
        this.projects.update((projects) => [...projects, project]);
        this.name.reset();
        this.message.set(null);
      },
      error: (error: HttpErrorResponse) =>
        this.message.set(error.status === 403 ? 'You may not create projects.' : 'The project could not be created.'),
    });
  }
}
