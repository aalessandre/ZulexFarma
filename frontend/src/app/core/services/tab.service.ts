import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';

export interface Tab {
  id: string;
  titulo: string;
  rota: string;
  iconKey: string;
}

@Injectable({ providedIn: 'root' })
export class TabService {
  tabs = signal<Tab[]>([]);
  tabAtiva = signal<string>('');

  constructor(private router: Router) {}

  abrirTab(tab: Tab): void {
    const existente = this.tabs().find(t => t.id === tab.id);
    if (!existente) {
      this.tabs.update(ts => [...ts, tab]);
    }
    this.tabAtiva.set(tab.id);
    this.router.navigate([tab.rota]);
  }

  fecharTab(id: string, event: MouseEvent): void {
    event.stopPropagation();
    const lista = this.tabs();
    const idx = lista.findIndex(t => t.id === id);
    if (idx === -1) return;

    const novaLista = lista.filter(t => t.id !== id);
    this.tabs.set(novaLista);

    if (this.tabAtiva() === id) {
      if (novaLista.length > 0) {
        const proxima = novaLista[Math.min(idx, novaLista.length - 1)];
        this.tabAtiva.set(proxima.id);
        this.router.navigate([proxima.rota]);
      } else {
        this.tabAtiva.set('');
        this.router.navigate(['/erp']);
      }
    }
  }

  fecharTabAtiva(): void {
    const id = this.tabAtiva();
    if (!id) return;
    const lista = this.tabs();
    const idx = lista.findIndex(t => t.id === id);
    if (idx === -1) return;

    const novaLista = lista.filter(t => t.id !== id);
    this.tabs.set(novaLista);

    if (novaLista.length > 0) {
      const proxima = novaLista[Math.min(idx, novaLista.length - 1)];
      this.tabAtiva.set(proxima.id);
      this.router.navigate([proxima.rota]);
    } else {
      this.tabAtiva.set('');
      this.router.navigate(['/erp']);
    }
  }

  ativarTab(id: string, rota: string): void {
    this.tabAtiva.set(id);
    this.router.navigate([rota]);
  }

  fecharTodas(): void {
    this.tabs.set([]);
    this.tabAtiva.set('');
  }
}
