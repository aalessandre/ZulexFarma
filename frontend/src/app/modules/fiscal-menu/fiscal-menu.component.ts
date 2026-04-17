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
    label: 'NF-e Emitidas',
    sigla: 'NE',
    iconKey: 'fiscal',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>`,
    rota: '/erp/nfe-lista'
  },
  {
    label: 'Emitir NF-e',
    sigla: 'EN',
    iconKey: 'fiscal',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="12" y1="18" x2="12" y2="12"/><line x1="9" y1="15" x2="15" y2="15"/></svg>`,
    rota: '/erp/nfe-emissao'
  },
  {
    label: 'ICMS por UF',
    sigla: 'IC',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/></svg>`,
    rota: '/erp/icms-uf'
  },
  {
    label: 'Tabela IBPTax',
    sigla: 'IB',
    iconKey: 'percent',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><line x1="19" y1="5" x2="5" y2="19"/><circle cx="6.5" cy="6.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/></svg>`,
    rota: '/erp/ibptax'
  },
  {
    label: 'Natureza de Operacao',
    sigla: 'NO',
    iconKey: 'fiscal',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 2v20l4-2 4 2 4-2 4 2V2l-4 2-4-2-4 2L4 2z"/><path d="M8 10h8"/><path d="M8 14h4"/></svg>`,
    rota: '/erp/natureza-operacao'
  },
];

@Component({
  selector: 'app-fiscal-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './fiscal-menu.component.html',
  styleUrl: './fiscal-menu.component.scss'
})
export class FiscalMenuComponent {
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
