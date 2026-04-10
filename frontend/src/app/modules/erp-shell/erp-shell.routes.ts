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
        path: 'financeiro',
        loadComponent: () =>
          import('../financeiro-menu/financeiro-menu.component').then(m => m.FinanceiroMenuComponent),
      },
      {
        path: 'contas-pagar',
        loadComponent: () =>
          import('../contas-pagar/contas-pagar.component').then(m => m.ContasPagarComponent),
      },
      {
        path: 'contas-bancarias',
        loadComponent: () =>
          import('../contas-bancarias/contas-bancarias.component').then(m => m.ContasBancariasComponent),
      },
      {
        path: 'plano-contas',
        loadComponent: () =>
          import('../plano-contas/plano-contas.component').then(m => m.PlanoContasComponent),
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
        path: 'ibptax',
        loadComponent: () =>
          import('../ibptax/ibptax.component').then(m => m.IbptaxComponent),
      },
      {
        path: 'atualizacao-precos',
        loadComponent: () =>
          import('../atualizacao-precos/atualizacao-precos.component').then(m => m.AtualizacaoPrecosComponent),
      },
      {
        path: 'promocoes',
        loadComponent: () =>
          import('../promocoes-menu/promocoes-menu.component').then(m => m.PromocoesMenuComponent),
      },
      {
        path: 'promocao-progressiva',
        loadComponent: () =>
          import('../promocao-progressiva/promocao-progressiva.component').then(m => m.PromocaoProgressivaComponent),
      },
      {
        path: 'promocao-fixa',
        loadComponent: () =>
          import('../promocao-fixa/promocao-fixa.component').then(m => m.PromocaoFixaComponent),
      },
      {
        path: 'convenios',
        loadComponent: () =>
          import('../convenios/convenios.component').then(m => m.ConveniosComponent),
      },
      {
        path: 'pre-venda',
        loadComponent: () =>
          import('../pre-venda/pre-venda.component').then(m => m.PreVendaComponent),
      },
      {
        path: 'caixa',
        loadComponent: () =>
          import('../caixa/caixa.component').then(m => m.CaixaComponent),
      },
      {
        path: 'hierarquia-descontos',
        loadComponent: () =>
          import('../hierarquia-descontos/hierarquia-descontos.component').then(m => m.HierarquiaDescontosComponent),
      },
      {
        path: 'adquirentes',
        loadComponent: () =>
          import('../adquirentes/adquirentes.component').then(m => m.AdquirentesComponent),
      },
      {
        path: 'contas-receber',
        loadComponent: () =>
          import('../contas-receber/contas-receber.component').then(m => m.ContasReceberComponent),
      },
      {
        path: 'tipos-pagamento',
        loadComponent: () =>
          import('../tipos-pagamento/tipos-pagamento.component').then(m => m.TiposPagamentoComponent),
      },
      {
        path: 'todo-board',
        loadComponent: () =>
          import('../todo-board/todo-board.component').then(m => m.TodoBoardComponent),
      },
      {
        path: 'clientes',
        loadComponent: () =>
          import('../clientes/clientes.component').then(m => m.ClientesComponent),
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
