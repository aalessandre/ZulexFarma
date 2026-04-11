import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { TabService } from '../../core/services/tab.service';

interface TileItem {
  label: string;
  sigla: string;
  icon: string;
  rota: string;
}

const TILES: TileItem[] = [
  {
    label: 'NCM',
    sigla: 'NC',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7V4a2 2 0 0 1 2-2h8.5L20 7.5V20a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-3"/><polyline points="14 2 14 8 20 8"/><line x1="2" y1="12" x2="12" y2="12"/><line x1="2" y1="16" x2="10" y2="16"/></svg>`,
    rota: '/erp/ncm'
  },
  {
    label: 'Locais',
    sigla: 'LC',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>`,
    rota: '/erp/locais'
  },
  {
    label: 'Plano de Contas',
    sigla: 'PC',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 3h7v7H3z"/><path d="M14 3h7v4H14z"/><path d="M14 10h7v4H14z"/><path d="M14 17h7v4H14z"/><path d="M3 13h7v8H3z"/></svg>`,
    rota: '/erp/plano-contas'
  },
  {
    label: 'Contas Bancárias',
    sigla: 'CB',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 21h18"/><path d="M3 10h18"/><path d="M5 6l7-3 7 3"/><line x1="4" y1="10" x2="4" y2="21"/><line x1="8" y1="10" x2="8" y2="21"/><line x1="12" y1="10" x2="12" y2="21"/><line x1="16" y1="10" x2="16" y2="21"/><line x1="20" y1="10" x2="20" y2="21"/></svg>`,
    rota: '/erp/contas-bancarias'
  },
  {
    label: 'Tipos de Pagamento',
    sigla: 'TP',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="14" rx="2"/><line x1="2" y1="10" x2="22" y2="10"/><path d="M6 15h4"/><path d="M14 15h2"/></svg>`,
    rota: '/erp/tipos-pagamento'
  },
];

@Component({
  selector: 'app-outros-cadastros',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './outros-cadastros.component.html',
  styleUrl: './outros-cadastros.component.scss'
})
export class OutrosCadastrosComponent {
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
      iconKey: 'gear',
    });
  }

  sairDaTela() {
    this.tabService.fecharTabAtiva();
  }
}
