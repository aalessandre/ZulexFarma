import { Component, signal, computed, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TabService } from '../../core/services/tab.service';
import { AuthService } from '../../core/services/auth.service';
import { ModalService } from '../../core/services/modal.service';
import { EnterTabDirective } from '../../core/directives/enter-tab.directive';

interface LogCampo { campo: string; valorAnterior?: string; valorAtual?: string; }
interface LogEntry { id: number; realizadoEm: string; acao: string; nomeUsuario: string; campos: LogCampo[]; }

interface HierarquiaList {
  id: number;
  nome: string;
  padrao: boolean;
  totalItens: number;
  ativo: boolean;
  criadoEm: string;
}

interface HierarquiaDetalhe {
  id: number;
  nome: string;
  padrao: boolean;
  ativo: boolean;
  itens: HierarquiaItem[];
  colaboradorIds: number[];
}

interface HierarquiaItem {
  ordem: number;
  componente: number;
  secaoIds: number[];
}

interface LookupItem {
  id: number;
  nome: string;
}

interface ColunaDef {
  campo: string;
  label: string;
  largura: number;
  minLargura: number;
  padrao: boolean;
}

interface ColunaEstado extends ColunaDef {
  visivel: boolean;
}

type Modo = 'lista' | 'form';

const HIERARQUIA_COLUNAS: ColunaDef[] = [
  { campo: 'nome', label: 'Nome', largura: 240, minLargura: 120, padrao: true },
  { campo: 'padrao', label: 'Padr\u00e3o', largura: 80, minLargura: 60, padrao: true },
  { campo: 'totalItens', label: 'Itens', largura: 60, minLargura: 50, padrao: true },
  { campo: 'ativo', label: 'Ativo', largura: 60, minLargura: 50, padrao: true },
];

const COMPONENTES = [
  { valor: 1, label: 'Colaboradores', icone: 'user' },
  { valor: 2, label: 'Grupo Principal', icone: 'box' },
  { valor: 3, label: 'Grupo', icone: 'box' },
  { valor: 4, label: 'SubGrupo', icone: 'box' },
  { valor: 5, label: 'Se\u00e7\u00e3o (escolhida)', icone: 'grid' },
  { valor: 6, label: 'Se\u00e7\u00e3o (demais)', icone: 'grid' },
  { valor: 7, label: 'Comiss\u00e3o Fixa', icone: 'tag' },
  { valor: 8, label: 'Comiss\u00e3o Progressiva', icone: 'tag' },
  { valor: 9, label: 'Fabricantes', icone: 'box' },
  { valor: 10, label: 'Metas', icone: 'dollar' },
];

const ICONE_CORES: Record<string, string> = {
  tag: '#e67e22',
  grid: '#2980b9',
  pill: '#8e44ad',
  user: '#27ae60',
  users: '#16a085',
  box: '#d35400',
  dollar: '#2c3e50',
};

@Component({
  selector: 'app-hierarquia-comissoes',
  standalone: true,
  imports: [CommonModule, FormsModule, EnterTabDirective],
  templateUrl: './hierarquia-comissoes.component.html',
  styleUrl: './hierarquia-comissoes.component.scss'
})
export class HierarquiaComissoesComponent implements OnInit, OnDestroy {
  private readonly STATE_KEY = 'zulex_hierarquia_comissoes_state';
  private readonly STORAGE_KEY_COLUNAS = 'zulex_colunas_hierarquia_comissoes';

  modo = signal<Modo>('lista');
  registros = signal<HierarquiaList[]>([]);
  selecionado = signal<HierarquiaList | null>(null);
  carregando = signal(false);
  salvando = signal(false);
  excluindo = signal(false);
  busca = signal('');
  filtroStatus = signal<'ativos' | 'inativos' | 'todos'>('ativos');
  sortColuna = signal<string>('nome');
  sortDirecao = signal<'asc' | 'desc'>('asc');
  modoEdicao = signal(false);
  isDirty = signal(false);
  editandoId = signal<number | null>(null);

  // Colunas
  colunas = signal<ColunaEstado[]>(this.carregarColunas());
  colunasVisiveis = computed(() => this.colunas().filter(c => c.visivel));
  painelColunas = signal(false);
  private resizeState: { campo: string; startX: number; startWidth: number } | null = null;
  private dragColIdx: number | null = null;

