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
    label: 'Campanhas de Pontos',
    sigla: 'CP',
    iconKey: 'star',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>`,
    rota: '/erp/fidelidade-pontos'
  },
  {
    label: 'Campanhas de Cashback',
    sigla: 'CB',
    iconKey: 'dollar',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><path d="M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8"/><path d="M12 18V6"/></svg>`,
    rota: '/erp/fidelidade-cashback'
  },
  {
    label: 'Catálogo de Prêmios',
    sigla: 'CT',
    iconKey: 'gift',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="20 12 20 22 4 22 4 12"/><rect x="2" y="7" width="20" height="5"/><line x1="12" y1="22" x2="12" y2="7"/><path d="M12 7H7.5a2.5 2.5 0 010-5C11 2 12 7 12 7z"/><path d="M12 7h4.5a2.5 2.5 0 000-5C13 2 12 7 12 7z"/></svg>`,
    rota: '/erp/fidelidade-premios'
  }
];

@Component({
  selector: 'app-fidelidade-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './fidelidade-menu.component.html',
  styleUrl: './fidelidade-menu.component.scss'
})
export class FidelidadeMenuComponent {
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
