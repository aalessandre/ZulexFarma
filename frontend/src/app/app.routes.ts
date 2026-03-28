import { Routes } from '@angular/router';
import { BlankComponent } from './layouts/blank/blank.component';
import { FullComponent } from './layouts/full/full.component';

export const routes: Routes = [
  // ── Shell ERP (com abas) ─────────────────────────────────────────
  {
    path: 'erp',
    loadChildren: () =>
      import('./modules/erp-shell/erp-shell.routes').then(m => m.ErpRoutes),
  },

  // ── Redirect /dashboard para /erp (unificado) ───────────────────
  {
    path: 'dashboard',
    redirectTo: 'erp',
    pathMatch: 'full',
  },

  // ── Rotas sem sidebar ────────────────────────────────────────────
  {
    path: '',
    component: BlankComponent,
    children: [
      {
        path: 'authentication',
        loadChildren: () =>
          import('./pages/authentication/authentication.routes').then(
            (m) => m.AuthenticationRoutes
          ),
      },
    ],
  },

  // ── Rotas do template (mantidas para referência) ─────────────────
  {
    path: 'template',
    component: FullComponent,
    children: [
      {
        path: 'starter',
        loadChildren: () =>
          import('./pages/pages.routes').then((m) => m.PagesRoutes),
      },
      {
        path: 'dashboards',
        loadChildren: () =>
          import('./pages/dashboards/dashboards.routes').then((m) => m.DashboardsRoutes),
      },
      {
        path: 'forms',
        loadChildren: () =>
          import('./pages/forms/forms.routes').then((m) => m.FormsRoutes),
      },
      {
        path: 'charts',
        loadChildren: () =>
          import('./pages/charts/charts.routes').then((m) => m.ChartsRoutes),
      },
      {
        path: 'apps',
        loadChildren: () =>
          import('./pages/apps/apps.routes').then((m) => m.AppsRoutes),
      },
      {
        path: 'widgets',
        loadChildren: () =>
          import('./pages/widgets/widgets.routes').then((m) => m.WidgetsRoutes),
      },
      {
        path: 'tables',
        loadChildren: () =>
          import('./pages/tables/tables.routes').then((m) => m.TablesRoutes),
      },
      {
        path: 'datatable',
        loadChildren: () =>
          import('./pages/datatable/datatable.routes').then((m) => m.DatatablesRoutes),
      },
      {
        path: 'theme-pages',
        loadChildren: () =>
          import('./pages/theme-pages/theme-pages.routes').then((m) => m.ThemePagesRoutes),
      },
      {
        path: 'ui-components',
        loadChildren: () =>
          import('./pages/ui-components/ui-components.routes').then((m) => m.UiComponentsRoutes),
      },
    ],
  },

  // ── Redirects ────────────────────────────────────────────────────
  {
    path: '',
    redirectTo: 'authentication/login',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'authentication/login',
  },
];
