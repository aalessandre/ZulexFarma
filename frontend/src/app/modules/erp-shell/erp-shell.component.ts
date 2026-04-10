import { Component, computed, signal, effect, HostListener, ElementRef, ViewChild, OnDestroy } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { TabService } from '../../core/services/tab.service';
import { ErpSettingsService, FonteEscala, Tema } from '../../core/services/erp-settings.service';
import { ModalGlobalComponent } from '../../core/components/modal-global.component';
import { CassiComponent } from '../cassi/cassi.component';
import { ModalService } from '../../core/services/modal.service';
import { environment } from '../../../environments/environment';

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

interface ResultadoBusca {
  tile: TileItem;
  bloco: string;
  cor: string;
}

@Component({
  selector: 'app-erp-shell',
  standalone: true,
  imports: [RouterOutlet, CommonModule, FormsModule, ModalGlobalComponent, CassiComponent],
  templateUrl: './erp-shell.component.html',
  styleUrl: './erp-shell.component.scss'
})
export class ErpShellComponent {
  usuario = computed(() => this.authService.usuarioLogado());

  painelAberto = signal(false);
  painelAbasAberto = signal(false);
  nomeSistema = 'ZulexPharma';

  // ── Sync status ────────────────────────────────────────────────
  syncStatus = signal<any>(null);
  private syncIntervalId: any = null;

  syncIndicador = computed(() => {
    const s = this.syncStatus();
    if (!s || !s.rodando) return 'desabilitado';
    if (s.falhasConsecutivas >= 3) return 'offline';
    if (s.ultimoStatus === 'ERRO') return 'vermelho';
    if (s.pendentesEnvio > 0) return 'amarelo';
    return 'verde';
  });

  syncTooltip = computed(() => {
    const s = this.syncStatus();
    const ind = this.syncIndicador();
    if (ind === 'verde') return `Sync OK | Pendentes: ${s?.pendentesLocal ?? 0}`;
    if (ind === 'amarelo') return `Sync: ${s?.pendentesEnvio} pendentes de envio`;
    if (ind === 'vermelho') return `Sync com erro`;
    if (ind === 'offline') return `Sync offline`;
    return 'Sync desabilitado';
  });

  telaAtual = computed(() => {
    const id = this.tabService.tabAtiva();
    const tab = this.tabService.tabs().find(t => t.id === id);
    return tab ? tab.titulo : '';
  });

  estaHome = signal(true);