  // Form signals
  fNome = signal('');
  fPadrao = signal(false);
  fAtivo = signal(true);
  itens = signal<HierarquiaItem[]>([]);
  colaboradorIds = signal<Set<number>>(new Set());

  // Lookups
  colaboradores = signal<LookupItem[]>([]);
  secoes = signal<LookupItem[]>([]);

  // Multi-select dropdowns
  colabDropdownAberto = signal(false);

  colabSelecionadosLabel = computed(() => {
    const ids = this.colaboradorIds();
    const lista = this.colaboradores().filter(c => ids.has(c.id));
    if (lista.length === 0) return 'Nenhum selecionado';
    if (lista.length <= 3) return lista.map(c => c.nome).join(', ');
    return `${lista.length} selecionados`;
  });

  // Accordion
  accVinculacoes = signal(false);

  // Drag for hierarchy items
  private dragItemIdx: number | null = null;

  // Log
  modalLog = signal(false);
  logRegistros = signal<LogEntry[]>([]);
  logSelecionado = signal<LogEntry | null>(null);
  logDataInicio = signal('');
  logDataFim = signal('');
  carregandoLog = signal(false);

  private apiUrl = `${environment.apiUrl}/hierarquiacomissoes`;
  private tokenLiberacao: string | null = null;

  readonly COMPONENTES = COMPONENTES;
  readonly ICONE_CORES = ICONE_CORES;

  constructor(private http: HttpClient, private tabService: TabService, private auth: AuthService, private modal: ModalService) {}

  private async verificarPermissao(acao: string): Promise<boolean> {
    if (this.auth.temPermissao('hierarquia-comissoes', acao)) return true;
    const resultado = await this.modal.permissao('hierarquia-comissoes', acao);
    if (resultado.tokenLiberacao) this.tokenLiberacao = resultado.tokenLiberacao;
    return resultado.confirmado;
  }

  private headerLiberacao(): { [h: string]: string } {
    if (this.tokenLiberacao) {
      const h = { 'X-Liberacao': this.tokenLiberacao };
      this.tokenLiberacao = null;
      return h;
    }
    return {};
  }

  ngOnInit() {
    this.carregar();
    this.carregarLookups();
  }

  ngOnDestroy() {
    sessionStorage.removeItem(this.STATE_KEY);
  }

  sairDaTela() {
    sessionStorage.removeItem(this.STATE_KEY);
    this.tabService.fecharTabAtiva();
  }

  // ── Data ───────────────────────────────────────────────────────────
  carregar() {
    this.carregando.set(true);
    this.http.get<any>(this.apiUrl).subscribe({
      next: r => {
        this.registros.set(r.data ?? []);
        this.carregando.set(false);
      },
      error: (e) => {
        this.carregando.set(false);
        if (e.status === 403) {
          this.modal.permissao('hierarquia-comissoes', 'c').then(r => {
            if (r.confirmado) this.carregar();
            else this.tabService.fecharTabAtiva();
          });
        }
      }
    });
  }

  private carregarLookups() {
    this.http.get<any>(`${environment.apiUrl}/colaboradores`).subscribe({
      next: r => this.colaboradores.set((r.data ?? []).map((x: any) => ({ id: x.id, nome: x.nome }))),
      error: () => {}
    });
    this.http.get<any>(`${environment.apiUrl}/secoes`).subscribe({
      next: r => this.secoes.set((r.data ?? []).map((x: any) => ({ id: x.id, nome: x.nome }))),
      error: () => {}
    });
  }

