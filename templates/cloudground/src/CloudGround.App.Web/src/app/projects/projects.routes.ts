import { Routes } from '@angular/router';
import { MyProjects } from './my-projects';
import { PublicProjects } from './public-projects';

export const projectRoutes: Routes = [
  { path: 'explore', component: PublicProjects },
  { path: 'projects', component: MyProjects },
];
