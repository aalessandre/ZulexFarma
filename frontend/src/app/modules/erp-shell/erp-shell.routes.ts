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
        path: '',
        loadComponent: () =>
          import('../dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
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
      {
        path: 'fabricantes',
        loadComponent: () =>
          import('../fabricantes/fabricantes.component').then(m => m.FabricantesComponent),
      },
      {
        path: 'substancias',
        loadComponent: () =>
          import('../substancias/substancias.component').then(m => m.SubstanciasComponent),
      },
      {
        path: 'gerenciar-produtos',
        loadComponent: () =>
          import('../produtos/produtos.component').then(m => m.ProdutosComponent),
      },
      {
        path: 'dicionario-dados',
        loadComponent: () =>
          import('../dicionario-dados/dicionario-dados.component').then(m => m.DicionarioDadosComponent),
      },
      {
        path: 'help',
        loadComponent: () =>
          import('../help/help.component').then(m => m.HelpComponent),
      },
      {
        path: 'ncm',
        loadComponent: () =>
          import('../ncm/ncm.component').then(m => m.NcmComponent),
      },
      {
        path: 'locais',
        loadComponent: () =>
          import('../locais/locais.component').then(m => m.LocaisComponent),
      },
      {
        path: 'outros-cadastros',
        loadComponent: () =>
          import('../outros-cadastros/outros-cadastros.component').then(m => m.OutrosCadastrosComponent),
      },
      {
        path: 'compras',
        loadComponent: () =>
          import('../compras-menu/compras-menu.component').then(m => m.ComprasMenuComponent),
      },
      {
        path: 'lancar-compras',
        loadComponent: () =>
          import('../compras/compras.component').then(m => m.ComprasComponent),
      },
      {
        path: 'fiscal',
        loadComponent: () =>
          import('../fiscal-menu/fiscal-menu.component').then(m => m.FiscalMenuComponent),
      },
      {
        path: 'icms-uf',
        loadComponent: () =>
          import('../icms-uf/icms-uf.component').then(m => m.IcmsUfComponent),
      },
      {
        path: 'atualizacao-precos',
        loadComponent: () =>
          import('../atualizacao-precos/atualizacao-precos.component').then(m => m.AtualizacaoPrecosComponent),
      },
      {
        path: 'consultar-sefaz',
        loadComponent: () =>
          import('../consultar-sefaz/consultar-sefaz.component').then(m => m.ConsultarSefazComponent),
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
