import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Project {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
  archivedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProjectsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Project[]> {
    return this.http.get<Project[]>('/api/projects');
  }

  create(name: string, description: string | null): Observable<Project> {
    return this.http.post<Project>('/api/projects', { name, description });
  }
}
