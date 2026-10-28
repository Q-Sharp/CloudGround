import { Routes } from '@angular/router';
import { RenderMode, ServerRoute } from '@angular/ssr';
import { PublicProjects } from './public-projects';

export const projectRoutes: Routes = [{ path: 'explore', component: PublicProjects }];

export const projectServerRoutes: ServerRoute[] = [{ path: 'explore', renderMode: RenderMode.Server }];
