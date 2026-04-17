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
    label: 'Hierarquia de Descontos',
    sigla: 'HD',
    iconKey: 'hierarquia',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="5" r="3"/><circle cx="6" cy="19" r="3"/><circle cx="18" cy="19" r="3"/><path d="M12 8v4"/><path d="M6 16v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2"/></svg>`,
    rota: '/erp/hierarquia-descontos'
  },
  {
    label: 'Hierarquia de Comissão',
    sigla: 'HC',
    iconKey: 'hierarquia',
    icon: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="5" r="3"/><circle cx="6" cy="19" r="3"/><circle cx="18" cy="19" r="3"/><path d="M12 8v4"/><path d="M6 16v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2"/></svg>`,
    rota: '/erp/hierarquia-comissoes'
  },
];

@Component({
  selector: 'app-hierarquias-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './hierarquias-menu.component.html',
  styleUrl: './hierarquias-menu.component.scss'
})
export class HierarquiasMenuComponent {
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