  registrosFiltrados = computed(() => {
    const termo = this.normalizar(this.busca());
    const status = this.filtroStatus();
    const col = this.sortColuna();
    const dir = this.sortDirecao();

    const lista = this.registros().filter(f => {
      if (status === 'ativos'   && !f.ativo) return false;
      if (status === 'inativos' &&  f.ativo) return false;
      if (termo.length < 2) return true;
      return this.normalizar(f.nome).includes(termo);
    });

    if (!col) return lista;
    return [...lista].sort((a, b) => {
      const va = (a as any)[col] ?? '';
      const vb = (b as any)[col] ?? '';
      const cmp = typeof va === 'boolean'
        ? (va === vb ? 0 : va ? -1 : 1)
        : typeof va === 'number'
          ? va - (vb as number)
          : String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  private normalizar(s: string): string {
    return (s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
  }

  getCellValue(f: HierarquiaList, campo: string): string {
    const v = (f as any)[campo];
    if (typeof v === 'boolean') return v ? 'Sim' : 'N\u00e3o';
    return v ?? '';
  }

  selecionar(f: HierarquiaList) { this.selecionado.set(f); }

  ordenar(coluna: string) {
    if (this.sortColuna() === coluna) {
      this.sortDirecao.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColuna.set(coluna);
      this.sortDirecao.set('asc');
    }
  }

  sortIcon(campo: string): string { return this.sortColuna() === campo ? (this.sortDirecao() === 'asc' ? '\u25B2' : '\u25BC') : '\u21C5'; }

  // ── Columns ────────────────────────────────────────────────────────
  private carregarColunas(): ColunaEstado[] {
    try {
      const json = localStorage.getItem(this.STORAGE_KEY_COLUNAS);
      if (json) {
        const saved: ColunaEstado[] = JSON.parse(json);
        return HIERARQUIA_COLUNAS.map(def => {
          const s = saved.find(c => c.campo === def.campo);
          return { ...def, visivel: s ? s.visivel : def.padrao, largura: s?.largura ?? def.largura };
        });
      }
    } catch {}
    return HIERARQUIA_COLUNAS.map(c => ({ ...c, visivel: c.padrao }));
  }

  private salvarColunasStorage() {
    localStorage.setItem(this.STORAGE_KEY_COLUNAS, JSON.stringify(this.colunas()));
  }

  toggleColunaVisivel(campo: string) {
    this.colunas.update(cols => cols.map(c => c.campo === campo ? { ...c, visivel: !c.visivel } : c));
    this.salvarColunasStorage();
  }

  restaurarPadrao() {
    this.colunas.set(HIERARQUIA_COLUNAS.map(c => ({ ...c, visivel: c.padrao })));
    this.salvarColunasStorage();
  }

  iniciarResize(e: MouseEvent, campo: string, largura: number) {
    e.stopPropagation(); e.preventDefault();
    this.resizeState = { campo, startX: e.clientX, startWidth: largura };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    if (!this.resizeState) return;
    const delta = e.clientX - this.resizeState.startX;
    const def = HIERARQUIA_COLUNAS.find(c => c.campo === this.resizeState!.campo);
    const min = def?.minLargura ?? 50;
    const novaLargura = Math.max(min, this.resizeState.startWidth + delta);
    this.colunas.update(cols =>
      cols.map(c => c.campo === this.resizeState!.campo ? { ...c, largura: novaLargura } : c)
    );
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    if (!(e.target as HTMLElement).closest('.ms-wrap')) {
      this.colabDropdownAberto.set(false);
    }
  }

  @HostListener('document:mouseup')
  onMouseUp() {
    if (this.resizeState) {
      this.salvarColunasStorage();
      this.resizeState = null;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }
  }

  onDragStartCol(idx: number) { this.dragColIdx = idx; }
  onDragOverCol(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragColIdx === null || this.dragColIdx === idx) return;
    this.colunas.update(cols => { const arr = [...cols]; const [moved] = arr.splice(this.dragColIdx!, 1); arr.splice(idx, 0, moved); this.dragColIdx = idx; return arr; });
  }
  onDropCol() { this.dragColIdx = null; this.salvarColunasStorage(); }

  // ── CRUD ───────────────────────────────────────────────────────────
  async incluir() {
    if (!await this.verificarPermissao('i')) return;
    this.resetForm();
    this.modoEdicao.set(false);
    this.editandoId.set(null);
    this.isDirty.set(false);
    this.modo.set('form');
  }

  async editar() {
    if (!await this.verificarPermissao('a')) return;
    const sel = this.selecionado();
    if (!sel?.id) return;
    this.carregando.set(true);
    this.http.get<any>(`${this.apiUrl}/${sel.id}`).subscribe({
      next: r => {
        this.carregando.set(false);
        const d: HierarquiaDetalhe = r.data;
        this.fNome.set(d.nome);
        this.fPadrao.set(d.padrao);
        this.fAtivo.set(d.ativo);
        this.itens.set(d.itens ?? []);
        this.colaboradorIds.set(new Set(d.colaboradorIds ?? []));
        this.editandoId.set(d.id);
        this.modoEdicao.set(true);
        this.isDirty.set(false);
        this.accVinculacoes.set(false);
        this.modo.set('form');
      },
      error: () => {
        this.carregando.set(false);
        this.modal.erro('Erro', 'Erro ao carregar detalhes da hierarquia.');
      }
    });
  }

  fechar() {
    this.modo.set('lista');
    this.carregar();
  }

  fecharForm() {
    this.modo.set('lista');
    this.carregar();
  }

  async salvar() {
    if (!await this.verificarPermissao(this.modoEdicao() ? 'a' : 'i')) return;

    const nome = this.fNome().trim();
    if (!nome) {
      this.modal.erro('Valida\u00e7\u00e3o', 'O nome da hierarquia \u00e9 obrigat\u00f3rio.');
      return;
    }

    if (this.itens().length === 0) {
      this.modal.erro('Valida\u00e7\u00e3o', 'Adicione pelo menos um componente \u00e0 hierarquia.');
      return;
    }

    this.salvando.set(true);
    const headers = this.headerLiberacao();

    const body = {
      nome,
      padrao: this.fPadrao(),
      ativo: this.fAtivo(),
      itens: this.itens(),
      colaboradorIds: Array.from(this.colaboradorIds()),
    };

    const salvar$ = this.modoEdicao()
      ? this.http.put(`${this.apiUrl}/${this.editandoId()}`, body, { headers })
      : this.http.post<any>(this.apiUrl, body, { headers });

    salvar$.subscribe({
      next: () => {
        this.salvando.set(false);
        this.isDirty.set(false);
        this.carregar();
        this.modo.set('lista');
      },
      error: () => {
        this.salvando.set(false);
        this.modal.erro('Erro', 'Erro ao salvar hierarquia de comiss\u00e3o.');
      }
    });
  }

  async excluir() {
    const sel = this.selecionado();
    if (!sel?.id) return;
    const resultado = await this.modal.confirmar(
      'Confirmar Exclus\u00e3o',
      `Deseja excluir a hierarquia "${sel.nome}"? O registro ser\u00e1 removido permanentemente. Se estiver em uso, ser\u00e1 apenas desativado.`,
      'Sim, excluir',
      'N\u00e3o, manter'
    );
    if (!resultado.confirmado) return;
    if (!await this.verificarPermissao('e')) return;
    const headers = this.headerLiberacao();
    this.excluindo.set(true);
    this.http.delete<any>(`${this.apiUrl}/${sel.id}`, { headers }).subscribe({
      next: async (r: any) => {
        this.excluindo.set(false);
        this.selecionado.set(null);
        this.carregar();
        if (this.modo() === 'form') this.modo.set('lista');
        if (r.resultado === 'desativado') {
          await this.modal.aviso('Desativado', 'O registro est\u00e1 em uso e foi apenas desativado.');
        }
      },
      error: () => {
        this.excluindo.set(false);
        this.modal.erro('Erro', 'Erro ao excluir hierarquia.');
      }
    });
  }

  // ── Form helpers ───────────────────────────────────────────────────
  private resetForm() {
    this.fNome.set('');
    this.fPadrao.set(false);
    this.fAtivo.set(true);
    this.itens.set([]);
    this.colaboradorIds.set(new Set());
    this.accVinculacoes.set(false);
  }

  markDirty() {
    this.isDirty.set(true);
  }

  // ── Hierarchy builder ──────────────────────────────────────────────
  componentesDisponiveis = computed(() => {
    const usados = new Set(this.itens().map(i => i.componente));
    return COMPONENTES.filter(c => !usados.has(c.valor));
  });

  adicionarComponente(valor: number) {
    const atual = this.itens();
    const novoItem: HierarquiaItem = {
      ordem: atual.length + 1,
      componente: valor,
      secaoIds: [],
    };
    this.itens.set([...atual, novoItem]);
    this.isDirty.set(true);
  }

  removerComponente(idx: number) {
    const atual = [...this.itens()];
    atual.splice(idx, 1);
    this.itens.set(atual.map((it, i) => ({ ...it, ordem: i + 1 })));
    this.isDirty.set(true);
  }

  onDragStartItem(idx: number) {
    this.dragItemIdx = idx;
  }

  onDragOverItem(event: DragEvent, idx: number) {
    event.preventDefault();
    if (this.dragItemIdx === null || this.dragItemIdx === idx) return;
    const arr = [...this.itens()];
    const [moved] = arr.splice(this.dragItemIdx, 1);
    arr.splice(idx, 0, moved);
    this.dragItemIdx = idx;
    this.itens.set(arr.map((it, i) => ({ ...it, ordem: i + 1 })));
  }

  onDropItem() {
    this.dragItemIdx = null;
    this.isDirty.set(true);
  }

  componenteLabel(valor: number): string {
    return COMPONENTES.find(c => c.valor === valor)?.label ?? `Componente ${valor}`;
  }

  componenteIcone(valor: number): string {
    return COMPONENTES.find(c => c.valor === valor)?.icone ?? 'box';
  }

  componenteCor(valor: number): string {
    const icone = this.componenteIcone(valor);
    return ICONE_CORES[icone] ?? '#546e7a';
  }

  // Secao selection for item (componente 5 = SecaoEscolhida)
  toggleSecaoItem(itemIdx: number, secaoId: number) {
    const arr = [...this.itens()];
    const item = { ...arr[itemIdx] };
    const sids = new Set(item.secaoIds);
    if (sids.has(secaoId)) sids.delete(secaoId);
    else sids.add(secaoId);
    item.secaoIds = Array.from(sids);
    arr[itemIdx] = item;
    this.itens.set(arr);
    this.isDirty.set(true);
  }

  secaoSelecionada(itemIdx: number, secaoId: number): boolean {
    return this.itens()[itemIdx]?.secaoIds?.includes(secaoId) ?? false;
  }

  // ── Vinculacoes ────────────────────────────────────────────────────
  toggleColaborador(id: number) {
    const s = new Set(this.colaboradorIds());
    if (s.has(id)) s.delete(id);
    else s.add(id);
    this.colaboradorIds.set(s);
    this.isDirty.set(true);
  }

  // ── Log ────────────────────────────────────────────────────────────
  abrirLog() {
    const sel = this.modoEdicao() ? this.editandoId() : this.selecionado()?.id;
    if (!sel) return;
    this.modalLog.set(true);
    this.logRegistros.set([]);
    this.logSelecionado.set(null);
    this.filtrarLog();
  }

  fecharLog() { this.modalLog.set(false); }

  filtrarLog() {
    const id = this.modoEdicao() ? this.editandoId() : this.selecionado()?.id;
    if (!id) return;
    this.carregandoLog.set(true);
    let url = `${this.apiUrl}/${id}/log`;
    const params: string[] = [];
    if (this.logDataInicio()) params.push(`dataInicio=${this.logDataInicio()}`);
    if (this.logDataFim()) params.push(`dataFim=${this.logDataFim()}`);
    if (params.length) url += '?' + params.join('&');

    this.http.get<any>(url).subscribe({
      next: r => {
        this.logRegistros.set(r.data ?? []);
        this.carregandoLog.set(false);
        if (r.data?.length > 0) this.selecionarLogEntry(r.data[0]);
      },
      error: () => this.carregandoLog.set(false)
    });
  }

  selecionarLogEntry(entry: LogEntry) { this.logSelecionado.set(entry); }

  acaoCss(acao: string): string {
    const map: Record<string, string> = {
      'CRIA\u00c7\u00c3O': 'log-badge badge-criacao',
      'ALTERA\u00c7\u00c3O': 'log-badge badge-alteracao',
      'EXCLUS\u00c3O': 'log-badge badge-exclusao',
      'DESATIVA\u00c7\u00c3O': 'log-badge badge-desativacao'
    };
    return map[acao] ?? 'log-badge';
  }

  // ── Utils ──────────────────────────────────────────────────────────
  trackById(_: number, item: any) { return item.id; }
}
