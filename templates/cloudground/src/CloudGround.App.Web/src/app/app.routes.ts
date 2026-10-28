import { Routes } from '@angular/router';
import { Home } from './home/home';
import { NotFound } from './not-found/not-found';
//#if (sample)
import { projectRoutes } from './projects/projects.routes';
//#endif

export const routes: Routes = [
  { path: '', component: Home },
  //#if (sample)
  ...projectRoutes,
  //#endif
  { path: '**', component: NotFound },
];
