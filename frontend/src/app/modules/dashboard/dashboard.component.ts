import { Component, computed, signal, HostListener, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';
import { ErpSettingsService } from '../../core/services/erp-settings.service';

const ICONS: Record<string, string> = {
  cart:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>`,
  vendas:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>`,
  handshake: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"/><path d="M8 12h8"/><path d="M12 8v8"/></svg>`,
  box:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>`,
  wallet:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="4" width="20" height="16" rx="2"/><path d="M22 10H18a2 2 0 0 0 0 4h4"/><circle cx="18" cy="12" r="0.5" fill="currentColor"/></svg>`,
  user:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>`,
  cash:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 20h18a1 1 0 0 0 1-1v-3H2v3a1 1 0 0 0 1 1z"/><path d="M2 16V10a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v6"/><rect x="14" y="4" width="5" height="3" rx="0.5"/><line x1="16.5" y1="7" x2="16.5" y2="4"/><rect x="4" y="3" width="7" height="8" rx="1"/><line x1="6" y1="5.5" x2="9" y2="5.5"/><line x1="6" y1="7.5" x2="9" y2="7.5"/><line x1="6" y1="9.5" x2="9" y2="9.5"/><rect x="13" y="10" width="6" height="3" rx="0.5"/><circle cx="11" cy="18" r="0.7" fill="currentColor"/></svg>`,
  cart2:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/></svg>`,
  dollar:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>`,
  users:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>`,
  pill:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z"/><line x1="8.5" y1="8.5" x2="15.5" y2="15.5"/></svg>`,
  gerenciar: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2l3 3-3 3-3-3 3-3z"/><path d="M9 5l3 3 3-3"/><path d="M12 8v1"/><path d="M2 15l3-5 3 2-2 5-2 4h-1z"/><path d="M22 15l-3-5-3 2 2 5 2 4h1z"/></svg>`,
  truck:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="1" y="3" width="15" height="13"/><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></svg>`,
  chart:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>`,
  chartpie:  `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21.21 15.89A10 10 0 1 1 8 2.83"/><path d="M22 12A10 10 0 0 0 12 2v10z"/></svg>`,
  log:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>`,
  eye:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>`,
  wrench:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>`,
  sync:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 2v6h-6"/><path d="M3 12a9 9 0 0 1 15-6.7L21 8"/><path d="M3 22v-6h6"/><path d="M21 12a9 9 0 0 1-15 6.7L3 16"/></svg>`,
  gear:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>`,
  building:  `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 21h18"/><path d="M5 21V7l8-4v18"/><path d="M19 21V11l-6-4"/><path d="M9 9v.01M9 12v.01M9 15v.01M9 18v.01"/></svg>`,
  lock:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>`,
  grid:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/></svg>`,
  tag:       `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>`,
  flask:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M9 3h6"/><path d="M10 3v6.5L4 18a1 1 0 0 0 .85 1.5h14.3A1 1 0 0 0 20 18l-6-8.5V3"/></svg>`,
  conv:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1"/><path d="M9 14l2 2 4-4"/></svg>`,
  estoque:   `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M20 12V8H6a2 2 0 0 1-2-2c0-1.1.9-2 2-2h12v4"/><path d="M4 6v12c0 1.1.9 2 2 2h14v-4"/><path d="M18 12a2 2 0 0 0 0 4h4v-4h-4z"/></svg>`,
  fiscal:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 2v20l4-2 4 2 4-2 4 2V2l-4 2-4-2-4 2L4 2z"/><path d="M8 10h8"/><path d="M8 14h4"/></svg>`,
  promo:     `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2L15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2z"/></svg>`,
  help:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
  database:  `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>`,
  todo:      `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 3v18"/><path d="M13 8h5"/><path d="M13 12h5"/><path d="M13 16h5"/></svg>`,
  hierarquia:`<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="5" r="3"/><circle cx="6" cy="19" r="3"/><circle cx="18" cy="19" r="3"/><path d="M12 8v4"/><path d="M6 16v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2"/></svg>`,
  sistema:   `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>`,
  precos:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/><path d="M12 2L2 7l10 5 10-5L12 2z"/></svg>`,
  contacliente: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>`,
};

export interface TileItem {
  label: string;
  sigla: string;
  iconKey: string;
  rota: string;
  tamanho?: 'normal' | 'largo';
}

export interface BlocoTiles {
  nome: string;
  cor: string;
  tiles: TileItem[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  usuario = computed(() => this.authService.usuarioLogado());
  painelConfig = signal(false);

  blocos: BlocoTiles[] = [
    {
      nome: 'Movimento',
      cor: '#00acc1',
      tiles: [
        { label: 'Pré-Venda',        sigla: 'PV', iconKey: 'cart',      rota: '/erp/pre-venda' },
        { label: 'Vendas',           sigla: 'VE', iconKey: 'vendas',   rota: '/erp/vendas' },
        { label: 'Emprestimo',       sigla: 'EP', iconKey: 'handshake', rota: '/erp/emprestimo' },
        { label: 'Mov. de Estoque',  sigla: 'ME', iconKey: 'estoque',  rota: '/erp/movimentacao-estoque' },
        { label: 'Carteira Digital', sigla: 'CD', iconKey: 'wallet',    rota: '/erp/carteira' },
        { label: 'Conta do Cliente', sigla: 'CC', iconKey: 'contacliente', rota: '/erp/conta-cliente' },
        { label: 'Caixa',            sigla: 'CX', iconKey: 'cash',      rota: '/erp/caixa' },
        { label: 'Compras',          sigla: 'CP', iconKey: 'cart2',     rota: '/erp/compras' },
        { label: 'Financeiro',       sigla: 'FN', iconKey: 'dollar',    rota: '/erp/financeiro' },
        { label: 'Fiscal',           sigla: 'FS', iconKey: 'fiscal',    rota: '/erp/fiscal' },
        { label: 'Promoções',        sigla: 'PM', iconKey: 'promo',     rota: '/erp/promocoes' },
      ]
    },
    {
      nome: 'Cadastros',
      cor: '#e65100',
      tiles: [
        { label: 'Clientes',         sigla: 'CL', iconKey: 'users',     rota: '/erp/clientes' },
        { label: 'Colaboradores',    sigla: 'CO', iconKey: 'user',      rota: '/erp/colaboradores' },
        { label: 'Gerenciar Produtos', sigla: 'GP', iconKey: 'gerenciar', rota: '/erp/gerenciar-produtos' },
        { label: 'Fornecedores',     sigla: 'FO', iconKey: 'truck',     rota: '/erp/fornecedores' },
        { label: 'Fabricantes',      sigla: 'FB', iconKey: 'box',       rota: '/erp/fabricantes' },
        { label: 'Substancias',      sigla: 'SB', iconKey: 'flask',     rota: '/erp/substancias' },
        { label: 'Convenios',        sigla: 'CV', iconKey: 'conv',      rota: '/erp/convenios' },
        { label: 'Outros',           sigla: 'OT', iconKey: 'grid',      rota: '/erp/outros-cadastros' },
      ]
    },
    {
      nome: 'Relatorios',
      cor: '#6a1b9a',
      tiles: [
        { label: 'Analise de Vendas',   sigla: 'AV', iconKey: 'chart',    rota: '/erp/rel/vendas' },
        { label: 'Analise de Produtos', sigla: 'AP', iconKey: 'chartpie', rota: '/erp/rel/produtos' },
        { label: 'Log de Auditoria',    sigla: 'LA', iconKey: 'eye',     rota: '/erp/log-geral' },
      ]
    },
    {
      nome: 'Manutencao',
      cor: '#f9a825',
      tiles: [
        { label: 'Manutencao',        sigla: 'MT', iconKey: 'wrench',     rota: '/erp/manutencao' },
        { label: 'Sincronismo',      sigla: 'SI', iconKey: 'sync',      rota: '/erp/sync' },
        { label: 'Grupo de Usuarios', sigla: 'GU', iconKey: 'lock',     rota: '/erp/grupos' },
        { label: 'Usuarios',          sigla: 'US', iconKey: 'users',    rota: '/erp/usuarios' },
        { label: 'Filiais',           sigla: 'FL', iconKey: 'building', rota: '/erp/filiais' },
        { label: 'Sistema',           sigla: 'ST', iconKey: 'sistema',  rota: '/erp/sistema' },
        { label: 'Configuracoes',     sigla: 'CF', iconKey: 'gear',     rota: '/erp/configuracoes' },
        { label: 'Atual. Precos',    sigla: 'AP', iconKey: 'precos',   rota: '/erp/atualizacao-precos' },
        { label: 'Hierarquia Descontos', sigla: 'HD', iconKey: 'hierarquia', rota: '/erp/hierarquia-descontos' },
      ]
    },
    {
      nome: 'Dev',
      cor: '#546e7a',
      tiles: [
        { label: 'Help',            sigla: 'HP', iconKey: 'help',      rota: '/erp/help' },
        { label: 'Dic. de Dados',   sigla: 'DD', iconKey: 'database',  rota: '/erp/dicionario-dados' },
        { label: 'Todo Board',      sigla: 'TD', iconKey: 'todo',      rota: '/erp/todo-board' },
      ]
    }
  ];

  constructor(
    private authService: AuthService,
    public tabService: TabService,
    public settings: ErpSettingsService,
    private router: Router,
    private sanitizer: DomSanitizer,
    private elRef: ElementRef
  ) {}

  @HostListener('wheel', ['$event'])
  onWheel(e: WheelEvent) {
    const dashMain = this.elRef.nativeElement.querySelector('.dash-main') as HTMLElement;
    if (!dashMain) return;
    if (dashMain.scrollWidth > dashMain.clientWidth) {
      e.preventDefault();
      dashMain.scrollLeft += e.deltaY;
    }
  }

  getIcon(key: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(ICONS[key] ?? ICONS['gear']);
  }

  navegar(tile: TileItem) {
    this.tabService.abrirTab({
      id: tile.rota,
      titulo: tile.label,
      rota: tile.rota,
      iconKey: tile.iconKey,
    });
  }
}
