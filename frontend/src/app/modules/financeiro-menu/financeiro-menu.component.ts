import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { TabService } from '../../core/services/tab.service';

interface TileItem {
  label: string;
  sigla: string;
  icon: string;
  rota: string;
  iconKey: string;
}

const TILES: TileItem[] = [
  {
    label: 'Dashboard',
    sigla: 'DB',
    iconKey: 'chart',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>`,
    rota: '/erp/financeiro-dashboard'
  },
  {
    label: 'Contas a Pagar',
    sigla: 'CP',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 1v22M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/></svg>`,
    rota: '/erp/contas-pagar'
  },
  {
    label: 'Contas a Receber',
    sigla: 'CR',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/></svg>`,
    rota: '/erp/contas-receber'
  },
  {
    label: 'Controle Bancário',
    sigla: 'CB',
    iconKey: 'log',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>`,
    rota: '/erp/controle-bancario'
  },
  {
    label: 'Cofre',
    sigla: 'CF',
    iconKey: 'log',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/><circle cx="12" cy="16" r="1"/></svg>`,
    rota: '/erp/cofre'
  },
  {
    label: 'Adquirentes',
    sigla: 'AQ',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="4" width="22" height="16" rx="2"/><line x1="1" y1="10" x2="23" y2="10"/><circle cx="12" cy="15" r="2"/></svg>`,
    rota: '/erp/adquirentes'
  },
  {
    label: 'Conferência de Caixa',
    sigla: 'CC',
    iconKey: 'chart',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M9 12l2 2 4-4"/></svg>`,
    rota: '/erp/conferencia-caixa'
  },
  {
    label: 'Sangrias Pendentes',
    sigla: 'SP',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><line x1="12" y1="6" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`,
    rota: '/erp/sangrias-pendentes'
  },
];

@Component({
  selector: 'app-financeiro-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './financeiro-menu.component.html',
  styleUrl: './financeiro-menu.component.scss'
})
export class FinanceiroMenuComponent {
  tiles = TILES;

  constructor(
    private tabService: TabService,
    private sanitizer: DomSanitizer
  ) {}

  getIcon(svg: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  navegar(tile: TileItem) {
    this.tabService.abrirTab({
      id: tile.rota,
      titulo: tile.label,
      rota: tile.rota,
      iconKey: tile.iconKey,
    });
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