  // ── Blocos (tiles data for search) ──────────────────────────────
  blocos: BlocoTiles[] = [
    {
      nome: 'Movimento',
      cor: '#00acc1',
      tiles: [
        { label: 'Pre-Venda',        sigla: 'PV', iconKey: 'cart',      rota: '/erp/pre-venda' },
        { label: 'Vendas',           sigla: 'VE', iconKey: 'cart',      rota: '/erp/vendas' },
        { label: 'Emprestimo',       sigla: 'EP', iconKey: 'handshake', rota: '/erp/emprestimo' },
        { label: 'Mov. de Estoque',  sigla: 'ME', iconKey: 'box',       rota: '/erp/movimentacao-estoque' },
        { label: 'Carteira Digital', sigla: 'CD', iconKey: 'wallet',    rota: '/erp/carteira' },
        { label: 'Conta do Cliente', sigla: 'CC', iconKey: 'user',      rota: '/erp/conta-cliente' },
        { label: 'Caixa',            sigla: 'CX', iconKey: 'cash',      rota: '/erp/caixa' },
        { label: 'Compras',          sigla: 'CP', iconKey: 'cart2',     rota: '/erp/compras' },
        { label: 'Lancar Compras',   sigla: 'LC', iconKey: 'cart2',     rota: '/erp/lancar-compras' },
        { label: 'Financeiro',       sigla: 'FN', iconKey: 'dollar',    rota: '/erp/financeiro' },
        { label: 'Fiscal',           sigla: 'FS', iconKey: 'dollar',    rota: '/erp/fiscal' },
        { label: 'ICMS por UF',      sigla: 'IC', iconKey: 'dollar',    rota: '/erp/icms-uf' },
        { label: 'Promoção Fixa',   sigla: 'PF', iconKey: 'dollar',    rota: '/erp/promocao-fixa' },
        { label: 'Promoção Progressiva', sigla: 'PP', iconKey: 'dollar', rota: '/erp/promocao-progressiva' },
        { label: 'Contas a Pagar',   sigla: 'CP', iconKey: 'dollar',    rota: '/erp/contas-pagar' },
        { label: 'Contas a Receber', sigla: 'CR', iconKey: 'dollar',    rota: '/erp/contas-receber' },
        { label: 'Adquirentes',     sigla: 'AQ', iconKey: 'dollar',    rota: '/erp/adquirentes' },
      ]
    },
    {
      nome: 'Cadastros',
      cor: '#e65100',
      tiles: [
        { label: 'Clientes',         sigla: 'CL', iconKey: 'users',     rota: '/erp/clientes' },
        { label: 'Colaboradores',    sigla: 'CO', iconKey: 'user',      rota: '/erp/colaboradores' },
        { label: 'Gerenciar Produtos', sigla: 'GP', iconKey: 'pill',    rota: '/erp/gerenciar-produtos' },
        { label: 'Fornecedores',     sigla: 'FO', iconKey: 'truck',     rota: '/erp/fornecedores' },
        { label: 'Fabricantes',      sigla: 'FB', iconKey: 'box',       rota: '/erp/fabricantes' },
        { label: 'Substancias',      sigla: 'SB', iconKey: 'pill',      rota: '/erp/substancias' },
        { label: 'NCM',              sigla: 'NC', iconKey: 'log',       rota: '/erp/ncm' },
        { label: 'Locais',           sigla: 'LC', iconKey: 'log',       rota: '/erp/locais' },
        { label: 'Plano de Contas',  sigla: 'PC', iconKey: 'log',       rota: '/erp/plano-contas' },
        { label: 'Contas Bancarias', sigla: 'CB', iconKey: 'log',       rota: '/erp/contas-bancarias' },
        { label: 'Tipos de Pagamento', sigla: 'TP', iconKey: 'log',   rota: '/erp/tipos-pagamento' },
        { label: 'Convenios',        sigla: 'CV', iconKey: 'users',    rota: '/erp/convenios' },
      ]
    },
    {
      nome: 'Relatorios',
      cor: '#6a1b9a',
      tiles: [
        { label: 'Analise de Vendas',   sigla: 'AV', iconKey: 'chart', rota: '/erp/rel/vendas' },
        { label: 'Analise de Produtos', sigla: 'AP', iconKey: 'chart', rota: '/erp/rel/produtos' },
        { label: 'Log de Auditoria',    sigla: 'LA', iconKey: 'log',   rota: '/erp/log-geral' },
      ]
    },
    {
      nome: 'Manutencao',
      cor: '#f9a825',
      tiles: [
        { label: 'Manutencao',        sigla: 'MT', iconKey: 'wrench',   rota: '/erp/manutencao' },
        { label: 'Sincronismo',      sigla: 'SI', iconKey: 'wrench',   rota: '/erp/sync' },
        { label: 'Grupo de Usuarios', sigla: 'GU', iconKey: 'lock',     rota: '/erp/grupos' },
        { label: 'Usuarios',          sigla: 'US', iconKey: 'users',    rota: '/erp/usuarios' },
        { label: 'Filiais',           sigla: 'FL', iconKey: 'building', rota: '/erp/filiais' },
        { label: 'Sistema',           sigla: 'ST', iconKey: 'gear',     rota: '/erp/sistema' },
        { label: 'Configuracoes',     sigla: 'CF', iconKey: 'gear',     rota: '/erp/configuracoes' },
        { label: 'Atual. Precos',    sigla: 'AP', iconKey: 'dollar',   rota: '/erp/atualizacao-precos' },
        { label: 'Hierarquia Descontos', sigla: 'HD', iconKey: 'wrench', rota: '/erp/hierarquia-descontos' },
      ]
    },
    {
      nome: 'Dev',
      cor: '#546e7a',
      tiles: [
        { label: 'Help',            sigla: 'HP', iconKey: 'log',      rota: '/erp/help' },
        { label: 'Dic. de Dados',   sigla: 'DD', iconKey: 'log',      rota: '/erp/dicionario-dados' },
        { label: 'Todo Board',      sigla: 'TD', iconKey: 'log',      rota: '/erp/todo-board' },
      ]
    }
  ];

  // ── Busca global ────────────────────────────────────────────────
  @ViewChild('inputBusca') inputBuscaRef!: ElementRef<HTMLInputElement>;
  buscaGlobal     = signal('');
  buscaFocada     = signal(false);
  indiceBusca     = signal(-1);

  resultadosBusca = computed<ResultadoBusca[]>(() => {
    const termo = this.normalizar(this.buscaGlobal());
    if (termo.length < 1) return [];
    const results: ResultadoBusca[] = [];
    for (const bloco of this.blocos) {
      for (const tile of bloco.tiles) {
        if (
          this.normalizar(tile.label).includes(termo) ||
          this.normalizar(tile.sigla).includes(termo) ||
          this.normalizar(bloco.nome).includes(termo)
        ) {
          results.push({ tile, bloco: bloco.nome, cor: bloco.cor });
        }
      }
    }
    return results;
  });

  mostrarDropdown = computed(() =>
    this.buscaFocada() && this.buscaGlobal().trim().length > 0
  );

  // ── User menu ──────────────────────────────────────────────────
  menuUsuarioAberto = signal(false);

  // ── Alterar senha ──────────────────────────────────────────────
  modalSenhaAberta = signal(false);
  senhaAtual       = signal('');
  novaSenha        = signal('');
  confirmarSenha   = signal('');
  erroSenha        = signal('');
  sucessoSenha     = signal('');
  salvandoSenha    = signal(false);

