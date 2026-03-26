import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const DashboardRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./dashboard.component').then((m) => m.DashboardComponent),
  },
];
