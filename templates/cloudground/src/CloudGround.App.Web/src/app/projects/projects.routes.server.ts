import { RenderMode, ServerRoute } from '@angular/ssr';

export const projectServerRoutes: ServerRoute[] = [
  { path: 'explore', renderMode: RenderMode.Server },
  { path: 'projects', renderMode: RenderMode.Client },
];