  constructor(
    public tabService: TabService,
    public authService: AuthService,
    private router: Router,
    public settings: ErpSettingsService,
    private http: HttpClient,
    private modal: ModalService
  ) {
    this.carregarSyncStatus();
    this.syncIntervalId = setInterval(() => this.carregarSyncStatus(), 30000);

    // Detecta se está na home para ocultar barra de título
    this.estaHome.set(this.router.url === '/erp' || this.router.url === '/erp/');
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e: any) => {
      const url: string = e.urlAfterRedirects ?? e.url;
      this.estaHome.set(url === '/erp' || url === '/erp/');
    });

    effect(() => {
      const count = this.tabService.limiteAtingido();
      if (count > 0) {
        this.modal.aviso('Limite de telas', `Você atingiu o limite de ${this.tabService.MAX_TABS} telas abertas. Feche alguma tela antes de abrir outra.`);
      }
    });
  }

  ngOnDestroy() {
    if (this.syncIntervalId) clearInterval(this.syncIntervalId);
  }

  carregarSyncStatus() {
    this.http.get<any>(`${environment.apiUrl}/sync/status`).subscribe({
      next: (r) => this.syncStatus.set(r?.data ?? null),
      error: () => {}
    });
  }

  irHome() { this.router.navigate(['/erp']); }
  logout()  { this.authService.logout(); }

  abrirPainel()  { this.painelAberto.set(true); }
  fecharPainel() { this.painelAberto.set(false); }
  setTema(t: Tema)         { this.settings.tema.set(t); }
  setFonte(f: FonteEscala) { this.settings.fonte.set(f); }

  // ── Busca methods ──────────────────────────────────────────────
  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  onBuscaInput(valor: string) {
    this.buscaGlobal.set(valor);
    this.indiceBusca.set(-1);
  }

  onBuscaKeydown(e: KeyboardEvent) {
    const total = this.resultadosBusca().length;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.indiceBusca.update(i => Math.min(i + 1, total - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.indiceBusca.update(i => Math.max(i - 1, -1));
    } else if (e.key === 'Enter') {
      const idx = this.indiceBusca();
      const results = this.resultadosBusca();
      const alvo = idx >= 0 ? results[idx] : results[0];
      if (alvo) this.navegarBusca(alvo);
    } else if (e.key === 'Escape') {
      this.fecharBusca();
    }
  }

  navegarBusca(r: ResultadoBusca) {
    this.fecharBusca();
    this.tabService.abrirTab({
      id: r.tile.rota,
      titulo: r.tile.label,
      rota: r.tile.rota,
      iconKey: r.tile.iconKey,
    });
  }

  fecharBusca() {
    this.buscaGlobal.set('');
    this.buscaFocada.set(false);
    this.indiceBusca.set(-1);
  }

  @HostListener('document:keydown', ['$event'])
  onGlobalKeydown(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      this.inputBuscaRef?.nativeElement.focus();
      this.buscaFocada.set(true);
    }
  }

  // ── User menu methods ──────────────────────────────────────────
  toggleMenuUsuario() {
    this.menuUsuarioAberto.update(v => !v);
  }

  // ── Alterar senha methods ──────────────────────────────────────
  abrirAlterarSenha() {
    this.menuUsuarioAberto.set(false);
    this.senhaAtual.set('');
    this.novaSenha.set('');
    this.confirmarSenha.set('');
    this.erroSenha.set('');
    this.sucessoSenha.set('');
    this.modalSenhaAberta.set(true);
  }

  fecharModalSenha() {
    this.modalSenhaAberta.set(false);
  }

  alterarSenha() {
    this.erroSenha.set('');
    this.sucessoSenha.set('');

    if (!this.senhaAtual()) {
      this.erroSenha.set('Informe a senha atual.');
      return;
    }
    if (!this.novaSenha() || this.novaSenha().length < 4 || this.novaSenha().length > 12) {
      this.erroSenha.set('A nova senha deve ter entre 4 e 12 caracteres.');
      return;
    }
    if (this.novaSenha() !== this.confirmarSenha()) {
      this.erroSenha.set('A nova senha e a confirmacao nao coincidem.');
      return;
    }

    this.salvandoSenha.set(true);
    this.http.post<any>(`${environment.apiUrl}/auth/alterar-senha`, {
      senhaAtual: this.senhaAtual(),
      novaSenha: this.novaSenha()
    }).subscribe({
      next: r => {
        this.salvandoSenha.set(false);
        if (r.success) {
          this.sucessoSenha.set('Senha alterada com sucesso!');
          setTimeout(() => this.fecharModalSenha(), 2000);
        } else {
          this.erroSenha.set(r.message || 'Erro ao alterar senha.');
        }
      },
      error: () => {
        this.salvandoSenha.set(false);
        this.erroSenha.set('Erro ao comunicar com o servidor.');
      }
    });
  }
}
