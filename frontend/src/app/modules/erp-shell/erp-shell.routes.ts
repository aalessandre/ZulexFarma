import { Routes } from '@angular/router';
import { ErpShellComponent } from './erp-shell.component';
import { authGuard } from '../../core/guards/auth.guard';

export const ErpRoutes: Routes = [
  {
    path: '',
    component: ErpShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'filiais',
        loadComponent: () =>
          import('../filiais/filiais.component').then(m => m.FiliaisComponent),
      },
      {
        path: 'usuarios',
        loadComponent: () =>
          import('../usuarios/usuarios.component').then(m => m.UsuariosComponent),
      },
      {
        path: 'grupos',
        loadComponent: () =>
          import('../grupos/grupos.component').then(m => m.GruposComponent),
      },
      {
        path: 'colaboradores',
        loadComponent: () =>
          import('../colaboradores/colaboradores.component').then(m => m.ColaboradoresComponent),
      },
      {
        path: 'configuracoes',
        loadComponent: () =>
          import('../configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent),
      },
      {
        path: 'log-geral',
        loadComponent: () =>
          import('../log-geral/log-geral.component').then(m => m.LogGeralComponent),
      },
      {
        path: 'sync',
        loadComponent: () =>
          import('../sync/sync.component').then(m => m.SyncComponent),
      },
      {
        path: 'sistema',
        loadComponent: () =>
          import('../sistema/sistema.component').then(m => m.SistemaComponent),
      },
      {
        path: 'fornecedores',
        loadComponent: () =>
          import('../fornecedores/fornecedores.component').then(m => m.FornecedoresComponent),
      },
      // Placeholders — módulos futuros
      {
        path: '**',
        loadComponent: () =>
          import('./em-desenvolvimento.component').then(m => m.EmDesenvolvimentoComponent),
      },
    ],
  },
];
