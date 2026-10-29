import { Routes } from '@angular/router';
import { RenderMode, ServerRoute } from '@angular/ssr';
import { MyProjects } from './my-projects';
import { PublicProjects } from './public-projects';

export const projectRoutes: Routes = [
  { path: 'explore', component: PublicProjects },
  { path: 'projects', component: MyProjects },
];

export const projectServerRoutes: ServerRoute[] = [
  { path: 'explore', renderMode: RenderMode.Server },
  { path: 'projects', renderMode: RenderMode.Client },
];
