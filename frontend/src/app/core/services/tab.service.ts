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
  readonly MAX_TABS = 15;

  tabs = signal<Tab[]>([]);
  tabAtiva = signal<string>('');

  /** Incrementa quando o limite é atingido — o shell observa para mostrar aviso */
  limiteAtingido = signal(0);

  /** Histórico de IDs de abas visitadas (para o botão voltar) */
  private historico: string[] = [];

  /** Callbacks registrados por componentes para interceptar fechamento de tab */
  private beforeCloseHandlers = new Map<string, () => Promise<boolean>>();

  constructor(private router: Router) {}

  /** Registra um callback que será chamado antes de fechar a tab. Retorna false para cancelar. */
  registrarBeforeClose(tabId: string, handler: () => Promise<boolean>): void {
    this.beforeCloseHandlers.set(tabId, handler);
  }

  /** Remove o callback de interceptação */
  removerBeforeClose(tabId: string): void {
    this.beforeCloseHandlers.delete(tabId);
  }

  abrirTab(tab: Tab): void {
    const existente = this.tabs().find(t => t.id === tab.id);
    if (!existente) {
      if (this.tabs().length >= this.MAX_TABS) {
        this.limiteAtingido.update(v => v + 1);
        return;
      }
      this.tabs.update(ts => [...ts, tab]);
    }
    this.pushHistorico(tab.id);
    this.tabAtiva.set(tab.id);
    this.router.navigate([tab.rota]);
  }

  async fecharTab(id: string, event?: MouseEvent): Promise<void> {
    event?.stopPropagation();
    const lista = this.tabs();
    const idx = lista.findIndex(t => t.id === id);
    if (idx === -1) return;

    // Verificar se o componente permite fechar
    const handler = this.beforeCloseHandlers.get(id);
    if (handler) {
      const podeFechar = await handler();
      if (!podeFechar) return;
      this.beforeCloseHandlers.delete(id);
    }

    const novaLista = lista.filter(t => t.id !== id);
    this.tabs.set(novaLista);
    this.historico = this.historico.filter(h => h !== id);

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
    this.fecharTab(id);
  }

  ativarTab(id: string, rota: string): void {
    this.pushHistorico(id);
    this.tabAtiva.set(id);
    this.router.navigate([rota]);
  }

  /** Volta para a aba visitada anteriormente, ou para a home se for a última */
  voltar(): void {
    if (this.historico.length < 2) {
      // Última aba no histórico — volta pra home
      this.historico = [];
      this.tabAtiva.set('');
      this.router.navigate(['/erp']);
      return;
    }
    this.historico.pop(); // remove a atual
    const anteriorId = this.historico[this.historico.length - 1];
    const tab = this.tabs().find(t => t.id === anteriorId);
    if (tab) {
      this.tabAtiva.set(tab.id);
      this.router.navigate([tab.rota]);
    }
  }

  podeVoltar(): boolean {
    return this.tabAtiva() !== '';
  }

  fecharTodas(): void {
    this.tabs.set([]);
    this.tabAtiva.set('');
    this.historico = [];
  }

  private pushHistorico(id: string): void {
    if (this.historico[this.historico.length - 1] !== id) {
      this.historico.push(id);
      if (this.historico.length > 50) this.historico.shift();
    }
  }
}
