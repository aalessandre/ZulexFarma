import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { TabService } from '../../core/services/tab.service';

interface TileItem { label: string; sigla: string; icon: string; rota: string; iconKey: string; }

const TILES: TileItem[] = [
  {
    label: 'Promoção Fixa', sigla: 'PF', iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20.59 13.41l-7.17 7.17a2 2 0 01-2.83 0L2 12V2h10l8.59 8.59a2 2 0 010 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>`,
    rota: '/erp/promocao-fixa'
  },
  {
    label: 'Promoção Progressiva', sigla: 'PP', iconKey: 'chart',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>`,
    rota: '/erp/promocao-progressiva'
  },
];

@Component({
  selector: 'app-promocoes-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './promocoes-menu.component.html',
  styleUrl: './promocoes-menu.component.scss'
})
export class PromocoesMenuComponent {
  tiles = TILES;
  constructor(private tabService: TabService, private sanitizer: DomSanitizer) {}
  getIcon(svg: string): SafeHtml { return this.sanitizer.bypassSecurityTrustHtml(svg); }
  navegar(tile: TileItem) { this.tabService.abrirTab({ id: tile.rota, titulo: tile.label, rota: tile.rota, iconKey: tile.iconKey }); }
  sairDaTela() { this.tabService.fecharTabAtiva(); }
}
