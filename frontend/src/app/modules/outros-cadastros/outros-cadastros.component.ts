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
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>`,
    rota: '/erp/ncm'
  },
  {
    label: 'Locais',
    sigla: 'LC',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>`,
    rota: '/erp/locais'
  },
  {
    label: 'Plano de Contas',
    sigla: 'PC',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M2 3h6a4 4 0 014 4v14a3 3 0 00-3-3H2z"/><path d="M22 3h-6a4 4 0 00-4 4v14a3 3 0 013-3h7z"/></svg>`,
    rota: '/erp/plano-contas'
  },
  {
    label: 'Contas Bancárias',
    sigla: 'CB',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>`,
    rota: '/erp/contas-bancarias'
  },
  {
    label: 'Tipos de Pagamento',
    sigla: 'TP',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/><circle cx="17" cy="16" r="2"/></svg>`,
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
