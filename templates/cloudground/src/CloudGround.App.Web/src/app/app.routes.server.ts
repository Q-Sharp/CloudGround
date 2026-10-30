import { RenderMode, ServerRoute } from '@angular/ssr';
//#if (sample)
import { projectServerRoutes } from './projects/projects.routes.server';
//#endif

export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server },
  //#if (sample)
  ...projectServerRoutes,
  //#endif
  { path: '**', renderMode: RenderMode.Server, status: 404 },
];
